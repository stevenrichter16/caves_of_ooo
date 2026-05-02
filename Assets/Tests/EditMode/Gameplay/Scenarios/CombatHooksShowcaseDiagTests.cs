using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// End-to-end verification for <see cref="CombatHooksShowcase"/>
    /// — the scenario that demonstrates the Phase F/G/H combat hooks
    /// added in the parity port.
    ///
    /// The two pinnable contracts the scenario claims:
    ///
    ///   - **Phase F (BeforeTakeDamage)**: hits on the StoneSkin
    ///     Snapjaw run through <see cref="ShowcaseStoneSkinPart"/>,
    ///     which subtracts 2 from <see cref="Damage.Amount"/>. Hits
    ///     on the unmodified control Snapjaw take full damage.
    ///
    ///   - **Phase H (CanBeDismembered)**: dismember rolls that pass
    ///     the chance check on the Indestructible Snapjaw are vetoed
    ///     by <see cref="ShowcaseIndestructiblePart"/> — body parts
    ///     stay attached even with massive damage.
    ///
    /// Phase G (MultiWeaponSkillBonus) is a static stat assignment
    /// — already covered by ScenarioCustomSmokeTests' build check
    /// and not worth a dedicated end-to-end test.
    ///
    /// Pattern follows the OnHitEffectsShowcaseDiagTests fixture —
    /// real Player blueprint, Diag.ResetAll AFTER scenario setup,
    /// post-action diag_query / body-tree assertions.
    /// </summary>
    [TestFixture]
    public class CombatHooksShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // Phase F — StoneSkin reduces incoming damage by 2
        // ====================================================================

        [Test]
        public void StoneSkinSnapjaw_ReducesIncomingDamageByTwo()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatHooksShowcase().Apply(ctx);

            var stoneSkin = FindSnapjawWithPart<ShowcaseStoneSkinPart>(ctx);
            var control = FindControlSnapjaw(ctx);
            Assert.IsNotNull(stoneSkin, "Scenario must spawn a Snapjaw with ShowcaseStoneSkinPart.");
            Assert.IsNotNull(control, "Scenario must spawn a control Snapjaw with no showcase parts.");

            Diag.ResetAll();

            // Apply a fixed amount of damage to each via ApplyDamage. The
            // BeforeTakeDamage hook fires inside ApplyDamage and mutates
            // damage.Amount before HP is decremented; the diag DamageDealt
            // record then captures the post-mutation amount.
            //
            // Two separate Damage instances — StoneSkin's hook mutates the
            // .Amount field, so re-using the same object across both calls
            // would leak state.
            CombatSystem.ApplyDamage(stoneSkin, new Damage(10), ctx.PlayerEntity, ctx.Zone);
            CombatSystem.ApplyDamage(control, new Damage(10), ctx.PlayerEntity, ctx.Zone);

            var stoneRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = stoneSkin.ID,
                Limit = 5,
            }).Records;
            var controlRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = control.ID,
                Limit = 5,
            }).Records;

            Assert.AreEqual(1, stoneRecords.Count,
                $"StoneSkin must produce exactly one damage record. Got {stoneRecords.Count}.");
            Assert.AreEqual(1, controlRecords.Count,
                $"Control must produce exactly one damage record. Got {controlRecords.Count}.");

            // StoneSkin: 10 - 2 = 8 (the BeforeTakeDamage hook subtracted 2).
            Assert.IsTrue(stoneRecords[0].PayloadJson.Contains("\"amount\":8"),
                $"StoneSkin damage record must show amount=8 (10 - 2 reduction). " +
                $"Payload: {stoneRecords[0].PayloadJson}");
            // Control: full 10.
            Assert.IsTrue(controlRecords[0].PayloadJson.Contains("\"amount\":10"),
                $"Control damage record must show amount=10 (no reduction). " +
                $"Payload: {controlRecords[0].PayloadJson}");
        }

        // ====================================================================
        // Phase H — Indestructible Snapjaw never loses limbs (positive)
        // ====================================================================

        [Test]
        public void IndestructibleSnapjaw_NeverLosesLimbsAcrossManyRolls()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatHooksShowcase().Apply(ctx);

            var indestructible = FindSnapjawWithPart<ShowcaseIndestructiblePart>(ctx);
            Assert.IsNotNull(indestructible,
                "Scenario must spawn a Snapjaw with ShowcaseIndestructiblePart.");

            // Override max HP so a damage of 99 easily clears the
            // DISMEMBER_DAMAGE_THRESHOLD (0.25 * maxHP). At Max=100,
            // damage=99 → ratio=0.99, excess=0.74, chance=5+37=42 (capped at 50).
            // Without the veto, ~42% of seeds would dismember.
            indestructible.Statistics["Hitpoints"].Max = 100;
            indestructible.Statistics["Hitpoints"].BaseValue = 100;

            var body = indestructible.GetPart<Body>();
            Assert.IsNotNull(body, "Snapjaw must have a Body part.");

            var hitPart = body.GetParts()
                .FirstOrDefault(p => p.IsSeverable() && !p.Mortal);
            Assert.IsNotNull(hitPart,
                "Snapjaw body must contain at least one non-Mortal severable part " +
                "(arms, legs, etc.) for the dismember roll to land on.");

            int beforeCount = body.DismemberedParts.Count;
            Assert.AreEqual(0, beforeCount,
                "Sanity: a freshly-spawned Snapjaw should have zero dismembered parts.");

            // 100 seeded dismember-check calls. With chance=42 and the veto
            // disabled, we'd expect ~42 dismember attempts to succeed.
            // With the veto active, dismemberment must be exactly 0.
            for (int seed = 0; seed < 100; seed++)
            {
                CombatSystem.CheckCombatDismemberment(
                    indestructible, body, hitPart,
                    damage: 99, ctx.Zone, new Random(seed));
            }

            int afterCount = body.DismemberedParts.Count;
            Assert.AreEqual(0, afterCount,
                $"Indestructible Snapjaw must NEVER lose a limb — every " +
                $"CanBeDismembered event is vetoed by ShowcaseIndestructiblePart. " +
                $"DismemberedParts.Count went from {beforeCount} → {afterCount}. " +
                $"If non-zero, the veto path through FireEventAndRelease " +
                $"is broken or the showcase part isn't installed.");
        }

        // ====================================================================
        // Phase H counter-check — control Snapjaw DOES lose a limb
        //
        // Without this, the positive test would still pass even if a
        // hypothetical bug "dismemberment is silently disabled for ALL
        // entities" existed. The control proves the seeds-and-damage we
        // chose actually trigger dismembers when no veto is in place.
        // ====================================================================

        [Test]
        public void ControlSnapjaw_LosesAtLeastOneLimbAcrossManyRolls()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new CombatHooksShowcase().Apply(ctx);

            var control = FindControlSnapjaw(ctx);
            Assert.IsNotNull(control, "Scenario must spawn a control Snapjaw with no showcase parts.");

            control.Statistics["Hitpoints"].Max = 100;
            control.Statistics["Hitpoints"].BaseValue = 100;

            var body = control.GetPart<Body>();
            Assert.IsNotNull(body, "Snapjaw must have a Body part.");

            // Re-find the severable hit-target each loop: once a limb is
            // dismembered it leaves body.GetParts(), so we'd need a still-
            // attached candidate for subsequent rolls. (For our assertion
            // we only need ≥1, so practically the loop will hit success
            // long before running out of severable parts.)
            for (int seed = 0; seed < 100; seed++)
            {
                var hitPart = body.GetParts()
                    .FirstOrDefault(p => p.IsSeverable() && !p.Mortal);
                if (hitPart == null) break;  // body fully dismembered
                CombatSystem.CheckCombatDismemberment(
                    control, body, hitPart,
                    damage: 99, ctx.Zone, new Random(seed));
            }

            int finalDismemberCount = body.DismemberedParts.Count;
            Assert.GreaterOrEqual(finalDismemberCount, 1,
                $"Control (non-Indestructible) Snapjaw MUST lose at least one " +
                $"limb in 100 seeded rolls. Got {finalDismemberCount}. " +
                $"If 0, the test setup is broken (the rolls + threshold + chance " +
                $"never produce a successful dismember) — re-tune the damage / " +
                $"maxHP ratio so the chance roll lands.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Finds the first Snapjaw the scenario spawned that has
        /// <typeparamref name="TPart"/> attached. Used to locate the
        /// StoneSkin and Indestructible probes by their Part type.
        /// </summary>
        private static Entity FindSnapjawWithPart<TPart>(
            CavesOfOoo.Scenarios.ScenarioContext ctx) where TPart : Part
        {
            return ctx.Zone.GetAllEntities()
                .FirstOrDefault(e => e != null
                    && e != ctx.PlayerEntity
                    && e.BlueprintName == "Snapjaw"
                    && e.GetPart<TPart>() != null);
        }

        /// <summary>
        /// Finds the control Snapjaw — the one with NO showcase parts.
        /// The scenario spawns three Snapjaws: StoneSkin (NW), control
        /// (E), Indestructible (NE). The control is identified by the
        /// absence of ShowcaseStoneSkinPart and ShowcaseIndestructiblePart.
        /// </summary>
        private static Entity FindControlSnapjaw(CavesOfOoo.Scenarios.ScenarioContext ctx)
        {
            return ctx.Zone.GetAllEntities()
                .FirstOrDefault(e => e != null
                    && e != ctx.PlayerEntity
                    && e.BlueprintName == "Snapjaw"
                    && e.GetPart<ShowcaseStoneSkinPart>() == null
                    && e.GetPart<ShowcaseIndestructiblePart>() == null);
        }
    }
}
