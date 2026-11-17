using System;
using Ridgeline.Localization;
using Ridgeline.Vehicle;
using Ridgeline.World;
using UnityEngine;

namespace Ridgeline.Game
{
    public enum EndReason { DriverDown, OutOfFuel }

    public struct RunResult
    {
        public EndReason reason;
        public int distance;
        public int coinsCollected;
        public int bonusCoins;
        public int Total => coinsCollected + bonusCoins;
    }

    /// <summary>
    /// One run: fuel, distance, coins, stunt bonuses (flips, air time), milestones, and the end conditions.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        [Header("Stunt bonuses")]
        public int flipBonus = 50;
        public float airTimeThreshold = 1.6f;
        public int airTimeBonusPerSecond = 20;
        [Tooltip("Degrees of rotation that count as one flip (a bit under 360 to be forgiving).")]
        public float flipDegrees = 330f;

        [Header("End conditions")]
        public float crashEndDelay = 1.4f;
        [Tooltip("With an empty tank, the run ends after the vehicle has been slower than this for outOfFuelHold seconds.")]
        public float outOfFuelSpeed = 0.35f;
        public float outOfFuelHold = 1.5f;

        public event Action<RunResult> Ended;
        /// <summary>A bonus was earned: (label, coins).</summary>
        public event Action<string, int> Bonus;
        public event Action<Pickup> PickedUp;

        public bool Running { get; private set; }
        public float Fuel01 => _vehicle ? Mathf.Clamp01(_fuel / _vehicle.config.fuelCapacity) : 0f;
        public int Distance => Mathf.FloorToInt(_distance);
        public int Coins => _coins + _bonus;

        VehicleController _vehicle;
        StageConfig _stage;
        float _fuel, _startX, _distance;
        int _coins, _bonus, _milestonesPaid;
        float _airTime, _airRotation, _prevAngle;
        bool _wasGrounded = true;
        float _slowTimer, _crashTimer;
        bool _crashed;

        void OnEnable() => Pickup.Collected += OnPickup;
        void OnDisable() => Pickup.Collected -= OnPickup;

        public void Begin(VehicleController vehicle, StageConfig stage)
        {
            if (_vehicle) _vehicle.HeadHit -= OnHeadHit;
            _vehicle = vehicle;
            _stage = stage;
            _vehicle.HeadHit += OnHeadHit;

            _fuel = vehicle.config.fuelCapacity;
            _startX = vehicle.chassis.position.x;
            _distance = 0f;
            _coins = _bonus = _milestonesPaid = 0;
            _airTime = _airRotation = 0f;
            _prevAngle = vehicle.chassis.rotation;
            _wasGrounded = true;
            _slowTimer = _crashTimer = 0f;
            _crashed = false;
            Running = true;
        }

        public void Abort()
        {
            Running = false;
            if (_vehicle) _vehicle.HeadHit -= OnHeadHit;
        }

        void Update()
        {
            if (!Running || _vehicle == null) return;
            float dt = Time.deltaTime;

            // Distance (best forward progress this run).
            _distance = Mathf.Max(_distance, _vehicle.chassis.position.x - _startX);
            PayMilestones();

            if (_crashed)
            {
                _crashTimer += dt;
                if (_crashTimer >= crashEndDelay) Finish(EndReason.DriverDown);
                return;
            }

            // Fuel: idle burn + throttle burn.
            if (_fuel > 0f)
            {
                var c = _vehicle.config;
                _fuel -= (c.idleFuelBurn + c.throttleFuelBurn * _vehicle.Throttle01) * dt;
                if (_fuel <= 0f)
                {
                    _fuel = 0f;
                    _vehicle.controlsEnabled = false; // engine dies; the vehicle can still roll
                }
            }
            else
            {
                if (_vehicle.chassis.linearVelocity.magnitude < outOfFuelSpeed) _slowTimer += dt;
                else _slowTimer = 0f;
                if (_slowTimer >= outOfFuelHold) { Finish(EndReason.OutOfFuel); return; }
            }

            TrackStunts(dt);
        }

        void TrackStunts(float dt)
        {
            float angle = _vehicle.chassis.rotation;
            float delta = Mathf.DeltaAngle(_prevAngle, angle);
            _prevAngle = angle;

            bool grounded = _vehicle.Grounded;
            if (!grounded)
            {
                _airTime += dt;
                _airRotation += delta;
            }
            else if (!_wasGrounded)
            {
                // Landed: pay flips and air time.
                int flips = Mathf.FloorToInt(Mathf.Abs(_airRotation) / flipDegrees);
                if (flips > 0)
                {
                    int coins = flipBonus * flips * flips; // doubles reward the risk
                    AddBonus(flips == 1 ? Strings.Format("flip", coins) : Strings.Format("double_flip", flips, coins), coins);
                }
                if (_airTime >= airTimeThreshold)
                {
                    int coins = Mathf.RoundToInt(_airTime * airTimeBonusPerSecond);
                    AddBonus(Strings.Format("air_time", coins), coins);
                }
                _airTime = 0f;
                _airRotation = 0f;
            }
            _wasGrounded = grounded;
        }

        void PayMilestones()
        {
            if (_stage == null || _stage.milestoneEvery <= 0f) return;
            int reached = Mathf.FloorToInt(_distance / _stage.milestoneEvery);
            while (_milestonesPaid < reached)
            {
                _milestonesPaid++;
                int coins = _stage.milestoneBaseBonus * _milestonesPaid;
                int metres = Mathf.RoundToInt(_milestonesPaid * _stage.milestoneEvery);
                AddBonus(Strings.Format("milestone", metres, coins), coins);
            }
        }

        void AddBonus(string label, int coins)
        {
            _bonus += coins;
            Bonus?.Invoke(label, coins);
        }

        void OnPickup(Pickup p)
        {
            if (!Running || _crashed) return;
            if (p.kind == PickupKind.Coin) _coins += p.value;
            else
            {
                _fuel = _vehicle.config.fuelCapacity;
                _vehicle.controlsEnabled = true;
                _slowTimer = 0f;
            }
            PickedUp?.Invoke(p);
        }

        void OnHeadHit()
        {
            if (!Running) return;
            _crashed = true;
            _crashTimer = 0f;
        }

        void Finish(EndReason reason)
        {
            if (!Running) return;
            Running = false;
            if (_vehicle) _vehicle.HeadHit -= OnHeadHit;
            Ended?.Invoke(new RunResult
            {
                reason = reason,
                distance = Distance,
                coinsCollected = _coins,
                bonusCoins = _bonus,
            });
        }
    }
}
