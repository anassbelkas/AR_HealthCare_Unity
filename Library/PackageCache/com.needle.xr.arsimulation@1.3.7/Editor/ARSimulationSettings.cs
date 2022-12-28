using UnityEditor;
using UnityEngine;

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// Holds settings that are used to configure the Unity ARSimulation Plugin.
    /// </summary>
    public class ARSimulationSettings : ScriptableObject
    {
        /// <summary>
        /// Enum which defines whether ARSimulation is optional or required.
        /// </summary>
        public enum Requirement
        {
            /// <summary>
            /// ARSimulation is required, which means the app cannot be installed on devices that do not support ARSimulation.
            /// </summary>
            Required,

            /// <summary>
            /// ARSimulation is optional, which means the the app can be installed on devices that do not support ARSimulation.
            /// </summary>
            Optional
        }

        [SerializeField, Tooltip("Toggles whether ARSimulation is required for this app. Will make app only downloadable by devices with ARSimulation support if set to 'Required'.")]
        Requirement m_Requirement;

        /// <summary>
        /// Determines whether ARSimulation is required for this app: will make app only downloadable by devices with ARSimulation support if set to <see cref="Requirement.Required"/>.
        /// </summary>
        public Requirement requirement
        {
            get { return m_Requirement; }
            set { m_Requirement = value; }
        }

        /// <summary>
        /// Gets the currently selected settings, or create a default one if no <see cref="ARSimulationSettings"/> has been set in Player Settings.
        /// </summary>
        /// <returns>The ARSimulation settings to use for the current Player build.</returns>
        public static ARSimulationSettings GetOrCreateSettings()
        {
            var settings = currentSettings;
            if (settings != null)
                return settings;

            return CreateInstance<ARSimulationSettings>();
        }

        /// <summary>
        /// Get or set the <see cref="ARSimulationSettings"/> that will be used for the player build.
        /// </summary>
        public static ARSimulationSettings currentSettings
        {
            get
            {
                ARSimulationSettings settings = null;
                EditorBuildSettings.TryGetConfigObject(k_ConfigObjectName, out settings);
                return settings;
            }

            set
            {
                if (value == null)
                {
                    EditorBuildSettings.RemoveConfigObject(k_ConfigObjectName);
                }
                else
                {
                    EditorBuildSettings.AddConfigObject(k_ConfigObjectName, value, true);
                }
            }
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        internal static bool TrySelect()
        {
            var settings = currentSettings;
            if (settings == null)
                return false;

            Selection.activeObject = settings;
            return true;
        }

        static readonly string k_ConfigObjectName = "com.needle.xr.arsimulation.PlayerSettings";
    }
}
