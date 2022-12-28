using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;
using Task = System.Threading.Tasks.Task;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.SceneManagement;

#endif

#if !UNITY_AR_FOUNDATION_4_OR_NEWER
using Needle.XR.ARSimulation.Interfaces;
#endif

#if UNITY_NEW_INPUT_SYSTEM && UNITY_EDITOR
using UnityEditor.Presets;
#endif

// ReSharper disable Unity.InefficientPropertyAccess
#pragma warning disable CS0414

namespace Needle.XR.ARSimulation.Simulation
{
	internal static class SceneSetup
	{
		#region Clear

		private static bool isCleaningUp = false;
		internal static event Action BeforeCleanupData = default, PostCleanupData = default;

		internal static void ClearPreviousData()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
			if (isCleaningUp) return;
			isCleaningUp = true;
			// we need to give all simulated components some time to unregister
			BeforeCleanupData?.Invoke();
			const int updatesToWait = 2;
			var updates = 0;

			void OnUpdate()
			{
				++updates;
				if (updates < updatesToWait) return;
				EditorApplication.update -= OnUpdate;
				if (!isCleaningUp) return;
				isCleaningUp = false;
				MeshManager.Clear();
				PostCleanupData?.Invoke();
			}

			EditorApplication.update += OnUpdate;
#endif
		}

		#endregion

