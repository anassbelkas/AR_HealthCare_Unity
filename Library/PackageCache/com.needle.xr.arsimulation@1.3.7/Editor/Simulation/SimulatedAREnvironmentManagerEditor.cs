using System;
using System.Collections.Generic;
using System.Linq;
using Needle.XR.ARSimulation.Compatibility;
using Needle.XR.ARSimulation.Interfaces;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Task = System.Threading.Tasks.Task;

namespace Needle.XR.ARSimulation.Simulation
{
	[CustomEditor(typeof(SimulatedAREnvironmentManager))]
	public class SimulatedAREnvironmentManagerEditor : Editor
	{
		private List<(SimulatedAREnvironment env, bool inScene)> environments = new List<(SimulatedAREnvironment env, bool)>();
		private readonly List<Texture2D> previews = new List<Texture2D>();

		private GameObject lastClicked;
		private DateTime lastClickedTime;

		private bool EnvironmentsFoldout
		{
			get => SessionState.GetBool("_ARS_EnvironmentFoldout", true);
			set => SessionState.SetBool("_ARS_EnvironmentFoldout", value);
		}

		private void OnEnable()
		{
			UpdatePreviews();
		}

		private void UpdatePreviews()
		{
			environments.Clear();
			SimulatedAREnvironmentManager.FindAvailableEnvironments(environments);
			environments = environments.OrderBy(e => e.env.name).ToList();

			var rnd = new PreviewRenderUtility();
			rnd.camera.backgroundColor = Color.gray * .6f;
			var l0 = rnd.lights[0];
			var l1 = rnd.lights[1];
			l0.intensity = 1.3f;
			l0.shadowStrength = 1f;
			l0.shadows = LightShadows.Hard;
			l0.shadowResolution = LightShadowResolution.High;
			l1.intensity = .6f;
			l1.shadowStrength = 1f;
			l1.shadows = LightShadows.Hard;
			l1.shadowResolution = LightShadowResolution.High;
			previews.Clear();
			foreach (var (env, _) in environments)
			{
				if (!env) continue;

				var auto = env.GetComponentsInChildren<AutoMaterial>();
				foreach (var a in auto) a.UpdateMaterials();

				env.TryGetComponent(out IPropertyBlockDataProvider dp);

				rnd.BeginStaticPreview(new Rect(0, 0, 120, 120));
				var bounds = new Bounds();
				foreach (var renderer in env.GetComponentsInChildren<Renderer>())
				{
					if (renderer.TryGetComponent(out MeshFilter mf) && mf.sharedMesh)
					{
						var t = mf.transform;
						if (bounds.size == Vector3.zero && bounds.size == Vector3.zero) bounds.center = t.position;
						bounds.Encapsulate(renderer.bounds);
						var block = new MaterialPropertyBlock();
						renderer.GetPropertyBlock(block);
						dp?.ApplyData(block);
						rnd.DrawMesh(mf.sharedMesh, t.localToWorldMatrix, renderer.sharedMaterial, 0, block);
					}
				}

				var previewCameraTransform = rnd.camera.transform;
				var offset = 1.1f * (new Vector3(bounds.size.x, bounds.size.y * 1.5f, bounds.size.z * 1f)).normalized * bounds.size.magnitude;
				previewCameraTransform.position = bounds.center + offset;
				var pos = previewCameraTransform.position;
				var lookTarget = bounds.center - new Vector3(0, bounds.size.y * .2f, 0);
				var dir = lookTarget - pos;
				if (dir == Vector3.zero) dir = Vector3.forward;
				var camRot = Quaternion.LookRotation(dir, Vector3.up);
				previewCameraTransform.rotation = camRot;
				l0.transform.rotation = Quaternion.Euler(-30, -40f, 0) * camRot;
				l1.transform.rotation = Quaternion.Euler(-20, 125f, 0) * camRot;
				rnd.camera.fieldOfView = 25.0f * offset.magnitude / bounds.size.magnitude;
				rnd.camera.nearClipPlane = offset.magnitude * .1f;
				rnd.camera.farClipPlane = Vector3.Distance(lookTarget, pos) * 2.5f;
				rnd.ambientColor = Color.gray;
				rnd.Render(true, true);
				var tx = rnd.EndStaticPreview();
				previews.Add(tx);
			}

			rnd.Cleanup();
		}

		private Light[] lights;
		
		private void ShowWarnings(SimulatedAREnvironmentManager manager)
		{
			if (lights == null) lights = FindObjectsOfType<Light>().Where(l => !manager.IsPartOfSimulationInstance(l.gameObject)).ToArray();
			
			if (lights.Any(_light => _light && (_light.cullingMask & SimulatedAREnvironmentManager.SimulatedEnvironmentMask) != 0))
			{
				EditorGUILayout.HelpBox("Some lights in your scene do not cull simulated environment and might affect simulated environment rendering.", MessageType.Warning);
				if (GUILayout.Button("Fix light culling mask"))
				{
					foreach (var light in lights)
					{
						if (!light) continue;
						if (!manager.IsPartOfSimulationInstance(light.gameObject))
						{
							Undo.RegisterCompleteObjectUndo(light, "Cull simulated environment");
							light.cullingMask = SimulatedAREnvironmentManager.ExcludeEnvironment(light.cullingMask);
						}
					}
				}
			}
			
			if (manager.TryGetARCameraClearFlags(out var flags))
			{
				if (flags == CameraClearFlags.Skybox)
				{
					EditorGUILayout.HelpBox("Main Camera clear flags must not be set to skybox for simulated environments to be visible", MessageType.Warning, true);
					if (GUILayout.Button("Fix camera"))
					{
						manager.FixMainCameraClearFlags();
					}
				}
			}
		}

