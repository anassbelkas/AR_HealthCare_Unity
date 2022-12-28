using System;
using System.Collections.Generic;
using Assets.Scripts.MonoBehaviours.Controllers;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Assets.Scripts.MonoBehaviours.AR
{
    [RequireComponent(typeof(ARRaycastManager))]
    public class PlaceOnPlane : MonoBehaviour
    {
        public static event Action OnModelPlaced;

        [SerializeField] 
        private PlacementIndicator _placementIndicatorPrefab;
        
        [SerializeField]
        private ARSession _arSession;

        [SerializeField] 
        private HumanController _humanController;
        
        private ARRaycastManager _raycastManager;
        
        private ARSessionOrigin _arSessionOrigin;
        
        private bool _isPlaced;
        
        private bool _isIndicatorInArea;
        
        private PlacementIndicator _placementIndicator;

        private void Awake()
        {
            InitializeArComponents();
            InitializeArMode();
        }

        private void Update()
        {
            SetIndicator();
            SetPreview();
        }

        private void OnEnable()
        {
            SubscribeOnEvents();
        }

        private void OnDisable()
        {
            UnSubscribeFromEvents();
        }

        private void SubscribeOnEvents()
        {
        }
        
        private void UnSubscribeFromEvents()
        {
        }
        
        private void InitializeArMode()
        {
            _arSession.enabled = true;
            _arSessionOrigin.trackablesParent.gameObject.SetActive(true);
            _placementIndicator = Instantiate(_placementIndicatorPrefab);
        }

        private void InitializeArComponents()
        {
            _raycastManager = GetComponent<ARRaycastManager>();
            _arSessionOrigin = GetComponent<ARSessionOrigin>();
        }

        private void SetIndicator()
        {
            var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
            var hits = new List<ARRaycastHit>();

            if (_raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon) && !_isPlaced)
            {
                _placementIndicator.transform.position =  hits[0].pose.position;
                _isIndicatorInArea = true;
            }
        }
        
        private void SetPreview()
        {
            if (Input.GetMouseButtonDown(0) && _isIndicatorInArea && !_isPlaced)
            {
                _placementIndicator.InitializePreview(_humanController);
                _arSessionOrigin.trackablesParent.gameObject.SetActive(false);
                _isPlaced = true;
                OnModelPlaced?.Invoke();
            }
        }
    }
}