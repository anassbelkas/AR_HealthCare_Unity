

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    /// <summary>
    /// Helper to set private fields in <see cref="XRAnchor"/>
    /// </summary>
    public static class XRAnchorHelper
    {
        private static Type type;
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();
        private const BindingFlags flags = BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic;

        private static void SetField(object instance, string name, object value)
        {
            if (type == null) type = typeof(XRAnchor);
            if (Fields.ContainsKey(name) == false)
                Fields[name] = type.GetField(name, flags);
            Fields[name].SetValue(instance, value);
        }
        
        // we need to box the struct to set fields on its instance
        public static XRAnchor SetAnchor(object anchor, Pose pose, TrackableId id, TrackingState state)
        {
            SetField(anchor, "m_Pose", pose);
            SetField(anchor, "m_Id", id);
            anchor = SetTrackingState(anchor, state);
            return (XRAnchor) anchor;
        }

        public static XRAnchor SetTrackingState(object anchor, TrackingState state)
        {
            SetField(anchor, "m_TrackingState", state);
            return (XRAnchor) anchor;
        }
    }
}