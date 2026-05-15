using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WM.6 tests for POI rendering on the world-map zone.
    /// Pins the glyph/color/name mapping so a future renderer that
    /// reads each cell's <see cref="RenderPart"/> shows the right
    /// marker for each POI type.
    ///
    /// <para>The flagship surface: <see cref="WorldGenerator"/> hard-
    /// pins (10,10) to be a Village (Kyakukya). The worldmap zone
    /// renders that cell with '!' in yellow.</para>
    /// </summary>
    public class WorldMapPOIRenderingTests
    {
        // ── GetPOIRender pure tests ────────────────────────────

        [Test]
        public void GetPOIRender_Village_ReturnsExclamation()
        {
            var poi = new PointOfInterest(POIType.Village, "Kyakukya");
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(poi);
            Assert.AreEqual("!", glyph);
            Assert.AreEqual("&Y", color);
            StringAssert.Contains("Kyakukya", name);
            StringAssert.Contains("village", name);
        }

        [Test]
        public void GetPOIRender_Lair_ReturnsAmpersand()
        {
            var poi = new PointOfInterest(POIType.Lair, "Snapjaw");
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(poi);
            Assert.AreEqual("&", glyph);
            Assert.AreEqual("&R", color);
            StringAssert.Contains("Snapjaw", name);
            StringAssert.Contains("lair", name);
        }

        [Test]
        public void GetPOIRender_MerchantCamp_ReturnsDollar()
        {
            var poi = new PointOfInterest(POIType.MerchantCamp, "Caravan");
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(poi);
            Assert.AreEqual("$", glyph);
            Assert.AreEqual("&G", color);
            StringAssert.Contains("Caravan", name);
            StringAssert.Contains("merchant camp", name);
        }

        [Test]
        public void GetPOIRender_RiverChunk_ReturnsTilde()
        {
            var poi = new PointOfInterest(POIType.RiverChunk, "river");
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(poi);
            Assert.AreEqual("~", glyph);
            Assert.AreEqual("&C", color);
            StringAssert.Contains("river", name);
        }

        [Test]
        public void GetPOIRender_Null_ReturnsQuestionMark()
        {
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(null);
            Assert.AreEqual("?", glyph);
        }

        // ── Integration: zone-build with POI overlays ─────────

        private static (Zone zone, WorldMap map) BuildWorldMapZone(int seed = 42)
        {
            var worldMap = WorldGenerator.Generate(seed);
            var factory = new EntityFactory();
            factory.LoadBlueprints(@"{
              ""Objects"": [
                {
                  ""Name"": ""Wall"",
                  ""Parts"": [
                    { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                    { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""#"" }] }
                  ],
                  ""Stats"": [],
                  ""Tags"": [{ ""Key"": ""Solid"", ""Value"": """" }]
                }
              ]
            }");
            var zone = new Zone(WorldMap.WorldMapZoneID);
            var builder = new WorldMapZoneBuilder(worldMap);
            builder.BuildZone(zone, factory, new System.Random(1));
            return (zone, worldMap);
        }

        [Test]
        public void WorldMapZoneBuilder_CenterCell_RendersWithVillageMarker()
        {
            // WorldGenerator hard-pins center (10,10) to be a Village
            // (Kyakukya). The terrain entity at the corresponding
            // zone cell must render with '!' in &Y.
            var (zone, _) = BuildWorldMapZone();
            var (zx, zy) = WorldMap.WorldCellToZoneCell(10, 10);
            var cell = zone.GetCell(zx, zy);

            bool foundVillageMarker = false;
            foreach (var obj in cell.Objects)
            {
                if (obj.GetPart<WorldMapCellPart>() == null) continue;
                var render = obj.GetPart<RenderPart>();
                if (render == null) continue;
                Assert.AreEqual("!", render.RenderString,
                    "Center (10,10) is Kyakukya village; should render with '!'.");
                Assert.AreEqual("&Y", render.ColorString);
                StringAssert.Contains("village", render.DisplayName);
                foundVillageMarker = true;
            }
            Assert.IsTrue(foundVillageMarker,
                "Expected to find a WorldMapCellPart-tagged terrain entity at the center cell.");
        }

        [Test]
        public void WorldMapZoneBuilder_CellWithoutPOI_RendersBiomeGlyph()
        {
            // Counter-check: a cell with NO POI renders the biome glyph
            // (not a POI marker). Find such a cell empirically.
            var (zone, worldMap) = BuildWorldMapZone();
            int nonPOIx = -1, nonPOIy = -1;
            for (int wy = 0; wy < WorldMap.Height && nonPOIx < 0; wy++)
                for (int wx = 0; wx < WorldMap.Width; wx++)
                {
                    if (!worldMap.HasPOI(wx, wy))
                    {
                        nonPOIx = wx; nonPOIy = wy; break;
                    }
                }
            Assert.GreaterOrEqual(nonPOIx, 0, "Expected at least one non-POI cell.");

            var (zx, zy) = WorldMap.WorldCellToZoneCell(nonPOIx, nonPOIy);
            var cell = zone.GetCell(zx, zy);
            var biome = worldMap.GetBiome(nonPOIx, nonPOIy);
            var (expectedGlyph, _, _) = WorldMapZoneBuilder.GetBiomeRender(biome);

            foreach (var obj in cell.Objects)
            {
                if (obj.GetPart<WorldMapCellPart>() == null) continue;
                var render = obj.GetPart<RenderPart>();
                Assert.AreEqual(expectedGlyph, render.RenderString,
                    $"Non-POI cell ({nonPOIx},{nonPOIy}) biome={biome} should render with biome glyph.");
                return;
            }
            Assert.Fail("No WorldMapCellPart-tagged entity found on the non-POI cell.");
        }
    }
}
