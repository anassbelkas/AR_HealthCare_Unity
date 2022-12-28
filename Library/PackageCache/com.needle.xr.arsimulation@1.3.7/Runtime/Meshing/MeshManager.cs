#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
#define MESH_MANAGER_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Object = UnityEngine.Object;
#if UNITY_EDITOR && MESH_MANAGER_SUPPORTED
using UnityEditor;
using UnityEngine.XR.ARFoundation;

#endif

namespace Needle.XR.ARSimulation
{
    public static class MeshManager
    {
        internal static void Clear()
        {
            foreach (var md in meshData)
                Native.RemoveMesh(md.Value.info);
            meshData.Clear();
        }

        public static bool IsActive
        {
            get
            {
#if MESH_MANAGER_SUPPORTED
                return _activeMeshingInstance != null && _activeMeshingInstance.running;
#else
                return false;
#endif
            }
        }

        public static void RegisterOrUpdate(IMeshProvider prov)
        {
#if MESH_MANAGER_SUPPORTED
            if (prov == null) return;
            if (meshData.ContainsKey(prov)) Update(prov);
            else Register(prov);
#endif
        }

        public static void Register(IMeshProvider prov)
        {
#if MESH_MANAGER_SUPPORTED
            if (!Application.isPlaying) return;
            if (prov == null) return;
            if (meshData.ContainsKey(prov)) return;
            if (!prov.Mesh) return;
            if (!added.Contains(prov)) added.Add(prov);
            if (updated.Contains(prov)) updated.Remove(prov);
            if (removed.Contains(prov)) removed.Remove(prov);
#endif
        }

        public static void Update(IMeshProvider prov)
        {
#if MESH_MANAGER_SUPPORTED
            if (!Application.isPlaying) return;
            if (prov == null) return;
            if (!meshData.ContainsKey(prov)) return;
            if (!prov.Mesh) return;
            if (added.Contains(prov)) added.Remove(prov);
            if (!updated.Contains(prov)) updated.Add(prov);
            if (removed.Contains(prov)) removed.Remove(prov);
#endif
        }

        public static void Unregister(IMeshProvider prov)
        {
#if MESH_MANAGER_SUPPORTED
            if (prov == null) return;
            if (!meshData.ContainsKey(prov)) return;
            if (added.Contains(prov)) added.Remove(prov);
            if (updated.Contains(prov)) updated.Remove(prov);
            if (!removed.Contains(prov)) removed.Add(prov);
#endif
        }

        public static int GetInternalMeshCount()
        {
#if MESH_MANAGER_SUPPORTED
            return Native.GetMeshCount();
#else
            return 0;
#endif
        }

        private static readonly Dictionary<IMeshProvider, (MeshInfo info, XRMeshDescriptor desc)> meshData
            = new Dictionary<IMeshProvider, (MeshInfo, XRMeshDescriptor)>();

        private static readonly List<IMeshProvider>
            added = new List<IMeshProvider>(),
            updated = new List<IMeshProvider>(),
            removed = new List<IMeshProvider>();

        private static void OnInternalAdd()
        {
            foreach (var prov in added) 
            {
                try
                {
                    var mesh = prov.Mesh;
                    var id = MeshIdExtensions.GetId(prov.Id, 0);
                    if (meshData.ContainsKey(prov)) continue;
                    var info = id.GetInfo(MeshChangeState.Added, 0);
                    var desc = mesh.ToDescriptor();
                    if (prov is IMeshProviderWithMatrix provMat)
                    {
                        if (provMat.ApplyLocalToWorld)
                            desc = desc.Apply(provMat.LocalToWorld);
                    }

                    meshData.Add(prov, (info, desc));
                    Native.AddOrUpdateMesh(info, desc.vertexCount, desc.positions, desc.indexCount, desc.indices16, desc.normals);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            added.Clear();
        }

        private static void OnInternalUpdate()
        {
            foreach (var prov in updated)
            {
                try
                {
                    if (!meshData.ContainsKey(prov)) continue;
                    var mesh = prov.Mesh;
                    var id = MeshIdExtensions.GetId(prov.Id, 0);
                    var info = id.GetInfo(MeshChangeState.Updated, 0);
                    var desc = mesh.ToDescriptor();
                    if (prov is IMeshProviderWithMatrix provMat)
                    {
                        if (provMat.ApplyLocalToWorld)
                            desc = desc.Apply(provMat.LocalToWorld);
                    }

                    Native.AddOrUpdateMesh(info, desc.vertexCount, desc.positions, desc.indexCount, desc.indices16, desc.normals);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            updated.Clear();
        }

        private static void OnInternalRemove()
        {
            foreach (var prov in removed)
            {
                try
                {
                    if (!meshData.ContainsKey(prov)) continue;
                    var (info, _) = meshData[prov];
                    meshData.Remove(prov);
                    if (!Native.RemoveMesh(info))
                    {
                        Debug.LogError("Failed removing mesh " + info);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            removed.Clear();
        }

        private static void OnHandleChanges()
        {
            Native.SetMeshesUnchanged();
            OnInternalAdd();
            OnInternalUpdate();
            OnInternalRemove();
        }

#if MESH_MANAGER_SUPPORTED
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            try { Native.ClearData(); } catch(DllNotFoundException) { /*ignore, this happens when starting the editor, the dll has not been loaded yet */ }
            FixMeshingSetup();
        }

        private static void FixMeshingSetup()
        {
            if (!Application.isPlaying) return;
            // make sure MesManager assigned prefab has no mesh
            // and if it has a collider it is a MeshCollider without assigned mesh 
            var mm = Object.FindObjectOfType<ARMeshManager>();
            if (mm && mm.meshPrefab)
            {
                // if we dont do this the assigned mesh will be broken
                // and also runtime will not render correctly
                mm.meshPrefab.sharedMesh = null;
                // Debug.LogWarning("ARMeshing prefab should not have a mesh assigned", mm.meshPrefab);

                if (mm.meshPrefab.TryGetComponent(out Collider collider))
                {
                    if (collider is MeshCollider meshCollider)
                    {
                        meshCollider.sharedMesh = null;
                        // Debug.LogWarning("ARMeshing collider should not have a mesh assigned", meshCollider);
                    }
                    else
                    {
                        Debug.LogWarning("ARMeshing prefab does not have a MeshCollider component. If you expect correct collision please add a MeshCollider.", collider);
                    }
                }
            }
        }
#endif
        // TODO: for standalone support we would need someone else to call update
        private static void OnUpdate()
        {
            if (!Application.isPlaying) return;
            if (_activeMeshingInstance == null || !_activeMeshingInstance.running)
            {
                _activeMeshingInstance = GetActiveSubsystemInstance();
                return;
            }

            OnHandleChanges();
        }
#endif

        private static XRMeshSubsystem _activeMeshingInstance;

        private static XRMeshSubsystem GetActiveSubsystemInstance()
        {
            XRMeshSubsystem activeSubsystem = null;
            // Query the currently active loader for the created subsystem, if one exists.
            if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null) return null;
            var loader = XRGeneralSettings.Instance.Manager.activeLoader;
            if (loader == null || loader.GetType() != typeof(ARSimulationLoader)) return null;
            activeSubsystem = loader.GetLoadedSubsystem<XRMeshSubsystem>();
            return activeSubsystem;
        }
    }
}
