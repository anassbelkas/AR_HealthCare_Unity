using System;
using UnityEngine;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;
using System.Linq;
using System.Security.Cryptography;
using Object = System.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif
namespace Needle.XR.ARSimulation.Simulation
{
	/// <summary>
	/// Used to add a <see cref="ARTrackedImage">ARTrackedImage</see>
	/// </summary>
	public class SimulatedARTrackedImage : SimulatedARElement, ITrackedImageProvider
	{
		/// <summary>
		/// Image referenced in a <see cref="IReferenceImageLibrary">IReferenceImageLibrary</see> to simulate tracking for.
		/// If you want to update the tracked image during runtime call <see cref="MarkDirty"/> to apply changes
		/// </summary>
		public Texture2D Image;

		/// <summary>
		/// Reference Image, TODO: find reference image for user assigned texture
		/// </summary>
		private XRReferenceImage referenceImage;

		/// <summary>
		/// The <see cref="TrackingState">TrackingState</see> of the image. Will be automatically updated if <see cref="SimulateTracking"/> is set to true.
		/// </summary>
		public TrackingState Tracking = TrackingState.Tracking;

		/// <summary>
		/// Automatically adjusts tracking state of image depending on camera frustum. E.g. when near the frustum border the state changes to <see cref="TrackingState">TrackingState.Limited</see> and to None when not in view
		/// </summary>
		[Tooltip(
			"Automatically adjusts tracking state of image depending on camera frustum. E.g. when near the frustum border the state changes to TrackingState.Limited and to None when not in view")]
		public bool SimulateTracking = true;

		// TODO: add automatic Image aspect support
		// public bool UseImageAspect = false;

		[Header("Visuals")] [Tooltip("Assign to create an instance of the tracked image in editor")]
		public Material InstanceMaterial;

		private TrackableId id;

		public TrackableId TrackableId
		{
			get
			{
				{
					if (this)
						id = this.GetTrackableId();
					return id;
				}
			}
		}

		/// <summary>
		/// The currently tracked image texture
		/// </summary>
		public Texture2D Texture => currentImage;


		/// <summary>
		/// The session space relative pose of the simulated tracked image.
		/// </summary>
		public Pose Pose
		{
			get
			{
				UnityEngine.Pose pose;
				var t = this.transform;
				if (Tracking == TrackingState.Limited)
					pose = new Pose(limitedTrackingPosition, t.rotation);
				else
					pose = new Pose(t.position, t.rotation);
				return pose; // TransformPoseToSessionSpaceIfNecessary(pose);
			}
		}

		public Vector2 Size => new Vector2(transform.localScale.x, transform.localScale.z);

		public TrackingState TrackingState
		{
			get => Tracking;
			set
			{
				if (Tracking != value)
					MarkDirty();
				Tracking = value;
			}
		}

		private Vector3 limitedTrackingPosition;

		public IntPtr NativePointer => new IntPtr(this.GetInstanceID());

		private bool isDirty = false;
		private Texture2D currentImage;

#if UNITY_EDITOR
		private bool triedAutoAssignImage;
#endif
		private static ARTrackedImageManager imageManager;
		private static bool triedFindingImageManager;
		private static ARSessionOrigin arSession;
		private static Camera arCamera;


		public enum GizmoStyle
		{
			Outline,
			Transparent,
			Image
		}
		
		public GizmoStyle gizmoStyle = GizmoStyle.Image;

		public void MarkDirty()
		{
			isDirty = true;
		}

		[ContextMenu(nameof(TryAutoAssignRandomImageFromLibrary))]
		public void TryAutoAssignRandomImageFromLibrary()
		{
			if (!imageManager)
				imageManager = FindObjectOfType<ARTrackedImageManager>();
			if (imageManager && imageManager.referenceLibrary != null && imageManager.referenceLibrary.count > 0)
			{
				AssignRandomImage();
			}
		}

		private void AssignRandomImage()
		{
			this.referenceImage = imageManager.referenceLibrary[Mathf.FloorToInt(imageManager.referenceLibrary.count * Random.value)];
			var tex = this.referenceImage.texture;
			if (!tex)
			{
				// TODO: refactor interface to use guid only and no direct texture references for cases where textures are not available at runtime
#if UNITY_EDITOR
				var path = AssetDatabase.GUIDToAssetPath(this.referenceImage.textureGuid.ToString().Replace("-", ""));
				if (!string.IsNullOrEmpty(path))
					tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#else
                Debug.LogWarning("Texture can not be loaded at runtime");
#endif
			}

			this.currentImage = this.Image = tex;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			detectedImageExists.img = null;
			detectedImageExists.checkedIfExists = false;

			if (!triedFindingImageManager && !imageManager)
			{
				imageManager = FindObjectOfType<ARTrackedImageManager>();
				triedFindingImageManager = true;
			}

			if (!triedAutoAssignImage && imageManager && imageManager.referenceLibrary != null && imageManager.referenceLibrary.count > 0)
			{
				this.triedAutoAssignImage = true;
				if (!this.Image)
				{
					AssignRandomImage();
				}
			}

			MarkDirty();

			if (Application.isPlaying && Image != currentImage && currentImage)
			{
				// Image = currentImage;
				// Debug.LogWarning("Runtime changes are currently not supported", this);
				SimulatedTrackedImagesRegistry.Remove(this);
				currentImage = Image;
				// SimulatedTrackedImagesRegistry.Register(this);
			}

			currentImage = Image;

			if (hasStarted && Application.isPlaying && SimulateTracking && Tracking != lastSimulatedTrackingState && Time.time > 1)
			{
				Debug.LogWarning("Tracking state is simulated, disable " + nameof(SimulateTracking) + " to manually set tracking state", this);
			}
		}

