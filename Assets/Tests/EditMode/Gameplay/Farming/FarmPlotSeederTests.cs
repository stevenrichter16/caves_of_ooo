using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops follow-up — FarmPlotSeeder guarantees plantable grass near
    /// the player spawn regardless of biome (Ruins/stone starts have no
    /// Grass; the starter kit's seeds would be unusable). See
    /// <c>Docs/CROPS-WATERING-GRIMOIRE.md §5 SM6</c>.
    ///
    /// Conservative conversion contract pinned here:
    /// - counts existing Plantable cells first; no-ops when enough,
    /// - converts only bare cells or cells whose ONLY terrain is a
    ///   plain replaceable floor (Floor/stone-family/Rubble),
    /// - never touches walls, interiors, water (LiquidPool), or
    ///   special terrain like riverbank Bank entities,
    /// - exactly one terrain entity per converted cell afterwards.
    /// </summary>
    [TestFixture]
    public class FarmPlotSeederTests
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
        }

        // ── Helpers ──────────────────────────────────────────────

        private static void FillTerrain(Zone zone, string blueprint,
            int cx, int cy, int radius)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (!zone.InBounds(x, y)) continue;
                    var terrain = _factory.CreateEntity(blueprint);
                    zone.AddEntity(terrain, x, y);
                }
        }

        private static int CountPlantable(Zone zone, int cx, int cy, int radius)
        {
            int count = 0;
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (!zone.InBounds(x, y)) continue;
                    var cell = zone.GetCell(x, y);
                    for (int i = 0; i < cell.Objects.Count; i++)
                        if (cell.Objects[i].HasTag("Terrain") && cell.Objects[i].HasTag("Plantable"))
                        { count++; break; }
                }
            return count;
        }

        private static int CountTerrainEntities(Cell cell)
        {
            int count = 0;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasTag("Terrain")) count++;
            return count;
        }

        // ── No-op when already plantable ─────────────────────────

        [Test]
        public void AlreadyGrassy_NoChanges()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Grass", 10, 10, 2);
            int before = CountPlantable(zone, 10, 10, 2);
            Assert.GreaterOrEqual(before, 6, "sanity: plenty of grass");

            int ensured = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.AreEqual(before, ensured, "returns the existing count");
            Assert.AreEqual(before, CountPlantable(zone, 10, 10, 2), "no new grass added");
        }

        // ── Conversion paths ─────────────────────────────────────

        [Test]
        public void BareFloorStart_ConvertsToGrass_ExactlyOneTerrainPerCell()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2); // Ruins-style plain floor

            int ensured = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.GreaterOrEqual(ensured, FarmPlotSeeder.MIN_PLANTABLE_CELLS,
                "spawn now has at least the guaranteed plantable plot");
            Assert.GreaterOrEqual(CountPlantable(zone, 10, 10, 2), FarmPlotSeeder.MIN_PLANTABLE_CELLS);

            // Converted cells swapped their floor OUT — no stacked terrains.
            int converted = 0;
            for (int y = 8; y <= 12; y++)
                for (int x = 8; x <= 12; x++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    bool plantable = false;
                    for (int i = 0; i < cell.Objects.Count; i++)
                        if (cell.Objects[i].HasTag("Plantable")) plantable = true;
                    if (!plantable) continue;
                    converted++;
                    Assert.AreEqual(1, CountTerrainEntities(cell),
                        $"converted cell ({x},{y}) must hold exactly ONE terrain entity");
                }
            Assert.GreaterOrEqual(converted, FarmPlotSeeder.MIN_PLANTABLE_CELLS);
        }

        [Test]
        public void EmptyCells_NoTerrainAtAll_GetGrass()
        {
            var zone = new Zone("z"); // fully bare zone

            int ensured = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.GreaterOrEqual(ensured, FarmPlotSeeder.MIN_PLANTABLE_CELLS);
        }

        [Test]
        public void StoneFamilyFloors_AreReplaceable()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "StoneFloor", 10, 10, 2);

            int ensured = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.GreaterOrEqual(ensured, FarmPlotSeeder.MIN_PLANTABLE_CELLS);
        }

        [Test]
        public void SandFloors_AreReplaceable_DesertBiomeGetsItsPlot()
        {
            // SM7 audit finding F5: Sand was missing from the allowlist,
            // yet DesertBuilder floors whole zones with it and the
            // desert Village/Lair palettes use it for floor AND path —
            // the "regardless of biome" guarantee failed on the entire
            // desert family (silently: 0 conversions, no diag).
            var zone = new Zone("z");
            FillTerrain(zone, "Sand", 10, 10, 2);

            int ensured = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.GreaterOrEqual(ensured, FarmPlotSeeder.MIN_PLANTABLE_CELLS,
                "a desert (all-Sand) spawn must still get its plantable plot");
        }

        [Test]
        public void GuaranteeNotMet_EmitsFarmPlotSeedingFailedDiag()
        {
            // SM7 audit note: the failure branch was uninstrumented — a
            // diag query could not tell "healthy grassy no-op" from
            // "seeder ran and failed its guarantee".
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);
            for (int y = 8; y <= 12; y++)
                for (int x = 8; x <= 12; x++)
                    zone.GetCell(x, y).IsInterior = true; // nothing convertible

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "FarmPlotSeedingFailed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "underdelivery must be loud");
            StringAssert.Contains("\"plantableTotal\":0", recs[0].PayloadJson);
            StringAssert.Contains($"\"needed\":{FarmPlotSeeder.MIN_PLANTABLE_CELLS}", recs[0].PayloadJson);
        }

        [Test]
        public void GrassyNoOp_EmitsNeitherSeededNorFailedDiag()
        {
            // Counter-check: the healthy no-op path stays silent on BOTH
            // kinds — FarmPlotSeeded means "converted something",
            // FarmPlotSeedingFailed means "guarantee not met".
            var zone = new Zone("z");
            FillTerrain(zone, "Grass", 10, 10, 2);

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "FarmPlotSeeded", Limit = 5 }).Records.Count);
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "FarmPlotSeedingFailed", Limit = 5 }).Records.Count);
        }

        // ── Protected cells are never touched ────────────────────

        [Test]
        public void WallCells_Skipped()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);
            // Wall off part of the plot area.
            for (int x = 8; x <= 12; x++)
            {
                var wall = _factory.CreateEntity("VineWall");
                zone.AddEntity(wall, x, 9);
            }

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            for (int x = 8; x <= 12; x++)
            {
                var cell = zone.GetCell(x, 9);
                bool plantable = false;
                for (int i = 0; i < cell.Objects.Count; i++)
                    if (cell.Objects[i].HasTag("Plantable")) plantable = true;
                Assert.IsFalse(plantable, $"walled cell ({x},9) must not be converted");
            }
        }

        [Test]
        public void InteriorCells_Skipped()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);
            for (int y = 8; y <= 12; y++)
                for (int x = 8; x <= 12; x++)
                    zone.GetCell(x, y).IsInterior = true;

            int ensured = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.AreEqual(0, ensured, "an all-interior area (inside a building) is never paved");
            Assert.AreEqual(0, CountPlantable(zone, 10, 10, 2));
        }

        [Test]
        public void WaterCells_Skipped()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);
            var water = _factory.CreateEntity("WaterPuddle");
            zone.AddEntity(water, 10, 10);

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            var cell = zone.GetCell(10, 10);
            bool plantable = false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasTag("Plantable")) plantable = true;
            Assert.IsFalse(plantable, "water cells are never converted to grass");
        }

        [Test]
        public void BankTerrain_NotOnReplaceList_Preserved()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);
            // A riverbank inside the plot area — Terrain-tagged but special.
            var cell = zone.GetCell(11, 10);
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
                if (cell.Objects[i].HasTag("Terrain")) zone.RemoveEntity(cell.Objects[i]);
            var bank = _factory.CreateEntity("Bank");
            zone.AddEntity(bank, 11, 10);

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.IsNotNull(zone.GetEntityCell(bank), "Bank entity survives");
            bool plantableHere = false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasTag("Plantable")) plantableHere = true;
            Assert.IsFalse(plantableHere, "riverbank cell not paved over");
        }

        // ── Robustness + idempotency ─────────────────────────────

        [Test]
        public void NullZoneOrFactory_ReturnsZero_NoCrash()
        {
            var zone = new Zone("z");
            Assert.AreEqual(0, FarmPlotSeeder.EnsurePlantablePlot(null, 10, 10, _factory));
            Assert.AreEqual(0, FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, null));
        }

        [Test]
        public void SecondCall_Idempotent_NoAdditionalChanges()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);
            int afterFirst = CountPlantable(zone, 10, 10, 2);
            int secondResult = FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            Assert.AreEqual(afterFirst, CountPlantable(zone, 10, 10, 2),
                "second call converts nothing further");
            Assert.AreEqual(afterFirst, secondResult);
        }

        // ── Integration: the seeded plot actually accepts seeds ──

        [Test]
        public void PlantingWorks_OnSeededPlot()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);
            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            // Find a converted cell and plant there via the real command path.
            int px = -1, py = -1;
            for (int y = 8; y <= 12 && px < 0; y++)
                for (int x = 8; x <= 12 && px < 0; x++)
                {
                    var c = zone.GetCell(x, y);
                    if (c == null) continue;
                    for (int i = 0; i < c.Objects.Count; i++)
                        if (c.Objects[i].HasTag("Plantable")) { px = x; py = y; break; }
                }
            Assert.GreaterOrEqual(px, 0, "sanity: a converted cell exists");

            SeedPart.Factory = _factory;
            try
            {
                var actor = new Entity { ID = "farmer", BlueprintName = "TestActor" };
                actor.Tags["Creature"] = "";
                actor.AddPart(new RenderPart { DisplayName = "farmer" });
                actor.AddPart(new PhysicsPart { Solid = true });
                actor.AddPart(new InventoryPart { MaxWeight = 150 });
                zone.AddEntity(actor, px, py);
                var seed = _factory.CreateEntity("CandyCarrotSeed");
                actor.GetPart<InventoryPart>().AddObject(seed);

                var result = InventorySystem.ExecuteCommand(
                    new CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand(seed, "PlantSeed"),
                    actor, zone);

                Assert.IsTrue(result.Success, result.ErrorMessage);
                Assert.IsTrue(zone.GetCell(px, py).HasObjectWithPart<CropPart>(),
                    "seeded plot accepts a real planting end-to-end");
            }
            finally
            {
                SeedPart.Factory = null;
            }
        }

        [Test]
        public void Conversion_EmitsFarmPlotSeededDiag()
        {
            var zone = new Zone("z");
            FillTerrain(zone, "Floor", 10, 10, 2);

            FarmPlotSeeder.EnsurePlantablePlot(zone, 10, 10, _factory);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "FarmPlotSeeded", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"converted\":", recs[0].PayloadJson);
        }
    }
}
