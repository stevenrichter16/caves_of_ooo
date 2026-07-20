using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Gameplay.Alchemy
{
    /// <summary>
    /// M3-L3 SM4 — the still's world-action surface: the look-mode menu rows
    /// AlchemyStillPart declares (GetInventoryActions) and the InventoryAction
    /// handlers that resolve the player's set-aside reagents into
    /// BrewReagentsCommand executions. Events are fired exactly the way
    /// InputHandler.ExecuteWorldActionSelection fires them (Command + Actor +
    /// Zone). Statics discipline: AlchemyStillPart.Factory is wired in Setup
    /// and cleared in TearDown — the stale-static trap is the exact failure
    /// mode the FactoryUnwired test pins.
    /// </summary>
    public class AlchemyStillActionsTests
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
      ""Name"": ""AlchemyStill"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""alchemy still"" } ] },
        { ""Name"": ""AlchemyStill"", ""Params"": [] }
      ],
      ""Stats"": [],
      ""Tags"": []
    }
  ]
}";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            BrewRuleRegistry.ResetForTests();
            BrewRuleRegistry.InitializeFromJson(TestRulesJson);
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprintsJson);
            AlchemyStillPart.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            AlchemyStillPart.Factory = null;
            BrewRuleRegistry.ResetForTests();
        }

        private static Entity CreateBrewer()
        {
            var brewer = new Entity { ID = "brewer", BlueprintName = "Player" };
            brewer.Statistics["Hitpoints"] = new Stat
            {
                Owner = brewer, Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20
            };
            brewer.AddPart(new RenderPart { DisplayName = "brewer" });
            brewer.AddPart(new InventoryPart());
            return brewer;
        }

        private Entity GiveMarked(Entity brewer, string blueprint, int stack = 1)
        {
            Entity item = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            if (stack > 1)
                item.AddPart(new StackerPart { StackCount = stack });
            Assert.IsTrue(brewer.GetPart<InventoryPart>().AddObject(item));
            Assert.IsTrue(CraftingMarkPart.Toggle(item), "fixture marks the item");
            return item;
        }

        /// <summary>Brewer at (5,5), still adjacent at (6,5) — inside the 3×3 gate.</summary>
        private Zone MakeZoneWithStill(Entity brewer, out Entity still)
        {
            var zone = new Zone("StillActionZone");
            Assert.IsTrue(zone.AddEntity(brewer, 5, 5));
            still = _factory.CreateEntity("AlchemyStill");
            Assert.IsNotNull(still);
            Assert.IsTrue(zone.AddEntity(still, 6, 5));
            return zone;
        }

        /// <summary>Fire InventoryAction exactly the way InputHandler's world menu does.</summary>
        private static void FireWorldAction(Entity target, Entity actor, Zone zone, string command)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", command);
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Zone", (object)zone);
            target.FireEventAndRelease(e);
        }

        private static List<Entity> Carried(Entity brewer)
        {
            return brewer.GetPart<InventoryPart>().Objects;
        }

        // ════════════════ Menu rows ════════════════

        [Test]
        public void GatherActions_StillDeclaresBrewRows()
        {
            var brewer = CreateBrewer();
            MakeZoneWithStill(brewer, out Entity still);

            var actions = WorldInteractionSystem.GatherActions(still);
            var commands = actions.ConvertAll(a => a.Command);

            CollectionAssert.Contains(commands, "BrewMix");
            CollectionAssert.Contains(commands, "BrewMixBatch");
        }

        [Test]
        public void GatherActions_WithActor_ListsCarriedReagentsAsToggleRows()
        {
            // Live-playtest finding (2026-07-18): mark-then-use alone made the
            // still answer "go set things aside first" — the station menu must
            // let the player build the mix RIGHT THERE. With an actor, the
            // still appends one CraftToggle row per carried reagent, showing
            // its in-mix state.
            var brewer = CreateBrewer();
            Entity moss = _factory.CreateEntity("FireMoss");
            Assert.IsTrue(brewer.GetPart<InventoryPart>().AddObject(moss));
            var oil = GiveMarked(brewer, "LampOil");
            var comp = new Entity { ID = "blade-x", BlueprintName = "blade" };
            comp.AddPart(new RenderPart { DisplayName = "steel blade" });
            comp.AddPart(new WeaponComponentPart { Slot = "Blade" });
            Assert.IsTrue(brewer.GetPart<InventoryPart>().AddObject(comp));
            MakeZoneWithStill(brewer, out Entity still);

            var actions = WorldInteractionSystem.GatherActions(still, brewer);

            var mossRow = actions.Find(a => a.Command == "CraftToggle:" + moss.ID);
            Assert.IsNotNull(mossRow, "unmarked carried reagent gets a toggle row");
            StringAssert.Contains("add", mossRow.Display.ToLowerInvariant());

            var oilRow = actions.Find(a => a.Command == "CraftToggle:" + oil.ID);
            Assert.IsNotNull(oilRow, "marked reagent gets a toggle row too");
            StringAssert.Contains("picked", oilRow.Display.ToLowerInvariant());

            Assert.IsNull(actions.Find(a => a.Command == "CraftToggle:" + comp.ID),
                "a weapon component gets NO row on the STILL — wrong station");
        }

        [Test]
        public void GatherActions_WithoutActor_NoToggleRows()
        {
            // Back-compat counter-check: the actor-less gather (menus opened
            // outside the world-action flow) keeps the verb rows only.
            var brewer = CreateBrewer();
            GiveMarked(brewer, "FireMoss");
            MakeZoneWithStill(brewer, out Entity still);

            var actions = WorldInteractionSystem.GatherActions(still);

            Assert.IsNull(actions.Find(a => a.Command.StartsWith("CraftToggle:")),
                "no toggle rows without an actor");
            Assert.IsNotNull(actions.Find(a => a.Command == "BrewMix"), "verb rows remain");
        }

        // ════════════════ BrewMix handler ════════════════

        [Test]
        public void BrewMix_MarkedReagents_BrewsAndConsumes()
        {
            var brewer = CreateBrewer();
            var moss = GiveMarked(brewer, "FireMoss");
            var oil = GiveMarked(brewer, "LampOil");
            var zone = MakeZoneWithStill(brewer, out Entity still);

            FireWorldAction(still, brewer, zone, "BrewMix");

            Assert.IsFalse(Carried(brewer).Contains(moss), "fire moss consumed");
            Assert.IsFalse(Carried(brewer).Contains(oil), "lamp oil consumed");
            Assert.IsNotNull(Carried(brewer).Find(e => e.HasPart<BrewItemPart>()),
                "a brewed item lands in inventory");
        }

        [Test]
        public void BrewMix_NothingMarked_LegibleAndNoChange()
        {
            // Counter-check: carrying reagents is not enough — they must be
            // SET ASIDE. Handler tells the player what to do, changes nothing.
            var brewer = CreateBrewer();
            Entity moss = _factory.CreateEntity("FireMoss");
            Assert.IsTrue(brewer.GetPart<InventoryPart>().AddObject(moss));
            var zone = MakeZoneWithStill(brewer, out Entity still);

            FireWorldAction(still, brewer, zone, "BrewMix");

            Assert.IsTrue(Carried(brewer).Contains(moss), "unmarked reagent untouched");
            Assert.IsNull(Carried(brewer).Find(e => e.HasPart<BrewItemPart>()), "nothing brewed");
            StringAssert.Contains("aside", MessageLog.GetLast() ?? string.Empty,
                "the handler must tell the player about the set-aside flow");
        }

        [Test]
        public void BrewMix_MarkedComponentDoesNotFeedTheBrew()
        {
            // Cross-flow counter-check: a marked weapon component is in the
            // Components bucket, never the Reagents bucket — the still must
            // treat this as "nothing set aside to brew".
            var brewer = CreateBrewer();
            var comp = new Entity { ID = "blade", BlueprintName = "blade" };
            comp.AddPart(new RenderPart { DisplayName = "steel blade" });
            comp.AddPart(new WeaponComponentPart { Slot = "Blade" });
            Assert.IsTrue(brewer.GetPart<InventoryPart>().AddObject(comp));
            Assert.IsTrue(CraftingMarkPart.Toggle(comp));
            var zone = MakeZoneWithStill(brewer, out Entity still);

            FireWorldAction(still, brewer, zone, "BrewMix");

            Assert.IsTrue(Carried(brewer).Contains(comp), "component untouched");
            Assert.IsNull(Carried(brewer).Find(e => e.HasPart<BrewItemPart>()), "nothing brewed");
        }

        [Test]
        public void BrewMix_FactoryUnwired_LegibleNotCrash()
        {
            // The stale-static trap pin: if bootstrap wiring is missed (or a
            // domain reload cleared it), the handler must degrade to a
            // message — never a null-ref mid-event.
            var brewer = CreateBrewer();
            var moss = GiveMarked(brewer, "FireMoss");
            var oil = GiveMarked(brewer, "LampOil");
            var zone = MakeZoneWithStill(brewer, out Entity still);

            AlchemyStillPart.Factory = null;
            Assert.DoesNotThrow(() => FireWorldAction(still, brewer, zone, "BrewMix"));

            Assert.IsTrue(Carried(brewer).Contains(moss), "reagents untouched");
            Assert.IsTrue(Carried(brewer).Contains(oil));
        }

        // ════════════════ BrewMixBatch handler ════════════════

        [Test]
        public void BrewMixBatch_StackedReagents_BrewsTheFullStack()
        {
            var brewer = CreateBrewer();
            GiveMarked(brewer, "FireMoss", stack: 3);
            GiveMarked(brewer, "LampOil", stack: 3);
            var zone = MakeZoneWithStill(brewer, out Entity still);

            FireWorldAction(still, brewer, zone, "BrewMixBatch");

            int brews = Carried(brewer).FindAll(e => e.HasPart<BrewItemPart>()).Count;
            Assert.AreEqual(3, brews, "batch brews down to the smallest stack");
        }

        [Test]
        public void BrewMixBatch_SingleReagents_BrewsExactlyOne()
        {
            // Counter-check: batch with singles is just one brew — the batch
            // path must not loop past exhaustion or mint extras.
            var brewer = CreateBrewer();
            GiveMarked(brewer, "FireMoss");
            GiveMarked(brewer, "LampOil");
            var zone = MakeZoneWithStill(brewer, out Entity still);

            FireWorldAction(still, brewer, zone, "BrewMixBatch");

            int brews = Carried(brewer).FindAll(e => e.HasPart<BrewItemPart>()).Count;
            Assert.AreEqual(1, brews);
        }
    }
}
