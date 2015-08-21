using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shadow2D
{
    public static class ShadowMathUtils
    {
        // See http://geomalgorithms.com/a03-_inclusion.html
        // wn_PnPoly(): winding number test for a point in a polygon
        //      Input:   P = a point,
        //               V[] = vertex points of a polygon V[n+1] with V[n]=V[0]
        //      Return:  wn = the winding number (=0 only when P is outside)
        public static bool GetIsPointInPoly(Vector2 p, List<Vector2> v)
        {
            int wn = 0;    // the  winding number counter

            // loop through all edges of the polygon
            for (int i = 0; i < v.Count - 1; i++)
            {   // edge from V[i] to  V[i+1]
                if (v[i].y <= p.y)
                {          // start y <= P.y
                    if (v[i + 1].y > p.y)      // an upward crossing
                        if (GetIsLeft(v[i], v[i + 1], p))  // P left of  edge
                            ++wn;            // have  a valid up intersect
                }
                else
                {                        // start y > P.y (no test needed)
                    if (v[i + 1].y <= p.y)     // a downward crossing
                        if (!GetIsLeft(v[i], v[i + 1], p)) // P right of  edge
                        {
                            --wn; // have  a valid down intersect
                        }
                }
            }

            return wn != 0;
        }

        public static float PseudoAngle(Vector2 delta)
        {
            float divisor = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);

            if (Math.Abs(divisor) < 0.0001f)
            {
                return -1;
            }
            var result = delta.y / divisor;
            return delta.x < 0.0 ? 2.0f - result : 4.0f + result;
        }

        public static bool Approximately(Vector2 a, Vector2 b)
        {
            return
                Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y);
        }

        public static bool Approximately(Quaternion a, Quaternion b)
        {
            return
                Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.z, b.z)
                && Mathf.Approximately(a.w, b.w);
        }

        public static Vector2 LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            // From http://paulbourke.net/geometry/lineline2d/
            var s =
                ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x))
                / ((p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y));
            return new Vector2(p1.x + s * (p2.x - p1.x), p1.y + s * (p2.y - p1.y));
        }

        // Helper: leftOf(segment, point) returns true if point is "left"
        // of segment treated as a vector. Note that this assumes a 2D
        // coordinate system in which the Y axis grows downwards, which
        // matches common 2D graphics libraries, but is the opposite of
        // the usual convention from mathematics and in 3D graphics
        // libraries.
        public static bool GetIsLeft(Segment s, Vector2 p)
        {
            // This is based on a 3d cross product, but we don't need to
            // use z coordinate inputs (they're 0), and we only need the
            // sign. If you're annoyed that cross product is only defined
            // in 3d, see "outer product" in Geometric Algebra.
            // <http://en.wikipedia.org/wiki/Geometric_algebra>
            var cross =
                (s.End.Point.x - s.Start.Point.x) * (p.y - s.Start.Point.y)
                - (s.End.Point.y - s.Start.Point.y) * (p.x - s.Start.Point.x);
            return cross < 0;
            // Also note that this is the naive version of the test and
            // isn't numerically robust. See
            // <https://github.com/mikolalysenko/robust-arithmetic> for a
            // demo of how this fails when a point is very close to the
            // line.
        }


        public static bool GetIsLeft(Vector2 end, Vector2 start, Vector2 p)
        {
            var cross =
                (end.x - start.x) * (p.y - start.y)
                - (end.y - start.y) * (p.x - start.x);
            return cross < 0;
        }

        public enum SideTestResult
        {
            Left,
            Right,
            On
        }

        public static SideTestResult SideTest(Segment s, Vector2 p)
        {
            float cross =
                (s.End.Point.x - s.Start.Point.x) * (p.y - s.Start.Point.y)
                - (s.End.Point.y - s.Start.Point.y) * (p.x - s.Start.Point.x);

            if (Math.Abs(cross) < 0.00001f)
            {
                return SideTestResult.On;
            }
            if (cross < 0)
            {
                return SideTestResult.Left;
            }
            return SideTestResult.Right;
        }

        public static Vector2 Interpolate(Vector2 p, Vector2 q, float f)
        {
            return
                new Vector2(
                        p.x * (1 - f) + q.x * f,
                        p.y * (1 - f) + q.y * f);
        }
    }
}