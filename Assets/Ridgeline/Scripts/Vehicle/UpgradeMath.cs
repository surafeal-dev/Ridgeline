using UnityEngine;

namespace Ridgeline.Vehicle
{
    /// <summary>The three MVP upgrade slots.</summary>
    public enum UpgradeSlot { Engine = 0, Suspension = 1, Tires = 2 }

    /// <summary>
    /// Pure functions that turn upgrade levels into tuning values and coin costs.
    /// Kept separate from MonoBehaviours so it is easy to test and rebalance.
    /// </summary>
    public static class UpgradeMath
    {
        public const int MaxLevel = 10;

        /// <summary>0..1 progress through an upgrade track.</summary>
        public static float T(int level) => Mathf.Clamp01(level / (float)MaxLevel);

        public static float MotorSpeed(VehicleConfig c, int engine) => Mathf.Lerp(c.baseMotorSpeed, c.maxMotorSpeed, T(engine));
        public static float MotorTorque(VehicleConfig c, int engine) => Mathf.Lerp(c.baseMotorTorque, c.maxMotorTorque, T(engine));
        public static float SpringFrequency(VehicleConfig c, int suspension) => Mathf.Lerp(c.baseSpringFrequency, c.maxSpringFrequency, T(suspension));
        public static float DampingRatio(VehicleConfig c, int suspension) => Mathf.Lerp(c.baseDampingRatio, c.maxDampingRatio, T(suspension));
        public static float TireFriction(VehicleConfig c, int tires) => Mathf.Lerp(c.baseTireFriction, c.maxTireFriction, T(tires));

        /// <summary>Coin cost to go from <paramref name="currentLevel"/> to the next level. -1 when maxed.</summary>
        public static int Cost(int currentLevel)
        {
            if (currentLevel >= MaxLevel) return -1;
            // 400, 650, 1000, 1650, 2600, 4200, 6700, 10750, 17200, 27500 (~74k per slot to max)
            float c = 400f * Mathf.Pow(1.6f, currentLevel);
            return Mathf.RoundToInt(c / 50f) * 50;
        }
    }
}
