using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Needle.XR.ARSimulation.Compatibility;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.TouchPhase;
#if UNITY_NEW_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

#endif

namespace Needle.XR.ARSimulation.ExampleComponents
{
	/// <summary>
	/// Helper class to perform AR raycasts from mouse position
	/// </summary>
	public static class ARInput
	{
		public enum InputType
		{
			Any = 0,
			Mouse = 1,
			Touch = 2,
		}

		private static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();


		public static ARRaycastManager RaycastManager => new Lazy<ARRaycastManager>(() =>
		{
			var manager = Object.FindObjectOfType<ARRaycastManager>();
			if(!manager) Debug.LogWarning("No RaycastManager found");
			return manager;
		}).Value;

		public static bool TryGetHit(InputType type, out ARRaycastHit hit)
		{
			if (!TryGetInputPosition(type, out var inputPosition))
			{
				hit = new ARRaycastHit();
				return false;
			}
			
			if (!RaycastManager || !RaycastManager.Raycast(inputPosition, s_Hits, TrackableType.PlaneWithinPolygon))
			{				
				hit = new ARRaycastHit();
				return false;
			}

			hit = s_Hits[0];
			return true;
		}

#if UNITY_NEW_INPUT_SYSTEM
		private static readonly bool[] touchBlocked = new bool[128];
#endif

		// ReSharper disable once MemberCanBePrivate.Global
		public static bool TryGetInputPosition(InputType type, out Vector2 inputPos)
		{
			if (!Ensure.CorrectInputSystemConfiguration())
			{
				inputPos = Vector2.zero;
				return false;
			}

#if UNITY_NEW_INPUT_SYSTEM
			if (TryGetInputPositionNewInputSystem(type, out inputPos))
				return true;
#else
            if (type == InputType.Mouse || type == InputType.Any)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var mousePosition = Input.mousePosition;
                    inputPos = new Vector2(mousePosition.x, mousePosition.y);
                    return true;
                }
            }

            if (type == InputType.Touch || type == InputType.Any)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase != TouchPhase.Began) continue;
                    inputPos = t.position;
                    return true;
                }
            }
#endif

			inputPos = default;
			return false;
		}

#if UNITY_NEW_INPUT_SYSTEM
		private static bool TryGetInputPositionFromMouse(out Vector2 inputPos)
		{
			var mouse = Mouse.current;
			if (mouse != null && mouse.enabled && mouse.leftButton != null)
			{
				// Debug.Log(mouse.leftButton.ReadValue() + ", " + mouse.leftButton.wasPressedThisFrame + ", " + mouse.leftButton.wasReleasedThisFrame + ", " + mouse.leftButton.ReadValueFromPreviousFrame() + ", " + mouse.leftButton.device.wasUpdatedThisFrame);
				mouse.leftButton.pressPoint = .5f;
				if (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.ReadValue() > mouse.leftButton.ReadValueFromPreviousFrame())
				{
					inputPos = mouse.position.ReadValue();
					if (inputPos.x < 0 || inputPos.y < 0 || inputPos.x > Screen.width || inputPos.y > Screen.height) return false;
					return true;
				}
			}

			inputPos = default;
			return false;
		}

		private static bool TryGetInputPositionFromTouch(out Vector2 inputPos)
		{
			var touchScreen = Touchscreen.current;
			if (touchScreen != null && touchScreen.enabled && touchScreen.touches.Count > 0)
			{
				foreach (var touch in touchScreen.touches)
				{
					var touchId = touch.touchId.ReadValue();
					if (touchId < 0 || touchId >= touchBlocked.Length) continue;
					if (!touchBlocked[touchId] && touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
					{
						touchBlocked[touchId] = true;

						inputPos = touch.ReadValue().position;
						if (inputPos.x < 0 || inputPos.y < 0 || inputPos.x > Screen.width || inputPos.y > Screen.height) 
							return false;
						return true;
					}
					if (touch.phase.ReadValue().IsEndedOrCanceled())
					{
						touchBlocked[touchId] = false;
					}
				}
			}

			inputPos = default;
			return false;
		}

		private static bool TryGetInputPositionNewInputSystem(InputType type, out Vector2 inputPos)
		{
			if (type == InputType.Mouse || type == InputType.Any)
			{
				if (TryGetInputPositionFromMouse(out inputPos))
					return true;
			}

			if (type == InputType.Touch || type == InputType.Any)
			{
				if (TryGetInputPositionFromTouch(out inputPos))
					return true;
			}

			inputPos = default;
			return false;
		}
#endif
	}
}