using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;

namespace Needle.XR.ARSimulation.Simulation
{
    /// <summary>
    /// Dynamically generates AR point clouds from scene geometry
    /// <para>
    /// Using <see cref="Physics">Physics</see> raycasting to sample points in scene and add them as <see cref="XRPointCloud">PointCloud</see> to ARFoundation
    /// </para>
    /// </summary>
    public class SimulatedARPointRaycaster : SimulatedARElement, IPointCloudProvider
    {
        public float Interval = 0.1f;
        [Range(0, 100)] public uint RaysPerFrame = 5;
        public LayerMask Mask;
        public float MaxDistance = 5;
        [FormerlySerializedAs("MaxSize")] public float MaxCount = 1000;
        public float NormalOffset = 0.01f;
        public float RandomJitter = 0;
        public bool OrderHitsByDist = false;
        public bool DrawGizmosSelected;

        
        public TrackableId Id => this.GetTrackableId();
        public TrackingState State => TrackingState.Tracking;
        public Vector3 Position => Vector3.zero;
        public Quaternion Rotation => Quaternion.identity;
        public List<Vector3> Points => m_points;


        private readonly List<Vector3> m_points = new List<Vector3>();
        private readonly RaycastHit[] m_hits = new RaycastHit[10];

        [SerializeField, HideInInspector] private bool _isFirstValidate = true;
        
#if UNITY_EDITOR
        private Camera m_cam;
        private float m_lastRun = -1;

        private void OnValidate()
        {
            if (!_isFirstValidate) return;
            _isFirstValidate = false;
            if (Mask.value == 0 && !SimulatedAREnvironmentManager.Exists)
                Mask = LayerMask.GetMask("Default");
        }
        
        private void OnEnable()
        {
            SimulatedARPointCloudRegistry.Register(this);
        }

        private void OnDisable()
        {
            m_points.Clear();
            SimulatedARPointCloudRegistry.Unregister(this);
        }

        private void Update()
        {
            if (Interval > 0)
            {
                if (Time.time - m_lastRun < Interval) return;
                m_lastRun = Time.time;
            }
            
            if (!m_cam)
            {
                if (!SceneSetup.TryGetARCamera(out m_cam))
                    return;
            }


            if (MaxCount > 0)
            {
                while (m_points.Count > MaxCount)
                {
                    m_points.RemoveAt(0); // not optimal as whole list needs to be moved
                }
            }
            else m_points.Clear();

            // add reality layer to mask
            Mask = SimulatedAREnvironmentManager.IncludeEnvironment(Mask);
            for (var i = 0; i < RaysPerFrame; i++)
            {
                var ray = m_cam.ScreenPointToRay(new Vector2(Screen.width * Random.value, Screen.height * Random.value));
                ray = ray.ToWorld();
                var hits = Physics.RaycastNonAlloc(ray, m_hits,
                    MaxDistance,
                    Mask);
                if (hits <= 0) continue;
                var hit = OrderHitsByDist ? m_hits.OrderBy(p => Vector3.Distance(ray.origin, p.point)).First() : m_hits[(int) (Random.value * hits)];
                m_points.Add(hit.point + Random.insideUnitSphere * RandomJitter + hit.normal * NormalOffset);
            }

            SimulatedARPointCloudRegistry.Update(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!DrawGizmosSelected) return;
            if (m_points == null) return;
            Gizmos.color = Color.yellow;
            var s = Vector3.one * .1f;
            foreach (var p in m_points)
            {
                Gizmos.DrawWireCube(p, s);
            }
        }
#endif
    }
}