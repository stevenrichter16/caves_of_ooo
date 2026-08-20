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
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins, BiomeType.Spread, BiomeType.Beating })
                Assert.Greater(StampCatalog.For(biome).Count, 0, biome.ToString());
        }

        [Test]
        public void ShippedCatalogs_ValidateAgainstShippedContent()
        {
            // Every legend blueprint must exist; every chest table must
            // be in the shipped loot JSON. This is the gate that keeps
            // future stamp content honest.
            foreach (var biome in new[] { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins, BiomeType.Spread, BiomeType.Beating })
            {
                foreach (var stamp in StampCatalog.For(biome))
                {
                    foreach (var kvp in stamp.Legend)
                    {
                        string marker = kvp.Value;
                        if (string.IsNullOrEmpty(marker)) continue;
                        if (marker == "interior") continue;   // W2.4 pseudo-marker
                        if (marker.StartsWith("chest:"))
                        {
                            Assert.IsNotNull(LootTableRegistry.Get(marker.Substring(6)),
                                $"{stamp.Name}: loot table '{marker}'");
                        }
                        else if (marker.StartsWith("lockedchest:"))
                        {
                            Assert.IsNotNull(LootTableRegistry.Get(marker.Substring(12)),
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

        // ── 3. Docs/FELLING-W1-W2-PLAN.md SM4 — the Spread's own catalog ──

        private static Zone OpenSpreadField(string id = "Overworld.6.5.0")
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var grass = factory.CreateEntity("Grass");
                    if (grass != null) zone.AddEntity(grass, x, y);
                }
            return zone;
        }

        [Test]
        public void SpreadCatalog_DoesNotDelegateToJungle()
        {
            // The regression pin for the bug this SM fixes: a Ziggurat or
            // a Rot-Choir GroveShrine reachable in farmland. Before the
            // fix, StampCatalog.For(Spread) RETURNED For(Jungle) — the
            // same array reference.
            Assert.AreNotSame(StampCatalog.For(BiomeType.Jungle), StampCatalog.For(BiomeType.Spread));

            var spreadNames = new HashSet<string>();
            foreach (var s in StampCatalog.For(BiomeType.Spread)) spreadNames.Add(s.Name);
            CollectionAssert.DoesNotContain(spreadNames, "Ziggurat");
            CollectionAssert.DoesNotContain(spreadNames, "GroveShrine");
        }

        [TestCase("RiverShrine", "RiverShrine")]
        [TestCase("MillStead", "Farmer")]
        [TestCase("FestivalField", "FlowerField")]
        public void SpreadStamp_PlacesItsSignatureBlueprint(string stampName, string expectedBlueprint)
        {
            StructureStamp target = null;
            foreach (var s in StampCatalog.For(BiomeType.Spread))
                if (s.Name == stampName) target = s;
            Assert.IsNotNull(target, $"'{stampName}' should be in the Spread catalog");

            var forced = new StructureStamp
            {
                Name = target.Name, Chance = 100, MinTier = target.MinTier,
                Rows = target.Rows, Legend = target.Legend,
            };

            var zone = OpenSpreadField();
            var builder = new LandmarkBuilder(BiomeType.Spread, tier: 1,
                new List<StructureStamp> { forced });
            Assert.IsTrue(builder.BuildZone(zone, _factory, new Random(3)));

            Assert.Greater(FindByBlueprint(zone, expectedBlueprint).Count, 0,
                $"{stampName} placed no {expectedBlueprint}");
        }

        // ── 4. W2.4 — the tent camp and the interior pseudo-marker ──

        private static Zone OpenSand(string id = "Overworld.16.16.0")
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var sand = factory.CreateEntity("Sand");
                    if (sand != null) zone.AddEntity(sand, x, y);
                }
            return zone;
        }

        private static StructureStamp BeatingStamp(string name)
        {
            foreach (var st in StampCatalog.For(BiomeType.Beating))
                if (st.Name == name) return st;
            return null;
        }

        [Test]
        public void BeatingCatalog_NoLongerDelegatesToDesert()
        {
            Assert.AreNotSame(StampCatalog.For(BiomeType.Desert), StampCatalog.For(BiomeType.Beating));
            Assert.IsNotNull(BeatingStamp("TentRightCamp"), "the camp is the biome's thesis");
            // The shared stamps survive the extraction in BOTH catalogs.
            Assert.IsNotNull(BeatingStamp("ConcordWaystation"));
            bool desertStillHasIt = false;
            foreach (var st in StampCatalog.For(BiomeType.Desert))
                if (st.Name == "ConcordWaystation") desertStillHasIt = true;
            Assert.IsTrue(desertStillHasIt, "extraction must not have cost the Desert its waystation");
        }

        [Test]
        public void TentRightCamp_MarksItsTentInsidesInterior()
        {
            var stamp = BeatingStamp("TentRightCamp");
            var forced = new StructureStamp
            {
                Name = stamp.Name, Chance = 100, MinTier = 1,
                Rows = stamp.Rows, Legend = stamp.Legend,
                ClearsVegetation = stamp.ClearsVegetation,
            };
            var zone = OpenSand();
            var builder = new LandmarkBuilder(BiomeType.Beating, tier: 1,
                new List<StructureStamp> { forced });
            Assert.IsTrue(builder.BuildZone(zone, _factory, new Random(3)));

            // Find the camp by its pole, then check the tents' insides.
            Entity pole = null;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == "GuestClothPole") pole = e;
            Assert.IsNotNull(pole, "the camp placed no guest-cloth pole");

            int interiorCells = 0;
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    if (zone.GetCell(x, y).IsInterior) interiorCells++;
            Assert.AreEqual(2, interiorCells,
                "exactly the two tent insides are interior — the yard between tents is sky");
        }

        [Test]
        public void TheTentInside_IsShadeFromTheGlare()
        {
            // The integration that makes the biome's thesis mechanical:
            // W2.3's glare exempts interior cells, and W2.4's tents are
            // the first wilderness structure that MAKES interior cells.
            var stamp = BeatingStamp("TentRightCamp");
            var forced = new StructureStamp
            {
                Name = stamp.Name, Chance = 100, MinTier = 1,
                Rows = stamp.Rows, Legend = stamp.Legend,
                ClearsVegetation = stamp.ClearsVegetation,
            };
            var zone = OpenSand("Overworld.16.16.0");
            new LandmarkBuilder(BiomeType.Beating, tier: 1,
                new List<StructureStamp> { forced }).BuildZone(zone, _factory, new Random(3));

            (int x, int y) inside = (-1, -1);
            for (int x = 0; x < Zone.Width && inside.x < 0; x++)
                for (int y = 0; y < Zone.Height; y++)
                    if (zone.GetCell(x, y).IsInterior) { inside = (x, y); break; }
            Assert.GreaterOrEqual(inside.x, 0);

            var traveller = new Entity { ID = "t", BlueprintName = "Player" };
            traveller.Tags["Creature"] = "";
            traveller.AddPart(new RenderPart { DisplayName = "you" });
            traveller.Statistics["Strength"] = new Stat { Owner = traveller, Name = "Strength", BaseValue = 16 };
            traveller.Statistics["Agility"] = new Stat { Owner = traveller, Name = "Agility", BaseValue = 16 };
            zone.AddEntity(traveller, inside.x, inside.y);

            BeatingGlareSystem.ResetForTests();
            for (int i = 0; i < 30; i++)
                BeatingGlareSystem.OnPlayerTurnEnd(traveller, zone, 350);   // Height band

            Assert.IsFalse(traveller.HasEffect<ParchedEffect>(),
                "under the cloth, the temperature of the world changes");
        }

        [Test]
        public void RiverShrine_DonatingAtIt_WorksLikeAnyOtherShrine()
        {
            // RiverShrine reuses SanctuaryPart wholesale (zero new
            // mechanics) — pin that the donate action is really there.
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var shrine = factory.CreateEntity("RiverShrine");
            Assert.IsNotNull(shrine.GetPart<SanctuaryPart>());
        }
    }
}
