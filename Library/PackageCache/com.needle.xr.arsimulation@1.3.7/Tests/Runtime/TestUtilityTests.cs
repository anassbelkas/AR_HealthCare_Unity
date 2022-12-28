using System.Collections;
using Needle.XR.ARSimulation.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace ARSimulationTests
{
    public class TestUtilityTests
    {

        [UnityTest]
        public IEnumerator DestroyAllARElements()
        {
            yield return new WaitForSeconds(1);
            var elements = Object.FindObjectsOfType<SimulatedARElement>();
            Debug.Log("Before: Have " + elements.Length + " ARElements");
            // Debug.LogAssertion("Have " + elements.Length + " ARElements");
            Utility.DestroyAllARElements();
            elements = Object.FindObjectsOfType<SimulatedARElement>();
            Debug.Log("After: Have " + elements.Length + " ARElements");
            Debug.Assert(elements.Length <= 0, "Got: " + elements.Length);
        }
    }
}