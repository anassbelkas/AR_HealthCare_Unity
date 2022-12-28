using System;
using System.Collections.Generic;
using UnityEngine;

namespace Needle.XR.ARSimulation.Extensions
{
    public static class MeshHelper
    {
        public class ClockWiseComparer : IComparer<Vector2>
        {
            public int Compare(Vector2 v1, Vector2 v2)
            {
                return Mathf.Atan2(v1.x, v1.y).CompareTo(Mathf.Atan2(v2.x, v2.y));
            }
        }
        
        private static readonly ClockWiseComparer ClockWiseComparerInstance = new ClockWiseComparer();

        public static void OrderClockwise(this Vector2[] list)
        {
            Array.Sort(list, ClockWiseComparerInstance);
        }
        public static void OrderClockwise(this List<Vector2> list)
        {
            list.Sort(ClockWiseComparerInstance);
        }
    }
  
}