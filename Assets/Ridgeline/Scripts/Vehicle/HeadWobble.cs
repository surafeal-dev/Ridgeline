using UnityEngine;

namespace Ridgeline.Vehicle
{
    /// <summary>
    /// Bobblehead: rotates and bobs the driver's head (this transform = the neck pivot) on a damped spring,
    /// driven by the chassis' acceleration and spin. Visual only; the crash sensor stays fixed on the chassis.
    /// </summary>
    public class HeadWobble : MonoBehaviour
    {
        public Rigidbody2D body;

        [Header("Tilt spring (degrees)")]
        public float stiffness = 140f;
        public float damping = 5f;
        [Tooltip("Degrees of lean per m/s^2 of forward acceleration, at rest (divided by stiffness).")]
        public float accelGain = 260f;
        [Tooltip("Reaction to the chassis spinning (per deg/s^2).")]
        public float spinGain = 0.25f;
        public float maxAngle = 32f;

        [Header("Bob spring (metres)")]
        public float bobStiffness = 220f;
        public float bobDamping = 9f;
        public float bobGain = 1f;
        public float maxBob = 0.12f;

        float _angle, _angleVel, _bob, _bobVel;
        Vector2 _lastVel, _accel;
        float _lastAngVel;
        Vector3 _restPos;
        bool _primed;

        void Awake() => _restPos = transform.localPosition;

        void FixedUpdate()
        {
            if (body == null || !body.simulated) return;
            float dt = Time.fixedDeltaTime;
            Vector2 v = body.linearVelocity;
            float w = body.angularVelocity;
            if (!_primed) { _lastVel = v; _lastAngVel = w; _primed = true; }

            // Smoothed acceleration in the chassis frame (raw contact spikes would make the head jitter).
            Vector2 a = (v - _lastVel) / dt;
            _accel = Vector2.Lerp(_accel, a, 0.35f);
            float spinAccel = (w - _lastAngVel) / dt;
            _lastVel = v;
            _lastAngVel = w;

            Vector2 right = body.transform.right, up = body.transform.up;
            float ax = Vector2.Dot(_accel, right);
            float ay = Vector2.Dot(_accel, up);

            // Speeding up leans the head back (counter-clockwise); a spin makes it lag the other way.
            float drive = ax * accelGain - spinAccel * spinGain;
            _angleVel += (-stiffness * _angle - damping * _angleVel + drive) * dt;
            _angle = Mathf.Clamp(_angle + _angleVel * dt, -maxAngle, maxAngle);

            // Bumps and landings squash the neck down, then it springs back.
            _bobVel += (-bobStiffness * _bob - bobDamping * _bobVel - ay * bobGain) * dt;
            _bob = Mathf.Clamp(_bob + _bobVel * dt, -maxBob, maxBob);

            transform.localRotation = Quaternion.Euler(0f, 0f, _angle);
            transform.localPosition = _restPos + new Vector3(0f, _bob, 0f);
        }
    }
}
