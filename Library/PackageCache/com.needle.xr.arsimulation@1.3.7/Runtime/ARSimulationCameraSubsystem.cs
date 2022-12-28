using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Simulation;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// The camera subsystem implementation for ARSimulation.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationCameraSubsystem : XRCameraSubsystem
    {
        public static bool IsRunning { get; private set; }
        
#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        /// <summary>
        /// Create the ARSimulation camera functionality provider for the camera subsystem.
        /// </summary>
        /// <returns>
        /// The ARSimulation camera functionality provider for the camera subsystem.
        /// </returns>
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        /// <summary>
        /// The identifying name for the camera-providing implementation.
        /// </summary>
        /// <value>
        /// The identifying name for the camera-providing implementation.
        /// </value>
        const string k_SubsystemId = "ARSimulation-Camera";

        /// <summary>
        /// The name for the shader for rendering the camera texture.
        /// </summary>
        /// <value>
        /// The name for the shader for rendering the camera texture.
        /// </value>
        const string k_DefaultBackgroundShaderName = "Needle/ARSimulation/CameraBackground";

        enum CameraConfigurationResult
        {
            /// <summary>
            /// Setting the camera configuration was successful.
            /// </summary>
            Success = 0,

            /// <summary>
            /// The given camera configuration was not valid to be set by the provider.
            /// </summary>
            InvalidCameraConfiguration = 1,

            /// <summary>
            /// The provider session was invalid.
            /// </summary>
            InvalidSession = 2,

            /// <summary>
            /// An error occurred because the user did not dispose of all <c>XRCameraImages</c> and did not allow all
            /// asynchronous conversion jobs complete before changing the camera configuration.
            /// </summary>
            ErrorImagesNotDisposed = 3,
        }

        /// <summary>
        /// The name for the background shader based on the current render pipeline.
        /// </summary>
        /// <value>
        /// The name for the background shader based on the current render pipeline. Or, <c>null</c> if the current
        /// render pipeline is incompatible with the set of shaders.
        /// </value>
        /// <remarks>
        /// The value for the <c>GraphicsSettings.renderPipelineAsset</c> is not expected to change within the lifetime
        /// of the application.
        /// </remarks>
        public static string backgroundShaderName => k_DefaultBackgroundShaderName;

        /// <summary>
        /// Create and register the camera subsystem descriptor to advertise a providing implementation for camera
        /// functionality.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            var cameraSubsystemCinfo = new XRCameraSubsystemCinfo
            {
                id = k_SubsystemId,
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationCameraSubsystem),
#else
                implementationType = typeof(ARSimulationCameraSubsystem),
#endif
                supportsAverageBrightness = true,
                supportsAverageColorTemperature = false,
                supportsColorCorrection = true,
                supportsDisplayMatrix = true,
                supportsProjectionMatrix = true,
                supportsTimestamp = true,
                supportsCameraConfigurations = true,
                supportsCameraImage = true,
                supportsAverageIntensityInLumens = false,
                supportsFocusModes = true,
#if UNITY_AR_FOUNDATION_4_OR_NEWER
                supportsFaceTrackingAmbientIntensityLightEstimation = true,
                supportsFaceTrackingHDRLightEstimation = false,
                supportsWorldTrackingAmbientIntensityLightEstimation = true,
                supportsWorldTrackingHDRLightEstimation = true,
#endif
            };

            if (!XRCameraSubsystem.Register(cameraSubsystemCinfo))
            {
                // Debug.LogErrorFormat("Cannot register the {0} subsystem", k_SubsystemId);
            }
