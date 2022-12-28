using System.Collections;
using System.Collections.Generic;
using Needle.XR.ARSimulation.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

// ReSharper disable Unity.InefficientPropertyAccess

namespace ARSimulationTests
{
    internal static class DestroyHelper
    {
        public static void DestroySafe<T>(this T o) where T : Object
        {
            if (!o) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }

    internal static class Utility
    {
        public static class Create
        {
            public static T GameObjectWithComponent<T>(string name = null) where T : Component
            {
                var go = new GameObject(name);
                return go.AddComponent<T>();
            }

            public static T CameraFacingWithComponent<T>(float distanceToCamera) where T : Component
            {
                var orig = Object.FindObjectOfType<ARSessionOrigin>();
                var cam = orig.camera;
                var go = new GameObject();
                go.transform.position = cam.transform.position + cam.transform.forward * distanceToCamera;
                go.transform.rotation = Quaternion.LookRotation(cam.transform.up, -cam.transform.forward);
                var plane = go.AddComponent<T>();
                return plane;
            }
        }
        

        public static IEnumerator EnsureDefaultSetup(bool isAutomatic = false)
        {
            yield return null;
            SceneSetup.SetupScene(isAutomatic);
            yield return new WaitForSeconds(0.3f);
        }

        public static ARPlane[] GetARPlanes() => Object.FindObjectOfType<ARSessionOrigin>().trackablesParent.GetComponentsInChildren<ARPlane>();

        public static void DestroyAllARElements()
        {
            var arElements = Object.FindObjectsOfType<SimulatedARElement>();
            foreach (var el in arElements)
            {
                Object.DestroyImmediate(el);
            }
        }

        public static IEnumerator DestroyAllARTrackables()
        {
            Object.FindObjectOfType<ARPlaneManager>().enabled = false;
            Object.FindObjectOfType<ARSession>().Reset();
            yield return new WaitForSeconds(.5f);
            Object.FindObjectOfType<ARPlaneManager>().enabled = true;
        }

        public static Camera GetARCamera()
        {
            var orig = Object.FindObjectOfType<ARSessionOrigin>();
            return orig.camera;
        }

        public static float GetDistanceToARCamera(this Transform t) => Vector3.Distance(t.position, GetARCamera().transform.position);
    }
}