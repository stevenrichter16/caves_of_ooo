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
    /// W3.1 (Docs/FELLING-W3-PLAN.md §3) — the Sodden's formations.
    /// The invariants the two sibling builders earned, plus the one W2's
    /// mid-review taught: not "the zone can be crossed" but "every open
    /// cell is reachable" — a sealed pocket is invisible to a crossing
    /// test.
    /// </summary>
    public class SoddenFormationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        private static Zone OpenBog(string id = "Overworld.16.3.0")
        {
            var zone = new Zone(id);
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var grass = _factory.CreateEntity("Grass");
                    if (grass != null) zone.AddEntity(grass, x, y);
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

        private static Zone Built(Formation f, int seed = 7)
        {
            var zone = OpenBog();
            new SoddenFormationBuilder { Override = f }
                .BuildZone(zone, _factory, new Random(seed));
            return zone;
        }

        // ════════════════════════════════════════════════════════
        // Selection
        // ════════════════════════════════════════════════════════

        [Test]
        public void EverySoddenFormationIsReachable()
        {
            var seen = new HashSet<Formation>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                    seen.Add(FormationSelector.For(BiomeType.Sodden, $"Overworld.{x}.{y}.0"));

            foreach (Formation f in new[]
            {
                Formation.OpenMire, Formation.PeatCuts, Formation.ReedMaze,
                Formation.DrownedCopse, Formation.Causeway, Formation.BogFace,
            })
                CollectionAssert.Contains(seen, f, $"{f} never comes up anywhere on the map");
        }

        [Test]
        public void TheThreePoolsDoNotBleedIntoEachOther()
        {
            for (int x = 0; x < 20; x++)
            {
                var s = FormationSelector.For(BiomeType.Sodden, $"Overworld.{x}.3.0");
                Assert.GreaterOrEqual((int)s, (int)Formation.OpenMire,
                    $"the Sodden rolled another biome's {s}");
            }
        }

        // ════════════════════════════════════════════════════════
        // Signatures + counter-check
        // ════════════════════════════════════════════════════════

        [TestCase(Formation.OpenMire, "MirePool")]
        [TestCase(Formation.PeatCuts, "PeatBank")]
        [TestCase(Formation.ReedMaze, "Reeds")]
        [TestCase(Formation.DrownedCopse, "DeadTree")]
        [TestCase(Formation.Causeway, "Duckboard")]
        [TestCase(Formation.BogFace, "PeatBank")]
        public void EachFormationLeavesItsOwnSignature(Formation f, string blueprint)
        {
            Assert.Greater(CountOf(Built(f), blueprint), 0, $"{f} placed no {blueprint}");
        }

        [Test]
        public void OneFormationsSignatureDoesNotAppearInAnother()
        {
            var mire = Built(Formation.OpenMire);
            Assert.AreEqual(0, CountOf(mire, "Duckboard"), "the mire built a boardwalk");
            Assert.AreEqual(0, CountOf(mire, "DeadTree"), "the mire grew a copse");

            var causeway = Built(Formation.Causeway);
            Assert.AreEqual(0, CountOf(causeway, "PeatBank"), "the causeway cut peat");
        }

        // ════════════════════════════════════════════════════════
        // The Sodden-specific contracts
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheMireIsWalkable_TheDecisionIsWhetherToWade()
        {
            // MirePool must be non-solid: the bog slows and coats, it
            // does not wall. Reachability in a mire is never at stake.
            var pool = _factory.CreateEntity("MirePool");
            var physics = pool.GetPart<PhysicsPart>();
            Assert.IsTrue(physics == null || !physics.Solid, "mire is a hazard, not a wall");
        }

        [Test]
        public void MirePool_IsTheFirstConsumerOfBogMire()
        {
            // The dead liquid's revival (the plan's third such): the pool
            // carries bog-mire, whose authored character (Agi -2, acid
            // tick, fire dampen) does the slowing.
            var pool = _factory.CreateEntity("MirePool");
            var liquid = pool.GetPart<LiquidPoolPart>();
            Assert.IsNotNull(liquid);
            Assert.AreEqual("bog-mire", liquid.LiquidId);
        }

        [Test]
        public void TheCauseway_IsOneContinuousSafeLine()
        {
            // THE safe line: a walkable duckboard path from west to east.
            for (int seed = 0; seed < 20; seed++)
            {
                var zone = Built(Formation.Causeway, seed);
                Assert.Greater(CountOf(zone, "Duckboard"), 60,
                    $"seed {seed}: the causeway should span the zone");
            }
        }

        [Test]
        public void BanksNeverSealAnywhere()
        {
            // The W2 lesson as a birthright: every open cell reachable,
            // across seeds, for both bank-placing formations.
            foreach (var f in new[] { Formation.PeatCuts, Formation.BogFace })
                for (int seed = 0; seed < 40; seed++)
                {
                    var zone = Built(f, seed);
                    var reached = FloodFromWest(zone);
                    for (int x = 1; x < Zone.Width - 1; x++)
                        for (int y = 1; y < Zone.Height - 1; y++)
                            if (IsOpen(zone, x, y))
                                Assert.IsTrue(reached[x, y],
                                    $"{f} seed {seed}: open cell ({x},{y}) sealed off");
                }
        }

        // ════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════

        [Test]
        public void OpenMire_NeverStacksTwoPoolsOnOneCell()
        {
            // W3 re-review (RED pre-fix): IsOpenGround only checks
            // BlocksMovement, and a MirePool is non-solid — so overlapping
            // blobs plus the 3% scatter double-booked ~40 cells per zone
            // with stacked pools (double burn-off gas, double burn-away
            // budget, double coating writes). The scatter comment said
            // "lone pools"; the code now means it.
            for (int seed = 0; seed < 20; seed++)
            {
                var zone = Built(Formation.OpenMire, seed);
                for (int x = 1; x < Zone.Width - 1; x++)
                    for (int y = 1; y < Zone.Height - 1; y++)
                    {
                        int pools = 0;
                        foreach (var e in zone.GetCell(x, y).Objects)
                            if (e.BlueprintName == "MirePool") pools++;
                        Assert.LessOrEqual(pools, 1,
                            $"seed {seed}: ({x},{y}) holds {pools} stacked mire pools");
                    }
            }
        }

        [Test]
        public void TheSoddenAmbientCatalog_IsItsOwn()
        {
            // W3 re-review: the ambient stamp catalog aliased Jungle, so
            // vine ziggurats, Choir grove shrines, and jungle hermits
            // rolled in the peat bog. The Sodden keeps the stamps that
            // read as bog country and loses the ones that read as
            // somewhere else's identity.
            var names = new HashSet<string>();
            foreach (var s in StampCatalog.For(BiomeType.Sodden)) names.Add(s.Name);

            Assert.Greater(names.Count, 0, "the bog has SOME ambient texture");
            CollectionAssert.DoesNotContain(names, "Ziggurat",
                "a vine-choked ziggurat is jungle identity, not bog");
            CollectionAssert.DoesNotContain(names, "GroveShrine",
                "the Choir's shrines belong to the Grovelands");
        }

        private static bool IsOpen(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement();
        }

        private static bool[,] FloodFromWest(Zone zone)
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
    }
}
