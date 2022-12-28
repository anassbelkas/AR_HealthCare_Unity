using System;
using System.Collections;
using System.Collections.Generic;
using Needle.XR.ARSimulation;
using Needle.XR.ARSimulation.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace ARSimulationTests.ARRaycast
{
    public class WithCameraAlignedPlane : SetupOnce
    {
        protected override IEnumerator OnSetup()
        {
            TestEnv.Clean();
            yield return Utility.EnsureDefaultSetup();
            yield return new WaitForSeconds(1);
        }

        private static SimulatedARPlane _createdPlane;
        private static IEnumerator EnsurePlane(float distance)
        {
            Utility.DestroyAllARElements();
            _createdPlane = Utility.Create.CameraFacingWithComponent<SimulatedARPlane>(distance);
            yield return new WaitForSeconds(.1f);
            Debug.Assert(_createdPlane, "Failed creating " + nameof(SimulatedARPlane));
            Debug.Assert(Utility.GetARPlanes().Length > 0, "No AR Planes created");
        }
        
        [UnityTest]
        public IEnumerator DoesHitPlane()
        {
            const float planeDist = 1;
            yield return EnsurePlane(planeDist);
            RaycastCenterOfScreen(out var success, out var results);
            Debug.Assert(success, "Failed hitting plane");
            Debug.Assert(results.Count == 1, "Expected 1 hit but got " + results.Count);
            Debug.Assert(results[0].trackableId == _createdPlane.GetId(), "Hit another plane");
            Debug.Log("Hit Plane " + results[0]);
        }

        private static float[] distances = {1, 0.3f, 1.5f, 2, 0.999f};

        [UnityTest]
        public IEnumerator AtDistance([ValueSource(nameof(distances))] float planeDist)
        {
            yield return EnsurePlane(planeDist);
            Debug.Assert(Math.Abs(planeDist - _createdPlane.transform.GetDistanceToARCamera()) < 0.001f, "Created plane distance not as expected");
            RaycastCenterOfScreen(out var success, out var results);
            Debug.Assert(success, "Failed hitting plane");
            Debug.Assert(Math.Abs(results[0].distance - planeDist) < 0.0001f, "Expected hit distance " + planeDist + " but got " + results[0].distance);
            Debug.Assert(Math.Abs(results[0].sessionRelativeDistance - planeDist) < 0.0001f,
                "Expected session relative hit distance " + planeDist + " but got " + results[0].sessionRelativeDistance);
        }

        private static float[] sessionScale = {1, 0.5f, 2, 1.5f, 3, 3.333f};

        [UnityTest]
        public IEnumerator WithScaledSessionOrigin([ValueSource(nameof(sessionScale))] float scale)
        {
            const float planeDist = 1;
            yield return EnsurePlane(planeDist);
            var expectedWorldDistance = planeDist;
            var expectedSessionDistance = planeDist;
            var origin = Object.FindObjectOfType<ARSessionOrigin>();
            origin.transform.localScale = Vector3.one / scale;
            ARSimulationRaycastSubsystem.CallPhysicsSyncTransformsBeforeRaycasting = true;
            RaycastCenterOfScreen(out var success, out var results);
            Debug.Assert(success, "Failed hitting plane");
            Debug.Assert(Math.Abs(results[0].distance - expectedWorldDistance) < 0.0001f,
                "Expected hit distance " + expectedWorldDistance + " but got " + results[0].distance);
            Debug.Assert(Math.Abs(results[0].sessionRelativeDistance - expectedSessionDistance) < 0.0001f,
                "Expected session relative hit distance " + expectedSessionDistance + " but got " + results[0].sessionRelativeDistance);
        }


        private static void RaycastCenterOfScreen(out bool success, out List<ARRaycastHit> results)
        {
            var manager = Object.FindObjectOfType<ARRaycastManager>();
            results = new List<ARRaycastHit>();
            success = manager.Raycast(new Vector2(Screen.width / 2f, Screen.height / 2f), results, TrackableType.All);
        }
    }
}