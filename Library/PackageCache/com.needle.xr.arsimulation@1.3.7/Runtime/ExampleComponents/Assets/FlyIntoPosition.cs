using UnityEngine;

// ReSharper disable Unity.InefficientPropertyAccess

namespace Needle.XR.ARSimulation.ExampleComponents
{
    /// <summary>
    /// Simple component used by examples/samples to animate objects into position
    /// </summary>
    public class FlyIntoPosition : MonoBehaviour
    {
        public float Speed = 20f;
        public float Distance = 5;

        private Vector3 targetPosition = Vector3.zero;
        private Vector3 targetScale = Vector3.one;

        private void Start()
        {
            targetPosition = transform.position;
            targetScale = transform.localScale;
            MoveAway();
        }

        private void OnEnable()
        {
            if (targetPosition == Vector3.zero) return;
            MoveAway();
        }

        private void MoveAway()
        {
            transform.position = targetPosition + transform.up * Distance;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * Speed);
            transform.localScale = Vector3.Slerp(transform.localScale, targetScale, Time.deltaTime * Speed);
            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                transform.localScale = targetScale;
                transform.position = targetPosition;
                enabled = false;
            }
        }
    }
}