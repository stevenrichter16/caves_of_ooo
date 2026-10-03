using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent context, malformed-packet, and finite-source review.</summary>
    public sealed class GleanersFieldworkAdversarialTests
    {
        HaulingContentScope content;
        EntityFactory oldCropFactory, oldHarvestFactory;
        Zone oldActive;
        [SetUp] public void Setup()
        {
            content = new HaulingContentScope(); content.Seed(64);
            RepairRecipeRegistry.ResetForTests();
            oldCropFactory = CropSystem.Factory; oldHarvestFactory = HarvestablePart.Factory; oldActive = SettlementRuntime.ActiveZone;
            CropSystem.Factory = HarvestablePart.Factory = content.Factory;
        }
        [TearDown] public void Cleanup()
        {
            FieldworkCreationProbe.Remembered = null;
            FieldworkCreationProbe.ChangeRepair = false;
            CropSystem.Factory = oldCropFactory; HarvestablePart.Factory = oldHarvestFactory; SettlementRuntime.ActiveZone = oldActive;
            RepairRecipeRegistry.ResetForTests(); content.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyTheActualGladeWicketPromisesItsLocalGardenAndCellar(bool glade)
        {
            Entity wicket;
            if (glade)
            {
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var zone = manager.GetZone(ReferenceGladePlan.ZoneID);
                wicket = zone.GetCell(46, 6).Objects.Single(e => e.BlueprintName == "GleanersBuckledWicket");
                var originalNotice = zone.GetCell(43, 11).Objects.Single(e => e.BlueprintName == "Signpost");
                var districtNotice = zone.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "notice");
                Assert.AreNotSame(originalNotice, districtNotice);
                StringAssert.Contains("drawgourd bed grows north", originalNotice.GetPart<ExaminablePart>().Text);
                StringAssert.Contains("pallet in the cellar", originalNotice.GetPart<ExaminablePart>().Text);
                Assert.AreEqual((41, 10), zone.GetEntityPosition(districtNotice));
            }
            else wicket = content.Factory.CreateEntity("GleanersBuckledWicket");
            Assert.AreEqual(glade ? "garden wicket" : "timber wicket", wicket.GetPart<RenderPart>().DisplayName);
            string text = wicket.GetPart<ExaminablePart>().Text.ToLowerInvariant();
            Assert.AreEqual(glade, text.Contains("drawgourd"));
            Assert.AreEqual(glade, text.Contains("cellar"));
            if (glade) StringAssert.Contains("north", text);
        }

        [Test]
        public void IntactLowPalletBlocksMovementButDoesNotPretendToBlockSight()
        {
            var zone = new Zone("fieldwork-obstacle-review");
            var pallet = content.Factory.CreateEntity("GleanersTimberPallet");
            Assert.True(zone.AddEntity(pallet, 5, 5));
            Assert.True(zone.GetCell(5, 5).BlocksMovement());
            Assert.True(AIHelpers.HasLineOfSight(zone, 4, 5, 6, 5));
            StringAssert.DoesNotContain("as cover", pallet.GetPart<ExaminablePart>().Text.ToLowerInvariant());
            Assert.True(zone.RemoveEntity(pallet));
            Assert.False(zone.GetCell(5, 5).BlocksMovement());
            Assert.True(zone.AddEntity(content.Factory.CreateEntity("StoneWall"), 5, 5));
            Assert.False(AIHelpers.HasLineOfSight(zone, 4, 5, 6, 5), "Same sightline with an actual opaque owner must be blocked.");
        }

        [TestCase("wicket-recipe")]
        [TestCase("wicket-composition")]
        [TestCase("crop-seed")]
        [TestCase("pallet-yield")]
        [TestCase("pallet-weight")]
        public void MalformedNewContentCannotPublishHalfAFieldworkPacket(string fault)
        {
            bool cellar = fault.StartsWith("pallet", StringComparison.Ordinal);
            if (fault == "wicket-recipe") content.Factory.Blueprints["GleanersBuckledWicket"].Parts["Repairable"]["RecipeId"] = "clay-well-lining";
            if (fault == "wicket-composition") content.Factory.Blueprints["GleanersBuckledWicket"].Parts["Composition"]["MaterialsRaw"] = "Masonry";
            if (fault == "crop-seed") content.Factory.Blueprints["DrawgourdCrop"].Parts["Crop"]["SeedYieldCount"] = "0";
            if (fault == "pallet-yield") content.Factory.Blueprints["GleanersTimberPallet"].Parts["Harvestable"]["YieldMax"] = "3";
            if (fault == "pallet-weight") content.Factory.Blueprints["GleanersTimberPallet"].Parts["Handling"]["Weight"] = "1";
            string id = cellar ? GleanersCellarBuilder.ZoneID : ReferenceGladePlan.ZoneID;
            var zone = new Zone(id); zone.GenReservedCells.Add((1, 1));
            string tiles = zone.TileState.ToSaveString();
            IZoneBuilder fresh = cellar ? (IZoneBuilder)new GleanersCellarBuilder(64, true, null, true) : new ReferenceGladeBuilder(64, true);
            Assert.False(fresh.BuildZone(zone, content.Factory, new Random(7)));
            Assert.Zero(zone.EntityCount); Assert.AreEqual(tiles, zone.TileState.ToSaveString());
            CollectionAssert.AreEquivalent(new[] { (1, 1) }, zone.GenReservedCells);
            IZoneBuilder legacy = cellar ? (IZoneBuilder)new GleanersCellarBuilder(64, true) : new ReferenceGladeBuilder(64);
            Assert.True(legacy.BuildZone(new Zone(id), content.Factory, new Random(7)), "Optional new content cannot disable the historical builder.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LateCreationCannotTurnTheNewWicketIntoAnAlreadyPaidReward(bool changeRepair)
        {
            FieldworkCreationProbe.ChangeRepair = changeRepair;
            content.Factory.RegisterPartType<FieldworkCreationProbe>();
            content.Factory.Blueprints["GleanersBuckledWicket"].Parts["FieldworkCreationProbe"] = new Dictionary<string, string> { { "Remember", "true" } };
            content.Factory.Blueprints["Signpost"].Parts["FieldworkCreationProbe"] = new Dictionary<string, string>();
            var zone = new Zone(ReferenceGladePlan.ZoneID);
            Assert.AreEqual(!changeRepair, new ReferenceGladeBuilder(64, true).BuildZone(zone, content.Factory, new Random(7)));
            Assert.NotNull(FieldworkCreationProbe.Remembered);
            if (changeRepair) { Assert.Zero(zone.EntityCount); Assert.Zero(zone.GenReservedCells.Count); }
            else Assert.False(FieldworkCreationProbe.Remembered.GetPart<RepairablePart>().Repaired);
        }

        public sealed class FieldworkCreationProbe : Part
        {
            public bool Remember;
            public static Entity Remembered;
            public static bool ChangeRepair;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "ObjectCreated") return true;
                if (Remember) Remembered = ParentEntity;
                else if (ChangeRepair && Remembered != null) Remembered.GetPart<RepairablePart>().Repaired = true;
                return true;
            }
        }

        [Test]
        public void HarvestedGardenAndDismantledPalletStayGoneAfterFullPackSaveAndUnload()
        {
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            var glade = manager.GetZone(ReferenceGladePlan.ZoneID);
            var crop = glade.GetCell(46, 4).Objects.Single(e => e.BlueprintName == "DrawgourdCrop");
            var soil = glade.GetCell(46, 4).Objects.Single(e => e.HasPart<CultivatedSoilPart>());
            var actor = content.Factory.CreateEntity("Player"); actor.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.True(glade.AddEntity(actor, 46, 5)); manager.SetActiveZone(glade); SettlementRuntime.ActiveZone = glade;
            Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(crop, "HarvestCultivatedCrop"), actor, glade).Success);
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(crop, "HarvestCultivatedCrop"), actor, glade).Success);
            var groundProducts = glade.GetCell(46, 4).Objects.Where(e => e.BlueprintName == "DrawgourdShell" || e.BlueprintName == "DrawgourdSeed").ToArray();
            Assert.AreEqual(2, groundProducts.Length);
            Assert.True(CultivatedSoilPart.IsCultivated(glade, glade.GetCell(46, 4)));
            var cellar = manager.GetZone(GleanersCellarBuilder.ZoneID);
            var pallet = cellar.GetReadOnlyEntities().Single(e => e.BlueprintName == "GleanersTimberPallet");
            var anchor = cellar.GetEntityPosition(pallet);
            Assert.True(glade.TryTransferEntityTo(actor, cellar, anchor.x + 1, anchor.y)); manager.SetActiveZone(cellar); SettlementRuntime.ActiveZone = cellar;
            Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(pallet, "Harvest"), actor, cellar).Success);
            Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(pallet, "Harvest"), actor, cellar).Success);
            Assert.AreEqual(2, cellar.GetCell(anchor.x, anchor.y).Objects.Where(e => e.BlueprintName == "SalvagedTimber").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            Assert.Zero(actor.GetPart<InventoryPart>().Objects.Count);
            var session = GameSessionState.Capture("fieldwork-finite-save", "controlled-native-commands", manager, null, actor);
            GameSessionState saved;
            using (var bytes = new MemoryStream())
            {
                session.Save(new SaveWriter(bytes)); bytes.Position = 0;
                saved = GameSessionState.Load(new SaveReader(bytes, content.Factory));
            }
            var restoredGlade = saved.ZoneManager.GetZone(glade.ZoneID);
            var restoredCellar = saved.ZoneManager.GetZone(cellar.ZoneID);
            Assert.AreNotSame(glade, restoredGlade); Assert.AreNotSame(cellar, restoredCellar);
            Assert.False(restoredGlade.GetReadOnlyEntities().Any(e => e.ID == crop.ID));
            Assert.False(restoredCellar.GetReadOnlyEntities().Any(e => e.ID == pallet.ID));
            Assert.True(restoredGlade.GetCell(46, 4).Objects.Any(e => e.ID == soil.ID && e.HasPart<CultivatedSoilPart>()));
            CollectionAssert.AreEquivalent(groundProducts.Select(e => e.ID), restoredGlade.GetCell(46, 4).Objects.Where(e => e.BlueprintName == "DrawgourdShell" || e.BlueprintName == "DrawgourdSeed").Select(e => e.ID));
            Assert.AreEqual(2, restoredCellar.GetCell(anchor.x, anchor.y).Objects.Where(e => e.BlueprintName == "SalvagedTimber").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            saved.ZoneManager.UnloadZone(glade.ZoneID); saved.ZoneManager.UnloadZone(cellar.ZoneID);
            Assert.AreSame(restoredGlade, saved.ZoneManager.GetZone(glade.ZoneID));
            Assert.AreSame(restoredCellar, saved.ZoneManager.GetZone(cellar.ZoneID));
        }
    }
}
