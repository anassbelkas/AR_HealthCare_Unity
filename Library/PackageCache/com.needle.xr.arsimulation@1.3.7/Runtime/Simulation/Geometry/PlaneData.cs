using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Needle.XR.ARSimulation.Simulation.Geometry
{
    public readonly struct PlaneData : IEquatable<PlaneData>
    {
        public readonly BoundedPlane Plane;
        public readonly Vector2 BoundsCenter;
        public readonly IList<Vector2> Bounds;
        public bool HasBounds => Bounds != null && Bounds.Count >= 3;
        public TrackableId Id => Plane.trackableId;
        public readonly bool AllowMerging;

        public PlaneData(BoundedPlane plane, IReadOnlyCollection<Vector2> bounds, bool allowMerging = true)
        {
            if (bounds != null && bounds.Count < 3)
            {
                throw new Exception("Require 3 points to form a bounded plane");
            }

            if (bounds == null)
            {
                this.Bounds = new List<Vector2>(4)
                {
                    plane.center + plane.extents,
                    plane.center + new Vector2(plane.extents.x, -plane.extents.y),
                    plane.center - plane.extents,
                    plane.center + new Vector2(-plane.extents.x, plane.extents.y)
                };
            }
            else 
                this.Bounds = new List<Vector2>(bounds);

            this.AllowMerging = allowMerging;
            this.Plane = plane;
            this.BoundsCenter = new Vector2(plane.pose.position.x, plane.pose.position.z);
        }

        public static bool operator ==(PlaneData lhs, PlaneData rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(PlaneData lhs, PlaneData rhs)
        {
            return !lhs.Equals(rhs);
        }


        public bool Equals(PlaneData other)
        {
            return Plane.Equals(other.Plane) && BoundsCenter.Equals(other.BoundsCenter) && Equals(Bounds, other.Bounds);
        }

        public override bool Equals(object obj)
        {
            return obj is PlaneData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Plane.GetHashCode();
                hashCode = (hashCode * 397) ^ BoundsCenter.GetHashCode();
                hashCode = (hashCode * 397) ^ (Bounds != null ? Bounds.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}