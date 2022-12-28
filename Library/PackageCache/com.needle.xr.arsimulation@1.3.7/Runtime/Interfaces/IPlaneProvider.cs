using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Interfaces
{
    /// <summary>
    /// Implement interface for registering custom AR planes
    /// </summary>
    public interface IPlaneProvider
    {
        TrackableId GetId();
        /// <summary>
        /// Get the AR <see cref="BoundedPlane"/> in session space for adding to AR Foundation
        /// </summary>
        /// <returns>The <see cref="BoundedPlane"/> in session space</returns>
        BoundedPlane GetPlane();
        IReadOnlyList<Vector2> Bounds { get; }
        /// <summary>
        /// Indicates whether a new plane overlapping existing planes are allowed to be merged by ARDesktop
        /// </summary>
        bool AllowMerging { get; }
    }
}