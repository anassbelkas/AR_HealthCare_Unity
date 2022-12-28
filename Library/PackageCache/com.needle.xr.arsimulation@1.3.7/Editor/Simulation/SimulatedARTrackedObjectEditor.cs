using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Simulation
{
    [CustomEditor(typeof(SimulatedARTrackedObject))]
    public class SimulatedARTrackedObjectEditor : Editor
    {
#if UNITY_ARSUBSYSTEMS_4_OR_NEWER
        private readonly List<XRReferenceObject> availableOptions = new List<XRReferenceObject>();
        private string[] popupOptions = new string[0];
        private static ARTrackedObjectManager objectManager;
        private int selectedIndex;

        private void OnEnable()
        {
            RefreshOptions();
        }
#endif

#pragma warning disable CS0219
        public override void OnInspectorGUI()
        {
            var simObject = target as SimulatedARTrackedObject;

#if !UNITY_ARSUBSYSTEMS_4_OR_NEWER
            EditorGUILayout.HelpBox("Object Tracking is not supported using ARFoundation version prior version 4. Please upgrade ARFoundation.", MessageType.Warning); 
            
            using (new EditorGUI.DisabledScope(true))
#endif
            {
                var obj = this.serializedObject;
                EditorGUI.BeginChangeCheck();
                obj.UpdateIfRequiredOrScript();
                var iterator = obj.GetIterator();
                var drawPopup = false;
                for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
                {
                    var isScript = "m_Script" == iterator.propertyPath;
                    using (new EditorGUI.DisabledScope(isScript))
                    {
#if UNITY_ARSUBSYSTEMS_4_OR_NEWER
                        if (drawPopup)
                        {
                            drawPopup = false;
                            EditorGUI.BeginChangeCheck();
                            var noOptionAvailable = popupOptions == null || popupOptions.Length <= 0;

                            if (noOptionAvailable)
                            {
                                EditorGUILayout.HelpBox(
                                    "No TrackedObjects available. Please add objects to your ReferenceObjectLibrary asset and assign the library to your ARObjectTrackingManager",
                                    MessageType.Warning);
                            }

                            using (new EditorGUI.DisabledScope(noOptionAvailable))
                            {
                                var index = EditorGUILayout.Popup("Object", selectedIndex, popupOptions);
                                if (EditorGUI.EndChangeCheck() && index >= 0 && index < availableOptions.Count)
                                {
                                    selectedIndex = index;
                                    var referenceObject = availableOptions[index];
                                    if (simObject && simObject != null) simObject.Entry = referenceObject.guid;
                                }
                            }

                            if (selectedIndex < 0)
                            {
                                EditorGUILayout.HelpBox(
                                    "The currently referenced object is not available in any ReferenceObjectLibrary: " + iterator.objectReferenceValue,
                                    MessageType.Warning);
                            }
                        }
#endif
                        // ReSharper disable once PossibleNullReferenceException
                        if (simObject && simObject.simulateTracking && iterator.propertyPath == "trackingState")
                            using (new EditorGUI.DisabledScope(true))
                                EditorGUILayout.PropertyField(iterator, true);
                        else 
                            EditorGUILayout.PropertyField(iterator, true);
                    }

                    if (isScript) drawPopup = true;
                }

                obj.ApplyModifiedProperties();
                EditorGUI.EndChangeCheck();
            }
        }
#pragma warning restore CS0219

#if UNITY_ARSUBSYSTEMS_4_OR_NEWER
        private void RefreshOptions()
        {
            availableOptions.Clear();
            popupOptions = null;

            if (!objectManager) objectManager = FindObjectOfType<ARTrackedObjectManager>();
            if (objectManager && objectManager.referenceLibrary)
            {
                var lib = objectManager.referenceLibrary;
                popupOptions = new string[lib.count];
                for (var i = 0; i < lib.count; i++)
                {
                    var entry = lib[i];
                    availableOptions.Add(entry);
                    if (entry.guid == Guid.Empty)
                        popupOptions[i] = "Missing Reference";
                    else
                        popupOptions[i] = string.IsNullOrEmpty(entry.name) ? "Unnamed Object [" + i + "]" : entry.name;
                }
            }


            TryFindCurrent();
        }

        private void TryFindCurrent()
        {
            selectedIndex = 0;
            if (availableOptions != null && availableOptions.Count > 0)
            {
                var propLow = this.serializedObject.FindProperty("guidLow");
                var propHigh = this.serializedObject.FindProperty("guidHigh");
                if (propLow == null || propHigh == null) return;
                var low = propLow.longValue;
                var high = propHigh.longValue;
                var guid = GuidUtil.Compose((ulong) low, (ulong) high);
                for (var i = 0; i < availableOptions.Count; i++)
                {
                    var opt = availableOptions[i];
                    if (guid != opt.guid) continue;
                    selectedIndex = i;
                    if (target is SimulatedARTrackedObject @object) @object.Entry = guid;
                    return;
                }

                // if guid is not found
                if (selectedIndex < availableOptions.Count && target is SimulatedARTrackedObject @obj)
                    @obj.Entry = availableOptions[selectedIndex].guid;
            }
        }
#endif
    }
}