using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if UNITY_URP
using Needle.XR.ARSimulation.Compatibility;
#endif
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
#endif

#pragma warning disable 649, 414

namespace Needle.XR.ARSimulation.Simulation
{
	internal enum RealitySimulationSceneLoading
	{
		// Never = 0,
		EditAndPlayMode = 1,
		// OnlyPlayMode = 2,
	}

	/// <summary>
	/// Enables rendering part of your scene as the AR camera image
	/// </summary>
	[ExecuteAlways]
	[DefaultExecutionOrder(10)]
	public class SimulatedAREnvironmentManager : MonoBehaviour
	{
		/// <summary>
		/// Searches for all possible SimulatedAREnvironments in scene and assets (if in editor)
		/// </summary>
		public static void FindAvailableEnvironments(List<(SimulatedAREnvironment, bool inScene)> list) =>
			SimulatedAREnvironmentHelpers.FindAllAvailableSimulatedAREnvironments(list);

		/// <summary>
		/// Checks if a reality simulation manager exists in scene (accessing <see cref="Instance"/> will create a new instance if it does not exist yet)
		/// </summary>
		/// <returns>true if an instance of <see cref="SimulatedAREnvironmentManager"/> exists</returns>
		public static bool Exists => instance ? instance : FindObjectOfType<SimulatedAREnvironmentManager>();

		private static SimulatedAREnvironmentManager instance;

		/// <summary>
		/// Get or create a <see cref="SimulatedAREnvironmentManager"/> instance
		/// </summary>
		/// <returns>Instance of <see cref="SimulatedAREnvironmentManager"/></returns>
		public static SimulatedAREnvironmentManager Instance
		{
			get
			{
				if (instance) return instance;
				instance = FindObjectOfType<SimulatedAREnvironmentManager>();
				if (instance) return instance;
				var realityManager = new GameObject("ARSimulation Environment Manager");
				instance = realityManager.AddComponent<SimulatedAREnvironmentManager>();
				return instance;
			}
		}

		public bool IsActive => true;

#pragma warning disable 414
		public static event Action<SimulatedAREnvironmentManager> WillChangeEnvironment = default;
		public static event Action<SimulatedAREnvironmentManager> SceneChangedOrRecreated = default;
#pragma warning restore

		public IReadOnlyList<GameObject> SceneInstances => instantiatedGameObjects;

		public bool InvalidEnvironment { get; private set; }
		public string ErrorMessage { get; private set; }

		/// <summary>
		/// Camera used in ARSessionOrigin
		/// </summary>
		private Camera ARCamera
		{
			get
			{
				if (!_arCamera)
					SceneSetup.TryGetARCamera(out _arCamera);
				return _arCamera;
			}
		}

		private bool ARCameraIsActive => ARCamera && ARCamera.isActiveAndEnabled;
		private bool ARCameraActiveStateChanged => ARCameraIsActive != _previousARCameraActiveState;

		private Camera _arCamera;
		private bool _previousARCameraActiveState;


		/// <summary>
		/// Reference a <see cref="SceneAsset"/>, prefab or <see cref="GameObject"/> that is the simulated environment (the manager will create a instance of the assigned object for rendering)
		/// </summary>
		[FormerlySerializedAs("SceneOrPrefab")] [Header("Environment")] [Tooltip("Can be a scene asset, a prefab or a GameObject in the scene")]
		public UnityEngine.Object Environment;

		/// <summary>
		/// If true the instantiated simulated environment <see cref="GameObject">GameObjects</see> will not be editable
		/// </summary>
		public bool NotEditable = true;

		// /// <summary>
		// /// Factor to scale the rendered camera image resolution
		// /// </summary>
		// /// <returns>factor to scale the camera texture resolution</returns>
		// [Header("Environment Camera Image")] 
		// [Range(0.01f, 2)] public float Quality = 1;

		/// <summary>
		/// If set to none we use the Default RenderTextureFormat
		/// </summary>
		[Tooltip("Camera image format")] public GraphicsFormat Format = GraphicsFormat.None;

