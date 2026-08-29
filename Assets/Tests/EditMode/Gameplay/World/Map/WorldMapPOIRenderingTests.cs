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
    /// pins (10,10) to be a Village (Sill). The worldmap zone
    /// renders that cell with '!' in yellow.</para>
    /// </summary>
    public class WorldMapPOIRenderingTests
    {
        // ── GetPOIRender pure tests ────────────────────────────

        [Test]
        public void GetPOIRender_Village_ReturnsExclamation()
        {
            var poi = new PointOfInterest(POIType.Village, "Sill");
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(poi);
            Assert.AreEqual("!", glyph);
            Assert.AreEqual("&Y", color);
            StringAssert.Contains("Sill", name);
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
        public void GetPOIRender_Sinkhole_IsAHoleNotAQuestionMark()
        {
            // Close-out 🔴 — a Sinkhole used to fall through to the
            // default '?' AND leak its coined name on examine.
            var poi = new PointOfInterest(POIType.Sinkhole, "Ginmere", null, 3);
            var (glyph, color, name) = WorldMapZoneBuilder.GetPOIRender(poi);
            Assert.AreEqual("o", glyph);
            Assert.AreEqual("&K", color);
            Assert.AreEqual("Ginmere", name);
        }

        [Test]
        public void AnUndiscoveredMouth_WearsThePlainBiomeFace()
        {
            // Canon: "Overgrown mouths are found, not shown" — and the
            // mouth builder's own docstring promises no map marker.
            // Until the player has ENTERED the chunk, the world map
            // shows biome glyph, biome colour, biome name. No leak.
            var (zone, worldMap) = BuildWorldMapZone();
            foreach (var (siteName, wx, wy) in SinkholeSites.All)
            {
                Assert.IsFalse(worldMap.IsVisited(wx, wy),
                    siteName + " starts undiscovered in a fresh world");
                var (zx, zy) = WorldMap.WorldCellToZoneCell(wx, wy);
                foreach (var obj in zone.GetCell(zx, zy).Objects)
                {
                    if (obj.GetPart<WorldMapCellPart>() == null) continue;
                    var render = obj.GetPart<RenderPart>();
                    Assert.AreNotEqual("?", render.RenderString, siteName);
                    Assert.AreNotEqual("o", render.RenderString, siteName);
                    StringAssert.DoesNotContain(siteName, render.DisplayName ?? "",
                        "the name is the discovery; the map must not spoil it");
                }
            }
        }

        [Test]
        public void ADiscoveredMouth_EarnsItsMarker()
        {
            // Counter-check: once the chunk has been entered, the hole
            // is knowledge and the map shows it.
            var worldMap = WorldGenerator.Generate(42);
            var (name0, wx0, wy0) = SinkholeSites.All[0];
            worldMap.MarkVisited(wx0, wy0);

            // Same factory fixture as BuildWorldMapZone — the builder
            // creates border walls, and an empty blueprint set logs an
            // error that LogAssert turns into a failure.
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
            new WorldMapZoneBuilder(worldMap).BuildZone(zone, factory, new System.Random(1));

            var (zx, zy) = WorldMap.WorldCellToZoneCell(wx0, wy0);
            bool marker = false;
            foreach (var obj in zone.GetCell(zx, zy).Objects)
            {
                if (obj.GetPart<WorldMapCellPart>() == null) continue;
                var render = obj.GetPart<RenderPart>();
                if (render.RenderString == "o") marker = true;
            }
            Assert.IsTrue(marker, name0 + " has been walked into; the map may say so");
        }

        [Test]
        public void AVillage_IsStillShownUndiscovered()
        {
            // Counter-check: hiding is sinkhole-specific. Villages are
            // civilization — known of, marked, exactly as before.
            var (zone, worldMap) = BuildWorldMapZone();
            Assert.IsFalse(worldMap.IsVisited(10, 10));
            var (zx, zy) = WorldMap.WorldCellToZoneCell(10, 10);
            bool marker = false;
            foreach (var obj in zone.GetCell(zx, zy).Objects)
                if (obj.GetPart<WorldMapCellPart>() != null
                    && obj.GetPart<RenderPart>().RenderString == "!") marker = true;
            Assert.IsTrue(marker, "Sill renders '!' whether or not visited");
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
            // (Sill). The terrain entity at the corresponding
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
                    "Center (10,10) is Sill village; should render with '!'.");
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
