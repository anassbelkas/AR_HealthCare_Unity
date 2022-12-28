using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    public static class TrackableIdHelper
    {
        public static TrackableId GenerateRandomId() => new TrackableId((ulong) (Random.value * ulong.MaxValue), (ulong) (Random.value * ulong.MaxValue));
        
        // not sure if this is THE BEST way to generate unique ids (create a guid ?)
        public static TrackableId GetTrackableId(this Object obj) => new TrackableId((ulong)obj.GetHashCode(), (ulong)obj.GetInstanceID());
    }
}