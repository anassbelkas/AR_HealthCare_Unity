using UnityEngine;

namespace Needle.XR.ARSimulation.Interfaces
{
    public interface IInputDevice
    {
        Vector3 Forward { get; }
        void SetTargetPosition(Vector3 position);
        void SetTargetRotation(Quaternion rot);
    }
}