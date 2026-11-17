using System;
using UnityEngine;

namespace Ridgeline.Save
{
    /// <summary>
    /// Everything persisted to YouTube cloud save, as JSON. Keep it tiny (target &lt; 500 KiB; today ~200 bytes).
    /// Bump <see cref="CurrentVersion"/> when the shape changes and add a step to <see cref="Migrate"/>,
    /// so saves written by older builds always load.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        /// <summary>Best distance in whole metres. Also the score sent to YouTube; they must match.</summary>
        public int bestDistance;
        /// <summary>Upgrade levels, indexed by UpgradeSlot (Engine, Suspension, Tires).</summary>
        public int[] upgrades = new int[3];
        public int totalRuns;
        /// <summary>In-game sliders. YouTube's mute always overrides these.</summary>
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;

        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new SaveData();
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                data.Migrate();
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ridgeline] Save could not be parsed, starting fresh: " + e.Message);
                return new SaveData();
            }
        }

        public string ToJson() => JsonUtility.ToJson(this);

        /// <summary>Upgrades older save versions in place and repairs out-of-range values.</summary>
        public void Migrate()
        {
            // version 0 (no field) -> 1: nothing to convert yet.
            if (version < 1) version = 1;
            // Future: if (version < 2) { ...; version = 2; }

            if (upgrades == null || upgrades.Length < 3)
            {
                var fixedUp = new int[3];
                if (upgrades != null) Array.Copy(upgrades, fixedUp, Mathf.Min(upgrades.Length, 3));
                upgrades = fixedUp;
            }
            for (int i = 0; i < upgrades.Length; i++) upgrades[i] = Mathf.Clamp(upgrades[i], 0, Vehicle.UpgradeMath.MaxLevel);
            coins = Mathf.Max(0, coins);
            bestDistance = Mathf.Max(0, bestDistance);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            version = CurrentVersion;
        }
    }
}