		/// <summary>
		/// The <see cref="RenderTexture"/> to render the simulated environment to
		/// </summary>
		// [FormerlySerializedAs("RealitySimulationRT")] 
		public RenderTexture EnvironmentCameraRT => internalRT;

		/// <summary>
		/// Experimental feature: dont save environment to the scene
		/// </summary>
		/// <returns>if true: simulated environment instances are not saved to the scene</returns>
		[Header("Experimental")] public bool NotSaved = true;

		/// <summary>
		/// If true the instantiated simulated environment <see cref="GameObject">GameObjects</see> will be hidden in the editor hierarchy
		/// </summary>
		[Tooltip("Experimental: If true the instantiated simulated environment GameObjects will be hidden in the editor hierarchy")]
		public bool HideInHierarchy = false;

		[Tooltip("Experimental: If true the instantiated simulated environment GameObjects will be hidden in scene view. Does not work with HideInHierarchy")]
		public bool HideInSceneView = false;

		/// <summary>
		/// If assigned the simulated environment will be placed at the position and rotation of this transform
		/// </summary>
		[Tooltip("Experimental: If assigned the simulated environment will be placed at the position and rotation of this transform")]
		public Transform EnvironmentPose;

		[Header("Experimental: Camera Placement")] [SerializeField]
		private CameraPlacementAtStart cameraPlacement = CameraPlacementAtStart.UseSceneCamera;
		

		private enum CameraPlacementAtStart
		{
			DoNothing = 0,
			FindRandomPlacement = 1,
			UseSceneCamera = 2,
			UseEnvironmentDefaultPose = 3
		}

		/// <summary>
		/// The layer mask used for rendering the simulated environment
		/// </summary>
		/// <returns>mask layer number</returns>
		public static int RealityLayerMask = 31; // TODO: rename

		public static LayerMask SimulatedEnvironmentMask => (1 << RealityLayerMask);
		public static int ExcludeEnvironment(int layer) => layer & ~SimulatedEnvironmentMask;
		public static int IncludeEnvironment(int layer) => layer | SimulatedEnvironmentMask;

		[SerializeField, HideInInspector] private Object previousRealitySceneOrPrefab;
		[SerializeField, HideInInspector] private bool isValidRealityScene;

		[FormerlySerializedAs("realityCamera")] [SerializeField, HideInInspector]
		internal Camera environmentCamera;

		private RenderTexture internalRT;
		private RealitySimulationSceneLoading previousLoading;

		// [SerializeField, HideInInspector] private string currentEnvironmentScenePath;
		// [SerializeField, HideInInspector] private Scene currentEnvironmentSceneInstance;
		[SerializeField, HideInInspector] private UnityEngine.Object currentEnvironmentPrefabInstance;

		[SerializeField, HideInInspector] internal GameObject instantiatedGameObjectRoot;
		[SerializeField, HideInInspector] internal List<GameObject> instantiatedGameObjects = new List<GameObject>();

		[SerializeField, HideInInspector] private Vector3 sceneCameraPosition;
		[SerializeField, HideInInspector] private Quaternion sceneCameraRotation;
		
		[SerializeField, HideInInspector] private bool _rendererFeatureAdded;

#if UNITY_EDITOR

		/// <summary>
		/// Check if a <see cref="GameObject"/> is part of a simulated reality instance
		/// </summary>
		/// <param name="go">The <see cref="GameObject"/> to check</param>
		/// <returns>true if part of reality instance</returns>
		public bool IsPartOfSimulationInstance(GameObject go)
		{
			if (!go) return false;
			return instantiatedGameObjects != null && instantiatedGameObjects.Contains(go);
		}

		public bool TryGetARCameraClearFlags(out CameraClearFlags flags)
		{
			if (ARCamera)
			{
				flags = ARCamera.clearFlags;
				return true;
			}

			flags = 0;
			return false;
		}

		public void FixMainCameraClearFlags()
		{
			if (ARCamera && ARCamera.clearFlags == CameraClearFlags.Skybox)
			{
				Undo.RegisterCompleteObjectUndo(ARCamera, "Change clear flags");
				ARCamera.clearFlags = CameraClearFlags.Color;
			}
		}

