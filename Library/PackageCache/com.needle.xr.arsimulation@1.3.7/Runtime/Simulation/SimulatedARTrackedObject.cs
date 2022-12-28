using System;
using Needle.XR.ARSimulation.Extensions;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.XR.ARFoundation;
using Random = UnityEngine.Random;

#else
using Object = UnityEngine.Object;
#endif

namespace Needle.XR.ARSimulation.Simulation
{
    public class SimulatedARTrackedObject : MonoBehaviour
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        , ITrackedObjectProvider
#endif
    {
        private enum GizmoStyle
        {
            BoundingBox = 0,
            None = 999
        }

        [SerializeField, HideInInspector] private long guidLow, guidHigh;

#pragma warning disable CS0414
        [SerializeField] private TrackingState trackingState;
        public bool simulateTracking = true;
        [SerializeField] private GizmoStyle gizmosStyle = GizmoStyle.BoundingBox;
#pragma warning restore CS0414


        public TrackableId TrackableId => this.GetTrackableId();

        public Pose Pose
        {
            get
            {
                var t = transform;
                var pos = t.position;
                if (trackingState == TrackingState.Limited) pos += offset;
                return new Pose(pos, t.rotation);
            }
        }

        public IntPtr NativePtr => IntPtr.Zero;

        public TrackingState TrackingState
        {
            get => trackingState;
            set
            {
                if (trackingState != value) _isDirty = true;
                trackingState = value;
            }
        }

        public Guid Entry
        {
            get
            {
                if (_currentEntry == Guid.Empty && guidLow > 0 && guidHigh > 0)
                {
                    _currentEntry = GuidUtil.Compose((ulong) guidLow, (ulong) guidHigh);
                }

                return _currentEntry;
            }
            set
            {
                var pl = guidLow;
                var ph = guidHigh;
                value.Decompose(out var low, out var high);
                guidLow = (long) low;
                guidHigh = (long) high;
                if (pl != guidLow || ph != guidHigh)
                {
                    _isDirty = true;
                }
            }
        }
#pragma warning disable CS0414
#pragma warning disable CS0649
        private bool _isDirty = false;
        private Vector3 offset;
        private Guid _currentEntry;
#pragma warning restore CS0649
#pragma warning restore CS0414


#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER && UNITY_EDITOR
        private void OnValidate()
        {
            _isDirty = true;
        }

        private static ARTrackedObjectManager _objectManager;

        private void Awake()
        {
            if (guidLow <= 0 && guidHigh <= 0)
            {
                if (!_objectManager)
                    _objectManager = FindObjectOfType<ARTrackedObjectManager>();
                if (!_objectManager || !_objectManager.referenceLibrary || _objectManager.referenceLibrary.count <= 0) return;
                Entry = _objectManager.referenceLibrary[0].guid;
            }
        }

        private void OnEnable()
        {
            _isDirty = true;
        }

        private void OnDisable()
        {
            _isDirty = false;
            SimulatedTrackedObjectsRegistry.Remove(this);
        }

        private void LateUpdate()
        {
            if (simulateTracking)
                TrackingState = OnSimulateTracking();

            if (trackingState == TrackingState.Limited)
            {
                Random.InitState((int) (Time.time * 20));
                offset = Vector3.Lerp(offset, new Vector3(Random.value - .5f, Random.value - .5f, Random.value - .5f), Time.deltaTime);
                _isDirty = true;
            }

            if (_isDirty || transform.hasChanged)
            {
                transform.hasChanged = false;
                _isDirty = false;

                var current = GuidUtil.Compose((ulong) guidLow, (ulong) guidHigh);
                if (_currentEntry != current)
                {
                    if (_currentEntry != Guid.Empty)
                        SimulatedTrackedObjectsRegistry.Remove(this);
                    _currentEntry = current;
                    _isDirty = true;
                }
                else if (_currentEntry != Guid.Empty)
                {
                    switch (trackingState)
                    {
                        case TrackingState.None:
                            SimulatedTrackedObjectsRegistry.Remove(this);
                            break;
                        case TrackingState.Limited:
                        case TrackingState.Tracking:
                            SimulatedTrackedObjectsRegistry.Update(this);
                            break;
                    }
                }
            }
        }


        private static Camera _arCamera;

        private static Camera arCamera
        {
            get
            {
                if (!_arCamera)
                {
                    var session = FindObjectOfType<ARSessionOrigin>();
                    if (session)
                        _arCamera = session.camera;
                    else _arCamera = Camera.main;
                }

                return _arCamera;
            }
        }

        private float leftTrackingTime = -1, enteredTrackingTime = -1;

        private TrackingState OnSimulateTracking()
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(arCamera);
            // we shrink view size a bit to detect the image later
            var t = transform;
            var Size = t.localScale;
            var viewSize = new Vector3(Size.x * .02f, Size.y * .02f, Size.z * .02f);
            var visible = GeometryUtility.TestPlanesAABB(planes, new Bounds(t.position, viewSize));
            if (visible)
            {
                if (enteredTrackingTime <= 0) enteredTrackingTime = Time.time;
                leftTrackingTime = -1;
                return Time.time - enteredTrackingTime < .5f ? TrackingState.Limited : TrackingState.Tracking;
            }

            enteredTrackingTime = -1;
            if (leftTrackingTime <= 0) leftTrackingTime = Time.time;
            return Time.time - leftTrackingTime < 1 ? TrackingState.Limited : TrackingState.None;
        }

        private void OnDrawGizmos()
        {
            if (gizmosStyle == GizmoStyle.None) return;
            var t = transform;
            Gizmos.matrix = t.localToWorldMatrix;
            Gizmos.color = new Color(.2f, .2f, .2f, .5f);
            Gizmos.DrawWireCube(new Vector3(0, .5f, 0), Vector3.one);
            Gizmos.color = new Color(1f, .2f, .2f, .5f);
            Gizmos.DrawLine(Vector3.zero, Vector3.right);
            Gizmos.color = new Color(.2f, .2f, 1f, .5f);
            Gizmos.DrawLine(Vector3.zero, Vector3.forward);
            Gizmos.color = new Color(.2f, 1f, .2f, .5f);
            Gizmos.DrawLine(Vector3.zero, Vector3.up);
        }
#endif
    }
}