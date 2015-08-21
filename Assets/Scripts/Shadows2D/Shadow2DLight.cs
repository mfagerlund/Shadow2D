using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Shadow2D
{
    // Issues:
    // * When we save the scene or the project, the render texture is lost on the materials. This is ok because it's reset in
    //   update later on, but we get no events until the developer does something in the editor, so the backbground gets lost for a bit
    // * When we change a script and go back to the editor, all the light sources become un-occluded because when they first update, they find
    //   no colliders. No more events are fired until the developer changes something in the editor, so the lights start working at that point.
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Shadow2DLight : MonoBehaviour
    {
        private Collider2DTracker _collider2DTracker = new Collider2DTracker();
        private VisiblitySet _visiblitySet;
        private MeshFilter _meshFilter;
        internal bool _drawGizmos;
        private Vector2 _oldPosition = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        private Vector2 _oldCameraPosition = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        private Vector2 _screenSize = Vector2.zero;

        [Header("Shadow Settings")]
        public LayerMask layerMask = -1;
        public float size = 10;
        public int circleColliderSteps = 20;
        public bool clipping = true;
        public bool usePhysicsForShadowCasters = true;
        public Color color = Color.white;
        public bool drawGizmos = true;
        public bool mergeCollinearShadowSegments = true;

        [Header("Update Settings")]
        // Updates no matter what
        public bool updateOnEachUpdate = false;

        // Updates when the list of colliders has changed - or 
        // if their sizes or rotations have changed.
        public bool updateWhenCollidersChange = true;

        // Updates when light has moved
        public bool updateOnChangedLocation = true;

        public List<Vector2> ShadowPolygonPoints { get; private set; }

        // Compensates for camera movements - doesn't rebuild the mesh, just the uvs, because
        // the uvs determine what part of the RenderTexture to draw.
        public bool updateUvs = true;
        private float _oldSize;
        private float _oldOrthographicSize;
        public Shadow2DSegmentGenerator segmentGenerator;
        private Bounds _bounds;

        public void Start()
        {
            ShadowPolygonPoints = new List<Vector2>();
            if (segmentGenerator == null)
            {
                segmentGenerator = GetComponent<Shadow2DSegmentGenerator>();
            }

            if (segmentGenerator == null)
            {
                segmentGenerator = gameObject.AddComponent<Shadow2DSegmentGenerator>();
            }

            //if (segmentGenerator == null)
            //{
            //    Debug.Log("No segment generator found!");
            //}        
        }

        public void LateUpdate()
        {
            _visiblitySet = _visiblitySet ?? new VisiblitySet();

            // Can't rotate the lights at the moment
            transform.rotation = Quaternion.identity;

            Vector2 newPosition = transform.position;
            Vector2 oldScreenSize = _screenSize;
            _screenSize = new Vector2(Screen.width, Screen.height);

            bool rebuild = updateOnEachUpdate;
            rebuild = rebuild || _screenSize != oldScreenSize || !Mathf.Approximately(size, _oldSize);
            rebuild = rebuild || !Mathf.Approximately(Camera.main.orthographicSize, _oldOrthographicSize);
            rebuild = rebuild || updateOnChangedLocation && !ShadowMathUtils.Approximately(_oldPosition, newPosition);
            rebuild = rebuild || updateWhenCollidersChange && GetHaveCollidersChanged();

            _oldSize = size;

            if (rebuild)
            {
                RebuildShadow();
                _oldOrthographicSize = Camera.main.orthographicSize;
            }
            else
            {
                if (updateUvs
                    && !ShadowMathUtils.Approximately(Camera.main.transform.position, _oldCameraPosition))
                {
                    _visiblitySet.UpdateUvs();
                    _oldCameraPosition = Camera.main.transform.position;
                }
            }

            _oldPosition = newPosition;
        }

        public void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }
            _drawGizmos = true;
            try
            {
                RebuildShadow();
            }
            finally
            {
                _drawGizmos = false;
            }
        }

        public void RebuildShadow()
        {
            _visiblitySet = _visiblitySet ?? new VisiblitySet();
            _visiblitySet.Clear(this);
            _meshFilter = _meshFilter ?? GetComponent<MeshFilter>();

            AddSegments(_visiblitySet);
            _visiblitySet.PrepareSegments();
            _visiblitySet.UpdateAngles();
            _visiblitySet.Sweep();
            _bounds = new Bounds(transform.position, new Vector3(size, size, size));

            CreateShadowPolygonPoints();

            Mesh mesh = _visiblitySet.CreateOrUpdateMesh();
            _meshFilter.sharedMesh = mesh;

            if (_drawGizmos)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireMesh(mesh, transform.position, transform.rotation);
            }
        }

        public bool GetHaveCollidersChanged()
        {
            return
                _collider2DTracker
                    .UpdateColliders(_visiblitySet.TopLeft, _visiblitySet.BottomRight, layerMask);
        }

        private void CreateShadowPolygonPoints()
        {
            ShadowPolygonPoints = ShadowPolygonPoints ?? new List<Vector2>();
            ShadowPolygonPoints.Clear();
            if (_visiblitySet.Output.Any())
            {
                Vector2 previous = Vector2.one * 1000000;
                for (int index = 0; index < _visiblitySet.Output.Count; index += 1)
                {
                    Vector2 point = _visiblitySet.Output[index];
                    if (!ShadowMathUtils.Approximately(point, previous))
                    {
                        ShadowPolygonPoints.Add(point);
                    }
                    previous = point;
                }
                ShadowPolygonPoints.Add(_visiblitySet.Output.First());
            }

            if (_drawGizmos)
            {
                Gizmos.color = Color.yellow;
                for (int index = 0; index < ShadowPolygonPoints.Count - 1; index++)
                {
                    Vector2 va = ShadowPolygonPoints[index];
                    Vector2 vb = ShadowPolygonPoints[index + 1];
                    Gizmos.DrawLine(va * 1.05f, vb * 1.05f);
                }
                Gizmos.DrawLine(ShadowPolygonPoints.Last() * 1.05f, ShadowPolygonPoints.First() * 1.05f);
            }
        }

        private void AddSegments(VisiblitySet visiblitySet)
        {
            if (usePhysicsForShadowCasters)
            {
                _collider2DTracker.UpdateColliders(_visiblitySet.TopLeft, _visiblitySet.BottomRight, layerMask);
            }

            if (segmentGenerator == null)
            {
                Debug.LogError("No segment generator found!");
                return;
            }
            foreach (Collider2D collider in _collider2DTracker.colliders)
            {
                segmentGenerator.AddColliderSegments(this, visiblitySet.Segments, collider);
            }

            AddOutline(visiblitySet);
        }

        private void AddOutline(VisiblitySet visiblitySet)
        {
            float top = size / 2f;
            float btm = -size / 2f;
            float left = -size / 2f;
            float right = size / 2f;

            Vector3 topLeft = transform.TransformPoint(new Vector3(left, top, 0f));
            Vector3 topRight = transform.TransformPoint(new Vector3(right, top, 0f));
            Vector3 btmLeft = transform.TransformPoint(new Vector3(left, btm, 0f));
            Vector3 btmRight = transform.TransformPoint(new Vector3(right, btm, 0f));

            visiblitySet.Segments.Add(new Segment(topRight, topLeft) { IsOutline = true });
            visiblitySet.Segments.Add(new Segment(btmRight, topRight) { IsOutline = true });
            visiblitySet.Segments.Add(new Segment(btmLeft, btmRight) { IsOutline = true });
            visiblitySet.Segments.Add(new Segment(topLeft, btmLeft) { IsOutline = true });
        }

        public bool GetIsPointInLight(Vector2 position)
        {
            if (!_bounds.Contains(position))
            {
                return false;
            }

            if (!ShadowPolygonPoints.Any())
            {
                return false;
            }

            return ShadowMathUtils.GetIsPointInPoly(position, ShadowPolygonPoints);
        }
    }
}

