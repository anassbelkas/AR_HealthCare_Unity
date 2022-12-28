using System.Collections.Generic;
using UnityEngine;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Simulation
{
    /// <summary>
    /// Add this component to a GameObject to spawn a AR tracked plane. It can be positioned, scaled, rotated both at edit and at runtime if <see cref="allowRuntimeUpdate">enabled</see>
    /// </summary>
    /// <inheritdoc cref="IPlaneProvider"/>
    public class SimulatedARPlane : SimulatedARElement, IPlaneProvider
    {
        public TrackableId GetId()
        {
            return transform.GetTrackableId();
        }    

        public BoundedPlane GetPlane()
        {
            BoundedPlane boundedPlane;
            if (!_triedGettingMeshFilter && !_meshFilterOnObject)
                _meshFilterOnObject = this.GetComponent<MeshFilter>();
            _triedGettingMeshFilter = true;
            if (_meshFilterOnObject && _meshFilterOnObject.sharedMesh)
                boundedPlane = _meshFilterOnObject.ToBoundedPlane(state, classification, TransformPoseToSessionSpaceIfNecessary);
            else
                boundedPlane = this.transform.ToBoundedPlane(state, classification, TransformPoseToSessionSpaceIfNecessary);
            return boundedPlane;
        }

        public IReadOnlyList<Vector2> Bounds => null;

        public bool AllowMerging => allowMerging;


        [SerializeField] private TrackingState state = TrackingState.Tracking;
        [SerializeField] private PlaneClassification classification = PlaneClassification.None;

        /// <summary>
        /// Set true to allow merging of overlapping planes
        /// </summary>
        [Tooltip("Allows merging overlapping planes")]
        public bool allowMerging = false;

        /// <summary>
        /// Editing the transform during runtime updates the AR plane
        /// </summary>
        [Tooltip("Allows the plane to be moved once spawned")]
        public bool allowRuntimeUpdate = false;

        private bool _hasChanged = false;
        private MeshFilter _meshFilterOnObject;
        private bool _triedGettingMeshFilter = false;

        /// <summary>
        /// Update the plane's tracking state to <paramref name="newState"/>
        /// </summary>
        /// <param name="newState"></param>
        public void SetTrackingState(TrackingState newState)
        {
            if (state == newState) return;
            this.state = newState;
            _hasChanged = true;
        }

        public void SetClassification(PlaneClassification newClassification)
        {
            if (classification == newClassification) return;
            this.classification = newClassification;
            _hasChanged = true;
        }

        [ContextMenu(nameof(RequestPlaneUpdate))]
        public void RequestPlaneUpdate()
        {
            updateRequested = true;
        }

        private bool updateRequested;

#if UNITY_EDITOR
        protected void OnEnable()
        {
            transform.hasChanged = false;
            _hasChanged = false;
            SimulatedARPlanesRegistry.Register(this);
        }

        protected void OnDisable()
        {
            SimulatedARPlanesRegistry.Unregister(this);
        }

        private void OnValidate()
        {
            _hasChanged = true;
            _triedGettingMeshFilter = false;
        }

        // TODO: support scaled session origins
        private void LateUpdate()
        {
            if (!allowRuntimeUpdate && !updateRequested) return;

            if (!updateRequested && !transform.hasChanged && !_hasChanged) return;

            SimulatedARPlanesRegistry.Update(this);
            transform.hasChanged = false;
            _hasChanged = false;
            updateRequested = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SimulatedARPlanesRegistry.Unregister(this);
        }


        private void OnDrawGizmos()
        {
            PrepareGizmo(out var size, false);
        }

        private void OnDrawGizmosSelected()
        {
            PrepareGizmo(out var size, true);
        }

        private void PrepareGizmo(out Vector2 size, bool selected)
        {
            var t = transform;
            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            var plane = GetPlane();
            size = plane.size;
            Gizmos.color = new Color(1, 1, 0, .2f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0, size.y));
            Gizmos.color = selected ? new Color(1, 1, 0, .2f) : new Color(1, 1, 0, .05f);
            Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, 0, size.y));
        }
#endif
    }
}