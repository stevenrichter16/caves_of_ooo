using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM7 — regression pins for the 2026-07-25 farming audit
    /// (Docs/CROPS-WATERING-GRIMOIRE.md §5 SM7). Each section pins one
    /// confirmed finding's fix; test names carry the finding ID.
    ///
    /// SM7a — the critical ground-seed exploit: the world-action menu
    /// fires GetInventoryActions / InventoryAction directly on
    /// zone-resident items (WorldInteractionSystem.GatherActions →
    /// InputHandler.ExecuteWorldActionSelection), so a seed lying on
    /// the ground offered "Plant", planted at the PLAYER's cell, and
    /// was never consumed (InventoryPart.RemoveObject no-ops for items
    /// not in the inventory) — infinite crops from one dropped seed.
    /// The fix gates both the action row and DoPlant on possession.
    /// </summary>
    [TestFixture]
    public class FarmingAuditFixTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(path));
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            SeedPart.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            SeedPart.Factory = null;
        }

        // ── Helpers ──────────────────────────────────────────────

        private static Entity MakeActor(Zone zone, int x, int y)
        {
            var actor = new Entity { ID = "farmer-" + x + "-" + y, BlueprintName = "TestActor" };
            actor.Tags["Creature"] = "";
            actor.AddPart(new RenderPart { DisplayName = "farmer" });
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(actor, x, y);
            return actor;
        }

        private void PlaceTerrain(Zone zone, string blueprint, int x, int y)
        {
            var terrain = _factory.CreateEntity(blueprint);
            zone.AddEntity(terrain, x, y);
        }

        /// <summary>Mirror of InputHandler.ExecuteWorldActionSelection's
        /// generic dispatch (InputHandler.cs:2413-2419): the world-action
        /// menu fires InventoryAction DIRECTLY on the target entity,
        /// bypassing PerformInventoryActionCommand.</summary>
        private static void FireWorldMenuPlant(Entity target, Entity actor, Zone zone)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "PlantSeed");
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Zone", (object)zone);
            target.FireEventAndRelease(e);
        }

        private static bool CellHasCrop(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && cell.HasObjectWithPart<CropPart>();
        }

        // ════════════════════════════════════════════════════════════
        //   SM7a — ground-seed exploit (audit finding F1 critical,
        //   F2 yellow: remote plant-at-player)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Sm7a_GroundSeed_WorldMenuDispatch_RejectsNotCarried_NoCropNoConsume()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            PlaceTerrain(zone, "Grass", 6, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            zone.AddEntity(seed, 6, 5); // seed lies on the GROUND, not carried

            FireWorldMenuPlant(seed, actor, zone);

            Assert.IsFalse(CellHasCrop(zone, 5, 5),
                "no crop at the actor's cell — the exploit planted here");
            Assert.IsFalse(CellHasCrop(zone, 6, 5), "no crop at the seed's cell either");
            Assert.IsNotNull(zone.GetEntityCell(seed), "ground seed untouched");

            var rejects = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejects.Count, "exactly one PlantRejected record");
            StringAssert.Contains("\"reason\":\"not_carried\"", rejects[0].PayloadJson);

            var planted = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropPlanted", Limit = 5 }).Records;
            Assert.AreEqual(0, planted.Count, "no CropPlanted success record");
        }

        [Test]
        public void Sm7a_GroundSeedStack_NotDecrementedByRemotePlant()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            var stacker = seed.GetPart<StackerPart>();
            Assert.IsNotNull(stacker, "sanity: seeds stack");
            stacker.StackCount = 3;
            zone.AddEntity(seed, 8, 8);

            FireWorldMenuPlant(seed, actor, zone);

            Assert.AreEqual(3, stacker.StackCount,
                "a ground stack must not be decremented by a rejected remote plant");
        }

        [Test]
        public void Sm7a_GroundSeed_WorldMenu_OmitsPlantRow()
        {
            var zone = new Zone("z");
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            zone.AddEntity(seed, 6, 5);

            var actions = WorldInteractionSystem.GatherActions(seed, actor);

            for (int i = 0; i < actions.Count; i++)
                Assert.AreNotEqual("PlantSeed", actions[i].Command,
                    "a ground seed must not offer Plant in the world-action menu");
        }

        [Test]
        public void Sm7a_CarriedSeed_SameRawDispatch_StillPlants()
        {
            // Counter-check: the possession gate keys on POSSESSION, not
            // on which dispatch path fired the event. A carried seed via
            // the same raw world-menu event shape plants normally.
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);

            FireWorldMenuPlant(seed, actor, zone);

            Assert.IsTrue(CellHasCrop(zone, 5, 5), "carried seed plants at the actor's cell");
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "single carried seed is consumed");

            var planted = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropPlanted", Limit = 5 }).Records;
            Assert.AreEqual(1, planted.Count);
        }

        [Test]
        public void Sm7a_CarriedSeed_InventoryUi_StillOffersPlantRow()
        {
            // Counter-check for the action-row gate: the inventory-screen
            // path (InventorySystem.GetActions) must keep offering Plant
            // for a carried seed.
            var zone = new Zone("z");
            var actor = MakeActor(zone, 5, 5);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);

            var actions = InventorySystem.GetActions(actor, seed);

            bool hasPlant = false;
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].Command == "PlantSeed") hasPlant = true;
            Assert.IsTrue(hasPlant, "carried seeds keep the Plant action");
        }

        [Test]
        public void Sm7a_PlantReject_ActorNotInZone_DistinctReason()
        {
            // Audit note: three failure modes shared reason "no_zone".
            // The actor-missing-from-zone path now reports its own reason
            // so a diag query can tell "no active zone" from "actor not
            // registered in this zone".
            var zone = new Zone("z");
            var actor = new Entity { ID = "ghost", BlueprintName = "TestActor" };
            actor.AddPart(new RenderPart { DisplayName = "ghost" });
            actor.AddPart(new InventoryPart { MaxWeight = 150 });
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);
            // actor deliberately NOT added to the zone

            var result = InventorySystem.ExecuteCommand(
                new CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand(seed, "PlantSeed"),
                actor, zone);

            var rejects = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, rejects.Count);
            StringAssert.Contains("\"reason\":\"actor_not_in_zone\"", rejects[0].PayloadJson);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "seed not consumed on reject");
        }
    }
}
