using System.Collections.Generic;
using System.Reflection;
using Needle.XR.ARSimulation.Extensions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// The ARSimulation implementation of the <c>XRAnchorSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationAnchorSubsystem : XRAnchorSubsystem
    {
#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        internal static void Clear() => ARSimulationProvider._anchors.Clear();

        private class ARSimulationProvider : Provider
        {
            internal static readonly Dictionary<TrackableId, XRAnchor> _anchors = new Dictionary<TrackableId, XRAnchor>();
            
            public override void Start()
            {
                
            }

            public override void Stop()
            {
            }

            public override void Destroy()
            {
            }

            public override TrackableChanges<XRAnchor> GetChanges(
                XRAnchor defaultAnchor,
                Allocator allocator)
            {
                for (var i = AddedList.Count - 1; i >= 0; i--)
                {
                    var anchor = AddedList[i];
                    if (_anchors.ContainsKey(anchor.trackableId))
                    {
                        AddedList.RemoveAt(i);
                        continue;
                    }

                    _anchors.Add(anchor.trackableId, anchor);
                }

                for (var i = UpdatedList.Count - 1; i >= 0; i--)
                {
                    var anchor = UpdatedList[i];
                    if (!_anchors.ContainsKey(anchor.trackableId))
                    {
                        UpdatedList.RemoveAt(i);
                    }
                }

                for (var i = RemovedList.Count - 1; i >= 0; i--)
                {
                    var id = RemovedList[i];
                    if (!_anchors.ContainsKey(id))
                    {
                        RemovedList.RemoveAt(i);
                        continue;
                    }

                    _anchors.Remove(id);
                }

                var ch = new TrackableChanges<XRAnchor>(AddedList.Count, UpdatedList.Count, RemovedList.Count, allocator, defaultAnchor);
                ch.added.CopyFrom(AddedList);
                ch.removed.CopyFrom(RemovedList);
                ch.updated.CopyFrom(UpdatedList);
                for (var i = 0; i < AddedList.Count; i++)
                {
                    var added = (object) AddedList[i];
                    AddedList[i] = XRAnchorHelper.SetTrackingState(added, TrackingState.Tracking);
                }

                AddedList.Clear();
                UpdatedList.Clear();
                RemovedList.Clear();
                return ch;
            }

            private static readonly List<XRAnchor> AddedList = new List<XRAnchor>();
            private static readonly List<XRAnchor> UpdatedList = new List<XRAnchor>();
            private static readonly List<TrackableId> RemovedList = new List<TrackableId>();

            public override bool TryAddAnchor(Pose pose, out XRAnchor anchor)
            {
                anchor = CreateAnchor(pose);
                return true;
            }

            /// <summary>
            /// attatching anchors is not supported yet
            /// </summary>
            /// <param name="trackableToAffix"></param>
            /// <param name="pose"></param>
            /// <param name="anchor"></param>
            /// <returns></returns>
            public override bool TryAttachAnchor(TrackableId trackableToAffix, Pose pose, out XRAnchor anchor)
            {
                // TODO: attaching anchors is not yet supported
                // Debug.Log("Attaching Anchors is not supported yet");
                anchor = CreateAnchor(pose);
                return true;
            }

            private static XRAnchor CreateAnchor(Pose pose)
            {
                var a = XRAnchorHelper.SetAnchor(new XRAnchor(), pose, TrackableIdHelper.GenerateRandomId(), TrackingState.None);
                AddedList.Add(a);
                return a;
            }

            public override bool TryRemoveAnchor(TrackableId anchorId)
            {
                if (!_anchors.ContainsKey(anchorId)) return false;
                RemovedList.Add(anchorId);
                return true;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            var info = new XRAnchorSubsystemDescriptor.Cinfo
            {
                id = "ARSimulation-Anchor",
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationAnchorSubsystem),
#else
                subsystemImplementationType = typeof(ARSimulationAnchorSubsystem),
#endif
                supportsTrackableAttachments = true
            };

            XRAnchorSubsystemDescriptor.Create(info);
#endif
        }
    }
}