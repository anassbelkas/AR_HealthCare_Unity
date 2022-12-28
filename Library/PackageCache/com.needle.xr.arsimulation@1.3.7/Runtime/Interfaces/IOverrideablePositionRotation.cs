using UnityEngine;

namespace Needle.XR.ARSimulation.Interfaces
{
    /// <summary>
    /// Used to set position and rotation of input devices during startup phase when AR camera is not at identity
    /// </summary>
    public interface IOverrideablePositionRotation
    {
        void SetPositionAndRotation(Vector3 position, Quaternion rotation);
    }
}