		public override void OnInspectorGUI()
		{
			bool foldout = ARSimulationLoaderSettings.Instance.foldoutEnvironmentManagerSettings;
			foldout = EditorGUILayout.Foldout(foldout, "Settings");
			ARSimulationLoaderSettings.Instance.foldoutEnvironmentManagerSettings = foldout;
			if (foldout)
				base.OnInspectorGUI();

			if (!(target is SimulatedAREnvironmentManager manager)) return;
			
			GUILayout.Space(10);
			ShowWarnings(manager);

			if (environments.Count <= 0)
			{
				GUILayout.Space(5);
				EditorGUILayout.LabelField("No available environments found in project", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Add a " + nameof(SimulatedAREnvironment) + " component to your prefabs to show up in this list", MessageType.Info);
			}
			else
			{
				void CheckPing(GameObject go, Rect lastRect)
				{
					if (Event.current.type != EventType.MouseUp || Event.current.button != 0) return;
					if (lastRect == Rect.zero)
						lastRect = GUILayoutUtility.GetLastRect();
					if (lastRect.Contains(Event.current.mousePosition))
					{
						if (lastClicked == go && (DateTime.Now - lastClickedTime).Seconds < .5f)
						{
							if (EditorUtility.IsPersistent(go))
							{
								AssetDatabase.OpenAsset(go);
							}
						}
						else
						{
							EditorGUIUtility.PingObject(go);
						}

						lastClicked = go;
						lastClickedTime = DateTime.Now;
					}
				}
				
				GUILayout.Space(foldout ? 14 : 5);
				EnvironmentsFoldout = EditorGUILayout.Foldout(EnvironmentsFoldout, "Available Environments");
				if (EnvironmentsFoldout)
				{
					
					var big = Screen.width > 450;
					float size = big ? 70 : 15;
					int i = 0;
					foreach (var (env, inScene) in environments)
					{
						var index = i++;
						if (!env) continue;
						var prev = GUILayoutUtility.GetLastRect();
						Texture2D assetPreview = null;
						if (inScene)
						{
							var mf = env.gameObject.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m && m.sharedMesh);
							if (mf)
								assetPreview = AssetPreview.GetAssetPreview(mf.sharedMesh);
							else
								assetPreview = AssetPreview.GetMiniThumbnail(env.gameObject);
						}
						else
							assetPreview = AssetPreview.GetAssetPreview(env.gameObject);

						if(index >= 0 && index < previews.Count)
							assetPreview = previews[index];

						if (assetPreview)
						{
							EditorGUILayout.BeginHorizontal();
							var rect = new Rect(prev.x, prev.y + prev.height + 5, size, size); // EditorGUILayout.GetControlRect();
							rect.width = rect.height;
							EditorGUI.DrawTextureTransparent(rect, assetPreview);
							CheckPing(env.gameObject, rect);
							GUILayout.Space(rect.width + 3);
						}
						else
							EditorGUILayout.BeginHorizontal();

						var selected = env.IsCurrentEnvironment;
						var label = env.name + " (" + (inScene ? "Scene" : "Asset") + ")";
						if (selected)
							label += " ✓";
						EditorGUILayout.LabelField(label, selected ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.ExpandWidth(true));
						CheckPing(env.gameObject, Rect.zero);
						// GUILayout.FlexibleSpace();
						if (!selected)
						{
							if (GUILayout.Button("Activate", GUILayout.Width(80))) env.Activate();
						}
						else
						{
							if (GUILayout.Button("Disable", GUILayout.Width(80))) env.Deactivate();
						}

						// EditorGUI.BeginDisabledGroup(env.IsCurrentEnvironment);
						// EditorGUI.EndDisabledGroup();
						EditorGUILayout.EndHorizontal();
						if (big && assetPreview)
							GUILayout.Space(size * .5f + 18);
					}

					EditorGUILayout.Space(5);
				}
			}

			if (EnvironmentsFoldout || environments.Count <= 0)
			{
				if (GUILayout.Button("Rescan Environments"))
				{
					var objs = AssetDatabase.FindAssets("t:" + nameof(GameObject)).Select(AssetDatabase.GUIDToAssetPath).ToArray();
					Debug.Log("Found " + objs.Length + " GameObjects\n" + string.Join("\n", objs));
					foreach (var path in objs)
						if (SimulatedAREnvironment.TryAdd(path))
							Debug.Log("Added " + path);
					UpdatePreviews();
				}
			}
		}
	}
}