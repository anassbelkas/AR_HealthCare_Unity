using Assets.Scripts.MonoBehaviours.Controllers;
using UnityEngine;

namespace Assets.Scripts.MonoBehaviours.AR
{
    public class PlacementIndicator : MonoBehaviour
    {
        [SerializeField] 
        private GameObject _point;
        
        public void InitializePreview(HumanController previewObject)
        {
            _point.SetActive(false);
            Instantiate(previewObject, transform);

            transform.LookAt(Camera.main.transform);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y + 180, 0);
        }
    }
}