using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase A — dedicated adversarial sweep
    /// (ADVERSARIAL_TESTING.md taxonomy; log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Surfaces probed: SAVE/LOAD REACH (the whole A1/A2 container
    /// economy stands on looted-stays-looted — plan §8.1 was the one
    /// unverified 🔴-class risk), BOUNDARY INPUTS (null actors/zones,
    /// oversized stamps), PARSER (loot JSON garbage), STACKING (vial
    /// stack floor), and RNG BOUNDARIES beyond the per-SM pins.
    /// </summary>
    [TestFixture]
    public class BiomePhaseAAdversarialTests
    {
        private static EntityFactory _factory;
        private EntityFactory _savedHarvestFactory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            _savedHarvestFactory = HarvestablePart.Factory;
            HarvestablePart.Factory = _factory;
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            HarvestablePart.Factory = _savedHarvestFactory;
            LootTableRegistry.ResetForTests();
        }

        // ════════════════ Save/load reach ════════════════

        [Test]
        public void Adversarial_StockedChest_ContentsSurviveSaveRoundTrip()
        {
            // Plan §8.1 — THE load-bearing invariant: a looted-or-not
            // chest must come back from a save with exactly its
            // contents. If this breaks, every container refills or
            // empties on load and the loot economy is fiction.
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            var chest = _factory.CreateEntity("Chest");
            int added = LootStocker.StockContainer(chest, "CaveSupplyT1", _factory, new Random(3));
            Assert.Greater(added, 0, "precondition: stocked");
            var namesBefore = new List<string>();
            foreach (var item in chest.GetPart<ContainerPart>().Contents)
                namesBefore.Add(item.BlueprintName);

            var loaded = PartRoundTripHelper.RoundTripEntityWithBodies(chest);

            var container = loaded.GetPart<ContainerPart>();
            Assert.IsNotNull(container, "container part survives");
            var namesAfter = new List<string>();
            foreach (var item in container.Contents)
                namesAfter.Add(item.BlueprintName);
            namesBefore.Sort(); namesAfter.Sort();
            CollectionAssert.AreEqual(namesBefore, namesAfter,
                "chest contents round-trip exactly — looted stays looted");
        }

        [Test]
        public void Adversarial_HarvestableCorpse_ConfigSurvivesSaveRoundTrip()
        {
            // Runtime-attached parts (CorpsePart adds HarvestablePart at
            // corpse-spawn) must reach the save graph like blueprint parts.
            var corpse = _factory.CreateEntity("CreatureCorpse");
            corpse.AddPart(new HarvestablePart
            { YieldBlueprint = "VenomGland", YieldMin = 1, YieldMax = 2, YieldChance = 75 });

            var loaded = PartRoundTripHelper.RoundTripEntity(corpse);

            var harvest = loaded.GetPart<HarvestablePart>();
            Assert.IsNotNull(harvest, "runtime-attached part serialized");
            Assert.AreEqual("VenomGland", harvest.YieldBlueprint);
            Assert.AreEqual(2, harvest.YieldMax);
            Assert.AreEqual(75, harvest.YieldChance);
        }

        // ════════════════ Boundary inputs ════════════════

        [Test]
        public void Adversarial_LootStocker_NullEverything_NoCrash()
        {
            Assert.AreEqual(0, LootStocker.StockContainer(null, "X", _factory, new Random(1)));
            var chest = _factory.CreateEntity("Chest");
            Assert.AreEqual(0, LootStocker.StockContainer(chest, null, _factory, new Random(1)));
            Assert.AreEqual(0, LootStocker.StockContainer(chest, "X", null, new Random(1)));
        }

        [Test]
        public void Adversarial_Harvest_ActorWithoutInventory_ConsumesWithoutYield()
        {
            var zone = new Zone("T");
            var vein = _factory.CreateEntity("GlowQuartzVein");
            zone.AddEntity(vein, 5, 5);
            var bare = new Entity { ID = "bare" };
            bare.AddPart(new RenderPart { DisplayName = "bare" });

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Harvest");
            e.SetParameter("Actor", (object)bare);
            e.SetParameter("Zone", (object)zone);
            vein.FireEvent(e);
            e.Release();

            Assert.IsNull(zone.GetEntityCell(vein), "still consumed — no crash, no dupe");
        }

        [Test]
        public void Adversarial_Rest_NullZone_RestsWithoutScan()
        {
            // Contract: no zone = no hostile scan possible; rest proceeds
            // (conversation-driven inn rest with no active zone).
            var actor = new Entity { ID = "a" };
            actor.AddPart(new RenderPart { DisplayName = "a" });
            var hp = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 5, Min = 0, Max = 30 };
            actor.Statistics["Hitpoints"] = hp;

            Assert.IsTrue(RestSystem.TryRest(actor, null, "void", out _));
            Assert.AreEqual(30, actor.GetStat("Hitpoints").Value);
        }

        [Test]
        public void Adversarial_Stamp_BiggerThanZone_SkippedWithoutCrash()
        {
            var rows = new string[30];
            for (int i = 0; i < 30; i++) rows[i] = new string('#', 90);
            var giant = new StructureStamp
            {
                Name = "TooBig",
                Chance = 100,
                Rows = rows,
                Legend = new Dictionary<char, string> { { '#', "Wall" } },
            };
            var zone = new Zone("T");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(2)));
            int before = zone.GetAllEntities().Count;
            new LandmarkBuilder(BiomeType.Cave, 1, new List<StructureStamp> { giant })
                .BuildZone(zone, _factory, new Random(2));
            Assert.AreEqual(before, zone.GetAllEntities().Count, "nothing placed");
        }

        // ════════════════ Parser malformed inputs ════════════════

        [Test]
        public void Adversarial_LootRegistry_GarbageJson_EmptyButAlive()
        {
            LootTableRegistry.Initialize("{ not json at all ]]]");
            Assert.IsTrue(LootTableRegistry.IsInitialized);
            Assert.AreEqual(0, LootTableRegistry.Count);
            Assert.IsEmpty(LootTableRegistry.Roll("Anything", new Random(1)));
        }

        [Test]
        public void Adversarial_LootRegistry_AnonymousAndNullTables_Skipped()
        {
            LootTableRegistry.Initialize(@"{ ""Tables"": [
                { ""Entries"": [ { ""Blueprint"": ""Dagger"" } ] },
                { ""Name"": """", ""Entries"": [] },
                { ""Name"": ""Good"", ""Entries"": [ { ""Blueprint"": ""Dagger"" } ] }
            ]}");
            Assert.AreEqual(1, LootTableRegistry.Count, "only the named table registers");
            Assert.IsNotNull(LootTableRegistry.Get("Good"));
        }

        // ════════════════ Stacking / re-fire ════════════════

        [Test]
        public void Adversarial_InkVial_StackOfOne_ConsumedOutright_NoZeroStack()
        {
            var actor = new Entity { ID = "a" };
            actor.AddPart(new RenderPart { DisplayName = "a" });
            actor.AddPart(new InventoryPart { MaxWeight = 100 });
            var vial = _factory.CreateEntity("InkVial");
            Assert.AreEqual(1, vial.GetPart<StackerPart>().StackCount, "precondition");
            actor.GetPart<InventoryPart>().AddObject(vial);

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "UseInkVial");
            e.SetParameter("Actor", (object)actor);
            vial.FireEvent(e);
            e.Release();

            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(vial),
                "a 1-stack never lingers at count 0");
        }

        // ════════════════ RNG boundaries ════════════════

        [Test]
        public void Adversarial_PickTable_MinPicksGreaterThanEntries_StillBounded()
        {
            LootTableRegistry.Initialize(@"{ ""Tables"": [
                { ""Name"": ""Tiny"", ""PickOne"": true, ""MinPicks"": 5, ""MaxPicks"": 5,
                  ""Entries"": [ { ""Blueprint"": ""Dagger"", ""Weight"": 1 } ] }
            ]}");
            var rolled = LootTableRegistry.Roll("Tiny", new Random(4));
            Assert.AreEqual(5, rolled.Count,
                "picks repeat the sole entry — bounded, no infinite loop, no crash");
        }
    }
}
