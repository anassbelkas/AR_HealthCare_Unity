using UnityEngine;

namespace Needle.XR.ARSimulation
{
    public interface IMeshProvider
    {
        Mesh Mesh { get; }
        /// <summary>
        /// Necessary for managing lifetime of a provided mesh, should not change and be unique per instance
        /// </summary>
        ulong Id { get; }
    }
}