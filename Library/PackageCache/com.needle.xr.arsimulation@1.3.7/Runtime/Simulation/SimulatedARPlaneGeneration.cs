using System;
using System.Collections.Generic;
using Needle.XR.ARSimulation.Extensions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;

namespace Needle.XR.ARSimulation.Simulation
{
    /// <summary>
    /// Used to dynamically generate planes at runtime mimicking scene meshing on device.
    /// Only one <see cref="SimulatedARPlaneGeneration"/> component should be active in the scene at once.
    /// <para>
    /// It uses <see cref="Physics">Physics</see> raycasts to sample positions in the scene. Make sure to add <see cref="Collider">Collider</see> components to <see cref="GameObject">GameObjects</see> you want to use for meshing.
    /// </para>
    /// </summary>
    public class SimulatedARPlaneGeneration : SimulatedARElement
    {
        /// <summary>
        /// Amount of samples to capture before trying to generate a plane from it 
        /// </summary>
        public int sampleCount = 3;

        /// <summary>
        /// Interval in seconds for trying to sample new points
        /// </summary>
        public float interval = .2f;

        /// <summary>
        /// Max sample point distance to camera to be used for meshing
        /// </summary>
        public float maxDistance = 5;

        /// <summary>
        /// Controls how flat a surface has to be to generate a plane from it (lower means flatter, higher means more curves accepted)
        /// </summary>
        [Tooltip("Controls how flat a surface has to be to generate a plane from it (lower means flatter, higher means more curves accepted)")]
        [Range(0.01f, 0.3f)]
        public float planeEps = 0.1f;

        /// <summary>
        /// <see cref="LayerMask">LayerMask</see> used for raycasting (to exclude objects from meshing)
        /// </summary>
        public LayerMask mask;

        private Camera _camera;
        private Transform _cameraTransform;
        private ARSessionOrigin _session;

        private readonly RaycastHit[] _hitBuffer = new RaycastHit[100];
        private readonly List<RaycastHit> _currentHits = new List<RaycastHit>();
        private readonly List<Vector3> _sampleBuffer = new List<Vector3>();
        private Vector3 _sampleNormal;


        private readonly List<Vector3> _debugPoints = new List<Vector3>();

        private bool _running;
        private float _lastRunTime;

        private List<BoundedPlane> _planes = new List<BoundedPlane>();
        
        [SerializeField, HideInInspector] private bool _isFirstValidate = true;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_isFirstValidate) return;
            _isFirstValidate = false;
            if (mask.value == 0 && !SimulatedAREnvironmentManager.Exists)
                mask = LayerMask.GetMask("Default");
        }

        private void Start()
        {
            _session = FindObjectOfType<ARSessionOrigin>();

            if (Application.isPlaying)
            {
                var planeManager = FindObjectOfType<ARPlaneManager>();
                if (planeManager && planeManager.planePrefab)
                {
                    var prefab = planeManager.planePrefab;
                    prefab = planeManager.planePrefab = Instantiate(prefab);
                    if (prefab.TryGetComponent(out MeshFilter mf))
                    {
                        if (mf.sharedMesh)
                        {
                            mf.sharedMesh = null;
                        }
                    }
                }
            }
        }
        
        private void OnEnable()
        {
            _debugPoints.Clear();
            _running = true;
        }

        private void OnDisable()
        {
            _debugPoints.Clear();
            _sampleBuffer.Clear();
            _currentHits.Clear();
            foreach (var bp in _planes) 
                SimulatedARPlanesRegistry.Unregister(bp.trackableId);
            _planes.Clear();
        }

        private void Update()
        {
            sampleCount = Mathf.Max(sampleCount, 3);

            if (Time.time - _lastRunTime > interval)
            {
                _debugPoints.Clear();
                _running = true;
            }

            if (!_running) return;
            if (!_camera)
            {
                if (!SceneSetup.TryGetARCamera(out _camera))
                    return;
                _cameraTransform = _camera.transform;
            }
            if (!_camera) return;

            var ray = _camera.ScreenPointToRay(new Vector3(Random.value * Screen.width, Random.value * Screen.height, 0));
            // add reality layer to mask
            mask = SimulatedAREnvironmentManager.IncludeEnvironment(mask);
            var hits = Physics.RaycastNonAlloc(ray.ToWorld(), _hitBuffer, maxDistance, mask);

            if (hits <= 0) return;

            if (!_session) _session = FindObjectOfType<ARSessionOrigin>();
            
            // Array.Sort(_hitBuffer, (h1, h2) => (int)(100*(h1.distance- h2.distance)));
            var closest = new RaycastHit() {distance = float.MaxValue};
            for (var i = 0; i < hits; i++)
            {
                var h = _hitBuffer[i];
                if (!h.transform || !_session || h.transform.parent == _session.trackablesParent) continue;
                if (h.distance < closest.distance) closest = h;
            }

            if (closest.distance < float.MaxValue)
            {
                var hit = closest;
                CheckMatchingSampleBuffer(hit, _sampleBuffer);
            }
        }

        private bool CheckMatchingSampleBuffer(RaycastHit hit, IList<Vector3> sampleBuffer)
        {
            if (sampleBuffer.Count > sampleCount)
            {
                var localPoints = new List<Vector2>();
                var plane = BoundedPlaneHelper.ToBoundedPlane(sampleBuffer, _sampleNormal, ref localPoints);
                if (localPoints.Count >= 3)
                {
                    _planes.Add(plane);
                    SimulatedARPlanesRegistry.Register(plane, localPoints);
                    sampleBuffer.Clear();
                    _running = false;
                    _lastRunTime = Time.time;
                    return true;
                }
            }
            else
            {
                if (sampleBuffer.Count <= 0)
                {
                    _sampleNormal = hit.normal;
                    sampleBuffer.Add(hit.point);
                    _debugPoints.Add(hit.point);
                    return true;
                }

                var right = _cameraTransform.right;
                var rotation = Quaternion.LookRotation(right, hit.normal);
                var pRotation = Quaternion.LookRotation(right, _sampleNormal);
                if (Geometry.Geometry.IsOnOnePlane(hit.point, rotation, sampleBuffer[0], pRotation, planeEps))
                {
                    sampleBuffer.Add(hit.point);
                    _debugPoints.Add(hit.point);
                    return true;
                }

                sampleBuffer.Clear();
                _debugPoints.Clear();
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            var offset = new Vector3(0, 0.0001f, 0);
            for (var i = 0; _debugPoints != null && i < _debugPoints.Count; i++)
            {
                var p = _debugPoints[i] + offset;
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(p, 0.02f);
                Handles.Label(p + new Vector3(0, 0.08f, 0), i.ToString());
            }
        }
#endif
    }
}