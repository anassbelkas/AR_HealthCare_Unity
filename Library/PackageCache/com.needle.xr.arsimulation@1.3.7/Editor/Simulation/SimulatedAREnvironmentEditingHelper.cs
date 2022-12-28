using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Needle.XR.ARSimulation.Simulation
{
	internal static class SimulatedAREnvironmentEditingHelper
	{
		[InitializeOnLoadMethod]
		private static void Init()
		{
			if (BuildPipeline.isBuildingPlayer) return;
			
			Selection.selectionChanged += SelectedChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChange;
			
			if (EditorApplication.isPlayingOrWillChangePlaymode) return;
			
			OnUpdateEnvironmentInstance(false);
			const int autoUpdateFrequency = 300;
			var lastUpdate = autoUpdateFrequency - 3;
			EditorApplication.update += () =>
			{
				lastUpdate += 1;
				if (lastUpdate < autoUpdateFrequency) return;
				lastUpdate = 0;
				if (EditorApplication.isPlayingOrWillChangePlaymode) return;
				OnUpdateEnvironmentInstance(false);
			};
		}

		private const string k_EnabledKey = "_ARSimulation_EnvironmentManagerWasEnabled";
		private static bool environmentManagerWasEnabled
		{
			get => EditorPrefs.GetBool(k_EnabledKey);
			set => EditorPrefs.SetBool(k_EnabledKey, value);
		}

		private static void OnPlayModeStateChange(PlayModeStateChange obj)
		{
			switch (obj)
			{
				case PlayModeStateChange.ExitingEditMode:
					OnUpdateEnvironmentInstance(true);
					// make sure to destroy all simulated objects when leaving edit mode
					if (SimulatedAREnvironmentManager.Exists)
					{
						environmentManagerWasEnabled = SimulatedAREnvironmentManager.Instance.enabled;
						SimulatedAREnvironmentManager.Instance.enabled = false;
					}
					break;
				case PlayModeStateChange.EnteredPlayMode:
					// when entering playmode enable environment manager
					if (SimulatedAREnvironmentManager.Exists) SimulatedAREnvironmentManager.Instance.enabled = environmentManagerWasEnabled;
					break;
				case PlayModeStateChange.ExitingPlayMode:
					break;
				case PlayModeStateChange.EnteredEditMode:
					OnUpdateEnvironmentInstance(false);
					if (SimulatedAREnvironmentManager.Exists) SimulatedAREnvironmentManager.Instance.enabled = environmentManagerWasEnabled;
					break;
			}
		}

		// private static Object currentlyAssignedSceneOrPrefab;
		// private static HashSet<Object> assignedObjectLookup;

		private static void SelectedChanged()
		{
			OnUpdateEnvironmentInstance(false);
		}

		private static void OnUpdateEnvironmentInstance(bool ignoreSelectionAndUpdateImmediately)
		{
			if (Application.isPlaying) return;
			if (!SimulatedAREnvironmentManager.Exists || !SimulatedAREnvironmentManager.Instance.IsActive)
			{
				return;
			}
			
			const bool disableIfEnvironmentManagerIsSelected = false;
			if (!ignoreSelectionAndUpdateImmediately)
			{
				var currentEnvironment = SimulatedAREnvironmentManager.Instance.Environment as GameObject;
				if (!currentEnvironment) return;
				var sel = Selection.gameObjects;
				var envManagerGo = SimulatedAREnvironmentManager.Instance.gameObject; 
				foreach (var obj in sel)
				{
					// Debug.Log(obj + " -> " + currentEnvironment);
					// ReSharper disable once RedundantAssignment
					var isEnvironmentManager = obj == envManagerGo;
					// ReSharper disable once ConditionIsAlwaysTrueOrFalse
					if ((disableIfEnvironmentManagerIsSelected && isEnvironmentManager) || IsPartOfSimulatedScenePrefab(obj, currentEnvironment))
					{
						_shouldEnableAREnvironmentManager = false;
						SimulatedAREnvironmentManager.Instance.enabled = false;
						return;
					}
				}

			}
			
			if(SimulatedAREnvironmentManager.Instance.enabled) return;
			_shouldEnableAREnvironmentManager = true;
			DeferredEnableEnvironmentManager(ignoreSelectionAndUpdateImmediately);
		}

		private static bool _shouldEnableAREnvironmentManager = false;
		private static int _requestEnablingCounter = 0;
		private static async void DeferredEnableEnvironmentManager(bool immediately)
		{
			if (!immediately)
			{
				var cnt = ++_requestEnablingCounter;
				await Task.Delay(1000);
				if (cnt != _requestEnablingCounter) return;
			}
			
			if (!_shouldEnableAREnvironmentManager) return;
			if (!SimulatedAREnvironmentManager.Exists) return;
			_shouldEnableAREnvironmentManager = false;
			if (Application.isPlaying) return;
			// enable it
			if (SimulatedAREnvironmentManager.Instance.enabled) return;
			SimulatedAREnvironmentManager.Instance.enabled = true;
			SimulatedAREnvironmentManager.Instance.UpdateIfChanged();
		}

		private static bool IsPartOfSimulatedScenePrefab(GameObject selectedObject, Object assignedEnvironment)
		{
			// do nothing if the selected object is an asset
			if (EditorUtility.IsPersistent(selectedObject)) return false;
			// traverse selected parents up until we find assigned environment object
			var cur = selectedObject;
			while (cur)
			{
				if (cur == assignedEnvironment) return true;
				var p = cur.transform.parent;
				if (!p) break;
				cur = p.gameObject;
			}
			return false;
		}
	}
}