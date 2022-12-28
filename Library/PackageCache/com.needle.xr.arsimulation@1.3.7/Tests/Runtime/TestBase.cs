using System.Collections;
using UnityEngine.TestTools;

namespace ARSimulationTests
{
    public abstract class TestBase
    {
        [UnitySetUp]
        protected abstract IEnumerator Setup();
    }

    public abstract class SetupOnce : TestBase
    {
        private bool didSetup = false;

        [UnitySetUp]
        protected sealed override IEnumerator Setup()
        {
            if (!didSetup)
                yield return OnSetup();
            didSetup = true;
        }

        protected abstract IEnumerator OnSetup();
    }
}