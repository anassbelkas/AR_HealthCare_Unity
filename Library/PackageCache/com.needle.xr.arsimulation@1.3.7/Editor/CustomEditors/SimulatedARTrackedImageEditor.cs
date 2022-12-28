using System;
using System.Collections.Generic;
using Needle.XR.ARSimulation.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Needle.XR.ARSimulation.CustomEditors
{
    [CustomEditor(typeof(SimulatedARTrackedImage), true)]
    public class SimulatedARTrackedImageEditor : UnityEditor.Editor
    {
        private static readonly List<Texture2D> availableImages = new List<Texture2D>();
        private static string[] popupOptions;
        private static ARTrackedImageManager imageManager;

        private int selectedIndex = 0;
        
        private void OnEnable()
        {
            UpdateAvailableImages();
        }

        private void UpdateAvailableImages()
        {
            availableImages.Clear();
            
            if (!imageManager)
                imageManager = FindObjectOfType<ARTrackedImageManager>();
            if (imageManager && imageManager.referenceLibrary != null && imageManager.referenceLibrary.count > 0)
            {
                var opts = new List<string>();
                for (var i = 0; i < imageManager.referenceLibrary.count; i++)
                {
                    var refImg = imageManager.referenceLibrary[i];
                    var img = refImg.textureGuid;
                    if (img.Equals(Guid.Empty)) img = refImg.guid;
                    var assetPath = AssetDatabase.GUIDToAssetPath(img.ToString().Replace("-", ""));
                    var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    if (!asset) asset = refImg.texture;
                    if (asset == null || availableImages.Contains(asset)) continue;
                    availableImages.Add(asset);
                    opts.Add(refImg.name);
                }

                popupOptions = opts.ToArray();
            }
            else if(popupOptions == null || popupOptions.Length > 0)
                popupOptions = new string[0];

            TryFindCurrent();
        }

        private void TryFindCurrent()
        {
            if (availableImages == null || availableImages.Count <= 0) return;

            var prop = this.serializedObject.FindProperty(nameof(SimulatedARTrackedImage.Image));
            if (prop == null) return;
            var img = prop.objectReferenceValue as Texture2D;
            if (img)
            {
                selectedIndex = availableImages.IndexOf(img);
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateAvailableImages();

            var obj = this.serializedObject;
            EditorGUI.BeginChangeCheck();
            obj.UpdateIfRequiredOrScript();
            var iterator = obj.GetIterator();
            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath))
                {
                    if (iterator.propertyPath == nameof(SimulatedARTrackedImage.Image))
                    {
                        EditorGUI.BeginChangeCheck();
                        var noImagesAvailable = popupOptions == null || popupOptions.Length <= 0;
                        using (new EditorGUI.DisabledScope(noImagesAvailable))
                        {
                            var index = EditorGUILayout.Popup(nameof(SimulatedARTrackedImage.Image), selectedIndex, popupOptions);
                            if (index >= 0 && index < availableImages.Count)
                            {
                                if (EditorGUI.EndChangeCheck())
                                {
                                    var img = availableImages[index];
                                    selectedIndex = index;
                                    iterator.objectReferenceValue = img;
                                    obj.ApplyModifiedProperties();
                                }
                            }
                        }

                        if (noImagesAvailable)
                        {
                            EditorGUILayout.HelpBox("No Referenced Images available. Please add images to your ReferenceImageLibrary asset and assign the library to your ARImageTrackingManager", MessageType.Warning);
                        }
                        else  if (selectedIndex < 0)
                        {
                            EditorGUILayout.HelpBox("The currently referenced image is not available in any ReferenceImageLibrary: " + iterator.objectReferenceValue, MessageType.Warning);
                        }

                    }
                    else
                        EditorGUILayout.PropertyField(iterator, true);
                }
            }

            obj.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }
    }
}