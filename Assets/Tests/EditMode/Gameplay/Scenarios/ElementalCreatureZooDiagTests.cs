using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// T1.4 verification for <see cref="ElementalCreatureZoo"/>.
    /// Pins the layout contract:
    ///   1. All 9 expected creature blueprints spawn and reach the
    ///      zone (rules out a typo'd blueprint name silently dropping
    ///      a creature)
    ///   2. Each creature carries the expected elemental resistance
    ///      Stats (rules out an Objects.json edit silently changing
    ///      the matrix the scenario depends on)
    ///   3. Counter-check: applying the scenario produces zero diag
    ///      records on quest/trade channels (it's a layout-only
    ///      showcase, no quest/trade interactions)
    ///
    /// Pattern follows the 12+ prior scenario diag fixtures.
    /// </summary>
    [TestFixture]
    public class ElementalCreatureZooDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. All 9 creatures spawn into the zone
        // ====================================================================

        [Test]
        public void Apply_SpawnsAllNineResistanceCreatures()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");

            new ElementalCreatureZoo().Apply(ctx);

            string[] expectedBlueprints =
            {
                "Snapjaw", "SnapjawHunter", "IceWight", "CharredHusk",
                "Glowmaw", "StoneGolem", "BrassHusk", "CaveSlime", "Scorpion",
            };

            // Use zone enumeration via the harness's zone reference.
            // Note: ctx.Zone is the production zone. We collect all
            // entities via GetAllEntities (the safe enumerator).
            var spawned = ctx.Zone.GetAllEntities()
                .Select(e => e.BlueprintName)
                .ToList();

            foreach (var bp in expectedBlueprints)
            {
                Assert.IsTrue(spawned.Contains(bp),
                    $"Zoo must spawn a {bp}. Got: [{string.Join(",", spawned)}]");
            }
        }

        // ====================================================================
        // 2. Each creature carries its expected resistance stat. Pinning
        //    these prevents an Objects.json edit silently changing the
        //    matrix the scenario advertises in its docstring.
        // ====================================================================

        [Test]
        public void Apply_EachCreatureCarriesExpectedResistanceStat()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");

            new ElementalCreatureZoo().Apply(ctx);

            // Walk the zone, find each by blueprint, assert resistance value.
            // (BlueprintName lookup is O(zone size); fine for a 9-creature
            // scenario test — not a hot path.)
            var byBlueprint = ctx.Zone.GetAllEntities()
                .GroupBy(e => e.BlueprintName)
                .ToDictionary(g => g.Key, g => g.First());

            AssertResistance(byBlueprint, "Snapjaw",        "ColdResistance",     25);
            AssertResistance(byBlueprint, "SnapjawHunter",  "ColdResistance",     50);
            AssertResistance(byBlueprint, "IceWight",       "ColdResistance",    100);
            AssertResistance(byBlueprint, "IceWight",       "HeatResistance",    -50);
            AssertResistance(byBlueprint, "CharredHusk",    "HeatResistance",    100);
            AssertResistance(byBlueprint, "CharredHusk",    "ColdResistance",    -50);
            AssertResistance(byBlueprint, "Glowmaw",        "HeatResistance",     50);
            AssertResistance(byBlueprint, "StoneGolem",     "ElectricResistance", 50);
            AssertResistance(byBlueprint, "BrassHusk",      "ElectricResistance",-50);
            AssertResistance(byBlueprint, "CaveSlime",      "AcidResistance",     50);
            AssertResistance(byBlueprint, "Scorpion",       "AcidResistance",    -50);
        }

        // ====================================================================
        // 3. Counter-check: applying the scenario produces NO quest/trade
        //    records. The zoo is a layout-only showcase — if a future
        //    edit accidentally wires it to a quest/trade flow, this test
        //    catches the regression.
        // ====================================================================

        [Test]
        public void Apply_ProducesNoQuestOrTradeDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            Diag.ResetAll();

            new ElementalCreatureZoo().Apply(ctx);

            var questRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Limit = 50,
            }).Records;
            var tradeRecs = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "trade",
                Limit = 50,
            }).Records;

            Assert.AreEqual(0, questRecs.Count,
                "Zoo apply must NOT emit quest/* records — it's a layout-only " +
                $"showcase. Got {questRecs.Count}.");
            Assert.AreEqual(0, tradeRecs.Count,
                "Zoo apply must NOT emit trade/* records — it's a layout-only " +
                $"showcase. Got {tradeRecs.Count}.");
        }

        private static void AssertResistance(
            System.Collections.Generic.Dictionary<string, Entity> byBlueprint,
            string blueprint, string statName, int expected)
        {
            Assert.IsTrue(byBlueprint.ContainsKey(blueprint),
                $"Zoo must spawn {blueprint} (precondition for {statName} check)");
            var entity = byBlueprint[blueprint];
            Assert.IsTrue(entity.Statistics.ContainsKey(statName),
                $"{blueprint} must carry stat {statName} (resistance matrix " +
                $"depends on this — Objects.json must declare it)");
            Assert.AreEqual(expected, entity.Statistics[statName].BaseValue,
                $"{blueprint}.{statName} expected {expected}");
        }
    }
}
