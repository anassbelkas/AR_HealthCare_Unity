using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

namespace Needle.XR.ARSimulation
{
    internal static class Native
    {
        [DllImport("ARSimulationPlugin", EntryPoint = "GetBoundingVolume")]
        public static extern void GetBoundingVolume(ref Vector3 origin, ref Vector3 bounds);
        
        [DllImport("ARSimulationPlugin", EntryPoint = "GetDensity")]
        public static extern void GetDensity(ref float meshDensity);
        
        [DllImport("ARSimulationPlugin", EntryPoint = "GetMeshCount")]
        public static extern int GetMeshCount();

        [DllImport("ARSimulationPlugin", EntryPoint = "AddOrUpdateMesh", CallingConvention = CallingConvention.StdCall)]
        public static extern void AddOrUpdateMesh(MeshInfo info, uint positionCount, Vector3[] positions, uint indexCount, UInt16[] indices, Vector3[] normals);
        
        [DllImport("ARSimulationPlugin", EntryPoint = "RemoveMesh", CallingConvention = CallingConvention.StdCall)]
        public static extern bool RemoveMesh(MeshInfo info);

        [DllImport("ARSimulationPlugin", EntryPoint = "ClearData", CallingConvention = CallingConvention.StdCall)]
        public static extern void ClearData();

        [DllImport("ARSimulationPlugin", EntryPoint = "SetMeshesUnchanged", CallingConvention = CallingConvention.StdCall)]
        public static extern void SetMeshesUnchanged();
    }
}