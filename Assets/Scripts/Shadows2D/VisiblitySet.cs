using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Shadow2D
{
    /* TODO: There is an edge case where this code: 
     *   when a elongated box straddles one of the outline corners, 
     *   it will sometimes generate a light volume that extends outside of the outline.
     *   See "Documents\Shadow2d bug.png"
     */
    public class VisiblitySet
    {
        private bool _drawGizmos;
        private bool _clipping;
        private Mesh _mesh;

        public VisiblitySet()
        {
            Segments = new List<Segment>();
            EndPoints = new List<EndPoint>();
            Open = new List<Segment>();
            Output = new List<Vector2>();
        }

        public Color Color { get; set; }
        public List<Segment> Segments { get; set; }
        public List<EndPoint> EndPoints { get; set; }
        public List<Segment> Open { get; set; }
        public List<Vector2> Output { get; set; }
        public Vector2 Center { get; set; }
        public float Size { get; set; }
        public Vector2 TopLeft { get; set; }
        public Vector2 BottomRight { get; set; }
        public Vector2 BottomLeft { get; set; }
        public bool MergeCollinearSegments { get; set; }

        public void Clear(Shadow2DLight shadow2DLight)
        {
            Segments.Clear();
            EndPoints.Clear();
            Open.Clear();
            Output.Clear();

            Center = shadow2DLight.transform.position;
            Size = shadow2DLight.size;
            Color = shadow2DLight.color;

            MergeCollinearSegments = shadow2DLight.mergeCollinearShadowSegments;

            float top = Size / 2f;
            float btm = -Size / 2f;
            float left = -Size / 2f;
            float right = Size / 2f;

            TopLeft = shadow2DLight.transform.TransformPoint(new Vector3(left, top, 0f));
            BottomLeft = shadow2DLight.transform.TransformPoint(new Vector3(left, btm, 0f));
            BottomRight = shadow2DLight.transform.TransformPoint(new Vector3(right, btm, 0f));
            _drawGizmos = shadow2DLight._drawGizmos;
            _clipping = shadow2DLight.clipping;
        }

        public void UpdateAngles()
        {
            foreach (Segment segment in Segments)
            {
                float dx = 0.5f * (segment.Start.Point.x + segment.End.Point.x) - Center.x;
                float dy = 0.5f * (segment.Start.Point.y + segment.End.Point.y) - Center.y;
                // NOTE: we only use this for comparison so we can use
                // distance squared instead of distance. However in
                // practice the sqrt is plenty fast and this doesn't
                // really help in this situation.
                segment.D = dx * dx + dy * dy;

                // NOTE: future optimization: we could record the quadrant
                // and the y/x or x/y ratio, and sort by (quadrant,
                // ratio), instead of calling atan2. See
                // <https://github.com/mikolalysenko/compare-slope> for a
                // library that does this. Alternatively, calculate the
                // angles and use bucket sort to get an O(N) sort.
                segment.Start.Angle = Mathf.Atan2(segment.Start.Point.y - Center.y, segment.Start.Point.x - Center.x);
                segment.End.Angle = Mathf.Atan2(segment.End.Point.y - Center.y, segment.End.Point.x - Center.x);

                var dAngle = segment.End.Angle - segment.Start.Angle;
                if (dAngle <= -Math.PI)
                {
                    dAngle += 2 * Mathf.PI;
                }
                if (dAngle > Math.PI)
                {
                    dAngle -= 2 * Mathf.PI;
                }
                segment.Start.Begin = (dAngle > 0.0);
                segment.End.Begin = !segment.Start.Begin;
            }
        }

        public void Sweep()
        {
            
            EndPoints.Sort(EndPoint.Compare);
            float beginAngle = 0;

            for (int pass = 0; pass < 2; pass++)
            {
                Segment previous = null;
                foreach (EndPoint p in EndPoints)
                {
                    Segment currentOld = Open.FirstOrDefault();
                    InsertPoint(p);
                    Segment currentNew = Open.FirstOrDefault();
                    if (currentOld != currentNew)
                    {
                        if (pass == 1)
                        {
                            AddTriangle(beginAngle, p.Angle, currentOld, previous);
                            previous=currentOld;
                        }
                        beginAngle = p.Angle;
                    }
                }
            }
        }

        public void PrepareSegments()
        {
            Segments.ForEach(s => s.IsClipped = s.GetIsClipped(TopLeft, BottomRight) && !s.IsOutline);
            if (_clipping)
            {
                Segments.RemoveAll(s => s.IsClipped);
            }
            DoMergeCollinearSegments();
            SplitSegmentsThatOverlap();
            EndPoints.AddRange(Segments.Select(vs => vs.Start));
            EndPoints.AddRange(Segments.Select(vs => vs.End));
            DebugDrawSegments();
        }

        private void DoMergeCollinearSegments()
        {
            if (!MergeCollinearSegments)
            {
                return;
            }
            //int merges = 0;
            //int compares = 0;
            //int before = Segments.Count;

            //Stopwatch stopwatch = Stopwatch.StartNew();

            // Log of performance optimization of this very rouine.
            // Merged 287 to 149 segments (138 merges, 1710596 compares). 322 ms.
            // Merged 287 to 149 segments (138 merges, 105329 compares). 14 ms.
            // Merged 287 to 149 segments (138 merges, 84767 compares, 2 tries). 14 ms.
            // Merged 287 to 149 segments (138 merges, 84767 compares, 2 tries). 13 ms.
            // Merged 287 to 149 segments (138 merges, 1968 compares, 6 tries). 5 ms.
            // Merged 287 to 149 segments (138 merges, 1968 compares, 6 tries). 4 ms.
           
            int tries = 0;
            ILookup<Vector2, Segment> startpointLookup =
                Segments
                    .ToLookup(s => s.Start.Point, s => s);

            while (tries++ < 4)
            {
                bool merged = false;
                for (int index = 0; index < Segments.Count; index++)
                {
                    Segment segment = Segments[index];
                    if (segment.Merged)
                    {
                        continue;
                    }

                    IEnumerable<Segment> startpoints = startpointLookup[segment.End.Point];

                    foreach (Segment other in startpoints)
                    {
                        //compares++;
                        if (other.Merged)
                        {
                            continue;
                        }

                        if (ShadowMathUtils.Approximately(segment.Slope, other.Slope))
                        {
                            other.Merged = true;
                            segment.End.Point = other.End.Point;
                            //merges++;
                            merged = true;
                        }
                    }                   
                }

                Segments.RemoveAll(s => s.Merged);
                if (!merged)
                {
                    break;
                }
            }

            //Debug.LogFormat("Merged {0} to {1} segments ({2} merges, {3} compares, {5} tries). {4} ms.", before, Segments.Count, merges, compares, stopwatch.ElapsedMilliseconds, tries);
        }

        public Mesh CreateOrUpdateMesh()
        {
            int triangleCount = Output.Count / 2;
            int centerVertexId = Output.Count;
            Output.Add(Center);
            List<int> triangles = new List<int>(triangleCount);
            Gizmos.color = Color.yellow;
            for (int i = 0; i < triangleCount; i++)
            {
                triangles.Add(centerVertexId);
                triangles.Add(i * 2 + 1);
                triangles.Add(i * 2 + 0);
            }

            List<Vector2> lightUvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            float invSize = 1 / Size;
            Vector2 half = Vector2.one * 0.5f;

            foreach (Vector2 vector2 in Output)
            {
                lightUvs.Add((-vector2 + Center) * invSize + half);
                normals.Add(Vector3.up);
                colors.Add(Color);
            }

            Mesh mesh = _mesh;

            if (mesh != null)
            {
                //if (mesh.triangles.Length != triangles.Count)
                //{
                //    UnityEngine.Object.Destroy(mesh);
                //    mesh = new Mesh();
                //}
                mesh.triangles = null;
                mesh.vertices = null;
                mesh.colors = null;
                mesh.uv = null;
                mesh.uv2 = null;
            }
            else
            {
                mesh = new Mesh();
            }
            _mesh = mesh;

            //mesh.name = "My shadow mesh";
            mesh.vertices = Output.Select(o => (Vector3)(o - Center)).ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv2 = lightUvs.ToArray();
            mesh.colors = colors.ToArray();
            mesh.normals = normals.ToArray();
            mesh.RecalculateBounds();
            UpdateUvs();
            return mesh;
        }

        public void UpdateUvs()
        {
            List<Vector2> textureUvs = new List<Vector2>(Output.Count);

            foreach (Vector2 vector2 in Output)
            {
                textureUvs.Add(Camera.main.WorldToViewportPoint(vector2));
            }
            _mesh.uv = textureUvs.ToArray();
        }

        private void SplitSegmentsThatOverlap()
        {
            // O(N^2)
            Queue<Segment> open = new Queue<Segment>(Segments);
            Segments.Clear();
            while (open.Any())
            {
                if (Segments.Count > 300)
                {
                    Debug.Log("Too many segment splits!");
                    return;
                }
                Segment segment = open.Dequeue();
                bool splitFree = true;
                foreach (Segment testing in Segments)
                {
                    Segment.IntersectionResult intersection = Segment.IsInFrontOf(segment, testing, Center);

                    if (intersection == Segment.IntersectionResult.Intersects)
                    {
                        // Do they *really* overlap, there seems to be some issues with the IsInFrontOf method.
                        if (ShadowMathUtils.Approximately(testing.Start.Point, segment.Start.Point)
                            || ShadowMathUtils.Approximately(testing.End.Point, segment.End.Point)
                            || ShadowMathUtils.Approximately(testing.End.Point, segment.Start.Point)
                            || ShadowMathUtils.Approximately(testing.Start.Point, segment.End.Point))
                        {
                            //intersection = Segment.IsInFrontOf(segment, testing, Center);
                            //Debug.Log("They're the same point!"+intersection);
                            continue;
                        }

                        // segment && testing overlap, we must split one of them but both must go back on the queue
                        Segments.Remove(testing);
                        open.Enqueue(testing);

                        Vector2 position =
                            ShadowMathUtils.LineIntersection(
                                segment.Start.Point,
                                segment.End.Point,
                                testing.Start.Point,
                                testing.End.Point);

                        if (_drawGizmos)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(position, 0.2f);
                        }

                        // Split segments and add both parts back to the queue
                        Split(segment, position, open);
                        splitFree = false;
                        break;
                    }
                }
                if (splitFree)
                {
                    Segments.Add(segment);
                }
            }
        }

        private void Split(Segment segment, Vector2 middle, Queue<Segment> open)
        {
            Vector2 start = segment.Start.Point;
            Vector2 end = segment.End.Point;
            //Debug.LogFormat("Splitting {0}-{1} at {2}", start, end, middle);

            open.Enqueue(new Segment(start, middle));
            open.Enqueue(new Segment(middle, end));

            //Segment endSegment = new Segment(position, segment.End.Point);
            //segment.End.Point = position;
            //open.Enqueue(segment);
            //open.Enqueue(endSegment);
        }

        private void DebugDrawSegments()
        {
            if (_drawGizmos)
            {
                foreach (Segment segment in Segments)
                {
                    //Gizmos.color = segment.IsBackfacing ? Color.black : Color.white;
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(segment.Start.Point, segment.End.Point);
                }
            }
        }

        private void AddTriangle(float angle1, float angle2, Segment segment, Segment previous)
        {
            Vector2 delta1 = new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1));
            Vector2 delta2 = new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2));

            Vector2 p1 = Center;
            Vector2 p2 = p1 + delta1;
            Vector2 p3 = new Vector2(0.0f, 0.0f);
            Vector2 p4 = new Vector2(0.0f, 0.0f);

            Gizmos.color = Color.blue;
            if (segment != null)
            {
                // Stop the triangle at the intersecting segment
                p3 = segment.Start.Point;
                p4 = segment.End.Point;
            }
            else
            {
                // Stop the triangle at a fixed distance; this probably is
                // not what we want, but it never gets used in the demo
                p3 = p1 + delta1 * 500;
                p4 = p1 + delta2 * 500;
            }

            Vector2 pBegin = ShadowMathUtils.LineIntersection(p3, p4, p1, p2);
            p2 = p1 + delta2;
            Vector2 pEnd = ShadowMathUtils.LineIntersection(p3, p4, p1, p2);

            if (_drawGizmos)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(p1, pBegin);
                Gizmos.DrawLine(p2, pEnd);

                //Gizmos.DrawLine(pBegin, pEnd);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(pBegin, 0.1f);
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(pEnd, 0.1f);
            }

            if (previous != null 
                && ShadowMathUtils.Approximately(segment.Slope, previous.Slope)
                && ShadowMathUtils.Approximately(Output.Last(),pBegin))
            {
                // It's a continuation of the previous segment!
                Output.RemoveAt(Output.Count-1);
                Output.Add(pEnd);
            }
            else
            {
                Output.Add(pBegin);
                Output.Add(pEnd);                
            }
        }

        private void InsertPoint(EndPoint p)
        {
            if (p.Begin)
            {
                bool inserted = false;
                for (int index = 0; index < Open.Count; index++)
                {
                    Segment test = Open[index];
                    if (Segment.IsInFrontOf(p.Segment, test, Center) != Segment.IntersectionResult.InFront)
                    {
                        Open.Insert(index, p.Segment);
                        inserted = true;
                        break;
                    }
                }

                if (!inserted)
                {
                    Open.Add(p.Segment);
                }
            }
            else
            {
                Open.Remove(p.Segment);
            }
        }
    }
}