using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace ARSimulationTests
{
    public class TestEnv : IDisposable
    {
        private static readonly List<TestEnv> activeSetups = new List<TestEnv>();

        private static void Register(TestEnv testEnv)
        {
            if (testEnv != null && !activeSetups.Contains(testEnv))
                activeSetups.Add(testEnv);
        }

        private static void EnsureClean()
        {
            Debug.Log("Clean test environment");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var roots = scene.GetRootGameObjects();
                var gos = new HashSet<GameObject>();
                var dontDestroy = new HashSet<GameObject>();
                foreach (var root in roots)
                {
                    var components = root.GetComponentsInChildren<Component>();
                    foreach (var component in components)
                    {
                        if (!component) continue;
                        // if (component is Transform) continue;
                        if (component.GetType().Name == "PlaymodeTestsController")
                        {
                            if (!dontDestroy.Contains(component.gameObject))
                                dontDestroy.Add(component.gameObject);
                            continue;
                        }

                        if (!gos.Contains(component.gameObject))
                        {
                            gos.Add(component.gameObject);
                        }
                    }
                }

                foreach (var go in gos)
                {
                    if(!go) continue;
                    if (dontDestroy.Contains(go)) continue;
                    go.DestroySafe();
                }
                gos.Clear();
            }
        }

        public static TestEnv Clean([CallerMemberName] string callerName = "")
        {
            EnsureClean();
            // var name = (!string.IsNullOrEmpty(callerName) ? callerName : nameof(TestEnv)) + "-" + DateTime.Now;
            // Debug.Log("Create empty scene: " + name);
            var setup = new TestEnv();
            Register(setup);
            return setup;
        }

        public void Dispose()
        {
        }
        
    }
}