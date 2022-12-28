using System;
using System.Collections.Generic;
using System.Linq;
using Needle.XR.ARSimulation.Simulation;
using Needle.XR.ARSimulation.Simulation.Geometry;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Extensions
{
    public static class BoundedPlaneHelper
    {
        public static PlaneAlignment GetAlignment(Vector3 normal, bool normalized = true)
        {
            if (!normalized)
                normal.Normalize();
            var upDot = Vector3.Dot(normal, Vector3.up);
            const float dotTolerance = 0.0001f;
            if (Math.Abs(upDot - 1) < dotTolerance) return PlaneAlignment.HorizontalUp;
            if (Math.Abs(upDot - (-1)) < dotTolerance) return PlaneAlignment.HorizontalDown;
            if (Math.Abs(upDot) < dotTolerance) return PlaneAlignment.Vertical;
            return PlaneAlignment.NotAxisAligned;
        }
        
        public static BoundedPlane ToBoundedPlane(this MeshFilter mf, TrackingState state = TrackingState.Tracking,
            PlaneClassification classification = PlaneClassification.None, Func<Pose, Pose> transformPose = null)
        {
            // if no mesh assigned fallback to use the transform
            if (!mf.sharedMesh) return mf.transform.ToBoundedPlane(state, classification);
            
            var t = mf.transform;
            
            // for a mesh filter we try to calculate the plane size from the mesh bounds
            var size = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, t.lossyScale) * mf.sharedMesh.bounds.size;
            
            var center = Vector2.zero;
            var pos = t.position;
            var rot = t.rotation;
            var alignment = GetAlignment(t.up); // we need an alignment for xr interaction toolkit

            var pose = new Pose(pos, rot);
            if (transformPose != null) pose = transformPose(pose);

            return new BoundedPlane(
                mf.GetTrackableId(),
                TrackableId.invalidId, // The plane which subsumed this one. Use invalidId if it has not been subsumed. 
                pose,
                center,
                new Vector2(size.x, size.z),
                alignment,
                state,
                IntPtr.Zero, // The native pointer associated with the point cloud.
                classification);
        }

        
        public static BoundedPlane ToBoundedPlane(this Transform t, TrackingState state = TrackingState.Tracking,
            PlaneClassification classification = PlaneClassification.None, Func<Pose, Pose> transformPose = null)
        {
            var center = Vector2.zero;
            var pos = t.position;
            var rot = t.rotation;
            var size = t.lossyScale;
            var alignment = GetAlignment(t.up); // we need an alignment for xr interaction toolkit

            var pose = new Pose(pos, rot);
            if (transformPose != null) pose = transformPose(pose);

            return new BoundedPlane(
                t.GetTrackableId(),
                TrackableId.invalidId, // The plane which subsumed this one. Use invalidId if it has not been subsumed. 
                pose,
                center,
                new Vector2(size.x, size.z),
                alignment,
                state,
                IntPtr.Zero, // The native pointer associated with the point cloud.
                classification);
        }

        public static BoundedPlane ToBoundedPlane(IList<Vector3> points, Vector3 normal, ref List<Vector2> localPoints, float offset = 0.0001f, Func<Pose, Pose> transformPose = null)
        {
            if (points == null)
            {
                throw new NullReferenceException("Hits are null");
            }

            if (points.Count < 3)
            {
                Debug.LogWarning("Expecting at least 3 points but got " + points.Count);
                return BoundedPlane.defaultValue;
            }

            var forward = (points[0] - points[2]).normalized;
            if(forward.magnitude <= 0.0001f) return BoundedPlane.defaultValue;
            var rotation = Quaternion.LookRotation(forward, normal.normalized);
            var inverseRotation = Quaternion.Inverse(rotation).normalized;
            var center = new Vector3();
            center = points.Aggregate(center, (current, pt) => current + pt);
            center /= points.Count;
            center += normal * offset;

            var min2 = new Vector2();
            var max2 = new Vector2();
            for (var i = 0; i < points.Count; i++)
            {
                var hit = points[i];
                var p = hit - center;
                p = inverseRotation * p;
                // calc bounds
                min2.x = Mathf.Min(p.x, min2.x);
                min2.y = Mathf.Min(p.z, min2.y);
                max2.x = Mathf.Max(p.x, max2.x);
                max2.y = Mathf.Max(p.z, max2.y);

                localPoints?.Add(new Vector2(p.x, p.z));
            }
            
            var size = max2 - min2;

            localPoints = Geometry.GetConvexHull(localPoints);
            var alignment = GetAlignment(normal);


            var pose = new Pose(center, rotation);
            if (transformPose != null) pose = transformPose(pose);
            
            return new BoundedPlane(
                TrackableIdHelper.GenerateRandomId(),
                TrackableId.invalidId,
                pose,
                Vector2.zero,
                size,
                alignment,
                TrackingState.Tracking,
                IntPtr.Zero,
                PlaneClassification.None);
        }

        internal static PlaneData FromPlane(this BoundedPlane plane, IReadOnlyList<Vector2> bounds = null, bool allowMerging = true)
        {
            return new PlaneData(plane, bounds, allowMerging);
        }
    }
}