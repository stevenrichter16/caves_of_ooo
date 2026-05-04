using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// End-to-end verification for the four elemental melee-weapon
    /// showcases (AcidicDagger, ThunderHammer, EmberSpear, CryoLance).
    ///
    /// Each weapon has two declared contracts that this fixture pins
    /// at the scenario level:
    ///
    ///   1. Damage carries the elemental attribute (per the weapon
    ///      blueprint's MeleeWeapon.Attributes string).
    ///      AcidicDagger      "Piercing Acid"
    ///      ThunderHammer     "Bludgeoning Lightning Cudgel"
    ///      EmberSpear        "Piercing Fire"
    ///      CryoLance         "Piercing Ice LongBlades"
    ///
    ///   2. Per-weapon on-hit hook applies the matching status
    ///      effect at ~30% per hit (per OnHitEffectsRaw config).
    ///      AcidicDagger      → AcidicEffect
    ///      ThunderHammer     → ElectrifiedEffect
    ///      EmberSpear        → BurningEffect
    ///      CryoLance         → FrozenEffect
    ///
    /// (1) is a single-swing assertion; (2) needs ~100 seeded swings
    /// for the 30% chance to trigger with effectively-1 probability:
    /// (1 - 0.30)^100 ≈ 3.2e-16.
    ///
    /// Pattern follows the OnHitEffectsShowcaseDiagTests fixture —
    /// real Player blueprint (not stub), Diag.ResetAll AFTER scenario
    /// setup, seeded RNG loop driving PerformMeleeAttack, post-loop
    /// diag_query assertions.
    /// </summary>
    [TestFixture]
    public class ElementalWeaponShowcasesDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // AcidicDagger — Piercing + Acid → per-weapon AcidicEffect
        // ====================================================================

        [Test]
        public void AcidicDaggerShowcase_DamageCarriesAcid_AndAppliesAcidicEffect()
        {
            VerifyWeapon<AcidicDaggerShowcase>(
                expectedDamageAttribute: "Acid",
                expectedEffectTypeName: "AcidicEffect");
        }

        // ====================================================================
        // ThunderHammer — Bludgeoning + Lightning → per-weapon ElectrifiedEffect
        // ====================================================================

        [Test]
        public void ThunderHammerShowcase_DamageCarriesLightning_AndAppliesElectrified()
        {
            VerifyWeapon<ThunderHammerShowcase>(
                expectedDamageAttribute: "Lightning",
                expectedEffectTypeName: "ElectrifiedEffect");
        }

        // ====================================================================
        // EmberSpear — Piercing + Fire → per-weapon BurningEffect
        // ====================================================================

        [Test]
        public void EmberSpearShowcase_DamageCarriesFire_AndAppliesBurning()
        {
            VerifyWeapon<EmberSpearShowcase>(
                expectedDamageAttribute: "Fire",
                expectedEffectTypeName: "BurningEffect");
        }

        // ====================================================================
        // CryoLance — Piercing + Ice → per-weapon FrozenEffect
        // ====================================================================

        [Test]
        public void CryoLanceShowcase_DamageCarriesIce_AndAppliesFrozen()
        {
            VerifyWeapon<CryoLanceShowcase>(
                expectedDamageAttribute: "Ice",
                expectedEffectTypeName: "FrozenEffect");
        }

        // ====================================================================
        // FlamingSwordShowcase — equips FlamingSword (same Fire+Burning
        // contract as covered for FlamingSword in OnHitEffectsShowcase),
        // but the scenario sets up a different target lineup (Glowmaw
        // HR=50 vs Snapjaw control). Verifying this scenario as its
        // own ship.
        // ====================================================================

        [Test]
        public void FlamingSwordShowcase_DamageCarriesFire_AndAppliesBurning()
        {
            VerifyWeapon<FlamingSwordShowcase>(
                expectedDamageAttribute: "Fire",
                expectedEffectTypeName: "BurningEffect");
        }

        // ====================================================================
        // ElementalSwordsShowcase — equips FlamingSword, gives IceSword
        // in inventory. Default-equipped weapon is FlamingSword, so the
        // verify path tests Fire+Burning on this scenario too. (IceSword
        // would require unequip-then-equip which has its own quirk —
        // see OnHitEffectsShowcaseDiagTests for that workaround.)
        // ====================================================================

        [Test]
        public void ElementalSwordsShowcase_DamageCarriesFire_AndAppliesBurning()
        {
            VerifyWeapon<ElementalSwordsShowcase>(
                expectedDamageAttribute: "Fire",
                expectedEffectTypeName: "BurningEffect");
        }

        // ====================================================================
        // Counter-check: non-elemental weapon (Mace) does NOT cross-pollinate
        // — should not produce ANY of the elemental per-weapon effects.
        //
        // Without this, all the positive tests would still pass even if a
        // hypothetical bug "every swing applies every effect type" existed.
        // ====================================================================

        [Test]
        public void MaceSwing_DoesNotApplyAnyElementalPerWeaponEffect()
        {
            // Mace is bludgeoning + cudgel — no elemental attributes,
            // no per-weapon OnHitEffectsRaw. Should never apply Burning,
            // Frozen, Electrified, or Acidic effects.
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new OnHitEffectsShowcase().Apply(ctx);  // equips Mace

            var hostiles = AllHostileNonPlayers(ctx).ToList();
            foreach (var h in hostiles)
            {
                h.Statistics["Hitpoints"].BaseValue = 1000;
                h.Statistics["Hitpoints"].Max = 1000;
            }

            Diag.ResetAll();
            for (int seed = 0; seed < 100; seed++)
            {
                foreach (var h in hostiles)
                    if (h.GetStatValue("Hitpoints") > 0)
                        CombatSystem.PerformMeleeAttack(
                            ctx.PlayerEntity, h, ctx.Zone, new Random(seed));
            }

            var onApplyRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Limit = 500,
            }).Records;

            string[] elementalEffects =
            {
                "BurningEffect", "FrozenEffect",
                "ElectrifiedEffect", "AcidicEffect"
            };
            foreach (var eff in elementalEffects)
            {
                int matches = onApplyRecords.Count(r => r.PayloadJson.Contains(eff));
                Assert.AreEqual(0, matches,
                    $"Mace (no elemental attributes) must not apply {eff} via " +
                    $"the on-hit chain. Got {matches} OnApply records. " +
                    $"If non-zero, the per-weapon hook is firing on weapons " +
                    $"that didn't declare the effect — cross-pollination bug.");
            }
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Generic verification: build the scenario, find its primary
        /// hostile target, swing the equipped weapon up to 100 times
        /// with seeded RNG, then assert:
        ///   - At least one damage/DamageDealt record has the expected
        ///     elemental attribute.
        ///   - At least one effect/OnApply record has the expected
        ///     per-weapon effect type.
        ///
        /// Stops mid-loop if the defender dies (further swings would
        /// be no-ops on a corpse).
        /// </summary>
        private void VerifyWeapon<TScenario>(
            string expectedDamageAttribute,
            string expectedEffectTypeName)
            where TScenario : IScenario, new()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new TScenario().Apply(ctx);

            // CryoLance / EmberSpear scenarios deliberately put a
            // FULLY-IMMUNE target first (IceWight CR=100, CharredHusk HR=100).
            // Cold/Fire damage on those resolves to amount=0 and ApplyDamage
            // returns BEFORE the diag hook fires. The scenario's intent is
            // to demonstrate resistance interaction, not to be a damage-test
            // baseline.
            //
            // Iterate ALL hostile creatures, swing each, then aggregate diag
            // records across all of them for the assertion. As long as ONE
            // target took damage with the expected attribute and ONE got
            // the per-weapon effect applied, the weapon is wired correctly.
            var hostiles = AllHostileNonPlayers(ctx).ToList();
            Assert.IsNotEmpty(hostiles,
                $"{typeof(TScenario).Name} must spawn at least one hostile target.");

            Diag.ResetAll();

            // Pad each hostile's HP so the test loop runs to completion
            // even if a weapon would otherwise one-shot the default 100 HP.
            foreach (var h in hostiles)
            {
                h.Statistics["Hitpoints"].BaseValue = 1000;
                h.Statistics["Hitpoints"].Max = 1000;
            }

            for (int seed = 0; seed < 100; seed++)
            {
                bool anyAlive = false;
                foreach (var h in hostiles)
                {
                    if (h.GetStatValue("Hitpoints") <= 0) continue;
                    CombatSystem.PerformMeleeAttack(
                        ctx.PlayerEntity, h, ctx.Zone, new Random(seed));
                    anyAlive = true;
                }
                if (!anyAlive) break;
            }

            // Damage attribute assertion (across all hostiles).
            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Limit = 500,
            }).Records;

            Assert.GreaterOrEqual(damageRecords.Count, 1,
                $"{typeof(TScenario).Name}: expected at least one damage record " +
                $"after swinging at {hostiles.Count} hostile(s) × 100 seeded swings.");

            bool foundAttr = damageRecords.Any(r =>
                r.PayloadJson.Contains(expectedDamageAttribute));
            Assert.IsTrue(foundAttr,
                $"{typeof(TScenario).Name}: damage records must carry '{expectedDamageAttribute}' " +
                $"attribute. Sample: {damageRecords[0].PayloadJson}");

            // Per-weapon effect assertion (across all hostiles).
            var onApplyRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Limit = 500,
            }).Records;

            int matching = onApplyRecords.Count(r =>
                r.PayloadJson.Contains(expectedEffectTypeName));
            Assert.GreaterOrEqual(matching, 1,
                $"{typeof(TScenario).Name}: per-weapon hook must apply " +
                $"'{expectedEffectTypeName}' at least once across all hostiles " +
                $"in 100 seeded swings (30% per hit). Got {matching} matches. " +
                $"All OnApply payloads: [{string.Join(", ", onApplyRecords.Select(r => r.PayloadJson))}]");
        }

        /// <summary>
        /// Returns every hostile non-player creature in the scenario's
        /// zone. The elemental-weapon showcases tend to spawn 3-4
        /// creatures with varied resistances (immune control, graded,
        /// vulnerable); aggregating across all of them sidesteps
        /// "this scenario put an immune creature first" gotchas.
        /// </summary>
        private static System.Collections.Generic.IEnumerable<Entity> AllHostileNonPlayers(
            ScenarioContext ctx)
        {
            return ctx.Zone.GetAllEntities()
                .Where(e => e != null
                    && e != ctx.PlayerEntity
                    && e.Statistics.ContainsKey("Hitpoints")
                    && e.GetPart<StatusEffectsPart>() != null
                        // Restrict to "creatures" (have effect parts) — skip
                        // furniture / props that have HP but aren't combat
                        // targets.
                        || (e != null && e != ctx.PlayerEntity && e.HasTag("Creature")));
        }
    }
}
