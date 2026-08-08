using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Crops SM2 — CropSystem growth ticking.
    /// See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.3</c>.
    ///
    /// Drives <see cref="CropSystem.OnTickEnd"/> directly (the
    /// GasSystemTests loop pattern) — the TickEnd event wiring itself is
    /// SM4's CropSystemPart, pinned separately.
    /// </summary>
    [TestFixture]
    public class CropGrowthTests
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
            CropSystem.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            CropSystem.Factory = null;
        }

        // ── Helpers ──────────────────────────────────────────────

        private static Entity PlantCrop(Zone zone, string blueprint, int x, int y)
        {
            var crop = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(crop, $"Blueprint '{blueprint}' must exist.");
            zone.AddEntity(crop, x, y);
            return crop;
        }

        private static void Tick(Zone zone, int times)
        {
            for (int i = 0; i < times; i++)
                CropSystem.OnTickEnd(zone);
        }

        private static int CountInCell(Zone zone, int x, int y, string blueprintName)
        {
            int count = 0;
            var cell = zone.GetCell(x, y);
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].BlueprintName == blueprintName) count++;
            return count;
        }

        // ── Moisture gating ──────────────────────────────────────

        [Test]
        public void DryCrop_NeverAdvances()
        {
            var zone = new Zone("z");
            var crop = PlantCrop(zone, "CandyCarrotCrop", 5, 5).GetPart<CropPart>();
            Assert.AreEqual(0, crop.MoistureTicks, "sanity: starts dry");

            Tick(zone, 100);

            Assert.AreEqual(0, crop.GrowthStage, "dry crop must not grow");
            Assert.AreEqual(0, crop.TicksInStage, "dry crop accumulates zero progress");
        }

        [Test]
        public void WateredCrop_AdvancesStage_AtExactThreshold_NotBefore()
        {
            var zone = new Zone("z");
            var cropEntity = PlantCrop(zone, "CandyCarrotCrop", 5, 5);
            var crop = cropEntity.GetPart<CropPart>();
            crop.Water(100); // plenty of moisture, TicksPerStage = 20

            Tick(zone, 19);
            Assert.AreEqual(0, crop.GrowthStage, "one tick short of threshold: still seed");

            Tick(zone, 1);
            Assert.AreEqual(1, crop.GrowthStage, "20th tick advances seed -> sprout");
            Assert.AreEqual(0, crop.TicksInStage, "stage progress resets on advance");
        }

        [Test]
        public void StageAdvance_SwapsGlyphAndColor_FromCsvs()
        {
            var zone = new Zone("z");
            var cropEntity = PlantCrop(zone, "CandyCarrotCrop", 5, 5);
            var crop = cropEntity.GetPart<CropPart>();
            var render = cropEntity.GetPart<RenderPart>();
            Assert.AreEqual(".", render.RenderString, "sanity: seed glyph");

            crop.Water(100);
            Tick(zone, 20);

            Assert.AreEqual("t", render.RenderString, "sprout glyph from StageGlyphsRaw[1]");
            Assert.AreEqual("&g", render.ColorString, "sprout color from StageColorsRaw[1]");
        }

        [Test]
        public void Moisture_DecrementsOncePerTick_AndGrowthStopsWhenDry()
        {
            var zone = new Zone("z");
            var crop = PlantCrop(zone, "CandyCarrotCrop", 5, 5).GetPart<CropPart>();
            crop.Water(5); // less moisture than the 20-tick stage

            Tick(zone, 5);
            Assert.AreEqual(0, crop.MoistureTicks, "5 ticks drained 5 moisture");
            Assert.AreEqual(5, crop.TicksInStage, "5 ticks of progress accumulated");

            Tick(zone, 50);
            Assert.AreEqual(5, crop.TicksInStage,
                "progress frozen once dry — crop paused, not dead");
            Assert.AreEqual(0, crop.GrowthStage);
        }

        [Test]
        public void DryOut_ClearsWetSoilBackground_ExactlyOnce_WithDiag()
        {
            var zone = new Zone("z");
            var cropEntity = PlantCrop(zone, "CandyCarrotCrop", 5, 5);
            var crop = cropEntity.GetPart<CropPart>();
            var render = cropEntity.GetPart<RenderPart>();

            crop.Water(3);
            Assert.AreEqual(CropPart.WET_SOIL_BG, render.BackgroundColor);
            Diag.ResetAll(); // isolate the dry-out record

            Tick(zone, 3);
            Assert.IsTrue(string.IsNullOrEmpty(render.BackgroundColor),
                "wet bg cleared the tick moisture hits 0");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "SoilDried", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "SoilDried fires exactly once");

            Tick(zone, 10);
            recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "SoilDried", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "no repeat SoilDried while already dry");
        }

        // ── Maturity: produce replacement ────────────────────────

        [Test]
        public void SproutCompletion_ReplacesCropWithProduce()
        {
            var zone = new Zone("z");
            var cropEntity = PlantCrop(zone, "CandyCarrotCrop", 5, 5);
            cropEntity.GetPart<CropPart>().Water(100);

            Tick(zone, 40); // 20 (seed->sprout) + 20 (sprout->done)

            Assert.IsNull(zone.GetEntityCell(cropEntity),
                "crop entity removed from the zone at maturity");
            Assert.AreEqual(2, CountInCell(zone, 5, 5, "CandyCarrot"),
                "exactly YieldCount(2) produce in the cell (ALPHA economy: yield bumped 1->2)");

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropMatured", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"yieldBlueprint\":\"CandyCarrot\"", recs[0].PayloadJson);
        }

        [Test]
        public void Emberwheat_YieldsTwoProduce_AtItsSlowerPace()
        {
            var zone = new Zone("z");
            var cropEntity = PlantCrop(zone, "EmberwheatCrop", 5, 5);
            cropEntity.GetPart<CropPart>().Water(100);

            Tick(zone, 69);
            Assert.IsNotNull(zone.GetEntityCell(cropEntity),
                "one tick short (35+35=70): still growing");

            Tick(zone, 1);
            Assert.IsNull(zone.GetEntityCell(cropEntity));
            Assert.AreEqual(2, CountInCell(zone, 5, 5, "Emberwheat"),
                "YieldCount(2) produce items dropped");
        }

        [Test]
        public void StageAdvance_EmitsStageAdvancedDiag()
        {
            var zone = new Zone("z");
            PlantCrop(zone, "CandyCarrotCrop", 5, 5).GetPart<CropPart>().Water(100);

            Tick(zone, 20);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "StageAdvanced", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"stage\":1", recs[0].PayloadJson);
        }

        // ── Robustness ───────────────────────────────────────────

        [Test]
        public void OnTickEnd_NullZone_NoCrash()
        {
            Assert.DoesNotThrow(() => CropSystem.OnTickEnd(null));
        }

        [Test]
        public void OnTickEnd_ZoneWithNoCrops_NoCrash_NoDiag()
        {
            var zone = new Zone("z");
            Assert.DoesNotThrow(() => Tick(zone, 10));
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Limit = 5 }).Records;
            Assert.AreEqual(0, recs.Count);
        }

        [Test]
        public void Maturity_NullFactory_CropSurvives_RetriesNextTick()
        {
            // Factory lost (e.g. domain edge case): the crop must NOT be
            // deleted with no produce — it stays at the completion
            // boundary and converts as soon as the factory returns.
            var zone = new Zone("z");
            var cropEntity = PlantCrop(zone, "CandyCarrotCrop", 5, 5);
            cropEntity.GetPart<CropPart>().Water(100);
            Tick(zone, 39); // one short of full maturity

            CropSystem.Factory = null;
            Tick(zone, 1);
            Assert.IsNotNull(zone.GetEntityCell(cropEntity),
                "no factory: crop NOT deleted (produce would be lost)");
            Assert.AreEqual(0, CountInCell(zone, 5, 5, "CandyCarrot"));

            CropSystem.Factory = _factory;
            Tick(zone, 1);
            Assert.IsNull(zone.GetEntityCell(cropEntity), "converts once factory is back");
            Assert.AreEqual(2, CountInCell(zone, 5, 5, "CandyCarrot")); // ALPHA economy: yield 1->2
        }

        [Test]
        public void TwoCrops_TickIndependently()
        {
            var zone = new Zone("z");
            var fast = PlantCrop(zone, "CandyCarrotCrop", 3, 3);
            var slow = PlantCrop(zone, "EmberwheatCrop", 7, 7);
            fast.GetPart<CropPart>().Water(100);
            slow.GetPart<CropPart>().Water(100);

            Tick(zone, 20);

            Assert.AreEqual(1, fast.GetPart<CropPart>().GrowthStage, "carrot sprouted at 20");
            Assert.AreEqual(0, slow.GetPart<CropPart>().GrowthStage, "wheat (35/stage) still seed");
        }
    }
}