		private bool hasStarted;

		private void Start()
		{
			if (!enabled) return;
			if (!this.Image) AssignRandomImage();
			hasStarted = true;
			currentImage = Image;
			limitedTrackingPosition = transform.position;
			OnAddImage();
		}

		private void OnEnable()
		{
#if UNITY_URP
			if (gizmoStyle == GizmoStyle.Image) gizmoStyle = GizmoStyle.Transparent;
#endif
			limitedTrackingPosition = transform.position;
			currentImage = Image;
			// first registration should happen at start
			// because in editor subsystems should setup first and register image libraries
			if (hasStarted)
				OnAddImage();
		}

		private void OnDisable()
		{
			if (Application.isPlaying)
				OnRemoveImage();
		}

		private void LateUpdate()
		{
			if (!hasStarted || !enabled) return;

			UpdateOrCreateVisual();

			if (SimulateTracking)
			{
				var prevState = Tracking;
				UpdateLimitedTrackingPosition();
				OnSimulateTracking();
				if (prevState != Tracking || Tracking == TrackingState.Limited)
					isDirty = true;
			}
			else if (Tracking == TrackingState.Limited)
			{
				UpdateLimitedTrackingPosition();
				isDirty = true;
			}

			if (transform.hasChanged || isDirty)
			{
				currentImage = Image;
				if (Tracking != TrackingState.None)
					SimulatedTrackedImagesRegistry.Update(this);
				else
				{
					SimulatedTrackedImagesRegistry.Remove(this);
				}

				transform.hasChanged = false;
			}

			isDirty = false;
		}

		private GameObject instance;
		private Transform instanceTransform;
		private Material instanceMaterial;

		private void UpdateOrCreateVisual()
		{
			if (!Application.isPlaying) return;

			// Handle visual
			if (Tracking != TrackingState.None && InstanceMaterial && !instance)
			{
				instance = GameObject.CreatePrimitive(PrimitiveType.Quad);
				instance.name = this.name + "-Visual";
				instance.transform.SetParent(this.transform, false);
				instanceTransform = instance.transform;
				instanceMaterial = new Material(InstanceMaterial);
				var rend = instance.GetComponent<MeshRenderer>();
				rend.sharedMaterial = instanceMaterial;
			}

			if (Tracking != TrackingState.None && instanceMaterial && InstanceMaterial)
			{
				// if (SimulatedAREnvironmentManager.Exists && !SimulatedAREnvironmentManager.Instance.IsPartOfSimulationInstance(instance))
				// {
				//     // TODO: maybe add support to add image instance to simulated environment if necessary to have the image be rendered as part of the background image. Alternatively it could be up to the user to add the SimulatedARTrackedImage component to the environment instance
				// }
				if (!instance.activeSelf) instance.SetActive(true);
				instanceMaterial.mainTexture = Image;
				instanceTransform.localPosition = Vector3.zero;
				instanceTransform.localRotation = Quaternion.Euler(90, 0, 0);
				instanceTransform.localScale = Vector3.one;
			}
			else if (instance)
			{
				if (instance.activeSelf)
					instance.SetActive(false);
			}
		}

		private void OnAddImage()
		{
			SimulatedTrackedImagesRegistry.Register(this);
		}

		private void OnRemoveImage()
		{
			SimulatedTrackedImagesRegistry.Remove(this);
			if (Application.isPlaying)
			{
				if (instance)
					Destroy(instance);
				if (instanceMaterial)
					Destroy(instanceMaterial);
			}
			else
			{
				if (instance)
					DestroyImmediate(instance);
				if (instanceMaterial)
					DestroyImmediate(instanceMaterial);
			}
		}

		private float leftBoundsTime;
		private TrackingState lastSimulatedTrackingState;

		private void UpdateLimitedTrackingPosition()
		{
			limitedTrackingPosition += Random.insideUnitSphere * (Size.x * Time.deltaTime);
			limitedTrackingPosition = Vector3.Lerp(limitedTrackingPosition, transform.position, Time.deltaTime * .1f);
		}

