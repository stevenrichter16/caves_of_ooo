using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using System.IO;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M2.c — different faces. Pins the ambassador spawn (the five
    /// faction NPCs carried 147 authored dialogue nodes and NO spawn path)
    /// and the TradeStockBuilder faction-gate fix (pre-M2.c every merchant
    /// in a Desert/Jungle/Ruins village had zero trade stock, because
    /// WireNPC stamps village NPCs with the POI faction and the stock gate
    /// accepted only "Villagers").
    /// </summary>
    public class FactionVillageTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        private Zone BuildVillage(string zoneId, string faction, BiomeType biome,
            bool withTradeStock = false)
        {
            var poi = new PointOfInterest(POIType.Village, "Test Village", faction);
            var zone = new Zone(zoneId);
            Assert.IsTrue(new VillageBuilder(biome, poi).BuildZone(zone, _factory, new System.Random(42)));
            Assert.IsTrue(new VillagePopulationBuilder(poi).BuildZone(zone, _factory, new System.Random(42)));
            if (withTradeStock)
                Assert.IsTrue(new TradeStockBuilder(null, poi).BuildZone(zone, _factory, new System.Random(42)));
            return zone;
        }

        private static Entity FindByBlueprint(Zone zone, string blueprint)
        {
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) return e;
            return null;
        }

        [Test]
        [TestCase("RotChoir", BiomeType.Jungle, "ChoirTendril")]
        [TestCase("Palimpsest", BiomeType.Ruins, "PalimpsestEcho")]
        [TestCase("SaccharineConcord", BiomeType.Desert, "SaccharineEnvoy")]
        public void FactionVillage_SpawnsItsAmbassador(string faction, BiomeType biome, string ambassador)
        {
            var zone = BuildVillage("Overworld.5.5.0", faction, biome);
            var npc = FindByBlueprint(zone, ambassador);
            Assert.IsNotNull(npc,
                $"{faction} villages must spawn {ambassador} — the lore web " +
                "was unreachable while ambassadors had no spawn path.");
            Assert.AreEqual(faction, npc.Tags.TryGetValue("Faction", out var f) ? f : null,
                "Ambassador must keep its OWN faction tag (placed via " +
                "PlaceEntity, not WireNPC).");
        }

        [Test]
        public void VillagersVillage_SpawnsNoAmbassador()
        {
            // Counter-check: home has no cosmic lobbyist. The starting
            // village is Villagers-faction — the Choir finds you only once
            // you walk.
            var zone = BuildVillage("Overworld.10.10.0", "Villagers", BiomeType.Cave);
            foreach (var ambassador in VillagePopulationBuilder.AmbassadorBlueprintByFaction.Values)
                Assert.IsNull(FindByBlueprint(zone, ambassador),
                    $"Villagers villages must not spawn {ambassador}.");
        }

        [Test]
        public void FactionVillageMerchant_FinallyHasTradeStock()
        {
            // Pre-M2.c this was zero: WireNPC stamped the merchant
            // "SaccharineConcord", and the stock gate accepted only
            // "Villagers" — the latent starvation bug found by the sweep.
            var zone = BuildVillage("Overworld.5.5.0", "SaccharineConcord",
                BiomeType.Desert, withTradeStock: true);
            var merchant = FindByBlueprint(zone, "Merchant");
            Assert.IsNotNull(merchant, "Village must spawn a Merchant.");

            var nonRepairStock = merchant.GetPart<InventoryPart>().Objects
                .Where(o => !o.BlueprintName.EndsWith("Grimoire"))
                .ToList();
            Assert.IsTrue(nonRepairStock.Count > 0,
                "Concord-village merchant must carry trade stock — the " +
                "faction gate starved it pre-M2.c.");
        }

        [Test]
        public void WildernessCreatures_StillGetNoTradeStock()
        {
            // Counter-check: widening the gate to the POI faction must NOT
            // start stocking hostile faction creatures. A Snapjaw in a
            // hypothetical Snapjaws-faction "village" POI would qualify by
            // faction — but real villages never carry hostile factions, so
            // pin the simple case: a creature with a non-village faction in
            // a Concord village gets nothing.
            var poi = new PointOfInterest(POIType.Village, "Test", "SaccharineConcord");
            var zone = new Zone("Overworld.5.5.0");
            var snapjaw = _factory.CreateEntity("Snapjaw");
            zone.AddEntity(snapjaw, 10, 10);

            Assert.IsTrue(new TradeStockBuilder(null, poi).BuildZone(zone, _factory, new System.Random(1)));

            var inv = snapjaw.GetPart<InventoryPart>();
            Assert.IsTrue(inv == null || inv.Objects.Count == 0,
                "Snapjaws (faction 'Snapjaws') must not receive trade stock.");
        }
    }
}
