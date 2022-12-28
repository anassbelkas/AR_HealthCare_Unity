using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARSimulationTests.Setup
{
    public class Automatic : SetupOnce
    {
        private static IEnumerator CallSetupImplicit() => Utility.EnsureDefaultSetup(true);
        
        protected override IEnumerator OnSetup()
        {
            TestEnv.Clean();
            yield return CallSetupImplicit();
            yield return new WaitForSeconds(1);
        }
        
        [Test]
        public void CreatesNotARSession()
        {
            var session = Object.FindObjectOfType<ARSession>();
            Debug.Assert(!session, "Created ar session during automatic setup");
        }
    
        [Test]
        public void CreatesNotARSessionOrigin()
        {
            var origin = Object.FindObjectOfType<ARSessionOrigin>();
            Debug.Assert(!origin, "Created ar session origin during automatic setup");
        }
    }
}