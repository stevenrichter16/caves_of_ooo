using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M1.c — the XP economy pass. Before this, all four lair
    /// bosses lacked an XPValue stat: three inherited base-Creature 10 and
    /// SnapjawChieftain inherited 15 from Snapjaw — every boss awarded
    /// less XP than a common SnapjawHunter (40), and most of the
    /// wilderness bestiary awarded the base 10. These pins are the
    /// reward-scaling contract for the mortal-start progression curve.
    /// </summary>
    public class XPValueContentTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [Test]
        [TestCase("SnapjawChieftain", 150)]
        [TestCase("DesertProwler", 200)]
        [TestCase("JungleStalker", 200)]
        [TestCase("AncientGuardian", 400)]
        public void Bosses_AwardBossScaleXP(string blueprint, int expected)
        {
            var boss = _harness.Factory.CreateEntity(blueprint);
            Assert.IsNotNull(boss, blueprint);
            Assert.AreEqual(expected, boss.GetStatValue("XPValue", -1),
                $"{blueprint} must award boss-scale XP — pre-M1.c it awarded " +
                "less than a common snapjaw.");
        }

        [Test]
        [TestCase("StoneGolem", 80)]
        [TestCase("SandWurm", 60)]
        [TestCase("SleepingTroll", 75)]
        [TestCase("CaveBear", 35)]
        [TestCase("ChoirTendril", 100)]
        public void HeavyWilderness_AwardsTierScaleXP(string blueprint, int expected)
        {
            var creature = _harness.Factory.CreateEntity(blueprint);
            Assert.IsNotNull(creature, blueprint);
            Assert.AreEqual(expected, creature.GetStatValue("XPValue", -1),
                $"{blueprint} tier-scale XPValue.");
        }

        [Test]
        public void CaveBat_InheritsBaseCreatureDefault()
        {
            // Counter-check: creatures deliberately left without an explicit
            // XPValue keep inheriting base-Creature 10 — the pass added
            // values, it did not change the inheritance default.
            var bat = _harness.Factory.CreateEntity("CaveBat");
            Assert.IsNotNull(bat);
            Assert.AreEqual(10, bat.GetStatValue("XPValue", -1),
                "CaveBat inherits the base-Creature XPValue of 10.");
        }
    }
}
