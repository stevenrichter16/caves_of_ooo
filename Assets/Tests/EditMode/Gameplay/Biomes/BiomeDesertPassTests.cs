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
    /// BIOME-OVERHAUL Phase D — the Desert pass ("The Saccharine
    /// Barrens", Docs/BIOME-OVERHAUL.md §3.2). DuneLurker (a buried
    /// AIAmbush bruiser) and BrittleHound (fast glass skirmisher with
    /// bleeding fangs) join DesertTier3; the SandstoneTomb carries
    /// PlateArmor's FIRST-EVER source behind a locked chest; and two
    /// stranded factions get their world presence — the
    /// GlassblownDrifter (25-node tree) at obelisks, the
    /// SaccharineEnvoy (21 nodes) at Concord waystations.
    /// </summary>
    [TestFixture]
    public class BiomeDesertPassTests
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
        public void DuneLurker_BuriedAmbusher()
        {
            var lurker = _factory.CreateEntity("DuneLurker");
            Assert.IsNotNull(lurker);
            Assert.AreEqual(45, lurker.GetStat("Hitpoints").Value);
            Assert.AreEqual(85, lurker.GetStat("XPValue").Value);
            Assert.IsNotNull(lurker.GetPart<AIAmbushPart>(),
                "the lurker waits under the sand — SleepingTroll's dormant pattern");
            Assert.AreEqual("2d6", HandWeapon(lurker).BaseDamage, "LurkerMaw");
        }

        [Test]
        public void BrittleHound_FastGlassSkirmisher()
        {
            var hound = _factory.CreateEntity("BrittleHound");
            Assert.IsNotNull(hound);
            Assert.AreEqual(20, hound.GetStat("Hitpoints").Value);
            Assert.AreEqual(120, hound.GetStat("Speed").Value, "pack runner");
            var w = HandWeapon(hound);
            Assert.AreEqual("1d6", w.BaseDamage);
            StringAssert.Contains("Bleeding", w.OnHitEffectsRaw,
                "glass fangs cut ragged — the hound's identity");
        }

        [Test]
        public void DesertTier3_FieldsTheNewRoster()
        {
            var t3 = PopulationTable.DesertTier3();
            bool lurker = false, hound = false;
            foreach (var e in t3.Entries)
            {
                if (e.BlueprintName == "DuneLurker") lurker = true;
                if (e.BlueprintName == "BrittleHound") hound = true;
            }
            Assert.IsTrue(lurker && hound, "the far dunes have new teeth");
        }

        [Test]
        public void SandstoneTomb_ShipsWithLockedVaultAndKey()
        {
            StructureStamp tomb = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Desert))
                if (stamp.Name == "SandstoneTomb") tomb = stamp;
            Assert.IsNotNull(tomb, "the tomb ships");
            Assert.GreaterOrEqual(tomb.MinTier, 2);

            bool locked = false, key = false, sentry = false;
            foreach (var kvp in tomb.Legend)
            {
                if (kvp.Value.StartsWith("lockedchest:TombVaultT2")) locked = true;
                if (kvp.Value == "IronKey") key = true;
                if (kvp.Value == "spawn:SkeletalSentry") sentry = true;
            }
            Assert.IsTrue(locked && key && sentry, "vault, key, and the dead who guard both");
        }

        [Test]
        public void TombVault_IsPlateArmorsFirstSource()
        {
            // PlateArmor — the game's ONLY tier-3 armor — shipped with
            // no source of any kind. The tomb vault ends that.
            var table = LootTableRegistry.Get("TombVaultT2");
            Assert.IsNotNull(table, "TombVaultT2 ships");
            bool plate = false;
            foreach (var e in table.Entries)
                if (e.Blueprint == "PlateArmor") plate = true;
            Assert.IsTrue(plate, "AV8 waits in the dark under the dunes");
        }

        [Test]
        public void StrandedFactions_GetTheirDesertPresence()
        {
            bool drifter = false, envoy = false;
            foreach (var stamp in StampCatalog.For(BiomeType.Desert))
                foreach (var kvp in stamp.Legend)
                {
                    if (kvp.Value == "spawn:GlassblownDrifter") drifter = true;
                    if (kvp.Value == "spawn:SaccharineEnvoy") envoy = true;
                }
            Assert.IsTrue(drifter, "the Glassblown Remnant mans its obelisks (25-node tree, live at last)");
            Assert.IsTrue(envoy, "the Saccharine Concord posts its envoy (21 nodes, live at last)");
        }
    }
}
