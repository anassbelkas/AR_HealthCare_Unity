using System;
using Needle.XR.ARSimulation.Compatibility;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Needle.XR.ARSimulation.ExampleComponents
{
    /// <summary>
    /// <see cref="Component">Component</see> used by examples/samples to make <see cref="Content"/> appear at <see cref="ARRaycastHit"/> using <see cref="ARSessionOrigin"/> "MakeContentAppearAt" method
    /// </summary>
    public class MakeContentAppearExample : MonoBehaviour
    {
        public ARInput.InputType Type = ARInput.InputType.Any;
        
        /// <summary>
        /// The transform that holds the content. Should not be in <see cref="ARSessionOrigin"/> hierarchy
        /// </summary>
        public Transform Content;
        
        /// <summary>
        /// Will also set <see cref="ARRaycastHit"/> rotation if true
        /// </summary>
        public bool UseRotation = true;

        private static ARSessionOrigin origin => new Lazy<ARSessionOrigin>(FindObjectOfType<ARSessionOrigin>).Value;

        private float lastHitTime;
        
        private void Update()
        {
            if (!Ensure.CorrectInputSystemConfiguration())
            {
                Debug.Log("Disabling component, due to invalid input configuration.", this);
                enabled = false;
                return;
            }
            
            if (!ARInput.TryGetHit(Type, out var hit)) return;
            if (Time.time - lastHitTime < 0.1f) return;
            lastHitTime = Time.time;
            if (!Content)
            {
                Debug.Log("Missing content assignment");
                return;
            }
            if (!origin) return;
#if UNITY_AR_FOUNDATION_5_OR_NEWER
            Debug.LogWarning("MakeContentAppearAt API is currently missing from ARFoundation 5.x. Please use 4.x until Unity fixes that.");
#else
            if (UseRotation)
                origin.MakeContentAppearAt(Content, hit.pose.position, hit.pose.rotation);
            else
                origin.MakeContentAppearAt(Content, hit.pose.position);
#endif
        }
    }
}