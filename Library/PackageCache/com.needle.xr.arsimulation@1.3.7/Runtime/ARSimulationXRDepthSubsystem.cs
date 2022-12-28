using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation
{
    internal readonly struct ManagedPointCloud
    {
        private static ulong uniqueIdentifiers;

        public TrackableId Id => PointCloud.trackableId;
        public readonly XRPointCloud PointCloud;
        public readonly List<Vector3> Positions;
        public readonly List<ulong> Identifiers;
        public readonly List<float> ConfidenceValues;

        public ManagedPointCloud(XRPointCloud pointCloud, List<Vector3> points)
        {
            var positions = new List<Vector3>(points.Count);
            this.Identifiers = new List<ulong>(points.Count);
            this.ConfidenceValues = new List<float>(points.Count);

            positions.AddRange(points);
            for (var index = 0; index < points.Count; index++)
            {
                this.Identifiers.Add(uniqueIdentifiers);
                uniqueIdentifiers += 1;
            }

            this.PointCloud = pointCloud;
            this.Positions = positions;
        }
    }

    /// <summary>
    /// Use to register custom point clouds or implementation of <typeparam>
    ///     <name>IPointCloudProvider</name>
    /// </typeparam>
    /// </summary>
    public static class SimulatedARPointCloudRegistry
    {
        internal static readonly List<ManagedPointCloud> AddedList = new List<ManagedPointCloud>();
        internal static readonly List<TrackableId> RemovedList = new List<TrackableId>();
        internal static readonly List<ManagedPointCloud> UpdatedList = new List<ManagedPointCloud>();

        public static void Register(TrackableId id, Vector3 position, Quaternion rotation, List<Vector3> points)
        {
            if (!ARSimulationXRDepthSubsystem.Running) return;
            AddedList.Add(new ManagedPointCloud(
                new XRPointCloud(id, new Pose(position, rotation), TrackingState.Tracking, IntPtr.Zero),
                points
            ));
            UpdatedList.RemoveAll(a => a.Id == id);
            RemovedList.RemoveAll(i => i == id);
        }

        public static void Register(IPointCloudProvider provider)
        {
            if (!ARSimulationXRDepthSubsystem.Running) return;
            Register(provider.Id, provider.Position, provider.Rotation, provider.Points);
        }

        private static void Update(TrackableId id, Vector3 position, Quaternion rotation, List<Vector3> points,
            Func<Pose, Pose> transformPoseInSessionSpace = null)
        {
            if (!ARSimulationXRDepthSubsystem.Running) return;
            var p = new Pose(position, rotation);
            if (transformPoseInSessionSpace != null)
                p = transformPoseInSessionSpace(p);

            UpdatedList.Add(new ManagedPointCloud(
                new XRPointCloud(id, p, TrackingState.Tracking, IntPtr.Zero),
                points
            ));
            AddedList.RemoveAll(a => a.Id == id);
            RemovedList.RemoveAll(i => i == id);
        }

        public static void Update(IPointCloudProvider provider, Func<Pose, Pose> transformPoseInSessionSpace = null)
        {
            if (!ARSimulationXRDepthSubsystem.Running) return;
            Update(provider.Id, provider.Position, provider.Rotation, provider.Points, transformPoseInSessionSpace);
        }

        public static void Unregister(TrackableId id)
        {
            if (!ARSimulationXRDepthSubsystem.Running) return;
            RemovedList.Add(id);
            AddedList.RemoveAll(a => a.Id == id);
            UpdatedList.RemoveAll(a => a.Id == id);
        }

        public static void Unregister(IPointCloudProvider provider)
        {
            if (!ARSimulationXRDepthSubsystem.Running) return;
            Unregister(provider.Id);
        }
    }

    /// <summary>
    /// The ARDesktop implementation of the <c>XRDepthSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationXRDepthSubsystem : XRDepthSubsystem
    {
        internal static bool Running { get; private set; }
        internal static int TotalPoints { get; private set; }

        internal static IEnumerable<ManagedPointCloud> EnumeratePointClouds()
        {
            if (TotalPoints <= 0) yield break;
            var data = ARSimulationProvider.m_data;
            if (data == null) yield break;
            foreach (var kvp in data)
            {
                yield return kvp.Value;
            }
        }

#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif
        
        private class ARSimulationProvider : Provider
        {
            internal static readonly Dictionary<TrackableId, ManagedPointCloud> m_data = new Dictionary<TrackableId, ManagedPointCloud>();

            public override TrackableChanges<XRPointCloud> GetChanges(
                XRPointCloud defaultPointCloud,
                Allocator allocator)
            {
                if (!Running)
                {
                    // when stopping the subsystem remove all known planes
                    if (m_data != null && m_data.Count > 0)
                    {
                        var removeAll = new TrackableChanges<XRPointCloud>(
                            0,
                            0,
                            m_data.Count,
                            allocator);
                        removeAll.removed.CopyFrom(m_data.Keys.ToArray());
                        m_data.Clear();
                        TotalPoints = m_data.Sum(e => e.Value.Positions.Count);
                        return removeAll;
                    }

                    return new TrackableChanges<XRPointCloud>();
                }
                
                var added = SimulatedARPointCloudRegistry.AddedList;
                var updated = SimulatedARPointCloudRegistry.UpdatedList;
                var removed = SimulatedARPointCloudRegistry.RemovedList;

                for (var i = updated.Count - 1; i >= 0; i--)
                {
                    var entry = updated[i];
                    if (!m_data.ContainsKey(entry.Id))
                    {
                        updated.RemoveAt(i);
                        continue;
                    }

                    m_data[entry.Id] = entry;
                }

                for (var i = added.Count - 1; i >= 0; i--)
                {
                    var entry = added[i];
                    if (m_data.ContainsKey(entry.Id))
                    {
                        added.RemoveAt(i);
                        continue;
                    }

                    m_data.Add(entry.Id, entry);
                }

                for (var i = removed.Count - 1; i >= 0; i--)
                {
                    var entry = removed[i];
                    if (!m_data.ContainsKey(entry))
                    {
                        removed.RemoveAt(i);
                        continue;
                    }

                    m_data.Remove(entry);
                }

                var changed = new TrackableChanges<XRPointCloud>(added.Count, updated.Count, removed.Count, allocator, XRPointCloud.defaultValue);
                changed.added.CopyFrom(added, s => s.PointCloud);
                changed.updated.CopyFrom(updated, s => s.PointCloud);
                changed.removed.CopyFrom(removed);
                
                if(changed.added.Length > 0 || changed.updated.Length > 0 || changed.removed.Length > 0) 
                    TotalPoints = m_data.Sum(e => e.Value.Positions.Count);

                added.Clear();
                updated.Clear();
                removed.Clear();

                return changed;
            }

            public override XRPointCloudData GetPointCloudData(
                TrackableId trackableId,
                Allocator allocator)
            {
                if (!m_data.ContainsKey(trackableId))
                    return new XRPointCloudData();

                var entry = m_data[trackableId];

                return new XRPointCloudData()
                {
                    positions = new NativeArray<Vector3>(entry.Positions.Count, allocator).CopyFrom(entry.Positions),
                    identifiers = new NativeArray<ulong>(entry.Identifiers.Count, allocator).CopyFrom(entry.Identifiers),
                    confidenceValues = new NativeArray<float>(entry.ConfidenceValues.Count, allocator).CopyFrom(entry.ConfidenceValues),
                };
            }

            public override void Destroy()
            {
                Running = false;
            }

            /// <summary>
            /// Starts the DepthSubsystem provider to begin providing face data via the callback delegates
            /// </summary>
            public override void Start()
            {
                Running = true;
            }

            /// <summary>
            /// Stops the DepthSubsystem provider from providing face data
            /// </summary>
            public override void Stop()
            {
                Running = false;
            }
        }
        // this method is run on startup of the app to register this provider with XR Subsystem Manager
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            var descriptorParams = new XRDepthSubsystemDescriptor.Cinfo
            {
                id = "ARSimulation-Depth",
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationXRDepthSubsystem),
#else
                implementationType = typeof(ARSimulationXRDepthSubsystem),
#endif
                supportsFeaturePoints = true,
                supportsUniqueIds = true,
                supportsConfidence = true
            };

            XRDepthSubsystemDescriptor.RegisterDescriptor(descriptorParams);
#endif
        }
    }
}