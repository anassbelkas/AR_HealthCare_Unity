using System;
using System.Collections.Generic;
using Needle.XR.ARSimulation.Simulation;
using Needle.XR.ARSimulation.Simulation.Geometry;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    public static class NativeArrayExtensions
    {
        public static NativeArray<TTarget> CopyFrom<TSource, TTarget>(this NativeArray<TTarget> arr, List<TSource> content, Func<TSource, TTarget> copy, bool matchSize = false) where TTarget : struct
        {
            if (!arr.IsCreated) throw new Exception("Native Array is not created");
            if (content == null) return arr;
            if (matchSize)
            {
                if (arr.Length != content.Count)
                {
                    var temp = new NativeArray<TTarget>(content.Count, Allocator.Temp);
                    for (var k = 0; k < arr.Length; k++)
                        temp[k] = arr[k];
                    arr = temp;
                }
            }
            for (var i = 0; i < arr.Length && i < content.Count; i++) arr[i] = copy(content[i]);
            return arr;
        }
        
        public static NativeArray<T> CopyFrom<T>(this NativeArray<T> arr, List<T> content, bool matchSize = false) where T : struct
        {
            if (!arr.IsCreated) throw new Exception("Native Array is not created");
            if (content == null) return arr;
            if (matchSize)
            {
                if (arr.Length != content.Count)
                {
                    var temp = new NativeArray<T>(content.Count, Allocator.Temp);
                    for (var k = 0; k < arr.Length; k++)
                        temp[k] = arr[k];
                    arr = temp;
                }
            }
            for (var i = 0; i < arr.Length && i < content.Count; i++) arr[i] = content[i];
            return arr;
        }

        internal static NativeArray<BoundedPlane> CopyFrom(this NativeArray<BoundedPlane> arr, IEnumerable<PlaneData> enumerable)
        {
            var index = 0;
            foreach (var entry in enumerable)
            {
                arr[index] = entry.Plane;
                ++index;
            }

            return arr;
        }
        
    }
}