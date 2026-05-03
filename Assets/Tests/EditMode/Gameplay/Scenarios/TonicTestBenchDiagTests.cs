using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// T1.5 verification for <see cref="TonicTestBench"/>.
    /// Pins three contracts:
    ///   1. The scenario places all 10 tonic blueprints on the floor
    ///      (catches a typo'd or removed blueprint silently dropping
    ///      a tonic from the bench)
    ///   2. Drinking BleedTonic from the bench emits one
    ///      effect/OnApply record with payload.effect = "BleedingEffect"
    ///      → end-to-end verification that the T1.2 dispatcher case +
    ///      blueprint + diag substrate all line up
    ///   3. Drinking CharredTonic does the same for "CharredEffect"
    ///      → end-to-end verification of the T1.3 sub-milestone
    ///   4. Counter-check: applying the scenario alone (no drinks)
    ///      produces ZERO effect/OnApply records (rules out the
    ///      scenario passively triggering tonic effects on apply)
    ///
    /// Pattern follows the 12+ prior scenario diag fixtures, with the
    /// drink mechanic mirroring BleedTonicTests / CharredTonicTests'
    /// FireApplyTonic helper (direct GameEvent fire vs going through
    /// ThrowItemCommand or pickup/drink input flow — matches the
    /// scenario's intended observation mechanism).
    /// </summary>
    [TestFixture]
    public class TonicTestBenchDiagTests
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
        // 1. All 10 tonics placed on the floor
        // ====================================================================

        [Test]
        public void Apply_PlacesAllTenTonicsOnFloor()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");

            new TonicTestBench().Apply(ctx);

            string[] expectedTonics =
            {
                "HealingTonic", "PoisonTonic", "FireTonic", "FrostTonic", "AcidTonic",
                "LightningTonic", "WaterTonic", "StoneskinTonic",
                "BleedTonic", "CharredTonic",
            };

            var spawned = ctx.Zone.GetAllEntities()
                .Select(e => e.BlueprintName)
                .ToList();

            // Pre-build the diagnostic blob outside the format string to
            // avoid escaped-quote nesting inside the interpolation.
            string tonicNames = string.Join(",",
                spawned.Where(n => n != null && n.Contains("Tonic")));

            foreach (var bp in expectedTonics)
            {
                Assert.IsTrue(spawned.Contains(bp),
                    $"Bench must place a {bp} on the floor. Got: [{tonicNames}]");
            }
        }

        // ====================================================================
        // 2. Drinking BleedTonic emits effect/OnApply for BleedingEffect
        //    → end-to-end verification of T1.2's dispatcher case + blueprint
        // ====================================================================

        [Test]
        public void DrinkBleedTonic_RecordsEffectOnApplyWithBleedingEffect()
        {
            VerifyDrinkEmitsOnApply(
                tonicBlueprint: "BleedTonic",
                expectedEffectTypeName: "BleedingEffect");
        }

        // ====================================================================
        // 3. Drinking CharredTonic emits effect/OnApply for CharredEffect
        //    → end-to-end verification of T1.3's dispatcher case + blueprint
        // ====================================================================

        [Test]
        public void DrinkCharredTonic_RecordsEffectOnApplyWithCharredEffect()
        {
            VerifyDrinkEmitsOnApply(
                tonicBlueprint: "CharredTonic",
                expectedEffectTypeName: "CharredEffect");
        }

        // ====================================================================
        // 4. Counter-check: scenario apply produces zero effect/OnApply
        //    records (rules out passive auto-drinking)
        // ====================================================================

        [Test]
        public void Apply_WithoutDrinks_ProducesNoEffectOnApplyRecords()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            Diag.ResetAll();

            new TonicTestBench().Apply(ctx);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Limit = 50,
            }).Records;

            Assert.AreEqual(0, records.Count,
                "Bench apply must NOT auto-drink any tonic. The scenario " +
                "places tonics on the floor; drinking is a manual player " +
                $"action. Got {records.Count} effect/OnApply records.");
        }

        // ====================================================================
        // Helper: find a tonic on the floor by blueprint, fire ApplyTonic
        // on it with the player as drinker, assert the diag record.
        // ====================================================================

        private void VerifyDrinkEmitsOnApply(
            string tonicBlueprint, string expectedEffectTypeName)
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new TonicTestBench().Apply(ctx);

            var tonic = ctx.Zone.GetAllEntities()
                .FirstOrDefault(e => e.BlueprintName == tonicBlueprint);
            Assert.IsNotNull(tonic,
                $"Bench must place {tonicBlueprint} on the floor.");

            // Reset diag so we observe ONLY the drink we're about to fire.
            Diag.ResetAll();

            // Fire ApplyTonic with the player as drinker (mirrors the
            // direct-event drink path used by BleedTonicTests / etc.).
            var e = GameEvent.New("ApplyTonic");
            e.SetParameter("Actor", (object)ctx.PlayerEntity);
            e.SetParameter("Source", (object)ctx.PlayerEntity);
            tonic.FireEvent(e);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Limit = 50,
            }).Records;

            Assert.GreaterOrEqual(records.Count, 1,
                $"Drinking {tonicBlueprint} must emit at least one " +
                $"effect/OnApply record. Got {records.Count}.");

            // Build the JSON-substring needle separately to avoid quote
            // nesting inside the interpolated assertion message.
            string needle = "\"effect\":\"" + expectedEffectTypeName + "\"";
            int matches = records.Count(r => r.PayloadJson.Contains(needle));
            string allPayloads = string.Join(";", records.Select(r => r.PayloadJson));
            Assert.AreEqual(1, matches,
                $"Drinking {tonicBlueprint} must emit exactly one " +
                $"effect/OnApply record with payload.effect = {expectedEffectTypeName}. " +
                $"Got {matches}. All records: [{allPayloads}]");
        }
    }
}
