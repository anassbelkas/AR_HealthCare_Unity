using UnityEngine;

namespace Assets.Scripts.MonoBehaviours.Controllers
{
    public class Zoom : MonoBehaviour
    {
        private float _lastDist;
        private float _touchDist;
        private float _newScale;
        private Transform _transform;

        private void Awake()
        {
            _transform = GetComponent<Transform>();
        }

        private void Update()
        {
            if (Input.touchCount == 2)
            {
                var touch1 = Input.GetTouch(0);
                var touch2 = Input.GetTouch(1);

                if (touch1.phase == TouchPhase.Began && touch2.phase == TouchPhase.Began)
                {
                    _lastDist = Vector2.Distance(touch1.position, touch2.position);
                }

                if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
                {
                    var newDist = Vector2.Distance(touch1.position, touch2.position);
                    _touchDist = _lastDist - newDist;
                    _lastDist = newDist;
                    
                    _newScale = _touchDist * 0.0001f;

                    _transform.localScale = new Vector3(Mathf.Clamp(_transform.localScale.x - _newScale, 0.001f, 1f),
                                                        Mathf.Clamp(_transform.localScale.y - _newScale, 0.001f, 1f),
                                                        Mathf.Clamp(_transform.localScale.z - _newScale, 0.001f, 1f));
                }
            }
        }
    }
}