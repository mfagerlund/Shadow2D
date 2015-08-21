using UnityEngine;

namespace Shadow2D
{
    public class Segment
    {
        public Segment(Vector2 start, Vector2 end)
        {
            Start = new EndPoint(start, this);
            End = new EndPoint(end, this);
            Slope = (end - start).normalized;
        }

        public EndPoint Start { get; set; }
        public EndPoint End { get; set; }
        public bool IsOutline { get; set; }
        public Vector2 Slope { get; set; }
        public float D { get; set; }
        public bool IsClipped { get; set; }
        public bool Merged { get; set; }
        public override string ToString()
        {
            return string.Format("{0}-{1}", Start, End);
        }

        public enum IntersectionResult
        {
            InFront,
            Behind,
            Intersects
        }

        // Helper: do we know that segment a is in front of b?
        // Implementation not anti-symmetric (that is to say,
        // _segment_in_front_of(a, b) != (!_segment_in_front_of(b, a)).
        // Also note that it only has to work in a restricted set of cases
        // in the visibility algorithm; I don't think it handles all
        // cases. See http://www.redblobgames.com/articles/visibility/segment-sorting.html
        public static IntersectionResult IsInFrontOf(Segment a, Segment b, Vector2 relativeTo)
        {
            // NOTE: we slightly shorten the segments so that
            // intersections of the endpoints (common) don't count as
            // intersections in this algorithm
            const float epsilon = 0.01f;
            bool A1 = ShadowMathUtils.GetIsLeft(a, ShadowMathUtils.Interpolate(b.Start.Point, b.End.Point, epsilon));
            bool A2 = ShadowMathUtils.GetIsLeft(a, ShadowMathUtils.Interpolate(b.End.Point, b.Start.Point, epsilon));
            bool A3 = ShadowMathUtils.GetIsLeft(a, relativeTo);
            bool B1 = ShadowMathUtils.GetIsLeft(b, ShadowMathUtils.Interpolate(a.Start.Point, a.End.Point, epsilon));
            bool B2 = ShadowMathUtils.GetIsLeft(b, ShadowMathUtils.Interpolate(a.End.Point, a.Start.Point, epsilon));
            bool B3 = ShadowMathUtils.GetIsLeft(b, relativeTo);

            // NOTE: this algorithm is probably worthy of a short article
            // but for now, draw it on paper to see how it works. Consider
            // the line A1-A2. If both B1 and B2 are on one side and
            // relativeTo is on the other side, then A is in between the
            // viewer and B. We can do the same with B1-B2: if A1 and A2
            // are on one side, and relativeTo is on the other side, then
            // B is in between the viewer and A.
            if (B1 == B2 && B2 != B3) return IntersectionResult.InFront;
            if (A1 == A2 && A2 == A3) return IntersectionResult.InFront;
            if (A1 == A2 && A2 != A3) return IntersectionResult.Behind;
            if (B1 == B2 && B2 == B3) return IntersectionResult.Behind;

            // If A1 != A2 and B1 != B2 then we have an intersection.
            // Expose it for the GUI to show a message. A more robust
            // implementation would split segments at intersections so
            // that part of the segment is in front and part is behind.
            //demo_intersectionsDetected.push([a.p1, a.p2, b.p1, b.p2]);
            return IntersectionResult.Intersects;

            // NOTE: previous implementation was a.d < b.d. That's simpler
            // but trouble when the segments are of dissimilar sizes. If
            // you're on a grid and the segments are similarly sized, then
            // using distance will be a simpler and faster implementation.
        }      

        public bool GetIsClipped(Vector2 topLeft, Vector2 btmRight)
        {
            if (Start.Point.x < topLeft.x && End.Point.x < topLeft.x
                || Start.Point.x > btmRight.x && End.Point.x > btmRight.x
                || Start.Point.y < btmRight.y && End.Point.y < btmRight.y
                || Start.Point.y > topLeft.y && End.Point.y > topLeft.y)
            {
                return true;
            }

            return false;
        }
    }
}