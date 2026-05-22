using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Gas cloud visuals — density → CP437 shade glyph (░ ▒ ▓) for the
    /// Qud "densely packed dots" look, and the Refresh helper that syncs a
    /// gas entity's glyph + marks its cell dirty (the missing repaint that
    /// made clouds invisible in-game).
    /// </summary>
    public class GasVisualsTests
    {
        [SetUp]
        public void Setup()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""vis-gas"", ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":50, ""DefaultLevel"":1, ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            ZoneRenderHooks.Reset();
        }

        // ════════════════ GlyphForDensity (pure) ════════════════

        [Test]
        public void GlyphForDensity_Thin_LightShade()
            => Assert.AreEqual(GasVisuals.SHADE_LIGHT, GasVisuals.GlyphForDensity(5));

        [Test]
        public void GlyphForDensity_Medium_MediumShade()
            => Assert.AreEqual(GasVisuals.SHADE_MEDIUM, GasVisuals.GlyphForDensity(50));

        [Test]
        public void GlyphForDensity_Dense_DarkShade()
            => Assert.AreEqual(GasVisuals.SHADE_DARK, GasVisuals.GlyphForDensity(200));

        [Test]
        public void GlyphForDensity_MediumBoundary_Inclusive()
            => Assert.AreEqual(GasVisuals.SHADE_MEDIUM, GasVisuals.GlyphForDensity(GasVisuals.MEDIUM_THRESHOLD));

        [Test]
        public void GlyphForDensity_DarkBoundary_Inclusive()
            => Assert.AreEqual(GasVisuals.SHADE_DARK, GasVisuals.GlyphForDensity(GasVisuals.DARK_THRESHOLD));

        [Test]
        public void GlyphForDensity_JustBelowMedium_StaysLight()
            => Assert.AreEqual(GasVisuals.SHADE_LIGHT, GasVisuals.GlyphForDensity(GasVisuals.MEDIUM_THRESHOLD - 1));

        [Test]
        public void GlyphForDensity_Monotonic_DenserIsNeverLighter()
        {
            // Counter: density never makes the glyph LIGHTER as it grows.
            char prev = GasVisuals.GlyphForDensity(0);
            for (int d = 1; d <= 300; d++)
            {
                char g = GasVisuals.GlyphForDensity(d);
                Assert.GreaterOrEqual(g, prev, $"glyph regressed lighter at density {d}");
                prev = g;
            }
        }

        // ════════════════ Refresh ════════════════

        [Test]
        public void Refresh_SetsRenderStringToDensityGlyph()
        {
            var e = new Entity { ID = "g", BlueprintName = "Cloud" };
            e.AddPart(new RenderPart { RenderString = "?" });
            var pool = new GasPoolPart { GasId = "vis-gas" };
            e.AddPart(pool);
            pool.Density = 200; // dense

            GasVisuals.Refresh(e, pool, null);
            Assert.AreEqual(GasVisuals.SHADE_DARK.ToString(), e.GetPart<RenderPart>().RenderString);
        }

        [Test]
        public void Refresh_NullArgs_NoThrow()
        {
            Assert.DoesNotThrow(() => GasVisuals.Refresh(null, null, null));
            var e = new Entity { ID = "g2" };
            Assert.DoesNotThrow(() => GasVisuals.Refresh(e, null, null));
        }

        [Test]
        public void Refresh_MarksCellDirty_WhenRendererListening()
        {
            // Pin the missing-repaint fix: Refresh must flag the gas cell so
            // the renderer repaints it. Stub the hook to capture the call.
            (int x, int y, string src)? captured = null;
            ZoneRenderHooks.CellDirtyCallback = (x, y, src) => captured = (x, y, src);

            var zone = new Zone("VisDirty");
            var gas = GasFactory.SpawnGas(zone, 7, 9, "vis-gas", density: 100);
            // SpawnGas → Refresh → MarkCellDirty(7,9)
            Assert.IsTrue(captured.HasValue, "gas spawn marks its cell dirty");
            Assert.AreEqual((7, 9), (captured.Value.x, captured.Value.y));
        }

        // ════════════════ GasFactory integration ════════════════

        [Test]
        public void SpawnGas_DenseCloud_GetsDarkShadeGlyph()
        {
            var zone = new Zone("VisSpawnDense");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "vis-gas", density: 200);
            Assert.AreEqual(GasVisuals.SHADE_DARK.ToString(), gas.GetPart<RenderPart>().RenderString,
                "dense spawn shows the dark shade ▓");
            // Color still encodes the gas TYPE (poison green), independent of glyph.
            Assert.AreEqual("&g", gas.GetPart<RenderPart>().ColorString);
        }

        [Test]
        public void SpawnGas_ThinCloud_GetsLightShadeGlyph()
        {
            var zone = new Zone("VisSpawnThin");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "vis-gas", density: 5);
            Assert.AreEqual(GasVisuals.SHADE_LIGHT.ToString(), gas.GetPart<RenderPart>().RenderString,
                "thin spawn shows the light shade ░");
        }

        // ════════════════ Background tint (fills the cell) ════════════════

        [Test]
        public void ToBackgroundCode_ConvertsForegroundToBackground()
        {
            Assert.AreEqual("^g", GasVisuals.ToBackgroundCode("&g"));
            Assert.AreEqual("^Y", GasVisuals.ToBackgroundCode("&Y"));
            Assert.AreEqual("", GasVisuals.ToBackgroundCode(""));
            Assert.AreEqual("", GasVisuals.ToBackgroundCode("nocode"));
        }

        [Test]
        public void Refresh_SetsBackgroundTint_FromGasColor()
        {
            // The cell-background tint is what makes a cloud read as a
            // filled colored rectangle even when the foreground stipple is
            // sparse + dimmed by ambient light.
            var e = new Entity { ID = "g", BlueprintName = "Cloud" };
            e.AddPart(new RenderPart { RenderString = "?" });
            var pool = new GasPoolPart { GasId = "vis-gas", ColorString = "&g" };
            e.AddPart(pool);
            pool.Density = 100;
            GasVisuals.Refresh(e, pool, null);
            Assert.AreEqual("^g", e.GetPart<RenderPart>().BackgroundColor,
                "gas cell gets a background tint matching its type color");
        }

        [Test]
        public void SpawnGas_SetsBackgroundTint()
        {
            var zone = new Zone("VisBg");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "vis-gas", density: 100);
            Assert.AreEqual("^g", gas.GetPart<RenderPart>().BackgroundColor,
                "spawned gas has a colored cell background (&g → ^g)");
        }
    }
}
