#if UNITY_URP
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Needle.XR.ARSimulation.Simulation;
using UnityEditor;
using UnityEngine.XR.ARFoundation;

namespace Needle.XR.ARSimulation.Compatibility
{
#if UNITY_URP_12_0_OR_NEWER
    using URPRendererData = UniversalRendererData;
#else
    using URPRendererData = ForwardRendererData;
#endif
    
    public class ARSimulationCameraBackgroundRendererFeature : ScriptableRendererFeature
    {
        #if UNITY_EDITOR
        internal static class AutomaticSupport
        {
            // [InitializeOnLoadMethod]
            // private static void Init() => Run();
            
            internal static bool Run()
            {
                var guid = AssetDatabase.FindAssets("t:" + nameof(URPRendererData)).FirstOrDefault();
                if (string.IsNullOrEmpty(guid))
                    return false;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) 
                    return false;
                var renderer = AssetDatabase.LoadAssetAtPath<URPRendererData>(path);
                if (!renderer) return false;

                if (renderer.rendererFeatures.Any(f => f && f.GetType() == typeof(ARSimulationCameraBackgroundRendererFeature)))
                {
                    // Debug.Log(nameof(ARSimulationCameraBackgroundRendererFeature) + " is already assigned to " + renderer.name, renderer);
                    return true;
                }

                var sceneHasSimulatedEnvironmentManager = FindObjectOfType<SimulatedAREnvironmentManager>();
                if (!sceneHasSimulatedEnvironmentManager) return false;
                
                var instance = CreateInstance<ARSimulationCameraBackgroundRendererFeature>();
                instance.name = nameof(ARSimulationCameraBackgroundRendererFeature);
                AssetDatabase.AddObjectToAsset(instance, renderer);
                renderer.rendererFeatures.Add(instance);
                instance.Create();
                EditorUtility.SetDirty(renderer);
                AssetDatabase.SaveAssets();
                return true;
            }
        }
        #endif
        
        private class RenderCameraImage : ScriptableRenderPass
        {
            /// <summary>
            /// The name for the custom render pass which will be display in graphics debugging tools.
            /// </summary>
            private const string k_CustomRenderPassName = "AR Simulation Background Pass (URP)";

            private Texture m_Texture;
            private RenderTargetIdentifier m_ColorTargetIdentifier;
            // private RenderTargetIdentifier m_DepthTargetIdentifier;
            private int m_CullingMask;
            private Material m_Material;

            public RenderCameraImage(RenderPassEvent evt)
            {
                this.renderPassEvent = evt;
            }

            public void Setup(Texture tex, RenderTargetIdentifier colorTargetIdentifier, Material material)
            {
                m_Texture = tex;
                m_ColorTargetIdentifier = colorTargetIdentifier;
                // m_DepthTargetIdentifier = depthTargetIdentifier;
                m_Material = material;
            }

            /// <summary>
            /// Configure the render pass by configuring the render target and clear values.
            /// </summary>
            /// <param name="commandBuffer">The command buffer for configuration.</param>
            /// <param name="renderTextureDescriptor">The descriptor of the target render texture.</param>
            public override void Configure(CommandBuffer commandBuffer, RenderTextureDescriptor renderTextureDescriptor)
            {
                // ConfigureTarget(m_ColorTargetIdentifier, m_DepthTargetIdentifier);
                ConfigureClear(ClearFlag.Depth, Color.clear);
            }

            /// <summary>
            /// Execute the render commands to blit the camera background texture.
            /// </summary>
            /// <param name="context">The render context for executing the render commands.</param>
            /// <param name="renderingData">Additional rendering data about the current state of rendering.</param>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get(k_CustomRenderPassName);
                // ARCameraBackground.AddOpenGLES3ResetStateCommand(cmd);
                // cmd.SetInvertCulling(m_InvertCulling);
                if (m_Material)
                    Blit(cmd, m_Texture, m_ColorTargetIdentifier, m_Material);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        private RenderCameraImage renderImage;
        private SimulatedAREnvironmentManager realitySim;
        private Material blitMaterial;

        [NonSerialized] public bool noRealitySimInSceneFound;

        public override void Create()
        {
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
            renderImage = new RenderCameraImage(RenderPassEvent.BeforeRenderingOpaques);
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
            if (Application.isPlaying && noRealitySimInSceneFound) return;

            if (!realitySim) realitySim = FindObjectOfType<SimulatedAREnvironmentManager>();
            if (!realitySim) 
            {
                if (!Application.isPlaying && !noRealitySimInSceneFound && !Application.isBatchMode)
                {
                    if(FindObjectOfType<ARCameraManager>())
                        Debug.LogWarning("<b>[AR Simulation]</b> Can't render simulated camera background because no " + nameof(SimulatedAREnvironmentManager) + " component in scene found", this);
                }
                noRealitySimInSceneFound = true;
                return;
            }

            if (!blitMaterial) blitMaterial = ARSimulationProjectInfo.CreateRenderCameraImageMaterial();

            var currentCamera = renderingData.cameraData.camera;
            if ((currentCamera != null) && (currentCamera.cameraType == CameraType.Game))
            {
                var prov = realitySim;
                renderImage.Setup(
                    prov.EnvironmentCameraRT, 
                    renderer.cameraColorTarget, 
                    // #if UNITY_URP_10_1_OR_NEWER
                    // renderer.cameraDepthTarget, 
                    // #else       
                    // renderer.cameraDepth,
                    // #endif
                    blitMaterial);
                renderer.EnqueuePass(renderImage);
            }
#endif
        }
    }
}
#endif