#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Needle.XR.ARSimulation
{
    public static class SimulatedTrackedObjectsRegistry
    {
        internal static readonly List<ITrackedObjectProvider> added = new List<ITrackedObjectProvider>();
        internal static readonly List<ITrackedObjectProvider> updated = new List<ITrackedObjectProvider>();
        internal static readonly List<TrackableId> removed = new List<TrackableId>();

        internal static void Clear()
        {
            added.Clear();
            updated.Clear();
            removed.Clear();
        }

        /// <summary>
        /// Register a new instance of a <paramref name="p"/>
        /// </summary>
        /// <param name="p">Instance of <see cref="ITrackedObjectProvider"/> that adds data about tracked image.</param>
        public static void Register(ITrackedObjectProvider p)
        {
            updated.RemoveAll(r => r == p);
            removed.RemoveAll(r => r == p.TrackableId);
            if (!added.Contains(p))
                added.Add(p);
        }

        public static void Update(ITrackedObjectProvider p)
        {
            removed.RemoveAll(r => r == p.TrackableId);
            if (IsRegistered(p))
            {
                added.RemoveAll(r => r == p);
                if (!updated.Contains(p))
                    updated.Add(p);
            }
            else Register(p);
        }

        public static void Remove(ITrackedObjectProvider p)
        {
            added.RemoveAll(r => r == p);
            updated.RemoveAll(r => r == p);
            if (!removed.Contains(p.TrackableId))
                removed.Add(p.TrackableId);
        }

        public static bool IsRegistered(ITrackedObjectProvider p) => ARSimulationObjectTrackingSubsystem.IsRegistered(p);
    }

    /// <summary>
    /// An ARKit-specific implementation of the <c>XRObjectTrackingSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationObjectTrackingSubsystem : XRObjectTrackingSubsystem
    {
        internal static bool Running { get; private set; }
        internal static void Clear() => CurrentlyTrackedObjects.Clear();
        
#if !UNITY_2020_2_OR_NEWER
        /// <summary>
        /// Creates the ARKit-specific implementation which will service the `XRObjectTrackingSubsystem`.
        /// </summary>
        /// <returns>A new instance of the `Provider` specific to ARKit.</returns>
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        private class ARSimulationProvider : Provider
        {
#if UNITY_2020_2_OR_NEWER
            /// <summary>
            /// Invoked when <c>Start</c> is called on the subsystem. This method is only called if the subsystem was not previously running.
            /// </summary>
            public override void Start()
            {
                Running = true;
            }

            /// <summary>
            /// Invoked when <c>Stop</c> is called on the subsystem. This method is only called if the subsystem was previously running.
            /// </summary>
            public override void Stop()
            {
                Running = false;
            }
#endif

            public override void Destroy()
            {
                Running = false;
            }

            private XRReferenceObjectLibrary objectLibrary;

            public override XRReferenceObjectLibrary library
            {
                set
                {
                    if (value == null)
                    {
                        Running = false;
                    }
                    else
                    {
                        Running = true;
                        objectLibrary = value;
                        base.library = value;
                    }
                }
            }
            
            private static ARSessionOrigin sessionOrigin => new Lazy<ARSessionOrigin>(UnityEngine.Object.FindObjectOfType<ARSessionOrigin>).Value;

            /// <summary>
            /// find the image with the same texture in referenced images
            /// </summary>
            private bool TryFindInLibrary(ITrackedObjectProvider prov, out XRTrackedObject tracked)
            {
                if (objectLibrary != null && prov.Entry != Guid.Empty)
                {
                     foreach (var obj in objectLibrary)
                     {
                         if (obj.guid != prov.Entry) continue;
                         var worldPose = prov.Pose;
                         var session = sessionOrigin;
                         var cam = session.camera;
                         var sessionRelativePose = cam.transform.parent.InverseTransformPose(worldPose);
                         tracked = new XRTrackedObject(prov.TrackableId, sessionRelativePose, prov.TrackingState, prov.NativePtr, obj.guid);
                         return true;
                     }
                }

                tracked = XRTrackedObject.defaultValue;
                return false;
            }

            public override TrackableChanges<XRTrackedObject> GetChanges(
                XRTrackedObject defaultTrackedObject,
                Allocator allocator)
            {
                if (!Running)
                {
                    // when stopping the subsystem remove all known planes
                    if (CurrentlyTrackedObjects != null && CurrentlyTrackedObjects.Count > 0)
                    {
                        var removeAll = new TrackableChanges<XRTrackedObject>(
                            0,
                            0,
                            CurrentlyTrackedObjects.Count,
                            allocator);
                        removeAll.removed.CopyFrom(CurrentlyTrackedObjects.Select(i => i.TrackableId).ToArray());
                        CurrentlyTrackedObjects.Clear();
                        return removeAll;
                    }

                    return new TrackableChanges<XRTrackedObject>();
                }
                
                var added = SimulatedTrackedObjectsRegistry.added;
                var updated = SimulatedTrackedObjectsRegistry.updated;
                var removed = SimulatedTrackedObjectsRegistry.removed;

                var addedTracked = new List<XRTrackedObject>();
                var updatedTracked = new List<XRTrackedObject>();

                for (var i = added.Count - 1; i >= 0; i--)
                {
                    var entry = added[i];
                    if (CurrentlyTrackedObjects.Contains(entry))
                        continue;

                    if (TryFindInLibrary(entry, out var t))
                        addedTracked.Add(t);
                    else
                    {
                        Debug.LogWarning("Image " + entry.TrackableId + " not found in reference library", this.objectLibrary);
                        continue;
                    }

                    CurrentlyTrackedObjects.Add(entry);
                }

                for (var i = removed.Count - 1; i >= 0; i--)
                {
                    var entry = removed[i];
                    if (CurrentlyTrackedObjects.All(t => t.TrackableId != entry))
                    {
                        removed.RemoveAt(i);
                        continue;
                    }

                    CurrentlyTrackedObjects.RemoveAll(t => t.TrackableId == entry);
                }

                for (var i = updated.Count - 1; i >= 0; i--)
                {
                    var entry = updated[i];
                    if (!CurrentlyTrackedObjects.Contains(entry))
                        continue;

                    for (var k = 0; k < CurrentlyTrackedObjects.Count; k++)
                    {
                        var current = CurrentlyTrackedObjects[k];
                        if (current.TrackableId == entry.TrackableId)
                        {
                            CurrentlyTrackedObjects[k] = entry;
                        }
                    }

                    if (TryFindInLibrary(entry, out var t))
                        updatedTracked.Add(t);
                    else Debug.LogWarning("Object " + entry.TrackableId + " not found in reference library", this.objectLibrary);
                }

                var changes = new TrackableChanges<XRTrackedObject>(addedTracked.Count, updatedTracked.Count, removed.Count, allocator);
                changes.added.CopyFrom(addedTracked);
                changes.updated.CopyFrom(updatedTracked);
                changes.removed.CopyFrom(removed);

                SimulatedTrackedObjectsRegistry.Clear();

                return changes;
            }
        }

        private static readonly List<ITrackedObjectProvider> CurrentlyTrackedObjects = new List<ITrackedObjectProvider>();
        internal static bool IsRegistered(ITrackedObjectProvider p) => CurrentlyTrackedObjects.Contains(p);

        /// <summary>
        /// This method is run on startup of the app to register this provider with XR Subsystem Manager
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
            var capabilities = new XRObjectTrackingSubsystemDescriptor.Capabilities
            {
            };

#if UNITY_2020_2_OR_NEWER
            Register<ARSimulationObjectTrackingSubsystem.ARSimulationProvider, ARSimulationObjectTrackingSubsystem>("ARSimulation-ObjectTracking", capabilities);
#else
            Register<ARSimulationObjectTrackingSubsystem>("ARSimulation-ObjectTracking", capabilities);
#endif
        }

        internal static void EnsureLibrary(XRObjectTrackingSubsystem subsystem)
        {
            if (subsystem == null) return;
            if(!subsystem.library) subsystem.library = ScriptableObject.CreateInstance<XRReferenceObjectLibrary>();
        }
    }
}
#endif