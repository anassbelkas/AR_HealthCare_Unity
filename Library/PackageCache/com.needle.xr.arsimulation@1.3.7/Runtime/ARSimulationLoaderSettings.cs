using System;
using System.Collections.Generic;
using Needle.XR.ARSimulation.Simulation;
using UnityEngine;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// Settings to control the ARDesktopLoader behavior.
    /// </summary>
    [XRConfigurationData("AR Simulation", ARSimulationLoaderConstants.k_SettingsKey)]
    [Serializable]
    public class ARSimulationLoaderSettings : ScriptableObject
    {
        private static ARSimulationLoaderSettings instance;
        internal static ARSimulationLoaderSettings Instance
        {
            get
            {
                if (instance) return instance;
                instance = Object.FindObjectOfType<ARSimulationLoaderSettings>();
                if (instance) return instance;
#if UNITY_EDITOR
                var assets = AssetDatabase.FindAssets("t:" + typeof(ARSimulationLoaderSettings));
                foreach (var asset in assets)
                {
                    var loaded = AssetDatabase.LoadAssetAtPath<ARSimulationLoaderSettings>(AssetDatabase.GUIDToAssetPath(asset));
                    if (!loaded) continue;
                    instance = loaded;
                    return instance;
                }
#endif
                var settings = Resources.FindObjectsOfTypeAll<ARSimulationLoaderSettings>();
                if (settings != null && settings.Length > 0)
                {
                    instance = settings[settings.Length-1];
                    return instance;
                }
                return instance;
            }
        }

        [SerializeField, HideInInspector]
        private bool m_firstInstallation = true;
        internal bool firstInstallation
        {
            get => m_firstInstallation; 
            set => m_firstInstallation = value;
        }

        [SerializeField, Tooltip("Allow " + nameof(ARSimulationLoader) + " to start and stop subsystems.")]
        private bool m_StartAndStopSubsystems = true;

        public bool startAndStopSubsystems
        {
            get => m_StartAndStopSubsystems;
            set => m_StartAndStopSubsystems = value;
        }   

        [SerializeField, Tooltip("Allow AR Simulation to automatically enable itself as XR plugin")]
        private bool m_addXRLoader = true;
        public bool allowAddXRLoader
        {
            get => m_addXRLoader;
            set => m_addXRLoader = value;
        }  

        [Header("Automatic Scene Setup")]
        
        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically setup scene with camera controller components when entering playmode")]
        private bool m_autoSceneSetup = true;

        public bool allowAutoSceneSetup
        {
            get => m_autoSceneSetup;
            set => m_autoSceneSetup = value;
        }

        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically spawn an simulated AR plane if you have a ARPlaneManager component in your scene")]
        private bool m_autoPlane = true;

        public bool allowAutoPlaneSpawn
        {
            get => m_autoPlane;
            set
            {
                Debug.Log("SET AUTO PLANE VALUE", this);
                m_autoPlane = value;
            }
        }

        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically spawn an simulated PointCloud if you have a ARPointCloudManager component in your scene")]
        private bool m_allowAutoPointCloud = true;

        public bool allowAutoPointCloudSpawn
        {
            get => m_allowAutoPointCloud;
            set => m_allowAutoPointCloud = value;
        }

        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically spawn an Anchor if you have an ARAnchorManager component in your scene")]
        private bool m_autoAnchor = true;

        public bool allowAutoAnchorSpawn
        {
            get => m_autoAnchor;
            set => m_autoAnchor = value;
        }

        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically spawn an simulated TrackedImage if you have an ARTrackedImageManager component in your scene")]
        private bool m_autoTrackedImage = true;

        public bool allowAutoTrackedImageSpawn
        {
            get => m_autoTrackedImage;
            set => m_autoTrackedImage = value;
        }

        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically spawn an simulated TrackedObject if you have an ARTrackedObjectManager component in your scene")]
        private bool m_autoTrackedObject = true;

        public bool allowAutoTrackedObjectSpawn
        {
            get => m_autoTrackedObject;
            set => m_autoTrackedObject = value;
        }
        
        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to automatically spawn an simulated MeshProvider if you have an ARMeshManager component in your scene")]
        private bool m_autoMeshing = true;
        public bool allowAutoMeshingSpawn
        {
            get => m_autoMeshing;
            set => m_autoMeshing = value;
        }

        [Header("Editor UX"), Tooltip("Mark EditorOnly GameObjects in Hierarchy with Circle Icon (left side)")] [SerializeField] private bool m_allowMarkEditorOnlyGameObjectsInHierarchy = true;

        public bool allowMarkEditorOnlyGameObjectsInHierarchy
        {
            get => m_allowMarkEditorOnlyGameObjectsInHierarchy;
            set => m_allowMarkEditorOnlyGameObjectsInHierarchy = value;
        }
        
        [System.Serializable]
        public class DeviceInputSettings
        {
            [Tooltip("If enabled: we will stop providing camera controls if the Camera subsystem is disabled, otherwise we will continue to handle input")]
            public bool DeactivateWithSubsystem = true;
        
            [Tooltip("If enabled: InputHelper will check if the current EventSystem has any GameObject selected and if not it will not process Keyboard input.")]
            public bool DisableInputWithSelectedUI = true;
            
            [Header("Movement")]
            public MovementMode Mode = MovementMode.Walk;
            public enum MovementMode
            {
                Fly = 0,
                Walk = 1
            }

            public MovementKeys Keys = MovementKeys.WASD;
            [Flags]
            public enum MovementKeys
            {
                WASD = 1 << 0,
                ArrowKeys = 1 << 1,
            }
            
            public float MovementSensitivity = 1.5f;
            public float RotationSensitivity = 2.5f;

            public MouseMovement Mouse = MouseMovement.RequireRightMousePressedToMove;
            [Flags]
            public enum MouseMovement
            {
                RequireRightMousePressedToMove = 1 << 0
            }

            [Header("Focus")]
            public KeyCode FocusKey = KeyCode.F;
            public FocusMode FocusAt = FocusMode.MousePosition;
            public float DefaultFocusDistance = 1.5f;
            public enum FocusMode
            {
                ScreenCenter = 0,
                MousePosition = 1,
            }
        }

        [Header("Runtime")] [SerializeField] 
#pragma warning disable 0649
        private DeviceInputSettings m_inputSettings = new DeviceInputSettings();
#pragma warning restore 0649
        
        public DeviceInputSettings InputSettings => m_inputSettings ?? (m_inputSettings = new DeviceInputSettings());


        [Header("Debug Logs")]
        [SerializeField, Tooltip("Allow " + nameof(SceneSetup) + " to Debug.Log during auto setup phase")]
        private bool m_allowAutoSetupLogging;

        public bool allowAutoSetupLogging
        {
            get => m_allowAutoSetupLogging;
            set => m_allowAutoSetupLogging = value;
        }


        [SerializeField, HideInInspector] internal bool foldoutEnvironmentManagerSettings = true;
        [SerializeField] private List<string> prefabGuidsWithSimulatedEnvironmentComponent = new List<string>();

        public static List<string> PrefabGuidsWithSimulatedEnvironmentComponent
        {
            get => Instance ? Instance.prefabGuidsWithSimulatedEnvironmentComponent : null;
            set
            {
                if (Instance) Instance.prefabGuidsWithSimulatedEnvironmentComponent = value;
            }
        }


        [Serializable]
        private class SimulatedEnvironmentSettings
        {
            public GameObject Environment;
            public Vector3 defaultCameraLocalPosition;
            public Vector3 defaultCameraLocalRotation;
        }

        [SerializeField] private List<SimulatedEnvironmentSettings> simulatedEnvironmentSettings;


        public static void SetDefaultPose(GameObject env, Vector3 position, Vector3 rotation)
        {
            if (Instance)
            {
                foreach (var setting in Instance.simulatedEnvironmentSettings)
                {
                    if(setting.Environment == env)
                    {
#if UNITY_EDITOR
                        Undo.RecordObject(Instance, nameof(SetDefaultPose));
#endif
                        setting.defaultCameraLocalPosition = position;
                        setting.defaultCameraLocalRotation = rotation;
#if UNITY_EDITOR
                        EditorUtility.SetDirty(Instance);
#endif
                        return;
                    }
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(Instance, nameof(SetDefaultPose));
#endif
                Instance.simulatedEnvironmentSettings.Add(new SimulatedEnvironmentSettings()
                {
                    Environment = env,
                    defaultCameraLocalPosition = position,
                    defaultCameraLocalRotation = rotation
                });
#if UNITY_EDITOR
                EditorUtility.SetDirty(Instance);
#endif
            }
        }

        public static bool TryGetDefaultPose(GameObject env, out Vector3 position, out Vector3 rotation)
        {
            if (Instance)
            {
                foreach (var setting in Instance.simulatedEnvironmentSettings)
                {
                    if(setting.Environment == env)
                    {
                        position = setting.defaultCameraLocalPosition;
                        rotation = setting.defaultCameraLocalRotation;
                        return true;
                    }
                }
            }
            
            position = Vector3.zero;
            rotation = Vector3.zero;
            return false;
        }
    }
}