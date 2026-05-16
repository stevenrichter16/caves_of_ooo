using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LQ.3 — the pool entity layer. <see cref="LiquidPoolPart"/>
    /// carries (LiquidId, Volume) on a puddle entity and derives its
    /// render glyph/color from <see cref="LiquidRegistry"/> on
    /// Initialize (review finding F1: data-driven, mirrors
    /// MaterialPart.Initialize pushing tags). Save round-trips via
    /// reflection (plan §A5 — no FormatVersion bump).
    ///
    /// Also folds the three LQ.2 critical-review findings:
    ///   F3 — LiquidDefinition flyweight immutability contract pin
    ///   F4 — JsonUtility omitted-fields-keep-C#-defaults pin
    ///   F5 — within-file duplicate-Id later-wins pin
    ///
    /// Test discipline (plan §B1): bare Entity + inline-JSON
    /// EntityFactory; no Zone / OverworldZoneManager.
    /// </summary>
    public class LiquidPoolPartTests
    {
        [TearDown]
        public void TearDown() => LiquidRegistry.ResetForTests();

        private const string SeedJson = @"{
          ""Liquids"": [
            { ""Id"": ""water"", ""DisplayName"": ""water"", ""Adjective"": ""wet"",
              ""Glyph"": ""~"", ""Color"": ""&c"", ""Conductivity"": 100,
              ""Combustibility"": -50, ""FireDampen"": 40 },
            { ""Id"": ""oil"", ""DisplayName"": ""oil"", ""Adjective"": ""oily"",
              ""Glyph"": ""~"", ""Color"": ""&K"", ""Combustibility"": 90,
              ""FlameTemperature"": 250 },
            { ""Id"": ""acid"", ""DisplayName"": ""acid"", ""Adjective"": ""acid-covered"",
              ""Glyph"": ""~"", ""Color"": ""&G"",
              ""PerTurnDamage"": { ""Amount"": 3, ""Type"": ""Acid"" } }
          ]
        }";

        // Minimal blueprint set: a Terrain base + three pool blueprints
        // wiring the LiquidPool part. EntityFactory auto-registers Part
        // subclasses by Name, so "LiquidPool" resolves to LiquidPoolPart
        // with no registration-table edit (the expand-by-data property).
        private const string BlueprintsJson = @"{
          ""Objects"": [
            { ""Name"": ""Terrain"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""RenderString"", ""Value"": ""?"" } ] }
              ], ""Stats"": [], ""Tags"": [ { ""Key"": ""Terrain"", ""Value"": """" } ] },
            { ""Name"": ""WaterPuddle"", ""Inherits"": ""Terrain"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""puddle of water"" }, { ""Key"": ""RenderString"", ""Value"": ""#"" }, { ""Key"": ""ColorString"", ""Value"": ""&w"" } ] },
                { ""Name"": ""LiquidPool"", ""Params"": [ { ""Key"": ""LiquidId"", ""Value"": ""water"" }, { ""Key"": ""Volume"", ""Value"": ""120"" } ] }
              ], ""Stats"": [], ""Tags"": [ { ""Key"": ""Terrain"", ""Value"": """" } ] },
            { ""Name"": ""OilSlick"", ""Inherits"": ""Terrain"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""oil slick"" }, { ""Key"": ""RenderString"", ""Value"": ""#"" } ] },
                { ""Name"": ""LiquidPool"", ""Params"": [ { ""Key"": ""LiquidId"", ""Value"": ""oil"" }, { ""Key"": ""Volume"", ""Value"": ""80"" } ] }
              ], ""Stats"": [], ""Tags"": [ { ""Key"": ""Terrain"", ""Value"": """" } ] },
            { ""Name"": ""AcidPool"", ""Inherits"": ""Terrain"", ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""pool of acid"" }, { ""Key"": ""RenderString"", ""Value"": ""#"" } ] },
                { ""Name"": ""LiquidPool"", ""Params"": [ { ""Key"": ""LiquidId"", ""Value"": ""acid"" }, { ""Key"": ""Volume"", ""Value"": ""60"" } ] }
              ], ""Stats"": [], ""Tags"": [ { ""Key"": ""Terrain"", ""Value"": """" } ] }
          ]
        }";

        private static EntityFactory MakeFactory()
        {
            var f = new EntityFactory();
            f.LoadBlueprints(BlueprintsJson);
            return f;
        }

        // ── Blueprint → LiquidPoolPart wiring ──────────────────

        [Test]
        public void WaterPuddleBlueprint_HasLiquidPoolPart_WithIdAndVolume()
        {
            LiquidRegistry.Initialize(SeedJson);
            var e = MakeFactory().CreateEntity("WaterPuddle");
            var pool = e.GetPart<LiquidPoolPart>();
            Assert.IsNotNull(pool, "WaterPuddle blueprint must wire a LiquidPool part.");
            Assert.AreEqual("water", pool.LiquidId);
            Assert.AreEqual(120, pool.Volume);
        }

        [Test]
        public void OilAndAcidPoolBlueprints_WireCorrectLiquidIds()
        {
            LiquidRegistry.Initialize(SeedJson);
            var f = MakeFactory();
            Assert.AreEqual("oil", f.CreateEntity("OilSlick").GetPart<LiquidPoolPart>().LiquidId);
            Assert.AreEqual("acid", f.CreateEntity("AcidPool").GetPart<LiquidPoolPart>().LiquidId);
        }

        // ── Initialize derives render from the definition (F1) ──

        [Test]
        public void Initialize_DerivesRenderGlyphAndColorFromDefinition()
        {
            LiquidRegistry.Initialize(SeedJson);
            var e = MakeFactory().CreateEntity("WaterPuddle");
            var render = e.GetPart<RenderPart>();
            Assert.IsNotNull(render);
            // Blueprint authored "#"/"&w" but the LiquidPool part should
            // overwrite from the water definition's Glyph/Color on Init.
            Assert.AreEqual("~", render.RenderString,
                "LiquidPool.Initialize should derive glyph from the liquid definition.");
            Assert.AreEqual("&c", render.ColorString);
        }

        [Test]
        public void Initialize_OilSlick_DerivesOilColor()
        {
            LiquidRegistry.Initialize(SeedJson);
            var e = MakeFactory().CreateEntity("OilSlick");
            Assert.AreEqual("&K", e.GetPart<RenderPart>().ColorString);
        }

        // ── Null-safety (F2) ────────────────────────────────────

        [Test]
        public void Initialize_RegistryNotInitialized_DoesNotThrow_LeavesRenderAuthored()
        {
            // Bare path: registry never Initialize()d (e.g. a test that
            // doesn't load liquids). LiquidPool.Initialize must no-op
            // gracefully and leave the blueprint-authored render intact.
            // (No LiquidRegistry.Initialize call here.)
            var e = MakeFactory().CreateEntity("WaterPuddle");
            var render = e.GetPart<RenderPart>();
            Assert.IsNotNull(render);
            Assert.AreEqual("#", render.RenderString,
                "With no registry, the blueprint-authored glyph must survive.");
            Assert.AreEqual("&w", render.ColorString);
        }

        [Test]
        public void Initialize_UnknownLiquidId_DoesNotThrow()
        {
            LiquidRegistry.Initialize(SeedJson);
            var e = new Entity { ID = "weird", BlueprintName = "weird" };
            e.AddPart(new RenderPart { RenderString = "?", ColorString = "&y" });
            Assert.DoesNotThrow(() =>
                e.AddPart(new LiquidPoolPart { LiquidId = "plasma", Volume = 10 }));
            // Unknown id → render left as authored, no crash.
            Assert.AreEqual("?", e.GetPart<RenderPart>().RenderString);
        }

        [Test]
        public void Construct_NullLiquidId_NoCrashOnInitialize()
        {
            LiquidRegistry.Initialize(SeedJson);
            var e = new Entity { ID = "x", BlueprintName = "x" };
            e.AddPart(new RenderPart { RenderString = "?" });
            Assert.DoesNotThrow(() => e.AddPart(new LiquidPoolPart { LiquidId = null }));
        }

        // ── Save round-trip via reflection (plan §A5) ──────────

        [Test]
        public void LiquidPoolPart_RoundTrips_ViaReflection_NoFormatBump()
        {
            var e = new Entity { ID = "pool", BlueprintName = "WaterPuddle" };
            e.AddPart(new LiquidPoolPart { LiquidId = "acid", Volume = 47 });

            Entity loaded = PartRoundTripHelper.RoundTripEntity(e);

            var pool = loaded.GetPart<LiquidPoolPart>();
            Assert.IsNotNull(pool, "LiquidPoolPart must survive save/load reflection.");
            Assert.AreEqual("acid", pool.LiquidId);
            Assert.AreEqual(47, pool.Volume);
        }

        // ── Counter-check ──────────────────────────────────────

        [Test]
        public void NonPoolEntity_HasNoLiquidPoolPart()
        {
            LiquidRegistry.Initialize(SeedJson);
            var rock = new Entity { ID = "rock", BlueprintName = "rock" };
            rock.AddPart(new RenderPart { RenderString = "o" });
            Assert.IsNull(rock.GetPart<LiquidPoolPart>());
        }

        // ════════════════════════════════════════════════════════
        // Folded LQ.2 critical-review findings
        // ════════════════════════════════════════════════════════

        // F3 — LiquidDefinition is a shared flyweight. Mutating a
        // Get() result corrupts it process-wide; the only legitimate
        // writer is (re-)Initialize. This pins the contract: callers
        // must treat Get() as read-only.
        [Test]
        public void F3_LiquidDefinition_IsSharedFlyweight_ReInitIsTheOnlyWriter()
        {
            LiquidRegistry.Initialize(SeedJson);
            var water = LiquidRegistry.Get("water");
            int canonical = water.Combustibility; // -50

            // A buggy consumer mutates the shared flyweight.
            water.Combustibility = 999;
            Assert.AreEqual(999, LiquidRegistry.Get("water").Combustibility,
                "Documents the hazard: Get() returns the SHARED instance — " +
                "mutating it corrupts every reader. LQ.5+ MUST copy scalars out.");

            // Re-Initialize is the contract's reset path.
            LiquidRegistry.Initialize(SeedJson);
            Assert.AreEqual(canonical, LiquidRegistry.Get("water").Combustibility,
                "Re-Initialize restores canonical values (the only writer).");
        }

        // F4 — JsonUtility constructs T (running field initializers)
        // then overwrites only present fields, so omitted fields keep
        // the C# default. A regression here makes water with no
        // FlameTemperature read 0 → instantly flammable.
        [Test]
        public void F4_Definition_OmittedFields_KeepCSharpDefaults()
        {
            LiquidRegistry.Initialize(
                @"{ ""Liquids"": [ { ""Id"": ""bare"" } ] }");
            var d = LiquidRegistry.Get("bare");
            Assert.IsNotNull(d);
            Assert.AreEqual(99999, d.FlameTemperature,
                "Omitted FlameTemperature MUST keep the 99999 default " +
                "(else a bare liquid is instantly flammable).");
            Assert.AreEqual(100, d.Adsorbence,
                "Omitted Adsorbence MUST keep the 100 default.");
            Assert.AreEqual("~", d.Glyph);
            Assert.AreEqual("&c", d.Color);
        }

        // F5 — within-a-single-file duplicate Id: later row wins
        // (only cross-file was tested in LQ.2).
        [Test]
        public void F5_WithinFile_DuplicateId_LaterRowWins()
        {
            LiquidRegistry.Initialize(@"{ ""Liquids"": [
                { ""Id"": ""water"", ""Adjective"": ""damp"" },
                { ""Id"": ""water"", ""Adjective"": ""drenched"" }
            ] }");
            Assert.AreEqual(1, LiquidRegistry.Count);
            Assert.AreEqual("drenched", LiquidRegistry.Get("water").Adjective);
        }
    }
}
