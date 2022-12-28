using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Needle.XR.ARSimulation;
using Needle.XR.ARSimulation.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ARSimulationTests.CreateDestroy
{   
    public class ARPointCloud
    {
        [UnityTest]
        public IEnumerator InTheSameFrame()
        {
            TestEnv.Clean();
            yield return Utility.EnsureDefaultSetup();
            var pc = Utility.Create.GameObjectWithComponent<SimulatedARPointCloud>();
            SimulatedARPointCloudRegistry.Register(pc);
            Debug.Assert(SimulatedARPointCloudRegistry.AddedList.Any(e => pc.Id == e.Id));
            SimulatedARPointCloudRegistry.Unregister(pc);
            Debug.Assert(SimulatedARPointCloudRegistry.AddedList.All(e => pc.Id != e.Id));
            Debug.Assert(SimulatedARPointCloudRegistry.RemovedList.Any(e => e == pc.Id));
        }
    }
}