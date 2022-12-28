using UnityEngine;

namespace Needle.XR.ARSimulation
{
	internal class MeshUtils
	{
		public static Mesh CopyMesh(Mesh other)
		{
			if (!other) return null;
			var copy = new Mesh
			{
				vertices = other.vertices,
				triangles = other.triangles,
				uv = other.uv,
				normals = other.normals,
				colors = other.colors,
				tangents = other.tangents
			};
			return copy;
		}
	}
}