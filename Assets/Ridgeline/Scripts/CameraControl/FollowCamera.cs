using UnityEngine;

namespace Ridgeline.CameraControl
{
    /// <summary>
    /// Orthographic follow camera that works at every aspect ratio from 9:32 to 32:9.
    /// It aims to show a fixed width of track; in portrait it zooms out (capped) to
    /// keep enough road ahead visible, in landscape it keeps a minimum height.
    /// Recomputed every frame, so window resizes are handled without a restart.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FollowCamera : MonoBehaviour
    {
        public Rigidbody2D target;

        [Header("Framing")]
        [Tooltip("Half of the world width (m) we try to show.")]
        public float desiredHalfWidth = 11f;
        [Tooltip("Never show less than this half-height (m).")]
        public float minHalfHeight = 6.5f;
        [Tooltip("Never zoom out further than this half-height (m).")]
        public float maxHalfHeight = 20f;
        [Tooltip("Extra zoom-out at high speed (m of half-height at 20 m/s).")]
        public float speedZoom = 1.5f;

        [Tooltip("Where the vehicle sits horizontally: 0 = left edge, 0.5 = centre.")]
        [Range(0.1f, 0.5f)] public float horizontalAnchor = 0.35f;
        [Tooltip("Where the vehicle sits vertically: 0 = bottom, 0.5 = centre.")]
        [Range(0.2f, 0.9f)] public float verticalAnchor = 0.42f;
        [Tooltip("Extra look-ahead in seconds of velocity.")]
        public float lookAheadTime = 0.35f;
        public float smoothTime = 0.15f;

        Camera _cam;
        Vector3 _vel;
        float _sizeVel;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            _cam.orthographicSize = ComputeSize();
            transform.position = ComputeGoal(_cam.orthographicSize);
            _vel = Vector3.zero;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float size = Mathf.SmoothDamp(_cam.orthographicSize, ComputeSize(), ref _sizeVel, 0.4f);
            _cam.orthographicSize = size;
            transform.position = Vector3.SmoothDamp(transform.position, ComputeGoal(size), ref _vel, smoothTime);
        }

        float ComputeSize()
        {
            float aspect = Mathf.Max(0.01f, _cam.aspect);
            float speed = target != null ? target.linearVelocity.magnitude : 0f;
            float size = Mathf.Max(minHalfHeight, desiredHalfWidth / aspect);
            size += speedZoom * Mathf.Clamp01(speed / 20f);
            return Mathf.Min(size, maxHalfHeight);
        }

        Vector3 ComputeGoal(float size)
        {
            float halfW = size * Mathf.Max(0.01f, _cam.aspect);
            Vector2 p = target.position;
            Vector2 v = target.linearVelocity;
            float lookX = Mathf.Clamp(v.x * lookAheadTime, -halfW * 0.3f, halfW * 0.3f);
            float lookY = Mathf.Clamp(v.y * lookAheadTime * 0.5f, -size * 0.2f, size * 0.2f);
            // Offset so the vehicle sits at (horizontalAnchor, verticalAnchor) of the screen.
            float x = p.x + halfW * (1f - 2f * horizontalAnchor) + lookX;
            float y = p.y + size * (1f - 2f * verticalAnchor) + lookY;
            return new Vector3(x, y, transform.position.z);
        }
    }
}
