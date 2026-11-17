using System;
using Ridgeline.InputControl;
using UnityEngine;

namespace Ridgeline.Vehicle
{
    /// <summary>
    /// Two-pedal vehicle control on top of Unity 2D physics:
    /// chassis Rigidbody2D + two wheels on WheelJoint2D (spring suspension + motor).
    ///
    /// Ground: gas drives the rear motor and squats the nose up; brake holds the wheels
    /// while rolling forward, reverses when nearly stopped, and tips the nose down.
    /// Air: gas rotates backward (nose up), brake rotates forward (nose down).
    /// </summary>
    public class VehicleController : MonoBehaviour
    {
        public VehicleConfig config;

        [Header("Parts (filled by VehicleBuilder)")]
        public Rigidbody2D chassis;
        public Rigidbody2D rearWheel;
        public Rigidbody2D frontWheel;
        public WheelJoint2D rearJoint;
        public WheelJoint2D frontJoint;
        public CircleCollider2D rearWheelCollider;
        public CircleCollider2D frontWheelCollider;

        [Tooltip("Layers counted as ground for the grounded check.")]
        public LayerMask groundMask;

        [Tooltip("Sign that makes a positive 'forward' motor drive the vehicle to the right. " +
                 "WheelJoint2D motor speed is the connected wheel's spin relative to the chassis (CCW positive).")]
        public float motorDirection = -1f;

        /// <summary>When false the pedals are ignored (results screen, paused, crashed).</summary>
        public bool controlsEnabled = true;

        /// <summary>True when either wheel touches ground this physics step.</summary>
        public bool Grounded { get; private set; }

        /// <summary>0..1 throttle actually applied this step (for fuel burn and audio).</summary>
        public float Throttle01 { get; private set; }

        /// <summary>Speed along the chassis forward axis in m/s (positive = forward/right).</summary>
        public float ForwardSpeed { get; private set; }

        /// <summary>Raised once when the driver's head touches the ground.</summary>
        public event Action HeadHit;

        public bool IsCrashed { get; private set; }

        float _motorSpeed;
        float _motorTorque;
        PhysicsMaterial2D _tireMaterial;

        void Awake()
        {
            if (config != null) ApplyUpgrades(0, 0, 0);
        }

        /// <summary>Applies Engine / Suspension / Tires levels to the live physics parts.</summary>
        public void ApplyUpgrades(int engine, int suspension, int tires)
        {
            _motorSpeed = UpgradeMath.MotorSpeed(config, engine);
            _motorTorque = UpgradeMath.MotorTorque(config, engine);

            var susp = new JointSuspension2D
            {
                frequency = UpgradeMath.SpringFrequency(config, suspension),
                dampingRatio = UpgradeMath.DampingRatio(config, suspension),
                angle = 90f,
            };
            if (rearJoint) rearJoint.suspension = susp;
            if (frontJoint) frontJoint.suspension = susp;

            if (_tireMaterial == null)
                _tireMaterial = new PhysicsMaterial2D("Tire") { bounciness = 0f };
            _tireMaterial.friction = UpgradeMath.TireFriction(config, tires);
            if (rearWheelCollider) rearWheelCollider.sharedMaterial = _tireMaterial;
            if (frontWheelCollider) frontWheelCollider.sharedMaterial = _tireMaterial;
        }

        void FixedUpdate()
        {
            if (chassis == null) return;

            bool active = controlsEnabled && !IsCrashed;
            bool gas = active && PedalInput.Gas;
            bool brake = active && PedalInput.Brake;
            if (gas && brake) gas = false; // both held: brake wins

            Grounded = IsWheelGrounded(rearWheelCollider) || IsWheelGrounded(frontWheelCollider);
            ForwardSpeed = Vector2.Dot(chassis.linearVelocity, chassis.transform.right);
            Throttle01 = gas ? 1f : 0f;

            DriveWheels(gas, brake);
            ApplyTilt(gas, brake);
        }

        bool IsWheelGrounded(Collider2D wheel) => wheel != null && wheel.IsTouchingLayers(groundMask);

        void DriveWheels(bool gas, bool brake)
        {
            var motor = rearJoint.motor;
            bool frontBrake = false;

            if (gas)
            {
                motor.motorSpeed = motorDirection * _motorSpeed;
                motor.maxMotorTorque = _motorTorque;
                rearJoint.useMotor = true;
            }
            else if (brake)
            {
                if (ForwardSpeed > 0.6f)
                {
                    // Rolling forward: hold the wheels.
                    motor.motorSpeed = 0f;
                    motor.maxMotorTorque = config.brakeTorque;
                    frontBrake = true;
                }
                else
                {
                    // Nearly stopped: reverse.
                    motor.motorSpeed = -motorDirection * _motorSpeed * config.reverseSpeedFactor;
                    motor.maxMotorTorque = _motorTorque;
                }
                rearJoint.useMotor = true;
            }
            else
            {
                // No pedal: light engine braking, so the Jeep rolls to a stop instead of coasting forever.
                motor.motorSpeed = 0f;
                motor.maxMotorTorque = config.coastBrakeTorque;
                rearJoint.useMotor = config.coastBrakeTorque > 0f;
            }
            rearJoint.motor = motor;

            // Rear-wheel drive: the front motor is only used as a brake.
            if (frontJoint)
            {
                if (frontBrake)
                {
                    frontJoint.motor = new JointMotor2D { motorSpeed = 0f, maxMotorTorque = config.brakeTorque * 0.6f };
                    frontJoint.useMotor = true;
                }
                else
                {
                    frontJoint.useMotor = false;
                }
            }
        }

        void ApplyTilt(bool gas, bool brake)
        {
            float input = (gas ? 1f : 0f) - (brake ? 1f : 0f); // +1 = nose up (CCW), -1 = nose down
            if (Grounded)
            {
                if (input != 0f) chassis.AddTorque(input * config.groundTiltTorque);
            }
            else
            {
                if (input != 0f) chassis.AddTorque(input * config.airTiltTorque);
                chassis.angularVelocity = Mathf.Clamp(chassis.angularVelocity, -config.maxAirAngularSpeed, config.maxAirAngularSpeed);
            }
        }

        /// <summary>Called by <see cref="DriverHead"/>.</summary>
        public void NotifyHeadHit()
        {
            if (IsCrashed) return;
            IsCrashed = true;
            rearJoint.useMotor = false;
            if (frontJoint) frontJoint.useMotor = false;
            HeadHit?.Invoke();
        }

        /// <summary>Freezes or unfreezes every body (YouTube onPause / onResume).</summary>
        public void SetSimulated(bool simulated)
        {
            foreach (var rb in GetComponentsInChildren<Rigidbody2D>(true))
                rb.simulated = simulated;
        }
    }
}
