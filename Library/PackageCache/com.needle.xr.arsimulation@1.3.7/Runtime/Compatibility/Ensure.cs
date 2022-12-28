using UnityEngine;

namespace Needle.XR.ARSimulation.Compatibility
{
    public static class Ensure
    {
        public static bool CorrectInputSystemConfiguration()
        {
#if !UNITY_NEW_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Debug.LogError(
                "Your project is configured to use the new input system, " +
                "but the package is not installed. " +
                "Please open the PackageManager window and install the Input System package");
            return false;
#else
            return true;
#endif
        }
    }
}