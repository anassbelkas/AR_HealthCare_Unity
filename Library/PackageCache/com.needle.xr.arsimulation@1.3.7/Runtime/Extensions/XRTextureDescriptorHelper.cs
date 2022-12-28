using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    public static class XRTextureDescriptorHelper
    {
        public static bool ToTextureDescriptor(this Cubemap map, out XRTextureDescriptor desc, int propertyNameId = 0)
        {
            // var cm = new RenderTexture(map.width, map.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            // cm.dimension = TextureDimension.Cube;
            // cm.useMipMap = true;
            // cm.autoGenerateMips = false;
            // cm.Create();
            // // Graphics.CopyTexture(map, 0, 0, cm, 0, 0);
            // Graphics.Blit(map, cm);


            if (map.isReadable == false)
            {
                desc = new XRTextureDescriptor();
                Debug.Log("Can not create Texture Descriptor, Texture is not readable", map);
                return false;
            }

            // object to box it for setting values via reflection
            object instance = new XRTextureDescriptor();
            instance.XRTextureDescriptor_NativeTexture(map.GetNativeTexturePtr());
            instance.XRTextureDescriptor_Width(map.width);
            instance.XRTextureDescriptor_Height(map.height);
            instance.XRTextureDescriptor_MipmapCount(map.mipmapCount);
            instance.XRTextureDescriptor_TextureFormat(map.format);
            // not exactly sure why/which prop name id we need here:
            instance.XRTextureDescriptor_PropertyNameId(propertyNameId);
            desc = (XRTextureDescriptor) instance;
            return desc.valid;
        }

        private static Type _probeType;
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();

        private static void SetField(object instance, string name, object value)
        {
            if (_probeType == null) _probeType = typeof(XRTextureDescriptor);
            if (Fields.ContainsKey(name) == false)
                Fields[name] = _probeType.GetField(name, BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
            if(Fields[name] != null)
                Fields[name].SetValue(instance, value);
        }

        internal static void XRTextureDescriptor_NativeTexture(this object instance, IntPtr value)
        {
            SetField(instance, "m_NativeTexture", value);
        }

        internal static void XRTextureDescriptor_Width(this object instance, int value)
        {
            SetField(instance, "m_Width", value);
        }

        internal static void XRTextureDescriptor_Height(this object instance, int value)
        {
            SetField(instance, "m_Height", value);
        }

        internal static void XRTextureDescriptor_MipmapCount(this object instance, int value)
        {
            SetField(instance, "m_MipmapCount", value);
        }

        internal static void XRTextureDescriptor_TextureFormat(this object instance, TextureFormat value)
        {
            SetField(instance, "m_Format", value);
        }

        internal static void XRTextureDescriptor_PropertyNameId(this object instance, int value)
        {
            SetField(instance, "m_PropertyNameId", value);
        }

        internal static void XRTextureDescriptor_Dimension(this object instance, TextureDimension dimension)
        {
            SetField(instance, "m_Dimension", dimension);
        }
    }
}