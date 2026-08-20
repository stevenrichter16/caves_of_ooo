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
    /// Formations — the anti-sameness machine
    /// (<c>Docs/FELLING-WORLD-DESIGN.md</c> §formations, W1).
    ///
    /// <para>The complaint that started the world overhaul was that every
    /// chunk of a biome was the same place with the scatter reshuffled. A
    /// formation changes the chunk's <b>topology</b>: what is a room, what
    /// is a line, what is open. These tests hold the two properties that
    /// make that work — chunks differ from each other, and a given chunk
    /// does not differ from itself between sessions.</para>
    /// </summary>
    public class SpreadFormationTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        /// <summary>An open field of grass — what the Spread's base terrain
        /// leaves behind for a formation to shape.</summary>
        private static Zone OpenField(string id = "Overworld.5.5.0")
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

        private static Dictionary<string, int> Census(Zone zone)
        {
            var counts = new Dictionary<string, int>();
            foreach (var e in zone.GetAllEntities())
            {
                string n = e.BlueprintName ?? "?";
                counts[n] = counts.TryGetValue(n, out int c) ? c + 1 : 1;
            }
            return counts;
        }

        private static int CountOf(Zone zone, string blueprint)
            => Census(zone).TryGetValue(blueprint, out int c) ? c : 0;

        // ════════════════════════════════════════════════════════
        // Selection: stable, spread out, deterministic
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheSameZoneAlwaysGetsTheSameFormation()
        {
            // Stability across calls is the weak version of the claim...
            var first = FormationSelector.For(BiomeType.Spread, "Overworld.7.9.0");
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(first, FormationSelector.For(BiomeType.Spread, "Overworld.7.9.0"));
        }

        [Test]
        public void SelectionDoesNotUseTheRandomisedStringHash()
        {
            // ...and this is the strong one. string.GetHashCode is
            // randomised per process on modern .NET, so a selector built on
            // it would give a chunk a different formation every launch — the
            // world would reshuffle itself between sessions. Pinning a known
            // key to a known index proves the hash is our own stable one.
            //
            // If this fails after a deliberate change to StableIndex, the
            // world's formation layout has changed and that is a content
            // decision, not a test fix.
            Assert.AreEqual(5, FormationSelector.StableIndex("Overworld.10.10.0", 8));
            Assert.AreEqual(3, FormationSelector.StableIndex("Overworld.0.0.0", 8));
        }

        [Test]
        public void AdjacentChunksAreOftenDifferentKindsOfPlace()
        {
            // The actual goal. Walking a line across the Spread should not
            // show you the same place repeatedly.
            var seen = new HashSet<Formation>();
            for (int x = 0; x < 20; x++)
                seen.Add(FormationSelector.For(BiomeType.Spread, $"Overworld.{x}.10.0"));

            Assert.GreaterOrEqual(seen.Count, 4,
                "20 chunks in a row produced fewer than 4 kinds of place");
        }

        [Test]
        public void EveryFormationInThePoolIsReachable()
        {
            // A pool entry that never comes up is content nobody will ever
            // see. Sweep a realistic slab of the map.
            var seen = new HashSet<Formation>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                    seen.Add(FormationSelector.For(BiomeType.Spread, $"Overworld.{x}.{y}.0"));

            foreach (Formation f in new[]
            {
                Formation.Hedgerow, Formation.FieldStrips, Formation.OldRoad,
                Formation.FlowerMeadow, Formation.Fallow, Formation.RiverMeadow,
            })
                CollectionAssert.Contains(seen, f, $"{f} never comes up anywhere on the map");
        }

        [Test]
        public void ABiomeWithoutAPoolGetsNoFormation()
        {
            // Biomes without pools (W3+) must fall back to their plain
            // recipe rather than borrowing another biome's. Re-baselined
            // in W2.1: the Beating grew its own pool, so the poolless
            // examples are now the Sodden and Cave.
            Assert.AreEqual(Formation.None, FormationSelector.For(BiomeType.Sodden, "Overworld.1.1.0"));
            Assert.AreEqual(Formation.None, FormationSelector.For(BiomeType.Cave, "Overworld.1.1.0"));
        }

        // ════════════════════════════════════════════════════════
        // The formations actually change the ground
        // ════════════════════════════════════════════════════════

        [Test]
        public void EachFormationLeavesItsOwnSignature()
        {
            var expected = new (Formation formation, string blueprint)[]
            {
                (Formation.Hedgerow, "Hedge"),
                (Formation.FieldStrips, "CropRow"),
                (Formation.OldRoad, "RoadStone"),
                // FlowerMeadow places FlowerField, not CharmFlowers, since
                // Docs/FELLING-W1-W2-PLAN.md SM3 — see FlowerFieldTests.cs
                // for the bleed-wilt instrument's own coverage.
                (Formation.FlowerMeadow, "FlowerField"),
                (Formation.RiverMeadow, "Reeds"),
            };

            foreach (var (formation, blueprint) in expected)
            {
                var zone = OpenField();
                new SpreadFormationBuilder { Override = formation }
                    .BuildZone(zone, _factory, new Random(7));
                Assert.Greater(CountOf(zone, blueprint), 0,
                    $"{formation} placed no {blueprint}");
            }
        }

        [Test]
        public void OneFormationsSignatureDoesNotAppearInAnother()
        {
            // Counter-check on the above: if every formation ran every
            // routine, the signature test would still pass and the chunks
            // would still all look identical.
            var zone = OpenField();
            new SpreadFormationBuilder { Override = Formation.FieldStrips }
                .BuildZone(zone, _factory, new Random(7));

            Assert.AreEqual(0, CountOf(zone, "RoadStone"), "crop strips laid a road");
            Assert.AreEqual(0, CountOf(zone, "FlowerField"), "crop strips grew a meadow");
        }

        [Test]
        public void TwoChunksWithDifferentFormationsDoNotLookAlike()
        {
            // The end-to-end version of the whole point.
            var hedged = OpenField();
            new SpreadFormationBuilder { Override = Formation.Hedgerow }
                .BuildZone(hedged, _factory, new Random(3));

            var road = OpenField();
            new SpreadFormationBuilder { Override = Formation.OldRoad }
                .BuildZone(road, _factory, new Random(3));

            Assert.AreNotEqual(CountOf(hedged, "Hedge"), CountOf(road, "Hedge"));
            Assert.Greater(CountOf(road, "RoadStone"), CountOf(hedged, "RoadStone"));
        }

        [Test]
        public void ARoadCrossesTheWholeChunk()
        {
            // A road that stops halfway is a decoration, not a route.
            var zone = OpenField();
            new SpreadFormationBuilder { Override = Formation.OldRoad }
                .BuildZone(zone, _factory, new Random(11));

            bool west = false, east = false;
            foreach (var e in zone.GetAllEntities())
            {
                if (e.BlueprintName != "RoadStone") continue;
                var (x, _) = zone.GetEntityPosition(e);
                if (x <= 2) west = true;
                if (x >= Zone.Width - 3) east = true;
            }
            Assert.IsTrue(west && east, "the road does not reach both edges");
        }

        [Test]
        public void HedgesNeverSealTheChunk()
        {
            // Hedges are allowed to be inconvenient. They are not allowed
            // to make a chunk you cannot cross — the formation runs before
            // connectivity (3000), but relying on that to rescue a fully
            // walled zone would be fragile.
            for (int seed = 1; seed <= 60; seed++)
            {
                var zone = OpenField();
                new SpreadFormationBuilder { Override = Formation.Hedgerow }
                    .BuildZone(zone, _factory, new Random(seed));

                Assert.IsTrue(HasOpenPathAcross(zone),
                    $"seed {seed} produced a hedgerow with no way through");
            }
        }

        [Test]
        public void FormationsOnlyDecorateOpenGround()
        {
            // A formation shapes the space the terrain builder carved; it
            // must not bury walls or stack on solid cells.
            var zone = new Zone("Overworld.5.5.0");
            for (int y = 1; y < Zone.Height - 1; y++)
            {
                var wall = _factory.CreateEntity("Wall");
                if (wall != null) zone.AddEntity(wall, 20, y);
            }

            new SpreadFormationBuilder { Override = Formation.Hedgerow }
                .BuildZone(zone, _factory, new Random(4));

            for (int y = 1; y < Zone.Height - 1; y++)
            {
                var cell = zone.GetCell(20, y);
                Assert.AreEqual(1, cell.Objects.Count,
                    $"a hedge was stacked onto the wall at 20,{y}");
            }
        }

        [Test]
        public void NullsAreSafe()
        {
            var builder = new SpreadFormationBuilder();
            Assert.DoesNotThrow(() => builder.BuildZone(null, _factory, new Random(1)));
            Assert.DoesNotThrow(() => builder.BuildZone(OpenField(), null, new Random(1)));
            Assert.DoesNotThrow(() => builder.BuildZone(OpenField(), _factory, null));
        }

        /// <summary>Flood-fill from the west edge; true if any east-edge
        /// cell is reachable.</summary>
        private static bool HasOpenPathAcross(Zone zone)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            for (int y = 1; y < Zone.Height - 1; y++)
            {
                var c = zone.GetCell(1, y);
                if (c != null && !c.BlocksMovement()) { queue.Enqueue((1, y)); seen[1, y] = true; }
            }

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x >= Zone.Width - 2) return true;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                        if (seen[nx, ny]) continue;
                        var c = zone.GetCell(nx, ny);
                        if (c == null || c.BlocksMovement()) continue;
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
            }
            return false;
        }
    }
}
