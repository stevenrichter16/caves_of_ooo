using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BrewReagentsCommand's batch surface (the `count` constructor param).
    /// Parallel to BrewReagentsCommandTests.cs (not modifying that file) —
    /// covers still-gating-applies-to-the-whole-batch, cumulative mishap
    /// damage across a batch, and backward compatibility with the old
    /// 2-arg constructor call sites.
    /// </summary>
    public class BrewReagentsBatchCommandTests
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
    },
    {
      ""Name"": ""AlchemyStill"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""alchemy still"" } ] },
        { ""Name"": ""AlchemyStill"", ""Params"": [] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Furniture"", ""Value"": """" } ]
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.InitializeFromJson(TestRulesJson);
        }

        [TearDown]
        public void TearDown()
        {
            BrewRuleRegistry.ResetForTests();
        }

        private static EntityFactory CreateFactory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprintsJson);
            return factory;
        }

        private static Entity CreateCrafter(int hp = 20)
        {
            var crafter = new Entity { ID = "crafter", BlueprintName = "Player" };
            crafter.Statistics["Hitpoints"] = new Stat
            {
                Owner = crafter, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = 20
            };
            crafter.AddPart(new RenderPart { DisplayName = "crafter" });
            crafter.AddPart(new InventoryPart());
            return crafter;
        }

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

        private static Zone MakeZone(Entity crafter, EntityFactory factory, int? stillDx = null, int? stillDy = null)
        {
            var zone = new Zone("BrewBatchTestZone");
            Assert.IsTrue(zone.AddEntity(crafter, 5, 5));

            if (stillDx.HasValue && stillDy.HasValue)
            {
                Entity still = factory.CreateEntity("AlchemyStill");
                Assert.IsNotNull(still);
                Assert.IsTrue(zone.AddEntity(still, 5 + stillDx.Value, 5 + stillDy.Value));
            }

            return zone;
        }

        private static InventoryCommandResult Run(
            Entity crafter, EntityFactory factory, Zone zone, int count, params Entity[] reagents)
        {
            var command = new BrewReagentsCommand(new List<Entity>(reagents), factory, count);
            return InventorySystem.ExecuteCommand(command, crafter, zone);
        }

        private static bool AnyMessageContains(string substring)
        {
            foreach (string message in MessageLog.GetMessages())
            {
                if (message.Contains(substring))
                    return true;
            }

            return false;
        }

        private static int ReceiptCount(BrewReagentsCommand command, string name)
        {
            var property = typeof(BrewReagentsCommand).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, "The completed command must expose its actual last-execution " + name + " receipt.");
            Assert.AreEqual(typeof(int), property.PropertyType);
            return (int)property.GetValue(command);
        }

        private static void AssertReceipt(BrewReagentsCommand command, int completed, int brewed, int sludge, int mishaps)
        {
            Assert.AreEqual(completed, ReceiptCount(command, "CompletedCount"));
            Assert.AreEqual(brewed, ReceiptCount(command, "BrewCount"));
            Assert.AreEqual(sludge, ReceiptCount(command, "SludgeCount"));
            Assert.AreEqual(mishaps, ReceiptCount(command, "MishapCount"));
        }

        [TestCase(BrewOutcomeKind.Brew)]
        [TestCase(BrewOutcomeKind.InertSludge)]
        [TestCase(BrewOutcomeKind.Mishap)]
        public void LastExecutionReceiptCountsActualOutcomeUnitsInsteadOfRequestedBatch(BrewOutcomeKind outcome)
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var reagents = new List<Entity>();
            reagents.Add(GiveStacked(crafter, factory, outcome == BrewOutcomeKind.Mishap ? "BlastPowder" : "FireMoss", 2));
            if (outcome == BrewOutcomeKind.Brew) reagents.Add(GiveStacked(crafter, factory, "LampOil", 2));
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);
            var command = new BrewReagentsCommand(reagents, factory, count: 3);

            var result = InventorySystem.ExecuteCommand(command, crafter, zone);

            Assert.True(result.Success, "Two genuine committed attempts remain a partial success: " + result.ErrorMessage);
            var pack = crafter.GetPart<InventoryPart>();
            foreach (var reagent in reagents) Assert.False(pack.Contains(reagent), "Both available reagent units were consumed.");
            int brewedUnits = 0, sludgeUnits = 0;
            foreach (var item in pack.Objects)
            {
                if (item.BlueprintName == "BrewedTonic") brewedUnits += StackOf(item);
                else if (item.BlueprintName == "InertSludge") sludgeUnits += StackOf(item);
                else Assert.Fail("Unexpected output owner: " + item.BlueprintName);
            }
            Assert.AreEqual(outcome == BrewOutcomeKind.Brew ? 2 : 0, brewedUnits);
            Assert.AreEqual(outcome == BrewOutcomeKind.InertSludge ? 2 : 0, sludgeUnits);
            Assert.AreEqual(outcome == BrewOutcomeKind.Mishap ? 16 : 20, crafter.GetStat("Hitpoints").Value,
                "A mishap is a completed attempt with damage and no item; ordinary outputs must not damage the actor.");
            AssertReceipt(command, 2, brewedUnits, sludgeUnits, outcome == BrewOutcomeKind.Mishap ? 2 : 0);
        }

        [Test]
        public void ReusedCommandClearsItsLastExecutionReceiptWhenNoStockRemains()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 1);
            var oil = GiveStacked(crafter, factory, "LampOil", 1);
            var zone = MakeZone(crafter, factory);
            var command = new BrewReagentsCommand(new[] { moss, oil }, factory);
            Assert.True(InventorySystem.ExecuteCommand(command, crafter, zone).Success);
            AssertReceipt(command, 1, 1, 0, 0);
            var pack = crafter.GetPart<InventoryPart>();
            Assert.AreEqual(1, pack.Objects.Count); Entity output = pack.Objects[0];
            Assert.AreEqual("BrewedTonic", output.BlueprintName); Assert.AreEqual(1, StackOf(output));
            Assert.False(pack.Contains(moss)); Assert.False(pack.Contains(oil));

            var repeated = InventorySystem.ExecuteCommand(command, crafter, zone);

            Assert.False(repeated.Success);
            Assert.AreEqual(InventoryCommandErrorCode.ExecutionFailed, repeated.ErrorCode,
                "This must reach Execute and fail for exhausted stock, not stop at a different validation gate.");
            AssertReceipt(command, 0, 0, 0, 0);
            Assert.AreEqual(1, pack.Objects.Count); Assert.AreSame(output, pack.Objects[0]);
            Assert.AreEqual(1, StackOf(output)); Assert.AreEqual(20, crafter.GetStat("Hitpoints").Value);
        }

        // ════════════════ Batch through the command surface ════════════════

        [Test]
        public void Batch_AtStill_MakesRequestedCount_ConsumesStacks()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 4);
            var oil = GiveStacked(crafter, factory, "LampOil", 4);
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone, 3, moss, oil);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(1, StackOf(moss));
            Assert.AreEqual(1, StackOf(oil));
            Assert.IsTrue(AnyMessageContains("batch of 3"));
        }

        [Test]
        public void Batch_PartialDueToStackLimit_StillReturnsSuccess_ReportsShortfall()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);
            var oil = GiveStacked(crafter, factory, "LampOil", 2);
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone, 5, moss, oil);

            Assert.IsTrue(result.Success, "a partial batch is not a command failure.");
            Assert.IsTrue(AnyMessageContains("Requested 5, made 2"));
        }

        [Test]
        public void Batch_StillGatingAppliesToWholeBatch_NotJustFirstUnit()
        {
            // The still requirement is checked ONCE at Validate time (the
            // resolved form is deterministic and count-independent) — no
            // still means the WHOLE batch is refused, not just the units
            // beyond the first.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 5);
            var oil = GiveStacked(crafter, factory, "LampOil", 5);
            var zone = MakeZone(crafter, factory); // no still

            var result = Run(crafter, factory, zone, 3, moss, oil);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("still", result.ErrorMessage);
            Assert.AreEqual(5, StackOf(moss), "a rejected batch consumes nothing at all.");
            Assert.AreEqual(5, StackOf(oil));
        }

        [Test]
        public void Batch_RequestedCountZero_RejectedAtValidation_NothingConsumed()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 3);
            var oil = GiveStacked(crafter, factory, "LampOil", 3);
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var command = new BrewReagentsCommand(new List<Entity> { moss, oil }, factory, count: 0);
            var result = InventorySystem.ExecuteCommand(command, crafter, zone);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, StackOf(moss));
        }

        // ════════════════ Cumulative mishap damage — the sharp edge ════════════════

        [Test]
        public void Batch_MultipleMishaps_DamageAppliesPerIteration_NeverLethal()
        {
            // 3 mishap iterations from a stack of 3 blast powder, each
            // capped at MishapDamageMax=2 and each floored at 1 HP by
            // re-reading current HP — starting at 4 HP, this must NOT go
            // below 1 even though 3 * 2 = 6 would exceed it.
            var factory = CreateFactory();
            var crafter = CreateCrafter(hp: 4);
            var powder = GiveStacked(crafter, factory, "BlastPowder", 3);
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone, 3, powder);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.GreaterOrEqual(crafter.GetStat("Hitpoints").Value, 1,
                "cumulative mishap damage across a batch must never be lethal.");
        }

        [Test]
        public void Batch_NoMishaps_DealsNoDamage()
        {
            // Counter-check: a batch that resolves to real brews (no
            // mishap outcomes) must not touch the crafter's HP at all.
            var factory = CreateFactory();
            var crafter = CreateCrafter(hp: 20);
            var moss = GiveStacked(crafter, factory, "FireMoss", 3);
            var oil = GiveStacked(crafter, factory, "LampOil", 3);
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            Run(crafter, factory, zone, 3, moss, oil);

            Assert.AreEqual(20, crafter.GetStat("Hitpoints").Value);
        }

        // ════════════════ Backward compatibility ════════════════

        [Test]
        public void TwoArgConstructor_DefaultsToCountOne_BehavesLikeSingleBrew()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveStacked(crafter, factory, "FireMoss", 3);
            var oil = GiveStacked(crafter, factory, "LampOil", 3);
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var command = new BrewReagentsCommand(new List<Entity> { moss, oil }, factory);
            var result = InventorySystem.ExecuteCommand(command, crafter, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(2, StackOf(moss), "the old 2-arg constructor must still make exactly one.");
            Assert.AreEqual(2, StackOf(oil));
        }
    }
}
