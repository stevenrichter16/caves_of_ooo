using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// GatherNodePart — the renewable/single-use forage & mineral harvest
    /// point (M3 of the gather/loot system, Docs/GATHER-LOOT-SYSTEM.md).
    /// </summary>
    public class GatherNodePartTests
    {
        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""1"" }] },
        { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""item"" }, { ""Key"": ""RenderString"", ""Value"": ""?"" }] }
      ],
      ""Tags"": []
    },
    {
      ""Name"": ""TestBerry"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""test berry"" }] }
      ],
      ""Tags"": []
    }
  ]
}";

        private const string TestLootTablesJson = @"{
  ""Tables"": [
    {
      ""ID"": ""test_berry_bush"",
      ""Entries"": [
        { ""BlueprintName"": ""TestBerry"", ""Weight"": 100, ""MinCount"": 1, ""MaxCount"": 1 }
      ]
    },
    {
      ""ID"": ""test_dry_table"",
      ""Entries"": [
        { ""BlueprintName"": ""TestBerry"", ""Weight"": 0, ""MinCount"": 1, ""MaxCount"": 1 }
      ]
    }
  ]
}";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprintsJson);
            GatherHarvestFactory.Factory = _factory;
            LootTableRegistry.ResetForTests();
            LootTableRegistry.InitializeFromJson(TestLootTablesJson);
        }

        [TearDown]
        public void TearDown()
        {
            GatherHarvestFactory.Factory = null;
            LootTableRegistry.ResetForTests();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity CreateNode(string lootTableId, int maxUses = 1, int regrowTurns = 0)
        {
            var node = new Entity { BlueprintName = "TestNode" };
            node.AddPart(new RenderPart { DisplayName = "test node" });
            node.AddPart(new GatherNodePart
            {
                LootTableID = lootTableId,
                MaxUses = maxUses,
                RegrowTurns = regrowTurns,
                TestRng = new Random(0),
                TestCurrentTurn = 0
            });
            return node;
        }

        private static Entity CreateHarvester()
        {
            var actor = new Entity { BlueprintName = "TestHarvester" };
            actor.AddPart(new InventoryPart());
            return actor;
        }

        private static void FireHarvest(Entity node, Entity actor)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "HarvestNode");
            e.SetParameter("Actor", (object)actor);
            node.FireEvent(e);
            e.Release();
        }

        private static bool OffersHarvestAction(Entity node)
        {
            var e = GameEvent.New("GetInventoryActions");
            var actions = new InventoryActionList();
            e.SetParameter("Actions", (object)actions);
            node.FireEvent(e);
            e.Release();

            foreach (var a in actions.Actions)
                if (a.Command == "HarvestNode") return true;
            return false;
        }

        // ====================================================================
        // Basic harvest
        // ====================================================================

        [Test]
        public void Harvest_GivesItemToActorInventory()
        {
            var node = CreateNode("test_berry_bush");
            var actor = CreateHarvester();

            FireHarvest(node, actor);

            var inventory = actor.GetPart<InventoryPart>();
            Assert.AreEqual(1, inventory.Objects.Count);
            Assert.AreEqual("TestBerry", inventory.Objects[0].BlueprintName);
        }

        [Test]
        public void Harvest_WithoutFactory_GivesNothing_ButDoesNotThrow()
        {
            GatherHarvestFactory.Factory = null;
            var node = CreateNode("test_berry_bush");
            var actor = CreateHarvester();

            Assert.DoesNotThrow(() => FireHarvest(node, actor));
            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void Harvest_ActorWithoutInventory_RejectsGracefully()
        {
            var node = CreateNode("test_berry_bush");
            var actor = new Entity { BlueprintName = "NoInventoryActor" };

            Assert.DoesNotThrow(() => FireHarvest(node, actor));
        }

        // ====================================================================
        // Single-use depletion (RegrowTurns = 0, the default)
        // ====================================================================

        [Test]
        public void SingleUse_OffersHarvest_BeforeFirstUse()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 0);
            Assert.IsTrue(OffersHarvestAction(node));
        }

        [Test]
        public void SingleUse_NoLongerOffersHarvest_AfterDepleted()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 0);
            var actor = CreateHarvester();

            FireHarvest(node, actor);

            Assert.IsFalse(OffersHarvestAction(node),
                "A single-use node with RegrowTurns=0 must stop offering Harvest once its uses run out.");
        }

        [Test]
        public void SingleUse_SecondHarvestAttempt_GivesNothing_AndRejects()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 0);
            var actor = CreateHarvester();

            FireHarvest(node, actor);
            FireHarvest(node, actor); // depleted — must no-op, not double-give

            var inventory = actor.GetPart<InventoryPart>();
            Assert.AreEqual(1, inventory.Objects.Count,
                "A depleted single-use node must not yield a second item on a repeated harvest attempt.");
        }

        [Test]
        public void SingleUse_RemovesNodeFromZone_WhenDepleted()
        {
            var zone = new Zone("TestZone");
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 0);
            zone.AddEntity(node, 5, 5);

            var actor = CreateHarvester();
            var brain = new BrainPart { CurrentZone = zone };
            actor.AddPart(brain);

            FireHarvest(node, actor);

            Assert.IsNull(zone.GetEntityCell(node),
                "A single-use (RegrowTurns=0) node must be removed from the zone once its uses are exhausted.");
        }

        [Test]
        public void MultiUse_SingleUseFamily_SurvivesUntilLastUse()
        {
            // Counter-check for immediate removal: MaxUses=2 must survive the
            // FIRST harvest and only vanish after the second.
            var zone = new Zone("TestZone");
            var node = CreateNode("test_berry_bush", maxUses: 2, regrowTurns: 0);
            zone.AddEntity(node, 5, 5);

            var actor = CreateHarvester();
            actor.AddPart(new BrainPart { CurrentZone = zone });

            FireHarvest(node, actor);
            Assert.IsNotNull(zone.GetEntityCell(node), "Node with 2 uses must survive after only 1 harvest.");

            FireHarvest(node, actor);
            Assert.IsNull(zone.GetEntityCell(node), "Node must be removed after its 2nd (final) use.");

            Assert.AreEqual(2, actor.GetPart<InventoryPart>().Objects.Count);
        }

        // ====================================================================
        // Regrowing nodes (RegrowTurns > 0)
        // ====================================================================

        [Test]
        public void Regrowing_StaysInZone_AfterDepletion()
        {
            var zone = new Zone("TestZone");
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 50);
            zone.AddEntity(node, 5, 5);

            var actor = CreateHarvester();
            actor.AddPart(new BrainPart { CurrentZone = zone });

            FireHarvest(node, actor);

            Assert.IsNotNull(zone.GetEntityCell(node),
                "A regrowing node (RegrowTurns>0) must remain in the zone after depletion.");
        }

        [Test]
        public void Regrowing_DoesNotOfferHarvest_BeforeRegrowTurnsElapse()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 50);
            var actor = CreateHarvester();
            FireHarvest(node, actor);

            Assert.IsFalse(OffersHarvestAction(node),
                "A just-depleted regrowing node must not offer Harvest before its regrow window elapses.");
        }

        [Test]
        public void Regrowing_OffersHarvestAgain_AfterRegrowTurnsElapse()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 5);
            var part = node.GetPart<GatherNodePart>();
            var actor = CreateHarvester();

            FireHarvest(node, actor); // depletes at turn 0; regrow due at turn 5
            part.TestCurrentTurn = 5;

            Assert.IsTrue(OffersHarvestAction(node),
                "A regrowing node must offer Harvest again once its regrow window has elapsed.");
        }

        [Test]
        public void Regrowing_SecondHarvest_GivesItemAgain_AfterRegrowth()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 5);
            var part = node.GetPart<GatherNodePart>();
            var actor = CreateHarvester();

            FireHarvest(node, actor);
            part.TestCurrentTurn = 5;
            FireHarvest(node, actor);

            Assert.AreEqual(2, actor.GetPart<InventoryPart>().Objects.Count,
                "A regrown node must yield loot again on a second harvest.");
        }

        [Test]
        public void Regrowing_DisplayNameSwapsToDepletedVariant()
        {
            var node = CreateNode("test_berry_bush", maxUses: 1, regrowTurns: 50);
            node.GetPart<GatherNodePart>().DepletedDisplayName = "picked-over test node";
            var actor = CreateHarvester();

            FireHarvest(node, actor);

            Assert.AreEqual("picked-over test node", node.GetPart<RenderPart>().DisplayName);
        }

        // ====================================================================
        // Malformed / edge configuration
        // ====================================================================

        [Test]
        public void UnknownLootTableID_RejectsGracefully_DoesNotThrow()
        {
            var node = CreateNode("no_such_table");
            var actor = CreateHarvester();

            Assert.DoesNotThrow(() => FireHarvest(node, actor));
            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void ZeroWeightTable_GivesNothing_ButStillConsumesTheUse()
        {
            var node = CreateNode("test_dry_table", maxUses: 1, regrowTurns: 0);
            var actor = CreateHarvester();

            FireHarvest(node, actor);

            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
            Assert.IsFalse(OffersHarvestAction(node),
                "Even an empty roll consumes the harvest attempt — no infinite free re-rolls on a dry table.");
        }

    }
}
