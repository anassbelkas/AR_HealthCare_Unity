using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using Needle.XR.ARSimulation.Simulation;
using Needle.XR.ARSimulation.Simulation.Geometry;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

namespace Needle.XR.ARSimulation
{
    public static class SimulatedARPlanesRegistry
    {
        internal static readonly List<PlaneData> AddedList = new List<PlaneData>();
        internal static readonly List<TrackableId> RemovedList = new List<TrackableId>();
        internal static readonly List<PlaneData> UpdatedList = new List<PlaneData>();

        internal static void Clear()
        {
            AddedList.Clear();
            RemovedList.Clear();
            UpdatedList.Clear();
        }

        public static void Register(BoundedPlane plane, List<Vector2> triangles = null)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            RemovedList.RemoveAll(t => t == plane.trackableId);
            UpdatedList.RemoveAll(t => t.Id == plane.trackableId);
            AddedList.Add(plane.FromPlane(triangles, triangles != null));
        }

        public static void Register(IPlaneProvider prov)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            // settings HideFlags to HideAndDontSave makes onenable and ondisable being called multiple times the same frame 
            // thats why we have to make sure we only register the last add or remove call
            RemovedList.RemoveAll(t => t == prov.GetId());
            UpdatedList.RemoveAll(t => t.Id == prov.GetId());
            AddedList.Add(prov.GetPlane().FromPlane(prov.Bounds, prov.AllowMerging));
        }

        public static void Unregister(TrackableId id)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            AddedList.RemoveAll(a => a.Id == id);
            UpdatedList.RemoveAll(t => t.Id == id);
            RemovedList.Add(id);
        }

        public static void Unregister(IPlaneProvider prov)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            // settings HideFlags to HideAndDontSave makes onenable and ondisable being called multiple times the same frame 
            // thats why we have to make sure we only register the last add or remove call
            var id = prov.GetId();
            AddedList.RemoveAll(a => a.Id == id);
            UpdatedList.RemoveAll(t => t.Id == prov.GetId());
            RemovedList.Add(id);
        }

        public static void Update(BoundedPlane plane, List<Vector2> triangles = null)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            InternalUpdate(plane.FromPlane(triangles));
        }

        public static void Update(IPlaneProvider prov)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            var plane = prov.GetPlane().FromPlane(prov.Bounds, prov.AllowMerging);
            InternalUpdate(plane);
        }

        private static void InternalUpdate(PlaneData plane)
        {
            if (!ARSimulationPlaneProvider.IsRunning) return;
            RemovedList.RemoveAll(t => t == plane.Id);
            // make sure if a plane has been added in this frame
            // and updated in the same frame
            // we "update" the entry in the added list instead
            if (AddedList.Any(p => p.Id == plane.Id))
            {
                AddedList.RemoveAll(p => p.Id == plane.Id);
                AddedList.Add(plane);
            }
            else
            {
                // the plane has not been added this frame
                UpdatedList.Add(plane);
            }
        }
    }

    /// <summary>
    /// The ARSimulation implementation of the <c>XRPlaneSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationPlaneProvider : XRPlaneSubsystem
    {
#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        internal static void Clear()
        {
            // ARSimulationProvider._mergedPlanes.Clear();
        }

        // internal static Dictionary<TrackableId, PlaneData>.ValueCollection Planes => ARSimulationProvider._planes.Values;
        // internal static List<PlaneData> MergedPlanes => ARSimulationProvider._mergedPlanes;

        private class ARSimulationProvider : Provider
        {
            #region PLANE REGISTRY

            private static readonly Dictionary<TrackableId, PlaneData> _planes = new Dictionary<TrackableId, PlaneData>();

            private static bool AllowPlane(PlaneData data, PlaneDetectionMode mode)
            {
                if (mode == PlaneDetectionMode.None) return false;
                var pose = data.Plane.pose;
                var upDot = Vector3.Dot(pose.up.normalized, Vector3.up);
                var isHorizontal = upDot >= 0.8f;
                var isVertical = Mathf.Abs(upDot) <= 0.2f;
                // Debug.Log(upDot + ", " + pose.up + ", horizontal? " + isHorizontal + ", vertical?: " + isVertical);
                if (isHorizontal && (mode & PlaneDetectionMode.Horizontal) != 0) return true;
                if(isVertical && (mode & PlaneDetectionMode.Vertical) != 0) return true;
                return false;
            }
            
            /// <summary>
            /// Add the plane to cache if not added previously
            /// </summary>
            /// <param name="list">list of marked as added planes</param>
            private static void RegisterAddedPlanes(IList<PlaneData> list, PlaneDetectionMode mode)
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];
                    if (_planes.ContainsKey(entry.Plane.trackableId) || !AllowPlane(entry, mode))
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    _planes.Add(entry.Plane.trackableId, entry);
                }
            }

            /// <summary>
            /// Making sure the plane has been added
            /// </summary>
            /// <param name="list">list of marked as updated planes</param>
            private static void RegisterUpdatedPlanes(IList<PlaneData> list, PlaneDetectionMode mode)
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];
                    // if the plane was not registered yet remove it from updated list
                    if (!_planes.ContainsKey(entry.Plane.trackableId) || !AllowPlane(entry, mode))
                    {
#if UNITY_EDITOR || DEBUG || DEVELOPMENT_BUILD
                        Debug.LogWarning("Plane marked as Updated has not previously been registered");
#endif
                        list.RemoveAt(i);
                        continue;
                    }

                    _planes[entry.Plane.trackableId] = entry;
                }
            }

            /// <summary>
            /// Make sure the 
            /// </summary>
            /// <param name="list"></param>
            private static void RegisterRemovedPlanes(IList<TrackableId> list)
            {
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var entry = list[i];
                    if (!_planes.ContainsKey(entry))
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    _planes.Remove(entry);
                }
            }

            #endregion

            private ARPlaneManager planeManager;
            private void EnsurePlaneManagerPrefab()
            {
                if (!planeManager)
                {
                    planeManager = Object.FindObjectOfType<ARPlaneManager>();
                    if (!planeManager) return;
                }

                if (!planeManager.planePrefab)
                {
                    // build a plane prefab, this happens automatically in builds when no prefab is assigned
                    var planePrefab = new GameObject("ARSimPlanePrefab");
                    planePrefab.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                    planePrefab.AddComponent<MeshFilter>();
                    planePrefab.AddComponent<MeshCollider>();
                    planePrefab.AddComponent<ARPlane>();
                    planePrefab.AddComponent<ARPlaneMeshVisualizer>();
                    planeManager.planePrefab = planePrefab;
                }
            }

            public override TrackableChanges<BoundedPlane> GetChanges(
                BoundedPlane defaultPlane,
                Allocator allocator)
            {
                if (!IsRunning)
                {
                    // when stopping the subsystem remove all known planes
                    if (_planes != null && _planes.Count > 0)
                    {
                        var removeAll = new TrackableChanges<BoundedPlane>(
                            0,
                            0,
                            _planes.Count,
                            allocator);
                        removeAll.removed.CopyFrom(_planes.Keys.ToArray());
                        _planes.Clear();
                        return removeAll;
                    }

                    return new TrackableChanges<BoundedPlane>();
                }
                
                // handle overlaps must happen before registering new planes right now
                HandleOverlapsOfNewlyAddedPlanes();

                RegisterUpdatedPlanes(SimulatedARPlanesRegistry.UpdatedList, _currentMode);
                RegisterAddedPlanes(SimulatedARPlanesRegistry.AddedList, _currentMode);
                RegisterRemovedPlanes(SimulatedARPlanesRegistry.RemovedList);

                var changes = new TrackableChanges<BoundedPlane>(
                    SimulatedARPlanesRegistry.AddedList.Count,
                    SimulatedARPlanesRegistry.UpdatedList.Count,
                    SimulatedARPlanesRegistry.RemovedList.Count,
                    allocator);

                changes.added.CopyFrom(SimulatedARPlanesRegistry.AddedList);
                changes.updated.CopyFrom(SimulatedARPlanesRegistry.UpdatedList);
                changes.removed.CopyFrom(SimulatedARPlanesRegistry.RemovedList);

                if (changes.added.Length > 0 || changes.updated.Length > 0)
                {
                    EnsurePlaneManagerPrefab();
                }

                SimulatedARPlanesRegistry.Clear();
                return changes;
            }

            private static void HandleOverlapsOfNewlyAddedPlanes()
            {
                var list = SimulatedARPlanesRegistry.AddedList;
                if (list.Count <= 0) return;
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var added = list[i];
                    if (!added.HasBounds) continue;
                    if (!added.AllowMerging) continue;
                    foreach (var kvp in _planes)
                    {
                        var existing = kvp.Value;
                        if (!existing.HasBounds) continue;
                        if (!existing.AllowMerging) continue;
                        // skip self
                        if (added == existing)
                            continue;

                        // only if points are on the same plane we can merge them
                        if (!Geometry.IsOnOnePlane(added, existing)) continue;

                        if (!Geometry.IsConvexHullOverlapping(added.BoundsCenter, added.Bounds, existing.BoundsCenter, existing.Bounds)) continue;
                        var merged = MergePlanes(existing, added);
                        // _mergedPlanes.Add(merged);
                        SimulatedARPlanesRegistry.UpdatedList.Add(merged);
                        list.RemoveAt(i);
                        break;
                    }
                }
            }

            private static PlaneData MergePlanes(PlaneData keep, PlaneData merge)
            {
                var bounds = new List<Vector2>(keep.Bounds);
                var invRot = Quaternion.Inverse(keep.Plane.pose.rotation);
                for (var i = 0; i < merge.Bounds.Count; i++)
                {
                    var pt = merge.Bounds[i];
                    // converting to new hull space
                    var converted = merge.Plane.pose.rotation * new Vector3(pt.x, 0, pt.y);
                    converted += merge.Plane.pose.position;
                    converted -= keep.Plane.pose.position;
                    converted = invRot * converted;
                    bounds.Add(new Vector2(converted.x, converted.z));
                }

                bounds = Geometry.GetConvexHull(bounds, false);
                return new PlaneData(keep.Plane, bounds);
            }

            public override void GetBoundary(
                TrackableId trackableId,
                Allocator allocator,
                ref NativeArray<Vector2> boundary)
            {
                var data = _planes[trackableId];

                CreateOrResizeNativeArrayIfNecessary(data.Bounds.Count, allocator, ref boundary);
                for (var i = 0; i < boundary.Length; i++) boundary[i] = data.Bounds[i];
            }

