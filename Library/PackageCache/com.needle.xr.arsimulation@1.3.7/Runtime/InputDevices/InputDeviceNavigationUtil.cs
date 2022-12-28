using Needle.XR.ARSimulation.Compatibility;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Needle.XR.ARSimulation.Simulation
{
    public static class InputDeviceNavigationUtil
    {
        private static Camera _arCamera;

        private static Camera ARCamera
        {
            get
            {
                if (_arCamera) return _arCamera;
                var orig = Object.FindObjectOfType<ARSessionOrigin>();
                if (orig) _arCamera = orig.camera;
                return _arCamera;
            }
        }
        
        public static void SetFocus(IInputDevice device)
        {
            if (!ARCamera) return;
            var origin = InputHelper.Instance.MousePosition;
            var settings = ARSimulationLoader.GetSettings();
            if (settings && settings.InputSettings != null && settings.InputSettings.FocusAt == ARSimulationLoaderSettings.DeviceInputSettings.FocusMode.ScreenCenter)
                origin = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var ray = ARCamera.ScreenPointToRay(origin);
            if (!Physics.Raycast(ray, out var hit)) return;
            var point = hit.point;
            var offset = device.Forward;
            if (settings && settings.InputSettings != null)
                offset *= settings.InputSettings.DefaultFocusDistance;
            point -= offset;
            device.SetTargetPosition(point);
        }
    }
}