#endif
        }

        /// <summary>
        /// Provides the camera functionality for the ARSimulation implementation.
        /// </summary>
        private class ARSimulationProvider : Provider
        {
            // [InitializeOnLoadMethod]
            // private static void Init()
            // {
            //     EditorApplication.playModeStateChanged += pm =>
            //     {
            //         foreach (var aq in acquiredCpuImages)
            //         {
            //             OnDisposeImage(aq.Key, false);
            //         }
            //         acquiredCpuImages.Clear();
            //     };
            // }
            
            private SimulatedARCameraFrameDataProvider _simulatedARCameraFrameDataProvider;

            private SimulatedARCameraFrameDataProvider SimulatedARCameraFrameDataProvider
            {
                get
                {
                    #if UNITY_EDITOR
                    if (!BuildPipeline.isBuildingPlayer && !_simulatedARCameraFrameDataProvider)
                    {
                        _simulatedARCameraFrameDataProvider = UnityEngine.Object.FindObjectOfType<SimulatedARCameraFrameDataProvider>();
                        if (!_simulatedARCameraFrameDataProvider)
                        {
                            _simulatedARCameraFrameDataProvider = new GameObject("Simulated AR Camera Data").AddComponent<SimulatedARCameraFrameDataProvider>();
                        }
                    }
                    #endif

                    return _simulatedARCameraFrameDataProvider;
                }
            }

            private SimulatedAREnvironmentManager _environmentManager;
            private int _environmentManagerLastSearchFrame;
            private SimulatedAREnvironmentManager EnvironmentManager
            {
                get
                {
                    if (_environmentManager) return _environmentManager;
                    if (_environmentManagerLastSearchFrame != 0 && (Time.frameCount % 120 != 0 || Time.frameCount == _environmentManagerLastSearchFrame)) 
                        return _environmentManager;
                    _environmentManagerLastSearchFrame = Time.frameCount;
                    _environmentManager = UnityEngine.Object.FindObjectOfType<SimulatedAREnvironmentManager>();
                    return _environmentManager;
                }
            }

            private class CameraTextureCopyHelper
            {
                private ProfilerMarker marker = new ProfilerMarker(nameof(UpdateTexture));
                public int LastUpdateFrame { get; private set; }
                public Texture2D Texture2D { get; private set; }
                public void UpdateTexture(RenderTexture renderTex)
                {
                    using (marker.Auto())
                    {
                        if (renderTex == null) return;
                        if (Texture2D == null || Texture2D.width != renderTex.width || Texture2D.height != renderTex.height)
                        {
                            Texture2D = new Texture2D(renderTex.width, renderTex.height);
                        }
                        else if (Time.frameCount == LastUpdateFrame) return;
                        LastUpdateFrame = Time.frameCount;
                        RenderTexture.active = renderTex;
                        Texture2D.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                        Texture2D.Apply();
                        RenderTexture.active = null;
                    }
                }
            }

            private CameraTextureCopyHelper cameraTextureCopyHelper;
            private ProfilerMarker tryGetLatestImageMarker = new ProfilerMarker(nameof(TryGetLatestImage));

            private bool TryGetLatestImage(out Texture2D img, bool newInstance = false)
            { 
                using(tryGetLatestImageMarker.Auto())
                {
                    var manager = EnvironmentManager;
                    img = null;
                    if (!manager)
                    {
                        return false;
                    }
                    if (cameraTextureCopyHelper == null) cameraTextureCopyHelper = new CameraTextureCopyHelper();
                    var cameraImage = manager.EnvironmentCameraRT;
                    // update the one image we store
                    if (!newInstance)
                    {
                        cameraTextureCopyHelper.UpdateTexture(cameraImage);
                        img = cameraTextureCopyHelper.Texture2D;
                        return img;
                    }

                    if (!cameraImage)
                        return false;

                    // user requested a new instance (e.g. when calling aquire cpu image)
                    using(new ProfilerMarker("CreateNewImage").Auto())
                    {
                        var Texture2D = new Texture2D(cameraImage.width, cameraImage.height);
                        RenderTexture.active = cameraImage;
                        Texture2D.ReadPixels(new Rect(0, 0, cameraImage.width, cameraImage.height), 0, 0);
                        Texture2D.Apply();
                        RenderTexture.active = null;
                        img = Texture2D;
                        return img;
                    }
                }
            }

            
            /// <summary>
            /// The shader property name for the main texture of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name for the main texture of the camera video frame.
            /// </value>
            const string k_MainTexPropertyName = "_MainTex";

            const string k_CameraPermissionName = "editor.permission.CAMERA";

            /// <summary>
            /// The shader property name identifier for the main texture of the camera video frame.
            /// </summary>
            /// <value>
            /// The shader property name identifier for the main texture of the camera video frame.
            /// </value>
            static readonly int k_MainTexPropertyNameId = Shader.PropertyToID(k_MainTexPropertyName);

            /// <summary>
            /// Get the material used by <c>XRCameraSubsystem</c> to render the camera texture.
            /// </summary>
            /// <returns>
            /// The material to render the camera texture.
            /// </returns>
            public override Material cameraMaterial { get; }

            /// <summary>
            /// Determine whether camera permission has been granted.
            /// </summary>
            /// <returns>
            /// <c>true</c> if camera permission has been granted for this app. Otherwise, <c>false</c>.
            /// </returns>
            public override bool permissionGranted => true; // ARSimulationPermissionManager.IsPermissionGranted(k_CameraPermissionName);

            public override bool invertCulling => false; // NativeApi.UnityARSimulation_Camera_ShouldInvertCulling();


            /// <summary>
            /// Construct the camera functionality provider for ARSimulation.
            /// </summary>
            public ARSimulationProvider()
            {
                if (!Shader.Find(backgroundShaderName))
                {
                    Debug.Log("AR Simulation background shader is not found. Please add " + backgroundShaderName + " to included shaders");
                    return;
                }
                cameraMaterial = CreateCameraMaterial(backgroundShaderName);
            }

            /// <summary>
            /// Start the camera functionality.
            /// </summary>
            public override void Start()
            {
                IsRunning = true;
            }

            /// <summary>
            /// Stop the camera functionality.
            /// </summary>
            public override void Stop()
            {
                IsRunning = false;
            } // => NativeApi.UnityARSimulation_Camera_Stop();

            /// <summary>
            /// Destroy any resources required for the camera functionality.
            /// </summary>
            public override void Destroy()
            {
                IsRunning = false;
            } // => NativeApi.UnityARSimulation_Camera_Destruct();

            /// <summary>
            /// Get the camera frame for the subsystem.
            /// </summary>
            /// <param name="cameraParams">The current Unity <c>Camera</c> parameters.</param>
            /// <param name="cameraFrame">The current camera frame returned by the method.</param>
            /// <returns>
            /// <c>true</c> if the method successfully got a frame. Otherwise, <c>false</c>.
            /// </returns>
            public override bool TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                var fr = SimulatedARCameraFrameDataProvider;
                object frame = new XRCameraFrame();
                if (!fr)
                {
                    cameraFrame = (XRCameraFrame) frame;
                    return false;
                }
                
                frame.XRCameraFrame_SetProperties();
                frame.XRCameraFrame_SetBrightness(fr.AverageBrightness);
                frame.XRCameraFrame_SetColorTemperature(fr.ColorTemperature);
                frame.XRCameraFrame_SetColorCorrection(fr.ColorCorrection);
                frame.XRCameraFrame_SetProjectionMatrix(fr.ProjectionMatrix);
                frame.XRCameraFrame_SetDisplayMatrix(fr.DisplayMatrix);
                frame.XRCameraFrame_SetTrackingState(fr.TrackingState);
                frame.XRCameraFrame_SetExposureDuration(fr.ExposureDuration);
                frame.XRCameraFrame_SetExposureOffset(fr.ExposureOffset);

#if (UNITY_ARSUBSYSTEMS_3_1_0_PREVIEW_2_OR_NEWER && !UNITY_ARSUBSYSTEMS_3_1_3_OR_NEWER) || UNITY_ARSUBSYSTEMS_4_OR_NEWER
                // removed in 3.1.3 https://github.com/needle-mirror/com.unity.xr.arsubsystems/commit/928aca5600531b8a4eb7562c6a5f46ec3b580ba7
                frame.XRCameraFrame_SetMainLightIntensityLumens(fr.MainLightIntensityLumen);
                frame.XRCameraFrame_SetMainLightColor(fr.MainLightColor);
                frame.XRCameraFrame_SetMainLightDirection(fr.MainLightDirection);
                frame.XRCameraFrame_SetAmbientSphericalHarmonics(fr.SphericalHarmonicsL2);
#endif

#if UNITY_EDITOR
                Shader.EnableKeyword("_ARSIMULATION_EDITOR");
#endif

                cameraFrame = (XRCameraFrame) frame;
                return true;
            }

            /// <summary>
            /// Get the camera intrinisics information.
            /// </summary>
            /// <param name="cameraIntrinsics">The camera intrinsics information returned from the method.</param>
            /// <returns>
            /// <c>true</c> if the method successfully gets the camera intrinsics information. Otherwise, <c>false</c>.
            /// </returns>
            public override bool TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                cameraIntrinsics = new XRCameraIntrinsics();
                return false;
            }

            /// <summary>
            /// Queries the supported camera configurations.
            /// </summary>
            /// <param name="defaultCameraConfiguration">A default value used to fill the returned array before copying
            /// in real values. This ensures future additions to this struct are backwards compatible.</param>
            /// <param name="allocator">The allocation strategy to use for the returned data.</param>
            /// <returns>
            /// The supported camera configurations.
            /// </returns>
            public override NativeArray<XRCameraConfiguration> GetConfigurations(XRCameraConfiguration defaultCameraConfiguration,
                Allocator allocator)
            {
                return new NativeArray<XRCameraConfiguration>(0, allocator);
            }

            /// <summary>
            /// The current camera configuration.
            /// </summary>
            /// <value>
            /// The current camera configuration if it exists. Otherise, <c>null</c>.
            /// </value>
            /// <exception cref="System.ArgumentException">Thrown when setting the current configuration if the given
            /// configuration is not a valid, supported camera configuration.</exception>
            /// <exception cref="System.InvalidOperationException">Thrown when setting the current configuration if the
            /// implementation is unable to set the current camera configuration for various reasons such as:
            /// <list type="bullet">
            /// <item><description>ARSimulation session is invalid</description></item>
            /// <item><description>Captured <c>XRCameraImages</c> have not been disposed</description></item>
            /// </list>
            /// </exception>
            /// <seealso cref="TryAcquireLatestImage"/>
            public override XRCameraConfiguration? currentConfiguration
            {
                get
                {
                    if (TryGetCurrentConfiguration(out XRCameraConfiguration cameraConfiguration))
                    {
                        return cameraConfiguration;
                    }

                    return null;
                }
                set
                {
                    // Assert that the camera configuration is not null.
                    // The XRCameraSubsystem should have already checked this.
                    Debug.Assert(value != null, "Cannot set the current camera configuration to null");

                    // switch (NativeApi.UnityARSimulation_Camera_TrySetCurrentConfiguration((XRCameraConfiguration)value))
                    // {
                    //     case CameraConfigurationResult.Success:
                    //         break;
                    //     case CameraConfigurationResult.InvalidCameraConfiguration:
                    //         throw new ArgumentException("Camera configuration does not exist in the available "
                    //                                     + "configurations", "value");
                    //     case CameraConfigurationResult.InvalidSession:
                    //         throw new InvalidOperationException("Cannot set camera configuration because the ARSimulation "
                    //                                             + "session is not valid");
                    //     case CameraConfigurationResult.ErrorImagesNotDisposed:
                    //         throw new InvalidOperationException("Cannot set camera configuration because you have not "
                    //                                             + "disposed of all XRCameraImages and allowed all "
                    //                                             + "asynchronous conversion jobs to complete");
                    //     default:
                    //         throw new InvalidOperationException("cannot set camera configuration for ARSimulation");
                    // }
                }
            }

            private Vector2 lastScreenResolution;
            private XRTextureDescriptor cachedDescriptor;

            /// <summary>
            /// Gets the texture descriptors associated with the camera image.
            /// </summary>
            /// <returns>The texture descriptors.</returns>
            /// <param name="defaultDescriptor">Default descriptor.</param>
            /// <param name="allocator">Allocator.</param>
            public override NativeArray<XRTextureDescriptor> GetTextureDescriptors(
                XRTextureDescriptor defaultDescriptor,
                Allocator allocator)
            {
                var res = new Vector2(Screen.width, Screen.height);
                if (res != lastScreenResolution)
                {
                    if (TryGetLatestImage(out var tex))
                    {
                        lastScreenResolution = res;
                        var texDescriptor = (object) new XRTextureDescriptor();
                        // texDescriptor.XRTextureDescriptor_NativeTexture(tex.GetNativeTexturePtr());
                        texDescriptor.XRTextureDescriptor_Width(tex.width);
                        texDescriptor.XRTextureDescriptor_Height(tex.height);
                        texDescriptor.XRTextureDescriptor_TextureFormat(tex.format);
                        texDescriptor.XRTextureDescriptor_MipmapCount(tex.mipmapCount);              
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                        texDescriptor.XRTextureDescriptor_Dimension(tex.dimension);
#endif
                        cachedDescriptor = (XRTextureDescriptor)texDescriptor;
                    }
                }

                if (cachedDescriptor.nativeTexture == IntPtr.Zero)
                {
                    return new NativeArray<XRTextureDescriptor>(0, allocator);;
                }
                var arr = new NativeArray<XRTextureDescriptor>(1, allocator) {[0] = cachedDescriptor};
                return arr;
            }
            

            private bool didWarnAboutMissingEnvironmentManager;
            private ProfilerMarker tryAcquireLatestCpuImageMarker = new ProfilerMarker("TryAcquireLatestCpuImage");

            // private static byte[] GetAcquiredImageBytes(int key)
            // {
            //     if (acquiredCpuImages.TryGetValue(key, out var val))
            //     {
            //         if (val.bytes != null) return val.bytes.ToArray();
            //         val.bytes = val.img.GetRawTextureData();
            //         acquiredCpuImages[key] = val;
            //         return val.bytes;
            //     }
            //     return null;
            // }
            
            /// <summary>
            /// Query for the latest native camera image.
            /// </summary>
            /// <param name="imageInfo">The metadata required to construct a <see cref="XRCameraImage"/></param>
            /// <returns>
            /// <c>true</c> if the camera image is acquired. Otherwise, <c>false</c>.
            /// </returns>
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            public static XRCpuImage.Format ExpectedCpuImageFormat = XRCpuImage.Format.Unknown;
            private static readonly Dictionary<int, (Texture2D img, NativeArray<byte> bytes, GCHandle handle, XRCpuImage.Plane[] planes, XRCpuImage.Cinfo info)> 
                acquiredCpuImages  = new Dictionary<int, (Texture2D, NativeArray<byte>, GCHandle, XRCpuImage.Plane[], XRCpuImage.Cinfo)>();
            
            public override bool TryAcquireLatestCpuImage(out XRCpuImage.Cinfo imageInfo)
            {
#else
            public static CameraImageFormat ExpectedCpuImageFormat = CameraImageFormat.Unknown;
            private static readonly Dictionary<int, (Texture2D img, NativeArray<byte> bytes, GCHandle handle, XRCameraImagePlane[] planes, CameraImageCinfo info)> acquiredCpuImages 
                = new Dictionary<int, (Texture2D img, NativeArray<byte> bytes, GCHandle, XRCameraImagePlane[], CameraImageCinfo info)>();

            public override bool TryAcquireLatestImage(out CameraImageCinfo imageInfo)
            {
#endif
                using (tryAcquireLatestCpuImageMarker.Auto())
                {
                    if (TryGetLatestImage(out var img, false))
                    {
                        var ptr = img.GetNativeTexturePtr().ToInt32();
                        // var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                        // var ptr = handle.AddrOfPinnedObject().ToInt32();
                        var cc = (int)GraphicsFormatUtility.GetComponentCount(img.graphicsFormat);
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                        imageInfo = new XRCpuImage.Cinfo(
                            ptr,
                            new Vector2Int(img.width, img.height),
                            cc, 
                            Time.time,
                            ExpectedCpuImageFormat
                        );
                        if (!acquiredCpuImages.ContainsKey(ptr))
                        {
                            acquiredCpuImages.Add(ptr, (img, img.GetRawTextureData<byte>(), new GCHandle(), new XRCpuImage.Plane[cc], imageInfo));
                        }
#else
                        imageInfo = new CameraImageCinfo(
                            ptr,
                            new Vector2Int(img.width, img.height),
                            cc, 
                            Time.time,
                            ExpectedCpuImageFormat
                        );
                        if (!acquiredCpuImages.ContainsKey(ptr))
                        {
                            acquiredCpuImages.Add(ptr, (img, img.GetRawTextureData<byte>(), new GCHandle(), new XRCameraImagePlane[cc], imageInfo));
                        }
#endif
                        return true;
                    }
                    if (!didWarnAboutMissingEnvironmentManager)
                    {
                        Debug.LogWarning("Missing " + nameof(SimulatedAREnvironmentManager) + " to get camera image");
                        didWarnAboutMissingEnvironmentManager = true;
                    }
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                    imageInfo = new XRCpuImage.Cinfo();
#else
                    imageInfo = new CameraImageCinfo();
#endif
                    return false;
                }
            }

#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            private ARSimulationCpuImageApi _cpuImageApi;
            public override XRCpuImage.Api cpuImageApi
            {
                get
                {
                    if (_cpuImageApi == null) _cpuImageApi = new ARSimulationCpuImageApi();
                    return _cpuImageApi;
                }
            }
            
            private class ARSimulationCpuImageApi : XRCpuImage.Api
            {
#endif
                /// <summary>
                /// Get the status of an existing asynchronous conversion request.
                /// </summary>
                /// <param name="requestId">The unique identifier associated with a request.</param>
                /// <returns>The state of the request.</returns>
                /// <seealso cref="ConvertAsync(int, XRCameraImageConversionParams)"/>
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                public override XRCpuImage.AsyncConversionStatus GetAsyncRequestStatus(int requestId)
                {
                    return XRCpuImage.AsyncConversionStatus.Ready;
                }
#else
                public override AsyncCameraImageConversionStatus GetAsyncRequestStatus(int requestId)
                {
                    return AsyncCameraImageConversionStatus.Ready;
                }
#endif

                /// <summary>
                /// Dispose an existing native image identified by <paramref name="nativeHandle"/>.
                /// </summary>
                /// <param name="nativeHandle">A unique identifier for this camera image.</param>
                /// <seealso cref="TryAcquireLatestImage"/>
                public override void DisposeImage(int nativeHandle)
                {
                    OnDisposeImage(nativeHandle, true);
                }

                private static void OnDisposeImage(int nativeHandle, bool removeKey)
                {
                    if (!acquiredCpuImages.ContainsKey(nativeHandle)) return;
                    var entry = acquiredCpuImages[nativeHandle];
                    if(removeKey)
                        acquiredCpuImages.Remove(nativeHandle);
                    if (entry.handle.IsAllocated)
                        entry.handle.Free();
                    // disposing the original image native array caused "Invalid allocator" exception
                    // if (entry.bytes.IsCreated)  
                    //     entry.bytes.Dispose();
                    for (var index = 0; index < entry.planes.Length; index++)
                    {
                        var plane = entry.planes[index];
                        if (plane.data.IsCreated)
                        {
                            plane.data.Dispose();
                        }
                    }
                }

                /// <summary>
                /// Dispose an existing async conversion request.
                /// </summary>
                /// <param name="requestId">A unique identifier for the request.</param>
                /// <seealso cref="ConvertAsync(int, XRCameraImageConversionParams)"/>
                public override void DisposeAsyncRequest(int requestId)
                {
                }

                private NativeArray<byte> GetImageBytes(NativeArray<byte> data, int channel, int totalChannels, Allocator alloc)
                {
                    var channelData = new byte[data.Length / totalChannels];
                    var index = 0;
                    for (var i = channel; i < data.Length; i += totalChannels)
                    {
                        var val = data[i];
                        channelData[index] = val;
                        ++index;
                    }
                    return new NativeArray<byte>(channelData, alloc);
                }

                private ProfilerMarker tryGetPlaneMarker = new ProfilerMarker(nameof(TryGetPlane));
                /// <summary>
                /// Get information about an image plane from a native image handle by index.
                /// </summary>
                /// <param name="nativeHandle">A unique identifier for this camera image.</param>
                /// <param name="planeIndex">The index of the plane to get.</param>
                /// <param name="planeCinfo">The returned camera plane information if successful.</param>
                /// <returns>
                /// <c>true</c> if the image plane was acquired. Otherwise, <c>false</c>.
                /// </returns>
                /// <seealso cref="TryAcquireLatestImage"/>
                public override bool TryGetPlane(
                    int nativeHandle,
                    int planeIndex,
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                    out XRCpuImage.Plane.Cinfo planeCinfo)
#else
                    out CameraImagePlaneCinfo planeCinfo)
#endif
                {
                    using (tryGetPlaneMarker.Auto())
                    {
                        if (acquiredCpuImages.ContainsKey(nativeHandle))
                        {
                            var info = acquiredCpuImages[nativeHandle];
                            var planes = info.planes;
                            if (planeIndex < 0 || planeIndex >= planes.Length)
                                throw new ArgumentOutOfRangeException("Tried getting plane at index " + planeIndex + " but only " + planes.Length +
                                                                      " planes exist");
                            
                            var plane = info.planes[planeIndex];
                            if (!plane.data.IsCreated)
                            {
                                // Allocator persistent otherwise access to bytes will cause invalid exception because already disposed
                                var bytes = GetImageBytes(info.bytes, planeIndex, info.planes.Length, Allocator.Persistent);
                                var pixelStride = info.img.CalcPixelStride(info.bytes.Length);
                                var rowStride = pixelStride * info.img.width;
    #if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                                plane = new XRCpuImage.Plane(rowStride, pixelStride, bytes);
    #else
                                plane = new XRCameraImagePlane();
                                object obj = plane;
                                obj.XRCameraImagePlaneHelper_NativeArray(bytes);
                                obj.XRCameraImagePlaneHelper_PixelStride(pixelStride);
                                obj.XRCameraImagePlaneHelper_RowStride(rowStride);
                                plane = (XRCameraImagePlane) obj;
    #endif
                                info.planes[planeIndex] = plane;
                            }
                            plane = info.planes[planeIndex];
                            unsafe
                            {
                                var ptr = (IntPtr)plane.data.GetUnsafePtr();
    #if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                                planeCinfo = new XRCpuImage.Plane.Cinfo(ptr, plane.data.Length, plane.rowStride, plane.pixelStride);
    #else
                                planeCinfo = new CameraImagePlaneCinfo(ptr, plane.data.Length, plane.rowStride, plane.pixelStride);
    #endif
                            }
                            return true;
                        }   

                        
    #if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                        planeCinfo = new XRCpuImage.Plane.Cinfo();
    #else
                        planeCinfo = new CameraImagePlaneCinfo();
    #endif
                        return false;
                    }
                }

                /// <summary>
                /// Determine whether a native image handle returned by <see cref="TryAcquireLatestImage"/> is currently
                /// valid. An image may become invalid if it has been disposed.
                /// </summary>
                /// <remarks>
                /// If a handle is valid, <see cref="TryConvert"/> and <see cref="TryGetConvertedDataSize"/> should not fail.
                /// </remarks>
                /// <param name="nativeHandle">A unique identifier for the camera image in question.</param>
                /// <returns><c>true</c>, if it is a valid handle. Otherwise, <c>false</c>.</returns>
                /// <seealso cref="DisposeImage"/>
                public override bool NativeHandleValid(int nativeHandle)
                {
                    return acquiredCpuImages.ContainsKey(nativeHandle);
                }

                private ProfilerMarker tryGetConvertedDataSizeMarker = new ProfilerMarker(nameof(TryGetConvertedDataSize));
                /// <summary>
                /// Get the number of bytes required to store an image with the given dimensions and <c>TextureFormat</c>.
                /// </summary>
                /// <param name="nativeHandle">A unique identifier for the camera image to convert.</param>
                /// <param name="dimensions">The dimensions of the output image.</param>
                /// <param name="format">The <c>TextureFormat</c> for the image.</param>
                /// <param name="size">The number of bytes required to store the converted image.</param>
                /// <returns><c>true</c> if the output <paramref name="size"/> was set.</returns>
                public override bool TryGetConvertedDataSize(
                    int nativeHandle,
                    Vector2Int dimensions,
                    TextureFormat format,
                    out int size)
                {
                    using (tryGetConvertedDataSizeMarker.Auto())
                    {
                        if (!acquiredCpuImages.ContainsKey(nativeHandle))
                        {
                            size = 0;
                            return false;
                        }
                        var info = acquiredCpuImages[nativeHandle];
                        var gf = GraphicsFormatUtility.GetGraphicsFormat(format, false);
                        var channels = GraphicsFormatUtility.GetComponentCount(gf);
                        var stride = info.img.CalcPixelStride(info.bytes.Length);
                        size = ((int)channels * dimensions.x * dimensions.y * stride);
                        return true;
                    }
                }
                
                
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                public override bool FormatSupported(XRCpuImage image, TextureFormat format)
                {
                    return true;
                }
#endif

                private ProfilerMarker tryGetBufferMarker = new ProfilerMarker(nameof(TryGetBuffer));
                private bool TryGetBuffer(
                    int nativeHandle,
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                    XRCpuImage.ConversionParams conversionParams,
#else
                    XRCameraImageConversionParams conversionParams,
#endif 
                    out IntPtr ptr, out int dataLength)
                {
                    using (tryGetBufferMarker.Auto())
                    {
                        if (!acquiredCpuImages.ContainsKey(nativeHandle))
                        {
                            ptr = IntPtr.Zero;
                            dataLength = 0;
                            return false;
                        }
                        // TODO: support conversion params
                        var info = acquiredCpuImages[nativeHandle];
                        unsafe
                        {
                            ptr = new IntPtr(info.bytes.GetUnsafePtr());
                            dataLength = info.bytes.Length;
                            return true;
                        }
                    }
                }

                private ProfilerMarker tryConvertMarker = new ProfilerMarker(nameof(TryConvert));
                /// <summary>
                /// Convert the image with handle <paramref name="nativeHandle"/> using the provided
                /// <paramref cref="conversionParams"/>.
                /// </summary>
                /// <param name="nativeHandle">A unique identifier for the camera image to convert.</param>
                /// <param name="conversionParams">The parameters to use during the conversion.</param>
                /// <param name="destinationBuffer">A buffer to write the converted image to.</param>
                /// <param name="bufferLength">The number of bytes available in the buffer.</param>
                /// <returns>
                /// <c>true</c> if the image was converted and stored in <paramref name="destinationBuffer"/>.
                /// </returns>
                public override unsafe bool TryConvert(
                    int nativeHandle,
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                    XRCpuImage.ConversionParams conversionParams,
#else
                    XRCameraImageConversionParams conversionParams,
#endif
                    IntPtr destinationBuffer,
                    int bufferLength)
                {
                    using (tryConvertMarker.Auto())
                    {
                        if (!acquiredCpuImages.ContainsKey(nativeHandle)) return false;
                        try
                        {
                            // TODO: support conversion params
                            var info = acquiredCpuImages[nativeHandle];
                            UnsafeUtility.MemCpy(destinationBuffer.ToPointer(), info.bytes.GetUnsafePtr(), sizeof(byte) * bufferLength);
                            // Marshal.Copy(info.bytes.ToArray(), 0, destinationBuffer, bufferLength);
                            return true;
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }

                        return false;
                    }
                }
                
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                private readonly Dictionary<int, (int handle, XRCpuImage.ConversionParams convParams)> asyncRequests = new Dictionary<int, (int, XRCpuImage.ConversionParams)>();
#else
                private readonly Dictionary<int, (int handle,XRCameraImageConversionParams convParams)> asyncRequests = new Dictionary<int, (int, XRCameraImageConversionParams)>();
#endif

                private int requestCounter;

                /// <summary>
                /// Create an asynchronous request to convert a camera image, similar to <see cref="TryConvert"/> except
                /// the conversion should happen on a thread other than the calling (main) thread.
                /// </summary>
                /// <param name="nativeHandle">A unique identifier for the camera image to convert.</param>
                /// <param name="conversionParams">The parameters to use during the conversion.</param>
                /// <returns>A unique identifier for this request.</returns>
                public override int ConvertAsync(
                    int nativeHandle,
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                    XRCpuImage.ConversionParams conversionParams)
#else
                    XRCameraImageConversionParams conversionParams)
