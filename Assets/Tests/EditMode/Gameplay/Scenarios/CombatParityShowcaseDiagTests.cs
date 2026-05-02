using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// End-to-end verification for <see cref="CombatParityShowcase"/>
    /// — the demo for Phases A/C/D/E of the Qud-parity combat port.
    ///
    /// Three pinnable contracts at the scenario level:
    ///
    ///   - **Phase D (Critical attribute)**: across ~100 seeded swings
    ///     on a soak Snapjaw, at least one nat-20 crit lands and the
    ///     resulting damage record carries the "Critical" attribute.
    ///     P(no nat-20 in 100 to-hit rolls) = (1 - 0.05)^100 ≈ 0.59% —
    ///     statistically very likely to land at least one across seeds.
    ///
    ///   - **Phase E (Heat-immune)**: synthesized Damage(20)+"Fire"
    ///     applied to the Heat-immune Snapjaw (HeatResistance=100)
    ///     produces NO diag damage record — ApplyDamage's resistance
    ///     pass takes Amount to 0, fires DamageFullyResisted, and
    ///     returns BEFORE the diag DamageDealt hook. HP stays at full.
    ///
    ///   - **Phase E (Cold-vulnerable)**: synthesized Damage(20)+"Cold"
    ///     applied to the Cold-vulnerable Snapjaw (ColdResistance=-100)
    ///     produces a diag damage record with amount=40 (vulnerability
    ///     formula: 20 + 20·(-100/-100) = 40).
    ///
    /// Per-contract counter-checks ensure the positive assertions
    /// aren't passing vacuously:
    ///   - Critical: would always land somewhere, just verifying it
    ///     surfaces in the diag record (no separate counter — covered
    ///     by the prior elemental fixture's Mace counter-check).
    ///   - Heat resistance: same Fire damage on a normal Snapjaw (no
    ///     HeatResistance stat) DOES produce a diag record with
    ///     amount=20 — proves the resistance code is what's blocking,
    ///     not a global "Fire damage doesn't fire diag" bug.
    ///   - Cold vulnerability: same Cold damage on a normal Snapjaw
    ///     produces a diag record with amount=15 (the Snapjaw blueprint
    ///     has ColdResistance=25, so 20 × 0.75 = 15). Critically NOT
    ///     doubled to 40 — proves the doubling came from
    ///     ColdResistance=-100 on the vulnerable Snapjaw, not from a
    ///     global "Cold damage always doubles" bug.
    ///
    /// Phases A and C aren't given dedicated tests here — A is "damage
    /// has Melee+Strength attributes" (covered implicitly by every
    /// scenario diag fixture's damage records), C is "Damage object
    /// flows through ApplyDamage end-to-end" (also implicit). Neither
    /// has a separate user-visible behavior worth pinning.
    /// </summary>
    [TestFixture]
    public class CombatParityShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // Phase D — nat-20 crits add "Critical" attribute to damage records
        // ====================================================================

        [Test]
        public void PhaseD_NatTwentyCrits_AddCriticalAttribute()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatParityShowcase().Apply(ctx);

            // Soak Snapjaws (normal — no resistance stats). Pick the first
            // for a clean target. The 9999 HP padding from the scenario
            // means ~100 swings won't kill it.
            var soak = FindNormalSnapjaw(ctx);
            Assert.IsNotNull(soak, "Scenario must spawn at least one normal-stats Snapjaw.");

            Diag.ResetAll();

            // 100 seeded swings. With nat-20 chance 1/20 = 5%, P(no crits)
            // = 0.95^100 ≈ 0.59% — extremely unlikely to fall below 1.
            for (int seed = 0; seed < 100; seed++)
            {
                CombatSystem.PerformMeleeAttack(
                    ctx.PlayerEntity, soak, ctx.Zone, new Random(seed));
                if (soak.GetStatValue("Hitpoints") <= 0) break;
            }

            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = soak.ID,
                Limit = 200,
            }).Records;

            Assert.GreaterOrEqual(damageRecords.Count, 10,
                $"Expected at least 10 hits across 100 seeded swings (typical " +
                $"hit rate is well above 10%). Got {damageRecords.Count}. " +
                $"If too low, the test setup may have stat issues.");

            int criticalCount = damageRecords.Count(r => r.PayloadJson.Contains("Critical"));
            Assert.GreaterOrEqual(criticalCount, 1,
                $"Phase D: at least one of 100 seeded swings must land a nat-20 " +
                $"and tag damage with 'Critical'. Got {criticalCount} crits across " +
                $"{damageRecords.Count} damage records. " +
                $"Sample payload: {damageRecords[0].PayloadJson}");
        }

        // ====================================================================
        // Phase E — Heat-immune Snapjaw fully resists Fire damage
        // ====================================================================

        [Test]
        public void PhaseE_FireOnHeatImmune_FullyResistedNoDiagRecord()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatParityShowcase().Apply(ctx);

            var heatImmune = FindSnapjawByResistance(ctx, "HeatResistance", expected: 100);
            Assert.IsNotNull(heatImmune,
                "Scenario must spawn a Heat-immune Snapjaw (HeatResistance=100).");

            int hpBefore = heatImmune.GetStatValue("Hitpoints");
            Diag.ResetAll();

            // Synthesize a Fire-tagged damage object. Direct ApplyDamage —
            // melee fists don't carry Fire attribute, so the only way to
            // exercise the Fire-resistance branch is to construct the
            // Damage directly (the scenario's apply-time log spells this out).
            var fireDamage = new Damage(20);
            fireDamage.AddAttribute("Fire");

            CombatSystem.ApplyDamage(heatImmune, fireDamage, ctx.PlayerEntity, ctx.Zone);

            int hpAfter = heatImmune.GetStatValue("Hitpoints");
            Assert.AreEqual(hpBefore, hpAfter,
                $"Heat-immune Snapjaw must take ZERO HP from Fire damage. " +
                $"hpBefore={hpBefore}, hpAfter={hpAfter}.");

            // ApplyDamage's resistance pass takes Amount to 0, fires
            // DamageFullyResisted, and returns BEFORE the diag damage
            // hook. So we expect NO diag damage record on this target.
            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = heatImmune.ID,
                Limit = 50,
            }).Records;

            Assert.AreEqual(0, damageRecords.Count,
                $"Phase E: Heat-immune Snapjaw must produce no diag DamageDealt " +
                $"records when struck with Fire damage (full resistance returns " +
                $"early before the diag hook). Got {damageRecords.Count} records: " +
                $"[{string.Join(", ", damageRecords.Select(r => r.PayloadJson))}]");
        }

        // ====================================================================
        // Phase E counter-check — Fire damage on a normal Snapjaw applies
        // (verifies "no diag record on heat-immune" wasn't a global bug)
        // ====================================================================

        [Test]
        public void PhaseE_FireOnNormalSnapjaw_FullDamageAppliesAndRecordsToDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatParityShowcase().Apply(ctx);

            var normal = FindNormalSnapjaw(ctx);
            Assert.IsNotNull(normal, "Scenario must spawn at least one normal-stats Snapjaw.");

            int hpBefore = normal.GetStatValue("Hitpoints");
            Diag.ResetAll();

            var fireDamage = new Damage(20);
            fireDamage.AddAttribute("Fire");

            CombatSystem.ApplyDamage(normal, fireDamage, ctx.PlayerEntity, ctx.Zone);

            int hpAfter = normal.GetStatValue("Hitpoints");
            Assert.AreEqual(hpBefore - 20, hpAfter,
                $"Normal Snapjaw (no HeatResistance stat) must take full 20 Fire damage. " +
                $"hpBefore={hpBefore}, hpAfter={hpAfter}, expected delta=20.");

            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = normal.ID,
                Limit = 50,
            }).Records;

            Assert.AreEqual(1, damageRecords.Count,
                $"Normal Snapjaw must produce exactly one diag DamageDealt record. " +
                $"Got {damageRecords.Count}.");
            Assert.IsTrue(damageRecords[0].PayloadJson.Contains("\"amount\":20"),
                $"Damage record must show amount=20 (no resistance reduction). " +
                $"Payload: {damageRecords[0].PayloadJson}");
        }

        // ====================================================================
        // Phase E — Cold-vulnerable Snapjaw takes doubled Cold damage
        // ====================================================================

        [Test]
        public void PhaseE_ColdOnVulnerable_DoubledDamageRecordsToDiag()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatParityShowcase().Apply(ctx);

            var vulnerable = FindSnapjawByResistance(ctx, "ColdResistance", expected: -100);
            Assert.IsNotNull(vulnerable,
                "Scenario must spawn a Cold-vulnerable Snapjaw (ColdResistance=-100).");

            int hpBefore = vulnerable.GetStatValue("Hitpoints");
            Diag.ResetAll();

            var coldDamage = new Damage(20);
            coldDamage.AddAttribute("Cold");

            CombatSystem.ApplyDamage(vulnerable, coldDamage, ctx.PlayerEntity, ctx.Zone);

            int hpAfter = vulnerable.GetStatValue("Hitpoints");
            // Vulnerability formula (CombatSystem.ApplyResistanceFor):
            //   damage.Amount += damage.Amount * (resist / -100f)
            // ColdResistance=-100: 20 + 20 * (-100 / -100) = 20 + 20 = 40.
            Assert.AreEqual(hpBefore - 40, hpAfter,
                $"Cold-vulnerable Snapjaw must take DOUBLED 40 Cold damage " +
                $"(ColdResistance=-100). hpBefore={hpBefore}, hpAfter={hpAfter}, " +
                $"expected delta=40.");

            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = vulnerable.ID,
                Limit = 50,
            }).Records;

            Assert.AreEqual(1, damageRecords.Count,
                $"Cold-vulnerable Snapjaw must produce exactly one diag damage record. " +
                $"Got {damageRecords.Count}.");
            Assert.IsTrue(damageRecords[0].PayloadJson.Contains("\"amount\":40"),
                $"Phase E vulnerability: damage record must show amount=40 " +
                $"(20 base × 2 vulnerability multiplier). " +
                $"Payload: {damageRecords[0].PayloadJson}");
        }

        // ====================================================================
        // Phase E counter-check — Cold damage on a normal Snapjaw is NOT doubled
        // (proves doubling came from the resistance, not a global bug)
        // ====================================================================

        [Test]
        public void PhaseE_ColdOnNormalSnapjaw_DamageNotDoubled()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatParityShowcase().Apply(ctx);

            var normal = FindNormalSnapjaw(ctx);
            Assert.IsNotNull(normal, "Scenario must spawn at least one normal-stats Snapjaw.");

            int hpBefore = normal.GetStatValue("Hitpoints");
            Diag.ResetAll();

            var coldDamage = new Damage(20);
            coldDamage.AddAttribute("Cold");

            CombatSystem.ApplyDamage(normal, coldDamage, ctx.PlayerEntity, ctx.Zone);

            int hpAfter = normal.GetStatValue("Hitpoints");
            int hpDelta = hpBefore - hpAfter;

            // The Snapjaw blueprint has ColdResistance: 25 (Objects.json).
            // Resistance formula: damage.Amount = 20 * (100 - 25) / 100 = 15.
            // The PRIMARY contract being checked is "no doubling" — so we
            // assert hpDelta <= 20 (NOT 40). Pinning the exact 15 also
            // catches accidental regressions in the resistance formula.
            Assert.AreEqual(15, hpDelta,
                $"Normal Snapjaw with blueprint ColdResistance=25 must take " +
                $"15 damage (Cold 20 reduced 25%). hpBefore={hpBefore}, " +
                $"hpAfter={hpAfter}, hpDelta={hpDelta}. " +
                $"If 40, the doubling bug from cold-vulnerability is leaking.");

            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = normal.ID,
                Limit = 50,
            }).Records;

            Assert.IsTrue(damageRecords[0].PayloadJson.Contains("\"amount\":15"),
                $"Damage record must show amount=15 (Cold 20 × 0.75 resistance reduction; " +
                $"NOT doubled to 40). Payload: {damageRecords[0].PayloadJson}");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Finds the first "normal" soak Snapjaw — i.e., one of the 3
        /// the showcase spawns east of the player without modified
        /// resistance stats.
        ///
        /// IMPORTANT: the Snapjaw blueprint itself has
        /// <c>ColdResistance: 25</c> baked in (Objects.json), so we
        /// CAN'T filter by "no ColdResistance stat" — that would
        /// exclude every Snapjaw including the soak ones. Instead we
        /// filter by:
        ///   1. No HeatResistance stat (heat-immune adds one post-spawn).
        ///   2. ColdResistance.BaseValue != -100 (cold-vulnerable
        ///      overrides the blueprint default to -100).
        /// The 3 soak Snapjaws keep blueprint default ColdResistance=25
        /// and never have HeatResistance, so they pass.
        /// </summary>
        private static Entity FindNormalSnapjaw(CavesOfOoo.Scenarios.ScenarioContext ctx)
        {
            return ctx.Zone.GetAllEntities()
                .FirstOrDefault(e => e != null
                    && e != ctx.PlayerEntity
                    && e.BlueprintName == "Snapjaw"
                    && !e.Statistics.ContainsKey("HeatResistance")
                    && !(e.Statistics.ContainsKey("ColdResistance")
                         && e.Statistics["ColdResistance"].BaseValue == -100));
        }

        /// <summary>
        /// Finds the Snapjaw that has a specific resistance stat with the
        /// expected base value. Used to locate the heat-immune (HeatResistance=100)
        /// and cold-vulnerable (ColdResistance=-100) Snapjaws by their stats.
        /// </summary>
        private static Entity FindSnapjawByResistance(
            CavesOfOoo.Scenarios.ScenarioContext ctx,
            string statName,
            int expected)
        {
            return ctx.Zone.GetAllEntities()
                .FirstOrDefault(e => e != null
                    && e != ctx.PlayerEntity
                    && e.BlueprintName == "Snapjaw"
                    && e.Statistics.ContainsKey(statName)
                    && e.Statistics[statName].BaseValue == expected);
        }
    }
}
