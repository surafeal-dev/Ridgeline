using NUnit.Framework;
using Ridgeline.Save;
using Ridgeline.Vehicle;
using Ridgeline.World;
using UnityEngine;

namespace Ridgeline.Tests
{
    public class CoreLogicTests
    {
        [Test]
        public void Save_EmptyOrGarbage_StartsFresh()
        {
            Assert.AreEqual(0, SaveData.FromJson("").coins);
            Assert.AreEqual(0, SaveData.FromJson(null).coins);
            Assert.AreEqual(0, SaveData.FromJson("{not json").coins);
        }

        [Test]
        public void Save_OldVersionWithoutFields_LoadsAndMigrates()
        {
            // A "version 0" save from an older build: no version, short upgrades array.
            var d = SaveData.FromJson("{\"coins\":1234,\"bestDistance\":321,\"upgrades\":[2]}");
            Assert.AreEqual(SaveData.CurrentVersion, d.version);
            Assert.AreEqual(1234, d.coins);
            Assert.AreEqual(321, d.bestDistance);
            Assert.AreEqual(3, d.upgrades.Length);
            Assert.AreEqual(2, d.upgrades[0]);
        }

        [Test]
        public void Save_RoundTrip_IsSmall()
        {
            var d = new SaveData { coins = 999999, bestDistance = 12345, totalRuns = 500 };
            string json = d.ToJson();
            Assert.Less(json.Length, 500 * 1024, "save must stay under 500 KiB");
            var back = SaveData.FromJson(json);
            Assert.AreEqual(d.coins, back.coins);
            Assert.AreEqual(d.bestDistance, back.bestDistance);
        }

        [Test]
        public void Save_ClampsOutOfRangeValues()
        {
            var d = SaveData.FromJson("{\"version\":1,\"coins\":-5,\"upgrades\":[99,-1,3],\"sfxVolume\":4}");
            Assert.AreEqual(0, d.coins);
            Assert.AreEqual(UpgradeMath.MaxLevel, d.upgrades[0]);
            Assert.AreEqual(0, d.upgrades[1]);
            Assert.AreEqual(1f, d.sfxVolume);
        }

        [Test]
        public void Upgrades_CostRisesAndCapsAtMax()
        {
            int prev = 0;
            for (int lvl = 0; lvl < UpgradeMath.MaxLevel; lvl++)
            {
                int c = UpgradeMath.Cost(lvl);
                Assert.Greater(c, prev);
                prev = c;
            }
            Assert.AreEqual(-1, UpgradeMath.Cost(UpgradeMath.MaxLevel));
        }

        [Test]
        public void Stage_IsDeterministic_FlatAtStart_AndContinuous()
        {
            var s = ScriptableObject.CreateInstance<StageConfig>();
            Assert.AreEqual(0f, s.HeightAt(0f));
            Assert.AreEqual(0f, s.HeightAt(s.flatStart));
            // No step where the hills begin.
            Assert.Less(Mathf.Abs(s.HeightAt(s.flatStart + 0.5f)), 0.05f);
            // Same seed, same terrain.
            Assert.AreEqual(s.HeightAt(512.3f), s.HeightAt(512.3f));
            // Small step -> small change (no cliffs from the noise).
            for (float x = 0; x < 3000f; x += 0.5f)
                Assert.Less(Mathf.Abs(s.HeightAt(x + 0.5f) - s.HeightAt(x)), 1.0f, $"cliff at x={x}");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void Stage_FuelGetsSparserWithDistance()
        {
            var s = ScriptableObject.CreateInstance<StageConfig>();
            Assert.Less(s.FuelSpacingAt(0f), s.FuelSpacingAt(5000f));
            Object.DestroyImmediate(s);
        }
    }
}
