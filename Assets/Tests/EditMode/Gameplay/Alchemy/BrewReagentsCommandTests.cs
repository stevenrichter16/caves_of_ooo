using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BrewReagentsCommand — the player-facing brew surface. Covers the two
    /// command-layer rules the service deliberately doesn't own:
    /// still-adjacency gating (§6.2) and non-lethal mishap self-damage
    /// (§6.3). Uses production blueprints via the reagent fixtures below to
    /// also smoke the shipped ReagentItem content.
    /// </summary>
    public class BrewReagentsCommandTests
    {
        private const string TestRulesJson = @"{
            ""Rules"": [
                { ""ID"":""r_burn"", ""RequireAll"":""heat combustible"", ""Effect"":""Burning"", ""Form"":""Coating"", ""Priority"":10 },
                { ""ID"":""r_snack"", ""RequireAll"":""sweet"", ""Effect"":""Healing"", ""Form"":""Food"", ""Priority"":1 }
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
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""BrewedTonic"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""strange brew"" } ] },
        { ""Name"": ""Tonic"", ""Params"": [ { ""Key"": ""Drink"", ""Value"": ""true"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""InertSludge"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""inert sludge"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""FireMoss"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""heat:2"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""LampOil"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""combustible:3"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""BlastPowder"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""volatile:2"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
    },
    {
      ""Name"": ""SugarLump"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""sweet:1"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": []
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

        private static Entity GiveItem(Entity crafter, EntityFactory factory, string blueprint)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        /// <summary>Zone with the crafter at (5,5); optionally a still at the given offset.</summary>
        private static Zone MakeZone(Entity crafter, EntityFactory factory, int? stillDx = null, int? stillDy = null)
        {
            var zone = new Zone("BrewTestZone");
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
            Entity crafter, EntityFactory factory, Zone zone, params Entity[] reagents)
        {
            var command = new BrewReagentsCommand(new List<Entity>(reagents), factory);
            return InventorySystem.ExecuteCommand(command, crafter, zone);
        }

        // ════════════════ Still gating (§6.2) ════════════════

        [Test]
        public void AdjacentToStill_BrewSucceeds()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone, moss, oil);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(moss),
                "the brew executed, consuming the reagents.");
        }

        [Test]
        public void DiagonalStill_CountsAsAdjacent()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");
            var zone = MakeZone(crafter, factory, stillDx: -1, stillDy: -1);

            Assert.IsTrue(Run(crafter, factory, zone, moss, oil).Success);
        }

        [Test]
        public void NoStill_NonFoodBrew_Rejected_NothingConsumed()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");
            var zone = MakeZone(crafter, factory); // no still

            var result = Run(crafter, factory, zone, moss, oil);

            Assert.IsFalse(result.Success);
            StringAssert.Contains("still", result.ErrorMessage);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(moss),
                "a validation rejection must consume nothing.");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(oil));
        }

        [Test]
        public void FarStill_TwoCellsAway_Rejected()
        {
            // Counter-check on the 3×3 box: distance 2 is NOT adjacent.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");
            var zone = MakeZone(crafter, factory, stillDx: 2, stillDy: 0);

            Assert.IsFalse(Run(crafter, factory, zone, moss, oil).Success);
        }

        [Test]
        public void FoodBrew_NeedsNoStill()
        {
            // §6.2: simple foods field-brew anywhere. r_snack resolves
            // sweet → Form "Food".
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var sugar = GiveItem(crafter, factory, "SugarLump");
            var zone = MakeZone(crafter, factory); // no still

            var result = Run(crafter, factory, zone, sugar);

            Assert.IsTrue(result.Success, result.ErrorMessage);
        }

        // ════════════════ Mishap damage (§6.3) ════════════════

        [Test]
        public void Mishap_AppliesSmallSelfDamage()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter(hp: 20);
            var powder = GiveItem(crafter, factory, "BlastPowder");
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone, powder);

            Assert.IsTrue(result.Success, "a mishap is a completed (bad) brew, not a command failure.");
            Assert.AreEqual(
                20 - BrewReagentsCommand.MishapDamageMax,
                crafter.GetStat("Hitpoints").Value,
                "the mishap must singe the crafter for the capped amount.");
        }

        [Test]
        public void Mishap_AtOneHp_DealsNoDamage_NeverKills()
        {
            // §6.3 non-lethal clamp: experimenting can never kill outright.
            var factory = CreateFactory();
            var crafter = CreateCrafter(hp: 1);
            var powder = GiveItem(crafter, factory, "BlastPowder");
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone, powder);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(1, crafter.GetStat("Hitpoints").Value,
                "mishap damage is clamped so Hitpoints never drops below 1.");
        }

        [Test]
        public void SuccessfulBrew_DealsNoDamage()
        {
            // Counter-check: only the Mishap outcome damages the crafter.
            var factory = CreateFactory();
            var crafter = CreateCrafter(hp: 20);
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");
            var zone = MakeZone(crafter, factory, stillDx: 0, stillDy: 1);

            var result = Run(crafter, factory, zone, moss, oil);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(20, crafter.GetStat("Hitpoints").Value);
        }

        // ════════════════ Validation plumbing ════════════════

        [Test]
        public void EmptySelection_FailsValidation()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var result = Run(crafter, factory, zone);

            Assert.IsFalse(result.Success);
        }

        [Test]
        public void NullFactory_FailsValidation()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var zone = MakeZone(crafter, factory, stillDx: 1, stillDy: 0);

            var command = new BrewReagentsCommand(new List<Entity> { moss }, null);
            var result = InventorySystem.ExecuteCommand(command, crafter, zone);

            Assert.IsFalse(result.Success);
        }

        // ════════════════ Still adjacency helper directly ════════════════

        [Test]
        public void IsNearStill_SameCell_True()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var zone = MakeZone(crafter, factory, stillDx: 0, stillDy: 0);

            Assert.IsTrue(AlchemyStillPart.IsNearStill(crafter, zone));
        }

        [Test]
        public void IsNearStill_NullZoneOrUnplacedActor_False()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();

            Assert.IsFalse(AlchemyStillPart.IsNearStill(crafter, null));
            Assert.IsFalse(AlchemyStillPart.IsNearStill(crafter, new Zone("EmptyZone")),
                "an actor not placed in the zone is near nothing.");
        }
    }
}
