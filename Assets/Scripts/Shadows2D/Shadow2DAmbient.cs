using UnityEngine;

namespace Shadow2D
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class Shadow2DAmbient : MonoBehaviour
    {
        public Color color = Color.gray;

        private MeshFilter _meshFilter;
        private Mesh _mesh;

        public void Update()
        {
            // Todo: what about resizing?
            _meshFilter = GetComponent<MeshFilter>();

            if (_meshFilter == null)
            {
                Debug.Log("No mesh filter!");
                return;
            }

            _mesh = _mesh ?? _meshFilter.sharedMesh;
            if (_mesh == null)
            {
                _mesh = new Mesh();
            }
            _mesh.vertices =
                new[]
                {
                    Adjust(Camera.main.ViewportToWorldPoint(new Vector2(0, 0))),
                    Adjust(Camera.main.ViewportToWorldPoint(new Vector2(0, 1))),
                    Adjust(Camera.main.ViewportToWorldPoint(new Vector2(1, 1))),
                    Adjust(Camera.main.ViewportToWorldPoint(new Vector2(1, 0)))
                };

            _mesh.triangles = new[] { 0, 1, 2, 3, 0, 2 };

            _mesh.uv = new[]
                {
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0)
                };

            _mesh.colors = new[]
            {
                color,
                color,
                color,
                color
            };

            _meshFilter.mesh = _mesh;
        }

        private Vector3 Adjust(Vector3 v)
        {
            // Make sure that the ambient mesh is centered on the screen no matter where the actual game object is placed.
            return ZeroZ(v - transform.TransformPoint(Vector3.zero));
        }

        private Vector3 ZeroZ(Vector3 v)
        {
            v.z = 0;
            return v;
        }
    }
}