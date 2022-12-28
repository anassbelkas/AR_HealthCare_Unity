using System.Collections.Generic;
using Needle.XR.ARSimulation.Extensions;
using Needle.XR.ARSimulation.Simulation;
using TMPro;
using UnityEngine.Scripting;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// ReSharper disable InconsistentNaming

namespace Needle.XR.ARSimulation
{
    /// <summary>
    /// ARDesktop implementation of the <c>XRRaycastSubsystem</c>. Do not create this directly. Use the <c>SubsystemManager</c> instead.
    /// </summary>
    [Preserve]
    public sealed class ARSimulationRaycastSubsystem : XRRaycastSubsystem
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once ConvertToConstant.Global
        public static bool CallPhysicsSyncTransformsBeforeRaycasting = false;

#if !UNITY_2020_2_OR_NEWER || !UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
        protected override Provider CreateProvider() => new ARSimulationProvider();
#endif

        private class ARSimulationProvider : Provider
        {
            private readonly List<XRRaycastHit> _hits = new List<XRRaycastHit>();
            private readonly RaycastHit[] _raycastHits = new RaycastHit[1024];
            private ARSessionOrigin _sessionOrigin;
            private bool isRunning;

            public override void Start()
            {
                base.Start();
                isRunning = true;
            }

            public override void Stop()
            {
                base.Stop();
                isRunning = false;
            }

            public override NativeArray<XRRaycastHit> Raycast(
                XRRaycastHit defaultRaycastHit,
                Ray ray,
                TrackableType trackableTypeMask,
                Allocator allocator)
            {
                if (!isRunning) return new NativeArray<XRRaycastHit>();
                
                if (!SceneSetup.TryGetARCamera(out var arCamera)) return new NativeArray<XRRaycastHit>();
                var arCameraTransform = arCamera.transform;
                
                // if we use MakeContentAppear another Transform is mixed in and offset so that breaks raycasting pose transformation
                // we get the session from the parent of the camera because that way we either get the session origin transform directly 
                // if MakeContentAppear was never used OR we get the Content offset transform
                // either way: InverseTransformPoint puts us then in correct session space
                var cameraParent = arCameraTransform.parent;
                // ray = cameraParent.TransformRay(ray);
                // ray.origin -= cameraParent.position;

                Debug.DrawLine(ray.origin, ray.origin + ray.direction * 10, Color.gray, .1f);
                // ray.origin -= cameraParent.localPosition * 2;

                // var hitCount = Physics.RaycastNonAlloc(ray, _raycastHits);
                if (CallPhysicsSyncTransformsBeforeRaycasting)
                    Physics.SyncTransforms();
                var hitCount = Physics.RaycastNonAlloc(ray, _raycastHits, 1000);
                var hits = _raycastHits;
                if (!_sessionOrigin) _sessionOrigin = arCameraTransform.GetComponentInParent<ARSessionOrigin>();
                var sessionTransform = _sessionOrigin.transform;
                var origin = ray.origin; //session.InverseTransformPoint(ray.origin);

                if (hitCount > 0)
                {
                    _hits.Clear();
                    
                    for (var i = 0; i < hitCount; i++)
                    {
                        var hit = hits[i];
                        // try get id from hit plane
                        if (!hit.collider.TryGetComponent<ARPlane>(out var plane) && hit.collider.transform.parent != _sessionOrigin.trackablesParent) continue;
                        var id = plane ? plane.trackableId : hit.collider.gameObject.GetTrackableId();
                        var arHit = GetRaycastHit(id, hit.point, hit.normal);
                        _hits.Add(arHit);
                    }

                    if (_hits.Count > 0)
                    {
                        var arr = new NativeArray<XRRaycastHit>(_hits.Count, allocator);
                        for (var k = 0; k < arr.Length; k++)
                            arr[k] = _hits[k];
                        return arr;
                    }
                }

                foreach (var cloud in ARSimulationXRDepthSubsystem.EnumeratePointClouds())
                {
                    for (var index = 0; index < cloud.Positions.Count; index++)
                    {
                        var pt = cloud.Positions[index];
                        Vector3 GetClosestPoint(Vector3 point, Vector3 rayOrigin, Vector3 rayDir) => rayOrigin + Vector3.Project(point - rayOrigin, rayDir);
                        var closest = GetClosestPoint(pt, ray.origin, ray.direction);
                        var diff = closest - pt;
                        var length = diff.magnitude;
                        if (length < .1f)
                        {
                            var arr = new NativeArray<XRRaycastHit>(1, allocator);
                            arr[0] = GetRaycastHit(cloud.Id, closest, (ray.origin - closest).normalized);
                            return arr;
                        }
                    }
                }

                XRRaycastHit GetRaycastHit(TrackableId id, Vector3 point, Vector3 normal)
                {
                    var dist = Vector3.Distance(point, origin) / sessionTransform.localScale.x;
                    // point = cameraParent.InverseTransformPoint(point);
                    // hit.normal = cameraParent.InverseTransformDirection(hit.normal);
                    // Debug.DrawLine(point, point + normal, Color.red, 5);
                    var normalRotation = Quaternion.FromToRotation(Vector3.up, normal);
                    var pose = new Pose(point, normalRotation);
                    pose = cameraParent.InverseTransformPose(pose);
                    return new XRRaycastHit(id, pose, dist, trackableTypeMask);
                }

                return new NativeArray<XRRaycastHit>();
            }

            public override NativeArray<XRRaycastHit> Raycast(
                XRRaycastHit defaultRaycastHit,
                Vector2 screenPoint,
                TrackableType trackableTypeMask,
                Allocator allocator)
            {
                SceneSetup.TryGetARCamera(out var cam);
                if (cam == null) return new NativeArray<XRRaycastHit>();
                var sp = (Vector3) (screenPoint * new Vector2(Screen.width, Screen.height));
                var ray = cam.ScreenPointToRay(sp);
                ray.origin = cam.transform.position;
                Debug.DrawLine(ray.origin, ray.origin + ray.direction * 1.3f, Color.blue, .1f);
                return Raycast(defaultRaycastHit, ray, trackableTypeMask, allocator);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterDescriptor()
        {
            XRRaycastSubsystemDescriptor.RegisterDescriptor(new XRRaycastSubsystemDescriptor.Cinfo
            {
                id = "ARSimulation-Raycast",
#if UNITY_2020_2_OR_NEWER && UNITY_ARSUBSYSTEMS_4_0_1_OR_NEWER
                providerType = typeof(ARSimulationProvider),
                subsystemTypeOverride = typeof(ARSimulationRaycastSubsystem),
#else
                subsystemImplementationType = typeof(ARSimulationRaycastSubsystem),
#endif
                supportsViewportBasedRaycast = true,
                supportsWorldBasedRaycast = true,
                supportedTrackableTypes =
                    (TrackableType.Planes & ~TrackableType.PlaneWithinInfinity) |
                    TrackableType.FeaturePoint
            });
        }
    }
}