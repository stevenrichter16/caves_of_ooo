using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BrewingService.TryBrewBatch / GetMaxBatchCount — the direct answer
    /// to "don't punish the player for gathering lots of one ingredient by
    /// only letting them make one item per action." Repeats the already-
    /// tested TryBrew against the SAME reagent selection; these tests pin
    /// the looping/exhaustion/idempotency contract, not the brew mechanics
    /// (those are BrewingServiceTests' job).
    /// </summary>
    public class BrewingBatchTests
    {
        private const string TestRulesJson = @"{
            ""Rules"": [
                { ""ID"":""r_burn"", ""RequireAll"":""heat combustible"", ""Effect"":""Burning"", ""Form"":""Coating"", ""Priority"":10 }
            ]
        }";

        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""item"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""BrewedTonic"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""strange brew"" } ] },
        { ""Name"": ""Tonic"", ""Params"": [ { ""Key"": ""Drink"", ""Value"": ""true"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""InertSludge"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""inert sludge"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""FireMoss"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""heat:2"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""LampOil"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""combustible:3"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""BlastPowder"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""volatile:2"" } ] } ],
      ""Stats"": [], ""Tags"": []
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.InitializeFromJson(TestRulesJson);
        }

        [TearDown]
        public void TearDown()
        {
            BrewRuleRegistry.ResetForTests();
            Diag.ResetAll();
        }

        private static EntityFactory CreateFactory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprintsJson);
            return factory;
        }

        private static Entity CreateCrafter()
        {
            var crafter = new Entity { ID = "crafter", BlueprintName = "Player" };
            crafter.AddPart(new RenderPart { DisplayName = "crafter" });
            crafter.AddPart(new InventoryPart());
            return crafter;
        }

        /// <summary>Creates one entity carrying a StackerPart with the given count (count==1 omits the stacker, matching how "one unstacked item" looks in practice).</summary>
        private static Entity GiveStacked(Entity crafter, EntityFactory factory, string blueprint, int count)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            if (count > 1)
                item.AddPart(new StackerPart { StackCount = count });
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static int StackOf(Entity item)
        {
            StackerPart stacker = item.GetPart<StackerPart>();
            return stacker != null ? stacker.StackCount : 1;
        }

        // ════════════════ Happy path: exact batch ════════════════

        [Test]
        public void Batch_SufficientReagents_MakesExactlyRequestedCount()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);
            var oil = GiveStacked(crafter, factory, "LampOil", 5);

            bool ok = BrewingService.TryBrewBatch(
                crafter, factory, new List<Entity> { moss, oil }, 3,
                out List<Entity> produced, out List<BrewResult> results, out int madeCount, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(3, madeCount);
            Assert.AreEqual(3, produced.Count);
            Assert.AreEqual(3, results.Count);
            foreach (var r in results)
                Assert.AreEqual(BrewOutcomeKind.Brew, r.Kind);

            Assert.AreEqual(2, StackOf(moss), "3 of 5 fire-moss consumed.");
            Assert.AreEqual(2, StackOf(oil), "3 of 5 lamp oil consumed.");
        }

        // ════════════════ Partial batch: the core anti-punishment guarantee ════════════════

        [Test]
        public void Batch_PartialWhenOneReagentRunsOutFirst_ReturnsTrueWithReducedCount()
        {
            // FireMoss has plenty; LampOil only has 2 — the batch must be
            // capped by the SCARCEST reagent, not rejected outright.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);
            var oil = GiveStacked(crafter, factory, "LampOil", 2);

            bool ok = BrewingService.TryBrewBatch(
                crafter, factory, new List<Entity> { moss, oil }, 5,
                out List<Entity> produced, out List<BrewResult> results, out int madeCount, out string reason);

            Assert.IsTrue(ok, "a partial batch is a smaller SUCCESS, not a failure.");
            Assert.AreEqual(2, madeCount, "capped by the scarcer reagent (lamp oil).");
            Assert.AreEqual(2, produced.Count);
            StringAssert.Contains("Ran out", reason);
            Assert.AreEqual(3, StackOf(moss), "the plentiful reagent stops being consumed once the batch stops.");
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(oil), "lamp oil is fully spent.");
        }

        [Test]
        public void Batch_ZeroAvailable_ReturnsFalse_MadeCountZero()
        {
            // Counter-check to the partial-batch test: if the FIRST
            // iteration fails, that's a real failure (not "made 0, still
            // success") — matches TryBrew's own contract exactly.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            Entity strayMoss = factory.CreateEntity("FireMoss"); // never added to inventory
            var oil = GiveStacked(crafter, factory, "LampOil", 3);

            bool ok = BrewingService.TryBrewBatch(
                crafter, factory, new List<Entity> { strayMoss, oil }, 5,
                out List<Entity> produced, out _, out int madeCount, out string reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, madeCount);
            Assert.AreEqual(0, produced.Count);
            StringAssert.Contains("own", reason);
            Assert.AreEqual(3, StackOf(oil), "nothing consumed when the batch never gets off the ground.");
        }

        // ════════════════ Requested-count validation ════════════════

        [Test]
        public void Batch_RequestedCountZeroOrNegative_RejectedImmediately_NothingConsumed()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 3);
            var oil = GiveStacked(crafter, factory, "LampOil", 3);

            foreach (int badCount in new[] { 0, -1, -5 })
            {
                bool ok = BrewingService.TryBrewBatch(
                    crafter, factory, new List<Entity> { moss, oil }, badCount,
                    out _, out _, out int madeCount, out string reason);

                Assert.IsFalse(ok, "count " + badCount + " must be rejected.");
                Assert.AreEqual(0, madeCount);
                Assert.IsNotEmpty(reason);
            }

            Assert.AreEqual(3, StackOf(moss), "rejected requests must consume nothing.");
            Assert.AreEqual(3, StackOf(oil));
        }

        // ════════════════ GetMaxBatchCount preview helper ════════════════

        [Test]
        public void GetMaxBatchCount_ReturnsSmallestAvailableAcrossReagents()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);
            var oil = GiveStacked(crafter, factory, "LampOil", 2);

            Assert.AreEqual(2, BrewingService.GetMaxBatchCount(new List<Entity> { moss, oil }));
        }

        [Test]
        public void GetMaxBatchCount_UnstackedItem_CountsAsOne()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 1); // no StackerPart attached
            var oil = GiveStacked(crafter, factory, "LampOil", 5);

            Assert.AreEqual(1, BrewingService.GetMaxBatchCount(new List<Entity> { moss, oil }));
        }

        [Test]
        public void GetMaxBatchCount_NullOrEmptySelection_ReturnsZero()
        {
            Assert.AreEqual(0, BrewingService.GetMaxBatchCount(null));
            Assert.AreEqual(0, BrewingService.GetMaxBatchCount(new List<Entity>()));
        }

        [Test]
        public void GetMaxBatchCount_NullEntryInSelection_ReturnsZero()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);

            Assert.AreEqual(0, BrewingService.GetMaxBatchCount(new List<Entity> { moss, null }));
        }

        [Test]
        public void GetMaxBatchCount_IsReadOnly_DoesNotConsumeAnything()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);
            var oil = GiveStacked(crafter, factory, "LampOil", 5);

            BrewingService.GetMaxBatchCount(new List<Entity> { moss, oil });

            Assert.AreEqual(5, StackOf(moss));
            Assert.AreEqual(5, StackOf(oil));
        }

        // ════════════════ Mishap outcomes repeat deterministically ════════════════

        [Test]
        public void Batch_AllMishapOutcomes_RepeatDeterministically_AllConsumed()
        {
            // BrewResolver is pure — the SAME reagent properties resolve to
            // the SAME outcome every time, so a batch of pure BlastPowder
            // (volatile, nothing to stabilize it) mishaps on every unit.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var powder = GiveStacked(crafter, factory, "BlastPowder", 3);

            bool ok = BrewingService.TryBrewBatch(
                crafter, factory, new List<Entity> { powder }, 3,
                out List<Entity> produced, out List<BrewResult> results, out int madeCount, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(3, madeCount);
            Assert.AreEqual(3, produced.Count);
            foreach (var item in produced)
                Assert.IsNull(item, "a mishap yields no item.");
            foreach (var r in results)
                Assert.AreEqual(BrewOutcomeKind.Mishap, r.Kind);
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(powder), "the whole stack is spent.");
        }

        // ════════════════ Discovery idempotency across a batch ════════════════

        [Test]
        public void Batch_IdenticalDiscoveries_FireExactlyOnceAcrossWholeBatch()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 3);
            var oil = GiveStacked(crafter, factory, "LampOil", 3);

            BrewingService.TryBrewBatch(
                crafter, factory, new List<Entity> { moss, oil }, 3,
                out _, out List<BrewResult> results, out int madeCount, out string reason);

            Assert.AreEqual(3, madeCount, reason);
            var knowledge = crafter.GetPart<BrewKnowledgePart>();
            Assert.IsNotNull(knowledge);
            Assert.AreEqual(1, knowledge.GetDiscoveredRules().Count,
                "3 identical brews in one batch must discover the rule ONCE, not 3 times.");

            var discovered = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy", Kind = "BrewDiscovered", Limit = 10
            }).Records;
            Assert.AreEqual(1, discovered.Count);
        }

        // ════════════════ Diag: per-unit granularity is the intended contract ════════════════

        [Test]
        public void Batch_EmitsOneBrewResolvedRecordPerIteration()
        {
            // Pin the deliberate design choice: batching does NOT collapse
            // diag records into one summary — each unit is independently
            // traceable via diag_query.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 3);
            var oil = GiveStacked(crafter, factory, "LampOil", 3);

            BrewingService.TryBrewBatch(
                crafter, factory, new List<Entity> { moss, oil }, 3,
                out _, out _, out int madeCount, out string reason);

            Assert.AreEqual(3, madeCount, reason);
            var resolved = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy", Kind = "BrewResolved", Limit = 10
            }).Records;
            Assert.AreEqual(3, resolved.Count);
        }
    }
}
