using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Needle.XR.ARSimulation
{
    public enum CurrentRenderPipelineType
    {
        Builtin = 0,
        Universal = 1,
        HighDefinition = 2,
        Unknown = 3
    }

    public static class ARSimulationProjectInfo
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once StaticMemberInitializerReferesToMemberBelow
        public static CurrentRenderPipelineType CurrentRenderPipeline { get; private set; } = (CurrentRenderPipeline) - 1;
#pragma warning disable 414
        public static event Action<(CurrentRenderPipelineType previous, CurrentRenderPipelineType current)> RenderPipelineChanged = null;
#pragma warning restore

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            var cur = GetCurrentPipeline();
            if (cur != CurrentRenderPipeline)
            {
                var prev = CurrentRenderPipeline;
                CurrentRenderPipeline = cur;
                RenderPipelineChanged?.Invoke((previous: prev, current: CurrentRenderPipeline));
            }
        }

        public static CurrentRenderPipelineType GetCurrentPipeline()
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset;
            if (pipelineAsset == null)
            {
                return CurrentRenderPipelineType.Builtin;
            }

            switch (pipelineAsset)
            {
#if UNITY_URP
                case UniversalRenderPipelineAsset _:
                    return CurrentRenderPipelineType.Universal;
#endif
                default:
                    return CurrentRenderPipelineType.Unknown;
            }
        }
#endif


        internal static Material CreateRenderCameraImageMaterial()
        {
            switch (CurrentRenderPipeline)
            {
                case CurrentRenderPipelineType.Builtin:
                case CurrentRenderPipelineType.Universal:
                case CurrentRenderPipelineType.HighDefinition:
                case CurrentRenderPipelineType.Unknown:
                    return new Material(Shader.Find("Hidden/BlitCopy"));
            }

            return null;
        }
    }
}