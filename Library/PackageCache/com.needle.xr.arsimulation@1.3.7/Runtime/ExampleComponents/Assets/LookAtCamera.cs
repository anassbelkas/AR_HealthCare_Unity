using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Random = UnityEngine.Random;

namespace Needle.XR.ARSimulation.ExampleComponents
{
    /// <summary>
    /// Simple component used by examples/samples to make instances look towards camera
    /// </summary>
    public class LookAtCamera : MonoBehaviour
    {
        public float Speed = 10;
        
        private ARSessionOrigin m_SessionOrigin;
        private Vector3 lookVector;
        private Vector3 up;

        private void Awake()
        {
            m_SessionOrigin = FindObjectOfType<ARSessionOrigin>();
        }

        private void Start()
        {
            up = transform.up;
            if (m_SessionOrigin)
            {
                lookVector = m_SessionOrigin.camera.transform.position - transform.position;
                lookVector.y = 0;
                var lookAtCamera = Quaternion.LookRotation(lookVector, up);
                transform.rotation = lookAtCamera;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(LookRoutine());
            
        }

        private IEnumerator LookRoutine()
        {
            var wait = new WaitForSeconds(Mathf.Lerp(.2f, 2f, Random.value));
            while (true)
            {
                lookVector = m_SessionOrigin.camera.transform.position - transform.position;
                lookVector.y = 0;
                yield return wait;
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void Update()
        {
            if (!m_SessionOrigin || !m_SessionOrigin.camera) return;
            var lookAtCamera = Quaternion.LookRotation(lookVector, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAtCamera, Time.deltaTime * Speed);
        }
    }
}