using UnityEngine;

namespace Ridgeline.CameraControl
{
    /// <summary>
    /// Far scenery that fills the camera view and scrolls slower than the world.
    /// A single quad parented to nothing, resized to the view every frame; the texture repeats horizontally
    /// and the UV offset gives the parallax. Runs after FollowCamera.
    /// </summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        public Camera cam;
        [Tooltip("0 = fixed to the screen, 1 = moves with the world.")]
        [Range(0f, 1f)] public float horizontalFactor = 0.12f;
        [Tooltip("How much the layer drifts down as the camera climbs (world units per world unit).")]
        [Range(0f, 1f)] public float verticalFactor = 0.03f;
        [Tooltip("Fraction of the view height the layer covers, anchored to the bottom.")]
        public float heightFraction = 1.0f;

        Mesh _mesh;
        readonly Vector3[] _v = new Vector3[4];
        readonly Vector2[] _uv = new Vector2[4];
        float _originY = float.NaN;

        void Awake()
        {
            _mesh = new Mesh { name = "Parallax" };
            _mesh.MarkDynamic();
            _mesh.vertices = _v;
            _mesh.uv = _uv;
            _mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            var mr = GetComponent<MeshRenderer>();
            mr.sortingOrder = -100;
        }

        /// <summary>Call after a run reset so the vertical drift starts from zero again.</summary>
        public void ResetOrigin() => _originY = float.NaN;

        void LateUpdate()
        {
            if (cam == null) return;
            var p = cam.transform.position;
            if (float.IsNaN(_originY)) _originY = p.y;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            // Slightly taller than the view so the vertical drift never exposes an edge.
            float h = halfH * 2f * heightFraction + halfH * 0.3f;
            // Keep the image square-tiled: one tile is as wide as it is tall.
            float tileW = h;

            // Vertical: anchored a bit below the view bottom, drifting slightly with altitude, clamped to stay on screen.
            float drift = Mathf.Clamp((p.y - _originY) * verticalFactor, -halfH * 0.15f, halfH * 0.15f);
            float bottom = -halfH - drift - halfH * 0.15f;
            float top = bottom + h;

            transform.position = new Vector3(p.x, p.y, 5f);
            transform.rotation = Quaternion.identity;
            _v[0] = new Vector3(-halfW, bottom, 0f);
            _v[1] = new Vector3(-halfW, top, 0f);
            _v[2] = new Vector3(halfW, bottom, 0f);
            _v[3] = new Vector3(halfW, top, 0f);

            float u0 = p.x * horizontalFactor / tileW - halfW / tileW;
            float u1 = u0 + 2f * halfW / tileW;
            _uv[0] = new Vector2(u0, 0f);
            _uv[1] = new Vector2(u0, 1f);
            _uv[2] = new Vector2(u1, 0f);
            _uv[3] = new Vector2(u1, 1f);

            _mesh.vertices = _v;
            _mesh.uv = _uv;
            _mesh.RecalculateBounds();
        }
    }
}
