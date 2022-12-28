using System.Collections.Generic;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation
{
    public static class SimulatedARProbesRegistry
    {
        internal static readonly List<XREnvironmentProbe> AddedList = new List<XREnvironmentProbe>();
        internal static readonly List<XREnvironmentProbe> UpdatedList = new List<XREnvironmentProbe>();
        internal static readonly List<TrackableId> RemovedList = new List<TrackableId>();

        public static void Add(IProbeProvider probe)
        {
            if (XREnvironmentProbeHelper.ToProbe(probe, out var p))
                AddedList.Add(p);
        }

        public static void Update(IProbeProvider probe)
        {
            if (XREnvironmentProbeHelper.ToProbe(probe, out var p))
                UpdatedList.Add(p);
        }

        public static void Remove(IProbeProvider probe)
        {
            RemovedList.Add(probe.TrackableId);
        }
    }

    /// <summary>
    /// This subsystem provides implementing functionality for the <c>XREnvironmentProbeSubsystem</c> class.
    /// </summary>
    [Preserve]
    internal class ARSimulationEnvironmentProbeSubsystem : XREnvironmentProbeSubsystem
    {
        
#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        internal static void Clear() => ARSimulationProvider._registered.Clear();
        
        /// <summary>
        /// Create and register the environment probe subsystem descriptor to advertise a providing implementation for
        /// environment probe functionality.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            const string subsystemId = "ARSimulation-EnvironmentProbe";
            var environmentProbeSubsystemInfo = new XREnvironmentProbeSubsystemCinfo()
            {
                id = subsystemId,
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationEnvironmentProbeSubsystem.ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationEnvironmentProbeSubsystem),
#else
                implementationType = typeof(ARSimulationEnvironmentProbeSubsystem),
#endif
                supportsManualPlacement = true,
                supportsRemovalOfManual = true,
                supportsAutomaticPlacement = false,
                supportsRemovalOfAutomatic = false,
                supportsEnvironmentTexture = false,
                supportsEnvironmentTextureHDR = true,
            };

            if (!XREnvironmentProbeSubsystem.Register(environmentProbeSubsystemInfo))
            {
                // Debug.LogErrorFormat("Cannot register the {0} subsystem", subsystemId);
            }
#endif
        }

        private class ARSimulationProvider : Provider
        {
            internal static readonly Dictionary<TrackableId, XREnvironmentProbe> _registered = new Dictionary<TrackableId, XREnvironmentProbe>();
            
            /// <summary>
            /// Starts the environment probe subsystem by enabling the HDR Environmental Light Estimation.
            /// </summary>
            public override void Start()
            {
            }

            /// <summary>
            /// Stops the environment probe subsystem by disabling the environment probe state.
            /// </summary>
            public override void Stop()
            {
            }

            /// <summary>
            /// Destroy the environment probe subsystem by first ensuring that the subsystem has been stopped and then
            /// destroying the provider.
            /// </summary>
            public override void Destroy()
            {
            }


            public override TrackableChanges<XREnvironmentProbe> GetChanges(XREnvironmentProbe defaultEnvironmentProbe,
                Allocator allocator)
            {
                var added = SimulatedARProbesRegistry.AddedList;
                var updated = SimulatedARProbesRegistry.UpdatedList;
                var removed = SimulatedARProbesRegistry.RemovedList;

                for (var i = added.Count - 1; i >= 0; i--)
                {
                    var entry = added[i];
                    if (_registered.ContainsKey(entry.trackableId))
                    {
                        added.RemoveAt(i);
                        continue;
                    }

                    _registered.Add(entry.trackableId, entry);
                }

                for (var i = updated.Count - 1; i >= 0; i--)
                {
                    var entry = updated[i];
                    if (!_registered.ContainsKey(entry.trackableId))
                    {
                        updated.RemoveAt(i);
                        continue;
                    }

                    _registered[entry.trackableId] = entry;
                }

                for (var i = removed.Count - 1; i >= 0; i--)
                {
                    var entry = removed[i];
                    if (!_registered.ContainsKey(entry))
                    {
                        updated.RemoveAt(i);
                        continue;
                    }

                    _registered.Remove(entry);
                }

                var changes = new TrackableChanges<XREnvironmentProbe>(added.Count, updated.Count, removed.Count, allocator, XREnvironmentProbe.defaultValue);
                changes.added.CopyFrom(added);
                changes.updated.CopyFrom(updated);
                changes.removed.CopyFrom(removed);
                added.Clear();
                updated.Clear();
                removed.Clear();

                // NativeApi.UnityARDesktop_EnvironmentProbeProvider_GetChanges(out int numAdded, out int numUpdated, out int numRemoved, ref probe);
                //
                // // There is only ever one probe currently so allocating using it as the default template is safe.
                // var changes = new TrackableChanges<XREnvironmentProbe>(numAdded, numUpdated, numRemoved, allocator, probe);
                //
                // if (numRemoved > 0)
                // {
                //     var nativeRemovedArray = changes.removed;
                //     nativeRemovedArray[0] = probe.trackableId;
                // }

                return changes;
            }
        }
    }
}