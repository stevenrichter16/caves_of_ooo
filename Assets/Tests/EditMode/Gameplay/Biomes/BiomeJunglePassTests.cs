using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BIOME-OVERHAUL Phase E — the Jungle pass ("The Rotwood",
    /// Docs/BIOME-OVERHAUL.md §3.3). Rotling swarms teach the poison
    /// economy early; the CanopyStrangler is the tier-3 ambusher; the
    /// vine-choked Ziggurat guards jungle uniques behind a ChoirTendril;
    /// the GroveShrine finally spawns the five NAMED Rot Choir NPCs
    /// (Mogu/Grib/Nam/Sien/Sopp — authored dialogue, never placed); and
    /// the MendleafGarden gives Mendleaf a harvest source in the wild.
    /// </summary>
    [TestFixture]
    public class BiomeJunglePassTests
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

        [Test]
        public void Rotling_SwarmTeachesPoisonEarly()
        {
            var rotling = _factory.CreateEntity("Rotling");
            Assert.IsNotNull(rotling);
            Assert.AreEqual(8, rotling.GetStat("Hitpoints").Value, "fragile by design");
            var w = HandWeapon(rotling);
            Assert.AreEqual("1d3", w.BaseDamage);
            StringAssert.Contains("Poisoned", w.OnHitEffectsRaw, "a taste of the Rotwood's rules");
        }

        [Test]
        public void CanopyStrangler_DropsFromAbove()
        {
            var strangler = _factory.CreateEntity("CanopyStrangler");
            Assert.IsNotNull(strangler);
            Assert.AreEqual(40, strangler.GetStat("Hitpoints").Value);
            Assert.AreEqual(80, strangler.GetStat("XPValue").Value);
            Assert.IsNotNull(strangler.GetPart<AIAmbushPart>(), "waits in the canopy");
            Assert.AreEqual("2d4", HandWeapon(strangler).BaseDamage, "StranglerLash");
        }

        [Test]
        public void JungleTier3_FieldsTheNewRoster()
        {
            var t3 = PopulationTable.JungleTier3();
            bool rotling = false, strangler = false;
            foreach (var e in t3.Entries)
            {
                if (e.BlueprintName == "Rotling") rotling = true;
                if (e.BlueprintName == "CanopyStrangler") strangler = true;
            }
            Assert.IsTrue(rotling && strangler);
        }

        [Test]
        public void Ziggurat_TendrilGuardedLockedVault()
        {
            StructureStamp zig = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Jungle))
                if (stamp.Name == "Ziggurat") zig = stamp;
            Assert.IsNotNull(zig, "the ziggurat ships");
            Assert.GreaterOrEqual(zig.MinTier, 2);

            bool locked = false, key = false, tendril = false;
            foreach (var kvp in zig.Legend)
            {
                if (kvp.Value.StartsWith("lockedchest:ZigguratVaultT2")) locked = true;
                if (kvp.Value == "IronKey") key = true;
                if (kvp.Value == "spawn:ChoirTendril") tendril = true;
            }
            Assert.IsTrue(locked && key && tendril,
                "vault, key, and the 80-HP tendril that owns them");
        }

        [Test]
        public void ZigguratVault_CarriesJungleUniques()
        {
            var table = LootTableRegistry.Get("ZigguratVaultT2");
            Assert.IsNotNull(table);
            bool sporeblade = false, glaive = false;
            foreach (var e in table.Entries)
            {
                if (e.Blueprint == "Sporeblade") sporeblade = true;
                if (e.Blueprint == "FirstRootGlaive") glaive = true;
            }
            Assert.IsTrue(sporeblade && glaive,
                "the jungle's signature weapons circulate beyond the one crossroads set piece");
        }

        [Test]
        public void GroveShrine_SpawnsTheFiveNamedChoirNpcs()
        {
            StructureStamp grove = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Jungle))
                if (stamp.Name == "GroveShrine") grove = stamp;
            Assert.IsNotNull(grove, "the Rot Choir congregates");

            var wanted = new[] { "Mogu", "Grib", "Nam", "Sien", "Sopp" };
            foreach (var name in wanted)
            {
                bool found = false;
                foreach (var kvp in grove.Legend)
                    if (kvp.Value == $"spawn:{name}") found = true;
                Assert.IsTrue(found, $"{name} — authored dialogue, finally placed");
            }
        }

        [Test]
        public void MendleafGarden_GrowsHarvestablePlants()
        {
            var plant = _factory.CreateEntity("MendleafPlant");
            Assert.IsNotNull(plant, "MendleafPlant blueprint");
            var harvest = plant.GetPart<HarvestablePart>();
            Assert.IsNotNull(harvest);
            Assert.AreEqual("MendleafSprig", harvest.YieldBlueprint);

            bool garden = false;
            foreach (var stamp in StampCatalog.For(BiomeType.Jungle))
                if (stamp.Name == "MendleafGarden") garden = true;
            Assert.IsTrue(garden, "the garden stamp ships");
        }
    }
}
