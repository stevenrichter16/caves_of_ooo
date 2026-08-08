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
    /// BIOME-OVERHAUL Phase G — the Strata pass
    /// (Docs/BIOME-OVERHAUL.md §3.5). Depth finally means VARIETY, not
    /// just more snapjaws: limestone brings bears and rotlings, the
    /// shale band brings the burned and the pale, the quartzite deep
    /// brings golems and the ObsidianBrute. Underground landmarks
    /// arrive too — mine galleries, the Pale Curation's gallery (the
    /// PaleCurator: 22 authored nodes + a mineral-buying wallet,
    /// finally placed, with a rest fire), and sentinel-guarded
    /// reliquaries sharing the surface vaults' treasure table.
    /// </summary>
    [TestFixture]
    public class BiomeStrataPassTests
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
        public void PaleStalker_TheDeepsFastHunter()
        {
            var stalker = _factory.CreateEntity("PaleStalker");
            Assert.IsNotNull(stalker);
            Assert.AreEqual(50, stalker.GetStat("Hitpoints").Value);
            Assert.AreEqual(115, stalker.GetStat("Speed").Value);
            Assert.AreEqual(120, stalker.GetStat("XPValue").Value);
            Assert.AreEqual("2d5", HandWeapon(stalker).BaseDamage, "StalkerTalon");
        }

        [Test]
        public void ObsidianBrute_TheDeepsWall()
        {
            var brute = _factory.CreateEntity("ObsidianBrute");
            Assert.IsNotNull(brute);
            Assert.AreEqual(80, brute.GetStat("Hitpoints").Value);
            Assert.AreEqual(160, brute.GetStat("XPValue").Value);
            Assert.AreEqual("3d6", HandWeapon(brute).BaseDamage, "ObsidianFist");
            Assert.AreEqual("GlowQuartz", brute.GetPart<CorpsePart>().HarvestBlueprint,
                "quartz in its bones — same butchery rule as the golem");
        }

        [Test]
        public void UndergroundBands_ScaleVarietyNotJustCount()
        {
            bool Has(PopulationTable t, string bp)
            {
                foreach (var e in t.Entries)
                    if (e.BlueprintName == bp) return true;
                return false;
            }

            var shallow = PopulationTable.UndergroundTier(1);   // tier 1 — sandstone
            var lime = PopulationTable.UndergroundTier(4);      // tier 2 — limestone
            var shale = PopulationTable.UndergroundTier(7);     // tier 3 — shale
            var quartzite = PopulationTable.UndergroundTier(10); // tier 4 — quartzite

            Assert.IsFalse(Has(shallow, "CaveBear"), "the first shaft is snapjaw country");
            Assert.IsFalse(Has(shallow, "PaleStalker"));

            Assert.IsTrue(Has(lime, "CaveBear"), "bears den in the limestone");
            Assert.IsTrue(Has(lime, "Rotling"), "rot seeps down");

            Assert.IsTrue(Has(shale, "SkeletalSentry"), "the shale band remembers its dead");
            Assert.IsTrue(Has(shale, "CharredHusk"), "the fire twin finds a home at depth");
            Assert.IsTrue(Has(shale, "PaleStalker"), "and the pale hunt begins");

            Assert.IsTrue(Has(quartzite, "StoneGolem"), "golems walk the quartzite");
            Assert.IsTrue(Has(quartzite, "ObsidianBrute"), "and worse things below");
        }

        [Test]
        public void UndergroundCatalog_ScalesByBand()
        {
            // The catalog is uniform; LandmarkBuilder's MinTier gate does
            // the per-depth filtering (pinned by
            // BiomeLandmarkTests.Stamp_TierGate_Holds_CounterCheck).
            var stamps = StampCatalog.Underground(1);
            Assert.Greater(stamps.Count, 0, "the strata have landmarks");

            bool shallowReachable = false, gallery = false, reliquary = false;
            foreach (var stamp in stamps)
            {
                if (stamp.MinTier <= 1) shallowReachable = true;
                if (stamp.Name == "CurationGallery") gallery = true;
                if (stamp.Name == "Reliquary") reliquary = true;
            }
            Assert.IsTrue(shallowReachable,
                "the first shafts get SOMETHING (the mine gallery)");
            Assert.IsTrue(gallery && reliquary,
                "the deep bands carry the gallery and the reliquary");
        }

        [Test]
        public void CurationGallery_ThePaleCuratorKeepsALitHall()
        {
            StructureStamp gallery = null;
            foreach (var stamp in StampCatalog.Underground(7))
                if (stamp.Name == "CurationGallery") gallery = stamp;
            Assert.IsNotNull(gallery, "the Pale Curation curates the deep");

            bool curator = false, fire = false;
            foreach (var kvp in gallery.Legend)
            {
                if (kvp.Value == "spawn:PaleCurator") curator = true;
                if (kvp.Value == "Campfire") fire = true;
            }
            Assert.IsTrue(curator,
                "22 authored nodes + the mineral-buying wallet, finally in the world");
            Assert.IsTrue(fire, "the deep's only rest stop");
        }

        [Test]
        public void Reliquary_SentinelGuardedTreasure()
        {
            StructureStamp reliquary = null;
            foreach (var stamp in StampCatalog.Underground(10))
                if (stamp.Name == "Reliquary") reliquary = stamp;
            Assert.IsNotNull(reliquary);
            Assert.GreaterOrEqual(reliquary.MinTier, 4, "a deep-dive payoff, not a stroll");

            bool sentinel = false, locked = false, key = false;
            foreach (var kvp in reliquary.Legend)
            {
                if (kvp.Value == "spawn:VaultSentinel") sentinel = true;
                if (kvp.Value.StartsWith("lockedchest:SealedVaultT3")) locked = true;
                if (kvp.Value == "IronKey") key = true;
            }
            Assert.IsTrue(sentinel && locked && key,
                "the reliquary shares the surface vaults' treasure table");
        }

        [Test]
        public void UndergroundCatalogs_ValidateAgainstShippedContent()
        {
            foreach (var depth in new[] { 1, 4, 7, 10, 13 })
            {
                foreach (var stamp in StampCatalog.Underground(depth))
                {
                    foreach (var kvp in stamp.Legend)
                    {
                        string marker = kvp.Value;
                        if (string.IsNullOrEmpty(marker)) continue;
                        if (marker.StartsWith("chest:"))
                            Assert.IsNotNull(LootTableRegistry.Get(marker.Substring(6)),
                                $"{stamp.Name}: '{marker}'");
                        else if (marker.StartsWith("lockedchest:"))
                            Assert.IsNotNull(LootTableRegistry.Get(marker.Substring(12)),
                                $"{stamp.Name}: '{marker}'");
                        else
                        {
                            string bp = marker.StartsWith("spawn:") ? marker.Substring(6) : marker;
                            Assert.IsTrue(_factory.Blueprints.ContainsKey(bp),
                                $"{stamp.Name}: unknown blueprint '{bp}'");
                        }
                    }
                }
            }
        }
    }
}
