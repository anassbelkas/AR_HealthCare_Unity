// 🌵 needle - tools for unity

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Application = UnityEngine.Application;
using System.Linq;
using UnityEditor.PackageManager.UI;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable LoopCanBeConvertedToQuery

// ReSharper disable once CheckNamespace
namespace Needle.XR.ARSimulation.Installation
{
    // [CreateAssetMenu] // for development only
    public class GettingStarted : ScriptableObject {
        public string test;
    }

    [CustomEditor(typeof(GettingStarted))]
    public class GettingStartedEditor : Editor {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            GettingStartedWindow.DrawLogo();
            EditorGUILayout.Space();
            
            if(GUILayout.Button("Open Getting Started Window")) {
                GettingStartedWindowShow.ShowWindowNow();
            }    
        }
    }

    [InitializeOnLoad]
    public class GettingStartedWindowShow
    {
        // private const string SettingsPath = "Settings.asset";
        static GettingStartedWindowShow()
        {
            ARSimulationLoader.FirstInstall += OnFirstInstallation;
            
            // var settings = PathHelper.RelativeToScript<ARSimulationSettings>(SettingsPath);
            // bool showWindow = !(settings && !settings.OpenGettingStartedWindow);
            // if (showWindow)
            // {
            //     EditorApplication.update += OnUpdate;
            // }
        }

        private static void OnFirstInstallation()
        {
            ShowWindowNow();
        }

        // private static void OnUpdate()
        // {
        //     EditorApplication.update -= OnUpdate;
        //     
        //     // ARSimulationSettings newSettings = ScriptableObject.CreateInstance<ARSimulationSettings>();
        //     // AssetDatabase.CreateAsset(newSettings, SettingsPath);
        //     // AssetDatabase.SaveAssets();
        //     // AssetDatabase.Refresh();
        //
        //     ShowWindowNow();
        // }

        [MenuItem("Window/AR Simulation/Getting Started")]
        internal static void ShowWindowNow() {
            var window = EditorWindow.GetWindow<GettingStartedWindow>();
            window.Show();
        }
    }
    
    
    public class GettingStartedWindow : EditorWindow
    {
        private static Vector2 GetMinSize => new Vector2(280, 400);
        
        internal void SetPositionToScreenCenter()
        {
            var resFactor = Screen.dpi / 96f;   
            _defaultWindowSize = GetMinSize;
            var initialPosition = 0.5f * (new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) - _defaultWindowSize);
            initialPosition /= resFactor;
            position = new Rect(initialPosition, _defaultWindowSize);
        }
        
        private static GUIStyle veryLargeLabel = null;
        private static GUIStyle subTitleLabel = null;
        private bool _initialized = false;
        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _defaultWindowSize = Vector2.zero;
        private Sample gettingStartedSample;
        private string installedGettingStartedSampleVersion;

        private const string k_SET_POSITION_KEY = "ArSimulation_GettingStarted_SetPositionOnOpen";

        private void OnEnable() 
        {
            _initialized = false;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable() 
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void OnAfterAssemblyReload() 
        {
            _initialized = false;
        }

        private void OnProjectChanged()
        {
            UpdateInstalledSampleList();
        }
        
        private void InitializeWindow()
        {
            if (_initialized)
                return;
            
            _initialized = true;
            titleContent.text = "Getting Started";
            
            var setPosition = !EditorPrefs.HasKey(k_SET_POSITION_KEY) || EditorPrefs.GetBool(k_SET_POSITION_KEY);
            minSize = GetMinSize;
            if (setPosition)
            {
                SetPositionToScreenCenter();
                EditorPrefs.SetBool(k_SET_POSITION_KEY, false);
            }

            EnsureWeGotSampleData();

            UpdateInstalledSampleList();
            // Debug.Log("already installed version: " + installedGettingStartedSampleVersion);
        }

        private void EnsureWeGotSampleData()
        {
            WaitForReasonableSampleData();

            if(gettingStartedSample.displayName != GettingStartedSampleDisplayName) {
                Window.Open(null);
                EditorApplication.update += WaitForReasonableSampleData;
            }
        }

        static readonly string GettingStartedSampleDisplayName = "Getting Started";

        private void WaitForReasonableSampleData()
        {
            var v = GetVersion();

            if(!string.IsNullOrEmpty(v)) {
                var samples = Sample.FindByPackage("com.needle.xr.arsimulation", v);
                if(samples != null)
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    if(!samples.Any()) {
                        // Debug.Log("Package Manager is weird, no samples found. Please restart Unity.");
                    }

                    // foreach(var s in samples) {
                    //     Debug.Log("Sample found: " + s.displayName + ", " + s.importPath + ", " + s.isImported);
                    // }

                    // ReSharper disable once PossibleMultipleEnumeration
                    gettingStartedSample = samples.FirstOrDefault(x => x.displayName == GettingStartedSampleDisplayName);
                    if(gettingStartedSample.displayName == GettingStartedSampleDisplayName) {
                        // Debug.Log("found sample: " + gettingStartedSample);
                        EditorApplication.update -= WaitForReasonableSampleData;
                        ClosePackMan();
                        var window = EditorWindow.GetWindow<GettingStartedWindow>();
                        window.Repaint();
                    }
                }
            }
            else {
                Debug.Log("Version: " + v);
            }
        }

        private void UpdateInstalledSampleList()
        {
            // check installed version
            var installedSamples = SamplesHelper.GetPreviousImports(gettingStartedSample);
            if(installedSamples != null && installedSamples.Any()) {
                var parts = installedSamples[0].Split('\\');
                if (parts.Length >= 2)
                    installedGettingStartedSampleVersion = parts[parts.Length - 2];
            }
            else
                installedGettingStartedSampleVersion = null;
        }

        private static void ClosePackMan() {
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach(var wind in windows) {
                if(wind.titleContent.text == "Package Manager") {
                    wind.Close();
                }
            }
        }

        private static void SeparatorLine() {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GUILayout.Label("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        [System.Serializable]
        private class PackageJson {
            [System.Serializable]
            public class Sample {
                public string displayName = null;
                public string path = null;
            }

            public string version = null;
            public Sample[] samples = null;
        }

        private string _version = null;

        private string GetVersion() {
            if(!string.IsNullOrEmpty(_version)) return _version;

            const string packagePath = "Packages/com.needle.xr.arsimulation/package.json";
            _version = File.Exists(packagePath) ? JsonUtility.FromJson<PackageJson>(File.ReadAllText(packagePath)).version : "Not installed";
            return _version;
        }

        private static class SamplesHelper {
            public static List<string> GetPreviousImports(Sample sample)
            {
                var result = new List<string>();
                if (string.IsNullOrEmpty(sample.importPath)) return result;
                var importDirectoryInfo = new DirectoryInfo(sample.importPath);
                if (!importDirectoryInfo.Parent.Parent.Exists) return result;
                var versionDirs = importDirectoryInfo.Parent.Parent.GetDirectories();
                foreach (var d in versionDirs)
                {
                    var p = Path.Combine(d.ToString(), importDirectoryInfo.Name);
                    if (Directory.Exists(p))
                        result.Add(p);
                }
                return result;
            }
        }

        public static void DrawLogo() {
            if(veryLargeLabel == null) {
                veryLargeLabel = new GUIStyle(EditorStyles.largeLabel);
                veryLargeLabel.alignment = TextAnchor.LowerLeft;
                veryLargeLabel.fontSize *= 2;
            }
            if(subTitleLabel == null) {
                subTitleLabel = new GUIStyle(EditorStyles.miniLabel);
                subTitleLabel.alignment = TextAnchor.LowerLeft;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("AR Simulation", veryLargeLabel, GUILayout.ExpandWidth(false), GUILayout.Height(30));
            GUILayout.Label("by needle", subTitleLabel, GUILayout.ExpandWidth(false), GUILayout.Height(26));
            GUILayout.EndHorizontal();
        }

        private void OnGUI()
        {
            InitializeWindow();

            GUILayout.BeginVertical();
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true));

            EditorGUILayout.Space();
            DrawLogo();

            SeparatorLine();
            
            GUILayout.Label("Getting Started", EditorStyles.largeLabel);

            if (GUILayout.Button("View Online Documentation ↗"))
            {
                Application.OpenURL("https://github.com/needle-tools/ar-simulation");
            }
            
            if (GUILayout.Button("♥ Get from AssetStore ↗"))
                Application.OpenURL("https://buy.needle.tools/ar-simulation");
            
            
            
            // if (GUILayout.Button("PING"))
            // {
            //     PingSample(gettingStartedSample);
            // }

            
            if(gettingStartedSample.isImported) {
                // compare versions
                if(installedGettingStartedSampleVersion != GetVersion()) {
                    if (GUILayout.Button("Update Sample: Getting Started ⚠"))
                    {
                        gettingStartedSample.Import(Sample.ImportOptions.OverridePreviousImports);
                        PingSample(gettingStartedSample);
                    }
                }
                else {
                    using(var scope = new EditorGUI.DisabledScope(true)) {
                        GUILayout.Button("Samples are imported ✓");
                    }
                }
            }
            else if(!string.IsNullOrEmpty(gettingStartedSample.displayName))
            {
                if(installedGettingStartedSampleVersion != null && installedGettingStartedSampleVersion != GetVersion()) {
                    if (GUILayout.Button("Update Sample: Getting Started ⚠"))
                    {
                        gettingStartedSample.Import(Sample.ImportOptions.OverridePreviousImports);
                        AssetDatabase.Refresh();
                        PingSample(gettingStartedSample);
                    }
                }
                else if (GUILayout.Button("Import Sample: Getting Started ↓"))
                {
                    gettingStartedSample.Import(Sample.ImportOptions.None);
                    UpdateInstalledSampleList();
                    AssetDatabase.Refresh();
                    PingSample(gettingStartedSample);
                }
            }
            else {
                // this is weird - it means Unity hasn't parsed the actual sample data
                // from the package for whatever reason.
                if(GUILayout.Button("Open Package Manager to import samples")) {
                    Window.Open("com.needle.xr.arsimulation");
                }
            }


            EditorGUILayout.Space();

            var samplesAreImported = !string.IsNullOrEmpty(installedGettingStartedSampleVersion);

            using (new EditorGUI.DisabledScope(!samplesAreImported))
            {
                if (GUILayout.Button("Open: Plane Tracking Sample"))
                    OpenScene("BasicARPlane.unity");
                if (GUILayout.Button("Open: Image Tracking Sample"))
                    OpenScene("BasicImageTracking.unity");
                if (GUILayout.Button("Open: Simulated Environment Sample"))
                    OpenScene("RaycastPlanesWithCameraImage.unity");
                if (GUILayout.Button("Show: More Sample Scenes"))
                    PingSample(gettingStartedSample);
            }
            
            
            // EditorGUILayout.Space();
            

            // SeparatorLine();
            
            EditorGUILayout.Space();

            GUILayout.Label("Help ☂", EditorStyles.largeLabel);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Discord ↗"))
                Application.OpenURL("https://discord.gg/CFZDp4b");

            if (GUILayout.Button("Twitter ↗"))
                Application.OpenURL("https://twitter.com/needletools");
            
            if (GUILayout.Button("Forum ↗"))
                Application.OpenURL("https://forum.needle.tools/ar-simulation");
            
            if (GUILayout.Button("Email ↗"))
                Application.OpenURL("mailto:help@needle.tools?subject=%E2%9D%A4%EF%B8%8F%20Love%20ARSimulation,%20but%20need%20help%20%F0%9F%9A%A7&body=Invoice%20Number:%20please%20add%20your%20invoice%20number%20here.");
        
            
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Shortcuts", EditorStyles.largeLabel);
            
            if(GUILayout.Button("Open in Package Manager")) 
                Window.Open("com.needle.xr.arsimulation");
            
            if(GUILayout.Button("Open AR Simulation Settings")) 
                SettingsService.OpenProjectSettings("Project/XR Plug-in Management/AR Simulation");


            EditorGUILayout.Space(5);
            GUILayout.Label("current version: "+ GetVersion(), EditorStyles.miniLabel);
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        
        private static void PingSample(Sample sample)
        {
            var path = sample.importPath.Replace(Application.dataPath, "Assets");
            
            // ping PDF so the folder is expanded
            var files = Directory.GetFiles(sample.importPath, "*", SearchOption.AllDirectories);
            var file = files.FirstOrDefault(f => f.EndsWith(".pdf")) ?? files.FirstOrDefault(f => !f.EndsWith(".meta"));
            if (file == null) return;
            file = file.Replace(Application.dataPath, "Assets");
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file);
            EditorGUIUtility.PingObject(asset);
            
            // ping actual folder
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            EditorGUIUtility.PingObject(folder);
            Selection.activeObject = folder;
        }

        private void OpenScene(string sceneName, bool pingOnly = false)
        {
            var fullPath = gettingStartedSample.importPath + "/" + sceneName;

            if(!pingOnly)
                EditorSceneManager.OpenScene(fullPath, OpenSceneMode.Single);
            else {
                var path = fullPath.Replace(Application.dataPath, "Assets");
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }
        }
    }
}
