using UnityEngine;

namespace Shadow2D
{
    public class EndPoint
    {
        public EndPoint(Vector2 point, Segment segment)
        {
            Point = point;
            Segment = segment;
        }

        public Vector2 Point { get; set; }
        public bool Begin { get; set; }
        public Segment Segment { get; set; }
        public float Angle { get; set; }
        public bool Visualise { get; set; }

        public override string ToString()
        {
            return Point.ToString();
        }

        public static int Compare(EndPoint a, EndPoint b)
        {
            //return ShadowMathUtils.Approximately(a, b);
            // Traverse in angle order
            if (a.Angle > b.Angle) return 1;
            if (a.Angle < b.Angle) return -1;
            // But for ties (common), we want Begin nodes before End nodes
            if (!a.Begin && b.Begin) return 1;
            if (a.Begin && !b.Begin) return -1;
            return 0;
        }
    }
}