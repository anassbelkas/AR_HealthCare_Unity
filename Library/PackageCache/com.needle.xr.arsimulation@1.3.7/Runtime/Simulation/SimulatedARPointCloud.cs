using System;
using System.Collections.Generic;
using UnityEngine;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.Serialization;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;

namespace Needle.XR.ARSimulation.Simulation
{
    /// <summary>
    /// Providing a simple interface for AR point clouds. Use the <see cref="points"/> field to set you own points in local space of this component OR set <see cref="randomPointsCount"/> to a value greater than zero to generate random points.
    /// </summary>
    public class SimulatedARPointCloud : SimulatedARElement, IPointCloudProvider
    {
        public enum PointCloudShape
        {
            Planar = 0,
            Spherical = 1,
        }

        /// <summary>
        /// points in local space. If you set new points make sure to call <see cref="MarkPointsUpdated"/> to make sure your update is passed on to ARFoundation
        /// </summary>
        [NonSerialized]
        public List<Vector3> points = new List<Vector3>();

        private List<Vector3> currentPoints = new List<Vector3>();

        public bool updatePointsImmediately = false;

        [Header("Random Points")] public PointCloudShape randomPointsShape = PointCloudShape.Planar;
        public int randomSeed;

        /// <summary>
        /// set this value to less than one if you dont want to generate random points
        /// </summary>
        public int randomPointsCount = 10;

        /// <summary>
        /// local space radius for random points
        /// </summary>
        public float randomPointsRadius => transform.localScale.x;
        
        /// <summary>
        /// set true to draw point sampling as <see cref="Gizmos">Gizmos</see>
        /// for debugging
        /// </summary>
        [FormerlySerializedAs("gizmos")] [Header("Debug")] [SerializeField]
        public bool drawGizmos = true;

        /// <summary>
        /// should be called after setting custom <see cref="points"/> to make sure they are passed to AR Foundation
        /// </summary>
        public void MarkPointsUpdated(bool immediate)
        {
            this.updatePointsImmediately = immediate;
            this.m_changed = true;
            currentPoints.Clear();
            if (updatePointsImmediately) 
                currentPoints.AddRange(points);
        }

        /// <summary>
        /// can be used to generate new random points. The number of points can be set with <see cref="randomPointsCount"/> and the radius with <see cref="randomPointsRadius"/>
        /// </summary>
        [ContextMenu(nameof(GenerateRandomPoints))]
        public void GenerateRandomPoints()
        {
            Random.InitState(randomSeed);
            points.Clear();
            var scale = transform.lossyScale;
            for (var i = 0; i < randomPointsCount; i++)
            {
                switch (randomPointsShape)
                {
                    case PointCloudShape.Planar:
                        var rx = (Random.value - .5f) * scale.x;
                        var rz = (Random.value - .5f) * scale.y;
                        points.Add(new Vector3(rx, 0, rz));
                        break;
                    case PointCloudShape.Spherical:
                        points.Add(Random.insideUnitSphere * randomPointsRadius);
                        break;
                }
            }
            
            MarkPointsUpdated(updatePointsImmediately);
        }

        private bool m_changed = false;
        private bool m_hasStarted = false;

        protected override void Awake()
        {
            base.Awake();
            if (points == null) points = new List<Vector3>();
        }

        private void Start()
        {
            SimulatedARPointCloudRegistry.Register(this);
            m_hasStarted = true;
        }

        private void OnEnable()
        {
            if (m_hasStarted)
                SimulatedARPointCloudRegistry.Register(this);
        }

        private void OnDisable()
        {
            SimulatedARPointCloudRegistry.Unregister(this);
        }

        private PointCloudShape previousShape;
        private float previousRadius;
        private Vector3 previousScale;

        private void OnValidate()
        {
            if (randomSeed == 0) randomSeed = (int)(Random.value * int.MaxValue);
            m_changed = true;
            DetectChange();
        }

        private void DetectChange()
        {
            if (randomPointsCount > 0)
            {
                if (points.Count != randomPointsCount ||
                    previousShape != randomPointsShape ||
                    previousScale != transform.localScale ||
                    Math.Abs(previousRadius - randomPointsRadius) > 0.0001f)
                {
                    GenerateRandomPoints();
                }
            }
            previousShape = randomPointsShape;
            previousRadius = randomPointsRadius;
            previousScale = transform.localScale;
        }

        private void Update()
        {
            if (m_changed || transform.hasChanged || currentPoints.Count < points.Count)
            {
                if(currentPoints.Count < points.Count)
                    currentPoints.Add(points[currentPoints.Count]);
                
                var t = transform;
                SimulatedARPointCloudRegistry.Update(this, this.TransformPoseToSessionSpaceIfNecessary);
                transform.hasChanged = false;
                m_changed = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            if (points == null || points.Count <= 0) return;
            var t = transform;
            Gizmos.color = new Color(1, 1, 0, .1f);
            if (!Application.isPlaying)
            {
                if (randomPointsCount > 0)
                {
                    switch (randomPointsShape)
                    {
                        case PointCloudShape.Planar:
                            var localScale = transform.lossyScale;
                            Gizmos.DrawWireCube(t.position, new Vector3(localScale.x, 0, localScale.y));
                            break;
                        case PointCloudShape.Spherical:
                            Gizmos.DrawWireSphere(t.position, randomPointsRadius);
                            break;
                    }
                }
                OnDrawPointGizmos(0.15f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            DetectChange();
            OnDrawPointGizmos(.5f);
        }

        private void OnDrawPointGizmos(float alpha)
        {
            if (!drawGizmos) return;
            if (points == null) return;
            var t = transform;

            Gizmos.color = new Color(1, 1, 0, alpha);
            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            var size = Vector3.one * 0.01f;
            foreach (var p in points)
                Gizmos.DrawWireCube(p, size);
        }

        public TrackableId Id => this.GetTrackableId();
        public TrackingState State => TrackingState.Tracking;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public List<Vector3> Points => this.currentPoints ?? (this.currentPoints = new List<Vector3>());
    }
}