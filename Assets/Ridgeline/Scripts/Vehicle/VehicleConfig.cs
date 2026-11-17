using UnityEngine;

namespace Ridgeline.Vehicle
{
    /// <summary>
    /// Tuning data for one vehicle. MVP ships one: the Jeep (rear-wheel drive).
    /// Upgrade levels scale these base values (see <see cref="UpgradeMath"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Ridgeline/Vehicle Config", fileName = "VehicleConfig")]
    public class VehicleConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id = "jeep";
        public string displayName = "Jeep";

        [Header("Body")]
        [Tooltip("Chassis mass in kg-ish units (Rigidbody2D mass).")]
        public float chassisMass = 3f;
        [Tooltip("Centre of mass offset from the chassis pivot. Lower = more stable, higher = flips more easily.")]
        public Vector2 centerOfMass = new Vector2(0.15f, -0.2f);
        public float chassisLinearDamping = 0.05f;
        public float chassisAngularDamping = 0.25f;

        [Header("Wheels")]
        public float wheelRadius = 0.595f;
        public float wheelMass = 0.4f;
        [Tooltip("Local anchor of the rear wheel on the chassis.")]
        public Vector2 rearWheelAnchor = new Vector2(-0.81f, -0.69f);
        [Tooltip("Local anchor of the front wheel on the chassis.")]
        public Vector2 frontWheelAnchor = new Vector2(0.99f, -0.69f);
        [Tooltip("Base tyre friction (Tires upgrade adds to this).")]
        public float baseTireFriction = 0.6f;
        public float maxTireFriction = 1.1f;

        [Header("Engine (rear wheel motor)")]
        [Tooltip("Top wheel speed in degrees/second at Engine level 0.")]
        public float baseMotorSpeed = 1100f;
        public float maxMotorSpeed = 2000f;
        [Tooltip("Max motor torque at Engine level 0.")]
        public float baseMotorTorque = 22f;
        public float maxMotorTorque = 40f;
        [Tooltip("Reverse speed as a fraction of forward top speed.")]
        [Range(0f, 1f)] public float reverseSpeedFactor = 0.35f;
        [Tooltip("Torque used to hold the wheels when braking while rolling forward.")]
        public float brakeTorque = 60f;
        [Tooltip("Engine braking on the rear wheel when no pedal is held, so the Jeep slows down instead of coasting forever.")]
        public float coastBrakeTorque = 3f;

        [Header("Suspension")]
        [Tooltip("Spring frequency (Hz) at Suspension level 0. Higher = stiffer.")]
        public float baseSpringFrequency = 3.6f;
        [Tooltip("Spring frequency at max Suspension level (softer, more stable).")]
        public float maxSpringFrequency = 3.0f;
        public float baseDampingRatio = 0.3f;
        public float maxDampingRatio = 0.6f;

        [Header("Tilt control")]
        [Tooltip("Chassis torque from gas/brake while on the ground. With the motor's own reaction torque this is " +
                 "slightly more than gravity's righting torque, so holding gas lifts the nose and keeps lifting it (a flip).")]
        public float groundTiltTorque = 10f;
        [Tooltip("Chassis torque from gas/brake while airborne (rotate backward / forward).")]
        public float airTiltTorque = 22f;
        [Tooltip("Max angular speed (deg/s) the player can spin the vehicle in the air.")]
        public float maxAirAngularSpeed = 450f;

        [Header("Fuel")]
        [Tooltip("Fuel tank size in seconds of idle driving.")]
        public float fuelCapacity = 100f;
        [Tooltip("Fuel per second while idle.")]
        public float idleFuelBurn = 1.2f;
        [Tooltip("Extra fuel per second at full throttle.")]
        public float throttleFuelBurn = 1.8f;

        [Header("Driver (bobblehead in the open cab)")]
        [Tooltip("Crash sensor centre on the chassis: the driver's head. Keep it at the head's rest position.")]
        public Vector2 headOffset = new Vector2(0.13f, 0.68f);
        [Tooltip("Crash sensor radius (about half the head height).")]
        public float headRadius = 0.36f;
        [Tooltip("Driver seat (torso centre) on the chassis. The neck is at the top of the torso; the head wobbles around it.")]
        public Vector2 driverSeat = new Vector2(0.13f, 0.22f);
        [Tooltip("Head height in metres. Deliberately big (bobblehead).")]
        public float driverHeadHeight = 0.8f;
        public float driverTorsoHeight = 0.36f;

        [Header("Art layout")]
        [Tooltip("World width of the body sprite (art is scaled to this).")]
        public float bodyArtWidth = 3.4f;
        public Vector2 bodyArtOffset = Vector2.zero;
        [Tooltip("Chassis collider outline in chassis-local metres (matches the body art).")]
        public Vector2[] bodyShape =
        {
            // Open cab: the top edge is the bed/door line; the driver's head is the only thing above it
            // (apart from the roll bar and windshield, which have no collider).
            new Vector2(-1.66f, 0.19f), new Vector2(-1.66f, -0.45f), new Vector2(1.66f, -0.45f),
            new Vector2(1.68f, -0.03f), new Vector2(0.8f, 0.15f),
        };
    }
}
