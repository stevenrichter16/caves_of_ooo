using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSiteTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [TestCase(1)] [TestCase(64)] [TestCase(777)]
        public void EveryWorldPlacesTheTierFiveSiteBeforeRandomClaims(int seed)
        {
            var map = WorldGenerator.Generate(seed); var poi = map.GetPOI(3, 5);
            Assert.IsNotNull(poi); Assert.AreEqual("FellingSite", poi.Type.ToString());
            Assert.AreEqual("the Felling-Site", poi.Name); Assert.AreEqual(5, poi.Tier);
            Assert.AreEqual(BiomeType.Stump, map.GetBiome(3, 5));
            Assert.AreEqual(POIType.Sinkhole, map.GetPOI(4, 6).Type);
        }
        [Test]
        public void FreshSurfaceContainsSixBarePositionsAndOneEmptySeventh()
        {
            var manager = new OverworldZoneManager(_factory, 64); var zone = manager.GetZone("Overworld.3.5.0");
            Assert.AreEqual(6, zone.GetAllEntities().Count(e => e.BlueprintName == "FellingBarePosition"));
            Assert.AreEqual(1, zone.GetAllEntities().Count(e => e.BlueprintName == "SeventhPosition"));
            Assert.Greater(zone.GetAllEntities().Count(e => e.BlueprintName == "FellingScar"), 20);
            Assert.IsFalse(zone.GetCell(40, 12).Objects.Any(e => e.BlueprintName == "SeventhPosition"));
            Assert.AreEqual(0f, zone.UrquBleedLevel);
            Assert.IsFalse(zone.GetAllEntities().Any(e => e.HasTag("Creature") || e.HasPart<StairsDownPart>() || e.HasPart<ContainerPart>()));
            foreach (var e in zone.GetAllEntities().Where(e => e.BlueprintName == "FellingBarePosition" || e.BlueprintName == "SeventhPosition"))
            {
                Assert.IsFalse(zone.GetEntityCell(e).BlocksMovement());
                Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable);
                Assert.IsTrue(zone.GenReservedCells.Contains(zone.GetEntityPosition(e)));
            }
        }
        [TestCase("Overworld.3.5.1")] [TestCase("Overworld.2.5.0")]
        public void NeighborAndUndergroundDoNotBecomeTheSite(string id)
        {
            var zone = new OverworldZoneManager(_factory, 64).GetZone(id);
            Assert.IsFalse(zone.GetAllEntities().Any(e => e.BlueprintName == "SeventhPosition"));
        }
        [Test]
        public void MapHasANamedSiteMarker()
        {
            var manager = new OverworldZoneManager(_factory, 64); var zone = manager.GetZone("WorldMap");
            var p = WorldMap.WorldCellToZoneCell(3, 5);
            var marker = zone.GetCell(p.zoneX, p.zoneY).Objects.Single(e => e.HasPart<WorldMapCellPart>()).GetPart<RenderPart>();
            Assert.AreEqual("the Felling-Site", marker.DisplayName); Assert.AreEqual("O", marker.RenderString);
        }
        [TestCase("FellingBarePosition", "felling_bare_position")]
        [TestCase("FellingScar", "felling_scar")]
        [TestCase("SeventhPosition", "seventh_position")]
        public void EveryNewGroundObjectHasRealImportedArt(string blueprint, string file)
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey(blueprint));
            Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/" + file));
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(r => r.Blueprint == blueprint && r.File == file));
            if (blueprint != "SeventhPosition") Assert.AreEqual(4, EnvironmentSpriteRenderer.FixtureVariantCounts[blueprint]);
        }
        [TestCase("felling_bare_position_v1")] [TestCase("felling_bare_position_v2")] [TestCase("felling_bare_position_v3")]
        [TestCase("felling_scar_v1")] [TestCase("felling_scar_v2")] [TestCase("felling_scar_v3")]
        public void RepeatedGroundImportsAllVariants(string file)
            => Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/" + file));
    }
}
