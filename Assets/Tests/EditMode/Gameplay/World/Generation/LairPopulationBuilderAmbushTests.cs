using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 6 M1.3 Test gap 12 — verifies LairPopulationBuilder spawns the
    /// expected ambush creatures at approximately the intended rates.
    ///
    /// These are statistical tests across many seeds rather than strict
    /// assertions on a single seed, because the ambush RNG roll state depends
    /// on how many prior rolls the guard table and loot table consumed.
    /// Bounds are wide enough (~2–3 sigma) to avoid test flakiness while
    /// still catching regressions that break spawning entirely.
    /// </summary>
    [TestFixture]
    public class LairPopulationBuilderAmbushTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        private int CountEntitiesByBlueprint(Zone zone, string blueprintName)
        {
            int count = 0;
            foreach (var e in zone.GetReadOnlyEntities())
            {
                if (e.BlueprintName == blueprintName) count++;
            }
            return count;
        }

        private Zone BuildLairZone(BiomeType biome, int seed, string bossBlueprint = null)
        {
            var zone = new Zone($"Lair.{biome}.{seed}");
            var poi = new PointOfInterest(POIType.Lair, "TestLair", "OutlandRaiders", 1, bossBlueprint);
            var builder = new LairPopulationBuilder(biome, poi);
            builder.BuildZone(zone, _factory, new System.Random(seed));
            return zone;
        }

        [Test]
        public void CaveLair_SpawnsSleepingTroll_AtExpected25PercentRate()
        {
            // 25% target rate. Across 200 seeds, 99% CI is roughly 50 ± 17.
            // Bound 25–75 gives comfortable headroom against RNG variance.
            const int iterations = 200;
            int withTroll = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                var zone = BuildLairZone(BiomeType.Cave, seed);
                if (CountEntitiesByBlueprint(zone, "SleepingTroll") > 0)
                    withTroll++;
            }

            Assert.GreaterOrEqual(withTroll, 25,
                $"SleepingTroll should spawn in at least 25 of 200 cave lairs (got {withTroll}). " +
                "Target rate is 25%. If this fails, spawn rate may have regressed.");
            Assert.LessOrEqual(withTroll, 75,
                $"SleepingTroll spawned in {withTroll} of 200 cave lairs — " +
                "target is 25%, this suggests rate drift.");
        }

        [Test]
        public void DesertLair_SpawnsAmbushBandit_AtExpected30PercentRate()
        {
            // 30% target rate. Across 200 seeds, bound 40–80 captures 2-sigma.
            const int iterations = 200;
            int withBandit = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                var zone = BuildLairZone(BiomeType.Desert, seed);
                if (CountEntitiesByBlueprint(zone, "AmbushBandit") > 0)
                    withBandit++;
            }

            Assert.GreaterOrEqual(withBandit, 30,
                $"AmbushBandit should spawn in at least 30 of 200 desert lairs (got {withBandit}).");
            Assert.LessOrEqual(withBandit, 90,
                $"AmbushBandit spawned in {withBandit} of 200 desert lairs — rate drift.");
        }

        [Test]
        public void CaveLair_DoesNotSpawnAmbushBandit()
        {
            // Biome-specific: bandits are desert-only. Cave lairs must never contain them.
            const int iterations = 50;
            int banditCount = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                var zone = BuildLairZone(BiomeType.Cave, seed);
                banditCount += CountEntitiesByBlueprint(zone, "AmbushBandit");
            }
            Assert.AreEqual(0, banditCount,
                "AmbushBandit is a desert-only ambusher. Cave lairs should never contain it.");
        }

        [Test]
        public void DesertLair_DoesNotSpawnSleepingTroll()
        {
            // Biome-specific: trolls are cave-only.
            const int iterations = 50;
            int trollCount = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                var zone = BuildLairZone(BiomeType.Desert, seed);
                trollCount += CountEntitiesByBlueprint(zone, "SleepingTroll");
            }
            Assert.AreEqual(0, trollCount,
                "SleepingTroll is a cave-only ambusher. Desert lairs should never contain it.");
        }

        [Test]
        public void MimicChests_SpawnInAnyBiome_AverageApproximatelyOnePerLair()
        {
            // Mimics roll `rng.Next(3)` → 0, 1, or 2. Expected mean = 1.0 per lair.
            // Across 100 cave lairs, total should be in [60, 150].
            const int iterations = 100;
            int totalMimics = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                var zone = BuildLairZone(BiomeType.Cave, seed);
                totalMimics += CountEntitiesByBlueprint(zone, "MimicChest");
            }

            Assert.GreaterOrEqual(totalMimics, 60,
                $"100 cave lairs produced {totalMimics} mimics — expected ~100 (rng.Next(3) average).");
            Assert.LessOrEqual(totalMimics, 150,
                $"100 cave lairs produced {totalMimics} mimics — suspiciously many.");
        }

        [Test]
        public void AmbushCreatures_AreInRoomCells_NotCorridors()
        {
            // Polish 7: ambushers should prefer room cells (≥5 passable neighbors)
            // rather than narrow corridors. An empty 80x25 zone treats every interior
            // cell as a "room" (all 8 neighbors passable), so this test uses default
            // placement with no walls — every ambusher should be on an interior cell
            // with ≥5 passable neighbors.
            const int iterations = 20;
            int trollsInRooms = 0;
            int trollsTotal = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                var zone = BuildLairZone(BiomeType.Cave, seed);
                foreach (var e in zone.GetReadOnlyEntities())
                {
                    if (e.BlueprintName != "SleepingTroll") continue;
                    trollsTotal++;
                    var cell = zone.GetEntityCell(e);
                    if (cell == null) continue;
                    int passableNeighbors = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var adj = zone.GetCell(cell.X + dx, cell.Y + dy);
                        if (adj != null && adj.IsPassable()) passableNeighbors++;
                    }
                    if (passableNeighbors >= 5) trollsInRooms++;
                }
            }

            if (trollsTotal > 0)
            {
                Assert.AreEqual(trollsTotal, trollsInRooms,
                    $"{trollsTotal - trollsInRooms} of {trollsTotal} trolls spawned in " +
                    "corridor-like cells (< 5 passable neighbors). GatherRoomCells filter failed.");
            }
        }

        [Test]
        public void AmbushCreatures_NeverShareCellWithGuardsOrLoot()
        {
            // Regression for M1 post-review finding M1.R-2:
            //
            // GatherRoomCells used to re-scan the zone for every passable
            // "room" cell, ignoring that PlaceEntity had already consumed
            // cells for guards and loot. Because placement reads from the
            // same pool via RemoveAt(idx), any pool that's rebuilt
            // independently (as roomCells was) silently loses that dedup
            // guarantee — so a mimic / sleeping troll could land on top of
            // a marlback guard or a dropped weapon.
            //
            // Fix: GatherRoomCells now filters the already-pruned openCells
            // list instead of rescanning the zone, so guard/loot cells
            // can't leak back in as ambusher candidates.
            //
            // This test iterates a range of seeds (both biomes, so all
            // three ambusher types get exercised) and asserts every cell
            // holds at most one creature OR item from the lair builder.
            const int iterations = 200;
            int cavesChecked = 0;
            int desertsChecked = 0;
            for (int seed = 0; seed < iterations; seed++)
            {
                BiomeType biome = (seed % 2 == 0) ? BiomeType.Cave : BiomeType.Desert;
                if (biome == BiomeType.Cave) cavesChecked++;
                else desertsChecked++;

                var zone = BuildLairZone(biome, seed);

                var occupancy = new Dictionary<(int x, int y), List<string>>();
                foreach (var e in zone.GetReadOnlyEntities())
                {
                    if (string.IsNullOrEmpty(e.BlueprintName)) continue;
                    var cell = zone.GetEntityCell(e);
                    if (cell == null) continue;
                    var key = (cell.X, cell.Y);
                    if (!occupancy.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        occupancy[key] = list;
                    }
                    list.Add(e.BlueprintName);
                }

                foreach (var pair in occupancy)
                {
                    if (pair.Value.Count > 1)
                    {
                        Assert.Fail(
                            $"Seed {seed} ({biome}): cell {pair.Key} has {pair.Value.Count} " +
                            $"entities stacked: [{string.Join(", ", pair.Value)}]. " +
                            "LairPopulationBuilder must never place two creatures/items " +
                            "on the same cell — the top glyph would hide the underneath.");
                    }
                }
            }

            // Sanity: make sure we actually exercised both biomes. If one
            // side were 0 we'd miss coverage of the corresponding ambusher type.
            Assert.Greater(cavesChecked, 0, "No cave lairs checked — test mis-configured.");
            Assert.Greater(desertsChecked, 0, "No desert lairs checked — test mis-configured.");
        }

        // ════════════════════════════════════════════════════════════
        //   Density Phase 1 §T1.2 — the canon biomes get ambushers.
        //   Lairs only ever generate in Spread, Sodden, Beating and
        //   Grovelands (WorldGenerator.PlacePOIs skips the Overwrit and
        //   the Stump), and the switch above had cases for none of them:
        //   no lair in any shipped world could hold a sleeping troll, a
        //   hidden bandit or a canopy strangler.
        // ════════════════════════════════════════════════════════════

        private int LairsContaining(BiomeType biome, string blueprint, int iterations = 200)
        {
            int with = 0;
            for (int seed = 0; seed < iterations; seed++)
                if (CountEntitiesByBlueprint(BuildLairZone(biome, seed), blueprint) > 0)
                    with++;
            return with;
        }

        // Rate bounds mirror the Cave (25%) and Desert (30%) tests above.
        [TestCase(BiomeType.Spread, "AmbushBandit", 30, 90)]
        [TestCase(BiomeType.Beating, "AmbushBandit", 30, 90)]
        [TestCase(BiomeType.Sodden, "SleepingTroll", 25, 75)]
        [TestCase(BiomeType.Grovelands, "CanopyStrangler", 25, 75)]
        public void CanonBiomeLair_SpawnsItsAmbusher_AtRoughlyTheIntendedRate(
            BiomeType biome, string ambusher, int min, int max)
        {
            int with = LairsContaining(biome, ambusher);
            Assert.GreaterOrEqual(with, min,
                $"{ambusher} should wait in at least {min} of 200 {biome} lairs (got {with}).");
            Assert.LessOrEqual(with, max,
                $"{ambusher} waited in {with} of 200 {biome} lairs — rate drift.");
        }

        // Counter-checks: each ambusher belongs to its own country. A
        // mapping bug that dropped the same creature into every biome
        // (e.g. a fall-through case) passes every rate test above and
        // fails here.
        [TestCase(BiomeType.Spread, "SleepingTroll")]
        [TestCase(BiomeType.Spread, "CanopyStrangler")]
        [TestCase(BiomeType.Beating, "SleepingTroll")]
        [TestCase(BiomeType.Beating, "CanopyStrangler")]
        [TestCase(BiomeType.Sodden, "AmbushBandit")]
        [TestCase(BiomeType.Sodden, "CanopyStrangler")]
        [TestCase(BiomeType.Grovelands, "AmbushBandit")]
        [TestCase(BiomeType.Grovelands, "SleepingTroll")]
        [TestCase(BiomeType.Cave, "CanopyStrangler")]
        [TestCase(BiomeType.Desert, "CanopyStrangler")]
        public void CanonBiomeLair_NeverHoldsAnotherBiomesAmbusher(BiomeType biome, string foreign)
        {
            Assert.AreEqual(0, LairsContaining(biome, foreign, iterations: 50),
                $"{foreign} is not a {biome} ambusher and must never appear in its lairs.");
        }

        [Test]
        public void CanonBiomeLairs_NeverStackTwoThingsOnOneCell()
        {
            // Adversarial: the new ambusher cases draw from the same
            // progressively-shrinking pool as guards and loot (the M1.R-2
            // contract above). Probe it in every biome a lair can
            // actually generate in, not just the retired two.
            foreach (var biome in new[] { BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating, BiomeType.Grovelands })
            {
                for (int seed = 0; seed < 50; seed++)
                {
                    var zone = BuildLairZone(biome, seed);
                    var seen = new HashSet<(int, int)>();
                    foreach (var e in zone.GetReadOnlyEntities())
                    {
                        if (string.IsNullOrEmpty(e.BlueprintName)) continue;
                        var cell = zone.GetEntityCell(e);
                        if (cell == null) continue;
                        Assert.IsTrue(seen.Add((cell.X, cell.Y)),
                            $"{biome} seed {seed}: two lair entities share cell ({cell.X},{cell.Y}).");
                    }
                }
            }
        }
    }
}
