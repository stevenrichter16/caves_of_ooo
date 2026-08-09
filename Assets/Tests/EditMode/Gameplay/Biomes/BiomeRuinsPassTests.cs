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
    /// BIOME-OVERHAUL Phase F — the Ruins pass ("The Palimpsest
    /// Fields", Docs/BIOME-OVERHAUL.md §3.4). The loot-dense biome:
    /// the VaultSentinel stands before SealedVaults holding the SEVEN
    /// dead attack grimoires + PalimpsestBlade + SeveranceEdge behind
    /// the game's first placed LockedDoor; ClockworkWorkshops circulate
    /// all three schematics; RuneCultSites put the RuneCultist (and its
    /// complete trap-laying AI) into the world; and the
    /// PalimpsestArchive places the Echo — the game's LARGEST authored
    /// conversation (42 nodes) — with a rest fire beside it.
    /// </summary>
    [TestFixture]
    public class BiomeRuinsPassTests
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
        public void VaultSentinel_StandsItsGround()
        {
            var sentinel = _factory.CreateEntity("VaultSentinel");
            Assert.IsNotNull(sentinel);
            Assert.AreEqual(60, sentinel.GetStat("Hitpoints").Value);
            Assert.AreEqual(550 /* round-6 beta audit: tier-scaled XP (t3 x5, t2 x3) */, sentinel.GetStat("XPValue").Value);
            Assert.AreEqual("2d6", HandWeapon(sentinel).BaseDamage, "SentinelHalberd");
        }

        [Test]
        public void SealedVault_LockedDoorSentinelAndTheGrimoireHoard()
        {
            StructureStamp vault = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Ruins))
                if (stamp.Name == "SealedVault") vault = stamp;
            Assert.IsNotNull(vault, "the sealed vault ships");
            Assert.GreaterOrEqual(vault.MinTier, 2);

            bool door = false, locked = false, key = false, sentinel = false;
            foreach (var kvp in vault.Legend)
            {
                if (kvp.Value == "LockedDoor") door = true;
                if (kvp.Value.StartsWith("lockedchest:SealedVaultT3")) locked = true;
                if (kvp.Value == "IronKey") key = true;
                if (kvp.Value == "spawn:VaultSentinel") sentinel = true;
            }
            Assert.IsTrue(door, "the game's first placed LockedDoor");
            Assert.IsTrue(locked && key && sentinel);
        }

        [Test]
        public void SealedVaultT3_HoldsTheSevenDeadAttackGrimoires()
        {
            // 1,275 drams of authored spell content shipped with NO
            // source: the entire attack tier. The vault ends that.
            var table = LootTableRegistry.Get("SealedVaultT3");
            Assert.IsNotNull(table);
            var wanted = new[]
            {
                "ArcBoltGrimoire", "IceLanceGrimoire", "AcidSprayGrimoire",
                "ConflagrationGrimoire", "RimeNovaGrimoire", "ThunderclapGrimoire",
                "EmberVeinGrimoire",
            };
            foreach (var g in wanted)
            {
                bool found = false;
                foreach (var e in table.Entries)
                    if (e.Blueprint == g) found = true;
                Assert.IsTrue(found, $"{g} finally circulates");
            }

            bool blade = false, edge = false;
            foreach (var e in table.Entries)
            {
                if (e.Blueprint == "PalimpsestBlade") blade = true;
                if (e.Blueprint == "SeveranceEdge") edge = true;
            }
            Assert.IsTrue(blade && edge, "the dead uniques ride along");
        }

        [Test]
        public void ClockworkWorkshop_CirculatesAllThreeSchematics()
        {
            StructureStamp shop = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Ruins))
                if (stamp.Name == "ClockworkWorkshop") shop = stamp;
            Assert.IsNotNull(shop, "the workshop ships");

            var table = LootTableRegistry.Get("WorkshopCacheT2");
            Assert.IsNotNull(table);
            foreach (var s in new[]
            {
                "SchematicHonedEdge", "SchematicReinforcedPlating", "SchematicDuelistCut",
            })
            {
                bool found = false;
                foreach (var e in table.Entries)
                    if (e.Blueprint == s) found = true;
                Assert.IsTrue(found, $"{s} — three dead tinker recipes become learnable");
            }
        }

        [Test]
        public void RuneCultSite_PutsTheCultistsInTheWorld()
        {
            StructureStamp site = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Ruins))
                if (stamp.Name == "RuneCultSite") site = stamp;
            Assert.IsNotNull(site, "the Rune Cult digs in");

            int cultists = 0;
            foreach (var kvp in site.Legend)
                if (kvp.Value == "spawn:RuneCultist") cultists++;
            Assert.GreaterOrEqual(cultists, 1,
                "cultists spawn with their complete AILayRune trap AI — live traps at last");
        }

        [Test]
        public void PalimpsestArchive_TheEchoSpeaksAtLast()
        {
            StructureStamp archive = null;
            foreach (var stamp in StampCatalog.For(BiomeType.Ruins))
                if (stamp.Name == "PalimpsestArchive") archive = stamp;
            Assert.IsNotNull(archive, "the Archive ships");

            bool echo = false, fire = false;
            foreach (var kvp in archive.Legend)
            {
                if (kvp.Value == "spawn:PalimpsestEcho") echo = true;
                if (kvp.Value == "Campfire") fire = true;
            }
            Assert.IsTrue(echo, "the 42-node conversation — the game's largest — gets a speaker");
            Assert.IsTrue(fire, "and a scholar's fire to rest at");
        }
    }
}
