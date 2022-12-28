using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Needle.XR.ARSimulation.Extensions;

namespace Needle.XR.ARSimulation.Simulation.Geometry
{
    public static class Geometry
    {
        public static bool IsOnOnePlane(Vector3 p0, Quaternion r0, Vector3 p1, Quaternion r1, float eps = 0.1f)
        {
            var pt0 = Quaternion.Inverse(r0) * p0;
            var pt1 = Quaternion.Inverse(r1) * p1;
            return Math.Abs(pt0.y - pt1.y) < eps;
        }
        
        public static bool IsOnOnePlane(PlaneData plane0, PlaneData plane1)
        {
            return IsOnOnePlane(plane0.Plane.pose.position, plane0.Plane.pose.rotation, plane1.Plane.pose.position, plane1.Plane.pose.rotation);
        }
        
        public static bool IsConvexHullOverlapping(Vector2 center, IList<Vector2> h1, Vector2 h2Center, IList<Vector2> h2)
        {
            if (Equals(h1, h2)) return true;
            
            for (var i = 0; i < h1.Count; i++)
            {
                var p0 = h1[i] + center;
                var p1 = h1[(i + 1) % h1.Count] + center;
                
                for (var j = 0; j < h2.Count; j++)
                {
                    var p2 = h2[j] + h2Center;
                    var p3 = h2[(j + 1) % h2.Count] + h2Center;
                    if (FasterLineSegmentIntersection(p0, p1, p2, p3))
                        return true;
                }
            }
        
            return false;
        }
        
        // https://forum.unity.com/threads/line-intersection.17384/
        private static bool FasterLineSegmentIntersection (Vector2 p1, Vector2 p12, Vector2 p2, Vector2 p22) {
          
             var a = p12 - p1;
             var b = p2 - p22;
             var c = p1 - p2;
          
             var alphaNumerator = b.y * c.x - b.x * c.y;
             var betaNumerator  = a.x * c.y - a.y * c.x;
             var denominator    = a.y * b.x - a.x * b.y;
          
             if (Math.Abs(denominator) < 0.0001f) {
                 return false;
             } else if (denominator > 0) {
                 if (alphaNumerator < 0 || alphaNumerator > denominator || betaNumerator < 0 || betaNumerator > denominator) {
                     return false;
                 }
             } else if (alphaNumerator > 0 || alphaNumerator < denominator || betaNumerator > 0 || betaNumerator < denominator) {
                 return false;
             }
             return true;
         }

        private static double Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }

        // algorithm from https://stackoverflow.com/questions/14671206/convex-hull-library
        public static List<Vector2> GetConvexHull(List<Vector2> points, bool isSorted = false)
        {
            if (points == null)
                return null;

            if (points.Count <= 1)
                return points;

            if (!isSorted)
                points.OrderClockwise();

            int n = points.Count(), k = 0;
            var hull = new List<Vector2>(new Vector2[2 * n]);

            points.Sort((a, b) =>
                Math.Abs(a.x - b.x) < 0.00001f ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            // Build lower hull
            for (var i = 0; i < n; ++i)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], points[i]) <= 0)
                    k--;
                hull[k++] = points[i];
            }

            // Build upper hull
            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(hull[k - 2], hull[k - 1], points[i]) <= 0)
                    k--;
                hull[k++] = points[i];
            }

            return hull.Take(k - 1).Reverse().ToList();
        }


        #region TRIANGLES

        private static bool BoundaryCollideChk(Triangle t, float eps)
        {
            return t.BoundaryCollideCheck(eps);
        }

        private static bool BoundaryDoesntCollideChk(Triangle t, float eps)
        {
            return t.BoundaryDoesntCollideCheck(eps);
        }

        /// <summary>
        /// Test if two triangles overlap
        /// </summary>
        /// <param name="t1">triangle 1 to check</param>
        /// <param name="t2">triangle 2 to check</param>
        /// <param name="eps">epsilon</param>
        /// <param name="allowReversed">Triangles must be expressed anti-clockwise. If true triangles in clockwise order will be reversed for overlap check</param>
        /// <param name="onBoundary"></param>
        /// <returns></returns>
        public static bool TriangleOverlap(Triangle t1, Triangle t2, float eps = 0.0f, bool allowReversed = false, bool onBoundary = true)
        {
            // Triangles must be expressed anti-clockwise
            t1.CheckTriWinding(allowReversed);
            t2.CheckTriWinding(allowReversed);

            // 'onBoundary' determines whether points on boundary are considered as colliding or not
            var chkEdge = onBoundary ? (Func<Triangle, float, bool>) BoundaryCollideChk : BoundaryDoesntCollideChk;
            var lp1 = new List<Vector2>() {t1.P1, t1.P2, t1.P3};
            var lp2 = new List<Vector2>() {t2.P1, t2.P2, t2.P3};

            // for each edge E of t1
            for (int i = 0; i < 3; i++)
            {
                var j = (i + 1) % 3;
                // Check all points of t2 lay on the external side of edge E.
                // If they do, the triangles do not overlap.
                if (chkEdge(new Triangle(lp1[i], lp1[j], lp2[0]), eps) &&
                    chkEdge(new Triangle(lp1[i], lp1[j], lp2[1]), eps) &&
                    chkEdge(new Triangle(lp1[i], lp1[j], lp2[2]), eps))
                {
                    return false;
                }
            }

            // for each edge E of t2
            for (int i = 0; i < 3; i++)
            {
                var j = (i + 1) % 3;
                // Check all points of t1 lay on the external side of edge E.
                // If they do, the triangles do not overlap.
                if (chkEdge(new Triangle(lp2[i], lp2[j], lp1[0]), eps) &&
                    chkEdge(new Triangle(lp2[i], lp2[j], lp1[1]), eps) &&
                    chkEdge(new Triangle(lp2[i], lp2[j], lp1[2]), eps))
                {
                    return false;
                }
            }

            // The triangles overlap
            return true;
        }

        #endregion
    }
}