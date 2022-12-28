#define LIGHTESTIMATION_DEVELOPMENT
#undef LIGHTESTIMATION_DEVELOPMENT

#if (UNITY_ARSUBSYSTEMS_3_1_0_PREVIEW_2_OR_NEWER && !UNITY_ARSUBSYSTEMS_3_1_3_OR_NEWER) || UNITY_ARSUBSYSTEMS_4_OR_NEWER
#define SPHERICALHARMONICS_SUPPORTED
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Simulation
{
	/// <summary>
	/// Used to pass camera data to AR Foundation like <see cref="AverageBrightness"/> or <see cref="SphericalHarmonicsL2"/>
	/// </summary>
	[ExecuteAlways]
	public class SimulatedARCameraFrameDataProvider : MonoBehaviour
	{
#if UNITY_AR_FOUNDATION_4_OR_NEWER
		public bool HasLightData => (ARCameraManager && ARCameraManager.currentLightEstimation != LightEstimation.None);
		private bool ShouldSimulate(LightEstimation feature) => HasLightData && (ARCameraManager.currentLightEstimation & feature) != 0;
		private bool EstimateAmbientIntensity() => ShouldSimulate(LightEstimation.AmbientIntensity);
		private bool EstimateAmbientColor() => ShouldSimulate(LightEstimation.AmbientColor);
		private bool EstimateAmbientSphericalHarmonics() => ShouldSimulate(LightEstimation.AmbientSphericalHarmonics);
		private bool EstimateMainLightDirection() => ShouldSimulate(LightEstimation.MainLightDirection);
		private bool EstimateMainLightIntensity() => ShouldSimulate(LightEstimation.MainLightIntensity);
#else
		public bool HasLightData => (EstimateLight || OverrideLightData) &&
		                            (ARCameraManager && ARCameraManager.lightEstimationMode != LightEstimationMode.Disabled);

		private bool ShouldSimulate(LightEstimationMode feature) => HasLightData && (ARCameraManager.lightEstimationMode == feature);
		private bool EstimateAmbientIntensity() => ShouldSimulate(LightEstimationMode.AmbientIntensity);
		private bool EstimateAmbientColor() => ShouldSimulate(LightEstimationMode.AmbientIntensity);
#if SPHERICALHARMONICS_SUPPORTED
		private bool EstimateAmbientSphericalHarmonics() => ShouldSimulate(LightEstimationMode.EnvironmentalHDR);
		private bool EstimateMainLightDirection() => ShouldSimulate(LightEstimationMode.EnvironmentalHDR);
		private bool EstimateMainLightIntensity() => ShouldSimulate(LightEstimationMode.EnvironmentalHDR);
#endif
#endif

		/// <summary>
		/// The AR camera tracking state
		/// </summary>
		public TrackingState TrackingState = TrackingState.Tracking;


		public Matrix4x4 ProjectionMatrix
		{
			get
			{
#if UNITY_EDITOR
				if (projectionMatrix == Matrix4x4.zero)
					UpdateProjectionMatrix();
#endif
				return projectionMatrix;
			}
			set => projectionMatrix = value;
		}

		public Matrix4x4 DisplayMatrix = Matrix4x4.identity;

		[Header("Dynamic Estimation (Experimental)")]
		[Tooltip(
			"If enabled ARSimulation will try to sample light data from scene such as average brightness, ambient color, light direction, color. This data will be available via ARCameraManager.FrameReceived event and can be used to control ambient light and light direction. You can use the ARLight component for getting started.")]
		public bool EstimateLight = true;

		[Range(.2f, 2)] public float AdaptationDuration = 2f;
		[Range(0, 10)] public int SamplesPerLight = 3;
		[Range(0, 30)] public int SamplesPerDirectionalLight = 10;
		[Range(0, 1)] public float OcclusionFactor = .8f;
		[Range(0, 90)] public float RandomAngleRange = 70;
		[Range(0, 1)] public float SunlightBias = 1;
		[Range(0.1f, 2)] public float IntensityFactor = 1;


		[Header("Overrides")]
		[FormerlySerializedAs("InputLight")]
		[FormerlySerializedAs("MainLight")]
		[Tooltip("If you want to set fixed/custom lighting assign a light here to fetch data from.\n" +
		         "This will stop dynamically estimating light")]
		public Light OverrideLightData;

		public float MainLightIntensityLumen { get; set; } = 1000;
		public Color MainLightColor { get; set; } = Color.white;
		public Vector3 MainLightDirection { get; set; }

		public float AverageBrightness { get; set; } = 1;
		public Color ColorCorrection { get; set; } = Color.white;
		public float ColorTemperature { get; set; }
		public double ExposureDuration { get; set; }
		public float ExposureOffset { get; set; }

		public void SetDefaultLightValues()
		{
			MainLightIntensityLumen = 1000;
			MainLightColor = Color.white;
			ColorCorrection = Color.white;
			AverageBrightness = 1;
		}

#if SPHERICALHARMONICS_SUPPORTED // added in 3.1.0-preview2, removed in 3.1.3 https://github.com/needle-mirror/com.unity.xr.arsubsystems/commit/928aca5600531b8a4eb7562c6a5f46ec3b580ba7
		/// <summary>
		/// Spherical Harmonics to be passed to ARFoundation.
		/// They get automatically calculated from the light assigned to DirectionalLightToSetSphericalHarmonicsFrom.
		/// If no light is assigned it will use the first directional light it finds in the scene
		/// </summary>
		[HideInInspector] public SphericalHarmonicsL2 SphericalHarmonicsL2; // Bug? If this is a property this does not work
#endif

		private ARCameraManager _arCameraManager;

		private ARCameraManager ARCameraManager
		{
			get
			{
				if (!_arCameraManager) _arCameraManager = FindObjectOfType<ARCameraManager>();
				return _arCameraManager;
			}
		}

#if UNITY_AR_FOUNDATION_4_OR_NEWER
		private bool UseSphericalHarmonics => ARCameraManager && (ARCameraManager.currentLightEstimation & LightEstimation.AmbientSphericalHarmonics) != 0;
#else
		private bool UseSphericalHarmonics => ARCameraManager && (ARCameraManager.lightEstimationMode != LightEstimationMode.Disabled);
#endif

		private Camera _arCamera;
		private Matrix4x4 projectionMatrix;

#if UNITY_EDITOR
		private void EnsureARCamera()
		{
			if (_arCamera) return;
			if (!SceneSetup.TryGetARCamera(out _arCamera))
				_arCamera = Camera.main;
			if (!_arCamera) enabled = false;
		}

		private void Start()
		{
			SetDefaultLightValues();
			EnsureARCamera();
		}

		private void OnEnable()
		{
			SimulatedAREnvironmentManager.SceneChangedOrRecreated += OnEnvironmentChanged;
		}

		private void OnDisable()
		{
			SimulatedAREnvironmentManager.SceneChangedOrRecreated -= OnEnvironmentChanged;
		}

		private void LateUpdate()
		{
			EnsureARCamera();
			UpdateProjectionMatrix();
#if SPHERICALHARMONICS_SUPPORTED
			SphericalHarmonicsL2.Clear();
#endif
			UpdateLightData();
#if SPHERICALHARMONICS_SUPPORTED
			SphericalHarmonicsL2.AddAmbientLight(ColorCorrection * AverageBrightness);
#endif
			if (MainLightDirection == Vector3.zero) MainLightDirection = Vector3.down;
		}

		private void UpdateProjectionMatrix()
		{
			EnsureARCamera();
			if (!_arCamera) return;
			if (Application.isPlaying)
			{
				if (_arCamera.usePhysicalProperties)
				{
					_arCamera.usePhysicalProperties = false;
					// Camera.CalculateProjectionMatrixFromPhysicalProperties(out ProjectionMatrix, _arCamera.focalLength, _arCamera.sensorSize, _arCamera.lensShift, _arCamera.nearClipPlane, _arCamera.farClipPlane, new Camera.GateFitParameters(_arCamera.gateFit, _arCamera.aspect));
				}
			}

			ProjectionMatrix = _arCamera.projectionMatrix;
		}


		private void UpdateLightData()
		{
			if (!Application.isPlaying) return;
			if (TryUpdateLightDataFromOverride()) return;
			if (!_arCamera) return;
			if (EstimateLight && TryUpdateLightDataFromLightsInSimulation()) return;
			SetDefaultLightValues();
		}

		private bool TryUpdateLightDataFromOverride()
		{
			if (!OverrideLightData) return false;
			ColorCorrection = MainLightColor = OverrideLightData.color;
			var t = OverrideLightData.transform;
			MainLightDirection = t.forward;
			var intensity = OverrideLightData.intensity;
			AverageBrightness = intensity * (1 - Mathf.Clamp01(Vector3.Dot(t.forward, Vector3.up)));
			MainLightIntensityLumen = LightHelpers.ConvertBrightnessToLumen(intensity);
			return true;
		}

		private bool TryUpdateLightDataFromLightsInSimulation()
		{
			if (!Application.isPlaying) return false;
			if (!_arCamera) return false;

			// estimate light by checking all lights in simulated environment
			// if simulated lights were lit we could use the lightmap https://gist.github.com/st4rdog/fefe61884292ca457bbe9395c5458749
			if (!TryEnsureLightsAndData(ref lightData)) return false;
			if (lightsInSimulatedEnvironment.Length <= 0) return false;

			// estimate influence per light
			var cameraTransform = _arCamera.transform;
			var cameraPosition = cameraTransform.position;
			var mask = SimulatedAREnvironmentManager.SimulatedEnvironmentMask;

			var adaptationStep = (Time.deltaTime / AdaptationDuration);
			var decreaseStep = adaptationStep * .25f;
			MainLightColor = Color.Lerp(MainLightColor, Color.black, decreaseStep);
			AverageBrightness = Mathf.Lerp(AverageBrightness, 0, decreaseStep);
			if (MainLightDirection == Vector3.zero)
				MainLightDirection = new Vector3(45, 45, 45);


			var brightness = AverageBrightness;
			var ambientColorSum = Color.black;
			int ambientSamplesCount = 0;
			float ambientIntensitiesSum = 0;

			// ReSharper disable once LocalVariableHidesMember
			for (var index = 0; index < lightsInSimulatedEnvironment.Length; index++)
			{
				var light = lightsInSimulatedEnvironment[index];
				if (!light) continue;
				if (!light.isActiveAndEnabled) continue;
				if (light.intensity <= .001f) continue;

				var data = lightData[index];
				data.Samples = (int) (AdaptationDuration * 60);

				var lightTransform = light.transform;
				var lightPos = lightTransform.position;
				var vecCameraToLight = lightPos - cameraPosition;
				var distanceToLight = vecCameraToLight.magnitude;

				var distanceHitsCount = 0;
				float summedHitDistances = 0;
				float coverage = 0;
				var occlusions = 0;
				var lightDirection = -lightTransform.forward;
				var accumulatedOutsideDirections = Vector3.zero;
				var outsideDirectionsCount = 0;
				var meanEnvironmentHitPoints = Vector3.zero;

				var directionalLightOutsideInfluence = 1f;

				var iterations = light.type == LightType.Directional ? SamplesPerDirectionalLight : SamplesPerLight;
				for (var i = 0; i < iterations; i++)
				{
					var dir = cameraTransform.forward;

					dir = JitterDirection(dir, i <= 0 ? RandomAngleRange * .1f : RandomAngleRange);
					if (Physics.SphereCast(new Ray(cameraPosition, dir), .05f, out var hit, 100, mask, QueryTriggerInteraction.Ignore))
					{
						summedHitDistances += Vector3.Distance(hit.point, lightPos);
						distanceHitsCount += 1;
						meanEnvironmentHitPoints =
							meanEnvironmentHitPoints == Vector3.zero ? hit.point : Vector3.Lerp(meanEnvironmentHitPoints, hit.point, .5f);
						var point = Vector3.Lerp(hit.point, cameraPosition, .1f);
						var directionToLight = light.type == LightType.Directional
							? -(lightTransform.transform.forward)
							: (lightTransform.position - hit.point).normalized;
						directionToLight = JitterDirection(directionToLight, RandomAngleRange * .6f);
						var reflectionRay = new Ray(point, directionToLight);
						if (Physics.Raycast(reflectionRay, out var hit2, 100, mask, QueryTriggerInteraction.Ignore))
						{
							if (light.type == LightType.Directional ||
							    Vector3.Distance(cameraPosition, hit2.point) < Vector3.Distance(cameraPosition, lightPos))
							{
								// Debug.DrawLine(reflectionRay.origin, hit2.point, Color.red);
								occlusions += 1;
								var t = (float) occlusions / iterations;
								// t = Mathf.Pow(t, 2);
								coverage = Mathf.Lerp(coverage, 1, t * OcclusionFactor);
							}
						}
						else
						{
							var pointOutside = reflectionRay.origin + reflectionRay.direction;
							var dirToOutside = pointOutside - hit.point;
							accumulatedOutsideDirections += dirToOutside * 10 * SunlightBias;
							outsideDirectionsCount += 1;
#if LIGHTESTIMATION_DEVELOPMENT
							// Debug.DrawLine(hit.point, pointOutside, Color.gray);
							Debug.DrawLine(point, pointOutside, Color.yellow);
							Debug.DrawLine(point, point + Vector3.up * .05f, Color.yellow);
#endif
						}

						if (light.type == LightType.Directional)
						{
							var angle = Vector3.Reflect(dir, hit.normal);
#if LIGHTESTIMATION_DEVELOPMENT
							Debug.DrawLine(hit.point, hit.point - dir * .2f, Color.gray);
							Debug.DrawLine(hit.point, hit.point + angle * .3f, Color.gray);
#endif
							var dot = Vector3.Dot(angle, lightTransform.forward);
							dot = Mathf.Clamp01(dot);
							directionalLightOutsideInfluence = Mathf.Lerp(directionalLightOutsideInfluence, 1f - dot, 1f / iterations);
						}
					}
				}

				if (distanceHitsCount > 0)
				{
					distanceToLight = summedHitDistances / distanceHitsCount;
				}

				float influence = light.intensity;
				if (light.type == LightType.Directional)
				{
					influence *= 1 - Mathf.Clamp01(coverage - outsideDirectionsCount);
					influence *= directionalLightOutsideInfluence;
				}
				else
				{
					// 0 = close, 1 = far
					var distanceWeight = 1 - Mathf.Clamp01(distanceToLight / Mathf.Max(light.range, .1f));
					influence *= distanceWeight;
					// if the light points in another direction reduce the influence
					if (light.type != LightType.Point)
					{
						var lightForward = lightTransform.forward;
						var lightToSurface = (meanEnvironmentHitPoints - lightPos).normalized;
						if (light.type == LightType.Spot)
						{
							var angle = Vector3.Angle(lightForward, lightToSurface);
							var spotAngle = light.spotAngle;
							var spotFactor = Mathf.Clamp01((spotAngle - angle) / spotAngle);
							spotFactor = Mathf.Pow(spotFactor, 0.5f);
							influence *= Mathf.Clamp01(spotFactor);
						}
					}

					// if we are very close to the light reduce coverage influence
					influence -= coverage * distanceWeight;
				}

				// influence = Mathf.Clamp01(influence);
				if (light.type != LightType.Directional)
					lightDirection = -(lightPos - meanEnvironmentHitPoints).normalized;
				else if (accumulatedOutsideDirections != Vector3.zero)
					lightDirection = -accumulatedOutsideDirections;
				data.Add(influence, lightDirection);
				influence = data.Influence;
				lightDirection = data.Direction;


				var step = influence * adaptationStep;
#if SPHERICALHARMONICS_SUPPORTED
				if (EstimateMainLightDirection())
					MainLightDirection = Vector3.Lerp(MainLightDirection, lightDirection, step);
#endif
				var lightIntensityUnclamped = light.intensity;
				var lightIntensity01 = Mathf.Clamp01(lightIntensityUnclamped);
				var lightIntensityInfluenced = lightIntensity01 * influence;
				brightness = Mathf.Lerp(brightness, lightIntensityInfluenced * IntensityFactor, step);
				if (EstimateAmbientIntensity())
					AverageBrightness = brightness;
				if (EstimateAmbientColor())
					ColorTemperature = Mathf.Lerp(ColorTemperature, light.colorTemperature * IntensityFactor, step);
				// collect ambient values
				ambientColorSum += light.color * lightIntensityInfluenced;
				ambientIntensitiesSum += lightIntensityInfluenced;
				ambientSamplesCount += 1;

#if SPHERICALHARMONICS_SUPPORTED
				if (EstimateAmbientSphericalHarmonics())
				{
					var sphIntensity = LightHelpers.ConvertBrightnessFromLumen(light.intensity * influence * IntensityFactor);
					SphericalHarmonicsL2.AddDirectionalLight(
						-lightDirection,
						light.color,
						sphIntensity
					);
				}
#endif

#if LIGHTESTIMATION_DEVELOPMENT
				// if (light.type == LightType.Directional)
				{
					if (Physics.Raycast(new Ray(cameraPosition, cameraTransform.forward), out var hit, 1000, mask))
					{
						Debug.DrawLine(hit.point, hit.point - lightDirection * influence, light.color);
						var _lightDir = light.type == LightType.Point ? (lightPos - hit.point).normalized : lightTransform.forward;
						Debug.DrawLine(hit.point, hit.point - _lightDir * .5f, light.color);
						Debug.DrawLine(hit.point,  hit.point + accumulatedOutsideDirections.normalized * .3f, Color.red);
					}
				}
#endif
			}

#if SPHERICALHARMONICS_SUPPORTED
			if (EstimateMainLightIntensity())
			{
				MainLightIntensityLumen = LightHelpers.ConvertBrightnessToLumen(brightness);
				MainLightColor = Color.Lerp(MainLightColor, ambientColorSum * IntensityFactor, adaptationStep);
			}
#endif

			if (EstimateAmbientColor())
			{
				var ambient = ambientSamplesCount > 0 ? ambientColorSum / ambientSamplesCount : Color.black;
				ColorCorrection = Color.Lerp(ColorCorrection, ambient * IntensityFactor, adaptationStep);
			}
#if LIGHTESTIMATION_DEVELOPMENT
			Debug.Log(ambient +", " + AverageBrightness);
#endif

			return lightsInSimulatedEnvironment.Length > 0;
		}

		private static Light[] lightsInSimulatedEnvironment = null;
		private static LightData[] lightData;


		internal static Vector3 JitterDirection(Vector3 dir, float jitterAngle)
		{
			float RandomJitterAngle()
			{
				return Mathf.Lerp(-jitterAngle, jitterAngle, Random.value);
			}

			return Quaternion.Euler(RandomJitterAngle(), RandomJitterAngle(), RandomJitterAngle()) * dir;
		}

		private static bool TryEnsureLightsAndData<T>(ref T[] array, int expectedSize = -1) where T : new()
		{
			if (lightsInSimulatedEnvironment == null && SimulatedAREnvironmentManager.Exists)
			{
				var root = SimulatedAREnvironmentManager.Instance.instantiatedGameObjectRoot;
				UpdateLightsFromSimulatedEnvironment(root);
			}

			if (lightsInSimulatedEnvironment == null) return false;

			if (expectedSize == -1) expectedSize = lightsInSimulatedEnvironment.Length;
			if (array == null || array.Length != expectedSize)
			{
				array = new T[expectedSize];
				for (var index = 0; index < expectedSize; index++)
				{
					array[index] = new T();
				}
			}

			return expectedSize > 0 && array.Length == expectedSize;
		}

		private void OnEnvironmentChanged(SimulatedAREnvironmentManager obj)
		{
			lightsInSimulatedEnvironment = null;
			SetDefaultLightValues();
		}

		private static void UpdateLightsFromSimulatedEnvironment(GameObject environmentRoot)
		{
			if (!environmentRoot) return;
			lightsInSimulatedEnvironment = environmentRoot.GetComponentsInChildren<Light>(true);
		}


		private class LightData
		{
			private int index;

			public int Samples = 60;

			private Vector3[] directionValues;
			private float[] influenceValues;

			private float[] influence
			{
				get
				{
					if (influenceValues == null || influenceValues.Length != Samples) influenceValues = new float[Samples];
					return influenceValues;
				}
			}

			private Vector3[] Directions
			{
				get
				{
					if (directionValues == null || directionValues.Length != Samples) directionValues = new Vector3[Samples];
					return directionValues;
				}
			}

			public void Add(float influenceSample, Vector3 direction)
			{
				if (index >= influence.Length) index = 0;
				influence[index] = influenceSample;
				Directions[index] = direction;
				index += 1;
			}

			public float Influence
			{
				get
				{
					var val = 0f;
					foreach (var inf in influenceValues)
					{
						val += inf;
					}

					return Mathf.Clamp01(val / (influenceValues.Length * .2f));
				}
			}

			public Vector3 Direction
			{
				get
				{
					var vec = Vector3.zero;
					foreach (var dir in Directions)
					{
						vec += dir; // Vector3.Lerp(vec, dir, 1f / Directions.Length);
					}

					return vec.normalized;
				}
			}
		}


#endif
	}
}