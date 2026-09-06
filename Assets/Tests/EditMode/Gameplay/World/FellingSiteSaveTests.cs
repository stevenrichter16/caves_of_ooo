using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSiteSaveTests
    {
        private EntityFactory _factory;
        [SetUp] public void Setup()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        private GameSessionState Roundtrip(OverworldZoneManager manager, Zone zone, Entity actor)
        {
            manager.SetActiveZone(zone); var tm = new TurnManager(); tm.AddEntity(actor);
            var state = GameSessionState.Capture("felling-site", "test", manager, tm, actor);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                return GameSessionState.Load(new SaveReader(stream, _factory));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void LoadingOldCachedMapRepairsAppearanceAndOnlyMissingIdentity(bool missingId)
        {
            var manager = new OverworldZoneManager(_factory, 64); manager.WorldMap.SetPOI(3, 5, null);
            var zone = manager.GetZone("WorldMap"); var p = WorldMap.WorldCellToZoneCell(3, 5);
            var marker = zone.GetCell(p.zoneX, p.zoneY).Objects.Single(e => e.HasPart<WorldMapCellPart>());
            marker.ID = missingId ? null : "saved-map-marker:3,5";
            marker.SetIntProperty("PersonalMark", 17); marker.GetPart<RenderPart>().RenderLayer = 4;
            var player = _factory.CreateEntity("Player"); zone.AddEntity(player, p.zoneX, p.zoneY);
            var item = _factory.CreateEntity("Tepuibone"); zone.AddEntity(item, p.zoneX, p.zoneY);
            int count = zone.GetAllEntities().Count;
            var loaded = Roundtrip(manager, zone, player); var loadedZone = loaded.ZoneManager.GetZone("WorldMap");
            var cell = loadedZone.GetCell(p.zoneX, p.zoneY);
            var lm = cell.Objects.Single(e => e.HasPart<WorldMapCellPart>());
            Assert.AreEqual("FellingSite", loaded.ZoneManager.WorldMap.GetPOI(3, 5).Type.ToString());
            Assert.AreEqual("the Felling-Site", lm.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual("O", lm.GetPart<RenderPart>().RenderString);
            if (missingId)
            {
                Assert.IsTrue(System.Guid.TryParseExact(lm.ID, "N", out _));
                Assert.IsNull(marker.ID, "loading does not mutate the old saved source");
            }
            else Assert.AreEqual(marker.ID, lm.ID);
            Assert.AreEqual(17, lm.GetIntProperty("PersonalMark"));
            Assert.AreEqual(4, lm.GetPart<RenderPart>().RenderLayer);
            Assert.IsTrue(cell.Objects.Contains(loaded.Player)); Assert.IsTrue(cell.Objects.Any(e => e.ID == item.ID));
            Assert.AreEqual(count, loadedZone.GetAllEntities().Count);
        }
        [TestCase(1)] [TestCase(999)]
        public void LoadingPreservesAnExistingDifferentPOI(int kind)
        {
            var manager = new OverworldZoneManager(_factory, 64);
            manager.WorldMap.SetPOI(3, 5, new PointOfInterest((POIType)kind, "A saved place", "SavedFaction", 2, "Snapjaw"));
            var zone = manager.GetZone("WorldMap"); var player = _factory.CreateEntity("Player"); zone.AddEntity(player, 40, 12);
            var loaded = Roundtrip(manager, zone, player); var poi = loaded.ZoneManager.WorldMap.GetPOI(3, 5);
            Assert.AreEqual(kind, (int)poi.Type); Assert.AreEqual("A saved place", poi.Name);
            Assert.AreEqual("SavedFaction", poi.Faction); Assert.AreEqual(2, poi.Tier); Assert.AreEqual("Snapjaw", poi.BossBlueprint);
        }
        [Test]
        public void LoadingDoesNotGenerateAnUncachedWorldMap()
        {
            var manager = new OverworldZoneManager(_factory, 64); var zone = manager.GetZone("Overworld.3.5.0");
            var player = _factory.CreateEntity("Player"); zone.AddEntity(player, 40, 12);
            var loaded = Roundtrip(manager, zone, player);
            Assert.IsFalse(loaded.ZoneManager.CachedZones.ContainsKey("WorldMap"));
        }
    }
}
