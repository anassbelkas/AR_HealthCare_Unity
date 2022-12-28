using System;
using UnityEngine;
using UnityEngine.Rendering;

// ReSharper disable BuiltInTypeReferenceStyle

namespace Needle.XR.ARSimulation
{
    // [StructLayout(LayoutKind.Sequential)]
    public struct XRMeshDescriptor
    {
        public Vector3[] positions;
        public Vector3[] normals;
        // public Vector4[] tangents;
        // public Vector2[] uvs;
        // public Color32[] colors;
        public UInt16[] indices16;
        public uint vertexCount;
        public uint indexCount;
        public IndexFormat indexFormat;
        public MeshTopology topology;

        public static XRMeshDescriptor FromMesh(Mesh mesh)
        {
            var indices = mesh.GetIndices(0);
            UInt16[] ind = new UInt16[indices.Length];
            for (var i = 0; i < indices.Length; i++)
                ind[i] = (UInt16) indices[i];
            return new XRMeshDescriptor()
            {
                positions = mesh.vertices,
                indices16 = ind,// indices.Select(i => (UInt16)i).ToArray(),
                indexCount = (uint) indices.Length,
                vertexCount = (uint) mesh.vertexCount,
                topology = mesh.GetTopology(0),
                indexFormat = mesh.indexFormat,
                normals = mesh.normals,
            };
        }

        public XRMeshDescriptor Apply(Matrix4x4 localToWorld)
        {
            for (var index = 0; index < positions.Length; index++)
            {
                var pos = (Vector4) positions[index];
                pos.w = 1;
                positions[index] = localToWorld.MultiplyPoint(pos);
                
                var normal = normals[index];
                normals[index] = localToWorld.MultiplyVector(normal);
            }

            return this;
        }
    }
}