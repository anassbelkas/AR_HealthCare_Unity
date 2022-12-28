using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using Needle.XR.ARSimulation.Simulation;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// Use to register, update and remove <see cref="ITrackedImageProvider"/> implementations to ARFoundation. Used by <see cref="SimulatedARTrackedImage"/>
    /// </summary>
    public static class SimulatedTrackedImagesRegistry
    {
        internal static readonly List<ITrackedImageProvider> added = new List<ITrackedImageProvider>();
        internal static readonly List<ITrackedImageProvider> updated = new List<ITrackedImageProvider>();
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
        /// <param name="p">Instance of <see cref="ITrackedImageProvider"/> that adds data about tracked image.</param>
        public static void Register(ITrackedImageProvider p)
        {
            updated.RemoveAll(r => r.TrackableId == p.TrackableId);
            removed.RemoveAll(r => r == p.TrackableId);
            if (!added.Contains(p))
            {
                // Debug.Log("add " + p.TrackableId);
                added.Add(p);
            }
        }

        public static void Update(ITrackedImageProvider p)
        {
            removed.RemoveAll(r => r == p.TrackableId);
            if (IsRegistered(p))
            {
                added.RemoveAll(r => r == p);
                if (!updated.Contains(p))
                {
                    // Debug.Log("update " + p.TrackableId);
                    updated.Add(p);
                }
            }
            else Register(p);
        }

        public static void Remove(ITrackedImageProvider p)
        {
            added.RemoveAll(r => r.TrackableId == p.TrackableId);
            updated.RemoveAll(r => r.TrackableId == p.TrackableId);
            if (!removed.Contains(p.TrackableId))
            {
                // Debug.Log("Remove " + p.TrackableId);
                removed.Add(p.TrackableId);
            }
        }

        public static bool IsRegistered(ITrackedImageProvider p) => ARSimulationImageTrackingProvider.IsRegistered(p);
    }


    /// <summary>
    /// The ARSimulation implementation of the <c>XRImageTrackingSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationImageTrackingProvider : XRImageTrackingSubsystem
    {
        internal static bool Running { get; private set; }
        internal static void Clear()
        {
            foreach(var ct in CurrentlyTrackedImages) 
                SimulatedTrackedImagesRegistry.Remove(ct);
        }

#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        private class ARSimulationProvider : Provider
        {
            private XRReferenceImageLibrary _serializedLibrary;

            private ARSimulationImageDatabase library;

            public override RuntimeReferenceImageLibrary imageLibrary
            {
                set
                {
                    if (value == null) Running = false;
                    else Running = true;
                    library = (ARSimulationImageDatabase) value;
                }
            }

            public override RuntimeReferenceImageLibrary CreateRuntimeLibrary(XRReferenceImageLibrary serializedLibrary)
            {
                this._serializedLibrary = serializedLibrary;
                return imageLibrary = new ARSimulationImageDatabase(serializedLibrary);
            }

            private static ARSessionOrigin sessionOrigin => new Lazy<ARSessionOrigin>(Object.FindObjectOfType<ARSessionOrigin>).Value;


            /// <summary>
            /// find the image with the same texture in referenced images
            /// </summary>
            private bool TryFindInLibrary(ITrackedImageProvider prov, out XRTrackedImage tracked)
            {
                if (library != null && prov.Texture)
                {
                    // ReSharper disable once InlineOutVariableDeclaration
                    var guid = "";
#if UNITY_EDITOR
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prov.Texture, out guid, out long localId);
#endif
                    var textureGuid = new Guid(guid);
                    foreach (var img in library)
                    {
                        if (img.texture != prov.Texture && img.textureGuid != textureGuid) continue;
                        var worldPose = prov.Pose;
                        var session = sessionOrigin;
                        var cam = session.camera;
                        var sessionRelativePose = cam.transform.parent.InverseTransformPose(worldPose);
                        tracked = new XRTrackedImage(prov.TrackableId, img.guid, sessionRelativePose, prov.Size, prov.TrackingState, prov.NativePointer);
                        return true;
                    }
                }

                tracked = XRTrackedImage.defaultValue;
                return false;
            }

            public override TrackableChanges<XRTrackedImage> GetChanges(
                XRTrackedImage defaultTrackedImage,
                Allocator allocator)
            {
                if (!Running)
                {
                    // when stopping the subsystem remove all known planes
                    if (CurrentlyTrackedImages != null && CurrentlyTrackedImages.Count > 0)
                    {
                        var removeAll = new TrackableChanges<XRTrackedImage>(
                            0,
                            0,
                            CurrentlyTrackedImages.Count,
                            allocator);
                        removeAll.removed.CopyFrom(CurrentlyTrackedImages.Select(i => i.TrackableId).ToArray());
                        CurrentlyTrackedImages.Clear();
                        return removeAll;
                    }

                    return new TrackableChanges<XRTrackedImage>();
                }
                
                var added = SimulatedTrackedImagesRegistry.added;
                var updated = SimulatedTrackedImagesRegistry.updated;
                var removed = SimulatedTrackedImagesRegistry.removed;

                var addedTracked = new List<XRTrackedImage>();
                var updatedTracked = new List<XRTrackedImage>();

                for (var i = added.Count - 1; i >= 0; i--)
                {
                    var entry = added[i];
                    if (CurrentlyTrackedImages.Contains(entry))
                        continue;

                    if (TryFindInLibrary(entry, out var t))
                        addedTracked.Add(t);
                    else
                    {
                        Debug.LogWarning("Image " + (entry.Texture ? entry.Texture.name : entry.TrackableId.ToString()) + " not found in reference library", this._serializedLibrary);
                        continue;
                    }

                    CurrentlyTrackedImages.Add(entry);
                }

                for (var i = removed.Count - 1; i >= 0; i--)
                {
                    var entry = removed[i];
                    if (CurrentlyTrackedImages.All(t => t.TrackableId != entry))
                    {
                        removed.RemoveAt(i);
                        continue;
                    }

                    CurrentlyTrackedImages.RemoveAll(t => t.TrackableId == entry);
                }

                for (var i = updated.Count - 1; i >= 0; i--)
                {
                    var entry = updated[i];
                    if (!CurrentlyTrackedImages.Contains(entry))
                        continue;

                    for (var k = 0; k < CurrentlyTrackedImages.Count; k++)
                    {
                        var current = CurrentlyTrackedImages[k];
                        if (current.TrackableId == entry.TrackableId)
                        {
                            CurrentlyTrackedImages[k] = entry;
                        }
                    }

                    if (TryFindInLibrary(entry, out var t))
                        updatedTracked.Add(t);
                    else Debug.LogWarning("Image " + (entry.Texture ? entry.Texture.name : entry.TrackableId.ToString()) + " not found in reference library", this._serializedLibrary);
                }

                var changes = new TrackableChanges<XRTrackedImage>(addedTracked.Count, updatedTracked.Count, removed.Count, allocator);
                changes.added.CopyFrom(addedTracked);
                changes.updated.CopyFrom(updatedTracked);
                changes.removed.CopyFrom(removed);

                SimulatedTrackedImagesRegistry.Clear();

                return changes;
            }

#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            public override void Start()
            {
                Running = true;
            }

            public override void Stop()
            {
                Running = false;
            }
#endif

            public override void Destroy()
            {
                Running = false;
            }


#if UNITY_AR_FOUNDATION_4_OR_NEWER
            /// <summary>
            /// This must be implemented if supportsMovingImages is true.
            /// ARCore doesn't let you set the max number -- it just tracks everything
            /// </summary>
            public override int requestedMaxNumberOfMovingImages
            {
                get => m_RequestedMaxNumberOfMovingImages;
                set => m_RequestedMaxNumberOfMovingImages = value;
            }

            private int m_RequestedMaxNumberOfMovingImages;

            public override int currentMaxNumberOfMovingImages => Mathf.Max(m_RequestedMaxNumberOfMovingImages, CurrentlyTrackedImages.Count);
#else
            private int _maxNumberOfMovingImages;
            public override int maxNumberOfMovingImages
            {
                set => _maxNumberOfMovingImages = value;
            }
#endif
        }

        internal static ARSimulationImageTrackingProvider Instance { get; private set; }

        protected override void OnStart()
        {
            Instance = this;
            if (imageLibrary == null)
            {
                imageLibrary = CreateRuntimeLibrary(null);
                AddImageLibrariesFromAssets();
            }
            base.OnStart();
        }

        internal void AddImageLibrariesFromAssets()
        {
#if UNITY_EDITOR
            if (imageLibrary is ARSimulationImageDatabase simulatedLibrary)
            {
                var manager = Object.FindObjectOfType<ARTrackedImageManager>();
                // if (!manager) return;

                // TODO: for standalone support we need to cache these at build time
                var libraryGuids = AssetDatabase.FindAssets("t:" + nameof(XRReferenceImageLibrary));
                if (libraryGuids != null)
                {
                    foreach (var guid in libraryGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrEmpty(path)) continue;
                        var lib = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(path);
                        if (!manager || (path.Contains("Resources") || (manager.referenceLibrary != null && ReferenceEquals(manager.referenceLibrary, lib))))
                            simulatedLibrary.Add(lib);
                    }
                }
            }
#endif
        }

        private static readonly List<ITrackedImageProvider> CurrentlyTrackedImages = new List<ITrackedImageProvider>();
        internal static bool IsRegistered(ITrackedImageProvider p) => CurrentlyTrackedImages.Contains(p);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
#if UNITY_EDITOR || (!PLATFORM_ANDROID && !UNITY_IOS)
            XRImageTrackingSubsystemDescriptor.Create(new XRImageTrackingSubsystemDescriptor.Cinfo
            {
                id = "ARSimulation-ImageTracking",
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationImageTrackingProvider),
#else
                subsystemImplementationType = typeof(ARSimulationImageTrackingProvider),
#endif
                supportsMovingImages = true,
                supportsMutableLibrary = true
            });
#endif
        }
    }
}