using System;
using UnityEngine;
using Needle.XR.ARSimulation;
using Random = UnityEngine.Random;

namespace Needle.XR.ARSimulation.ExampleComponents
{
    /// <summary>
    /// Simple color provider component used by examples/samples
    /// </summary>
    public class RandomColor : MonoBehaviour
    {
        public Color[] Cols = new[]
        {
            UnityEngine.Color.red, UnityEngine.Color.blue, UnityEngine.Color.yellow, UnityEngine.Color.green
        };

        private static readonly int Color = Shader.PropertyToID("_Color");
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public Renderer[] Renderers = new Renderer[0];

        private void OnValidate()
        {
            if (Renderers == null || Renderers.Length <= 0)
            {
                Renderers = new[] {this.GetComponent<Renderer>()};
            }
        }

        private void Start()
        {
            // var rend = this.GetComponent<Renderer>();
            // if (!rend) return;
            var block = new MaterialPropertyBlock();
            var col = Cols[(int) (Random.value * Cols.Length)];
            foreach (var rend in Renderers)
            {
                if (!rend) continue;
                var id = ARSimulationProjectInfo.CurrentRenderPipeline == CurrentRenderPipelineType.Builtin ? Color : BaseColor;
                block.SetColor(id, col);
                rend.SetPropertyBlock(block);
            }
        }
    }
}