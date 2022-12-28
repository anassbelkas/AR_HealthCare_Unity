using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Interfaces
{
    /// <summary>
    /// Implement interface for registering custom AR tracked images
    /// </summary>
    public interface ITrackedImageProvider
    {
        TrackableId TrackableId { get; }
        Texture2D Texture { get; }
        Pose Pose { get; }
        Vector2 Size { get; }
        TrackingState TrackingState { get; }
        IntPtr NativePointer { get; }
    }
}