		internal static bool ARSimulationIsEnabled()
		{
#if UNITY_EDITOR
			if (!XRGeneralSettings.Instance)
				XRGeneralSettings.Instance = XRGeneralSettings.Instance
					? XRGeneralSettings.Instance
					: Resources.FindObjectsOfTypeAll<XRGeneralSettings>().FirstOrDefault(s => s.name.ToLowerInvariant().Contains("standalone"));
#endif
			if (!XRGeneralSettings.Instance || XRGeneralSettings.Instance == null) return false;
			if (!XRGeneralSettings.Instance.Manager) return false;
#if UNITY_XR_MANAGEMENT_4_0_1_OR_NEWER
            var loaders = XRGeneralSettings.Instance.Manager.activeLoaders;
#else
			var loaders = XRGeneralSettings.Instance.Manager.loaders;
#endif
			if (loaders == null) return false;
			var foundARSimLoader = loaders.Any(l => l is ARSimulationLoader);
			return foundARSimLoader;
		}

#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		// ReSharper disable once UnusedMember.Local
		private static void EnterPlayMode()
		{
			if (BuildPipeline.isBuildingPlayer) return;
			if (!ARSimulationIsEnabled())
			{
				return;
			}

			if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
			EditorApplication.playModeStateChanged += OnPlayModeChange;
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private static bool firstLoad = true;

		private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
		{
			if (firstLoad)
			{
				firstLoad = false;
				return;
			}

			SetupScene(true);
		}

		private static void OnPlayModeChange(PlayModeStateChange obj)
		{
			switch (obj)
			{
				case PlayModeStateChange.ExitingEditMode:
					EnforceIdentityCameraPositionAtStartup(true);
					break;
				case PlayModeStateChange.EnteredPlayMode:
					SetupScene(true);
					break;
			}
		}
#endif

		private static ARSessionOrigin sessionOrigin;
		private static bool didWarnAboutMissingSessionOrigin;

		internal static bool TryGetARCamera(out Camera cam)
		{
			if (!sessionOrigin)
			{
				sessionOrigin = FindObjectOfType<ARSessionOrigin>(true);

				if (sessionOrigin && sessionOrigin.camera)
				{
					cam = sessionOrigin.camera;
					return true;
				}

				if (!Settings || Settings.allowAutoSetupLogging)
				{
					if (!didWarnAboutMissingSessionOrigin && ARSimulationIsEnabled())
					{
						didWarnAboutMissingSessionOrigin = true;
						Debug.LogWarning(
							"No ARSessionOrigin was found in this scene, using main camera or first camera found in scene. " +
							"This can happen if your scene is not setup for AR during automatic setup",
							sessionOrigin);
					}
				}

				cam = Camera.main;

				if (!cam)
					cam = Camera.allCameras.FirstOrDefault();

				return cam;
			}

			cam = sessionOrigin.camera;
			return cam;
		}

		private static ARSimulationLoaderSettings settings;

		private static ARSimulationLoaderSettings Settings
		{
			get
			{
				if (!settings) settings = ARSimulationLoader.GetSettings();
				return settings;
			}
		}

		// ReSharper disable once SimplifyConditionalTernaryExpression
		private static bool AllowLogging => Settings ? Settings.allowAutoSetupLogging : false;


#if UNITY_EDITOR
		[MenuItem("Tools/AR Simulation/Convert to Basic AR Scene")]
#endif
		// ReSharper disable once UnusedMember.Local
		private static void SetupARFoundationSceneMenuItem()
		{
			EnsureARSessionOrigin(false, "AR Session Origin");
			EnsureARSession("AR Session");
			EnforceIdentityCameraPositionAtStartup(false);
		}

#if UNITY_EDITOR
		[MenuItem("Tools/AR Simulation/Convert to Extended AR Scene")]
#endif
		// ReSharper disable once UnusedMember.Local
		private static void SetupSceneMenuItem()
		{
			SetupScene(false);
		}

		// ReSharper disable once MemberCanBePrivate.Global
		internal static void SetupScene(bool isAutomatic)
		{
			if (isAutomatic && Settings != null && !Settings.allowAutoSceneSetup)
			{
				// TODO: check if camera input components exist and are setup correctly and if not log a warning
				return;
			}

			// only run automatic setup if we actually have an AR Session in the scene
			if (isAutomatic)
			{
				var session = FindObjectOfType<ARSession>(true);
				if (!session)
				{
					if (Settings && Settings.allowAutoSetupLogging)
						Debug.Log("No AR Session in scene, no automatic setup");
					return;
				}
			}

			EnsureARSessionOrigin(isAutomatic, "AR Simulation Session Origin");
			EnsureARSession("AR Session");
			SetupARObjects();
			SetupInputDevice(isAutomatic);
			EnforceColliderOnTrackablePlanes();
			SetupCameraBackground();
			EnforceIdentityCameraPositionAtStartup(isAutomatic);
		}

		private static async void SetupARObjects()
		{
#if !UNITY_AR_FOUNDATION_4_OR_NEWER
            do
            {
                await Task.Delay(5);
            } while (cameraPositionProcessIsActive);
#endif

			await Task.Delay(30);
			// var realitySim = FindObjectOfType<RealitySimulationManager>();
			if (Settings == null || Settings.allowAutoPlaneSpawn)
				SetupDefaultARPlanes();
			if (Settings == null || Settings.allowAutoPointCloudSpawn)
				SetupDefaultARPointClouds();
			if (Settings == null || Settings.allowAutoAnchorSpawn)
				SetupDefaultAnchors();
			if (Settings == null || Settings.allowAutoTrackedImageSpawn)
				SetupDefaultTrackedImage();
			if (Settings == null || Settings.allowAutoTrackedObjectSpawn)
				SetupDefaultTrackedObject();
			if (Settings == null || Settings.allowAutoMeshingSpawn)
				SetupDefaultMeshing();
		}

		private static void SetupCameraBackground()
		{
			var simManager = FindObjectOfType<SimulatedAREnvironmentManager>(true);
			if (simManager)
			{
			}
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
			else if (Application.isPlaying)
			{
				// if we dont have a manager that handles camera image rendering
				// just disable the component
				var bg = FindObjectOfType<ARCameraBackground>(true);
				if (bg && bg.enabled)
				{
#if UNITY_EDITOR
					Undo.RegisterCompleteObjectUndo(bg, "Modified AR Camera Background");
#endif
					bg.enabled = false;
					if (AllowLogging)
						Debug.Log("Disabled ARCameraBackground", bg);
				}
			}
#endif
		}

		private static void EnsureARSession(string resourceName)
		{
			var session = FindObjectOfType<ARSession>(true);
			if (!session)
			{
				InstantiateResourceWithUndo(resourceName);
			}

			// ensure ar input manager
			var inputManager = FindObjectOfType<ARInputManager>(true);
			if (!inputManager)
			{
				session = FindObjectOfType<ARSession>(true);
				if (session) session.gameObject.AddComponent<ARInputManager>();
			}
		}

		private static void EnsureARSessionOrigin(bool isAutomatic, string resourceName)
		{
			var origin = FindObjectOfType<ARSessionOrigin>(true);
			bool sceneWasAlreadySetupWithArSessionOrigin = origin;

			if (sceneWasAlreadySetupWithArSessionOrigin && isAutomatic) return;

			var currentCamera = !sceneWasAlreadySetupWithArSessionOrigin ? Camera.main : origin.camera;

			if (AllowLogging)
				Debug.Log("Active Main Camera found: " + currentCamera, currentCamera);

#if UNITY_EDITOR
			if (currentCamera)
				Undo.RegisterFullObjectHierarchyUndo(currentCamera, "Modify Main Cam");
#endif
			var instance = InstantiateResourceWithUndo(resourceName);

			if (currentCamera != null)
			{
				var instanceOrigin = instance.GetComponent<ARSessionOrigin>();

				if (origin)
					CopyComponentsFromTo(instanceOrigin.gameObject, origin.gameObject, false);

				var instantiatedCamera = instanceOrigin.camera;
				// if the scene already has a main camera we want to keep that
				// so we transfer to components that are currently on the session ar camera
				// to the current main camera and afterwards
				// we can delete the instantiated ar camera
				if (instantiatedCamera != currentCamera)
				{
					if (!instantiatedCamera)
					{
						instantiatedCamera = currentCamera;
						instantiatedCamera.transform.parent = instanceOrigin.transform;
					}

					if (isAutomatic)
						Debug.LogWarning("Auto setup scene for AR." +
						                 "Use \"Tools/ARSimulation/Setup Scene\" to setup your scene for development/production.\n" +
						                 "This behaviour can be configured in \"Project Settings/XR Plug-in Management/AR Simulation\".");

					if (!currentCamera.GetComponentInParent<ARSessionOrigin>())
						currentCamera.transform.SetParent(instantiatedCamera.transform.parent, true);
					CopyComponentsFromTo(instantiatedCamera.gameObject, currentCamera.gameObject, false);

					if (!sceneWasAlreadySetupWithArSessionOrigin)
					{
						if (!Application.isPlaying) Object.DestroyImmediate(instantiatedCamera.gameObject);
						else Object.Destroy(instantiatedCamera.gameObject);
					}

					instanceOrigin.camera = currentCamera;
					if (instanceOrigin.camera.nearClipPlane >= 0.29f)
						instanceOrigin.camera.nearClipPlane = 0.05f;
					if (instanceOrigin.camera.farClipPlane >= 500)
						instanceOrigin.camera.farClipPlane = 100;
				}
			}

			if (sceneWasAlreadySetupWithArSessionOrigin)
			{
				if (!Application.isPlaying) Object.DestroyImmediate(instance.gameObject);
				else Object.Destroy(instance.gameObject);
			}

			TryRemoveInvalidComponentsFromCamera();
		}

		private static void CopyComponentsFromTo(GameObject from, GameObject to, bool pasteValues)
		{
#if UNITY_EDITOR
			// transfer components from instantiated ar camera to existing camera in scene
			foreach (var component in from.GetComponents<Component>())
			{
				if (component is Transform) continue;
				if (component is Camera) continue;
				if (component is AudioListener) continue;

				if (!ComponentUtility.CopyComponent(component))
				{
					if (AllowLogging)
						Debug.LogWarning("Could not copy component values from " + component, from);
					continue;
				}

				var existingComponent = to.GetComponent(component.GetType());
				if (existingComponent)
				{
					if (pasteValues)
					{
#if UNITY_EDITOR
						Undo.RecordObject(existingComponent, "Paste Values to " + existingComponent);
#endif
						ComponentUtility.PasteComponentValues(existingComponent);
					}
				}
				else
				{
					ComponentUtility.PasteComponentAsNew(to.gameObject);
				}
			}
#endif // end unity editor
		}

		private static GameObject InstantiateResourceWithUndo(string resourceName)
		{
			var res = Resources.Load(resourceName);
			if (res)
			{
				// Debug.Log("Found " + resourceName, res);
				var instance = Object.Instantiate(res);
				instance.name = res.name;
#if UNITY_EDITOR
				Undo.RegisterCreatedObjectUndo(instance, "Created " + resourceName);
#endif
				// Debug.Log("Created " + resourceName, instance);
				return instance as GameObject;
			}
			else if (AllowLogging)
				Debug.LogWarning("Missing " + resourceName + " Resource?", res);

			return null;
		}

		private static void SetupInputDevice(bool isAutomatic)
		{
			var tracked = FindObjectOfType<TrackedPoseDriver>(true);
			if (tracked)
			{
				if (tracked.UseRelativeTransform)
				{
					if (AllowLogging) Debug.Log("Disabling \"UseRelativeTransform\" on TrackedPoseDriver", tracked);
				}

				if (tracked.originPose != Pose.identity)
				{
					if (AllowLogging) Debug.Log("Settings \"OriginPose\" on TrackedPoseDriver to identity", tracked);
				}

				tracked.UseRelativeTransform = false;
				tracked.originPose = Pose.identity;
			}

			if (SetupInputDeviceNewInputSystem(isAutomatic)) return;

			if (!FindObjectOfType<SimulatedARPoseProvider>(true))
			{
				if (tracked)
				{
#if UNITY_EDITOR
					var prov = Undo.AddComponent<SimulatedARPoseProvider>(tracked.gameObject);
#else
					var prov = tracked.gameObject.AddComponent<SimulatedARPoseProvider>();
#endif
					prov.setTransformDirectly = false;
					tracked.originPose = Pose.identity;
					tracked.poseProviderComponent = prov;
					tracked.UseRelativeTransform = false;
					if (AllowLogging)
						Debug.Log("Added " + nameof(SimulatedARPoseProvider) + " and assigned to " + nameof(TrackedPoseDriver), prov.gameObject);
				}
				else
				{
					// in some cases scenes use ARPoseDriver, we update the transform in late update
					var arPoseDriver = FindObjectOfType<ARPoseDriver>(true);
					if (arPoseDriver)
					{
#if UNITY_EDITOR
						var prov = Undo.AddComponent<SimulatedARPoseProvider>(arPoseDriver.gameObject);
#else
						var prov = arPoseDriver.gameObject.AddComponent<SimulatedARPoseProvider>();
#endif
						prov.setTransformDirectly = true;
						if (AllowLogging)
							Debug.Log("Added " + nameof(SimulatedARPoseProvider) + " because " + nameof(ARPoseDriver) + " was found.", prov.gameObject);
					}
				}
			}
		}

		/// <summary>
		/// search for desktop input device, if not found add it to the scene
		/// </summary>
		/// <returns>true if new input system installed and input device is setup up or found</returns>
		private static bool SetupInputDeviceNewInputSystem(bool isAutomatic)
		{
#if UNITY_NEW_INPUT_SYSTEM
            if (AllowLogging) Debug.Log("Setup input for new input system");
            GameObject targetGameObject = null;

            var desktopPose = FindObjectOfType<SimulatedARPoseProvider>(true);
            if (desktopPose)
            {
                targetGameObject = desktopPose.gameObject;
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Object.Destroy(desktopPose);
                else
                    Undo.DestroyObjectImmediate(desktopPose);
#endif
            }

            var tpd = FindObjectOfType<TrackedPoseDriver>(true);
            if (tpd)
            {
                targetGameObject = tpd.gameObject;
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Object.Destroy(tpd);
                else
                    Undo.DestroyObjectImmediate(tpd);
#endif
            }

            var desktopInputDevice = FindObjectOfType<SimulatedARInputDevice>(true);
            if (desktopInputDevice) targetGameObject = desktopInputDevice.gameObject;

            if (!targetGameObject || targetGameObject == null)
            {
                if (TryGetARCamera(out var cam))
                    targetGameObject = cam.gameObject;
                else
                    targetGameObject = new GameObject("AR Simulation Input Device");
            }

            var newInputTrackedPoseDriver = FindObjectOfType<UnityEngine.InputSystem.XR.TrackedPoseDriver>(true);
            if (!newInputTrackedPoseDriver) newInputTrackedPoseDriver = targetGameObject.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
#if UNITY_EDITOR
            var pr = Resources.Load<Preset>("TrackedPoseDriver");
            if (pr && pr.CanBeAppliedTo(newInputTrackedPoseDriver))
                pr.ApplyTo(newInputTrackedPoseDriver);
#endif
            newInputTrackedPoseDriver.enabled = false;
            newInputTrackedPoseDriver.enabled = true;
            newInputTrackedPoseDriver.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;

            if (!desktopInputDevice)
            {
                var go = targetGameObject.AddComponent<SimulatedARInputDevice>();
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(go, "Created DesktopInputDevice");
#endif
                if (AllowLogging)
                    Debug.Log("Added AR Simulation Input Device to support New Input System", go);
            }


            var arPoseDriver = FindObjectOfType<ARPoseDriver>(true);
            if (arPoseDriver)
            {
                if (Application.isPlaying) Object.Destroy(arPoseDriver);
                else Object.DestroyImmediate(arPoseDriver);
            }

            // EnforceIdentityCameraPositionAtStartup(isAutomatic);

            return true;
#else
			// new input system is not installed
			return false;
#endif
		}


#if !UNITY_AR_FOUNDATION_4_OR_NEWER
        private static bool cameraPositionProcessIsActive = false;
#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IOS)
        private static bool didEnforceCameraPosition = false;
#endif
#endif

#pragma warning disable 1998
		private static async void EnforceIdentityCameraPositionAtStartup(bool automaticSetup)
#pragma warning restore 1998
		{
#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IOS) // ON MOBILE DEVICES DONT MOVE SESSION SPACE

#if UNITY_AR_FOUNDATION_4_OR_NEWER
			// not sure why this breaks now
			// but accessing the main camera transform position/rotation
			// unity crashes since ARFoundation 4
			return;
#else
            // ReSharper disable once InlineOutVariableDeclaration
            Camera cam = null;
            if (didEnforceCameraPosition) return;
            if (!Application.isPlaying) return;
            if (cameraPositionProcessIsActive) return;
            cameraPositionProcessIsActive = true;

            if (!TryGetARCamera(out cam))
            {
                cameraPositionProcessIsActive = false;
                return;
            }

            var t = cam.transform;
            if (t)
            {
                var p = t.position;
                var r = t.rotation;
                if (p != Vector3.zero || r != Quaternion.identity)
                {
                    t.position = Vector3.zero;
                    t.rotation = Quaternion.identity;
                    if (AllowLogging)
                        Debug.Log("Captured camera start position " + p + " and rotation " + r, t);
                    await Task.Delay(5);
                    if (TryGetARCamera(out cam))
                    {
                        var overrideable = cam.GetComponent<IOverrideablePositionRotation>();
                        if (AllowLogging)
                            Debug.Log(
                                "Set Camera position to start position " + p + " and rotation: " + r.eulerAngles + ", found overrideable? " + overrideable,
                                cam);
                        if (overrideable != null)
                        {
                            overrideable.SetPositionAndRotation(p, r);
                        }
                        else
                        {
                            t.position = p;
                            t.rotation = r;
                        }
                    }
                }
            }

            cameraPositionProcessIsActive = false;
            didEnforceCameraPosition = true;
#endif
#endif
		}

		private static void SetupDefaultARPointClouds()
		{
			var pointCloudManager = FindObjectOfType<ARPointCloudManager>(true);
			if (pointCloudManager)
			{
				var pointCloudProviderInScene = FindObjectOfType<SimulatedARPointCloud>(true) ||
				                                FindObjectOfType<SimulatedARPointRaycaster>(true);
				if (!pointCloudProviderInScene)
				{
					var pointCloudGo = new GameObject("Simulated PointCloud");
#if UNITY_EDITOR
					Undo.RegisterCreatedObjectUndo(pointCloudGo, "Created AR PointCloud");
#endif
					var pointCloud = pointCloudGo.AddComponent<SimulatedARPointCloud>();
					if (AllowLogging) Debug.Log("Created AR PointCloud", pointCloud);
					if (TryGetARCamera(out var mainCam))
						pointCloud.transform.position = GetCameraForwardPlacement(mainCam.transform);
					// ReSharper disable once Unity.InefficientPropertyAccess
					pointCloud.transform.localScale = Vector3.one;
					pointCloud.randomPointsShape = SimulatedARPointCloud.PointCloudShape.Planar;
					pointCloud.randomPointsCount = 100;
					TryFindScenePlacementFromRaycast(pointCloud.transform);
					pointCloud.GenerateRandomPoints();
				}
			}
		}


		private static void SetupDefaultARPlanes()
		{
			var arPlaneManager = FindObjectOfType<ARPlaneManager>(true);
			if (arPlaneManager)
			{
				var arPlaneComponentFoundInScene =
					FindObjectOfType<SimulatedARPlane>(true) ||
					FindObjectOfType<SimulatedARPlaneGeneration>(true);
				if (!arPlaneComponentFoundInScene)
				{
					var planeGo = new GameObject("Simulated Plane");
#if UNITY_EDITOR
					Undo.RegisterCreatedObjectUndo(planeGo, "Created AR Plane");
#endif
					var plane = planeGo.AddComponent<SimulatedARPlane>();
					SimulatedARPlanesRegistry.Register(plane);
					if (AllowLogging) Debug.Log("Created AR Plane", plane);
					if (TryGetARCamera(out var mainCam))
					{
						plane.transform.position = GetCameraForwardPlacement(mainCam.transform);
						plane.allowRuntimeUpdate = true;
						plane.RequestPlaneUpdate();
					}

					TryFindScenePlacementFromRaycast(plane.transform);
				}
			}
		}

		/// <summary>
		/// necessary for AR Simulation physics raycast to work (and e.g. occlusion sample scene has no collider on prefab)
		/// </summary>
		private static void EnforceColliderOnTrackablePlanes()
		{
			void AddColliderToCreatedPlane(ARPlanesChangedEventArgs obj)
			{
				foreach (var plane in obj.added)
				{
					plane.gameObject.AddComponent<MeshCollider>();
				}
			}

			var arPlaneManager = FindObjectOfType<ARPlaneManager>(true);
			if (arPlaneManager && arPlaneManager.planePrefab && !arPlaneManager.planePrefab.GetComponent<Collider>())
			{
				arPlaneManager.planesChanged += AddColliderToCreatedPlane;
				if (AllowLogging) Debug.Log("Enforcing Collider on AR Planes", arPlaneManager.planePrefab);
			}
		}

		private static void SetupDefaultAnchors()
		{
			var anchorManager = FindObjectOfType<ARAnchorManager>(true);
			if (anchorManager)
			{
				TryGetARCamera(out var cam);
				if (cam)
				{
					Vector3 pos;
					var rot = Quaternion.identity;
					if (TryFindScenePlacementFromRaycast(out var hit))
					{
						pos = hit.point;
						rot = Quaternion.LookRotation(Vector3.forward, hit.normal);
					}
					else pos = GetCameraForwardPlacement(cam.transform);

					var pose = new Pose(pos, rot);
#if UNITY_AR_FOUNDATION_4_1_0_PREVIEW_OR_NEWER
					var go = new GameObject("AR Anchor");
					go.transform.position = pose.position;
					go.transform.rotation = pose.rotation;
					go.AddComponent<ARAnchor>();
#else
                    anchorManager.AddAnchor(pose);
#endif
					if (AllowLogging) Debug.Log("Created AR Anchor at " + pose, anchorManager);
				}
			}
		}

		private static void SetupDefaultTrackedImage()
		{
			var imgManager = FindObjectOfType<ARTrackedImageManager>(true);
			if (imgManager && imgManager.referenceLibrary != null && imgManager.referenceLibrary.count > 0)
			{
				if (!FindObjectOfType<SimulatedARTrackedImage>(true))
				{
					var go = new GameObject("Simulated Image");
#if UNITY_EDITOR
					Undo.RegisterCreatedObjectUndo(go, "Created AR Tracked Image");
#endif
					var img = go.AddComponent<SimulatedARTrackedImage>();
					var referenceImage = imgManager.referenceLibrary[0];
					img.Image = referenceImage.texture;
					go.transform.localScale = new Vector3(referenceImage.size.x, 1, referenceImage.size.y);
					if (AllowLogging) Debug.Log("Created AR Tracked Image " + img.Image.name, img);
					if (!TryFindScenePlacementFromRaycast(go.transform))
					{
						TryGetARCamera(out var cam);
						go.transform.position = GetCameraForwardPlacement(cam ? cam.transform : go.transform);
					}

					// if (TryGetARCamera(out var cam))
					// {
					//     go.transform.LookAt(cam.transform.position, cam.transform.up);
					//     go.transform.localRotation *= Quaternion.Euler(90, 0, 0);
					// }
				}
			}
		}

		private static void SetupDefaultTrackedObject()
		{
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
			var objManager = FindObjectOfType<ARTrackedObjectManager>(true);
			if (objManager && objManager.referenceLibrary != null && objManager.referenceLibrary.count > 0)
			{
				if (!FindObjectOfType<SimulatedARTrackedObject>(true))
				{
					var go = new GameObject("Simulated AR Tracked Object");
#if UNITY_EDITOR
					Undo.RegisterCreatedObjectUndo(go, "Created AR Tracked Object");
#endif
					var simulated = go.AddComponent<SimulatedARTrackedObject>();
					var entry = objManager.referenceLibrary[0];
					simulated.Entry = entry.guid;
					if (AllowLogging) Debug.Log("Created AR Tracked Object " + entry.name, simulated);
					if (!TryFindScenePlacementFromRaycast(go.transform))
						go.transform.position = GetCameraForwardPlacement(go.transform);
				}
			}
#endif
		}

		private static void SetupDefaultMeshing()
		{
			var meshManager = FindObjectOfType<ARMeshManager>(true);
			if (!meshManager) return;

			var objectWithColliderInScene = FindObjectOfType<Collider>(true);
			if (objectWithColliderInScene)
			{
				if (!FindObjectOfType<SimulatedARMeshing>(true))
				{
					var go = new GameObject("Simulated AR Meshing");
#if UNITY_EDITOR
					Undo.RegisterCreatedObjectUndo(go, "Created " + nameof(SimulatedARMeshing));
#endif
					var sim = go.AddComponent<SimulatedARMeshing>();
				}
			}
			else if (!FindObjectOfType<SimulatedSimpleMeshProvider>(true))
			{
				var go = new GameObject("Simulated AR Mesh Provider");
#if UNITY_EDITOR
				Undo.RegisterCreatedObjectUndo(go, "Created Simple AR Mesh Provider");
#endif
				var simulated = go.AddComponent<SimulatedSimpleMeshProvider>();
				simulated.Mesh = SimulatedSimpleMeshProvider.GetPrimitiveMesh(PrimitiveType.Cylinder);
				simulated.Dynamic = true;
				if (AllowLogging) Debug.Log("Created Simple Simulated Mesh Provider " + simulated.Mesh.name, simulated);
				TryGetARCamera(out var cam);
				if (!TryFindScenePlacementFromRaycast(go.transform))
					go.transform.position = GetCameraForwardPlacement(cam.transform) - DefaultCameraForwardOffset;
				var dist = Vector3.Distance(simulated.transform.position, cam.transform.position);
				simulated.transform.localScale = Vector3.one * (dist / 3f);
				simulated.transform.localRotation *= Quaternion.Euler(0, 45, 0);
			}
		}

		private static readonly Vector3 DefaultCameraForwardOffset = new Vector3(0, -.3f, 0);

		/// <summary>
		/// use to get a default position for AR elements in view of the camera
		/// </summary>
		private static Vector3 GetCameraForwardPlacement(Transform transform)
		{
			return transform.position + transform.forward + DefaultCameraForwardOffset;
		}

		private static bool TryFindScenePlacementFromRaycast(Transform t)
		{
			if (TryFindScenePlacementFromRaycast(out var hit))
			{
				if (hit.distance > .2)
				{
					t.position = hit.point + hit.normal * 0.001f;
					t.localScale = Vector3.one * hit.distance;
					return true;
				}
			}

			return false;
		}

		private static bool TryFindScenePlacementFromRaycast(out RaycastHit hit)
		{
			if (TryGetARCamera(out var cam))
			{
				if (Physics.Raycast(cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 3f)), out hit, 5))
				{
					return true;
				}
			}

