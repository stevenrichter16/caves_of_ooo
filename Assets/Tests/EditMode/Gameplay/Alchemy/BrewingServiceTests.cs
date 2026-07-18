using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BrewingService — validation, atomic consumption + rollback, output
    /// creation, discovery recording, and "alchemy" diag emission. Fixture
    /// mirrors TinkeringServiceTests: embedded blueprints + embedded rules,
    /// registry reset in Setup/TearDown.
    /// </summary>
    public class BrewingServiceTests
    {
        private const string TestRulesJson = @"{
            ""Rules"": [
                { ""ID"":""r_burn"", ""RequireAll"":""heat combustible"", ""Effect"":""Burning"", ""Form"":""Coating"", ""Priority"":10,
                  ""Description"":""Heat meets fuel — it catches fire."" },
                { ""ID"":""r_acid"", ""RequireAll"":""corrosive"", ""Effect"":""Acidic"", ""Form"":""Tonic"", ""Priority"":5 },
                { ""ID"":""r_mend"", ""RequireAll"":""vital"", ""Effect"":""Healing"", ""Form"":""Tonic"", ""Priority"":5 }
            ]
        }";

        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [
          { ""Key"": ""Takeable"", ""Value"": ""true"" },
          { ""Key"": ""Weight"", ""Value"": ""1"" }
        ]},
        { ""Name"": ""Render"", ""Params"": [
          { ""Key"": ""DisplayName"", ""Value"": ""item"" },
          { ""Key"": ""RenderString"", ""Value"": ""?"" },
          { ""Key"": ""ColorString"", ""Value"": ""&y"" }
        ]}
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""BrewedTonic"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [
          { ""Key"": ""DisplayName"", ""Value"": ""strange brew"" },
          { ""Key"": ""RenderString"", ""Value"": ""!"" },
          { ""Key"": ""ColorString"", ""Value"": ""&M"" }
        ]},
        { ""Name"": ""Tonic"", ""Params"": [ { ""Key"": ""Drink"", ""Value"": ""true"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""InertSludge"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [
          { ""Key"": ""DisplayName"", ""Value"": ""inert sludge"" },
          { ""Key"": ""RenderString"", ""Value"": ""~"" },
          { ""Key"": ""ColorString"", ""Value"": ""&K"" }
        ]}
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""FireMoss"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""fire-moss"" } ] },
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""heat:2"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""LampOil"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""lamp oil"" } ] },
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""combustible:3, viscous:1"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""InertPebble"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""inert pebble"" } ] },
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""bitter:1"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""BlastPowder"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""blast powder"" } ] },
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""volatile:2"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""VitalRoot"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""vital root"" } ] },
        { ""Name"": ""Reagent"", ""Params"": [ { ""Key"": ""PropertiesRaw"", ""Value"": ""vital:2"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""PlainRock"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""plain rock"" } ] }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    }
  ]
}";

        /// <summary>Factory variant with NO BrewedTonic blueprint — used to force the creation-failure rollback path.</summary>
        private const string BlueprintsWithoutBrewJson = @"{
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
        }

        private static EntityFactory CreateFactory(string blueprintsJson = TestBlueprintsJson)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(blueprintsJson);
            return factory;
        }

        private static Entity CreateCrafter()
        {
            var crafter = new Entity { ID = "crafter", BlueprintName = "Player" };
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

        // ════════════════ Happy path ════════════════

        [Test]
        public void Brew_FireMossPlusLampOil_CreatesBurningCoating_ConsumesReagents()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { moss, oil },
                out Entity brew, out BrewResult result, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);
            Assert.IsNotNull(brew);

            var inventory = crafter.GetPart<InventoryPart>();
            Assert.IsTrue(inventory.Contains(brew), "the brew must land in the crafter's inventory.");
            Assert.IsFalse(inventory.Contains(moss), "fire-moss must be consumed.");
            Assert.IsFalse(inventory.Contains(oil), "lamp oil must be consumed.");

            var brewPart = brew.GetPart<BrewItemPart>();
            Assert.IsNotNull(brewPart, "a status-effect brew must carry BrewItemPart.");
            StringAssert.Contains("Burning", brewPart.EffectsRaw);
            StringAssert.Contains("burning", brew.GetDisplayName(),
                "the brew must self-describe its effect in its name.");
        }

        [Test]
        public void Brew_HealingRule_MapsToTonicHealingDice_NotStatusEffect()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var root = GiveItem(crafter, factory, "VitalRoot");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { root },
                out Entity brew, out BrewResult result, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind);

            var tonic = brew.GetPart<TonicPart>();
            Assert.IsNotNull(tonic);
            Assert.AreEqual("2d4", tonic.Healing,
                "vital:2 must become instant healing dice 2d4 via the Healing mapping.");
            Assert.IsNull(brew.GetPart<BrewItemPart>(),
                "a pure-healing brew has no status entries, so no BrewItemPart.");
        }

        // ════════════════ Non-brew outcomes ════════════════

        [Test]
        public void InertMix_ProducesSludgeItem_ConsumesReagent()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var pebble = GiveItem(crafter, factory, "InertPebble");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { pebble },
                out Entity produced, out BrewResult result, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(BrewOutcomeKind.InertSludge, result.Kind);
            Assert.IsNotNull(produced);
            Assert.AreEqual("InertSludge", produced.BlueprintName);
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(pebble),
                "failed experiments still consume the reagents.");
        }

        [Test]
        public void VolatileAlone_Mishap_ConsumesReagent_ProducesNothing()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var powder = GiveItem(crafter, factory, "BlastPowder");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { powder },
                out Entity produced, out BrewResult result, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(BrewOutcomeKind.Mishap, result.Kind);
            Assert.IsNull(produced, "a mishap yields no item.");
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(powder));
        }

        // ════════════════ Validation — nothing consumed on rejection ════════════════

        [Test]
        public void UnownedReagent_Rejected_NothingConsumed()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var oil = GiveItem(crafter, factory, "LampOil");
            Entity strayMoss = factory.CreateEntity("FireMoss"); // never added to inventory

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { strayMoss, oil },
                out _, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("own", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(oil),
                "rejection must consume nothing.");
        }

        [Test]
        public void NonReagentItem_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var rock = GiveItem(crafter, factory, "PlainRock");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { rock },
                out _, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("not a reagent", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(rock));
        }

        [Test]
        public void SameEntityPassedTwice_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { moss, moss },
                out _, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("twice", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(moss));
        }

        [Test]
        public void EmptySelection_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity>(),
                out _, out _, out string reason);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(reason);
        }

        // ════════════════ Atomicity ════════════════

        [Test]
        public void MissingBrewBlueprint_RollsBack_ReagentsRestored()
        {
            // Factory lacks BrewedTonic → creation fails AFTER consumption —
            // the rollback must restore every consumed reagent.
            var factory = CreateFactory(BlueprintsWithoutBrewJson);
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { moss, oil },
                out Entity produced, out _, out string reason);

            Assert.IsFalse(ok);
            Assert.IsNull(produced);
            StringAssert.Contains("BrewedTonic", reason);

            var inventory = crafter.GetPart<InventoryPart>();
            Assert.IsTrue(inventory.Contains(moss), "rollback must restore fire-moss.");
            Assert.IsTrue(inventory.Contains(oil), "rollback must restore lamp oil.");
        }

        [Test]
        public void StackedReagent_ConsumesOneUnit_ItemStaysInInventory()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            moss.AddPart(new StackerPart { StackCount = 2 });
            var oil = GiveItem(crafter, factory, "LampOil");

            bool ok = BrewingService.TryBrew(
                crafter, factory, new List<Entity> { moss, oil },
                out _, out _, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(moss),
                "a stacked reagent stays in inventory after consuming one unit.");
            Assert.AreEqual(1, moss.GetPart<StackerPart>().StackCount);
        }

        // ════════════════ Discovery + diag ════════════════

        [Test]
        public void Discovery_FirstBrewDiscoversRule_RepeatDoesNot()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();

            var moss1 = GiveItem(crafter, factory, "FireMoss");
            var oil1 = GiveItem(crafter, factory, "LampOil");
            BrewingService.TryBrew(crafter, factory, new List<Entity> { moss1, oil1 },
                out _, out _, out string reason1);

            var knowledge = crafter.GetPart<BrewKnowledgePart>();
            Assert.IsNotNull(knowledge, "the service must auto-attach the knowledge part.");
            Assert.IsTrue(knowledge.Knows("r_burn"), reason1);

            var moss2 = GiveItem(crafter, factory, "FireMoss");
            var oil2 = GiveItem(crafter, factory, "LampOil");
            BrewingService.TryBrew(crafter, factory, new List<Entity> { moss2, oil2 },
                out _, out _, out _);

            var discovered = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy",
                Kind = "BrewDiscovered",
                Limit = 10
            }).Records;
            Assert.AreEqual(1, discovered.Count,
                "r_burn must be recorded as discovered exactly once across two brews.");
        }

        [Test]
        public void Diag_BrewResolved_EmittedWithOutcomeAndRules()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var moss = GiveItem(crafter, factory, "FireMoss");
            var oil = GiveItem(crafter, factory, "LampOil");

            BrewingService.TryBrew(crafter, factory, new List<Entity> { moss, oil },
                out _, out _, out _);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy",
                Kind = "BrewResolved",
                Limit = 5
            }).Records;
            Assert.AreEqual(1, records.Count, "a successful brew must emit BrewResolved.");
            StringAssert.Contains("Brew", records[0].PayloadJson);
            StringAssert.Contains("r_burn", records[0].PayloadJson);
        }

        [Test]
        public void Diag_BrewRejected_EmittedOnValidationFailure()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var rock = GiveItem(crafter, factory, "PlainRock");

            BrewingService.TryBrew(crafter, factory, new List<Entity> { rock },
                out _, out _, out _);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "alchemy",
                Kind = "BrewRejected",
                Limit = 5
            }).Records;
            Assert.AreEqual(1, records.Count, "a rejected brew must emit BrewRejected with the reason.");
            StringAssert.Contains("reagent", records[0].PayloadJson);
        }
    }
}
