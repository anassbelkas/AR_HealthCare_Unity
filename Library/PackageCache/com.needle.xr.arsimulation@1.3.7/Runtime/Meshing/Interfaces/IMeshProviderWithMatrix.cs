
using UnityEngine;

namespace Needle.XR.ARSimulation
{
    public interface IMeshProviderWithMatrix : IMeshProvider
    {
        bool ApplyLocalToWorld { get; }
        Matrix4x4 LocalToWorld { get; }
    }
}