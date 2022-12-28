#if UNITY_EDITOR

using UnityEngine;

namespace Needle.XR.ARSimulation.Simulation
{
	public class LightSamplerRayMarched
	{
		private static readonly RaycastHit[] _hitsBuffer = new RaycastHit[10];
		private RaySampleData _data;
		public float JitterAngle = 0;

		public RaySampleData Data => _data;

		public RaySampleData Run(Ray startRay, float startLength, int maxSteps, Light[] lights)
		{
			if (_data == null) _data = new RaySampleData();
			_data.Reset();
			March(startRay, _data, startLength, 0, maxSteps, lights);
			return _data;
		}

		public void March(Ray ray, RaySampleData data, float length, int step, int maxSteps, Light[] lights)
		{
			if (step > maxSteps || length <= .05f) return;
			var count = Physics.RaycastNonAlloc(ray, _hitsBuffer, length, SimulatedAREnvironmentManager.SimulatedEnvironmentMask,
				QueryTriggerInteraction.Ignore);
			if (count > 0)
			{
				var closestHit = _hitsBuffer[0];
				var closestDistance = float.MaxValue;
				for (var i = 1; i < count; i++)
				{
					var hit = _hitsBuffer[i];
					var hitDistance = Vector3.Distance(ray.origin, hit.point);
					if (hitDistance < closestDistance)
					{
						closestHit = hit;
						closestDistance = hitDistance;
					}
				}

				Debug.DrawLine(ray.origin, closestHit.point);
				data.Sample(closestHit.point, lights);
				var newRay = new Ray(closestHit.point, Vector3.Reflect(ray.direction, closestHit.normal));
				newRay.direction = SimulatedARCameraFrameDataProvider.JitterDirection(newRay.direction, JitterAngle);
				March(newRay, data, length * .5f, ++step, maxSteps, lights);
			}
			else
			{
				var pt = ray.origin + ray.direction * length;
				Debug.DrawLine(ray.origin, pt);
				data.Sample(pt, lights);
				var newRay = new Ray(pt, ray.direction);
				newRay.direction = SimulatedARCameraFrameDataProvider.JitterDirection(newRay.direction, JitterAngle);
				March(newRay, data, length * .8f, ++step, maxSteps, lights);
			}
		}

		public class RaySampleData
		{
			public void Reset()
			{
				Direction = Vector3.zero;
				Intensity = 0;
				Color = Color.black;
				Samples = 0;
				Factor = 0;
			}

			public Vector3 Direction;
			public float Intensity;
			public Color Color;
			public float Factor;
			public int Samples;

			private void Sample(float intensity, Color color, Vector3 lightVector, float factor)
			{
				Direction = Vector3.Lerp(Direction, lightVector.normalized, factor);
				Intensity = Mathf.Lerp(Intensity, intensity, factor);
				Color = Color.Lerp(Color, color, factor);
				Factor += factor;
				Samples += 1;
			}

			public void Sample(Vector3 point, Light[] lights)
			{
				foreach (var light in lights)
				{
					var lightTransform = light.transform;
					var lightPosition = lightTransform.position;
					switch (light.type)
					{
						case LightType.Directional:
							break;
						case LightType.Spot:
							break;
						case LightType.Point:
							var dist = Vector3.Distance(lightPosition, point);
							var t = 1 - (dist / light.range);
							t = Mathf.Clamp01(t);
							Sample(light.intensity, light.color, point - lightPosition, t);
							break;
						case LightType.Area:
							break;
						case LightType.Disc:
							break;
					}
				}
			}
		}
	}
}
#endif