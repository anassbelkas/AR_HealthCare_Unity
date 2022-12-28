using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

#if UNITY_EDITOR
using UnityEditor;
using System.Threading.Tasks;
#endif

namespace Needle.XR.ARSimulation
{
    public class ARSimulationLoader : XRLoaderHelper
    {
	    /// <summary>
	    /// workaround for XRAnchor exit playmode exception thrown by AnchorManager, case 1268386
	    /// </summary>
	    private static bool changingPlayModeFrame;
	    
#pragma warning disable 67
	    public static event Action FirstInstall, RequestLoaderActivation;
#pragma warning restore 67
	    
#if UNITY_EDITOR
	    internal static void RaiseRequestLoaderActivationEvent() => RequestLoaderActivation?.Invoke();
        
        [InitializeOnLoadMethod]
        // ReSharper disable once UnusedMember.Local
        private static async void DetectFirstInstallation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
	            changingPlayModeFrame = true;
                return;
            }
            changingPlayModeFrame = false;
            ARSimulationLoaderSettings settings = null;
            for (var i = 0; i < 120 && !settings; i++)
            {
                settings = GetSettings();
                if (settings) break;
                await Task.Delay(1000);
            }

            if (!settings || settings == null || !settings.firstInstallation) return;
            settings.firstInstallation = false;
            EditorUtility.SetDirty(settings);
            FirstInstall?.Invoke();
        }
#endif

        private static readonly List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new List<XRSessionSubsystemDescriptor>();
        private static readonly List<XRCameraSubsystemDescriptor> s_CameraSubsystemDescriptors = new List<XRCameraSubsystemDescriptor>();
        private static readonly List<XRDepthSubsystemDescriptor> s_DepthSubsystemDescriptors = new List<XRDepthSubsystemDescriptor>();
        private static readonly List<XRPlaneSubsystemDescriptor> s_PlaneSubsystemDescriptors = new List<XRPlaneSubsystemDescriptor>();
        private static readonly List<XRAnchorSubsystemDescriptor> s_AnchorSubsystemDescriptors = new List<XRAnchorSubsystemDescriptor>();
        private static readonly List<XRRaycastSubsystemDescriptor> s_RaycastSubsystemDescriptors = new List<XRRaycastSubsystemDescriptor>();
        private static readonly List<XRImageTrackingSubsystemDescriptor> s_ImageTrackingSubsystemDescriptors = new List<XRImageTrackingSubsystemDescriptor>();
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        private static readonly List<XRObjectTrackingSubsystemDescriptor> s_ObjectTrackingSubsystemDescriptors = new List<XRObjectTrackingSubsystemDescriptor>();
#endif
        private static readonly List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors = new List<XRInputSubsystemDescriptor>();
        private static readonly List<XRFaceSubsystemDescriptor> s_FaceSubsystemDescriptors = new List<XRFaceSubsystemDescriptor>();
        private static readonly List<XRMeshSubsystemDescriptor> s_MeshSubsystemDescriptors = new List<XRMeshSubsystemDescriptor>();

        public XRSessionSubsystem sessionSubsystem => GetLoadedSubsystem<XRSessionSubsystem>();
        public XRCameraSubsystem cameraSubsystem => GetLoadedSubsystem<XRCameraSubsystem>();
        public XRDepthSubsystem depthSubsystem => GetLoadedSubsystem<XRDepthSubsystem>();
        public XRPlaneSubsystem planeSubsystem => GetLoadedSubsystem<XRPlaneSubsystem>();
        public XRAnchorSubsystem anchorSubsystem => GetLoadedSubsystem<XRAnchorSubsystem>();
        public XRRaycastSubsystem raycastSubsystem => GetLoadedSubsystem<XRRaycastSubsystem>();
        public XRImageTrackingSubsystem imageTrackingSubsystem => GetLoadedSubsystem<XRImageTrackingSubsystem>();
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        public XRObjectTrackingSubsystem objectTrackingSubsystem => GetLoadedSubsystem<XRObjectTrackingSubsystem>();
#endif
        public XRInputSubsystem inputSubsystem => GetLoadedSubsystem<XRInputSubsystem>();
        public XRFaceSubsystem faceSubsystem => GetLoadedSubsystem<XRFaceSubsystem>();
        public XRMeshSubsystem meshSubsystem => GetLoadedSubsystem<XRMeshSubsystem>();


        // public override T GetLoadedSubsystem<T>()
        // {
        //     if (typeof(T) == typeof(XRMeshSubsystem))
        //         return new ARSimulationMeshingSubsystem() as T;
        //     return base.GetLoadedSubsystem<T>();
        // }
        
        public static bool Initialized { get; private set; }

        public override bool Initialize()
        {
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, "ARSimulation-Session");
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(s_CameraSubsystemDescriptors, "ARSimulation-Camera");
            CreateSubsystem<XRDepthSubsystemDescriptor, XRDepthSubsystem>(s_DepthSubsystemDescriptors, "ARSimulation-Depth");
            CreateSubsystem<XRPlaneSubsystemDescriptor, XRPlaneSubsystem>(s_PlaneSubsystemDescriptors, "ARSimulation-Plane");
            CreateSubsystem<XRAnchorSubsystemDescriptor, XRAnchorSubsystem>(s_AnchorSubsystemDescriptors, "ARSimulation-Anchor");
            CreateSubsystem<XRRaycastSubsystemDescriptor, XRRaycastSubsystem>(s_RaycastSubsystemDescriptors, "ARSimulation-Raycast");
            CreateSubsystem<XRImageTrackingSubsystemDescriptor, XRImageTrackingSubsystem>(s_ImageTrackingSubsystemDescriptors, "ARSimulation-ImageTracking");
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            CreateSubsystem<XRObjectTrackingSubsystemDescriptor, XRObjectTrackingSubsystem>(s_ObjectTrackingSubsystemDescriptors, "ARSimulation-ObjectTracking");
#endif
            CreateSubsystem<XRFaceSubsystemDescriptor, XRFaceSubsystem>(s_FaceSubsystemDescriptors, "ARSimulation-Face");
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, "arsimulation");
            CreateSubsystem<XRMeshSubsystemDescriptor, XRMeshSubsystem>(s_MeshSubsystemDescriptors, "arsimulation-meshing");

            if (sessionSubsystem == null)
            {
                Debug.LogError("Failed to load session subsystem.");
            }

            Initialized = sessionSubsystem != null;
            return Initialized;
