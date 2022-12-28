#if UNITY_XR_MANAGEMENT_3_2_10_OR_ABOVE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management.Metadata;

namespace Needle.XR.ARSimulation
{
	public static class ARSimulationXRLoaderSetup
	{
		public static string LoaderType => typeof(ARSimulationLoader).FullName;

		private static bool DidLogAdded
		{
			get => SessionState.GetBool("ARSimAddedLog", false);
			set => SessionState.SetBool("ARSimAddedLog", value);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		[InitializeOnLoadMethod]
		private static void Init()
		{
			if (!BuildPipeline.isBuildingPlayer && !Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
			{
				ARSimulationLoader.FirstInstall += async () =>
				{
					while (!XRGeneralSettings.Instance || !XRGeneralSettings.Instance.Manager) await Task.Delay(500);
					XRPackageMetadataStore.AssignLoader(XRGeneralSettings.Instance.Manager, LoaderType, BuildTargetGroup.Standalone);
				};
				ARSimulationLoader.RequestLoaderActivation += () => EnsureXRLoaderEnabled();
			}

			var settings = ARSimulationLoader.GetSettings();
			if (!Application.isPlaying)
			{
				if (settings && settings.allowAddXRLoader) EnsureXRLoaderEnabled(); 
			}
			
			void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange change) 
			{
				switch (change)
				{
					case PlayModeStateChange.ExitingEditMode:
						if (BuildPipeline.isBuildingPlayer) return;
						if (settings && settings.allowAddXRLoader) EnsureXRLoaderEnabled(); 
						break;
				}
			}
			
			EditorApplication.playModeStateChanged -= OnEditorApplicationOnplayModeStateChanged;
			EditorApplication.playModeStateChanged += OnEditorApplicationOnplayModeStateChanged;
		}

		private static bool didTryOpenProjectWindow;
		private static EditorWindow projectSettings;

		/// <summary>
		/// Make sure xr loader is added to xr plug in management
		/// </summary>
		/// <returns>true if added to plug in management</returns>
		private static bool EnsureXRLoaderEnabled()
		{
			if (BuildPipeline.isBuildingPlayer) return false;
			
			var inst = Resources.FindObjectsOfTypeAll<ARSimulationLoader>().LastOrDefault();
			if (!inst)
			{
				inst = ScriptableObject.CreateInstance<ARSimulationLoader>();
			}

			XRGeneralSettings.Instance = XRGeneralSettings.Instance
				? XRGeneralSettings.Instance
				: Resources.FindObjectsOfTypeAll<XRGeneralSettings>().FirstOrDefault();
			if (!inst || XRGeneralSettings.Instance == null || !XRGeneralSettings.Instance)
			{ 
				// see issue https://github.com/needle-tools/ar-simulation/issues/28
				if (!didTryOpenProjectWindow)
				{
					// Debug.Log("XR Management is not initialized. Please open \"Project Settings/XR Plug-in Management\" and enable AR Simulation");
					didTryOpenProjectWindow = true;
					// project settings window is initializing XRManagement. Dont ask me why
					try
					{
						projectSettings = SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
					}
					catch
					{
						// ignored
					}

					return EnsureXRLoaderEnabled();
				}

				return false;
				// in a new editor/project without having used XR management there might be no settings asset
				// xr management seems to create it when opening and selecting the preferences window
				// we want to set it up automatically here though on install so if there is no instance already
				// if(!CreateSettingsIfNonExist() || XRGeneralSettings.Instance == null)
				//     return false;
			}

			if (didTryOpenProjectWindow && projectSettings)
			{
				projectSettings.Close();
			}

			void LogARSimulationInstalledMessage()
			{
				Debug.Log(
					"<b>[AR Simulation]</b> Enabled XR Management Loader ✔ <color=#777777>If you dont want automatic XR Loader activation go to " +
					"\"Project Settings/XR Plug-In Management/AR Simulation\" and uncheck \"" +
					ObjectNames.NicifyVariableName(nameof(ARSimulationLoaderSettings.allowAddXRLoader)) + "\"</color>\n\n",
					XRGeneralSettings.Instance);
			}

			if (!XRGeneralSettings.Instance.Manager) return false;
#if UNITY_XR_MANAGEMENT_4_0_1_OR_ABOVE
			var settings = ARSimulationLoader.GetSettings();
			if (settings.allowAddXRLoader)
			{
				XRGeneralSettings.Instance.Manager.automaticLoading = true;
				XRGeneralSettings.Instance.Manager.automaticRunning = true; 
			}

			var debugLog = (settings && settings.allowAutoSetupLogging);

			var res = XRGeneralSettings.Instance.Manager.TryAddLoader(inst); 
			// var res = XRPackageMetadataStore.AssignLoader(XRGeneralSettings.Instance.Manager, LoaderType, BuildTargetGroup.Standalone);
			if (res)
			{
				// if (EditorApplication.isPlayingOrWillChangePlaymode)
				// {
				// 	EditorUtility.SetDirty(XRGeneralSettings.Instance.Manager);
				// 	AssetDatabase.SaveAssets();
				// }
				// internal static void InstallPackageAndAssignLoaderForBuildTarget(string package, string loaderType, BuildTargetGroup buildTargetGroup)
				// var m = typeof(XRPackageMetadataStore).GetMethod("InstallPackageAndAssignLoaderForBuildTarget", BindingFlags.Static | BindingFlags.NonPublic);
				// m?.Invoke(null, new object[] {"com.needle.xr.arsimulation", LoaderType, BuildTargetGroup.Standalone});
				// if(Application.isPlaying)
				// 	XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
				if (!DidLogAdded || debugLog)
				{
					DidLogAdded = true;
					LogARSimulationInstalledMessage(); 
				}
			}
			else if(debugLog) Debug.LogWarning("Failed adding AR Simulation Loader to XR-Plugin-Management");

			return res;
#else
#pragma warning disable 618
            var loaders = XRGeneralSettings.Instance.Manager.loaders;
#pragma warning restore 618
            if (loaders == null) return false;
            // if not in list already
            if (loaders.Any(l => l is ARSimulationLoader)) return true;
            if (IsOtherPackageEnabledForSamePlatform(loaders)) return false;
            loaders.Add(inst);
            if(!Application.isPlaying)
                XRPackageMetadataStore.AssignLoader(XRGeneralSettings.Instance.Manager, LoaderType, BuildTargetGroup.Standalone);
            EditorUtility.SetDirty(XRGeneralSettings.Instance.Manager);
            EditorUtility.SetDirty(XRGeneralSettings.Instance);
            AssetDatabase.SaveAssets();
			LogARSimulationInstalledMessage();
			return true;
#endif
		}

		private static FieldInfo packagesField;

		private static bool IsOtherPackageEnabledForSamePlatform(IList<XRLoader> loaders)
		{
			if (loaders.Count <= 0) return false;
			if (packagesField == null)
			{
				packagesField = typeof(XRPackageMetadataStore).GetField("s_Packages", (BindingFlags) ~0);
				if (packagesField == null) return false;
			}

			var dict = packagesField.GetValue(null) as Dictionary<string, IXRPackage>;
			if (dict == null) return false;
			foreach (var kvp in dict)
			{
				var package = kvp.Value;
				if (package == null) continue;
				var meta = package.metadata;
				if (meta == null) continue;
				foreach (var loaderMeta in meta.loaderMetadata)
				{
					if (loaderMeta == null) continue;
					if (loaders.Any(l => l != null && l.GetType().FullName == loaderMeta.loaderType))
					{
						return true;
					}
				}
			}

			return false;
		}

		// the following code is unfortunately not enough. an instance might be created but the manager is still missing
		// private static XRGeneralSettings CreateSettingsIfNonExist()
		// {
		//     if (XRGeneralSettings.Instance) return XRGeneralSettings.Instance;
		//     // more or less how XRSettingsManager creates the instance
		//     var generalSettings = ScriptableObject.CreateInstance(typeof(XRGeneralSettings)) as XRGeneralSettings;
		//     var assetPath = "Assets/XR/";
		//     if (!string.IsNullOrEmpty(assetPath))
		//     {
		//         assetPath += "/XRGeneralSettings.asset";
		//         AssetDatabase.CreateFolder("Assets", "XR");
		//         AssetDatabase.CreateAsset(generalSettings, assetPath);
		//     }
		//     EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, generalSettings, true);
		//     XRGeneralSettings.Instance = generalSettings;
		//     return XRGeneralSettings.Instance;
		// }
	}
}


#endif