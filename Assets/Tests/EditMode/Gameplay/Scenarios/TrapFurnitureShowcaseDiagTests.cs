using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// End-to-end verification that <see cref="TrapFurnitureShowcase"/>
    /// is correctly wired: stepping onto each trap triggers the
    /// expected diag records (damage + status effects).
    ///
    /// Why this style: existing TrapFurnitureTests verify each trap's
    /// trigger payload in isolation. This test verifies the
    /// SCENARIO-level wiring — placement, ordering, and that all
    /// three traps in the corridor fire their hooks correctly when
    /// the player walks east.
    ///
    /// Ships use the diag substrate to confirm:
    ///   - SpikeTrap → damage/DamageDealt with Piercing attribute.
    ///   - FireTrap → damage/DamageDealt with Fire attribute +
    ///                effect/OnApply for BurningEffect.
    ///   - BearTrap → damage/DamageDealt with Piercing attribute +
    ///                effect/OnApply for StunnedEffect AND BleedingEffect.
    /// </summary>
    [TestFixture]
    public class TrapFurnitureShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1. Walking onto SpikeTrap fires damage record with Piercing
        // ====================================================================

        [Test]
        public void StepOnSpikeTrap_FiresPiercingDamageRecord()
        {
            var ctx = BuildScenario();
            var player = ctx.PlayerEntity;
            int hpBefore = player.GetStatValue("Hitpoints");
            Diag.ResetAll();

            // Walk east twice — scenario places SpikeTrap at p.x+2.
            MovementSystem.TryMove(player, ctx.Zone, dx: 1, dy: 0);
            MovementSystem.TryMove(player, ctx.Zone, dx: 1, dy: 0);

            int hpAfter = player.GetStatValue("Hitpoints");
            Assert.Less(hpAfter, hpBefore,
                "Player must have taken damage from SpikeTrap. " +
                $"hpBefore={hpBefore}, hpAfter={hpAfter}");

            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = player.ID,
                Limit = 50,
            }).Records;

            Assert.GreaterOrEqual(damageRecords.Count, 1,
                "SpikeTrap must produce at least one damage/DamageDealt record.");

            bool foundPiercing = damageRecords.Any(r => r.PayloadJson.Contains("Piercing"));
            Assert.IsTrue(foundPiercing,
                $"SpikeTrap damage must carry Piercing attribute. " +
                $"Sample payload: {damageRecords.First().PayloadJson}");
        }

        // ====================================================================
        // 2. Walking onto BearTrap applies Stunned + Bleeding via OnApply
        // ====================================================================

        [Test]
        public void StepOnBearTrap_AppliesStunnedAndBleeding()
        {
            var ctx = BuildScenario();
            var player = ctx.PlayerEntity;

            // BearTrap is at p.x+6 in the scenario. Walking east 6 cells
            // would also trigger the spike + fire traps in between, which
            // would give us partial state. Cleaner: spawn a fresh BearTrap
            // adjacent to the player and step onto it.
            //
            // Actually simpler: let the scenario set up all 3 traps as
            // designed, then walk east through them all. Verify that AT
            // LEAST the BearTrap-specific status effects (Stunned +
            // Bleeding) appear in the OnApply records by the end of the
            // walk. The other traps' effects (Burning from FireTrap)
            // will also be there but don't interfere with our assertion.
            for (int step = 0; step < 7; step++)
            {
                MovementSystem.TryMove(player, ctx.Zone, dx: 1, dy: 0);
                if (player.GetStatValue("Hitpoints") <= 0)
                    break;
            }

            var onApplyRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Target = player.ID,
                Limit = 50,
            }).Records;

            bool foundStunned = onApplyRecords.Any(r => r.PayloadJson.Contains("StunnedEffect"));
            bool foundBleeding = onApplyRecords.Any(r => r.PayloadJson.Contains("BleedingEffect"));

            Assert.IsTrue(foundStunned,
                $"BearTrap must apply StunnedEffect. " +
                $"OnApply records: [{string.Join(", ", onApplyRecords.Select(r => r.PayloadJson))}]");
            Assert.IsTrue(foundBleeding,
                $"BearTrap must apply BleedingEffect. " +
                $"OnApply records: [{string.Join(", ", onApplyRecords.Select(r => r.PayloadJson))}]");
        }

        // ====================================================================
        // 3. Walking onto FireTrap applies BurningEffect via OnApply
        // ====================================================================

        [Test]
        public void StepOnFireTrap_AppliesBurningEffect()
        {
            var ctx = BuildScenario();
            var player = ctx.PlayerEntity;

            // FireTrap is at p.x+4. Walk east through the corridor —
            // along the way the player triggers SpikeTrap (p.x+2) then
            // FireTrap (p.x+4).
            for (int step = 0; step < 5; step++)
            {
                MovementSystem.TryMove(player, ctx.Zone, dx: 1, dy: 0);
                if (player.GetStatValue("Hitpoints") <= 0)
                    break;
            }

            var onApplyRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Target = player.ID,
                Limit = 50,
            }).Records;

            bool foundBurning = onApplyRecords.Any(r => r.PayloadJson.Contains("BurningEffect"));
            Assert.IsTrue(foundBurning,
                $"FireTrap must apply BurningEffect. " +
                $"OnApply records: [{string.Join(", ", onApplyRecords.Select(r => r.PayloadJson))}]");

            // Counter-check: damage records from FireTrap carry Fire attribute.
            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Kind = "DamageDealt",
                Target = player.ID,
                Limit = 50,
            }).Records;

            bool foundFireDamage = damageRecords.Any(r => r.PayloadJson.Contains("Fire"));
            string samplePayload = damageRecords.Count > 0 ? damageRecords[0].PayloadJson : "<none>";
            Assert.IsTrue(foundFireDamage,
                $"FireTrap damage must carry Fire attribute. Sample payload: {samplePayload}");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Build the full TrapFurnitureShowcase scenario with the real
        /// Player blueprint. The stub player from BuildStubPlayer
        /// lacks the parts MovementSystem needs; the real Player
        /// blueprint carries all of them.
        /// </summary>
        private static CavesOfOoo.Scenarios.ScenarioContext BuildScenario()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new TrapFurnitureShowcase().Apply(ctx);
            return ctx;
        }
    }
}
