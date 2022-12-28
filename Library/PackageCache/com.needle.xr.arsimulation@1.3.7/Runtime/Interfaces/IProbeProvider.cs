using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Interfaces
{
    /// <summary>
    /// Implement interface for registering custom AR environment probes
    /// </summary>
    public interface IProbeProvider
    {
        TrackableId TrackableId { get; }
        Cubemap Texture { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        Vector3 Scale { get; }
        Vector3 Size { get; }
        TrackingState TrackingState { get; }
        IntPtr NativePointer { get; }
    }
}