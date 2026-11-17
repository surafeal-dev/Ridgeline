using System;
using UnityEngine;

namespace Ridgeline.World
{
    public enum PickupKind { Coin, Fuel }

    /// <summary>
    /// A coin or fuel can. Trigger collider on the Pickup layer; collected when any
    /// collider attached to the vehicle's bodies touches it.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class Pickup : MonoBehaviour
    {
        public PickupKind kind;
        public int value;
        /// <summary>Stable id (chunk index * 1000 + slot) so collected pickups stay collected.</summary>
        public int id;
        /// <summary>World size in metres (set by the generator).</summary>
        public float size = 1f;

        public static event Action<Pickup> Collected;

        /// <summary>Set by the run; anything that is not the vehicle is ignored.</summary>
        public static Transform VehicleRoot;

        /// <summary>Clears static state (editor play sessions without domain reload).</summary>
        public static void ResetStatics()
        {
            Collected = null;
            VehicleRoot = null;
        }

        float _spin;

        void Awake()
        {
            GetComponent<CircleCollider2D>().isTrigger = true;
        }

        void OnEnable()
        {
            _spin = UnityEngine.Random.value * 6f;
            transform.localScale = Vector3.one * size;
        }

        void Update()
        {
            // Cheap "spinning coin" look: squash horizontally.
            if (kind == PickupKind.Coin)
            {
                _spin += Time.deltaTime * 4f;
                float squash = Mathf.Lerp(0.25f, 1f, Mathf.Abs(Mathf.Cos(_spin)));
                transform.localScale = new Vector3(size * squash, size, 1f);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Chassis and wheels can both enter in the same step; count it once.
            if (VehicleRoot == null || !gameObject.activeSelf) return;
            var rb = other.attachedRigidbody;
            if (rb == null || !rb.transform.IsChildOf(VehicleRoot)) return;
            gameObject.SetActive(false);
            Collected?.Invoke(this);
        }
    }
}
