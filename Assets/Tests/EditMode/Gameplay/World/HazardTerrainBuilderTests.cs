using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The builder that actually puts status-holding terrain in the
    /// world.
    ///
    /// <para>Ten blueprints existed and behaved correctly for a whole
    /// commit without a single one ever spawning — they were content a
    /// player could not reach. These tests are mostly about
    /// reachability: does a generated zone contain any, do they land
    /// somewhere sane, and does the conductive-pool-plus-conductor
    /// pairing that makes them interesting actually occur.</para>
    /// </summary>
    public class HazardTerrainBuilderTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        /// <summary>An open floor zone — nothing solid, nothing placed.</summary>
        private static Zone OpenZone()
        {
            var zone = new Zone();
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                    zone.GetCell(x, y);
            return zone;
        }

        private static List<Entity> HazardsIn(Zone zone)
        {
            var found = new List<Entity>();
            foreach (var e in zone.GetAllEntities())
                if (e != null && e.HasPart<TileStateSourcePart>()) found.Add(e);
            return found;
        }

        private static int CountBlueprint(Zone zone, string bp)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e != null && e.BlueprintName == bp) n++;
            return n;
        }

        // ════════════════════════════════════════════════════════
        // Reachability — the whole point
        // ════════════════════════════════════════════════════════

        [Test]
        public void EveryBiome_ProducesHazardTerrain()
        {
            // Across a spread of seeds, each biome must produce some.
            // Per-seed it is allowed to produce none — the placement is
            // deliberately sparse — but a biome that NEVER does is a
            // table wired to nothing.
            foreach (BiomeType biome in new[]
            { BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins })
            {
                int totalPlaced = 0;
                for (int seed = 0; seed < 12; seed++)
                {
                    var zone = OpenZone();
                    new HazardTerrainBuilder(biome)
                        .BuildZone(zone, _factory, new System.Random(seed));
                    totalPlaced += HazardsIn(zone).Count;
                }

                Assert.Greater(totalPlaced, 0,
                    biome + " never produced any hazard terrain across 12 seeds");
            }
        }

        [Test]
        public void TheUndergroundTable_ProducesItsOwnColdTerrain()
        {
            // Underground has a distinct table (ice/frost); if the flag
            // were ignored it would silently generate the Cave set.
            int frostOrIce = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Cave, underground: true)
                    .BuildZone(zone, _factory, new System.Random(seed));
                frostOrIce += CountBlueprint(zone, "FrostVent") + CountBlueprint(zone, "IceSheet");
            }

            Assert.Greater(frostOrIce, 0,
                "the underground table never produced frost or ice — the "
                + "underground flag is not reaching the table");
        }

        [Test]
        public void TheDesertTable_NeverProducesFrost()
        {
            // Counter-check for the test above: if every table were the
            // same, "underground produces ice" would prove nothing.
            for (int seed = 0; seed < 20; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Desert)
                    .BuildZone(zone, _factory, new System.Random(seed));

                Assert.AreEqual(0, CountBlueprint(zone, "FrostVent"),
                    "a frost vent in the desert (seed " + seed + ")");
                Assert.AreEqual(0, CountBlueprint(zone, "IceSheet"),
                    "sheet ice in the desert (seed " + seed + ")");
            }
        }

        // ════════════════════════════════════════════════════════
        // The pairing that makes them interesting
        // ════════════════════════════════════════════════════════

        [Test]
        public void AConductivePool_SometimesGetsAConductorLeadingAway()
        {
            // A lone puddle is scenery. A puddle with a pipe run out of
            // it is a decision. Ruins is the biome where that pairing is
            // most likely and the conductor is a copper pipe.
            int zonesWithBoth = 0;
            for (int seed = 0; seed < 25; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Ruins)
                    .BuildZone(zone, _factory, new System.Random(seed));

                bool pool = CountBlueprint(zone, "BrinePool") > 0;
                bool conductor = CountBlueprint(zone, "CopperPipe") > 0;
                if (pool && conductor) zonesWithBoth++;
            }

            Assert.Greater(zonesWithBoth, 0,
                "no generated Ruins zone paired a pool with a conductor run — "
                + "the greenhouse setup never occurs");
        }

        // ════════════════════════════════════════════════════════
        // Placement sanity
        // ════════════════════════════════════════════════════════

        [Test]
        public void HazardsNeverStackOnEachOther()
        {
            // Two tile-state sources on one cell would both assert a
            // coating every turn and fight over it.
            for (int seed = 0; seed < 25; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Jungle)
                    .BuildZone(zone, _factory, new System.Random(seed));

                var occupied = new HashSet<(int, int)>();
                foreach (var e in HazardsIn(zone))
                {
                    (int x, int y) = zone.GetEntityPosition(e);
                    Assert.IsTrue(occupied.Add((x, y)),
                        "two hazards share cell (" + x + "," + y + ") at seed " + seed);
                }
            }
        }

        [Test]
        public void HazardsStayInsideTheZoneBorder()
        {
            for (int seed = 0; seed < 25; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Cave)
                    .BuildZone(zone, _factory, new System.Random(seed));

                foreach (var e in HazardsIn(zone))
                {
                    (int x, int y) = zone.GetEntityPosition(e);
                    Assert.IsTrue(x >= 1 && x < Zone.Width - 1
                               && y >= 1 && y < Zone.Height - 1,
                        "hazard placed at the border (" + x + "," + y + ")");
                }
            }
        }

        [Test]
        public void PlacementIsSparse_NotACarpet()
        {
            // These tiles change how a fight resolves. A zone paved in
            // them would make every fight the same fight.
            int worst = 0;
            for (int seed = 0; seed < 25; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Jungle)
                    .BuildZone(zone, _factory, new System.Random(seed));
                worst = Mathf.Max(worst, HazardsIn(zone).Count);
            }

            // 3 patches x 5 cells + 3 conductor runs x 6 = 33 absolute
            // ceiling; anything near a fraction of the 2000-cell zone
            // would be a carpet.
            Assert.LessOrEqual(worst, 40,
                "a zone generated " + worst + " hazard tiles — that is a carpet");
        }

        [Test]
        public void ASolidZone_PlacesNothing_AndDoesNotThrow()
        {
            // Boundary: a zone with no open floor at all.
            var zone = new Zone();
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                {
                    var wall = new Entity { ID = "w" + x + "_" + y, BlueprintName = "Wall" };
                    wall.Tags["Solid"] = "";
                    zone.AddEntity(wall, x, y);
                }

            bool ok = false;
            Assert.DoesNotThrow(() => ok = new HazardTerrainBuilder(BiomeType.Cave)
                .BuildZone(zone, _factory, new System.Random(1)));
            Assert.IsTrue(ok, "a full zone is a no-op, not a failure");
            Assert.AreEqual(0, HazardsIn(zone).Count);
        }

        [Test]
        public void NullInputs_AreRefusedRatherThanThrowing()
        {
            var b = new HazardTerrainBuilder(BiomeType.Cave);
            Assert.DoesNotThrow(() => b.BuildZone(null, _factory, new System.Random(1)));
            Assert.DoesNotThrow(() => b.BuildZone(OpenZone(), null, new System.Random(1)));
            Assert.DoesNotThrow(() => b.BuildZone(OpenZone(), _factory, null));
        }

        [Test]
        public void GenerationIsDeterministicForASeed()
        {
            // Same seed, same world — otherwise a save/reload could
            // regenerate a zone differently.
            var a = OpenZone();
            var b = OpenZone();
            new HazardTerrainBuilder(BiomeType.Ruins).BuildZone(a, _factory, new System.Random(99));
            new HazardTerrainBuilder(BiomeType.Ruins).BuildZone(b, _factory, new System.Random(99));

            var pa = new List<string>();
            foreach (var e in HazardsIn(a))
            { (int x, int y) = a.GetEntityPosition(e); pa.Add(e.BlueprintName + "@" + x + "," + y); }
            var pb = new List<string>();
            foreach (var e in HazardsIn(b))
            { (int x, int y) = b.GetEntityPosition(e); pb.Add(e.BlueprintName + "@" + x + "," + y); }

            pa.Sort(); pb.Sort();
            CollectionAssert.AreEqual(pa, pb);
        }

        [Test]
        public void ThePlacedTerrain_ActuallySeedsItsTile()
        {
            // End to end: generation puts a source down, and the turn
            // resolution makes its tile carry the status. Either half
            // alone would be useless.
            Zone withHazard = null;
            for (int seed = 0; seed < 25 && withHazard == null; seed++)
            {
                var zone = OpenZone();
                new HazardTerrainBuilder(BiomeType.Jungle)
                    .BuildZone(zone, _factory, new System.Random(seed));
                if (CountBlueprint(zone, "BrinePool") > 0 || CountBlueprint(zone, "PeatBog") > 0)
                    withHazard = zone;
            }
            Assert.IsNotNull(withHazard, "no seed produced a water-bearing hazard");

            ZoneTileStateSystem.SeedTerrainSources(withHazard);

            bool anyWet = false;
            foreach (var e in HazardsIn(withHazard))
            {
                (int x, int y) = withHazard.GetEntityPosition(e);
                if (withHazard.TileState.HasCoating(x, y, "water")) { anyWet = true; break; }
            }

            Assert.IsTrue(anyWet,
                "generated terrain exists but no tile carries its status");
        }
    }
}
