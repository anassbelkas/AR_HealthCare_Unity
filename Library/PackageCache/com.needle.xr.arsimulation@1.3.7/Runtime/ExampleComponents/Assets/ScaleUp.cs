using UnityEngine;

namespace Needle.XR.ARSimulation.ExampleComponents
{
    public class ScaleUp : MonoBehaviour
    {
        public float lifetime = -1;
        
        private Vector3 targetScale;
        private float startTime;

        private void Start()
        {
            targetScale = transform.localScale;
            transform.localScale = Vector3.one * 0.00001f;
            startTime = Time.time;
        }

        private void Update()
        {
            transform.localScale = Vector3.Slerp(transform.localScale, targetScale, Time.deltaTime / .1f);

            if (lifetime > 0 && Time.time - startTime > lifetime)
            {
                targetScale = Vector3.zero;
                if (transform.localScale.x <= 0.01f)
                {
                    Object.Destroy(this.gameObject);
                }
            }
        }
    }
}

