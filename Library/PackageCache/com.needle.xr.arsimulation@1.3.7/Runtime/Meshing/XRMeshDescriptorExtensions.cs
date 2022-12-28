using UnityEngine;

namespace Needle.XR.ARSimulation
{
    public static class XRMeshDescriptorExtensions
    {
        public static XRMeshDescriptor ToDescriptor(this Mesh mesh)
        {
            return XRMeshDescriptor.FromMesh(mesh);
        }
    }
}