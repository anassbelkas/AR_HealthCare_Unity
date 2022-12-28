using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEditor;
using UnityEditor.PackageManager;

#endif

namespace Needle.XR.ARSimulation.Simulation
{
	/// <summary>
	/// Used to mark a <see cref="GameObject">GameObject</see>
	/// to be used as a simulated reality scene provider for <see cref="SimulatedAREnvironmentManager"/>
	/// <para>
	/// Add this component to a gameobject and set <see cref="IsActive"/> to true
	/// to render the object including children as camera image
	/// </para>
	/// <para>
	/// NOTE: if another scene or gameobject is currently rendered as a camera image
	/// it will be swapped with the new one
	/// </para>
	/// </summary>
	[ExecuteAlways]
	public class SimulatedAREnvironment : MonoBehaviour
	{
		public bool IsActive
		{
			get => _isActive;
			private set => _isActive = value;
		}

		[Header("Settings")] public bool DisableGameObjectIfInactive = true;
		[Tooltip("When enabled this will ")] public bool AllowAutoOptimizeColliders = true;

		public const int MaxVertices = 20_000;

		[SerializeField, HideInInspector] public List<OptimizedMesh> OptimizedColliders = new List<OptimizedMesh>();
		[SerializeField, HideInInspector] private bool _isActive;

		// [SerializeField, HideInInspector] private bool hasDefaultCameraPose;
		// [SerializeField] private Vector3 defaultCameraLocalPosition;
		// [SerializeField, HideInInspector] private Vector3 defaultCameraLocalRotation;


#if UNITY_EDITOR
		private GameObject GetPrefabOrInstance(GameObject instance)
		{
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			#if UNITY_2019_4
			var exists = prefabStage != null;
			#else
			var exists = (bool)prefabStage;
			#endif
			if (exists && prefabStage.IsPartOfPrefabContents(this.gameObject))
			{
#if UNITY_2019_4
				var prefabPath = prefabStage.prefabAssetPath;
#else
				var prefabPath = prefabStage.assetPath;
#endif
				instance = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
				return instance;
			}
			
			if (PrefabUtility.IsPartOfPrefabInstance(this.gameObject))
			{
				var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this.gameObject);
				instance = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			}
			
			return instance;
		}
		
		public bool TryGetDefaultCameraPose(GameObject environment, out Vector3 localPosition, out Vector3 localRotation)
		{
			if (ARSimulationLoaderSettings.TryGetDefaultPose(GetPrefabOrInstance(environment), out localPosition, out localRotation))
				return true;

			// if (hasDefaultCameraPose)
			// {
			// 	localPosition = defaultCameraLocalPosition;
			// 	localRotation = defaultCameraLocalRotation;
			// 	return true;
			// }
			// localPosition = Vector3.zero;
			// localRotation = Vector3.zero;
			return false;
		}
		
		[ContextMenu(nameof(CaptureDefaultCameraPoseFromEditorSceneCamera))]
		public void CaptureDefaultCameraPoseFromEditorSceneCamera()
		{
			if (!SceneView.lastActiveSceneView) return;
			var sceneCam = SceneView.lastActiveSceneView.camera;
			if (!sceneCam) return;
			var t = sceneCam.transform;
			var pos = transform.InverseTransformPoint(t.position);
			var rot =  transform.InverseTransformDirection(t.rotation.eulerAngles);
			
			// if(!PrefabUtility.IsPartOfImmutablePrefab(this.gameObject))
			// {
			// 	hasDefaultCameraPose = true;
			// 	defaultCameraLocalPosition = pos;
			// 	defaultCameraLocalRotation = rot;
			// 	EditorUtility.SetDirty(this);
			// }

			var prefab = GetPrefabOrInstance(this.gameObject);
			if(ARSimulationLoaderSettings.Instance)
			{
				ARSimulationLoaderSettings.SetDefaultPose(prefab, pos, rot);
			}
		}
#endif

		[ContextMenu(nameof(Activate))]
		public void Activate()
		{
#if UNITY_EDITOR
			if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
#endif
			IsActive = true;
#if UNITY_EDITOR
			InternalUpdate();
#endif
		}

		[ContextMenu(nameof(Deactivate))]
		public void Deactivate()
		{
#if UNITY_EDITOR
			if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;
#endif
			IsActive = false;
#if UNITY_EDITOR
			InternalUpdate();
#endif
		}

