using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM1 — content blueprints + planting flow.
    /// See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.2, §2.5</c>.
    ///
    /// Uses the REAL Objects.json via EntityFactory (VenomDaggerChallengeTests
    /// pattern) so the content ships validated end-to-end, and the REAL
    /// command path (InventorySystem.ExecuteCommand + PerformInventoryActionCommand)
    /// so the event plumbing is exercised, not simulated.
    /// </summary>
    [TestFixture]
    public class CropPlantingTests
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
            var e = new Entity { ID = $"actor_{x}_{y}", BlueprintName = "TestActor" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "farmer" });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>Spawn a terrain entity from a real blueprint into a cell.</summary>
        private static Entity PlaceTerrain(Zone zone, string blueprint, int x, int y)
        {
            var terrain = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(terrain, $"Blueprint '{blueprint}' must exist.");
            zone.AddEntity(terrain, x, y);
            return terrain;
        }

        private static Entity GiveSeed(Entity actor, string blueprint)
        {
            var seed = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(seed, $"Blueprint '{blueprint}' must exist.");
            actor.GetPart<InventoryPart>().AddObject(seed);
            return seed;
        }

        private static InventoryCommandResult Plant(Entity actor, Entity seed, Zone zone)
        {
            return InventorySystem.ExecuteCommand(
                new PerformInventoryActionCommand(seed, "PlantSeed"), actor, zone);
        }

        private static Entity FindCropInCell(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasPart<CropPart>())
                    return cell.Objects[i];
            return null;
        }

        // ── Content sanity ───────────────────────────────────────

        [Test]
        public void Content_SeedBlueprints_CarrySeedPart_PointingAtCropBlueprints()
        {
            var carrot = _factory.CreateEntity("CandyCarrotSeed");
            var wheat = _factory.CreateEntity("EmberwheatSeed");
            Assert.IsNotNull(carrot);
            Assert.IsNotNull(wheat);
            Assert.AreEqual("CandyCarrotCrop", carrot.GetPart<SeedPart>()?.CropBlueprint);
            Assert.AreEqual("EmberwheatCrop", wheat.GetPart<SeedPart>()?.CropBlueprint);
        }

        [Test]
        public void Content_CropBlueprints_CarryCropPart_CropTag_AndParams()
        {
            var carrot = _factory.CreateEntity("CandyCarrotCrop");
            var wheat = _factory.CreateEntity("EmberwheatCrop");
            Assert.IsNotNull(carrot);
            Assert.IsNotNull(wheat);
            Assert.IsTrue(carrot.HasTag("Crop"), "CropSystem snapshots via the Crop tag — required.");
            Assert.IsTrue(wheat.HasTag("Crop"));

            var cc = carrot.GetPart<CropPart>();
            var wc = wheat.GetPart<CropPart>();
            Assert.AreEqual(20, cc.TicksPerStage);
            Assert.AreEqual(35, wc.TicksPerStage);
            Assert.AreEqual("CandyCarrot", cc.YieldBlueprint);
            Assert.AreEqual(2, cc.YieldCount); // ALPHA economy: yield bumped 1->2
            Assert.AreEqual("Emberwheat", wc.YieldBlueprint);
            Assert.AreEqual(2, wc.YieldCount);
            Assert.AreEqual(0, cc.GrowthStage, "crops start at seed stage");
            Assert.AreEqual(0, cc.MoistureTicks, "crops start dry");
        }

        [Test]
        public void Content_StageCsvs_ResolvePerStage()
        {
            var crop = _factory.CreateEntity("CandyCarrotCrop").GetPart<CropPart>();
            Assert.AreEqual('.', crop.GlyphForStage(0));
            Assert.AreEqual('t', crop.GlyphForStage(1));
            Assert.AreEqual("&w", crop.ColorForStage(0));
            Assert.AreEqual("&g", crop.ColorForStage(1));
            Assert.AreEqual('\0', crop.GlyphForStage(2), "out-of-range stage yields sentinel, not crash");
            Assert.IsNull(crop.ColorForStage(9));
        }

        [Test]
        public void Content_ProduceBlueprints_AreTakeable()
        {
            var carrot = _factory.CreateEntity("CandyCarrot");
            var wheat = _factory.CreateEntity("Emberwheat");
            Assert.IsNotNull(carrot);
            Assert.IsNotNull(wheat);
            Assert.IsTrue(carrot.GetPart<PhysicsPart>().Takeable);
            Assert.IsTrue(wheat.GetPart<PhysicsPart>().Takeable);
        }

        [Test]
        public void Content_Grass_IsPlantable_PlainFloorIsNot()
        {
            var grass = _factory.CreateEntity("Grass");
            var floor = _factory.CreateEntity("Floor");
            Assert.IsTrue(grass.HasTag("Plantable"));
            Assert.IsTrue(grass.HasTag("Terrain"), "tag inherited from Terrain base blueprint");
            Assert.IsFalse(floor.HasTag("Plantable"),
                "plain Floor must NOT be plantable — the not_plantable gate depends on it");
        }

        // ── Action offer ─────────────────────────────────────────

        [Test]
        public void Seed_OffersPlantAction()
        {
            var zone = new Zone("z");
            var actor = MakeActor(zone, 5, 5);
            var seed = GiveSeed(actor, "CandyCarrotSeed");

            var actions = InventorySystem.GetActions(actor, seed);

            bool hasPlant = false;
            for (int i = 0; i < actions.Count; i++)
                if (actions[i].Command == "PlantSeed") hasPlant = true;
            Assert.IsTrue(hasPlant, "seed items must offer the Plant action");
        }

        // ── Planting: success path ───────────────────────────────

        [Test]
        public void Plant_OnPlantableGrass_SpawnsCrop_ConsumesSeed_EmitsDiag()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = GiveSeed(actor, "CandyCarrotSeed");

            var result = Plant(actor, seed, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            var crop = FindCropInCell(zone, 5, 5);
            Assert.IsNotNull(crop, "a crop entity must be spawned in the actor's cell");
            Assert.AreEqual("CandyCarrotCrop", crop.BlueprintName);
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "single seed consumed on planting");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropPlanted", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"cropBlueprint\":\"CandyCarrotCrop\"", recs[0].PayloadJson);
        }

        [Test]
        public void Plant_StackOfThree_DecrementsToTwo_SeedStaysInInventory()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = GiveSeed(actor, "CandyCarrotSeed");
            seed.GetPart<StackerPart>().StackCount = 3;

            Plant(actor, seed, zone);

            Assert.AreEqual(2, seed.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "stack of 2 remains in inventory");
        }

        // ── Planting: reject gates (each with counter-diag) ──────

        [Test]
        public void Plant_OnPlainFloor_Rejects_NotPlantable_NoCropNoConsume()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Floor", 5, 5); // Terrain but NOT Plantable
            var actor = MakeActor(zone, 5, 5);
            var seed = GiveSeed(actor, "CandyCarrotSeed");

            Plant(actor, seed, zone);

            Assert.IsNull(FindCropInCell(zone, 5, 5), "no crop on non-plantable ground");
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "seed NOT consumed on rejection");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"not_plantable\"", recs[0].PayloadJson);
        }

        [Test]
        public void Plant_BareCellWithNoTerrainEntity_Rejects_NotPlantable()
        {
            var zone = new Zone("z");
            var actor = MakeActor(zone, 5, 5); // no terrain entity at all
            var seed = GiveSeed(actor, "CandyCarrotSeed");

            Plant(actor, seed, zone);

            Assert.IsNull(FindCropInCell(zone, 5, 5));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"not_plantable\"", recs[0].PayloadJson);
        }

        [Test]
        public void Plant_SecondSeedSameCell_Rejects_AlreadyPlanted()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var first = GiveSeed(actor, "CandyCarrotSeed");
            var second = GiveSeed(actor, "EmberwheatSeed");

            Plant(actor, first, zone);
            Diag.ResetAll(); // isolate the second attempt's records
            Plant(actor, second, zone);

            var cell = zone.GetCell(5, 5);
            int cropCount = 0;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasPart<CropPart>()) cropCount++;
            Assert.AreEqual(1, cropCount, "one crop per cell");
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(second),
                "second seed NOT consumed");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"already_planted\"", recs[0].PayloadJson);
        }

        [Test]
        public void Plant_NoFactory_Rejects_NoFactory_GracefulNoCrash()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);
            var seed = GiveSeed(actor, "CandyCarrotSeed");

            SeedPart.Factory = null;
            Assert.DoesNotThrow(() => Plant(actor, seed, zone));

            Assert.IsNull(FindCropInCell(zone, 5, 5));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"no_factory\"", recs[0].PayloadJson);
        }

        [Test]
        public void Plant_UnknownCropBlueprint_Rejects_UnknownBlueprint()
        {
            var zone = new Zone("z");
            PlaceTerrain(zone, "Grass", 5, 5);
            var actor = MakeActor(zone, 5, 5);

            // Synthetic seed pointing at a bogus blueprint — content bugs
            // must reject cleanly, not crash or spawn nothing silently.
            var seed = new Entity { ID = "bogus-seed", BlueprintName = "BogusSeed" };
            seed.Tags["Item"] = "";
            seed.AddPart(new RenderPart { DisplayName = "bogus seed" });
            seed.AddPart(new PhysicsPart { Takeable = true });
            seed.AddPart(new SeedPart { CropBlueprint = "NoSuchCropBlueprint" });
            actor.GetPart<InventoryPart>().AddObject(seed);

            // EntityFactory logs an [Error] for unknown blueprints — expected
            // here; the assertion is that WE reject gracefully, not silently.
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "EntityFactory: unknown blueprint 'NoSuchCropBlueprint'");
            Assert.DoesNotThrow(() => Plant(actor, seed, zone));

            Assert.IsNull(FindCropInCell(zone, 5, 5));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"unknown_blueprint\"", recs[0].PayloadJson);
        }

        // ── CropPart.Water unit behavior (used by SM3's Conjure Rain) ──

        [Test]
        public void Water_TopUpSemantics_NotAdditive()
        {
            var crop = _factory.CreateEntity("CandyCarrotCrop").GetPart<CropPart>();

            crop.Water(40);
            Assert.AreEqual(40, crop.MoistureTicks);

            crop.MoistureTicks = 10;
            crop.Water(40);
            Assert.AreEqual(40, crop.MoistureTicks, "top-up to 40, not 50");

            crop.Water(5);
            Assert.AreEqual(40, crop.MoistureTicks, "a weaker watering never REDUCES moisture");
        }

        [Test]
        public void Water_SetsWetSoilBackground_DryOutClearsIt()
        {
            var cropEntity = _factory.CreateEntity("CandyCarrotCrop");
            var crop = cropEntity.GetPart<CropPart>();
            var render = cropEntity.GetPart<RenderPart>();
            Assert.IsTrue(string.IsNullOrEmpty(render.BackgroundColor),
                "crop blueprints must not author a BackgroundColor — dry-out restores to empty");

            crop.Water(40);
            Assert.AreEqual(CropPart.WET_SOIL_BG, render.BackgroundColor,
                "watering darkens the soil (bg block behind the crop glyph)");

            crop.OnDriedOut();
            Assert.IsTrue(string.IsNullOrEmpty(render.BackgroundColor),
                "drying out clears the wet-soil look");
        }
    }
}
