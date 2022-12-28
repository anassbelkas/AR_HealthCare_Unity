using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Needle.XR.ARSimulation.Simulation
{
	public static class SimulatedAREnvironmentHelpers
	{
		public static void FindAllAvailableSimulatedAREnvironments(List<(SimulatedAREnvironment env, bool inScene)> environments)
		{
#if UNITY_EDITOR
			if (environments == null) return;
			environments.Clear();

			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				var roots = scene.GetRootGameObjects();
				foreach (var rt in roots)
				{
					var list = rt.GetComponentsInChildren<SimulatedAREnvironment>(true);
					foreach (var env in list)
					{
						if (SimulatedAREnvironmentManager.Instance.IsPartOfSimulationInstance(env.gameObject)) continue;
						if (environments.Any(e => e.env.gameObject == env.gameObject)) continue;
						environments.Add((env, true));
					}
				}
			}

			if (ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent != null)
			{
				foreach (var guid in ARSimulationLoaderSettings.PrefabGuidsWithSimulatedEnvironmentComponent)
				{
					var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
					if (asset && asset.TryGetComponent(out SimulatedAREnvironment env))
					{
						if (environments.Any(e => e.env.gameObject == env.gameObject)) continue;
						environments.Add((env, false));
					}
				}
			}
#endif
		}
	}
}