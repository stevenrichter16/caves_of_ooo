using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M1.d — the MP stat wire. Hypothesis pinned RED first: the
    /// Player blueprint declared no "MP" stat, so LevelingSystem's
    /// per-level entity.GainMP(1) silently returned false and the entire
    /// mutation-point economy (SpendMPToIncreaseMutation, BuyRandomMutation)
    /// was unreachable on a real blueprint player. The fix is pure content:
    /// one stat entry on the Player blueprint.
    /// </summary>
    public class MPStatContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [Test]
        public void PlayerBlueprint_DeclaresMPStat()
        {
            var player = _harness.Factory.CreateEntity("Player");
            Assert.IsNotNull(player);

            var mp = player.GetStat("MP");
            Assert.IsNotNull(mp,
                "Player blueprint must declare an MP stat — without it, " +
                "Entity.GainMP silently no-ops and level-up MP grants vanish.");
            Assert.AreEqual(0, mp.BaseValue, "MP starts at 0.");
            Assert.AreEqual(999, mp.Max, "MP cap mirrors the SP cap.");
        }

        [Test]
        public void BlueprintPlayer_LevelUp_ActuallyGainsMP()
        {
            // End-to-end through the real blueprint: seed XP at the level-2
            // threshold, level up, and the MP grant must LAND — this is the
            // exact call chain that silently no-opped pre-M1.d.
            var player = _harness.Factory.CreateEntity("Player");
            var xp = player.GetStat("Experience");
            Assert.IsNotNull(xp);
            xp.BaseValue = LevelingSystem.XPToNextLevel(1);

            LevelingSystem.CheckLevelUp(player, null);

            Assert.AreEqual(2, player.GetStatValue("Level"), "Level advanced.");
            Assert.AreEqual(1, player.GetStatValue("MP", -1),
                "The +1 MP level-up grant must reach the blueprint player.");
        }
    }
}
