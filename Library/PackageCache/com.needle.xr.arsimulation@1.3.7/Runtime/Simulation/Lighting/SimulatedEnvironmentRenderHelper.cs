using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Needle.XR.ARSimulation.Simulation
{
	public class SimulatedEnvironmentRenderHelper : MonoBehaviour
	{
		public bool Active = true;

		public class RenderEventArgs : EventArgs
		{
			public Camera Camera;
			public bool IsSimulatedEnvironmentCamera;
		}

		public static event Action<RenderEventArgs> BeforeRender, AfterRender;

		private int requireReset;

		private void Start()
		{
			hideFlags = HideFlags.None;
		}

		private void OnEnable()
		{
			requireReset = Time.frameCount;
			original.Read();
#if UNITY_URP
			RenderPipelineManager.beginCameraRendering += OnBegin;
			RenderPipelineManager.endCameraRendering += OnEnd;
#else
			Camera.onPreRender += OnBegin;
			Camera.onPostRender += OnEnd;
#endif
		}

		private void OnDisable()
		{
#if UNITY_URP
			RenderPipelineManager.beginCameraRendering -= OnBegin;
			RenderPipelineManager.endCameraRendering -= OnEnd;
#else
			Camera.onPreRender -= OnBegin;
			Camera.onPostRender -= OnEnd;
#endif
		}

#if UNITY_URP
		private void OnBegin(ScriptableRenderContext arg1, Camera arg2)
		{
			OnBegin(arg2);
		}

		private void OnEnd(ScriptableRenderContext arg1, Camera arg2)
		{
			OnEnd(arg2);
		}
#endif

		[System.Serializable]
		private class AmbientState
		{
			public float ambientIntensity;
			public AmbientMode mode;
			public Color light;
			public SphericalHarmonicsL2 probe;
			public Material skybox;

			public void Read()
			{
				ambientIntensity = RenderSettings.ambientIntensity;
				mode = RenderSettings.ambientMode;
				light = RenderSettings.ambientLight;
				probe = RenderSettings.ambientProbe;
				skybox = RenderSettings.skybox;
			}

			public void Write()
			{
				RenderSettings.ambientIntensity = ambientIntensity;
				RenderSettings.ambientMode = mode;
				RenderSettings.ambientLight = light;
				RenderSettings.ambientProbe = probe;
				RenderSettings.skybox = skybox;
			}
		}

		[SerializeField] private AmbientState original = new AmbientState();
		private readonly AmbientState state = new AmbientState();

		private void OnBegin(Camera cam)
		{
			if (!SimulatedAREnvironmentManager.Exists) return;
			var isSimulatedEnvironmentCamera = cam == SimulatedAREnvironmentManager.Instance.environmentCamera;
			BeforeRender?.Invoke(new RenderEventArgs() {Camera = cam, IsSimulatedEnvironmentCamera = isSimulatedEnvironmentCamera});
			if (!Active) return;
			if (!isSimulatedEnvironmentCamera) return;
			SetToOriginal();
		}

		private void OnEnd(Camera cam)
		{
			if (!SimulatedAREnvironmentManager.Exists) return;
			var isSimulatedEnvironmentCamera = cam == SimulatedAREnvironmentManager.Instance.environmentCamera;
			AfterRender?.Invoke(new RenderEventArgs() {Camera = cam, IsSimulatedEnvironmentCamera = isSimulatedEnvironmentCamera});
			if (!Active) return;
			if (!isSimulatedEnvironmentCamera) return;
			Write();
		}


		[ContextMenu(nameof(SetToOriginal))]
		private void SetToOriginal()
		{
			if (requireReset > 0)
			{
				if (Time.frameCount - requireReset > 1)
				{
					requireReset = 0;
					original.Read();
				}
			}
			
			state.Read();
			if (RenderSettings.skybox && !original.skybox)
				original.skybox = RenderSettings.skybox;
			original.Write();
		}

		private void Write()
		{
			state.Write();
		}
	}
}