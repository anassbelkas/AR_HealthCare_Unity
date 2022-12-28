using UnityEditor;
using UnityEngine;

namespace Needle.XR.ARSimulation.HierarchyDrawer
{
    public static class MarkEditorOnly
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawInHierarchy;

            backgroundColor = EditorGUIUtility.isProSkin
                ? new Color32(56, 56, 56, 255)
                : new Color32(194, 194, 194, 255);
            backgroundColor *= GUI.backgroundColor;
        }

        private static GUIStyle utfIconStyle;
        private static Color backgroundColor;

        private static void DrawInHierarchy(int instanceID, Rect selectRect)
        {
            var settings = ARSimulationLoader.GetSettings();
            if (settings && !settings.allowMarkEditorOnlyGameObjectsInHierarchy)
                return;

            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (!gameObject || gameObject == null) return;
            if (gameObject.CompareTag("EditorOnly"))
            {
                if (utfIconStyle == null)
                {
                    utfIconStyle = new GUIStyle(EditorStyles.label);
                    utfIconStyle.alignment = TextAnchor.MiddleLeft;
                    var c = utfIconStyle.normal.textColor;
                    c.a = .5f;
                    utfIconStyle.normal.textColor = c;
                    utfIconStyle.fontSize += 2;
                }

                var rect = selectRect;
                rect.width = 14;
                EditorGUI.DrawRect(rect, backgroundColor);

                rect.x += .5f;
                rect.y -= 1f;
                EditorGUI.LabelField(rect, new GUIContent("○", "This GameObject is not included in builds: tagged with \"Editor Only\""), utfIconStyle);

                // ◆ ❖ ◇ 
                // EditorGUI.LabelField(selectRect, "Not in build", new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                // {
                //     alignment = TextAnchor.MiddleRight
                // });
            }
        }
    }
}