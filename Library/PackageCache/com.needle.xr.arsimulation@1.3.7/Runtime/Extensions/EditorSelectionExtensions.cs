#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Needle.XR.ARSimulation.Extensions
{
	internal static class EditorSelectionExtensions
	{
		public static bool AnyChildSelected(GameObject go, bool includeSelf)
		{
			if (!go) return false;
			if (includeSelf && Selection.Contains(go)) return true;
			for (var i = 0; i < go.transform.childCount; i++)
			{
				var child = go.transform.GetChild(i);
				if (AnyChildSelected(child.gameObject, true))
					return true;
			}
			return false;
		}
	}
}
#endif