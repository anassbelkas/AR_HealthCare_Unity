using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Needle.XR.ARSimulation
{
    public static class ARSimulationSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            GUIContent s_WarningToCreateSettings = EditorGUIUtility.TrTextContent(
                "This controls the Build Settings for ARSimulation.\n\nYou must create a serialized instance of the settings data in order to modify the settings in this UI. Until then only default settings set by the provider will be available.");

            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/XR/ARSimulation", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "ARSimulation Build Settings",

                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    if (ARSimulationSettings.currentSettings == null)
                    {
                        EditorGUILayout.HelpBox(s_WarningToCreateSettings);
                        if (GUILayout.Button(EditorGUIUtility.TrTextContent("Create")))
                        {
                            Create();
                        }
                        else
                        {
                            return;
                        }
                    }

                    var serializedSettings = ARSimulationSettings.GetSerializedSettings();

                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("m_Requirement"), new GUIContent(
                        "Requirement",
                        "Toggles whether ARSimulation is required for this app. This will make the app only downloadable by devices with ARSimulation support if set to 'Required'."));

                    serializedSettings.ApplyModifiedProperties();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "ARSimulation", "optional", "required" })
            };

            return provider;
        }

        private static void Create()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save ARSimulation Settings", "ARSimulationSettings", "asset", "Please enter a filename to save the ARSimulation settings.");
            if (string.IsNullOrEmpty(path))
                return;

            var settings = ScriptableObject.CreateInstance<ARSimulationSettings>();
            AssetDatabase.CreateAsset(settings, path);
            ARSimulationSettings.currentSettings = settings;
        }
    }
}
