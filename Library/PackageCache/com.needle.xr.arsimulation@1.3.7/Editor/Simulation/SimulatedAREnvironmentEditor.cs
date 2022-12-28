using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.XR.ARSimulation.Simulation
{
	[CustomEditor(typeof(SimulatedAREnvironment))]
	public class SimulatedAREnvironmentEditor : Editor
	{
		private readonly List<MeshFilter> requireOptimization = new List<MeshFilter>();
		private bool meshListFoldout;

		private void OnEnable()
		{
			UpdateMeshList();
		}

		private void UpdateMeshList()
		{
			requireOptimization.Clear();
			if (target is SimulatedAREnvironment env)
			{
				var arr = SimulatedEnvironmentUtils.CollectInstancesThatRequireOptimization(env.gameObject, SimulatedAREnvironment.MaxVertices);
				foreach (var mf in arr)
				{
					if (!env.OptimizedColliders.Any(o => o.Input == mf.sharedMesh && o.Output))
					{
						requireOptimization.Add(mf);
					}
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var env = target as SimulatedAREnvironment;
			if (!env) return;

			if (SimulatedAREnvironmentManager.Exists)
			{
				GUILayout.Space(10);
				if (!env.IsActive && GUILayout.Button("Activate"))
					env.Activate();
				else if (env.IsActive && GUILayout.Button("Deactivate"))
					env.Deactivate();
			}

			if (requireOptimization.Count > 0)
			{
				GUILayout.Space(10);
				EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
				
				meshListFoldout = EditorGUILayout.Foldout(meshListFoldout,
					requireOptimization.Count + " Mesh(es) could be optimized");
				if (meshListFoldout)
				{
					using (new EditorGUI.DisabledScope(true))
					{
						foreach (var mf in requireOptimization)
						{
							var mesh = mf.sharedMesh;
							EditorGUILayout.ObjectField(mf.name, mesh, typeof(Mesh), mesh);
						}
					}
					GUILayout.Space(5);
				}

				if (SimulatedEnvironmentUtils.CanOptimize())
				{
					if (GUILayout.Button(new GUIContent("Optimize " + requireOptimization.Count + " Meshes now", "This may take a few seconds")))
					{
						env.Optimize();
						UpdateMeshList();
						AssetDatabase.Refresh();
					}
				}
				else
				{
					EditorGUILayout.HelpBox("Install Whinarn Mesh-Simplifier package to automatically optimize meshes", MessageType.Warning);
					if (GUILayout.Button("Open in browser"))
						Application.OpenURL("https://github.com/Whinarn/UnityMeshSimplifier#installation-into-unity-project");
				}
			}

			
			GUILayout.Space(10);
			EditorGUILayout.LabelField("Camera Positions", EditorStyles.boldLabel);
			var hasCamera = SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.camera;
			using (new EditorGUI.DisabledScope(!hasCamera))
			{
				if (GUILayout.Button("Capture Default Camera Pose"))
				{
					env.CaptureDefaultCameraPoseFromEditorSceneCamera();
				}

				using (new EditorGUI.DisabledScope(!env.TryGetDefaultCameraPose(env.gameObject, out var localPosition, out var localRotation)))
				{
					if (GUILayout.Button("Set Scene Camera To Default Pose"))
					{
						var et = env.transform;
						var ct = SceneView.lastActiveSceneView.camera.transform;
						var position = et.TransformPoint(localPosition);
						var rotation = Quaternion.Euler(et.TransformDirection(localRotation));
						ct.position = position;
						ct.rotation = rotation;
						SceneView.lastActiveSceneView.AlignViewToObject(ct);
					}
				}
			}
		}
	}
}