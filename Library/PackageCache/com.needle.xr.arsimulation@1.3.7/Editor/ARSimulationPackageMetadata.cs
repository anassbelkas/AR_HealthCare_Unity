#if UNITY_XR_MANAGEMENT_3_2_10_OR_ABOVE

using System.Collections.Generic;
using Needle.XR.ARSimulation;
using UnityEditor;
using UnityEngine;
using UnityEditor.XR.Management.Metadata;

namespace Needle.XR.ARSimulation
{
    // ReSharper disable once UnusedType.Global
    internal class XRPackage : IXRPackage
    {
        
        private class ARSimulationLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        private class ARSimulationPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; } 
        }

        private static readonly IXRPackageMetadata s_Metadata = new ARSimulationPackageMetadata()
        {
            packageName = "AR Simulation XR Plugin",
            packageId = "com.needle.xr.arsimulation",
            settingsType = typeof(ARSimulationLoaderSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>() 
            {
                new ARSimulationLoaderMetadata() 
                {
                    loaderName = "AR Simulation",
                    loaderType = ARSimulationXRLoaderSetup.LoaderType,
                    supportedBuildTargets = new List<BuildTargetGroup>() 
                    {
                        BuildTargetGroup.Standalone
                    }
                },
            }
        };

        public IXRPackageMetadata metadata => s_Metadata;

        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            return true;
        }
    }
}

#endif