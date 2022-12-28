// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Object = UnityEngine.Object;
// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEditor.Experimental.SceneManagement;
// using UnityEditor.SceneManagement;
// #endif
// namespace Needle.XR.ARSimulation.Simulation
// {
// 	public static class SimulatedAREnvironmentAccess
// 	{
// #if UNITY_EDITOR
// 		[InitializeOnLoadMethod]
// #endif
// 		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
// 		private static void Init()
// 		{
// #if UNITY_EDITOR
// 			SimulatedAREnvironmentManager.SceneChangedOrRecreated += mgr => OnChanged();
// 			EditorSceneManager.activeSceneChangedInEditMode += (o, s) => OnChanged();
//
// 			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
// 			EditorApplication.update += () =>
// 			{
// 				var current = PrefabStageUtility.GetCurrentPrefabStage();
// 				if (current == prefabStage) return;
// 				prefabStage = current;
// 				OnChanged();
// 			};
// #endif
// 		}
//
// 		private static void OnChanged()
// 		{
// 			renderers = null; 
// 		}
//
// 		private static List<Renderer> renderers = null;
//
// 		public static IEnumerable<Renderer> Renderers(Predicate<Renderer> filter)
// 		{
// #if UNITY_EDITOR
// 			if (renderers == null)
// 			{
// 				renderers = new List<Renderer>();
// 				foreach (var obj in All())
// 				{
// 					if (obj && obj.TryGetComponent<Renderer>(out var renderer))
// 					{
// 						renderers.Add(renderer);
// 					}
// 				}
// 			}
// #endif
//
// 			if (renderers == null) yield break;
// 			foreach (var rnd in renderers)
// 			{
// 				if (rnd && filter(rnd))
// 				{
// 					yield return rnd;
// 				}
// 			}
// 		}
//
// 		public static IEnumerable<GameObject> Objects()
// 		{
// 			foreach (var obj in SimulatedAREnvironmentManager.Instance.SceneInstances)
// 				yield return obj;
// 		}
//
// #if UNITY_EDITOR
// 		private static IEnumerable<GameObject> All()
// 		{
// 			var all = new List<GameObject>(SimulatedAREnvironmentManager.Instance.SceneInstances);
// 			
// 			void Traverse(GameObject obj)
// 			{
// 				all.Add(obj);
// 				for (var i = 0; i < obj.transform.childCount; i++)
// 				{
// 					var child = obj.transform.GetChild(i).gameObject;
// 					Traverse(child);
// 				}
// 			}
//
// 			if (SimulatedAREnvironmentManager.Instance.gameObject)
// 			{
// 				Traverse(SimulatedAREnvironmentManager.Instance.gameObject);
// 			}
//
// 			if (SimulatedAREnvironmentManager.Instance.Environment is GameObject go)
// 			{
// 				Traverse(go);
// 			}
// 			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
// 			if (prefabStage != null && prefabStage.prefabContentsRoot)
// 				Traverse(prefabStage.prefabContentsRoot);
//
// 			return all;
// 		}
// #endif
// 	}
// }