using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase A3 — harvest &amp; butchery
    /// (Docs/BIOME-OVERHAUL.md §2 A3, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Before this: corpses were 2-3 dram vendor trash for ~40 creature
    /// types, every alchemy reagent was purchase-only ("VenomGland —
    /// harvested from something that bit first" had no harvest path),
    /// and the three minerals with full tinker recipes had zero spawn
    /// sources. HarvestablePart adds the "harvest" action; CorpsePart
    /// attaches it to corpses from per-creature blueprint config;
    /// MineralVein entities carry it directly and enter the underground
    /// spawn tables by strata band.
    /// </summary>
    [TestFixture]
    public class BiomeHarvestTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private EntityFactory _savedHarvestFactory;

        [SetUp]
        public void Setup()
        {
            // HarvestablePart resolves yields via the codebase's standard
            // static-factory convention (CorpsePart.Factory shape).
            _savedHarvestFactory = HarvestablePart.Factory;
            HarvestablePart.Factory = _factory;
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown() => HarvestablePart.Factory = _savedHarvestFactory;

        private static Entity MakeActor()
        {
            var actor = new Entity { ID = "harvester" };
            actor.AddPart(new RenderPart { DisplayName = "harvester" });
            actor.AddPart(new InventoryPart { MaxWeight = 500 });
            return actor;
        }

        private static void FireHarvest(Entity target, Entity actor, Zone zone = null)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Harvest");
            e.SetParameter("Actor", (object)actor);
            if (zone != null)
                e.SetParameter("Zone", (object)zone);
            target.FireEvent(e);
            e.Release();
        }

        private static int CountIn(InventoryPart inv, string blueprint)
        {
            int n = 0;
            foreach (var item in inv.Objects)
                if (item.BlueprintName == blueprint)
                    n += item.GetPart<StackerPart>()?.StackCount ?? 1;
            return n;
        }

        // ── 1. HarvestablePart core semantics ────────────────────

        [Test]
        public void Harvest_ZoneEntity_YieldsToInventory_AndConsumesTarget()
        {
            var zone = new Zone("T");
            var vein = _factory.CreateEntity("GlowQuartzVein");
            Assert.IsNotNull(vein, "GlowQuartzVein blueprint must exist");
            zone.AddEntity(vein, 5, 5);
            var actor = MakeActor();

            Diag.ResetAll();
            FireHarvest(vein, actor, zone);

            Assert.GreaterOrEqual(CountIn(actor.GetPart<InventoryPart>(), "GlowQuartz"), 1,
                "mineral lands in the harvester's pack");
            Assert.IsNull(zone.GetEntityCell(vein), "the vein is spent and gone");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "loot", Kind = "Harvested", Limit = 5 }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("GlowQuartz", records[0].PayloadJson);
        }

        [Test]
        public void Harvest_ChanceZero_NoYield_TargetStillConsumed_CounterCheck()
        {
            // Butchery can fail — the carcass is spent either way. A
            // failed harvest must not leave a re-rollable target behind.
            var zone = new Zone("T");
            var husk = new Entity { ID = "husk", BlueprintName = "TestHusk" };
            husk.AddPart(new RenderPart { DisplayName = "husk" });
            husk.AddPart(new HarvestablePart
            { YieldBlueprint = "GlowQuartz", YieldChance = 0 });
            zone.AddEntity(husk, 3, 3);
            var actor = MakeActor();

            FireHarvest(husk, actor, zone);

            Assert.AreEqual(0, CountIn(actor.GetPart<InventoryPart>(), "GlowQuartz"));
            Assert.IsNull(zone.GetEntityCell(husk), "spent even on a failed roll");
        }

        [Test]
        public void Harvest_CarriedItem_ConsumedFromInventory()
        {
            var actor = MakeActor();
            var corpse = _factory.CreateEntity("CreatureCorpse");
            corpse.AddPart(new HarvestablePart
            { YieldBlueprint = "RawMeat", YieldMin = 2, YieldMax = 2 });
            actor.GetPart<InventoryPart>().AddObject(corpse);

            FireHarvest(corpse, actor);

            Assert.AreEqual(2, CountIn(actor.GetPart<InventoryPart>(), "RawMeat"));
            Assert.IsFalse(actor.GetPart<InventoryPart>().Objects.Contains(corpse),
                "the butchered corpse is consumed");
        }

        // ── 2. CorpsePart attaches harvest config to spawned corpses ──

        private static Entity DieAndGetCorpse(Entity creature, Zone zone)
        {
            zone.AddEntity(creature, 4, 4);
            var died = GameEvent.New("Died");
            died.SetParameter("Zone", (object)zone);
            creature.FireEvent(died);
            died.Release();
            foreach (var e in zone.GetAllEntities())
                if (e != creature && e.BlueprintName.Contains("Corpse"))
                    return e;
            return null;
        }

        [Test]
        public void CorpsePart_SpawnsCorpseWithHarvestable_WhenConfigured()
        {
            var saved = CorpsePart.Factory;
            try
            {
                CorpsePart.Factory = _factory;
                var zone = new Zone("T");
                var viper = _factory.CreateEntity("Viper");
                var corpse = DieAndGetCorpse(viper, zone);
                Assert.IsNotNull(corpse, "viper drops a corpse (chance 100 via base Creature)");

                var harvest = corpse.GetPart<HarvestablePart>();
                Assert.IsNotNull(harvest, "the corpse is butcherable");
                Assert.AreEqual("VenomGland", harvest.YieldBlueprint,
                    "venom glands come from something that bit first");
            }
            finally
            {
                CorpsePart.Factory = saved;
            }
        }

        [Test]
        public void CorpsePart_NoHarvestConfig_PlainCorpse_CounterCheck()
        {
            var saved = CorpsePart.Factory;
            try
            {
                CorpsePart.Factory = _factory;
                var zone = new Zone("T");
                var villager = _factory.CreateEntity("Villager");
                var corpse = DieAndGetCorpse(villager, zone);
                Assert.IsNotNull(corpse);
                Assert.IsNull(corpse.GetPart<HarvestablePart>(),
                    "creatures without harvest config drop plain corpses");
            }
            finally
            {
                CorpsePart.Factory = saved;
            }
        }

        // ── 3. Per-creature yield config (blueprint pins) ────────

        [Test]
        public void CreatureYields_MatchTheButcheryTable()
        {
            foreach (var (creature, yield) in new[]
            {
                ("Viper", "VenomGland"), ("GiantSpider", "VenomGland"), ("Scorpion", "VenomGland"),
                ("CaveBear", "RawMeat"), ("SandWurm", "RawMeat"), ("JungleApe", "RawMeat"),
                ("CaveSlime", "BogSap"), ("Glowmaw", "EmberFruit"),
                ("StoneGolem", "GlowQuartz"),
                ("SkeletalSentry", "Bone"), ("CharredHusk", "Bone"),
            })
            {
                var e = _factory.CreateEntity(creature);
                var corpsePart = e.GetPart<CorpsePart>();
                Assert.IsNotNull(corpsePart, $"{creature} has a Corpse part");
                Assert.AreEqual(yield, corpsePart.HarvestBlueprint, creature);
            }
        }

        [Test]
        public void MeatYield_IsAMeal_NotAScrap()
        {
            var bear = _factory.CreateEntity("CaveBear").GetPart<CorpsePart>();
            Assert.AreEqual(2, bear.HarvestMin, "big game yields 2-3 meat");
            Assert.AreEqual(3, bear.HarvestMax);
        }

        // ── 4. Mineral veins ─────────────────────────────────────

        [Test]
        public void VeinBlueprints_AreSolidAndYieldTheirMineral()
        {
            foreach (var (vein, mineral) in new[]
            {
                ("GlowQuartzVein", "GlowQuartz"),
                ("PaleSaltVein", "PaleSalt"),
                ("ChoirIronVein", "ChoirIron"),
            })
            {
                var e = _factory.CreateEntity(vein);
                Assert.IsNotNull(e, vein);
                Assert.IsTrue(e.GetPart<PhysicsPart>()?.Solid ?? false, $"{vein} blocks movement");
                var harvest = e.GetPart<HarvestablePart>();
                Assert.IsNotNull(harvest, $"{vein} is mineable");
                Assert.AreEqual(mineral, harvest.YieldBlueprint, vein);
            }
        }

        [Test]
        public void UndergroundTables_SeedVeinsByStrataBand()
        {
            var shallow = PopulationTable.UndergroundTier(1);   // tier 1
            var mid = PopulationTable.UndergroundTier(4);       // tier 2
            var deep = PopulationTable.UndergroundTier(7);      // tier 3

            Assert.IsTrue(HasEntry(shallow, "GlowQuartzVein"), "quartz from the first shaft");
            Assert.IsFalse(HasEntry(shallow, "PaleSaltVein"), "salt starts deeper");
            Assert.IsFalse(HasEntry(shallow, "ChoirIronVein"), "iron starts deeper still");

            Assert.IsTrue(HasEntry(mid, "PaleSaltVein"), "limestone band carries salt");
            Assert.IsFalse(HasEntry(mid, "ChoirIronVein"));

            Assert.IsTrue(HasEntry(deep, "ChoirIronVein"), "shale band carries choir iron");
        }

        private static bool HasEntry(PopulationTable t, string blueprint)
        {
            foreach (var e in t.Entries)
                if (e.BlueprintName == blueprint) return true;
            return false;
        }
    }
}
