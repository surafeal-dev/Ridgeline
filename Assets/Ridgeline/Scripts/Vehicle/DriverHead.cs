using UnityEngine;

namespace Ridgeline.Vehicle
{
    /// <summary>
    /// Crash sensor: a trigger circle around the cab roof / driver's head. Touching ground ends the run.
    /// Swaps the driver's face to the "hurt" sprite on impact.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class DriverHead : MonoBehaviour
    {
        public VehicleController vehicle;
        public SpriteRenderer face;
        public Sprite hurtFace;

        void OnTriggerEnter2D(Collider2D other) => Check(other);
        void OnTriggerStay2D(Collider2D other) => Check(other);

        void Check(Collider2D other)
        {
            if (vehicle == null || vehicle.IsCrashed) return;
            if ((vehicle.groundMask.value & (1 << other.gameObject.layer)) != 0)
            {
                if (face != null && hurtFace != null) face.sprite = hurtFace;
                vehicle.NotifyHeadHit();
            }
        }
    }
}