#else
            return false;
#endif
        }

        public override bool Start()
        {
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            var settings = GetSettings();
            if (settings != null && settings.startAndStopSubsystems)
            {
                StartSubsystem<XRSessionSubsystem>();
                StartSubsystem<XRCameraSubsystem>();
                StartSubsystem<XRDepthSubsystem>();
                StartSubsystem<XRPlaneSubsystem>();
                StartSubsystem<XRAnchorSubsystem>();
                StartSubsystem<XRRaycastSubsystem>();
                StartSubsystem<XRImageTrackingSubsystem>();
                StartSubsystem<XRInputSubsystem>();
                StartSubsystem<XRFaceSubsystem>();
                StartSubsystem<XRMeshSubsystem>();
                
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                ARSimulationObjectTrackingSubsystem.EnsureLibrary(objectTrackingSubsystem);
                StartSubsystem<XRObjectTrackingSubsystem>();
#endif
            }

            return true;
#else
            return false;
#endif
        }

        public override bool Stop()
        {
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            var settings = GetSettings();
            if (settings != null && settings.startAndStopSubsystems)
            {
                StopSubsystem<XRSessionSubsystem>();
                StopSubsystem<XRCameraSubsystem>();
                StopSubsystem<XRDepthSubsystem>();
                StopSubsystem<XRPlaneSubsystem>();
                if(!changingPlayModeFrame)
					StopSubsystem<XRAnchorSubsystem>();
                StopSubsystem<XRRaycastSubsystem>();
                StopSubsystem<XRImageTrackingSubsystem>();
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                StopSubsystem<XRObjectTrackingSubsystem>();
#endif
                StopSubsystem<XRInputSubsystem>();
                StopSubsystem<XRFaceSubsystem>();
                StopSubsystem<XRMeshSubsystem>();
            }

            changingPlayModeFrame = false;
            return true;
#else
            return false;
#endif
        }

        public override bool Deinitialize()
        {            
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            DestroySubsystem<XRSessionSubsystem>();
            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XRDepthSubsystem>();
            DestroySubsystem<XRPlaneSubsystem>();
            // DestroySubsystem<XRAnchorSubsystem>();
            DestroySubsystem<XRRaycastSubsystem>();
            DestroySubsystem<XRImageTrackingSubsystem>();
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            DestroySubsystem<XRObjectTrackingSubsystem>();
#endif
            DestroySubsystem<XRInputSubsystem>();
            DestroySubsystem<XRFaceSubsystem>();
            DestroySubsystem<XRMeshSubsystem>();
            return true;
#else
            return false;
#endif
        }

        public static ARSimulationLoaderSettings GetSettings()
        {
            // When running in the Unity Editor, we have to load user's customization of configuration data directly from
            // EditorBuildSettings. At runtime, we need to grab it from the static instance field instead.
#if UNITY_EDITOR
            if (!ARSimulationLoaderSettings.Instance && !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorBuildSettings.TryGetConfigObject(ARSimulationLoaderConstants.k_SettingsKey, out ARSimulationLoaderSettings settings);
                return settings;
            }

            return ARSimulationLoaderSettings.Instance;
#else
            return ARSimulationLoaderSettings.Instance;
#endif
        }
    }
}