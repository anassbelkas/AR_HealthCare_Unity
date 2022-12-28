using System;
using System.Collections.Generic;
using UnityEngine;

namespace Needle.XR.ARSimulation.Compatibility
{
    [ExecuteAlways]
    public class AutoMaterial : MonoBehaviour
    {
        public bool UpdateChildren = false;
#pragma warning disable 109
#pragma warning disable CS0414
        private new Renderer renderer = null;
#pragma warning restore

        [NonSerialized]
        public List<string> ShaderNames = new List<string>()
        {
            "Hidden/InternalErrorShader",
            "Standard",
            "Particles/Standard Unlit",
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Particles/Unlit",
        };

#if UNITY_EDITOR
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

        private void OnEnable()
        {
            if (!renderer)
                renderer = GetComponent<Renderer>();
            ARSimulationProjectInfo.RenderPipelineChanged += OnRenderPipelineChanged;
            var pipeline = ARSimulationProjectInfo.CurrentRenderPipeline;
            UpdateMaterials(pipeline, pipeline);
        }

        private void OnDisable()
        {
            ARSimulationProjectInfo.RenderPipelineChanged -= OnRenderPipelineChanged;
        }

        private void OnValidate()
        {
            if (!renderer)
                renderer = GetComponent<Renderer>();
        }

        private void OnRenderPipelineChanged((CurrentRenderPipelineType previous, CurrentRenderPipelineType current) obj)
        {
            UpdateMaterials(obj.previous, obj.current);
        }

        [ContextMenu(nameof(UpdateMaterials))]
        internal void UpdateMaterials()
        {
            UpdateMaterials(ARSimulationProjectInfo.CurrentRenderPipeline, ARSimulationProjectInfo.CurrentRenderPipeline);
        }
        
        private void UpdateMaterials(CurrentRenderPipelineType previous, CurrentRenderPipelineType current)
        {
            if (UpdateChildren)
            {
                var childRenderer = this.GetComponentsInChildren<Renderer>(true);
                foreach (var ch in childRenderer) UpdateMaterials(ch, previous, current);
            }
            else
            {
                UpdateMaterials(renderer, previous, current);
            }
        }

        private void UpdateMaterials(Renderer rend, CurrentRenderPipelineType previous, CurrentRenderPipelineType current)
        {
            if (!rend) return;
            var materials = rend.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                var upgraded = UpdateMaterial(mat, previous, current);
                if (upgraded && upgraded != mat)
                {
                    materials[i] = upgraded;
                }
            }

            rend.sharedMaterials = materials;
        }


        // private readonly Dictionary<Material, Material> materialCache = new Dictionary<Material, Material>();
        
        private Material UpdateMaterial(Material mat, CurrentRenderPipelineType previous, CurrentRenderPipelineType current)
        {
            if (!mat) return null;
            var currentShader = mat.shader;
            if (!ShaderNames.Contains(currentShader.name)) return mat;

            // if (!materialCache.ContainsKey(mat) || !materialCache[mat]) materialCache.Add(mat, new Material(mat));
            // mat = materialCache[mat];
            
            var queue = mat.renderQueue;
            Shader shader = null;

            switch (current)
            {
                case CurrentRenderPipelineType.Builtin:
                    if (currentShader.name.Contains("Particle"))
                    {
                        shader = Shader.Find("Particles/Standard Unlit");
                        mat.SetFloat(DstBlend, 10);
                    }
                    else
                        shader = Shader.Find("Standard");

                    break;

                case CurrentRenderPipelineType.Universal:
                    if (currentShader.name.Contains("Particle"))
                    {
                        shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                        mat.SetFloat(DstBlend, 10);
                    }
                    else
                        shader = Shader.Find("Universal Render Pipeline/Lit");

                    break;
                case CurrentRenderPipelineType.HighDefinition:
                    break;
                case CurrentRenderPipelineType.Unknown:
                    break;
            }

            if (shader != null && shader != mat.shader)
            {
                mat.shader = shader;
                mat.renderQueue = queue;

                mat.SetMainTex(current);
                var color = mat.GetColor(previous, out var hasColor);
                if (hasColor)
                    mat.SetColor(current, color);
                var smoothness = mat.GetSmoothness(previous, out var hasSmoothness);
                if (hasSmoothness)
                    mat.SetSmoothness(current, smoothness);
                for (var i = 0; i < mat.passCount; i++) UnityEditor.ShaderUtil.CompilePass(mat, i);
            }


            return mat;
        }
    }


    public static class PropertyMapping
    {
        private static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int Glossiness = Shader.PropertyToID("_GlossMapScale");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Color = Shader.PropertyToID("_Color");

        public static Color GetColor(this Material mat, CurrentRenderPipelineType pipeline, out bool hasColor)
        {
            hasColor = true;
            switch (pipeline)
            {
                case CurrentRenderPipelineType.Builtin:
                    if (mat.HasProperty(Color))
                        return mat.GetColor(Color);
                    break;
                // previous was urp
                default:
                    if (mat.HasProperty(BaseColor))
                        return mat.GetColor(BaseColor);
                    break;
            }

            hasColor = false;
            return UnityEngine.Color.black;
        }

        public static void SetColor(this Material mat, CurrentRenderPipelineType pipeline, Color color)
        {
            switch (pipeline)
            {
                case CurrentRenderPipelineType.Builtin:
                    mat.SetColor(Color, color);
                    break;
                // previous was urp
                default:
                    mat.SetColor(BaseColor, color);
                    break;
            }
        }

        public static float GetSmoothness(this Material mat, CurrentRenderPipelineType pipeline, out bool hasSmoothness)
        {
            hasSmoothness = true;
            switch (pipeline)
            {
                case CurrentRenderPipelineType.Builtin:
                    if (mat.HasProperty(Glossiness))
                        return mat.GetFloat(Glossiness);
                    break;
                // previous was urp
                default:
                    if (mat.HasProperty(Smoothness))
                        return mat.GetFloat(Smoothness);
                    break;
            }

            hasSmoothness = false;
            return -1;
        }

        public static void SetSmoothness(this Material mat, CurrentRenderPipelineType pipeline, float smoothness)
        {
            switch (pipeline)
            {
                case CurrentRenderPipelineType.Builtin:
                    mat.SetFloat(Glossiness, smoothness);
                    break;
                // previous was urp
                default:
                    mat.SetFloat(Smoothness, smoothness);
                    break;
            }
        }

        public static void SetMainTex(this Material mat, CurrentRenderPipelineType pipeline)
        {
            mat.SetTexture(MainTex(pipeline), mat.mainTexture);
        }

        public static string MainTex(CurrentRenderPipelineType pipelineType, Shader shader = null)
        {
            switch (pipelineType)
            {
                case CurrentRenderPipelineType.Builtin:
                    return "_MainTex";

                case CurrentRenderPipelineType.Universal:
                    return "_BaseMap";
            }

            return null;
        }
#endif
    }
}