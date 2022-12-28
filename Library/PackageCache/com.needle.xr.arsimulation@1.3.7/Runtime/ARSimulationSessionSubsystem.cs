using Needle.XR.ARSimulation.Simulation;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// ARDesktop implementation of the <c>XRSessionSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationSessionSubsystem : XRSessionSubsystem
    {
	    public static bool IsRunning { get; private set; }

#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        /// <summary>
        /// Creates the provider interface.
        /// </summary>
        /// <returns>The provider interface for ARDesktop</returns>
        protected override Provider CreateProvider() => new ARSimulationProvider(this);
#endif
        
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override void OnCreate()
        {
        }
#endif

        private class ARSimulationProvider : Provider
        {
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            public ARSimulationProvider()
            {
                // we need a default constructor now
            }
#else
            private readonly ARSimulationSessionSubsystem m_Subsystem;
            public ARSimulationProvider(ARSimulationSessionSubsystem subsystem)
            {
                m_Subsystem = subsystem;
            }
#endif

            private bool hasStarted;

#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            public override void Start()
            {
	            base.Start();
#else
            public override void Resume()
            {
	            base.Resume();
#endif
	            
	            IsRunning = true;
                if (!Application.isPlaying || hasStarted) return;
                hasStarted = true;
                // SceneSetup.SetupScene(true);
            }

            public override void Reset()
            {
	            base.Reset();
	            
#if UNITY_EDITOR
	            SceneSetup.ClearPreviousData();
#endif
	            
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                if (running)
                {
                    Start();
                }
#else
                if (m_Subsystem.running)
                {
                    Resume();
                }
#endif
            }

#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
            public override void Stop()
            {
                base.Stop();
                IsRunning = false;
            }
#else
	        public override void Pause()
	        {
		        base.Pause();
		        IsRunning = false;
	        }
#endif


	        public override void Update(XRSessionUpdateParams updateParams)
            {
            }

            public override void Destroy()
            {
	            base.Destroy();
	            IsRunning = false;
            }

            public override void OnApplicationPause()
            {
            }

            public override void OnApplicationResume()
            {
            }

            public override Promise<SessionAvailability> GetAvailabilityAsync()
            {
                return Promise<SessionAvailability>.CreateResolvedPromise(SessionAvailability.Installed | SessionAvailability.Supported);
            }

            public override Promise<SessionInstallationStatus> InstallAsync()
            {
                return Promise<SessionInstallationStatus>.CreateResolvedPromise(SessionInstallationStatus.Success);
            }

            public override TrackingState trackingState => TrackingState.Tracking; // NativeApi.UnityARDesktop_session_getTrackingState();

            public override NotTrackingReason notTrackingReason => NotTrackingReason.None; // NativeApi.UnityARDesktop_session_getNotTrackingReason();

            public override int frameRate => 30;


#if UNITY_AR_FOUNDATION_4_OR_NEWER
            private bool _matchFrameRate;
            public override bool matchFrameRateEnabled => _matchFrameRate;
            public override bool matchFrameRateRequested
            {
                get => matchFrameRateEnabled;
                set => _matchFrameRate = value;
            }
#else
            public override bool matchFrameRate { get; set; }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
#if (!UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR
            XRSessionSubsystemDescriptor.RegisterDescriptor(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = "ARSimulation-Session",
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationSessionSubsystem),
#else
                subsystemImplementationType = typeof(ARSimulationSessionSubsystem),
#endif
                supportsInstall = true,
                supportsMatchFrameRate = true
            });
#endif
        }
    }
}