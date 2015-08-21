using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shadow2D
{
    public class Shadow2DSegmentGenerator : MonoBehaviour
    {
        private List<Vector2> _unitCircle;
        private Shadow2DLight _shadow2DLight;

        public virtual void AddColliderSegments(Shadow2DLight shadow2DLight, List<Segment> segments, Collider2D collider)
        {
            _shadow2DLight = shadow2DLight;
            BoxCollider2D boxCollider2D = collider as BoxCollider2D;
            if (boxCollider2D != null)
            {
                AddBoxCollider2DSegments(segments, boxCollider2D);
                return;
            }

            CircleCollider2D circleCollider2D = collider as CircleCollider2D;
            if (circleCollider2D != null)
            {
                AddCircleCollider2DSegments(segments, circleCollider2D);
                return;
            }

            PolygonCollider2D polygonCollider2D = collider as PolygonCollider2D;
            if (polygonCollider2D != null)
            {
                AddPolygonCollider2DSegments(segments, polygonCollider2D);
                return;
            }

            EdgeCollider2D edgeCollider2D = collider as EdgeCollider2D;
            if (edgeCollider2D != null)
            {
                AddEdgeCollider2DSegments(segments, edgeCollider2D);
                return;
            }

            //Debug.LogFormat("Unhandled collider: {0}", collider);
        }

        private static void AddBoxCollider2DSegments(List<Segment> segments, BoxCollider2D collider)
        {
            float top = collider.offset.y + (collider.size.y / 2f);
            float btm = collider.offset.y - (collider.size.y / 2f);
            float left = collider.offset.x - (collider.size.x / 2f);
            float right = collider.offset.x + (collider.size.x / 2f);

            Vector3 topLeft = collider.transform.TransformPoint(new Vector3(left, top, 0f));
            Vector3 topRight = collider.transform.TransformPoint(new Vector3(right, top, 0f));
            Vector3 btmLeft = collider.transform.TransformPoint(new Vector3(left, btm, 0f));
            Vector3 btmRight = collider.transform.TransformPoint(new Vector3(right, btm, 0f));

            segments.Add(new Segment(topLeft, topRight));
            segments.Add(new Segment(topRight, btmRight));
            segments.Add(new Segment(btmRight, btmLeft));
            segments.Add(new Segment(btmLeft, topLeft));
        }

        private void AddCircleCollider2DSegments(List<Segment> segments, CircleCollider2D collider)
        {
            if (_unitCircle == null || _unitCircle.Count != _shadow2DLight.circleColliderSteps)
            {
                // Why use a superfast circle method when we can cache the vertices?
                float ang = 0;
                float angd = Mathf.PI * 2 / _shadow2DLight.circleColliderSteps;
                _unitCircle = new List<Vector2>();
                for (int i = 0; i < _shadow2DLight.circleColliderSteps; i++)
                {
                    _unitCircle.Add(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)));
                    ang += angd;
                }
            }

            Vector2 circleCenter = collider.transform.TransformPoint(collider.offset);
            float radius = collider.transform.lossyScale.y * collider.radius;

            for (int i = 0; i < _shadow2DLight.circleColliderSteps; i++)
            {
                Vector2 start = circleCenter + _unitCircle[i] * radius;
                Vector2 end = circleCenter + _unitCircle[(i + 1) % _shadow2DLight.circleColliderSteps] * radius;
                segments.Add(new Segment(start, end));
            }
        }

        //private void AddCircleCollider2DAsSingleSegment(VisiblitySet visiblitySet, CircleCollider2D collider)
        //{
        //    Vector2 circleCenter = collider.transform.TransformPoint(collider.offset);
        //    float radius = collider.transform.lossyScale.y * collider.radius;
        //    Vector2 delta = visiblitySet.Center - circleCenter;
        //    Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
        //    Vector2 offset = normal * radius;
        //    Vector2 start = circleCenter + offset;
        //    Vector2 end = circleCenter - offset;
        //    visiblitySet.Segments.Add(new Segment(start, end));
        //}

        private static void AddPolygonCollider2DSegments(List<Segment> segments, PolygonCollider2D collider)
        {
            if (collider.points.Length < 2)
            {
                return;
            }

            List<Vector2> transformed =
                collider
                    .points
                    .Select(p => (Vector2)collider.transform.TransformPoint(p + collider.offset))
                    .ToList();

            for (int index = 0; index < transformed.Count - 1; index++)
            {
                segments.Add(
                    new Segment(
                        transformed[index],
                        transformed[index + 1]));
            }

            if (transformed.Count > 2)
            {
                segments.Add(
                    new Segment(
                        transformed[0],
                        transformed[transformed.Count - 1]));
            }
        }

        private static void AddEdgeCollider2DSegments(List<Segment> segments, EdgeCollider2D collider)
        {
            if (collider.points.Length < 2)
            {
                return;
            }

            List<Vector2> transformed =
                collider
                    .points
                    .Select(p => (Vector2)collider.transform.TransformPoint(p + collider.offset))
                    .ToList();

            for (int index = 0; index < transformed.Count - 1; index++)
            {
                segments.Add(
                    new Segment(
                        transformed[index],
                        transformed[index + 1]));
            }
        }
    }
}