		private bool requestedRecreate = false;

		/// <summary>
		/// Force to destroy and create new instances of simulated environment <see cref="GameObject"/>s
		/// </summary>
		[ContextMenu(nameof(RequestRecreate))]
		public async void RequestRecreate()
		{
			if (BuildPipeline.isBuildingPlayer) return;
			if (requestedRecreate) return;
			requestedRecreate = true;
			// TODO: do we need this delay here still?
			await Task.Delay(1);
			requestedRecreate = false;
			DetectIfRealitySceneHasChanged(true);
		}

		public async void UpdateIfChanged()
		{
			await Task.Delay(1);
			DetectIfRealitySceneHasChanged(true);
		}

		[ContextMenu(nameof(Disable))]
		private void Disable()
		{
			this.enabled = false;
			this.UnloadPreviousInstance(true);
		}

		private RealitySimulationSceneLoading RealitySimulationLoading = RealitySimulationSceneLoading.EditAndPlayMode;

		private void Awake()
		{
			instance = this;
			RequestRecreate();
		}

		private async void OnEnable()
		{
			if (!gameObject.activeInHierarchy) return;
			if (BuildPipeline.isBuildingPlayer) return;

			ARSimulationProjectInfo.RenderPipelineChanged += OnPipelineChanged;
			EditorSceneManager.sceneSaving += OnSaving;
			EditorSceneManager.sceneSaved += OnSaved;
#if UNITY_URP
			SetupRendererURPCameraImageFeature();
#endif
			SetupCommandBuffer(true);

			if (!Application.isPlaying)
				await Task.Delay(1);
			if (this && enabled)
			{
				// if (!Application.isPlaying)
				DetectIfRealitySceneHasChanged(true);
			}

			if (Application.isPlaying)
			{
				var background = FindObjectOfType<ARCameraBackground>();
				if (background && background.enabled)
				{
					Debug.Log("Disabling AR Camera background", this);
					background.enabled = false;
				}
			}

			if (!Application.isPlaying)
				EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			ARSimulationProjectInfo.RenderPipelineChanged -= OnPipelineChanged;
			EditorSceneManager.sceneSaving -= OnSaving;
			EditorSceneManager.sceneSaved -= OnSaved;
			RemoveCommandBuffer();
			this.UnloadPreviousInstance(true);
			if (!Application.isPlaying)
				EditorApplication.update -= OnEditorUpdate;
		}

		private async void OnValidate()
		{
			if (BuildPipeline.isBuildingPlayer) return;
			if (IsPartOfSimulationInstance(Environment as GameObject) || (environmentCamera && Environment == environmentCamera.gameObject))
			{
				Debug.Log("Referencing an object in simulation instance or the environment camera is not allowed", this);
				Environment = previousRealitySceneOrPrefab;
				return;
			}

			if (!this.enabled) return;
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;
			if (!Selection.Contains(this.gameObject)) return;

			SetupCommandBuffer(true);
			await Task.Delay(1);
			if (this && isActiveAndEnabled)
			{
				DetectIfRealitySceneHasChanged();
			}

			ApplySettings();
		}

		private void OnSaving(Scene scene, string path)
		{
			if (NotSaved)
				UnloadPreviousInstance(true);
		}

		private void OnSaved(Scene scene)
		{
			if (NotSaved)
				DetectIfRealitySceneHasChanged(true);
		}

		private void OnEditorUpdate()
		{
			if (Application.isPlaying) return;
			if(!SceneView.lastActiveSceneView) return;
			var sceneCam = SceneView.lastActiveSceneView.camera;
			if (sceneCam)
			{
				var sceneCamTransform = sceneCam.transform;
				sceneCameraPosition = sceneCamTransform.position;
				sceneCameraRotation = sceneCamTransform.rotation;
			}
		}

#if UNITY_URP
		[ContextMenu(nameof(SetupRendererURPCameraImageFeature))]
		private void SetupRendererURPCameraImageFeature()
		{
			_rendererFeatureAdded = ARSimulationCameraBackgroundRendererFeature.AutomaticSupport.Run();
		}
#endif