		private void OnSimulateTracking()
		{
			if (!arSession) arSession = FindObjectOfType<ARSessionOrigin>();
			if (!arCamera && arSession) arCamera = arSession.camera;
			if (!arCamera) return;

			var direction = Vector3.Dot(arCamera.transform.forward, transform.up);
			if (direction > .1f)
			{
				Tracking = TrackingState.None;
				return;
			}

			var planes = GeometryUtility.CalculateFrustumPlanes(arCamera);
			// we shrink view size a bit to detect the image later
			var viewSize = new Vector3(Size.x * .05f, 0, Size.y * .05f);
			var visible = GeometryUtility.TestPlanesAABB(planes, new Bounds(transform.position, viewSize));
			if (visible)
			{
				leftBoundsTime = 0;
				Tracking = TrackingState.Tracking;
				if (direction > -.1f && direction < .2f)
				{
					Tracking = TrackingState.Limited;
				}
			}
			else
			{
				if (leftBoundsTime <= 0.01f)
				{
					limitedTrackingPosition = transform.position;
					leftBoundsTime = Time.time;
				}

				if (Time.time - leftBoundsTime < 1.5f)
					Tracking = TrackingState.Limited;
				else
					Tracking = TrackingState.None;
			}

			lastSimulatedTrackingState = Tracking;
		}

		private void OnDrawGizmos()
		{
			InternalDrawGizmo(out var size, false);
		}

		private void OnDrawGizmosSelected()
		{
			InternalDrawGizmo(out var size, true);
		}

		private Material gizmoMaterial;
		private Mesh gizmoMesh;

		private void InternalDrawGizmo(out Vector2 size, bool selected)
		{
			var t = transform;
			var exists = ExistsInReferencedImageLibrary();

			Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
			size = Size;
			var c = new Color(0, .5f, 1);
			Gizmos.color = new Color(c.r, c.g, c.b, .2f);
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0, size.y));

#if UNITY_URP
			if (gizmoStyle == GizmoStyle.Image && ARSimulationProjectInfo.CurrentRenderPipeline != CurrentRenderPipelineType.Builtin)
			{
				gizmoStyle = GizmoStyle.Transparent;
				Debug.Log("Image Gizmos are not supported with RenderPipeline: " + ARSimulationProjectInfo.CurrentRenderPipeline, this);
			}
#endif

			if (gizmoStyle == GizmoStyle.Transparent || !exists)
			{
				Gizmos.color = selected ? new Color(c.r, c.g, c.b, .2f) : new Color(c.r, c.g, c.b, .05f);
				if (!exists) Gizmos.color = new Color(1, 0, 1, .5f);
				Gizmos.DrawCube(new Vector3(0, -.001f, 0), new Vector3(size.x, 0, size.y));
			}
#if UNITY_EDITOR
			else if (gizmoStyle == GizmoStyle.Image && exists)
			{
				if (!gizmoMesh)
				{
					// Load default Quad from Unity resources
					gizmoMesh = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Library/unity default resources")
						.Where(x => x is Mesh).Cast<Mesh>()
						.FirstOrDefault(x => x.name == "Quad");
				}

				if (!gizmoMaterial)
					gizmoMaterial = new Material(Shader.Find("Unlit/Texture"));

				gizmoMaterial.mainTexture = Image;
				gizmoMaterial.SetPass(0);

				var mat = Matrix4x4.TRS(t.position, t.rotation * Quaternion.Euler(90, 0, 0), new Vector3(size.x, size.y, 1));
				Graphics.DrawMeshNow(gizmoMesh, mat);
			}
#endif
		}


		private (Texture2D img, bool checkedIfExists, bool result) detectedImageExists;

		private bool ExistsInReferencedImageLibrary()
		{
#if UNITY_EDITOR
			if (!currentImage) return false;
			if (imageManager && (imageManager.referenceLibrary == null || imageManager.referenceLibrary.count <= 0)) return false;
			if (currentImage && detectedImageExists.img == currentImage && detectedImageExists.checkedIfExists) return detectedImageExists.result;

			if (!imageManager)
				imageManager = FindObjectOfType<ARTrackedImageManager>();
			if (imageManager && imageManager.referenceLibrary != null)
			{
				detectedImageExists.img = currentImage;
				detectedImageExists.checkedIfExists = true;
				detectedImageExists.result = false;

				var lib = imageManager.referenceLibrary;
				for (var i = 0; i < lib.count; i++)
				{
					var entry = lib[i];
					var textureGuid = entry.textureGuid.ToString().Replace("-", "");
					AssetDatabase.TryGetGUIDAndLocalFileIdentifier(currentImage, out var guid, out long id);
					if (textureGuid == guid)
					{
						detectedImageExists.result = true;
						return detectedImageExists.result;
					}
				}
			}

			return false;
#else
            return true;
#endif
		}
#endif
	}
}