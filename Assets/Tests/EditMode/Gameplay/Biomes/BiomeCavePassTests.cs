using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase C — the Cave pass ("The Mossveil Reach",
    /// Docs/BIOME-OVERHAUL.md §3.1, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Two new creatures (SnapjawWarlord: the tier-3 warband leader;
    /// Mosshulk: the slow tank whose corpse yields Mendleaf), the
    /// warband camp stamp carrying the game's FIRST world-placed
    /// LockedChest + IronKey loop (lockedchest: marker), and the
    /// WarlordCleaver — a real weapon that circulates via warband loot.
    /// </summary>
    [TestFixture]
    public class BiomeCavePassTests
    {
        private static EntityFactory _factory;

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
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
        }

        [TearDown]
        public void TearDown() => LootTableRegistry.ResetForTests();

        private static MeleeWeaponPart HandWeapon(Entity creature)
        {
            var body = creature.GetPart<Body>();
            body.RegenerateDefaultEquipment();
            foreach (var part in body.GetBody().GetParts())
                if (part._DefaultBehavior?.GetPart<MeleeWeaponPart>() is MeleeWeaponPart w)
                    return w;
            return null;
        }

        // ── 1. New creatures ─────────────────────────────────────

        [Test]
        public void SnapjawWarlord_LeadsFromTheFront()
        {
            var warlord = _factory.CreateEntity("SnapjawWarlord");
            Assert.IsNotNull(warlord);
            Assert.AreEqual(45, warlord.GetStat("Hitpoints").Value);
            Assert.AreEqual(90, warlord.GetStat("XPValue").Value,
                "a warband leader outpays its chieftain cousin (65)");
            Assert.AreEqual("Snapjaws", warlord.Tags["Faction"]);
            Assert.AreEqual("2d5", HandWeapon(warlord).BaseDamage, "WarlordCleaver");
        }

        [Test]
        public void Mosshulk_TankWithAHerbalistsHeart()
        {
            var hulk = _factory.CreateEntity("Mosshulk");
            Assert.IsNotNull(hulk);
            Assert.AreEqual(55, hulk.GetStat("Hitpoints").Value);
            Assert.AreEqual("Beasts", hulk.Tags["Faction"]);
            Assert.AreEqual("2d5", HandWeapon(hulk).BaseDamage, "MosshulkSlam");
            var corpse = hulk.GetPart<CorpsePart>();
            Assert.AreEqual("MendleafSprig", corpse.HarvestBlueprint,
                "butchering a mosshulk feeds the alchemy economy");
            Assert.AreEqual(2, corpse.HarvestMin);
        }

        [Test]
        public void CaveTier3_FieldsTheNewRoster()
        {
            var t3 = PopulationTable.CaveTier3();
            bool warlord = false, hulk = false;
            foreach (var e in t3.Entries)
            {
                if (e.BlueprintName == "SnapjawWarlord") warlord = true;
                if (e.BlueprintName == "Mosshulk") hulk = true;
            }
            Assert.IsTrue(warlord, "warlord stalks the far ring");
            Assert.IsTrue(hulk, "mosshulk grazes the far ring");
        }

        // ── 2. WarlordCleaver circulates ─────────────────────────

        [Test]
        public void WarlordCleaver_IsARealWeapon()
        {
            var cleaver = _factory.CreateEntity("WarlordCleaver");
            Assert.IsNotNull(cleaver);
            var w = cleaver.GetPart<MeleeWeaponPart>();
            Assert.AreEqual("1d10", w.BaseDamage);
            Assert.AreEqual(2, w.PenBonus);
            Assert.IsNotNull(cleaver.GetPart<CommercePart>(), "sellable trophy");
        }

        // ── 3. The warband camp: first placed LockedChest + key ──

        [Test]
        public void WarbandCamp_ShipsInCaveCatalog_Tier2Up()
        {
            StructureStamp camp = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Cave))
                if (stamp.Name == "WarbandCamp") camp = stamp;
            Assert.IsNotNull(camp, "the warband camp ships");
            Assert.GreaterOrEqual(camp.MinTier, 2, "not a doorstep encounter");

            bool locked = false, key = false, warlord = false;
            foreach (var kvp in camp.Legend)
            {
                if (kvp.Value.StartsWith("lockedchest:")) locked = true;
                if (kvp.Value == "IronKey") key = true;
                if (kvp.Value == "spawn:SnapjawWarlord") warlord = true;
            }
            Assert.IsTrue(locked, "the haul is locked");
            Assert.IsTrue(key, "the key is in the camp — always obtainable");
            Assert.IsTrue(warlord, "the warlord guards it");
        }

        [Test]
        public void LockedChestMarker_PlacesLockedStockedChest()
        {
            var stamp = new StructureStamp
            {
                Name = "LockTest",
                Chance = 100,
                Rows = new[] { "L" },
                Legend = new Dictionary<char, string> { { 'L', "lockedchest:WarbandLootT2" } },
            };
            var zone = new Zone("T");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(4)));
            new LandmarkBuilder(BiomeType.Cave, 3,
                new List<StructureStamp> { stamp }).BuildZone(zone, _factory, new Random(4));

            Entity chest = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "LockedChest") { chest = e; break; }
            Assert.IsNotNull(chest, "the lockedchest: marker places a LockedChest");
            Assert.IsTrue(chest.GetPart<LockPart>()?.IsLocked ?? false, "and it IS locked");
            Assert.Greater(chest.GetPart<ContainerPart>().Contents.Count, 0, "and stocked");
        }

        [Test]
        public void WarbandLoot_CanCarryTheCleaver()
        {
            var table = LootTableRegistry.Get("WarbandLootT2");
            Assert.IsNotNull(table, "WarbandLootT2 ships");
            bool cleaver = false;
            foreach (var e in table.Entries)
                if (e.Blueprint == "WarlordCleaver") cleaver = true;
            Assert.IsTrue(cleaver, "the warlord's cleaver is the camp's prize");
        }
    }
}
