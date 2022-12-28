using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Needle.XR.ARSimulation
{
	[Serializable]
	public class OptimizedMesh
	{
		public Mesh Input;
		public Mesh Output;
	}

	internal static class SimulatedEnvironmentUtils
	{
		public static MeshFilter[] CollectInstancesThatRequireOptimization(GameObject root, int maxVertices)
		{
			var renderers = root.GetComponentsInChildren<MeshRenderer>(false);
			var mfs = renderers
				.Select(mr => mr.GetComponent<MeshFilter>())
				.Where(f =>
				{
					var mesh = f.sharedMesh;
					return mesh && mesh.vertexCount > maxVertices;
				});
			return mfs.ToArray();
		}

		public static bool CanOptimize() =>
#if MESH_SIMPLIFIER
			true;
#else
			false;
#endif

		public static void OptimizeForMeshing(GameObject environmentInstanceRoot, int maxVertices, ref List<OptimizedMesh> list)
		{
			if (list == null) list = new List<OptimizedMesh>();
#if MESH_SIMPLIFIER
			var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
#endif
			var mfs = CollectInstancesThatRequireOptimization(environmentInstanceRoot, maxVertices);
			for (var index = 0; index < mfs.Length; index++)
			{
				var mf = mfs[index];
				if (!mf.TryGetComponent(out MeshCollider col))
					col = mf.gameObject.AddComponent<MeshCollider>();
				if (col.sharedMesh && col.sharedMesh.vertexCount <= maxVertices) continue;
				var input = mf.sharedMesh;
				if (!input) continue;
#if MESH_SIMPLIFIER
				if (TryGetExistingOptimized(input, list, out var existing, out _) && existing.Output && existing.Output.vertexCount > 0)
				{
					col.sharedMesh = existing.Output;
					continue;
				}

				var outputPath = "Assets/ARSimulation/Temp/OptimizedMeshes/" + environmentInstanceRoot.name + "/" + mf.name + "-" + input.name + ".asset";
#if UNITY_EDITOR
				if (existing != null && File.Exists(outputPath))
				{
					var loaded = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
					if (loaded)
					{
						existing.Output = loaded;
						return;
					}
				}
#endif

				try
				{
#if UNITY_EDITOR
					EditorUtility.DisplayProgressBar("Optimizing", "Mesh: " + input.name, index / (float)Mathf.Max(1,mfs.Length));
#endif
					var quality = 1 - Mathf.InverseLerp(maxVertices, 150_000, input.vertexCount);
					quality = Mathf.Max(quality, .04f);
					meshSimplifier.Initialize(input);
					meshSimplifier.SimplifyMesh(quality);
					var output = meshSimplifier.ToMesh();
					output.name = input.name;
					col.sharedMesh = output;
					var entry = new OptimizedMesh()
					{
						Input = input,
						Output = output
					};
					Debug.Log("Optimized " + input + ". Before: " + input.vertexCount + " vertices. After: " + output.vertexCount + " vertices\n" + "Quality: " + quality, input);

#if UNITY_EDITOR
					SaveAsset(output, outputPath);
					InternalEditorUtility.SetIsInspectorExpanded(col, true);
#endif

					if (existing != null)
					{
						existing.Output = output;
					}
					else
						list.Add(entry);
				}
				catch
				{
					// ignore
				}
#else
				//Debug.Log("Install https://github.com/Whinarn/UnityMeshSimplifier to automatically optimize colliders");
				break;
#endif
			}

#if UNITY_EDITOR
			EditorUtility.ClearProgressBar();
#endif
		}

		private static bool TryGetExistingOptimized(Mesh mesh, IList<OptimizedMesh> meshes, out OptimizedMesh optimized, out int index)
		{
			for (var i = 0; i < meshes.Count; i++)
			{
				var m = meshes[i];
				if (m.Input == mesh)
				{
					optimized = m;
					index = i;
					return true;
				}
			}

			optimized = null;
			index = -1;
			return false;
		}

#if UNITY_EDITOR
		private static void SaveAsset(Object asset, string path)
		{
			var dir = Path.GetDirectoryName(path);
			if (string.IsNullOrEmpty(dir)) return;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			if (File.Exists(path)) File.Delete(path);
			Debug.Log("Save asset to " + path, asset);
			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.Refresh();
		}
#endif
	}
}