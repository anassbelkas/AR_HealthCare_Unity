// using System.Collections.Generic;
// using UnityEngine;
// #if UNITY_EDITOR
// using UnityEditor.Experimental.SceneManagement;
//
// #endif
//
// namespace Needle.XR.ARSimulation.Simulation
// {
// 	[ExecuteAlways, DisallowMultipleComponent]
// 	public class SimulatedAREnvironmentClipBox : MonoBehaviour
// 	{
// 		public Color GizmoColor = new Color(1, 0, 0, 1f);
//
// #if UNITY_EDITOR
// 		private void OnDrawGizmos()
// 		{
// 			DrawBounds(false);
// 		}
//
// 		private void OnDrawGizmosSelected()
// 		{
// 			DrawBounds(true);
// 		}
//
// 		private void DrawBounds(bool selected)
// 		{
// 			Gizmos.matrix = transform.localToWorldMatrix;
// 			Gizmos.color = GizmoColor;
// 			Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
// 			var col = GizmoColor;
// 			col.a *= selected ? .5f : .1f;
// 			Gizmos.color = col;
// 			Gizmos.DrawCube(Vector3.zero, Vector3.one);
// 		}
//
// 		private void Update()
// 		{
// 			OnUpdateBounds();
// 		}
//
// 		private static readonly List<SimulatedAREnvironmentClipBox> boundsList = new List<SimulatedAREnvironmentClipBox>();
// 		private static readonly Matrix4x4[] boundsMatrices = new Matrix4x4[20];
// 		private static readonly int ARSimulationEnvironmentBounds = Shader.PropertyToID("_ARSimulation_WorldToEnvironmentBound");
// 		private static int lastFrameBoundsUpdated = -1;
//
//
// 		private static void OnUpdateBounds()
// 		{
// 			if (Time.frameCount == lastFrameBoundsUpdated) return;
// 			lastFrameBoundsUpdated = Time.frameCount;
// 			FindComponents();
// 			int matrixIndex = 0;
// 			for (var index = 0; index < boundsMatrices.Length; index++)
// 			{
// 				if (index < boundsList.Count)
// 				{
// 					var bnd = boundsList[index];
// 					if (!bnd || !bnd.isActiveAndEnabled)
// 						continue;
// 					boundsMatrices[matrixIndex] = bnd.transform.worldToLocalMatrix;
// 				}
// 				else
// 				{
// 					boundsMatrices[matrixIndex] = Matrix4x4.zero;
// 				}
//
// 				++matrixIndex;
// 			}
//
// 			Shader.SetGlobalMatrixArray(ARSimulationEnvironmentBounds, boundsMatrices);
// 		}
//
// 		private static void FindComponents()
// 		{
// 			boundsList.Clear();
// #if UNITY_EDITOR
// 			var pfs = PrefabStageUtility.GetCurrentPrefabStage();
// 			if (pfs != null && pfs.prefabContentsRoot)
// 			{
// 				boundsList.AddRange(pfs.prefabContentsRoot.GetComponentsInChildren<SimulatedAREnvironmentClipBox>());
// 			}
// 			else
// #endif
// 			{
// 				boundsList.AddRange(FindObjectsOfType<SimulatedAREnvironmentClipBox>());
// 			}
// 		}
// #endif
// 	}
// }