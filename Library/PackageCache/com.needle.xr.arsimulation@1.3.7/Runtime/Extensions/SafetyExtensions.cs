using System;
using UnityEngine;

namespace Needle.XR.ARSimulation.Extensions
{
    internal static class SafetyExtensions
    {
        internal static bool HasInfinity(this Vector2 vec)
        {
            for (var i = 0; i < 2; i++)
            {
                if (float.IsInfinity(vec[i]))
                    return true;
            }

            return false;
        }
        
        internal static bool HasNaN(this Vector2 vec)
        {
            for (var i = 0; i < 2; i++)
            {
                if (float.IsNaN(vec[i]))
                    return true;
            }
            return false;
        }
        
        internal static bool HasNaN(this Vector3 vec)
        {
            for (var i = 0; i < 3; i++)
            {
                if (float.IsNaN(vec[i]))
                    return true;
            }
            return false;
        }

        internal static bool HasNan(this Quaternion rot)
        {
            for (var i = 0; i < 4; i++)
            {
                if (float.IsNaN(rot[i]))
                    return true;
            }
            return false;
        }
    }
}