#if UNITY_AR_FOUNDATION_4_OR_NEWER
            private static PlaneDetectionMode _currentMode;
            public override PlaneDetectionMode currentPlaneDetectionMode => _currentMode;
            public override PlaneDetectionMode requestedPlaneDetectionMode
            {
                get => currentPlaneDetectionMode;
                set => _currentMode = value;
            }
#else
            private static PlaneDetectionMode _currentMode;
            public override PlaneDetectionMode planeDetectionMode
            {
                set => _currentMode = base.planeDetectionMode = value;
            }
#endif

            public override void Start()
            {
                IsRunning = true;
            }

            public override void Stop()
            {
                IsRunning = false;
            }

            public override void Destroy()
            {
                IsRunning = false;
            }
        }
        
        internal static bool IsRunning { get; private set; }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            var cinfo = new XRPlaneSubsystemDescriptor.Cinfo
            {
                id = "ARSimulation-Plane",
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationPlaneProvider),
#else
                subsystemImplementationType = typeof(ARSimulationPlaneProvider),
#endif
                supportsHorizontalPlaneDetection = true,
                supportsVerticalPlaneDetection = true,
                supportsArbitraryPlaneDetection = false,
                supportsBoundaryVertices = true
            };

            XRPlaneSubsystemDescriptor.Create(cinfo);
#endif
        }
    }
}