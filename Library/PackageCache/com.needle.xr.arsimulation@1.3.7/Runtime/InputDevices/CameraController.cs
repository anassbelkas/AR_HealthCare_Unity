// #if UNITY_NEW_INPUT_SYSTEM
// // GENERATED AUTOMATICALLY FROM 'Packages/com.needle.xr.arsimulation/Runtime/InputDevices/CameraController.inputactions'
//
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine.InputSystem;
// using UnityEngine.InputSystem.Utilities;
//
// public class @CameraController : IInputActionCollection, IDisposable
// {
//     public InputActionAsset asset { get; }
//     public @CameraController()
//     {
//         asset = InputActionAsset.FromJson(@"{
//     ""name"": ""CameraController"",
//     ""maps"": [
//         {
//             ""name"": ""CameraControls"",
//             ""id"": ""8fba81c1-9be1-4746-8b2f-bc1933cf3191"",
//             ""actions"": [
//                 {
//                     ""name"": ""Forward"",
//                     ""type"": ""Value"",
//                     ""id"": ""86fa2e7e-1629-4470-8c71-6e30fa3f1361"",
//                     ""expectedControlType"": """",
//                     ""processors"": """",
//                     ""interactions"": """"
//                 },
//                 {
//                     ""name"": ""Right"",
//                     ""type"": ""Value"",
//                     ""id"": ""b8b81021-4180-465d-a754-46acff78fd15"",
//                     ""expectedControlType"": ""Button"",
//                     ""processors"": """",
//                     ""interactions"": """"
//                 },
//                 {
//                     ""name"": ""Up"",
//                     ""type"": ""Value"",
//                     ""id"": ""a65dde5d-52a6-4f3f-be7c-7ac73d214ca0"",
//                     ""expectedControlType"": ""Button"",
//                     ""processors"": """",
//                     ""interactions"": """"
//                 },
//                 {
//                     ""name"": ""RotateHorizontal"",
//                     ""type"": ""Value"",
//                     ""id"": ""363bb05a-0db2-4581-a78e-ebf58bc9c7bc"",
//                     ""expectedControlType"": """",
//                     ""processors"": """",
//                     ""interactions"": """"
//                 },
//                 {
//                     ""name"": ""RotateVertical"",
//                     ""type"": ""Value"",
//                     ""id"": ""21f5ef28-b101-4592-a9c3-80aa2c4fecf7"",
//                     ""expectedControlType"": """",
//                     ""processors"": """",
//                     ""interactions"": """"
//                 },
//                 {
//                     ""name"": ""RotateActive"",
//                     ""type"": ""Value"",
//                     ""id"": ""3e66f21f-fcbe-4f9e-914a-722f1ea5cd64"",
//                     ""expectedControlType"": ""Button"",
//                     ""processors"": """",
//                     ""interactions"": """"
//                 }
//             ],
//             ""bindings"": [
//                 {
//                     ""name"": ""Keyboard"",
//                     ""id"": ""a8b6daf3-e243-4fcc-8ad2-4745b894b6f7"",
//                     ""path"": ""1DAxis"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Forward"",
//                     ""isComposite"": true,
//                     ""isPartOfComposite"": false
//                 },
//                 {
//                     ""name"": ""negative"",
//                     ""id"": ""fde96929-63e4-4823-a929-856f6e246dbf"",
//                     ""path"": ""<Keyboard>/s"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Forward"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": true
//                 },
//                 {
//                     ""name"": ""positive"",
//                     ""id"": ""ee8ce992-1f44-413a-9656-c9a202e9c242"",
//                     ""path"": ""<Keyboard>/w"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Forward"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": true
//                 },
//                 {
//                     ""name"": ""Keyboard"",
//                     ""id"": ""8414b1b1-276b-4319-83bc-cfddc7e5777e"",
//                     ""path"": ""1DAxis"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Right"",
//                     ""isComposite"": true,
//                     ""isPartOfComposite"": false
//                 },
//                 {
//                     ""name"": ""negative"",
//                     ""id"": ""ff20d871-1388-4040-b32a-a4cd8956d4f1"",
//                     ""path"": ""<Keyboard>/a"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Right"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": true
//                 },
//                 {
//                     ""name"": ""positive"",
//                     ""id"": ""21453e77-4ee5-4b5f-a617-9f00fe21d65f"",
//                     ""path"": ""<Keyboard>/d"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Right"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": true
//                 },
//                 {
//                     ""name"": """",
//                     ""id"": ""570cb9d4-9638-4374-a859-bc20f907e735"",
//                     ""path"": ""<Mouse>/delta/x"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""RotateHorizontal"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": false
//                 },
//                 {
//                     ""name"": """",
//                     ""id"": ""c79c8c32-7a2b-4684-8ceb-895588656bb2"",
//                     ""path"": ""<Mouse>/rightButton"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""RotateActive"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": false
//                 },
//                 {
//                     ""name"": """",
//                     ""id"": ""4243b3f3-567e-498b-b585-9a86e28707e4"",
//                     ""path"": ""<Mouse>/delta/y"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""RotateVertical"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": false
//                 },
//                 {
//                     ""name"": ""Keyboard"",
//                     ""id"": ""c47f6950-103d-4c0f-aaba-97cf177443c1"",
//                     ""path"": ""1DAxis"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Up"",
//                     ""isComposite"": true,
//                     ""isPartOfComposite"": false
//                 },
//                 {
//                     ""name"": ""negative"",
//                     ""id"": ""77ae7d10-5ed6-4903-987c-975cf7a88ca8"",
//                     ""path"": ""<Keyboard>/q"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Up"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": true
//                 },
//                 {
//                     ""name"": ""positive"",
//                     ""id"": ""d87fe21d-c279-4702-86d6-bdeb457eb182"",
//                     ""path"": ""<Keyboard>/e"",
//                     ""interactions"": """",
//                     ""processors"": """",
//                     ""groups"": """",
//                     ""action"": ""Up"",
//                     ""isComposite"": false,
//                     ""isPartOfComposite"": true
//                 }
//             ]
//         }
//     ],
//     ""controlSchemes"": []
// }");
//         // CameraControls
//         m_CameraControls = asset.FindActionMap("CameraControls", throwIfNotFound: true);
//         m_CameraControls_Forward = m_CameraControls.FindAction("Forward", throwIfNotFound: true);
//         m_CameraControls_Right = m_CameraControls.FindAction("Right", throwIfNotFound: true);
//         m_CameraControls_Up = m_CameraControls.FindAction("Up", throwIfNotFound: true);
//         m_CameraControls_RotateHorizontal = m_CameraControls.FindAction("RotateHorizontal", throwIfNotFound: true);
//         m_CameraControls_RotateVertical = m_CameraControls.FindAction("RotateVertical", throwIfNotFound: true);
//         m_CameraControls_RotateActive = m_CameraControls.FindAction("RotateActive", throwIfNotFound: true);
//     }
//
//     public void Dispose()
//     {
//         UnityEngine.Object.Destroy(asset);
//     }
//
//     public InputBinding? bindingMask
//     {
//         get => asset.bindingMask;
//         set => asset.bindingMask = value;
//     }
//
//     public ReadOnlyArray<InputDevice>? devices
//     {
//         get => asset.devices;
//         set => asset.devices = value;
//     }
//
//     public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;
//
//     public bool Contains(InputAction action)
//     {
//         return asset.Contains(action);
//     }
//
//     public IEnumerator<InputAction> GetEnumerator()
//     {
//         return asset.GetEnumerator();
//     }
//
//     IEnumerator IEnumerable.GetEnumerator()
//     {
//         return GetEnumerator();
//     }
//
//     public void Enable()
//     {
//         asset.Enable();
//     }
//
//     public void Disable()
//     {
//         asset.Disable();
//     }
//
//     // CameraControls
//     private readonly InputActionMap m_CameraControls;
//     private ICameraControlsActions m_CameraControlsActionsCallbackInterface;
//     private readonly InputAction m_CameraControls_Forward;
//     private readonly InputAction m_CameraControls_Right;
//     private readonly InputAction m_CameraControls_Up;
//     private readonly InputAction m_CameraControls_RotateHorizontal;
//     private readonly InputAction m_CameraControls_RotateVertical;
//     private readonly InputAction m_CameraControls_RotateActive;
//     public struct CameraControlsActions
//     {
//         private @CameraController m_Wrapper;
//         public CameraControlsActions(@CameraController wrapper) { m_Wrapper = wrapper; }
//         public InputAction @Forward => m_Wrapper.m_CameraControls_Forward;
//         public InputAction @Right => m_Wrapper.m_CameraControls_Right;
//         public InputAction @Up => m_Wrapper.m_CameraControls_Up;
//         public InputAction @RotateHorizontal => m_Wrapper.m_CameraControls_RotateHorizontal;
//         public InputAction @RotateVertical => m_Wrapper.m_CameraControls_RotateVertical;
//         public InputAction @RotateActive => m_Wrapper.m_CameraControls_RotateActive;
//         public InputActionMap Get() { return m_Wrapper.m_CameraControls; }
//         public void Enable() { Get().Enable(); }
//         public void Disable() { Get().Disable(); }
//         public bool enabled => Get().enabled;
//         public static implicit operator InputActionMap(CameraControlsActions set) { return set.Get(); }
//         public void SetCallbacks(ICameraControlsActions instance)
//         {
//             if (m_Wrapper.m_CameraControlsActionsCallbackInterface != null)
//             {
//                 @Forward.started -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnForward;
//                 @Forward.performed -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnForward;
//                 @Forward.canceled -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnForward;
//                 @Right.started -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRight;
//                 @Right.performed -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRight;
//                 @Right.canceled -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRight;
//                 @Up.started -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnUp;
//                 @Up.performed -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnUp;
//                 @Up.canceled -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnUp;
//                 @RotateHorizontal.started -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateHorizontal;
//                 @RotateHorizontal.performed -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateHorizontal;
//                 @RotateHorizontal.canceled -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateHorizontal;
//                 @RotateVertical.started -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateVertical;
//                 @RotateVertical.performed -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateVertical;
//                 @RotateVertical.canceled -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateVertical;
//                 @RotateActive.started -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateActive;
//                 @RotateActive.performed -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateActive;
//                 @RotateActive.canceled -= m_Wrapper.m_CameraControlsActionsCallbackInterface.OnRotateActive;
//             }
//             m_Wrapper.m_CameraControlsActionsCallbackInterface = instance;
//             if (instance != null)
//             {
//                 @Forward.started += instance.OnForward;
//                 @Forward.performed += instance.OnForward;
//                 @Forward.canceled += instance.OnForward;
//                 @Right.started += instance.OnRight;
//                 @Right.performed += instance.OnRight;
//                 @Right.canceled += instance.OnRight;
//                 @Up.started += instance.OnUp;
//                 @Up.performed += instance.OnUp;
//                 @Up.canceled += instance.OnUp;
//                 @RotateHorizontal.started += instance.OnRotateHorizontal;
//                 @RotateHorizontal.performed += instance.OnRotateHorizontal;
//                 @RotateHorizontal.canceled += instance.OnRotateHorizontal;
//                 @RotateVertical.started += instance.OnRotateVertical;
//                 @RotateVertical.performed += instance.OnRotateVertical;
//                 @RotateVertical.canceled += instance.OnRotateVertical;
//                 @RotateActive.started += instance.OnRotateActive;
//                 @RotateActive.performed += instance.OnRotateActive;
//                 @RotateActive.canceled += instance.OnRotateActive;
//             }
//         }
//     }
//     public CameraControlsActions @CameraControls => new CameraControlsActions(this);
//     public interface ICameraControlsActions
//     {
//         void OnForward(InputAction.CallbackContext context);
//         void OnRight(InputAction.CallbackContext context);
//         void OnUp(InputAction.CallbackContext context);
//         void OnRotateHorizontal(InputAction.CallbackContext context);
//         void OnRotateVertical(InputAction.CallbackContext context);
//         void OnRotateActive(InputAction.CallbackContext context);
//     }
// }
// #endif