		[ContextMenu(nameof(UpdateState))]
		public void UpdateState()
		{
#if UNITY_EDITOR
			InternalUpdate();
#endif
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			TryAdd(this);

			if (Application.isPlaying) return;

			SimulatedAREnvironmentManager.SceneChangedOrRecreated -= OnEnvironmentManagerChanged;
			SimulatedAREnvironmentManager.SceneChangedOrRecreated += OnEnvironmentManagerChanged;

			// if(!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

			// dont update if in prefab mode
			if (EditorUtility.IsPersistent(this.gameObject))
				return;
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			if(EditorApplication.isCompiling || EditorApplication.isUpdating) 
				return;
			if (PrefabStageUtility.GetCurrentPrefabStage() != null) 
				return;
			AsyncValidate();
		}

		private bool evaluating;

		private async void AsyncValidate()
		{
			evaluating = true;
			await Task.Delay(10);
			evaluating = false;
			InternalUpdate();
		}

		private void InternalUpdate()
		{
			if (!this || !this.gameObject) return;
			if (SimulatedAREnvironmentManager.Exists && SimulatedAREnvironmentManager.Instance.IsPartOfSimulationInstance(this.gameObject))
			{
				return;
			}

			if (IsActive)
			{
				SimulatedAREnvironmentManager.Instance.Environment = this.gameObject;
				SimulatedAREnvironmentManager.Instance.UpdateIfChanged();
				SimulatedAREnvironmentManager.Instance.enabled = true;
				SimulatedAREnvironmentManager.Instance.FixMainCameraClearFlags();
			}
			else if (SimulatedAREnvironmentManager.Exists && SimulatedAREnvironmentManager.Instance.Environment == this.gameObject)
			{
				SimulatedAREnvironmentManager.Instance.Environment = null;
				SimulatedAREnvironmentManager.Instance.UpdateIfChanged();
			}
		}

		private void OnDestroy()
		{
			SimulatedAREnvironmentManager.SceneChangedOrRecreated -= OnEnvironmentManagerChanged;
		}

		private GameObject _gizmoPrefab;
		private void OnDrawGizmosSelected()
		{
			void DrawFrustum(Vector3 _pos, Vector3 _rot)
			{
				Gizmos.color = Color.yellow;
				// Gizmos.matrix = transform.localToWorldMatrix;
				var rot = Quaternion.Euler(transform.TransformDirection(_rot));
				// Gizmos.DrawLine(defaultCameraLocalPosition, defaultCameraLocalPosition + rot * Vector3.forward);
				Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(_pos), rot, Vector3.one);
				Gizmos.DrawFrustum(Vector3.zero, 60, 0.1f, 3f, Screen.width / (float)Screen.height);
			}

			if(!_gizmoPrefab)
				_gizmoPrefab = GetPrefabOrInstance(this.gameObject);
			if (TryGetDefaultCameraPose(_gizmoPrefab, out var position, out var rotation))
			{
				DrawFrustum(position, rotation);
			}
		}

		[ContextMenu(nameof(Optimize))]
		public void Optimize()
		{
			SimulatedEnvironmentUtils.OptimizeForMeshing(this.gameObject, MaxVertices, ref OptimizedColliders);
		}

		public bool IsCurrentEnvironment => this && gameObject && SimulatedAREnvironmentManager.Exists &&
		                                    SimulatedAREnvironmentManager.Instance.Environment == this.gameObject;
		
		private void OnEnvironmentManagerChanged(SimulatedAREnvironmentManager obj)
		{
			if (SimulatedAREnvironmentManager.Exists && !evaluating && this)
			{
				IsActive = IsCurrentEnvironment;
			}

			if (!IsActive && DisableGameObjectIfInactive && this && gameObject)
			{
				if (!EditorUtility.IsPersistent(this.gameObject))
				{
					var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
					if (prefabStage == null || prefabStage.prefabContentsRoot != gameObject)
						this.gameObject.SetActive(false);
				}
			}
		}

		internal static bool TryAdd(string path)
		{
			if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(GameObject))
			{
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (asset.GetComponent<SimulatedAREnvironment>())
				{
					var guid = AssetDatabase.AssetPathToGUID(path);
					if (!ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent.Contains(guid))
					{
						ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent.Add(guid);
						return true;
					}
				}
			}

			return false;
		}

		internal static void TryAdd(SimulatedAREnvironment obj)
		{
			var path = AssetDatabase.GetAssetPath(obj);
			var guid = AssetDatabase.AssetPathToGUID(path);
			if (!ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent?.Contains(guid) ?? false)
				ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent.Add(guid);
		}

		private class SimulatedAREnvironmentImport : AssetPostprocessor
		{
			private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
			{
				if (ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent == null) return;
				foreach (var path in importedAssets) TryAdd(path);
				foreach (var moved in movedAssets) TryAdd(moved);
			}
		}
#endif
	}
}