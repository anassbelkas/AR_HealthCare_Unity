using System;
using UnityEngine;

namespace Needle.XR.ARSimulation.Simulation.Geometry
{
    // https://rosettacode.org/wiki/Determine_if_two_triangles_overlap#C.23
    public struct Triangle
    {
        public Vector2 P1 { get; set; }
        public Vector2 P2 { get; set; }
        public Vector2 P3 { get; set; }
        
        public float Det2d()
        {
            return P1.x * (P2.y - P3.y) +
                   P2.x * (P3.y - P1.y) +
                   P3.x * (P3.x - P2.y);
        }

        public void ReverseWinding()
        {
            var a = P3;
            P3 = P2;
            P2 = a;
        }
        
        public void CheckTriWinding(bool allowReversed) {
            var detTri = Det2d();
            if (!(detTri < 0.0)) return;
            if (allowReversed) {
                ReverseWinding();
            } else {
                throw new Exception("Triangle has wrong winding direction");
            }
        }

        public bool BoundaryCollideCheck(float eps)
        {
            return Det2d() < eps;
        } 
        
        public bool BoundaryDoesntCollideCheck(double eps) {
            return Det2d() <= eps;
        }

        public Triangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            this.P1 = p1;
            this.P2 = p2;
            this.P3 = p3;
        }

        public Triangle(Vector2[] pts)
        {
            this.P1 = pts[0];
            this.P2 = pts[1];
            this.P3 = pts[2];
        }
        
        public Vector2 GetPoint(int index)
        {
            switch (index)
            {
                case 0:
                    return P1;
                case 1:
                    return P2;
                case 2:
                    return P3;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}