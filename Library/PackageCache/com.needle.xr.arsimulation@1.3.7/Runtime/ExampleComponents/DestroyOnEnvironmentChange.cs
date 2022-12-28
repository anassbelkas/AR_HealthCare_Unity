using System;
using Needle.XR.ARSimulation.Simulation;
using UnityEngine;

namespace Needle.XR.ARSimulation.ExampleComponents
{
	public class DestroyOnEnvironmentChange : MonoBehaviour
	{
		#if UNITY_EDITOR
		private void Awake()
		{
			SimulatedAREnvironmentManager.WillChangeEnvironment += OnWillChangeEnvironment;
		}

		private void OnWillChangeEnvironment(SimulatedAREnvironmentManager obj)
		{
			SimulatedAREnvironmentManager.WillChangeEnvironment -= OnWillChangeEnvironment;
			if (this && this.gameObject)
				Destroy(this.gameObject);
		}
		#endif
	}
}