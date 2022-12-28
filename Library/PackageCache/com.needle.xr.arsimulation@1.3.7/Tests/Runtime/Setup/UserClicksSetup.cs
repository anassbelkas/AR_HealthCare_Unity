using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;
using UnityEngine.XR.ARFoundation;

namespace ARSimulationTests.Setup
{
    public class UserClicksSetup : SetupOnce
    {
        private static IEnumerator CallAutomaticSetupManually() => Utility.EnsureDefaultSetup();
        
        protected override IEnumerator OnSetup()
        {
            TestEnv.Clean();
            yield return CallAutomaticSetupManually();
            yield return new WaitForSeconds(1);
        }
        
        [Test]
        public void CreatesARSession()
        {
            var session = Object.FindObjectOfType<ARSession>();
            Debug.Assert(session, "Should have created an AR session");
        }

        [Test]
        public void CreatesARSessionOrigin()
        {
            var origin = Object.FindObjectOfType<ARSessionOrigin>();
            Debug.Assert(origin, "No session origin created");
            Debug.Assert(Vector3EqualityComparer.Instance.Equals(Vector3.one, origin.transform.lossyScale),
                "AR Session Origin scale is " + origin.transform.lossyScale);
        }

        [Test]
        public void CreatesRaycastManager()
        {
            var manager = Object.FindObjectOfType<ARRaycastManager>();
            Debug.Assert(manager, "No ar raycast manager created");
        }

        [Test]
        public void CreatesARPlaneManager()
        {
            var manager = Object.FindObjectOfType<ARPlaneManager>();
            Debug.Assert(manager, "No ar plane manager created");
        }

        [Test]
        public void CreatesARPlane()
        {
            var plane = Object.FindObjectsOfType<ARPlane>();
            Debug.Assert(plane.Length > 0, "Has no " + nameof(ARPlane) + "s");
            Debug.Assert(plane.Length == 1, "Has not exactly one " + nameof(ARPlane));
        }
    }
}