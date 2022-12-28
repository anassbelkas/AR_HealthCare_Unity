using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Needle.XR.ARSimulation
{
	public static class RayHelper
	{
		private static ARSessionOrigin _origin;
		private static bool _didSearchForOrigin;
		private static Transform _sessionSpace;

		private static ARSessionOrigin Origin
		{
			get
			{
				if (!_didSearchForOrigin && !_origin)
				{
					_origin = Object.FindObjectOfType<ARSessionOrigin>();
					_didSearchForOrigin = true;
				}
				return _origin;
			}
		}

		private static Transform SessionSpace
		{
			get
			{
				if (Origin && Origin.camera) 
					_sessionSpace = Origin.camera.transform.parent;
				return _sessionSpace;
			}
		}
		
		
		public static Ray ToSessionSpace(Vector3 origin, Vector3 direction)
		{
			return new Ray(origin, direction).ToWorld();
		}
		
		public static Ray ToWorld(this Ray ray)
		{
			if (!SessionSpace) return ray;
			// Debug.DrawRay(ray.origin, ray.direction, Color.blue);
			ray.origin = SessionSpace.InverseTransformPoint(ray.origin);
			ray.direction = SessionSpace.InverseTransformDirection(ray.direction).normalized;
			// Debug.DrawRay(ray.origin, ray.direction, Color.red);
			return ray;
		}
	}
}