		private void LateUpdate()
		{
			if (BuildPipeline.isBuildingPlayer) return;
			var envChanged = EnvironmentReferenceChanged();
			if (envChanged)
			{
				DetectIfRealitySceneHasChanged(false);
				SetupCommandBuffer(true);
			}
			
			if(ARCameraIsActive && ARCameraActiveStateChanged) 
				StartCoroutine(RecreateCommandBufferNextFrame());

			SyncSimulation();
			
			_previousARCameraActiveState = ARCameraIsActive;
		}

		private IEnumerator RecreateCommandBufferNextFrame()
		{
			yield return null;
			SetupCommandBuffer(true);
		}

		private void OnPipelineChanged((CurrentRenderPipelineType previous, CurrentRenderPipelineType current) obj)
		{
			if (BuildPipeline.isBuildingPlayer) return;
			SetupCommandBuffer(true);
			DetectIfRealitySceneHasChanged();
			SyncSimulation();
		}

		private bool EnvironmentReferenceChanged() => previousRealitySceneOrPrefab != Environment;

		private void UnloadPreviousInstance(bool destroyInstances)
		{
			RemoveCommandBuffer();

			previousLoading = RealitySimulationLoading;

			isValidRealityScene = Environment && (
				Environment is SceneAsset ||
				Environment is GameObject);


			if (previousRealitySceneOrPrefab && previousRealitySceneOrPrefab is GameObject previousGameObject)
			{
				var isAsset = EditorUtility.IsPersistent(previousGameObject);
				if (!isAsset && (EditorSelectionExtensions.AnyChildSelected(previousGameObject, true)))
					previousGameObject.SetActive(true);
			}

			if (currentEnvironmentPrefabInstance && currentEnvironmentPrefabInstance != Environment)
			{
				if (Application.isPlaying)
					Destroy(currentEnvironmentPrefabInstance);
				else DestroyImmediate(currentEnvironmentPrefabInstance);
				currentEnvironmentPrefabInstance = null;
			}

			if (destroyInstances)
			{
				if (instantiatedGameObjectRoot)
				{
					if (Application.isPlaying) Destroy(instantiatedGameObjectRoot);
					else DestroyImmediate(instantiatedGameObjectRoot);
				}

				instantiatedGameObjectRoot = null;
				if (environmentCamera)
				{
					if (Application.isPlaying) Destroy(environmentCamera.gameObject);
					else DestroyImmediate(environmentCamera.gameObject);
				}

				environmentCamera = null;

				instantiatedGameObjects.Clear();
			}
		}


