using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Object = UnityEngine.Object;

namespace Needle.XR.ARSimulation.Utilities
{
    public static class PlacementHelper
    {
        private static ARSessionOrigin _sessionOrigin;
        private static ARSessionOrigin sessionOrigin
        {
            get
            {
                if (!_sessionOrigin) _sessionOrigin = Object.FindObjectOfType<ARSessionOrigin>();
                return _sessionOrigin;
            }
        }

        public static Camera ARCamera => sessionOrigin ? sessionOrigin.camera : null;

        public static bool IsTrackable(Transform t)
        {
            if (!t.parent) return false;
            var so = sessionOrigin;
            if (!so) return false;
            return so.trackablesParent == t.parent;
        }

        public static Vector3 GetRayDirection(this ARRaycastHit hit, ARSessionOrigin session = null)
        {
            return hit.GetRayVector(session).normalized;
        }

        public static Vector3 GetRayVector(this ARRaycastHit hit, ARSessionOrigin session = null)
        {
            if (!session) session = sessionOrigin;
            if (!session || session == null) throw new Exception("Missing session");
            var dir = session.camera.transform.localPosition - hit.sessionRelativePose.position;
            return dir;
        }
    }
}