#endif
                {
                    requestCounter += 1;
                    if (!asyncRequests.ContainsKey(requestCounter)) asyncRequests.Add(requestCounter, (nativeHandle, conversionParams));
                    return requestCounter;
                }

                /// <summary>
                /// Get a pointer to the image data from a completed asynchronous request. This method should only succeed
                /// if <see cref="GetAsyncRequestStatus"/> returns <see cref="AsyncCameraImageConversionStatus.Ready"/>.
                /// </summary>
                /// <param name="requestId">The unique identifier associated with a request.</param>
                /// <param name="dataPtr">A pointer to the native buffer containing the data.</param>
                /// <param name="dataLength">The number of bytes in <paramref name="dataPtr"/>.</param>
                /// <returns><c>true</c> if <paramref name="dataPtr"/> and <paramref name="dataLength"/> were set and point
                ///  to the image data.</returns>
                public override bool TryGetAsyncRequestData(int requestId, out IntPtr dataPtr, out int dataLength)
                {
                    if (!asyncRequests.ContainsKey(requestId))
                    {
                        dataPtr = IntPtr.Zero;
                        dataLength = 0;
                        return false;
                    }
                    var req = asyncRequests[requestId];
                    if (TryGetBuffer(req.handle, req.convParams, out dataPtr, out dataLength))
                    {
                        return TryConvert(req.handle, req.convParams, dataPtr, dataLength);
                    }
                    return false;
                }

                /// <summary>
                /// Similar to <see cref="ConvertAsync(int, XRCameraImageConversionParams)"/> but takes a delegate to
                /// invoke when the request is complete, rather than returning a request id.
                /// </summary>
                /// <remarks>
                /// If the first parameter to <paramref name="callback"/> is
                /// <see cref="AsyncCameraImageConversionStatus.Ready"/> then the <c>dataPtr</c> parameter must be valid
                /// for the duration of the invocation. The data may be destroyed immediately upon return. The
                /// <paramref name="context"/> parameter must be passed back to the <paramref name="callback"/>.
                /// </remarks>
                /// <param name="nativeHandle">A unique identifier for the camera image to convert.</param>
                /// <param name="conversionParams">The parameters to use during the conversion.</param>
                /// <param name="callback">A delegate which must be invoked when the request is complete, whether the
                /// conversion was successfully or not.</param>
                /// <param name="context">A native pointer which must be passed back unaltered to
                /// <paramref name="callback"/>.</param>
                public override void ConvertAsync(
                    int nativeHandle,
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                    XRCpuImage.ConversionParams conversionParams,
                    XRCpuImage.Api.OnImageRequestCompleteDelegate callback,
#else
                    XRCameraImageConversionParams conversionParams,
                    OnImageRequestCompleteDelegate callback,
#endif
                    IntPtr context)
                {
                    if (TryGetBuffer(nativeHandle, conversionParams, out var ptr, out var length))
                    {
                        var res = TryConvert(nativeHandle, conversionParams, ptr, length);
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                        var status = res ? XRCpuImage.AsyncConversionStatus.Ready : XRCpuImage.AsyncConversionStatus.Failed;
#else
                        var status = res ? AsyncCameraImageConversionStatus.Ready : AsyncCameraImageConversionStatus.Failed;
#endif
                        callback(status, conversionParams, ptr, length, context);
                    }
                }
#if UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            }
#endif

#if UNITY_AR_FOUNDATION_4_OR_NEWER
            private Feature _currentLightEstimation, _requestedLightEstimation;
            public override Feature currentLightEstimation => _currentLightEstimation;

            public override Feature requestedLightEstimation
            {
                get => _requestedLightEstimation;
                set => _requestedLightEstimation = _currentLightEstimation = value;
            }
#endif
        }

        private static bool TryGetCurrentConfiguration(out XRCameraConfiguration configuration)
        {
            //Debug.Log("Not implemented/supported yet: " + nameof(TryGetCurrentConfiguration));
            configuration = new XRCameraConfiguration();
            return false;
        }
    }
}