			hit = new RaycastHit();
			return false;
		}

		private static void TryRemoveInvalidComponentsFromCamera()
		{
#if UNITY_URP
            // urp template uses a simple camera controller component
            // which interferes with ar input providers
            // we cant get it by type because it's only part of the template project
            if (TryGetARCamera(out var arCamera))
            {
                foreach (var comp in arCamera.GetComponents<MonoBehaviour>())
                {
                    if (comp.GetType().FullName == "UnityTemplateProjects.SimpleCameraController")
                    {
                        Debug.Log("Removing URP Template SimpleCameraController for AR Simulation (undoable) on " + comp.name, comp.gameObject);
                        if (Application.isPlaying)
                            Object.Destroy(comp);
#if UNITY_EDITOR
                        else
                            Undo.DestroyObjectImmediate(comp);
#endif
                    }
                }
            }
#endif
		}


#if UNITY_2019_4_OR_NEWER && !UNITY_2020_1_OR_NEWER
		private static T FindObjectOfType<T>(bool includeInActive) where T : Object
		{
			var found = Resources.FindObjectsOfTypeAll<T>();
			if (found.Length > 0)
			{
				// ReSharper disable once UnusedParameter.Local
				bool IsInCurrentScene(GameObject go)
				{
#if UNITY_EDITOR
					if (!go) return false;
					var activeScene = SceneManager.GetActiveScene();
					if (go.scene.IsValid() && go.scene == activeScene)
						return true;
#endif
					return false;
				}
				
				foreach (var f in found)
				{
					if (!f) continue;
					if (f is Behaviour b)
					{
						if (!includeInActive && !b.isActiveAndEnabled) continue;
						if (!IsInCurrentScene(b.gameObject))
							continue;
					}
					else if (f is Component comp)
					{
						if (!includeInActive && (!comp.gameObject || !comp.gameObject.activeInHierarchy)) continue;
						if (!IsInCurrentScene(comp.gameObject))
							continue;
					}
					else if (f is GameObject go)
					{
						if (!includeInActive && !go.activeInHierarchy) continue;
						if (!IsInCurrentScene(go))
							continue;
					}

					return f;
				}

				return null;
			}

			return null;
		}
#else
		private static T FindObjectOfType<T>(bool includeInActive) where T : Object
		{
			return Object.FindObjectOfType<T>(includeInActive);
		}
#endif
	}
}