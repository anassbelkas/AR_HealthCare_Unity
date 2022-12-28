using System;
using System.Collections.Generic;
using System.Reflection;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    public static class XREnvironmentProbeHelper
    {
        public static bool ToProbe(IProbeProvider provider, out XREnvironmentProbe probe, int propertyNameId = 0)
        {
            // object to box it for setting values via reflection
            object instance = XREnvironmentProbe.defaultValue;
            XREnvironmentProbe_SetTrackableId(instance, provider.TrackableId);
            XREnvironmentProbe_SetScale(instance, provider.Scale);
            XREnvironmentProbe_SetPose(instance, new Pose(provider.Position, provider.Rotation));
            XREnvironmentProbe_SetSize(instance, provider.Size);
            if (provider.Texture.ToTextureDescriptor(out var desc, propertyNameId))
                instance.XREnvironmentProbe_SetTextureDescriptor(desc);
            else
            {
                probe = XREnvironmentProbe.defaultValue;
                return false;
            }
            instance.XREnvironmentProbe_SetTrackingState(provider.TrackingState);
            instance.XREnvironmentProbe_SetNativePointer(provider.NativePointer);
            probe = (XREnvironmentProbe) instance;
            return true;
        }

        private static Type _probeType;
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();

        private static void SetField(object instance, string name, object value)
        {
            if (_probeType == null) _probeType = typeof(XREnvironmentProbe);
            if (Fields.ContainsKey(name) == false)
                Fields[name] = _probeType.GetField(name, BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
            Fields[name].SetValue(instance, value);
        }

        private static void XREnvironmentProbe_SetTrackableId(this object instance, TrackableId value)
        {
            SetField(instance, "m_TrackableId", value);
        }

        private static void XREnvironmentProbe_SetScale(this object instance, Vector3 value)
        {
            SetField(instance, "m_Scale", value);
        }

        private static void XREnvironmentProbe_SetSize(this object instance, Vector3 value)
        {
            SetField(instance, "m_Size", value);
        }

        private static void XREnvironmentProbe_SetPose(this object instance, Pose value)
        {
            SetField(instance, "m_Pose", value);
        }

        private static void XREnvironmentProbe_SetTextureDescriptor(this object instance, XRTextureDescriptor value)
        {
            SetField(instance, "m_TextureDescriptor", value);
        }

        private static void XREnvironmentProbe_SetTrackingState(this object instance, TrackingState value)
        {
            SetField(instance, "m_TrackingState", value);
        }

        private static void XREnvironmentProbe_SetNativePointer(this object instance, IntPtr value)
        {
            SetField(instance, "m_NativePtr", value);
        }
    }
}