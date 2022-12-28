using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Interfaces
{
    /// <summary>
    /// Implement interface for registering custom AR point clouds
    /// </summary>
    public interface IPointCloudProvider
    {
        TrackableId Id { get; }
        TrackingState State { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        List<Vector3> Points { get; }
    }
}