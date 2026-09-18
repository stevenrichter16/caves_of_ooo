using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W0.6 — the authored world map.
    ///
    /// <para>The overworld used to be one noise field quartiled into four
    /// biome names, which is why the lore could only ever be a label on
    /// it. It is a table now. These tests hold the table's geometry (60
    /// hand-authored rows are exactly the sort of thing that quietly
    /// grows a 21st character), the canon relationships the design is
    /// built on, and the promise that every cell still generates a zone
    /// somebody can walk around in.</para>
    /// </summary>
    public class WorldMapAuthoringTests
    {
        // ════════════════════════════════════════════════════════
        // Table geometry — the boring tests that catch real typos
        // ════════════════════════════════════════════════════════

        [Test]
        public void EveryTable_Is20x20()
        {
            foreach (var (name, rows) in new[]
            {
                ("BiomeRows", WorldMapAuthoring.BiomeRows),
                ("TierRows", WorldMapAuthoring.TierRows),
                ("RoadRows", WorldMapAuthoring.RoadRows),
                ("RiverRows", WorldMapAuthoring.RiverRows),
            })
            {
                Assert.AreEqual(WorldMap.Height, rows.Length, $"{name} row count");
                for (int y = 0; y < rows.Length; y++)
                    Assert.AreEqual(WorldMap.Width, rows[y].Length,
                        $"{name} row {y} is {rows[y].Length} chars: \"{rows[y]}\"");
            }
        }

        [Test]
        public void EveryBiomeChar_IsKnown()
        {
            // An unrecognised char silently becomes Spread. Catch it here
            // rather than wondering why a corner of the map is farmland.
            for (int y = 0; y < WorldMap.Height; y++)
                foreach (char c in WorldMapAuthoring.BiomeRows[y])
                    Assert.Contains(c, new List<char> { 'S', 'D', 'B', 'G', 'O', 'T' },
                        $"unknown biome char '{c}' in row {y}");
        }

        [Test]
        public void EveryTier_IsInRange()
        {
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    int t = WorldMapAuthoring.TierAt(x, y);
                    Assert.GreaterOrEqual(t, 1);
                    Assert.LessOrEqual(t, 5);
                }
        }

        [Test]
        public void OutOfBounds_IsSafe()
        {
            Assert.AreEqual(BiomeType.Spread, WorldMapAuthoring.BiomeAt(-1, 0));
            Assert.AreEqual(1, WorldMapAuthoring.TierAt(999, 999));
            Assert.IsFalse(WorldMapAuthoring.IsRoad(-5, -5));
            Assert.IsFalse(WorldMapAuthoring.IsRiver(20, 20));
        }

        // ════════════════════════════════════════════════════════
        // The canon relationships the design rests on
        // ════════════════════════════════════════════════════════

        [Test]
        public void Sill_IsAtTheCentre_OnSpread_AtTierOne()
        {
            var (sx, sy) = (10, 10);
            // Settlement identity is coordinate-based. Fresh-game
            // placement reads GameBootstrap.FreshGameZoneID, not this
            // table's order (see WesternVoxelSpawnTests).
            var sill = WorldMapAuthoring.PlaceAt(sx, sy);
            Assert.IsNotNull(sill, "Sill remains an authored settlement.");
            Assert.AreEqual("Sill", sill.Value.Name);
            Assert.AreEqual(sx, sill.Value.X);
            Assert.AreEqual(sy, sill.Value.Y);
            Assert.AreEqual(BiomeType.Spread, WorldMapAuthoring.BiomeAt(sx, sy));
            Assert.AreEqual(1, WorldMapAuthoring.TierAt(sx, sy),
                "Sill is in the recovered world, where the "
                + "Felling is a children's story");
        }

        [Test]
        public void TallyAndTheRoot_AreDifferentPlaces()
        {
            // Canon's own named error was making the cosmological centre
            // also the traversal centre. Tally is the hub; the Stump is
            // the destination. If these ever coincide, the map has
            // re-made the mistake Phase 2 v2 exists to fix.
            var tally = WorldMapAuthoring.PlaceAt(10, 14);
            Assert.IsNotNull(tally);
            Assert.AreEqual("Tally", tally.Value.Name);
            Assert.AreNotEqual(BiomeType.Stump,
                WorldMapAuthoring.BiomeAt(tally.Value.X, tally.Value.Y));
        }

        [Test]
        public void TheOverwrit_IsAContiguousHoleInTheWest()
        {
            // "The roads bend around it the way handwriting bends around
            // a hole in the vellum." It must be a REGION, not scattered
            // cells, or it reads as noise instead of a wound.
            int count = 0;
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                    if (WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Overwrit)
                    {
                        count++;
                        Assert.Less(x, 6, "the Overwrit is the western bite");
                    }
            Assert.GreaterOrEqual(count, 12, "big enough to read as a region");
        }

        [Test]
        public void TheStump_SitsInTheHighTierNorthwest_WithGrovelandsAround()
        {
            // Grove density rises toward the Stump because the roots were
            // densest there. Check the Stump has Grovelands neighbours.
            bool sawStump = false, sawGroveNeighbour = false;
            for (int y = 0; y < WorldMap.Height && !sawGroveNeighbour; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Stump) continue;
                    sawStump = true;
                    Assert.GreaterOrEqual(WorldMapAuthoring.TierAt(x, y), 4,
                        "the Stump is near-truth ground");
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        if (WorldMapAuthoring.BiomeAt(x + dx, y + dy) == BiomeType.Grovelands)
                            sawGroveNeighbour = true;
                }
            Assert.IsTrue(sawStump);
            Assert.IsTrue(sawGroveNeighbour);
        }

        [Test]
        public void TheRiver_RunsThroughSill()
        {
            // Sill is "a Tier-1 surface village on a slow river".
            bool adjacent = false;
            for (int dx = -3; dx <= 3; dx++)
                if (WorldMapAuthoring.IsRiver(10 + dx, 10)) adjacent = true;
            Assert.IsTrue(adjacent, "Sill must sit on its river");
        }

        [Test]
        public void EveryPlace_IsInBounds_Unique_AndFactioned()
        {
            var seen = new HashSet<(int, int)>();
            foreach (var p in WorldMapAuthoring.Places)
            {
                Assert.IsTrue(WorldMapAuthoring.InBounds(p.X, p.Y), p.Name);
                Assert.IsTrue(seen.Add((p.X, p.Y)),
                    $"two places share cell ({p.X},{p.Y}) — the second "
                    + "would overwrite the first's POI");
                Assert.IsNotEmpty(p.Name);
                Assert.IsNotEmpty(p.Faction);
            }
        }

        [Test]
        public void NoPlace_SitsInTheOverwrit()
        {
            // Nobody resettled it. A settlement there would contradict
            // the region's entire premise.
            foreach (var p in WorldMapAuthoring.Places)
                Assert.AreNotEqual(BiomeType.Overwrit,
                    WorldMapAuthoring.BiomeAt(p.X, p.Y), p.Name);
        }

        // ════════════════════════════════════════════════════════
        // The generator consumes the tables
        // ════════════════════════════════════════════════════════

        [Test]
        public void Generate_ProducesTheAuthoredBiomes_ForAnySeed()
        {
            var a = WorldGenerator.Generate(42);
            var b = WorldGenerator.Generate(7919);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    Assert.AreEqual(WorldMapAuthoring.BiomeAt(x, y), a.Tiles[x, y]);
                    Assert.AreEqual(a.Tiles[x, y], b.Tiles[x, y],
                        "the MAP is authored — only its contents are rolled");
                }
        }

        [Test]
        public void Generate_PlacesEveryAuthoredPlace()
        {
            var map = WorldGenerator.Generate(1234);
            foreach (var p in WorldMapAuthoring.Places)
            {
                var poi = map.GetPOI(p.X, p.Y);
                Assert.IsNotNull(poi, $"{p.Name} is missing from the map");
                Assert.AreEqual(p.Name, poi.Name);
                Assert.AreEqual(p.Faction, poi.Faction);
                Assert.AreEqual(WorldMapAuthoring.TierAt(p.X, p.Y), poi.Tier,
                    "POI tier must come from the authored table, not a "
                    + "second formula that can drift from it");
            }
        }

        [Test]
        public void Generate_StillRollsLairsAndCamps()
        {
            // Counter-check on the authoring: the world is decided, but
            // not everything in it is. If this passes trivially the
            // opportunistic layer has been lost.
            var a = WorldGenerator.Generate(11);
            var b = WorldGenerator.Generate(999999);
            bool differs = false;
            for (int x = 0; x < WorldMap.Width && !differs; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    var pa = a.GetPOI(x, y); var pb = b.GetPOI(x, y);
                    if ((pa == null) != (pb == null)) { differs = true; break; }
                    if (pa != null && pb != null && pa.Type != pb.Type)
                    { differs = true; break; }
                }
            Assert.IsTrue(differs,
                "different seeds must still produce different lairs/camps");
        }

        [Test]
        public void Generate_PutsNoLairOrCampOnTheStump()
        {
            // W6.2a excluded the tepui from both opportunistic rolls —
            // canon: the mountain is "designed sequence, not garrison",
            // and empirically a lair claimed (2,6)'s neighbour and the
            // slope generated as a den with zero grain. The Overwrit's
            // exclusion has had a pin since W0; this one shipped with
            // none, so deleting either `continue` stayed green.
            for (int seed = 1; seed <= 12; seed++)
            {
                var map = WorldGenerator.Generate(seed);
                for (int x = 0; x < WorldMap.Width; x++)
                    for (int y = 0; y < WorldMap.Height; y++)
                    {
                        if (map.GetBiome(x, y) != BiomeType.Stump) continue;
                        var poi = map.GetPOI(x, y);
                        // Authored landmarks are deliberately present; random
                        // lairs/camps still fail the original exclusion below.
                        if (poi?.Type == POIType.FellingSite)
                        {
                            Assert.AreEqual((FellingSiteBuilder.WorldX, FellingSiteBuilder.WorldY), (x, y));
                            continue;
                        }
                        if (poi?.Type == POIType.Sinkhole)
                        {
                            Assert.IsTrue(SinkholeSites.IsMouth(x, y));
                            continue;
                        }
                        Assert.IsNull(poi,
                            $"seed {seed}: ({x},{y}) is tepui and holds a "
                            + (poi != null ? poi.Type.ToString() : "POI"));
                    }
            }
        }

        [Test]
        public void Generate_PutsNoLairOrCampInTheOverwrit()
        {
            for (int seed = 0; seed < 12; seed++)
            {
                var map = WorldGenerator.Generate(seed * 7717 + 3);
                for (int x = 0; x < WorldMap.Width; x++)
                    for (int y = 0; y < WorldMap.Height; y++)
                    {
                        if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Overwrit) continue;
                        Assert.IsNull(map.GetPOI(x, y),
                            $"seed {seed}: something denned in the scraped region at ({x},{y})");
                    }
            }
        }

        [Test]
        public void Generate_IsDeterministic()
        {
            var a = WorldGenerator.Generate(31337);
            var b = WorldGenerator.Generate(31337);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    Assert.AreEqual(a.Tiles[x, y], b.Tiles[x, y]);
                    Assert.AreEqual(a.GetPOI(x, y)?.Name, b.GetPOI(x, y)?.Name);
                }
        }
    }
}
