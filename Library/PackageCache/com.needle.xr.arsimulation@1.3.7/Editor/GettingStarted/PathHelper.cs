using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Needle.XR.ARSimulation.Installation
{
    internal class PathHelper
    {
        public static string FindScriptPath()
        {
            var frame = new StackFrame(2, true);
            return frame.GetFileName();
        }

        public static string RelativeToCaller(params string[] path)
        {
            var scriptPath = FindScriptPath();
            if (string.IsNullOrEmpty(scriptPath)) return null;
            var fullPath = Path.GetDirectoryName(scriptPath);  
            foreach (var segment in path) fullPath += "/" + segment;
            return fullPath;
        }

        public static T RelativeToCaller<T>(params string[] path) where T : UnityEngine.Object
        {
            var fullPath = RelativeToCaller(path);
            return string.IsNullOrEmpty(fullPath) ? default : AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }
    }
}