		private void DetectIfRealitySceneHasChanged(bool force = false)
		{
			if (BuildPipeline.isBuildingPlayer) return;
			if (!force && instantiatedGameObjects.Count > 0)
			{
				if (!EnvironmentReferenceChanged())
					return;
				if (previousLoading == RealitySimulationLoading && previousRealitySceneOrPrefab == Environment)
					return;
			}

			if (Environment is GameObject prefab)
			{
				if (prefab && prefab.GetComponentsInChildren<SimulatedAREnvironmentManager>().Any(s => s && s == this))
				{
					InvalidEnvironment = true;
					ErrorMessage = nameof(SimulatedAREnvironmentManager) + " can not be part of the assigned scene object";
					Debug.LogError(ErrorMessage, this);
					Environment = previousRealitySceneOrPrefab;
					return;
				}
			}

			WillChangeEnvironment?.Invoke(this);

			if (previousRealitySceneOrPrefab && previousRealitySceneOrPrefab != Environment)
				SceneSetup.ClearPreviousData();

			UnloadPreviousInstance(true);

			if (this && !isActiveAndEnabled && SceneSetup.ARSimulationIsEnabled()) return;

			previousRealitySceneOrPrefab = Environment;

			if (!Environment && Environment != null)
			{
				Debug.Log("Referenced object is destroyed or missing", this);
				return;
			}

			if (Environment is GameObject go)
			{
				var meshing = FindObjectOfType<ARMeshManager>();
				var planeDetection = FindObjectOfType<ARPlaneManager>();
				if ((meshing && meshing.enabled || planeDetection && planeDetection.enabled) 
				    && go.TryGetComponent(out SimulatedAREnvironment env) && env.AllowAutoOptimizeColliders)
				{
					env.Optimize();
#if UNITY_EDITOR
					if (EditorUtility.IsPersistent(go))
					{
						if (!AssetDatabase.GetAssetPath(go).StartsWith("Packages/"))
						{
							EditorUtility.SetDirty(go);
							AssetDatabase.SaveAssets();
						};
					}
#endif
				}
				
				if (!go)
				{
					InvalidEnvironment = true;
					ErrorMessage = "Referenced environment is destroyed or missing";
					Debug.LogError(ErrorMessage, this);
					return;
				}
#if UNITY_EDITOR
				var inst = PrefabUtility.InstantiatePrefab(go) as GameObject;
				if (!inst) inst = Instantiate(go);
#else
                var inst = Instantiate(go);
#endif
				inst.SetActive(true);
				currentEnvironmentPrefabInstance = inst;
				SetupEnvironmentInstances(inst);

				var isAsset = EditorUtility.IsPersistent(go);
				if (!isAsset)
					go.SetActive(false);
			}
			else if (Environment)
			{
				Debug.LogWarning("Assigned object is not supported: " + Environment);
			}

			ErrorMessage = null;
			InvalidEnvironment = false;
		}

