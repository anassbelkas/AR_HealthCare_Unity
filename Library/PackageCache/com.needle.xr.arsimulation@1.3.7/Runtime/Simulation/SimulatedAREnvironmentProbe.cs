using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Needle.XR.ARSimulation;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Interfaces;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Simulation
{
    /// <summary>
    /// Currently not supported due to unity issue when copying cubemap
    /// </summary>
    public class SimulatedAREnvironmentProbe : MonoBehaviour, IProbeProvider
    {
        public Cubemap Cubemap;
        public Vector3 ProbeScale = Vector3.one;
        public TrackingState State = TrackingState.Tracking;
        
        public TrackableId TrackableId => this.GetTrackableId();
        public Cubemap Texture => Cubemap;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        public Vector3 Scale => ProbeScale;
        public Vector3 Size => transform.lossyScale;
        public TrackingState TrackingState => State;
        // not sure if pointer should point to texture of this component
        // code comment for XREnvironmentProbe says:
        /// A native pointer associated with this environment probe.
        /// The data pointed to by this pointer is implementation-defined. Its lifetime
        /// is also implementation-defined, but will be valid at least until the next
        /// call to <see cref="XREnvironmentProbeSubsystem.GetChanges(Allocator)"/>.
        public IntPtr NativePointer => GCHandle.ToIntPtr(GCHandle.Alloc(this));

        private void OnEnable()
        {
            SimulatedARProbesRegistry.Add(this);
        }    

        private void OnDisable()
        {
            SimulatedARProbesRegistry.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }

        public Cubemap clone;
        public Texture2D tx2;

        public ReflectionProbe probe;

        [ContextMenu("Copy Texture Now Hacky")]
        private void CopyNow() {
            var ptr = Cubemap.GetNativeTexturePtr();
            clone = UnityEngine.Cubemap.CreateExternalTexture(Cubemap.width, Cubemap.format, false, ptr);

            tx2 = new Texture2D(Cubemap.width, Cubemap.height, Cubemap.format, false, false);
            Graphics.CopyTexture(clone, 0, 0, tx2, 0, 0);

            probe.customBakedTexture = clone;
        }
    }
}
