using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    internal static class CameraFrameHelper
    {
        private static Type _type;

        private static Type Type
        {
            get
            {
                if (_type == null) _type = typeof(XRCameraFrame);
                return _type;
            }
        }
        
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();

        private static FieldInfo GetField(string name)
        {
            return Type.GetField(name, BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void SetValue(this object frame, string name, object value)
        {
            // Debug.Log(frame + " - " + name + " - " + value);
            if (!Fields.ContainsKey(name))
                Fields[name] = GetField(name);
            Fields[name].SetValue(frame, value);
        }

        internal static void XRCameraFrame_SetTimeStampNs(this object frame, long value)
        {
            frame.SetValue("m_TimestampNs", value);
        }

        internal static void XRCameraFrame_SetBrightness(this object frame, float value)
        {
            frame.SetValue("m_AverageBrightness", value);
        }

        internal static void XRCameraFrame_SetColorTemperature(this object frame, float value)
        {
            frame.SetValue("m_AverageColorTemperature", value);
        }

        internal static void XRCameraFrame_SetColorCorrection(this object frame, Color value)
        {
            frame.SetValue("m_ColorCorrection", value);
        }

        internal static void XRCameraFrame_SetProjectionMatrix(this object frame, Matrix4x4 value)
        {
            frame.SetValue("m_ProjectionMatrix", value);
        }

        internal static void XRCameraFrame_SetDisplayMatrix(this object frame, Matrix4x4 value)
        {
            frame.SetValue("m_DisplayMatrix", value);
        }

        internal static void XRCameraFrame_SetTrackingState(this object frame, TrackingState value)
        {
            frame.SetValue("m_TrackingState", value);
        }
        
        /// <summary>
        /// defaults to all
        /// </summary>
        internal static void XRCameraFrame_SetProperties(this object frame, XRCameraFrameProperties value = (XRCameraFrameProperties) ~ 0)
        {
            frame.SetValue("m_Properties", value);
        }

        public static XRCameraFrameProperties All(this XRCameraFrameProperties props) => (XRCameraFrameProperties) ~0;

        internal static void XRCameraFrame_SetExposureDuration(this object frame, double value)
        {
            frame.SetValue("m_ExposureDuration", value);
        }

        internal static void XRCameraFrame_SetExposureOffset(this object frame, float value)
        {
            frame.SetValue("m_ExposureOffset", value);
        }

        internal static void XRCameraFrame_SetMainLightIntensityLumens(this object frame, float value)
        {
            frame.SetValue("m_MainLightIntensityLumens", value);
        }

        internal static void XRCameraFrame_SetMainLightColor(this object frame, Color value)
        {
            frame.SetValue("m_MainLightColor", value);
        }

        internal static void XRCameraFrame_SetMainLightDirection(this object frame, Vector3 value)
        {
            frame.SetValue("m_MainLightDirection", value);
        }

        internal static void XRCameraFrame_SetAmbientSphericalHarmonics(this object frame, SphericalHarmonicsL2 value)
        {
            frame.SetValue("m_AmbientSphericalHarmonics", value);
        }
    }

}