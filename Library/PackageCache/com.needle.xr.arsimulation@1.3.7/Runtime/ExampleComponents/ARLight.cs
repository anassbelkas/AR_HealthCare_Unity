using System.Collections;
using Needle.XR.ARSimulation.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

#pragma warning disable 108,114

namespace ExampleComponents
{
	[RequireComponent(typeof(Light))]
	public class ARLight : MonoBehaviour
	{
		public bool ControlAmbience = true;
		public AmbientMode AmbientMode = AmbientMode.Flat;
		
		private Light light;
		private ARCameraManager camera;
		private ARCameraFrameEventArgs lastFrameReceived;
		private SimulatedARCameraFrameDataProvider prov;
		
		private void OnValidate()
		{
			if (!light && !TryGetComponent(out light)) return;
			light.type = LightType.Directional;
			light.cullingMask = SimulatedAREnvironmentManager.ExcludeEnvironment(light.cullingMask);
		}

		private IEnumerator Start()
		{
			// if a scene has no camera data component at the start we need to wait a frame, because it will be added at runtime by the scene setup
			yield return null;
			prov = FindObjectOfType<SimulatedARCameraFrameDataProvider>();
			if (!prov || (!prov.EstimateLight && !prov.OverrideLightData))
			{
				Debug.LogWarning("Enable Estimate Light or assign a light as data source to " + nameof(SimulatedARCameraFrameDataProvider), prov);
			}
		}

		private void OnEnable()
		{
			camera = FindObjectOfType<ARCameraManager>();
			if (!camera)
			{
				enabled = false;
				return;
			}
			camera.frameReceived += OnFrame;
			SimulatedEnvironmentRenderHelper.BeforeRender += OnBeforeRender;
			
			if(!prov)
				prov = FindObjectOfType<SimulatedARCameraFrameDataProvider>();
		}

		private void OnDisable()
		{
			if (!camera) return;
			camera.frameReceived -= OnFrame;
			SimulatedEnvironmentRenderHelper.BeforeRender -= OnBeforeRender;
		}

		private void OnFrame(ARCameraFrameEventArgs obj)
		{
			lastFrameReceived = obj;

			if (!light && !TryGetComponent(out light))
			{
				enabled = false;
				return;
			}
			
			if (!prov || !prov.HasLightData) return;

			var est = obj.lightEstimation;
#if UNITY_AR_FOUNDATION_4_OR_NEWER
			light.color = est.mainLightColor.GetValueOrDefault(Color.black);
			light.transform.forward = est.mainLightDirection.GetValueOrDefault(Vector3.down);
			light.intensity = est.averageMainLightBrightness.GetValueOrDefault(0);
#endif
			light.cullingMask = SimulatedAREnvironmentManager.ExcludeEnvironment(light.cullingMask);
		}
		
		private void OnBeforeRender(SimulatedEnvironmentRenderHelper.RenderEventArgs args)
		{
			if (!prov || !prov.HasLightData) return;
			if (args.IsSimulatedEnvironmentCamera) return;
			var est = lastFrameReceived.lightEstimation;
			if (!ControlAmbience) return;
			RenderSettings.ambientMode = AmbientMode;
			RenderSettings.ambientIntensity = est.averageBrightness.GetValueOrDefault(0);
			RenderSettings.ambientLight = est.colorCorrection.GetValueOrDefault(Color.black);
#if UNITY_AR_FOUNDATION_4_OR_NEWER
			if (!est.ambientSphericalHarmonics.HasValue) return;
			RenderSettings.ambientProbe = est.ambientSphericalHarmonics.Value;
#endif
		}

		private void OnAfterRender(SimulatedEnvironmentRenderHelper.RenderEventArgs args)
		{
			
		}


	}
}