		private void SetupEnvironmentInstances(GameObject environmentRootInstance)
		{
			if (this && enabled && ARCamera)
			{
				ARCamera.cullingMask = ExcludeEnvironment(ARCamera.cullingMask);
			}

			var foundMeshCollider = false;
			void SetupLayerMaskRecursively(GameObject go, Action<GameObject> callback)
			{
				instantiatedGameObjects.Add(go);
				ApplySettings(go);

				callback?.Invoke(go);

				foreach (var component in go.GetComponents<Component>())
				{
					foundMeshCollider |= component is MeshCollider mf && mf.sharedMesh;
					
					if (component is Light lightComponent)
					{
						// lights should only affect simulated environment
						lightComponent.cullingMask &= ~RealityLayerMask;
						// lights should be editable
						lightComponent.hideFlags &= ~HideFlags.NotEditable;
						lightComponent.transform.hideFlags &= ~HideFlags.NotEditable;
						lightComponent.gameObject.hideFlags &= ~HideFlags.NotEditable;
					}

					if (component is Collider)
						InternalEditorUtility.SetIsInspectorExpanded(component, false);
				}

				for (var i = 0; i < go.transform.childCount; i++)
					SetupLayerMaskRecursively(go.transform.GetChild(i).gameObject, callback);
			}

			instantiatedGameObjectRoot = new GameObject("Simulated AR Environment Instance");
			if (NotEditable)
				instantiatedGameObjectRoot.hideFlags = HideFlags.NotEditable;
			else instantiatedGameObjectRoot.hideFlags &= ~HideFlags.NotEditable;
			instantiatedGameObjectRoot.layer = RealityLayerMask;
			instantiatedGameObjects.Add(instantiatedGameObjectRoot);
			ApplySettings(instantiatedGameObjectRoot);

			for (var i = 0; i < 31; i++)
				Physics.IgnoreLayerCollision(RealityLayerMask, i, true);

			var simParent = instantiatedGameObjectRoot.transform; // .camera.transform.parent;
			environmentRootInstance.transform.SetParent(simParent, true);
			if (!environmentCamera)
				environmentCamera = environmentRootInstance.GetComponentInChildren<Camera>();
			SetupLayerMaskRecursively(environmentRootInstance, null);
			

			if (!foundMeshCollider && Application.isPlaying)
			{
				var meshing = FindObjectOfType<ARMeshManager>();
				if (meshing && meshing.enabled)
				{
					if (EditorUtility.DisplayDialog("Environment not setup for meshing",
						"Your scene is using ARMeshManager but " + Environment.name +
						" does not contain any MeshCollider components (or no mesh collider has a mesh assigned)." +
						"\n\nDo you want to add MeshCollider components to the instance of this environment. This will NOT change your asset persistently!",
						"Yes add MeshCollider", "No, do nothing"))
					{
						SetupLayerMaskRecursively(environmentRootInstance, go =>
						{
							if (go.TryGetComponent(out Collider col) && go.TryGetComponent(out MeshFilter mf))
							{
								if (col is MeshCollider mc)
								{
									mc.sharedMesh = mf.sharedMesh;
									return;
								}
								Destroy(col);
								mc = go.AddComponent<MeshCollider>();
								mc.sharedMesh = mf.sharedMesh;
							}
						});
					}
				}
			}
			
			environmentRootInstance.SetActive(true);

			if (EnvironmentPose)
			{
				var t = environmentRootInstance.transform;
				t.position = EnvironmentPose.position;
				t.rotation = EnvironmentPose.rotation;
			}

			if (!environmentCamera)
			{
				environmentCamera = new GameObject("Simulated AR Environment Camera").AddComponent<Camera>();
				environmentCamera.gameObject.AddComponent<SimulatedEnvironmentRenderHelper>();
			}

			ApplySettings(environmentCamera.gameObject);

			if (Application.isPlaying)
			{
				// set simulation camera HideFlags so "FindObjectOfType" will not find this camera
				environmentCamera.hideFlags = HideFlags.HideAndDontSave;
			}

			if (cameraPlacement != CameraPlacementAtStart.DoNothing)
				StartCoroutine(TryFindValidARCameraPlacement());
			SetupCommandBuffer(true);
			SyncSimulation();
			SceneChangedOrRecreated?.Invoke(this);

			if (ARCamera.renderingPath == RenderingPath.DeferredLighting)
				Debug.LogWarning("Camera Image with deferred rendering is currently not supported", this);
			
			// if (Application.isPlaying)
			// {
			// 	var lights = FindObjectsOfType<Light>();
			// 	foreach (var _light in lights)
			// 	{
			// 		if (environmentLights.Contains(_light)) continue;
			// 		if ((_light.cullingMask & SimulatedEnvironmentMask) != 0)
			// 		{
			// 			Undo.RegisterCompleteObjectUndo(_light, "Cull simulated environment");
			// 			_light.cullingMask = ExcludeEnvironment(_light.cullingMask);
			// 		}
			// 	}
			// }

			InternalEditorUtility.RepaintAllViews();
		}

		[ContextMenu(nameof(TryFindValidARCameraPlacement))]
		private void TryFindValidARCameraPlacementMenu() => StartCoroutine(TryFindValidARCameraPlacement());

		private bool firstTimePlacedCamera = true;

