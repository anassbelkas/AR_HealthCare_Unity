using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Interfaces
{
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
    public interface ITrackedObjectProvider
    {
        TrackableId TrackableId { get; }
        Pose Pose { get; }
        TrackingState TrackingState { get; }
        IntPtr NativePtr { get; }
        Guid Entry { get; }
    }
#endif
}