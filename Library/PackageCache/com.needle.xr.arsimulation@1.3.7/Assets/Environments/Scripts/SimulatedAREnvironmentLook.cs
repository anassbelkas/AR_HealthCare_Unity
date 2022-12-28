using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Needle.XR.ARSimulation.Interfaces;
using UnityEditor;
using UnityEditor.Presets;
#endif

namespace Needle.XR.ARSimulation.Simulation
{
	[ExecuteAlways]
	public class SimulatedAREnvironmentLook : MonoBehaviour//, IPropertyBlockDataProvider
	{
#pragma warning disable CS0414
		[SerializeField] [Range(0, 1)] private float albedoIntensity = 1f;

		[SerializeField] [Range(0, 1)] private float lightingIntensity = 0.7f;

		[SerializeField, HideInInspector] private bool viewClipping = true;
#pragma warning restore

		private static readonly int AlbedoIntensity = Shader.PropertyToID("_AlbedoIntensity");
		private static readonly int LightIntensity = Shader.PropertyToID("_LightIntensity");
		private const string ClippingKeyword = "_ARSIMULATION_CLIPPING_ON";

#if UNITY_EDITOR
		// private MaterialPropertyBlock _block;
		private void OnValidate()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;
			if (!Selection.Contains(this.gameObject)) return;
			if(SimulatedAREnvironmentManager.Exists && SimulatedAREnvironmentManager.Instance.Environment == this.gameObject)
				SimulatedAREnvironmentManager.Instance.RequestRecreate();
		}

		private void LateUpdate()
		{
			// if (_block == null) _block = new MaterialPropertyBlock();
			// foreach (var rend in SimulatedAREnvironmentAccess.Renderers(r => r.sharedMaterials.Any(m => m && m.shader.name.EndsWith("AR Simulation/Lightbake"))))
			// {
			// 	rend.GetPropertyBlock(_block);
			// 	ApplyData(_block);
			// 	rend.SetPropertyBlock(_block);
			// }
			UpdateShaderValues();
		}

		public void UpdateShaderValues()
		{
			Shader.SetGlobalFloat(AlbedoIntensity, albedoIntensity);
			Shader.SetGlobalFloat(LightIntensity, lightingIntensity);

			if (!viewClipping && Shader.IsKeywordEnabled(ClippingKeyword)) Shader.DisableKeyword(ClippingKeyword);
			else if (viewClipping && !Shader.IsKeywordEnabled(ClippingKeyword)) Shader.EnableKeyword(ClippingKeyword);
		}

		[ContextMenu("Remove keyword " + ClippingKeyword + " from all renderers")]
		private void RemoveKeyword()
		{
			foreach (var r in FindObjectsOfType<Renderer>())
			foreach (var mat in r.sharedMaterials)
				mat.DisableKeyword(ClippingKeyword);
		}
		
		public void ApplyData(MaterialPropertyBlock block)
		{
			block.SetFloat(AlbedoIntensity, albedoIntensity);
			block.SetFloat(LightIntensity, lightingIntensity);
		}
#endif
	}
	
#if UNITY_EDITOR

	[CustomEditor(typeof(SimulatedAREnvironmentLook))]
	public class SimulatedEnvironmentLookEditor : Editor
	{
		// from PresetSelector.cs (private)
		private static IEnumerable<Preset> FindAllPresetsOfType(PresetType presetType) =>
			AssetDatabase.FindAssets("t:Preset")
				.Select(a => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(a)))
				.Where(preset => preset.GetPresetType() == presetType);

		private List<Preset> applicablePresets;

		private void OnEnable()
		{
			applicablePresets = FindAllPresetsOfType(new PresetType(target)).ToList();
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Look Presets", EditorStyles.boldLabel);
			foreach (var preset in applicablePresets)
			{
				if (GUILayout.Button(ObjectNames.NicifyVariableName(preset.name.Replace("SimEnv-", ""))))
				{
					preset.ApplyTo(target);
					if(target is SimulatedAREnvironmentLook look)
						look.UpdateShaderValues();
					if(SimulatedAREnvironmentManager.Exists)
						SimulatedAREnvironmentManager.Instance.RequestRecreate();
				}
			}
		}
	}

#endif
}