		private IEnumerator TryFindValidARCameraPlacement()
		{
			if (!Application.isPlaying) yield break;
			if (!ARCamera) yield break;
			yield return new WaitForEndOfFrame();

			var t = ARCamera.transform;
			var et = instantiatedGameObjectRoot.transform;

			void Set(Vector3 pt, Quaternion rot)
			{
				t.position = pt;
				t.rotation = rot;
				if (t.TryGetComponent(out IOverrideablePositionRotation ov))
					ov.SetPositionAndRotation(t.position, t.rotation);
			}

			bool UsePoseConfiguredInEnvironment()
			{
				var env = instantiatedGameObjectRoot.GetComponentInChildren<SimulatedAREnvironment>();
				if(env && env.TryGetDefaultCameraPose(currentEnvironmentPrefabInstance as GameObject, out var localPosition, out var localRotation))
				{
					var pos = et.TransformPoint(localPosition);
					var dir = et.TransformDirection(localRotation);
					var rot = Quaternion.Euler(dir);
					Set(pos, rot);
					return true;
				}
				return false;
			}

			switch (cameraPlacement)
			{
				case CameraPlacementAtStart.UseSceneCamera:
					if(firstTimePlacedCamera)
						Set(sceneCameraPosition, sceneCameraRotation);
					else
					{
						UsePoseConfiguredInEnvironment();
					}
					firstTimePlacedCamera = false;
					break;
				
				case CameraPlacementAtStart.UseEnvironmentDefaultPose:
					if (!UsePoseConfiguredInEnvironment())
					{
						Debug.LogWarning("<b>No default environment position configured for \"" + (Environment ? Environment.name : "<?>") + "\"</b>, to set an environment default pose open the prefab and set the camera pose in the SimulatedEnvironment component", Environment);
						Set(sceneCameraPosition, sceneCameraRotation);
					}
					break;

				case CameraPlacementAtStart.FindRandomPlacement:

					bool TryGetRandomPoint(out Vector3 pt)
					{
						var ray = new Ray(et.position + Vector3.up + Random.insideUnitSphere, Random.insideUnitSphere);
						if (Physics.Raycast(ray, out var hit, 5, SimulatedEnvironmentMask))
						{
							pt = hit.point;
							return true;
						}

						pt = Vector3.zero;
						return false;
					}

					var lowestPoint = new Vector3(0, float.MaxValue, 0);
					for (var i = 0; i < 10; i++)
					{
						if (TryGetRandomPoint(out var pt))
						{
							if (pt.y < lowestPoint.y)
								lowestPoint = pt;
						}
					}

					if (lowestPoint.y < float.MaxValue)
						Set(lowestPoint + Vector3.up * 1.7f, Quaternion.Euler(45, Random.value * 360f, 0));
					break;
			}
		}

		[ContextMenu("Internal/SetSimulationCameraEditable")]
		private void SetSimulationCameraEditable() => environmentCamera.hideFlags = HideFlags.None;

		private void ApplySettings()
		{
			if (ARCamera && ARCamera.clearFlags == CameraClearFlags.Skybox)
				Debug.LogWarning("Main camera clear flags must not be set to " + CameraClearFlags.Skybox + " for AR reality simulation", ARCamera);

			foreach (var inst in instantiatedGameObjects)
				ApplySettings(inst);
			if (environmentCamera)
				ApplySettings(environmentCamera.gameObject);

			InternalEditorUtility.RepaintAllViews();
		}

		private void ApplySettings(GameObject go)
		{
			if (!go) return;

			go.layer = RealityLayerMask;
			go.tag = "EditorOnly";

			if (HideInHierarchy)
				go.hideFlags |= HideFlags.HideInHierarchy;
			else
				go.hideFlags &= ~HideFlags.HideInHierarchy;

			// settings HideFlags to HideAndDontSave makes onenable and ondisable being called multiple times the same frame 
			if (NotEditable)
				go.hideFlags |= HideFlags.NotEditable;
			else
				go.hideFlags &= ~HideFlags.NotEditable;

#if UNITY_2019_3_OR_NEWER
			SceneVisibilityManager.instance.DisablePicking(go, false);
#endif
			if (HideInSceneView)
				SceneVisibilityManager.instance.Hide(go, false);
			else SceneVisibilityManager.instance.Show(go, false);
		}

		private CommandBuffer renderCameraBackground;
		private CameraEvent renderCameraEvent = CameraEvent.BeforeForwardOpaque;
		private CameraEvent previousCameraEvent; // for debugging


		private void SetupCommandBuffer(bool force)
		{
			// if (!EnvironmentCameraRT) return;
			if (force || renderCameraBackground == null || renderCameraEvent != previousCameraEvent || (ARCameraIsActive && ARCameraActiveStateChanged))
			{
				if (!ARCameraIsActive) return;
				if (renderCameraBackground != null) ARCamera.RemoveCommandBuffer(previousCameraEvent, renderCameraBackground);
				renderCameraEvent = CameraEvent.BeforeForwardOpaque;
				previousCameraEvent = renderCameraEvent;
				if (ARSimulationProjectInfo.CurrentRenderPipeline != CurrentRenderPipelineType.Builtin) return;
				renderCameraBackground = new CommandBuffer() {name = "Render Simulated AR Environment"};
				renderCameraBackground.ClearRenderTarget(true, true, Color.black);
				var mat = ARSimulationProjectInfo.CreateRenderCameraImageMaterial();
				// renderCameraBackground.Blit(internalRT, RealitySimulationRT);
				renderCameraBackground.Blit(internalRT, BuiltinRenderTextureType.CurrentActive, mat);
				ARCamera.AddCommandBuffer(renderCameraEvent, renderCameraBackground);
			}
		}

