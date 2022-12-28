using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

#if !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
namespace Needle.XR.ARSimulation.Extensions
{
	public static class XRCameraImagePlaneHelper
	{
		private static Type _type;
		private static readonly Dictionary<string, PropertyInfo> _properties = new Dictionary<string, PropertyInfo>();

		private static void SetProperty(object instance, string name, object value)
		{
			if (_type == null) _type = typeof(XRCameraImagePlane);
			if (_properties.ContainsKey(name) == false)
				_properties[name] = _type.GetProperty(name, (BindingFlags)(~0));
			if(_properties[name] != null)
				_properties[name].SetValue(instance, value);
		}

		internal static void XRCameraImagePlaneHelper_NativeArray(this object instance, NativeArray<byte> value)
		{
			SetProperty(instance, "data", value);
		}

		internal static void XRCameraImagePlaneHelper_PixelStride(this object instance, int value)
		{
			SetProperty(instance, "pixelStride", value);
		}

		internal static void XRCameraImagePlaneHelper_RowStride(this object instance, int value)
		{
			SetProperty(instance, "rowStride", value);
		}
	}
}
#endif