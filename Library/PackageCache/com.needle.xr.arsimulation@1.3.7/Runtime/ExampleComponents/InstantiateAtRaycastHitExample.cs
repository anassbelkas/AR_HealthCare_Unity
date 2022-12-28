using System;
using System.Collections.Generic;
using Needle.XR.ARSimulation.Compatibility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;

namespace Needle.XR.ARSimulation.ExampleComponents
{
    /// <summary>
    /// Tidy component used by examples/samples to instantiate a prefab at <see cref="ARRaycastHit"/> position and rotation
    /// </summary>
    public class InstantiateAtRaycastHitExample : MonoBehaviour
    {
        public ARInput.InputType Type = ARInput.InputType.Any;

        /// <summary>
        /// The prefab to instantiate
        /// </summary>
        [FormerlySerializedAs("m_prefab")] [SerializeField]
        public List<GameObject> Prefabs;

        [SerializeField, HideInInspector, FormerlySerializedAs("Prefab")]
        private GameObject old_prefab;

        public bool randomRotation = false;
        public Vector2 randomScale = Vector2.one;

        private readonly List<GameObject> m_spawnedInstances = new List<GameObject>();
        private float lastHitTime;

        private void OnValidate()
        {
            if (!old_prefab || Prefabs.Contains(old_prefab)) return;
            Prefabs.Add(old_prefab);
            old_prefab = null;
        }

        private void OnDisable()
        {
            DestroyCreatedInstances();
        }

        [ContextMenu(nameof(DestroyCreatedInstances))]
        private void DestroyCreatedInstances()
        {
            foreach (var instance in m_spawnedInstances)
            {
                if (instance)
                    Destroy(instance);
            }

            m_spawnedInstances.Clear();
        }

        private void Update()
        {
            if (!Ensure.CorrectInputSystemConfiguration())
            {
                Debug.Log("Disabling component, due to invalid input configuration.", this);
                enabled = false;
                return;
            }

            if (EventSystem.current)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;
            }
            
            if (ARInput.TryGetHit(Type, out var hit))
            {
                if (Prefabs == null || Prefabs.Count <= 0)
                {
                    Debug.Log("Missing prefab assignment", this);
                    return;
                }

                if (Time.time - lastHitTime < 0.1f) return;
                lastHitTime = Time.time;
                var hitPose = hit.pose;
                // Debug.DrawLine(hit.pose.position, hit.pose.position + hit.pose.up, Color.yellow, 2);
                var instance = Instantiate(
                    Prefabs[Mathf.FloorToInt(Random.value * Prefabs.Count)], 
                    hitPose.position, 
                    hitPose.rotation);
                
                // randomize rotation
                if(randomRotation)
                    instance.transform.rotation *= Quaternion.Euler(0, Random.Range(-180f, 180f), 0);
                
                // randomize scale
                var scale = instance.transform.localScale;
                scale *= Random.Range(randomScale.x, randomScale.y);
                instance.transform.localScale = scale;

                m_spawnedInstances.Add(instance);
                instance.SetActive(true);
            }
        }
    }
}