		private void RemoveCommandBuffer()
		{
			if (renderCameraBackground != null && ARCamera) ARCamera.RemoveCommandBuffer(previousCameraEvent, renderCameraBackground);
			renderCameraBackground = null;
		}

		private GraphicsFormat format;

		private void SyncSimulation()
		{
			if (!environmentCamera) return;

			// if (EnvironmentCameraRT)
			{
				// Quality = Mathf.Abs(Quality);
				// if (Quality < 0.00001f)
				// {
				//     Debug.LogWarning("A value of zero for " + nameof(Quality) + " is not allowed");
				//     Quality = 0.00001f;
				// }

				if (!internalRT || internalRT.width != Screen.width || internalRT.height != Screen.height || format != Format)
				{
					if (internalRT && internalRT.IsCreated())
						internalRT.Release();
					if (Format != GraphicsFormat.None && !SystemInfo.IsFormatSupported(Format, FormatUsage.Render))
					{
						Debug.LogError("Format is not supported: " + Format, this);
						Format = GraphicsFormat.None;
					}

					format = Format;
					// if format is set to None use the default RenderTexture format
					internalRT = Format == GraphicsFormat.None
						? new RenderTexture(Screen.width, Screen.height, 1, RenderTextureFormat.Default)
						: new RenderTexture(Screen.width, Screen.height, 1, Format);
					internalRT.autoGenerateMips = false;
					internalRT.Create();
					SetupCommandBuffer(true);
				}

				environmentCamera.targetTexture = internalRT;
				environmentCamera.cullingMask = IncludeEnvironment(0);
				// Graphics.Blit(internalRT, EnvironmentCameraRT);
			}

			if (!ARCamera || !ARCamera.isActiveAndEnabled) return;
			var envCamTransform = environmentCamera.transform;
			var arCamTransform = ARCamera.transform;
			var camPose = new Pose(arCamTransform.position, arCamTransform.rotation);
			var sessionSpace = arCamTransform.parent;
			bool hasSessionSpace = sessionSpace;

			// if you dont have a scene setup for AR but setup the reality sim
			// it's possible for the camera to not be in session space
			// in that case we dont need to transform our simulation camera
			// at runtime tho a session is expected to be present
			if (!Application.isPlaying)
				hasSessionSpace &= arCamTransform.GetComponentInParent<ARSessionOrigin>();
			else if (!hasSessionSpace && Time.frameCount == 300)
				Debug.LogWarning("The main camera is expected to be in ARSessionOrigin hierarchy", this);

			if (hasSessionSpace)
				camPose = sessionSpace.InverseTransformPose(camPose);
			envCamTransform.position = camPose.position;
			envCamTransform.rotation = camPose.rotation;
			environmentCamera.fieldOfView = ARCamera.fieldOfView;
			environmentCamera.focalLength = ARCamera.focalLength;
			environmentCamera.nearClipPlane = Mathf.Min(.1f, ARCamera.nearClipPlane);
			environmentCamera.farClipPlane = Mathf.Max(100, ARCamera.farClipPlane);
			environmentCamera.usePhysicalProperties = ARCamera.usePhysicalProperties;
			if (ARSimulationProjectInfo.CurrentRenderPipeline == CurrentRenderPipelineType.Builtin)
			{
				environmentCamera.clearFlags = CameraClearFlags.Color;
				environmentCamera.backgroundColor = ARCamera.backgroundColor;
			}
		}

#endif
	}
}