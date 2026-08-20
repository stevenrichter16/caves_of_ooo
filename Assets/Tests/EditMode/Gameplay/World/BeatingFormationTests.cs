using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W2.1 (Docs/FELLING-W1-W2-PLAN.md §7.6) — the Beating's formations.
    /// Same invariants the Spread's earned: stable per-zone selection,
    /// every pool entry reachable, one signature per formation with a
    /// counter-check, and walls that may be inconvenient but never seal.
    /// Plus two Beating-specific contracts: the salt pan CLEARS cover
    /// (exposure is the biome), and pans carry minable salt (the supply
    /// half of the W2.7 economy).
    /// </summary>
    public class BeatingFormationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        /// <summary>Open sand — what DesertBuilder leaves for a formation
        /// to shape. Optionally pre-scattered so SaltPan has cover to clear.</summary>
        private static Zone OpenPan(string id = "Overworld.16.16.0", int cactusEvery = 0)
        {
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var sand = _factory.CreateEntity("Sand");
                    if (sand != null) zone.AddEntity(sand, x, y);
                    if (cactusEvery > 0 && (x + y * Zone.Width) % cactusEvery == 0)
                    {
                        var cactus = _factory.CreateEntity("Cactus");
                        if (cactus != null) zone.AddEntity(cactus, x, y);
                    }
                }
            return zone;
        }

        private static int CountOf(Zone zone, string blueprint)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.BlueprintName == blueprint) n++;
            return n;
        }

        private static Zone Built(Formation f, int seed = 7, int cactusEvery = 0)
        {
            var zone = OpenPan(cactusEvery: cactusEvery);
            new BeatingFormationBuilder { Override = f }
                .BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        // ════════════════════════════════════════════════════════
        // Selection
        // ════════════════════════════════════════════════════════

        [Test]
        public void EveryBeatingFormationIsReachable()
        {
            var seen = new HashSet<Formation>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                    seen.Add(FormationSelector.For(BiomeType.Beating, $"Overworld.{x}.{y}.0"));

            foreach (Formation f in new[]
            {
                Formation.SaltPan, Formation.RuinField, Formation.DuneBelt,
                Formation.CaravanRoad, Formation.WindBarrens, Formation.BrineLens,
            })
                CollectionAssert.Contains(seen, f, $"{f} never comes up anywhere on the map");
        }

        [Test]
        public void TheSpreadAndTheBeatingRollFromTheirOwnPools()
        {
            // Counter-check on the pool split: the same zone id must not
            // hand the Beating a Spread formation or vice versa.
            for (int x = 0; x < 20; x++)
            {
                var b = FormationSelector.For(BiomeType.Beating, $"Overworld.{x}.16.0");
                Assert.GreaterOrEqual((int)b, (int)Formation.SaltPan,
                    $"the Beating rolled the Spread's {b}");
            }
        }

        // ════════════════════════════════════════════════════════
        // Signatures + counter-check
        // ════════════════════════════════════════════════════════

        [TestCase(Formation.SaltPan, "SaltCrust")]
        [TestCase(Formation.RuinField, "SandstoneWall")]
        [TestCase(Formation.DuneBelt, "DuneCrest")]
        [TestCase(Formation.CaravanRoad, "RoadStone")]
        [TestCase(Formation.WindBarrens, "DryBrush")]
        [TestCase(Formation.BrineLens, "BrinePool")]
        public void EachFormationLeavesItsOwnSignature(Formation f, string blueprint)
        {
            Assert.Greater(CountOf(Built(f), blueprint), 0, $"{f} placed no {blueprint}");
        }

        [Test]
        public void OneFormationsSignatureDoesNotAppearInAnother()
        {
            var pan = Built(Formation.SaltPan);
            Assert.AreEqual(0, CountOf(pan, "DuneCrest"), "the pan grew dunes");
            Assert.AreEqual(0, CountOf(pan, "RoadStone"), "the pan laid a road");

            var dunes = Built(Formation.DuneBelt);
            Assert.AreEqual(0, CountOf(dunes, "SaltCrust"), "the dunes crusted over");
            Assert.AreEqual(0, CountOf(dunes, "BrinePool"), "the dunes pooled brine");
        }

        // ════════════════════════════════════════════════════════
        // The Beating-specific contracts
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheSaltPanClearsCover_ExposureIsTheBiome()
        {
            var zone = Built(Formation.SaltPan, cactusEvery: 9);
            int interior = 0;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "Cactus") continue;
                var pos = zone.GetEntityPosition(e);
                if (pos.x >= 2 && pos.y >= 2 && pos.x < Zone.Width - 2 && pos.y < Zone.Height - 2)
                    interior++;
            }
            Assert.AreEqual(0, interior, "cover survived on the open pan");
        }

        [Test]
        public void TheSaltPanCarriesMinableSalt()
        {
            // The supply half of the W2.7 salt economy: 2-4 outcrops.
            int veins = CountOf(Built(Formation.SaltPan), "PaleSaltVein");
            Assert.GreaterOrEqual(veins, 1, "a pan with no salt is just glare");
            Assert.LessOrEqual(veins, 4);
        }

        [Test]
        public void RuinWallsNeverSealTheChunk()
        {
            // The Spread's hedge lesson, applied to walls: flood-fill
            // west→east across a seed sweep. The repair pass removes only
            // walls the builder itself placed, so this must hold on every
            // seed, not most.
            for (int seed = 0; seed < 60; seed++)
            {
                var zone = Built(Formation.RuinField, seed: seed);
                Assert.IsTrue(CrossableWestToEast(zone), $"seed {seed} sealed the chunk");
            }
        }

        [Test]
        public void RuinRooms_AlwaysHaveAtLeastOneDoor()
        {
            // The W2 mid-review's critical: the perimeter walk visited
            // each corner twice, and the antipodal gap offset mapped the
            // corner indices onto each other — so ~41% of rooms sealed
            // with BOTH doors voided, invisible to the west-east
            // crossability test. The real invariant: the flood from the
            // west must reach EVERY open cell — a sealed interior is a
            // region the flood cannot enter.
            for (int seed = 0; seed < 60; seed++)
            {
                var zone = Built(Formation.RuinField, seed: seed);
                var reached = FloodFrom(zone, westEdge: true);
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                        if (IsOpen(zone, x, y))
                            Assert.IsTrue(reached[x, y],
                                $"seed {seed}: open cell ({x},{y}) is sealed off — a room with no door");
            }
        }

        [Test]
        public void DuneBeltsLeaveStandingGaps()
        {
            // Occlusion, not blockade — the belt must be crossable
            // north-to-south too (a solid east-west band would wall the
            // zone in half).
            for (int seed = 0; seed < 20; seed++)
            {
                var zone = Built(Formation.DuneBelt, seed: seed);
                Assert.IsTrue(CrossableNorthToSouth(zone), $"seed {seed} walled the zone in half");
            }
        }

        // ════════════════════════════════════════════════════════
        // Helpers — flood fills over open ground
        // ════════════════════════════════════════════════════════

        private static bool IsOpen(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement();
        }

        private static bool[,] FloodFrom(Zone zone, bool westEdge)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            for (int y = 1; y < Zone.Height - 1; y++)
                if (IsOpen(zone, 1, y)) { seen[1, y] = true; queue.Enqueue((1, y)); }
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                        if (seen[nx, ny] || !IsOpen(zone, nx, ny)) continue;
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
            }
            return seen;
        }

        private static bool Crossable(Zone zone, bool westToEast)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            if (westToEast)
            {
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (IsOpen(zone, 1, y)) { seen[1, y] = true; queue.Enqueue((1, y)); }
            }
            else
            {
                for (int x = 1; x < Zone.Width - 1; x++)
                    if (IsOpen(zone, x, 1)) { seen[x, 1] = true; queue.Enqueue((x, 1)); }
            }
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (westToEast && x >= Zone.Width - 2) return true;
                if (!westToEast && y >= Zone.Height - 2) return true;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                        if (seen[nx, ny] || !IsOpen(zone, nx, ny)) continue;
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
            }
            return false;
        }

        private static bool CrossableWestToEast(Zone zone) => Crossable(zone, true);
        private static bool CrossableNorthToSouth(Zone zone) => Crossable(zone, false);
    }
}
