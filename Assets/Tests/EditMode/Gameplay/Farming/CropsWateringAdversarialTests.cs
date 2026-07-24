using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Dedicated adversarial sweep for Docs/CROPS-WATERING-GRIMOIRE.md
    /// (CLAUDE.md's mandatory gate — this feature touches the CSV
    /// parser, stacking/top-up semantics, save/load reach, boundary
    /// inputs, diag contracts, and factory-null paths). Complements,
    /// does NOT duplicate, the per-SM suites: each of those proves a
    /// gate in isolation; this file probes malformed content, hostile
    /// values, zone edges, and cross-feature composition.
    /// </summary>
    [TestFixture]
    public class CropsWateringAdversarialTests
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
            AsciiFxBus.Clear();
            CropSystem.Factory = _factory;
        }

        [TearDown]
        public void TearDown()
        {
            CropSystem.Factory = null;
            SeedPart.Factory = null;
        }

        // ── Helpers ──────────────────────────────────────────────

        private static Entity MakeCaster(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "caster", BlueprintName = "TestCaster" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "caster" });
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new MutationsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static ConjureRainMutation GrantRain(Entity caster)
        {
            var rain = new ConjureRainMutation();
            caster.GetPart<MutationsPart>().AddMutation(rain, 1);
            return rain;
        }

        /// <summary>Synthetic crop with fully controllable (possibly
        /// hostile) CropPart values.</summary>
        private static Entity MakeSyntheticCrop(Zone zone, int x, int y,
            string glyphs = ".,t", string colors = "&w,&g",
            int ticksPerStage = 5, string yield = "CandyCarrot", int yieldCount = 1,
            bool withRender = true)
        {
            var e = new Entity { ID = $"crop_{x}_{y}", BlueprintName = "SyntheticCrop" };
            e.Tags["Crop"] = "";
            if (withRender)
                e.AddPart(new RenderPart { DisplayName = "synthetic crop", RenderString = ".", ColorString = "&w" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.AddPart(new CropPart
            {
                StageGlyphsRaw = glyphs,
                StageColorsRaw = colors,
                TicksPerStage = ticksPerStage,
                YieldBlueprint = yield,
                YieldCount = yieldCount
            });
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP A — CSV parser malformed inputs
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EmptyGlyphCsv_StageAdvance_KeepsOldGlyph_NoCrash()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, glyphs: "", colors: "");
            crop.GetPart<CropPart>().Water(100);

            Assert.DoesNotThrow(() => { for (int i = 0; i < 5; i++) CropSystem.OnTickEnd(zone); });

            Assert.AreEqual(1, crop.GetPart<CropPart>().GrowthStage, "stage still advances");
            Assert.AreEqual(".", crop.GetPart<RenderPart>().RenderString,
                "missing CSV entry: glyph unchanged, not corrupted");
        }

        [Test]
        public void Adversarial_OnlyCommasCsv_YieldsSentinels_NoCrash()
        {
            var crop = new CropPart { StageGlyphsRaw = ",,,", StageColorsRaw = ",,," };
            Assert.AreEqual('\0', crop.GlyphForStage(0));
            Assert.AreEqual('\0', crop.GlyphForStage(1));
            Assert.IsNull(crop.ColorForStage(0));
        }

        [Test]
        public void Adversarial_WhitespaceCsvEntries_Trimmed()
        {
            var crop = new CropPart { StageGlyphsRaw = " . , t ", StageColorsRaw = " &w , &g " };
            Assert.AreEqual('.', crop.GlyphForStage(0));
            Assert.AreEqual('t', crop.GlyphForStage(1));
            Assert.AreEqual("&w", crop.ColorForStage(0));
            Assert.AreEqual("&g", crop.ColorForStage(1));
        }

        [Test]
        public void Adversarial_NegativeStageIndex_Sentinel_NoCrash()
        {
            var crop = new CropPart { StageGlyphsRaw = ".,t" };
            Assert.AreEqual('\0', crop.GlyphForStage(-1));
            Assert.IsNull(crop.ColorForStage(-3));
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP B — Water/top-up boundary values
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_WaterZeroOrNegative_NoOp_NoWetBg()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5);
            var part = crop.GetPart<CropPart>();

            part.Water(0);
            part.Water(-40);

            Assert.AreEqual(0, part.MoistureTicks);
            Assert.IsTrue(string.IsNullOrEmpty(crop.GetPart<RenderPart>().BackgroundColor),
                "no wet bg from a no-op watering");
        }

        [Test]
        public void Adversarial_WateringMidStage_DoesNotResetProgress()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, ticksPerStage: 10);
            var part = crop.GetPart<CropPart>();
            part.Water(100);
            for (int i = 0; i < 4; i++) CropSystem.OnTickEnd(zone);
            Assert.AreEqual(4, part.TicksInStage);

            part.Water(100); // re-water mid-stage

            Assert.AreEqual(4, part.TicksInStage, "re-watering must not reset growth progress");
        }

        [Test]
        public void Adversarial_CropWithoutRenderPart_WaterAndGrowth_NoCrash()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, withRender: false);
            var part = crop.GetPart<CropPart>();

            Assert.DoesNotThrow(() => part.Water(10));
            Assert.DoesNotThrow(() => { for (int i = 0; i < 12; i++) CropSystem.OnTickEnd(zone); });
            Assert.DoesNotThrow(() => part.OnDriedOut());
        }

        [Test]
        public void Adversarial_HugeMoisture_TickDecrement_NoOverflow()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, ticksPerStage: int.MaxValue);
            var part = crop.GetPart<CropPart>();
            part.MoistureTicks = int.MaxValue;

            Assert.DoesNotThrow(() => { for (int i = 0; i < 10; i++) CropSystem.OnTickEnd(zone); });
            Assert.AreEqual(int.MaxValue - 10, part.MoistureTicks);
            Assert.AreEqual(10, part.TicksInStage, "TicksInStage advances without overflow");
        }

        [Test]
        public void Adversarial_NegativeMoisture_CorruptedSave_TreatedAsDry_NoCrash()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5);
            crop.GetPart<CropPart>().MoistureTicks = -100;

            Assert.DoesNotThrow(() => { for (int i = 0; i < 5; i++) CropSystem.OnTickEnd(zone); });
            Assert.AreEqual(0, crop.GetPart<CropPart>().TicksInStage, "negative moisture = dry = paused");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP C — Malformed growth params
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ZeroTicksPerStage_ConvertsFast_Terminates_NoCrash()
        {
            // TicksPerStage=0 (malformed content): TicksInStage >= 0 is
            // true every moist tick, so the crop rushes through both
            // stages. Must terminate cleanly (convert to produce), not
            // loop or crash.
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, ticksPerStage: 0);
            crop.GetPart<CropPart>().Water(100);

            Assert.DoesNotThrow(() => { for (int i = 0; i < 5; i++) CropSystem.OnTickEnd(zone); });

            Assert.IsNull(zone.GetEntityCell(crop), "rushed to maturity and converted");
            int produce = 0;
            var cell = zone.GetCell(5, 5);
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].BlueprintName == "CandyCarrot") produce++;
            Assert.AreEqual(1, produce);
        }

        [Test]
        public void Adversarial_ZeroYieldCount_CropHeldWithMatureBlockedDiag_NotSilentlyDeleted()
        {
            // YieldCount=0 (malformed content): nothing can spawn, so the
            // crop is HELD (loudly, via MatureBlocked diag) rather than
            // deleted-with-no-produce. A stuck crop is visible and
            // debuggable; a vanished harvest is not.
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, ticksPerStage: 2, yieldCount: 0);
            crop.GetPart<CropPart>().Water(100);

            for (int i = 0; i < 10; i++) CropSystem.OnTickEnd(zone);

            Assert.IsNotNull(zone.GetEntityCell(crop), "crop held, not deleted");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "MatureBlocked", Limit = 50 }).Records;
            Assert.Greater(recs.Count, 0, "held state is loud (MatureBlocked diag)");
        }

        [Test]
        public void Adversarial_UnknownYieldBlueprint_CropHeld_NoCrash()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, ticksPerStage: 2, yield: "NoSuchProduce");
            crop.GetPart<CropPart>().Water(100);

            // EntityFactory logs an [Error] per failed create, once per
            // retry tick — the exact count depends on the retry cadence,
            // so suppress log-failure matching rather than pin a brittle
            // count. The behavioral assertion (held, not deleted) is the
            // real contract.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() => { for (int i = 0; i < 10; i++) CropSystem.OnTickEnd(zone); });
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }

            Assert.IsNotNull(zone.GetEntityCell(crop), "held, not deleted");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP D — Zone-edge boundaries (rain FX above the map top)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_RainCastAtZoneCorner_NoCrash_CropsStillWatered()
        {
            var zone = new Zone("z");
            var caster = MakeCaster(zone, 0, 0);
            var rain = GrantRain(caster);
            var crop = MakeSyntheticCrop(zone, 1, 0); // adjacent to corner: y-2 is out of bounds for FX

            Assert.DoesNotThrow(() => rain.Cast(zone, zone.GetCell(0, 0)));

            Assert.Greater(crop.GetPart<CropPart>().MoistureTicks, 0,
                "corner cast still waters the crop");
            // FX drops above the map top are silently dropped by
            // EmitParticle's InBounds guard; the splash at (1,0) survives.
            var requests = AsciiFxBus.Drain();
            int bursts = 0;
            foreach (var r in requests)
                if (r.Type == AsciiFxRequestType.Burst) bursts++;
            Assert.AreEqual(1, bursts, "splash still lands even when the sky is off-map");
        }

        [Test]
        public void Adversarial_CropAtZoneEdge_Matures_ProduceInSameCell()
        {
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 0, 0, ticksPerStage: 2);
            crop.GetPart<CropPart>().Water(100);

            for (int i = 0; i < 4; i++) CropSystem.OnTickEnd(zone);

            Assert.IsNull(zone.GetEntityCell(crop));
            var cell = zone.GetCell(0, 0);
            bool hasProduce = false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].BlueprintName == "CandyCarrot") hasProduce = true;
            Assert.IsTrue(hasProduce, "produce lands in the corner cell");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP E — Cross-feature composition + diag contracts
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoCropsOneCell_OnlyFirstWatered_ByDesign()
        {
            // The planting gate enforces one crop per cell; if content or
            // a future spawner bypasses it, rain waters only the FIRST
            // crop found (documented assumption, pinned here so a change
            // is visible).
            var zone = new Zone("z");
            var caster = MakeCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            var first = MakeSyntheticCrop(zone, 11, 10);
            var second = MakeSyntheticCrop(zone, 11, 10);

            rain.Cast(zone, zone.GetCell(10, 10));

            Assert.Greater(first.GetPart<CropPart>().MoistureTicks, 0);
            Assert.AreEqual(0, second.GetPart<CropPart>().MoistureTicks,
                "second stacked crop untouched — one-crop-per-cell assumption pinned");
        }

        [Test]
        public void Adversarial_FullLifecycle_DiagPipelineComplete()
        {
            // Plant (real seed) → rain → grow → mature: every gate's diag
            // record appears exactly as contracted, in one integration run.
            var zone = new Zone("z");
            SeedPart.Factory = _factory;
            var grass = _factory.CreateEntity("Grass");
            zone.AddEntity(grass, 10, 11);

            var caster = MakeCaster(zone, 10, 10);
            caster.AddPart(new InventoryPart { MaxWeight = 150 });
            var rain = GrantRain(caster);
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            caster.GetPart<InventoryPart>().AddObject(seed);

            // Plant one cell south (actor stands there for the plant).
            zone.MoveEntity(caster, 10, 11);
            InventorySystem.ExecuteCommand(
                new CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand(seed, "PlantSeed"),
                caster, zone);
            zone.MoveEntity(caster, 10, 10);

            rain.Cast(zone, zone.GetCell(10, 10));
            for (int i = 0; i < 40; i++) CropSystem.OnTickEnd(zone);
            rain.Cast(zone, zone.GetCell(10, 10)); // top-up covers the 40-tick lifecycle
            for (int i = 0; i < 40; i++) CropSystem.OnTickEnd(zone);

            string[] expectedKinds = { "CropPlanted", "RainConjured", "CropWatered", "StageAdvanced", "CropMatured" };
            foreach (var kind in expectedKinds)
            {
                var recs = DiagQuery.Apply(new DiagQuery.Filter
                { Category = "crop", Kind = kind, Limit = 100 }).Records;
                Assert.Greater(recs.Count, 0, $"lifecycle diag '{kind}' must appear");
            }
        }

        [Test]
        public void Adversarial_SecondRainAfterMaturity_ZeroWatered_NoFx()
        {
            var zone = new Zone("z");
            var caster = MakeCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            var crop = MakeSyntheticCrop(zone, 11, 10, ticksPerStage: 1);
            crop.GetPart<CropPart>().Water(100);
            for (int i = 0; i < 3; i++) CropSystem.OnTickEnd(zone); // matures fast
            Assert.IsNull(zone.GetEntityCell(crop), "sanity: converted");
            AsciiFxBus.Clear();
            Diag.ResetAll();

            rain.Cast(zone, zone.GetCell(10, 10));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "RainConjured", Limit = 5 }).Records;
            StringAssert.Contains("\"cropsWatered\":0", recs[0].PayloadJson,
                "produce items are not crops — rain ignores them");
            Assert.AreEqual(0, AsciiFxBus.Drain().Count);
        }

        [Test]
        public void Adversarial_RainConjured_ExactlyOncePerCast_CropWateredOncePerCrop()
        {
            var zone = new Zone("z");
            var caster = MakeCaster(zone, 10, 10);
            var rain = GrantRain(caster);
            MakeSyntheticCrop(zone, 9, 10);
            MakeSyntheticCrop(zone, 11, 10);

            rain.Cast(zone, zone.GetCell(10, 10));

            var conjured = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "RainConjured", Limit = 10 }).Records;
            var watered = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "CropWatered", Limit = 10 }).Records;
            Assert.AreEqual(1, conjured.Count, "one RainConjured per cast");
            Assert.AreEqual(2, watered.Count, "one CropWatered per crop");
        }

        [Test]
        public void Adversarial_EmptyYieldBlueprint_HeldWithDistinctReason()
        {
            // Cold-eye Q3 gap: the no_yield_blueprint branch of
            // MatureBlocked was implemented but unpinned (its sibling
            // no_factory is pinned in CropGrowthTests).
            var zone = new Zone("z");
            var crop = MakeSyntheticCrop(zone, 5, 5, ticksPerStage: 2, yield: "");
            crop.GetPart<CropPart>().Water(100);

            for (int i = 0; i < 5; i++) CropSystem.OnTickEnd(zone);

            Assert.IsNotNull(zone.GetEntityCell(crop), "held, not deleted");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "MatureBlocked", Limit = 10 }).Records;
            Assert.Greater(recs.Count, 0);
            StringAssert.Contains("\"reason\":\"no_yield_blueprint\"", recs[0].PayloadJson);
        }

        [Test]
        public void Adversarial_PlantWithNullZone_Rejects_NoZone_NoCrash()
        {
            // Cold-eye Q4 check: the plan doc's no_zone gate — planting
            // with a null zone (and no SettlementRuntime.ActiveZone in
            // EditMode) must reject cleanly, not NRE.
            SeedPart.Factory = _factory;
            var actor = new Entity { ID = "a", BlueprintName = "TestActor" };
            actor.AddPart(new RenderPart { DisplayName = "actor" });
            actor.AddPart(new InventoryPart { MaxWeight = 150 });
            var seed = _factory.CreateEntity("CandyCarrotSeed");
            actor.GetPart<InventoryPart>().AddObject(seed);

            Assert.DoesNotThrow(() => InventorySystem.ExecuteCommand(
                new CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand(seed, "PlantSeed"),
                actor, null));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "crop", Kind = "PlantRejected", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"reason\":\"no_zone\"", recs[0].PayloadJson);
            Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(seed),
                "seed not consumed on reject");
        }

        // ════════════════════════════════════════════════════════════
        //   GROUP F — Save/load reach (adversarial values)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ExtremeFieldValues_RoundTrip()
        {
            var crop = _factory.CreateEntity("CandyCarrotCrop");
            var part = crop.GetPart<CropPart>();
            part.GrowthStage = 1;
            part.TicksInStage = int.MaxValue - 1;
            part.MoistureTicks = int.MaxValue;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(crop);
            var loadedPart = loaded.GetPart<CropPart>();

            Assert.AreEqual(int.MaxValue - 1, loadedPart.TicksInStage);
            Assert.AreEqual(int.MaxValue, loadedPart.MoistureTicks);
        }
    }
}
