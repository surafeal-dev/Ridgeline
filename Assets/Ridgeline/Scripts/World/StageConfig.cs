using UnityEngine;

namespace Ridgeline.World
{
    /// <summary>
    /// Data for one stage. The MVP ships one: Meadow.
    /// The terrain is generated from a fixed seed, so it is the same every run and players can learn it.
    /// </summary>
    [CreateAssetMenu(menuName = "Ridgeline/Stage Config", fileName = "StageConfig")]
    public class StageConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id = "meadow";
        public string displayName = "Meadow";

        [Header("Physics")]
        public int seed = 1729;
        [Tooltip("World gravity (m/s^2, negative = down).")]
        public float gravity = -9.81f;
        [Tooltip("Ground friction (combined with the tyre friction).")]
        public float surfaceFriction = 0.9f;

        [Header("Shape")]
        [Tooltip("Flat run-up at the start, in metres.")]
        public float flatStart = 18f;
        [Tooltip("Metres over which the hills blend in after the flat start.")]
        public float blendIn = 25f;
        [Tooltip("Distance (m) at which difficulty reaches its maximum.")]
        public float difficultyDistance = 1700f;
        [Tooltip("Large rolling hills: amplitude at start / at max difficulty (m).")]
        public float hillAmplitudeStart = 18f;
        public float hillAmplitudeMax = 30f;
        public float hillFrequency = 0.014f;
        [Tooltip("Medium bumps.")]
        public float bumpAmplitudeStart = 5f;
        public float bumpAmplitudeMax = 9f;
        public float bumpFrequency = 0.052f;
        [Tooltip("Small roughness.")]
        public float roughAmplitudeStart = 0.25f;
        public float roughAmplitudeMax = 0.7f;
        public float roughFrequency = 0.22f;
        [Tooltip("Slow overall climb in metres per metre (0 = flat on average).")]
        public float upwardTrend = 0.02f;

        [Header("Mesh")]
        public float chunkWidth = 40f;
        public float pointSpacing = 0.5f;
        public float groundDepth = 30f;
        public float grassThickness = 0.45f;
        public Color grassColor = new Color(0.42f, 0.71f, 0.29f);
        public Color dirtColor = new Color(0.55f, 0.38f, 0.24f);
        public Color dirtDeepColor = new Color(0.36f, 0.24f, 0.16f);
        public Color skyTop = new Color(0.45f, 0.72f, 0.95f);
        public Color skyBottom = new Color(0.85f, 0.93f, 0.98f);

        [Header("Fuel cans")]
        [Tooltip("Distance between fuel cans at the start / at max difficulty (m).")]
        public float fuelSpacingStart = 140f;
        public float fuelSpacingMax = 420f;

        [Header("Coins")]
        public float coinGroupSpacing = 55f;
        public int coinsPerGroupMin = 4;
        public int coinsPerGroupMax = 8;
        public float coinSpacing = 1.3f;
        public float coinHeight = 1.2f;

        [Header("Distance milestones")]
        [Tooltip("A milestone bonus is paid every this many metres.")]
        public float milestoneEvery = 250f;
        public int milestoneBaseBonus = 100;

        /// <summary>0..1 difficulty at a given x.</summary>
        public float Difficulty(float x) => Mathf.Clamp01((x - flatStart) / Mathf.Max(1f, difficultyDistance));

        /// <summary>Terrain height at world x. Deterministic for a given seed.</summary>
        public float HeightAt(float x)
        {
            if (x <= flatStart) return 0f;

            float d = Difficulty(x);
            float sx = seed * 0.6180339f;
            float sy = seed * 0.4142135f;

            float hills = (Mathf.PerlinNoise(x * hillFrequency + sx, sy) - 0.5f) * 2f
                          * Mathf.Lerp(hillAmplitudeStart, hillAmplitudeMax, d);
            float bumps = (Mathf.PerlinNoise(x * bumpFrequency + sx * 1.7f, sy + 13.1f) - 0.5f) * 2f
                          * Mathf.Lerp(bumpAmplitudeStart, bumpAmplitudeMax, d);
            float rough = (Mathf.PerlinNoise(x * roughFrequency + sx * 3.1f, sy + 41.7f) - 0.5f) * 2f
                          * Mathf.Lerp(roughAmplitudeStart, roughAmplitudeMax, d);
            float trend = (x - flatStart) * upwardTrend;

            // Subtract the noise value at the flat start so the track joins the run-up without a step.
            float h = hills + bumps + rough + trend - BaseAt(sx, sy);

            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((x - flatStart) / Mathf.Max(0.01f, blendIn)));
            return h * blend;
        }

        float BaseAt(float sx, float sy)
        {
            float x = flatStart;
            return (Mathf.PerlinNoise(x * hillFrequency + sx, sy) - 0.5f) * 2f * hillAmplitudeStart
                 + (Mathf.PerlinNoise(x * bumpFrequency + sx * 1.7f, sy + 13.1f) - 0.5f) * 2f * bumpAmplitudeStart
                 + (Mathf.PerlinNoise(x * roughFrequency + sx * 3.1f, sy + 41.7f) - 0.5f) * 2f * roughAmplitudeStart;
        }

        /// <summary>Slope angle (degrees) of the terrain at x.</summary>
        public float SlopeAt(float x, float dx = 0.25f)
        {
            float dy = HeightAt(x + dx) - HeightAt(x - dx);
            return Mathf.Atan2(dy, 2f * dx) * Mathf.Rad2Deg;
        }

        public float FuelSpacingAt(float x) => Mathf.Lerp(fuelSpacingStart, fuelSpacingMax, Difficulty(x));
    }
}
