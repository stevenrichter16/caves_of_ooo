using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// End-to-end verification for <see cref="ThrowableTonicsShowcase"/>
    /// — the demo for the throwable-consumables mechanic.
    ///
    /// What's being pinned at the scenario level:
    ///
    ///   1. Each elemental tonic the showcase puts in player inventory
    ///      shatters on impact at the snapjaw cluster center and applies
    ///      its expected status effect to ≥1 cluster Snapjaw via the
    ///      AOE-radius-1 splash.
    ///         AcidTonic       → AcidicEffect
    ///         FrostTonic      → FrozenEffect
    ///         LightningTonic  → ElectrifiedEffect
    ///         FireTonic       → BurningEffect
    ///   2. Counter-check: HealingTonic does NOT apply any of the
    ///      elemental status effects to cluster Snapjaws (it has no
    ///      throwable status payload — the AOE produces zero status
    ///      effect/OnApply records).
    ///
    /// Throws are driven via <see cref="ThrowItemCommand"/> + the
    /// existing <see cref="InventorySystem.ExecuteCommand"/> pipeline,
    /// matching the pattern in CombatContentAdversarialTests' throw
    /// tests (the canonical existing throw harness).
    ///
    /// Why this style: existing TonicTests verify each tonic's
    /// status payload in isolation. This fixture verifies the
    /// scenario-level wiring — the showcase's player inventory + the
    /// snapjaw cluster + the shatter-on-impact AOE all line up so a
    /// throw produces the right effect on the cluster.
    /// </summary>
    [TestFixture]
    public class ThrowableTonicsShowcaseDiagTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 1-4. Each elemental tonic applies its expected effect on the cluster
        // ====================================================================

        [Test]
        public void AcidTonic_ThrownAtCluster_AppliesAcidicEffect()
        {
            VerifyTonic(blueprintName: "AcidTonic", expectedEffectTypeName: "AcidicEffect");
        }

        [Test]
        public void FrostTonic_ThrownAtCluster_AppliesFrozenEffect()
        {
            VerifyTonic(blueprintName: "FrostTonic", expectedEffectTypeName: "FrozenEffect");
        }

        [Test]
        public void LightningTonic_ThrownAtCluster_AppliesElectrifiedEffect()
        {
            VerifyTonic(blueprintName: "LightningTonic", expectedEffectTypeName: "ElectrifiedEffect");
        }

        [Test]
        public void FireTonic_ThrownAtCluster_AppliesBurningEffect()
        {
            VerifyTonic(blueprintName: "FireTonic", expectedEffectTypeName: "BurningEffect");
        }

        // ====================================================================
        // 5. Counter-check: HealingTonic produces NO elemental effects
        //
        // Without this, a hypothetical bug "every throw applies every effect"
        // would still pass the 4 positive tests above.
        // ====================================================================

        [Test]
        public void HealingTonic_ThrownAtCluster_AppliesNoElementalEffects()
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new ThrowableTonicsShowcase().Apply(ctx);

            var tonic = FindTonicInInventory(ctx, "HealingTonic");
            Assert.IsNotNull(tonic, "Showcase must put HealingTonic in player inventory.");

            var (cx, cy) = GetClusterCenter(ctx);

            Diag.ResetAll();

            var throwCmd = new ThrowItemCommand(tonic, cx, cy);
            var result = InventorySystem.ExecuteCommand(throwCmd, ctx.PlayerEntity, ctx.Zone);
            Assert.IsTrue(result.Success,
                $"Throw must validate + execute. Failure: {result.ErrorMessage}");

            var onApplyRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Limit = 200,
            }).Records;

            string[] elementalEffects =
            {
                "AcidicEffect", "FrozenEffect",
                "ElectrifiedEffect", "BurningEffect"
            };
            foreach (var eff in elementalEffects)
            {
                int matches = onApplyRecords.Count(r => r.PayloadJson.Contains(eff));
                Assert.AreEqual(0, matches,
                    $"HealingTonic AOE must not apply {eff} (no throwable status payload). " +
                    $"Got {matches} OnApply records. " +
                    $"If non-zero, the AOE pipeline is mis-routing effect types — " +
                    $"cross-pollination bug or HealingTonic blueprint regressed.");
            }
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Build the showcase, find the tonic of the requested blueprint
        /// in player inventory, throw it at the cluster center, then
        /// assert that ≥1 effect/OnApply record matching
        /// <paramref name="expectedEffectTypeName"/> appears in the diag
        /// substrate.
        ///
        /// Match-by-payload-Contains because the OnApply record's
        /// payload includes the effect's type-name string.
        /// </summary>
        private void VerifyTonic(string blueprintName, string expectedEffectTypeName)
        {
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new ThrowableTonicsShowcase().Apply(ctx);

            var tonic = FindTonicInInventory(ctx, blueprintName);
            Assert.IsNotNull(tonic,
                $"Showcase must put {blueprintName} in player inventory.");

            var (cx, cy) = GetClusterCenter(ctx);

            Diag.ResetAll();

            var throwCmd = new ThrowItemCommand(tonic, cx, cy);
            var result = InventorySystem.ExecuteCommand(throwCmd, ctx.PlayerEntity, ctx.Zone);
            Assert.IsTrue(result.Success,
                $"{blueprintName} throw at cluster center ({cx},{cy}) must validate + " +
                $"execute. Failure: {result.ErrorMessage}");

            var onApplyRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Kind = "OnApply",
                Limit = 200,
            }).Records;

            int matching = onApplyRecords.Count(r => r.PayloadJson.Contains(expectedEffectTypeName));
            Assert.GreaterOrEqual(matching, 1,
                $"{blueprintName}: must apply '{expectedEffectTypeName}' to at least " +
                $"one cluster Snapjaw via the AOE shatter. Got {matching} matching OnApply " +
                $"records. All OnApply payloads: " +
                $"[{string.Join(", ", onApplyRecords.Select(r => r.PayloadJson))}]");
        }

        /// <summary>
        /// Returns the cluster center position the scenario uses: 4 east
        /// of the player on the same row. The 5-snapjaw cluster is in
        /// a 3×3 around this point, so a thrown tonic lands its AOE
        /// (radius 1) entirely over the cluster.
        /// </summary>
        private static (int cx, int cy) GetClusterCenter(CavesOfOoo.Scenarios.ScenarioContext ctx)
        {
            var p = ctx.Zone.GetEntityPosition(ctx.PlayerEntity);
            return (p.x + 4, p.y);
        }

        /// <summary>
        /// Finds the first inventory item whose BlueprintName matches the
        /// requested name. The showcase gives the player 5 of each
        /// elemental tonic plus 5 HealingTonics — the first match is a
        /// representative pick. Scanning the InventoryPart's Objects
        /// list directly bypasses any stacker layering.
        /// </summary>
        private static Entity FindTonicInInventory(
            CavesOfOoo.Scenarios.ScenarioContext ctx, string blueprintName)
        {
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            if (inv == null) return null;
            return inv.Objects.FirstOrDefault(e =>
                e != null && e.BlueprintName == blueprintName);
        }
    }
}
