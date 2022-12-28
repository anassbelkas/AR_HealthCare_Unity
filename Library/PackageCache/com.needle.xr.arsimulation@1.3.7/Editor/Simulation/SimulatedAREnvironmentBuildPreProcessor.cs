using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Needle.XR.ARSimulation.Simulation
{
	public class SimulatedAREnvironmentBuildPreProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder { get; }
		public void OnPreprocessBuild(BuildReport report)
		{
			var env = Object.FindObjectOfType<SimulatedAREnvironmentManager>();
			if (!env) return;
			if (env.environmentCamera && env.environmentCamera.gameObject) Object.DestroyImmediate(env.environmentCamera.gameObject);
			if (env.instantiatedGameObjectRoot) Object.DestroyImmediate(env.instantiatedGameObjectRoot);
			env.instantiatedGameObjects.Clear();
		}
	}
}