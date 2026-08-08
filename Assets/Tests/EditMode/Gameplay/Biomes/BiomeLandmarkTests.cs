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
    /// BIOME-OVERHAUL Phase A2 — structure stamps + LandmarkBuilder
    /// (Docs/BIOME-OVERHAUL.md §2 A2, log in Docs/BIOME-OVERHAUL-LOG.md).
    /// Wilderness zones had ZERO structures — no buildings, no
    /// containers, nothing between "empty biome" and "full village".
    /// StructureStamp is a small hand-authored ASCII footprint (legend
    /// chars → blueprints, chest:Table markers, spawn markers);
    /// LandmarkBuilder places 0-2 per non-POI wilderness zone with the
    /// proven all-cells-passable anchor search, claims the footprint in
    /// Zone.GenReservedCells so PopulationBuilder doesn't dump a bear
    /// in the hermit's bedroom, and skips any stamp whose blueprints
    /// the factory doesn't know (keeps minimal test fixtures safe; the
    /// shipped-content test below is the loud gate).
    /// </summary>
    [TestFixture]
    public class BiomeLandmarkTests
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

        private static StructureStamp TestHut(int chance = 100, int minTier = 1)
        {
            return new StructureStamp
            {
                Name = "TestHut",
                Chance = chance,
                MinTier = minTier,
                Rows = new[]
                {
                    "#####",
                    "#c..#",
                    "#...+",
                    "#####",
                },
                Legend = new Dictionary<char, string>
                {
                    { '#', "StoneWall" },
                    { 'c', "chest:CaveSupplyT1" },
                    { '+', "" }, // door gap — deliberate opening
                },
            };
        }

        private Zone BuildCaveZoneWith(List<StructureStamp> stamps, int seed, int tier = 1)
        {
            var zone = new Zone($"Overworld.2.2.0");
            Assert.IsTrue(new CaveBuilder().BuildZone(zone, _factory, new Random(seed)));
            var builder = new LandmarkBuilder(BiomeType.Cave, tier, stamps);
            builder.BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        private static List<Entity> FindByBlueprint(Zone zone, string bp)
        {
            var found = new List<Entity>();
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == bp) found.Add(e);
            return found;
        }

        // ── 1. Placement mechanics ───────────────────────────────

        [Test]
        public void Stamp_Places_WallsAndStockedChest()
        {
            var zone = BuildCaveZoneWith(new List<StructureStamp> { TestHut() }, seed: 5);

            var chests = FindByBlueprint(zone, "Chest");
            Assert.AreEqual(1, chests.Count, "exactly one chest from one stamp");
            Assert.Greater(chests[0].GetPart<ContainerPart>().Contents.Count, 0,
                "the chest marker stocks from its loot table");

            Assert.AreEqual(13, FindByBlueprint(zone, "StoneWall").Count,
                "every '#' in the 5x4 hut becomes a wall");
        }

        [Test]
        public void Stamp_ChanceZero_NeverPlaces_CounterCheck()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var zone = BuildCaveZoneWith(
                    new List<StructureStamp> { TestHut(chance: 0) }, seed);
                Assert.AreEqual(0, FindByBlueprint(zone, "Chest").Count, $"seed {seed}");
            }
        }

        [Test]
        public void Stamp_TierGate_Holds_CounterCheck()
        {
            var zone = BuildCaveZoneWith(
                new List<StructureStamp> { TestHut(minTier: 3) }, seed: 5, tier: 1);
            Assert.AreEqual(0, FindByBlueprint(zone, "Chest").Count,
                "a MinTier-3 stamp must not appear in a tier-1 zone");
        }

        [Test]
        public void Stamp_FootprintCells_AreReserved()
        {
            var zone = BuildCaveZoneWith(new List<StructureStamp> { TestHut() }, seed: 5);
            Assert.AreEqual(20, zone.GenReservedCells.Count,
                "the full 5x4 footprint is claimed, door gap and floors included");
        }

        [Test]
        public void PopulationBuilder_RespectsReservedCells()
        {
            var zone = BuildCaveZoneWith(new List<StructureStamp> { TestHut() }, seed: 5);
            var reserved = new HashSet<(int, int)>(zone.GenReservedCells);

            // Flood the zone so any unguarded cell would very likely be hit.
            var table = new PopulationTable
            {
                Name = "Flood",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 1, MinCount = 200, MaxCount = 200 },
                }
            };
            new PopulationBuilder(table).BuildZone(zone, _factory, new Random(9));

            foreach (var snapjaw in FindByBlueprint(zone, "Snapjaw"))
            {
                var cell = zone.GetEntityCell(snapjaw);
                Assert.IsFalse(reserved.Contains((cell.X, cell.Y)),
                    $"creature spawned inside the stamp footprint at {cell.X},{cell.Y}");
            }
        }

        [Test]
        public void Stamp_UnknownBlueprint_SkippedSilently()
        {
            // Minimal-fixture safety: a stamp naming a blueprint this
            // factory doesn't know must be skipped whole, never half-placed.
            var stamp = TestHut();
            stamp.Legend['#'] = "NoSuchWall";
            var zone = BuildCaveZoneWith(new List<StructureStamp> { stamp }, seed: 5);
            Assert.AreEqual(0, FindByBlueprint(zone, "Chest").Count,
                "no partial placement when any legend entry is unknown");
        }

        [Test]
        public void Stamp_SpawnMarker_PlacesCreature()
        {
            var stamp = TestHut();
            stamp.Rows = new[]
            {
                "#####",
                "#c.b#",
                "#...+",
                "#####",
            };
            stamp.Legend['b'] = "spawn:Snapjaw";
            var zone = BuildCaveZoneWith(new List<StructureStamp> { stamp }, seed: 5);
            Assert.AreEqual(1, FindByBlueprint(zone, "Snapjaw").Count,
                "spawn: markers place their creature");
        }

        // ── 2. Shipped catalog integrity ─────────────────────────

        [Test]
        public void ShippedCatalogs_AllBiomesHaveStamps()
        {
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins })
                Assert.Greater(StampCatalog.For(biome).Count, 0, biome.ToString());
        }

        [Test]
        public void ShippedCatalogs_ValidateAgainstShippedContent()
        {
            // Every legend blueprint must exist; every chest table must
            // be in the shipped loot JSON. This is the gate that keeps
            // future stamp content honest.
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins })
            {
                foreach (var stamp in StampCatalog.For(biome))
                {
                    foreach (var kvp in stamp.Legend)
                    {
                        string marker = kvp.Value;
                        if (string.IsNullOrEmpty(marker)) continue;
                        if (marker.StartsWith("chest:"))
                        {
                            Assert.IsNotNull(LootTableRegistry.Get(marker.Substring(6)),
                                $"{stamp.Name}: loot table '{marker}'");
                        }
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

        [Test]
        public void RuinsLibraryStamp_DeliversTheDeadGrimoires()
        {
            // The point of the collapsed library: the six utility
            // grimoires that shipped with NO source finally circulate.
            var table = LootTableRegistry.Get("LibraryShelfT1");
            Assert.IsNotNull(table, "LibraryShelfT1 ships");
            var grimoires = new HashSet<string>
            {
                "KindleFlameGrimoire", "DryingBreezeGrimoire", "ChillDraftGrimoire",
                "HearthwarmGrimoire", "ConjureWaterGrimoire", "WardGleamGrimoire",
            };
            bool anyGrimoire = false;
            foreach (var entry in table.Entries)
                if (entry.Blueprint != null && grimoires.Contains(entry.Blueprint))
                    anyGrimoire = true;
            Assert.IsTrue(anyGrimoire, "the library shelf carries the orphaned utility grimoires");
        }
    }
}
