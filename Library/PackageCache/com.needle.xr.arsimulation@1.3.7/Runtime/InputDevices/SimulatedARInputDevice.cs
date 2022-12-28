using Needle.XR.ARSimulation.Compatibility;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine;
#if UNITY_NEW_INPUT_SYSTEM
using System.Collections;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

#endif

namespace Needle.XR.ARSimulation.Simulation
{
    /// <summary>
    /// Provides editor input support for the new input system
    /// </summary>
    public class SimulatedARInputDevice : MonoBehaviour, IOverrideablePositionRotation, IInputDevice
    {
        public Vector3 Forward => transform.forward;

        public void SetTargetPosition(Vector3 position)
        {
#if UNITY_NEW_INPUT_SYSTEM
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            _targetPosition = position;
#endif
#endif
        }

        public void SetTargetRotation(Quaternion rot)
        {
#if UNITY_NEW_INPUT_SYSTEM
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            _targetRotation = rot;
#endif
#endif
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
#if UNITY_NEW_INPUT_SYSTEM
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
            requestedOverride = true;
            requestedPosition = position;
            requestedRotation = rotation;
            _targetPosition = position;
            _targetRotation = rotation;
#endif
#endif
        }

        public float moveSensibility = 1;
        public float rotationSensibility = 5;
        [Range(0.01f, 1)] public float interpolation = .1f;

#if UNITY_NEW_INPUT_SYSTEM
        private bool requestedOverride = false;
        private Vector3 requestedPosition = Vector3.zero;
        private Quaternion requestedRotation;

        private HandheldARInputDevice m_device;

        // private CameraController _mController;
        private Quaternion _targetRotation;
        private Vector3 _targetPosition;
        private Vector2 lastMousePosition;
#endif


#if UNITY_NEW_INPUT_SYSTEM
#if (!UNITY_IOS && !UNITY_ANDROID) || UNITY_EDITOR
        private IEnumerator Start()
        {
            if (requestedOverride) yield break;
            var c = Camera.main;
            if (c == null) yield break;
            var t = c.transform;
            var p = t.localPosition;
            var r = t.localRotation;
            SetPositionAndRotation(p, r);
            using (StateEvent.From(m_device, out var ptr))
            {
                InputSystem.QueueEvent(ptr);
                InputSystem.Update();
            }
        }

        // Start is called before the first frame update
        private void OnEnable()
        {
            m_device = InputSystem.AddDevice<HandheldARInputDevice>("ARSimulation-Device");
            // if (_mController == null)
            //     _mController = new CameraController();
            // _mController.Enable();
        }

        private void OnDisable()
        {
            InputSystem.RemoveDevice(m_device);
        }


        private void Update()
        {
            using (StateEvent.From(m_device, out var ptr))
            {
                if (requestedOverride)
                {
                    _targetPosition = requestedPosition;
                    _targetRotation = requestedRotation;
                    m_device.devicePosition.WriteValueIntoEvent(requestedPosition, ptr);
                    m_device.deviceRotation.WriteValueIntoEvent(requestedRotation, ptr);
                    InputSystem.QueueEvent(ptr);
                    InputSystem.Update();
                }

                InputHelper.GetInput(out var h, out var v, out var u, out var active, out var wasActive);

                var pos = m_device.devicePosition.ReadValue();

                // ReSharper disable once Unity.NoNullPropagation
                var settings = ARSimulationLoaderSettings.Instance?.InputSettings ?? new ARSimulationLoaderSettings.DeviceInputSettings();
                var requireRightMouseToMove = (settings.Mouse & ARSimulationLoaderSettings.DeviceInputSettings.MouseMovement.RequireRightMousePressedToMove) != 0;

                if ((active && wasActive) || !requireRightMouseToMove)
                {
                    var dt = Time.deltaTime;
                    dt = Mathf.Clamp(dt, 0, .1f);
                    var factor = InputHelper.Instance.ShiftPressed ? 5 : 1;
                    h *= moveSensibility * dt * settings.MovementSensitivity * factor;
                    v *= moveSensibility * dt * settings.MovementSensitivity * factor;
                    u *= moveSensibility * dt * settings.MovementSensitivity * factor;

                    var t = transform;
                    var forward = Quaternion.Inverse(t.parent.rotation) * t.forward;
                    Vector3 up;
                    if (settings.Mode == ARSimulationLoaderSettings.DeviceInputSettings.MovementMode.Walk)
                    {
                        forward.y = 0;
                        forward.Normalize();
                        up = Vector3.up;
                    }
                    else
                    {
                        forward = t.forward;
                        up = t.up;
                    }
                    
                    var right = Quaternion.Inverse(t.parent.rotation) * t.right;
                    _targetPosition += (v * forward + h * right + u * up);

                    if(active && wasActive)
                    {
                        var mouseDelta = InputHelper.Instance.MousePosition - lastMousePosition;
                        InputHelper.NormalizeSpeed(ref mouseDelta);
                        var delta = new Vector2(mouseDelta.x, mouseDelta.y);
                        delta = Vector2.ClampMagnitude(delta, 200);
                        var d = new Vector2(delta.x, delta.y);
                        d *= rotationSensibility * Time.deltaTime * settings.RotationSensitivity;
                        if (!d.HasNaN())
                        {
                            var wr = new Vector3(0, d.x, 0);
                            var lr = new Vector3(-d.y, 0, 0);
                            _targetRotation.Normalize();
                            _targetRotation = Quaternion.Euler(wr) * (_targetRotation * Quaternion.Euler(lr));
                        }
                    }
                }

                lastMousePosition = InputHelper.Instance.MousePosition;
                
                if (!requestedOverride)
                {
                    if (InputHelper.Instance.FocusKeyDown) InputDeviceNavigationUtil.SetFocus(this);

                    var gainedFocus = InputHelper.Instance.GainedFocus;
                    
                    var step = gainedFocus ? 1 : Mathf.Clamp01(Time.deltaTime / interpolation);
                    var newPosition = Vector3.Lerp(pos, _targetPosition, step);
                    m_device.devicePosition.WriteValueIntoEvent(newPosition, ptr);
                    var newRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, step);
                    
                    m_device.deviceRotation.WriteValueIntoEvent(newRotation, ptr);

                    InputSystem.QueueEvent(ptr);
                    InputSystem.Update();
                }

                if (active) requestedOverride = false;
            }
        }
#endif
#endif
    }
}