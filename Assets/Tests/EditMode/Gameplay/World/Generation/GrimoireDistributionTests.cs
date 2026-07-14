using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M2.a — grimoire reward geography. Pre-M2 every village
    /// carried a full copy of the 18-grimoire chest (8 of which the
    /// container cap silently dropped); the magic-discovery arc was a
    /// spawn menu. These tests pin the distribution table's totality,
    /// its reachability against real blueprints, and the builder wiring
    /// (starting chest teaches, other villages sell by biome).
    /// </summary>
    public class GrimoireDistributionTests
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

        private static readonly string[] AllKnownGrimoires =
        {
            "PurifyWaterGrimoire", "MendingRiteGrimoire", "KindleRiteGrimoire",
            "KindleGrimoire", "QuenchGrimoire", "ConflagrationGrimoire",
            "IceLanceGrimoire", "AcidSprayGrimoire", "ArcBoltGrimoire",
            "RimeNovaGrimoire", "ThunderclapGrimoire", "EmberVeinGrimoire",
            "KindleFlameGrimoire", "DryingBreezeGrimoire", "HearthwarmGrimoire",
            "ConjureWaterGrimoire", "ChillDraftGrimoire", "WardGleamGrimoire",
        };

        // ====================================================================
        // The distribution table itself.
        // ====================================================================

        [Test]
        public void Distribution_PlacesEveryGrimoire_ExactlyOnce()
        {
            // The reachability lesson from the fun-gap analysis, as a test:
            // a grimoire missing from every source is dead content; one in
            // two sources is an accidental double. Adding a 19th grimoire
            // blueprint without placing it here must fail this test.
            var placed = GrimoireDistribution.AllPlacedGrimoires().ToList();

            CollectionAssert.AreEquivalent(AllKnownGrimoires, placed.Distinct().ToList(),
                "Every known grimoire must appear in the distribution table.");
            Assert.AreEqual(placed.Count, placed.Distinct().Count(),
                "No grimoire may be double-sourced: " +
                string.Join(", ", placed.GroupBy(g => g).Where(g => g.Count() > 1).Select(g => g.Key)));
        }

        [Test]
        public void Distribution_EveryEntry_ResolvesToARealBlueprint()
        {
            foreach (var name in GrimoireDistribution.AllPlacedGrimoires())
            {
                var e = _factory.CreateEntity(name);
                Assert.IsNotNull(e, $"'{name}' in the distribution table has no blueprint.");
                Assert.IsNotNull(e.GetPart<GrimoirePart>(),
                    $"'{name}' must actually be a grimoire (GrimoirePart).");
            }
        }

        [Test]
        public void Distribution_UnknownFaction_FallsBackToVillagersPool()
        {
            CollectionAssert.AreEqual(
                GrimoireDistribution.ScribeStockByFaction["Villagers"],
                GrimoireDistribution.ScribeStockForFaction("SomeUnmappedFaction"),
                "Unmapped factions must fall back to the Villagers pool, not throw.");
        }

        // ====================================================================
        // Builder wiring.
        // ====================================================================

        private Zone BuildVillage(string zoneId, string faction, BiomeType biome)
        {
            var poi = new PointOfInterest(POIType.Village, "Test Village", faction);
            var villageBuilder = new VillageBuilder(biome, poi);
            var populationBuilder = new VillagePopulationBuilder(poi);
            var zone = new Zone(zoneId);
            Assert.IsTrue(villageBuilder.BuildZone(zone, _factory, new System.Random(42)));
            Assert.IsTrue(populationBuilder.BuildZone(zone, _factory, new System.Random(42)));
            return zone;
        }

        private static Entity FindByBlueprint(Zone zone, string blueprint)
        {
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) return e;
            return null;
        }

        [Test]
        public void StartingVillage_ChestHoldsExactlyTheTeachingTrio()
        {
            var zone = BuildVillage("Overworld.10.10.0", "Villagers", BiomeType.Cave);
            var chest = FindByBlueprint(zone, "Chest");
            Assert.IsNotNull(chest, "Starting village keeps its grimoire chest.");

            var contents = chest.GetPart<ContainerPart>().Contents
                .Select(e => e.BlueprintName).ToList();
            CollectionAssert.AreEquivalent(GrimoireDistribution.StartingChest, contents,
                "Starting chest holds exactly the teaching trio — no more menus.");
        }

        [Test]
        public void NonStartingVillage_HasNoGrimoireChest()
        {
            // Counter-check for the de-clone: the second village must NOT
            // contain a copy of the starting chest.
            var zone = BuildVillage("Overworld.4.7.0", "SaccharineConcord", BiomeType.Desert);
            Assert.IsNull(FindByBlueprint(zone, "Chest"),
                "Only the starting village places the grimoire chest.");
        }

        [Test]
        public void DesertVillageScribe_CarriesConcordGrimoirePool()
        {
            var zone = BuildVillage("Overworld.4.7.0", "SaccharineConcord", BiomeType.Desert);
            var scribe = FindByBlueprint(zone, "Scribe");
            Assert.IsNotNull(scribe, "Every village spawns a Scribe.");

            var stock = scribe.GetPart<InventoryPart>().Objects
                .Select(e => e.BlueprintName)
                .Where(n => n.EndsWith("Grimoire"))
                .ToList();
            CollectionAssert.AreEquivalent(
                GrimoireDistribution.ScribeStockByFaction["SaccharineConcord"], stock,
                "Desert scribes sell the Concord pool — the second village " +
                "must offer something home didn't.");
        }

        [Test]
        public void Scribe1Conversation_OffersTheBrowseTradeChoice()
        {
            var ta = Resources.Load<TextAsset>("Content/Conversations/FriendlyNPCs");
            Assert.IsNotNull(ta);
            var convo = JsonUtility.FromJson<ConversationFileData>(ta.text)
                .Conversations.FirstOrDefault(c => c.ID == "Scribe_1");
            Assert.IsNotNull(convo, "Scribe_1 conversation must exist.");

            var start = convo.Nodes.FirstOrDefault(n => n.ID == "Start");
            var browse = start.Choices.FirstOrDefault(
                ch => ch.Actions != null && ch.Actions.Any(a => a.Key == "StartTrade"));
            Assert.IsNotNull(browse,
                "Scribe_1's Start node must offer a StartTrade browse choice — " +
                "without it the scribe's grimoire stock is unreachable.");
        }
    }
}
