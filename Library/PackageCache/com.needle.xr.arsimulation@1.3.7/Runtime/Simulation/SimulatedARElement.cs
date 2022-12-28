using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Needle.XR.ARSimulation.Simulation
{
	/// <summary>
	/// Base AR Desktop simulation component that provides handy utility methods
	/// </summary>
	public abstract class SimulatedARElement : MonoBehaviour
	{
		private bool _triedGettingOrigin;
		private ARSessionOrigin _arSessionOrigin;

		private ARSessionOrigin ArSessionOrigin
		{
			get
			{
				if (_triedGettingOrigin) return _arSessionOrigin;
				_triedGettingOrigin = true;
				if (!_arSessionOrigin)
				{
					_arSessionOrigin = GetComponentInParent<ARSessionOrigin>();
					if (!_arSessionOrigin) _arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
				}

				return _arSessionOrigin;
			}
		}

		/// <summary>
		/// If the component is part of the <see cref="ArSessionOrigin"/> hierarchy this method transforms the provided world space in session space
		/// </summary>
		/// <param name="poseInWorldSpace">The Pose in world space to be passed to AR Foundation</param>
		/// <returns>The Pose transformed in session space if the component is part of the AR Session Origin hierarchy</returns>
		protected Pose TransformPoseToSessionSpaceIfNecessary(Pose poseInWorldSpace)
		{
			if (ArSessionOrigin && ArSessionOrigin.trackablesParent)
			{
				return ArSessionOrigin.trackablesParent.InverseTransformPose(poseInWorldSpace);
			}

			return poseInWorldSpace;
		}

		protected void Reset()
		{
			_triedGettingOrigin = false;
		}

		protected virtual void Awake()
		{
#if UNITY_EDITOR
			SceneSetup.BeforeCleanupData += OnBeforeCleanup;
			SceneSetup.PostCleanupData += OnPostCleanup;
#endif
		}

		protected virtual void OnDestroy()
		{
#if UNITY_EDITOR
			SceneSetup.BeforeCleanupData -= OnBeforeCleanup;
			SceneSetup.PostCleanupData -= OnPostCleanup;
#endif
		}


#if UNITY_EDITOR
		private bool didReceiveBeforeCleanup = false;
		private bool wasEnabled = false;

		protected virtual void OnBeforeCleanup()
		{
			wasEnabled = enabled;
			enabled = false;
			didReceiveBeforeCleanup = true;
		}

		protected virtual void OnPostCleanup()
		{
			if (didReceiveBeforeCleanup)
				enabled = wasEnabled;
		}
#endif
	}
}