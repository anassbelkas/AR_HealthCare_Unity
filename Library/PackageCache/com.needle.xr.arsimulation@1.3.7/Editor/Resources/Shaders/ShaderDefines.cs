#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Rendering;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Needle.XR.ARSimulation.Simulation
{
    internal class ShaderDefines : IPreprocessShaders
    {
        private string pathSelf;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            #if UNITY_URP
            for (var i = 0; i < data.Count; i++)
            {
                var entry = data[i];
                if (ARSimulationProjectInfo.CurrentRenderPipeline == CurrentRenderPipelineType.Universal)
                    entry.shaderKeywordSet.Enable(new ShaderKeyword(shader, "_AR_SIMULATION_URP"));
                data[i] = entry;
            }
            #endif
        }

        public int callbackOrder { get; }
    }
}
#endif