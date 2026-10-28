using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A bounded addition to fresh SummitScrub: real passable bushes,
    /// with the prior landform, routes, habitat and population kept intact.</summary>
    public sealed class StumpSummitScrubPopulationTests
    {
        private DensityLootTestScope content;
        private static readonly int[] Seeds = { 0, 1, 17, 64, 65, 12345, int.MaxValue, int.MinValue };
        private static readonly string[] SummitIds = { "Overworld.3.4.0", "Overworld.3.2.0" };

        [SetUp] public void SetUp() => content = new DensityLootTestScope();
        [TearDown] public void TearDown() => content.Dispose();

        // Captured from the unchanged native plan before this addition, in
        // summit-scrub-protected-plan-baselines.json. These are frozen hashes,
        // not expectations regenerated from the implementation under test.
        private static readonly object[] ProtectedPlans =
        {
            new object[] { "Overworld.3.4.0", 0, "255d8a7c40e2614d1c72126fb641073459ee9a7c945d61967f00cfacf3530a7f" },
            new object[] { "Overworld.3.4.0", 1, "22f668d41b75663ade6c44c14079a3fef612f90337ae5ca22eb67dd98a6cf8ee" },
            new object[] { "Overworld.3.4.0", 17, "0709febd518aed5404c1c758025f49b4f35d89d1850ba4e9c35d9e1b2920e30e" },
            new object[] { "Overworld.3.4.0", 64, "8e0f479c85219c58f02e15c97a27d94f04bdce22d5a81027b8eadc008ca2cc24" },
            new object[] { "Overworld.3.4.0", 65, "4ffc87e9c9d0d491aec14b18a59140938ae7a4aa72718632b05a0ec35130f5fe" },
            new object[] { "Overworld.3.4.0", 12345, "e87de18dd3aff5e706048afc6cd861d71a8d6bf5bfe50d1730bd002e66ddb038" },
            new object[] { "Overworld.3.4.0", int.MaxValue, "6e9ca7e884502e01845880e81cce7f4fe8ecf541a9b83dab9a158208893bab2a" },
            new object[] { "Overworld.3.4.0", int.MinValue, "f27b7ab6ceabee954523f258ccd8ebbdd333e5829f07c3f1ff12b816c6b87ba7" },
            new object[] { "Overworld.3.2.0", 0, "0b22731e6d03e25d0bf0cc4cff32ed160d322623484e850a207b172a464231e0" },
            new object[] { "Overworld.3.2.0", 1, "ef49e645d806e8c1a3a03f0e6912dcac8e68807d90a668283e880824eb97d355" },
            new object[] { "Overworld.3.2.0", 17, "3f8c7feff50c8bf5d6f937cee4e8e260cb1c6d65ea9ae0d24b28aa9d3425a2d6" },
            new object[] { "Overworld.3.2.0", 64, "6c63f4a20a40dbec00cdae02eea01a1f8624d927f4c2532fa7780e51ab5e100c" },
            new object[] { "Overworld.3.2.0", 65, "83b26c6d799c6076f4e16cf3645606ef3ff29e5f830179b5fb05352d52734dba" },
            new object[] { "Overworld.3.2.0", 12345, "38e7ef255f10c21679fb9a017f9345e4e33d02ad754c5b815158ef5e5f0aba5b" },
            new object[] { "Overworld.3.2.0", int.MaxValue, "ec206ef67a351ab0dc2fa5f3d38912ab5dabf5b2e0eca77f6a95b6d1397c0e67" },
            new object[] { "Overworld.3.2.0", int.MinValue, "3116468040a919e01a60c4857ce21ea407d825c2c2e586351ddc5e53c7ee8ed9" }
        };

        [TestCaseSource(nameof(ProtectedPlans))]
        public void ScrubOnlyAddsBushToPreviouslyEmptyProtectedCells(string id, int seed, string expected)
        {
            var plan = StumpCompositionPlan.Create(id, seed);
            Assert.AreEqual(Formation.SummitScrub, plan.Formation);
            Assert.AreEqual(expected, Hash(plan.Signature().Replace("Bush", ".")),
                "Every old object position, approach and habitat flag must survive; only empty cells may gain Bush.");
            Assert.AreEqual(8, Cells(plan, "TankBrocchinia").Count);
            Assert.AreEqual(6, Cells(plan, "Tree").Count);
        }

        [TestCase("Overworld.3.4.0")] [TestCase("Overworld.3.2.0")]
        public void ScrubHasTwelveToTwentyFourBushesInTwoOrThreeUnequalShortDrifts(string id)
        {
            foreach (int seed in Seeds)
            {
                var bushes = Cells(StumpCompositionPlan.Create(id, seed), "Bush");
                string context = id + " seed " + seed;
                Assert.That(bushes.Count, Is.InRange(12, 24), context);
                var groups = Components(bushes);
                Assert.That(groups.Count, Is.InRange(2, 3), "Distinct local drifts, not isolated scatter: " + context);
                Assert.Greater(groups.Select(g => g.Count).Distinct().Count(), 1, "Drifts must differ in size: " + context);
                foreach (var group in groups)
                {
                    Assert.GreaterOrEqual(group.Count, 3, "An isolated one- or two-cell fleck is not a drift: " + context);
                    Assert.LessOrEqual(group.Max(c => c.x) - group.Min(c => c.x) + 1, 10, context);
                    Assert.LessOrEqual(group.Max(c => c.y) - group.Min(c => c.y) + 1, 10, context);
                }
            }
        }

        [Test] public void Seed64DriftsReachThreeSeparatedShelterNeighborhoods()
        {
            // Actual V1 spent two drifts around the same western dome. Three
            // disconnected shapes alone do not distribute cover across the area.
            var plan = StumpCompositionPlan.Create(SummitIds[0], 64);
            var groups = Components(Cells(plan, "Bush"));
            Assert.AreEqual(3, groups.Count, "This observed seed must reach three shelter neighborhoods.");
            var centres = groups.Select(g => (x: g.Average(c => c.x), y: g.Average(c => c.y))).ToArray();
            for (int i = 0; i < centres.Length; i++) for (int j = i + 1; j < centres.Length; j++)
            {
                double dx = centres[i].x - centres[j].x, dy = centres[i].y - centres[j].y;
                Assert.GreaterOrEqual(dx * dx + dy * dy, 12d * 12d,
                    "Nearby drifts spend density on the same shelter: " + centres[i] + " and " + centres[j]);
            }
        }

        [TestCase("Overworld.3.4.0")] [TestCase("Overworld.3.2.0")]
        public void EveryBushStaysInExistingShelteredHabitatOutsideTheApproachHalo(string id)
        {
            foreach (int seed in Seeds)
            {
                var plan = StumpCompositionPlan.Create(id, seed);
                var bushes = Cells(plan, "Bush");
                Assert.That(bushes.Count, Is.InRange(12, 24));
                foreach (var c in bushes)
                {
                    string context = id + " seed " + seed + " at " + c;
                    Assert.IsTrue(plan.IsHabitat(c.x, c.y), context);
                    Assert.That(c.x, Is.InRange(2, Zone.Width - 3), context);
                    Assert.That(c.y, Is.InRange(2, Zone.Height - 3), context);
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        Assert.IsFalse(plan.IsApproach(c.x + dx, c.y + dy), "Reserved route plus one-cell margin: " + context);
                    Assert.IsTrue(Near(plan, c, "Tree", 3) || Near(plan, c, "StoneDome", 3),
                        "Cover must belong to existing shelter: " + context);
                }
            }
        }

        [TestCase(64)] [TestCase(65)]
        public void RealBuilderProducesUniquePassablePlantOwnersAndKeepsRoutesReachable(int seed)
        {
            var zone = Build(SummitIds[0], seed, out var builder);
            var bushes = zone.GetAllEntities().Where(e => e.BlueprintName == "Bush").ToArray();
            Assert.That(bushes.Length, Is.InRange(12, 24));
            CollectionAssert.AreEquivalent(Cells(builder.Plan, "Bush"), bushes.Select(e =>
            { var c = zone.GetEntityCell(e); return (c.X, c.Y); }));
            foreach (var bush in bushes)
            {
                var cell = zone.GetEntityCell(bush);
                Assert.AreEqual(1, cell.Objects.Count(e => e.BlueprintName == "Bush"));
                Assert.AreEqual(1, cell.Objects.Count(e => e.BlueprintName == "TepuiStone"));
                Assert.IsTrue(zone.GenReservedCells.Contains((cell.X, cell.Y)));
                Assert.IsFalse(bush.GetPart<PhysicsPart>().Solid);
                Assert.IsFalse(cell.BlocksMovement());
                Assert.AreEqual("Plant", bush.GetPart<MaterialPart>().MaterialID);
                Assert.AreEqual(6, bush.GetPart<DestructiblePart>().HP);
                Assert.AreEqual(6, bush.GetPart<DestructiblePart>().MaxHP);
                Assert.IsTrue(string.IsNullOrEmpty(bush.GetPart<DestructiblePart>().WreckageBlueprint));
                Assert.IsNull(bush.GetPart<HarvestablePart>());
                Assert.IsNull(bush.GetPart<InventoryPart>());
                Assert.IsNull(bush.GetPart<BrainPart>());
                Assert.IsFalse(zone.TileState.HasCoating(cell.X, cell.Y, "water"));
            }
            Assert.IsTrue(FormationReachability.FullyReached(zone, FormationReachability.FloodFromWest(zone)));
        }

        [Test] public void ScrubIsRepeatableAndDoesNotConsumeThePipelineRandomStream()
        {
            var rng = new NoDrawRandom();
            foreach (string id in SummitIds)
            {
                var builder = new StumpCompositionBuilder(64);
                Assert.IsTrue(builder.BuildZone(new Zone(id), content.Factory, rng));
                Assert.That(Cells(builder.Plan, "Bush").Count, Is.InRange(12, 24));
                Assert.AreEqual(builder.Plan.Signature(), StumpCompositionPlan.Create(id, 64).Signature());
                Assert.AreNotEqual(string.Join(";", Cells(builder.Plan, "Bush")),
                    string.Join(";", Cells(StumpCompositionPlan.Create(id, 65), "Bush")),
                    "The same stamp must not repeat at every world seed.");
            }
        }

        [TestCase(64, 1)] [TestCase(64, 17)] [TestCase(65, 1)] [TestCase(65, 17)]
        public void OrdinaryPopulationHasIdenticalSpeciesAndLocationsWithBushesPresentOrRemoved(int seed, int populationSeed)
        {
            var table = PopulationTable.GetStumpTable(StumpBand.Summit, 3);
            var withBush = Build(SummitIds[0], seed, out var withTerrain);
            var withoutBush = Build(SummitIds[0], seed, out var withoutTerrain);
            var removed = withoutBush.GetAllEntities().Where(e => e.BlueprintName == "Bush").ToArray();
            Assert.That(removed.Length, Is.InRange(12, 24));
            foreach (var bush in removed) Assert.IsTrue(withoutBush.RemoveEntity(bush));
            var protectedCells = withBush.GenReservedCells.ToArray();
            CollectionAssert.AreEquivalent(protectedCells, withoutBush.GenReservedCells);
            var originalWith = new HashSet<Entity>(withBush.GetAllEntities());
            var originalWithout = new HashSet<Entity>(withoutBush.GetAllEntities());
            content.Seed(populationSeed);
            Assert.IsTrue(new StumpHabitatPopulationBuilder(withTerrain, table)
                .BuildZone(withBush, content.Factory, new Random(populationSeed)));
            content.Seed(populationSeed);
            Assert.IsTrue(new StumpHabitatPopulationBuilder(withoutTerrain, table)
                .BuildZone(withoutBush, content.Factory, new Random(populationSeed)));
            var actual = AddedPopulation(withBush, originalWith);
            Assert.IsNotEmpty(actual, "Exercise the real native population, not an empty table.");
            CollectionAssert.AreEqual(AddedPopulation(withoutBush, originalWithout), actual);
            CollectionAssert.AreEquivalent(protectedCells, withBush.GenReservedCells);
            CollectionAssert.AreEquivalent(protectedCells, withoutBush.GenReservedCells);
        }

        [Test] public void DestroyedBushLeavesNoLootAndCannotBeRegrownByRejectedRebuild()
        {
            var zone = Build(SummitIds[0], 64, out var builder);
            var bushes = zone.GetAllEntities().Where(e => e.BlueprintName == "Bush").ToArray();
            Assert.That(bushes.Length, Is.InRange(12, 24));
            var before = zone.GetAllEntities().ToArray();
            DestructionSystem.Destroy(bushes[0], null, zone, "summit-scrub-test");
            Assert.IsNull(zone.GetEntityCell(bushes[0]));
            CollectionAssert.AreEquivalent(before.Where(e => e != bushes[0]), zone.GetAllEntities(),
                "Ordinary scrub must not add harvest loot or destruction drops.");
            var after = zone.GetAllEntities().ToArray();
            var reserved = zone.GenReservedCells.ToArray();
            Assert.IsFalse(builder.BuildZone(zone, content.Factory, new NoDrawRandom()));
            Assert.IsNull(builder.Plan);
            CollectionAssert.AreEquivalent(after, zone.GetAllEntities());
            CollectionAssert.AreEquivalent(reserved, zone.GenReservedCells);
        }

        [TestCase(false)] [TestCase(true)]
        public void SaveReloadKeepsOldBareAndPlayerEditedScrubZonesWithoutGeneration(bool keepRemainingScrub)
        {
            var zone = Build(SummitIds[0], 64, out var builder);
            var bushes = zone.GetAllEntities().Where(e => e.BlueprintName == "Bush").ToArray();
            if (keepRemainingScrub)
            {
                Assert.That(bushes.Length, Is.InRange(12, 24));
                zone.RemoveEntity(bushes[0]);
                bushes[1].GetPart<DestructiblePart>().HP = 2;
            }
            else foreach (var bush in bushes) zone.RemoveEntity(bush);
            var player = content.Factory.CreateEntity("Player");
            Assert.IsTrue(zone.AddEntity(player, 0, builder.Plan.WestY));
            var expected = OwnerRows(zone);
            var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            manager.SetActiveZone(zone);
            var turns = new TurnManager(); turns.AddEntity(player);
            var state = GameSessionState.Capture("summit-scrub-test", "test", manager, turns, player);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                loaded = GameSessionState.Load(new SaveReader(stream, content.Factory));
            }
            var restored = loaded.ZoneManager.GetZone(zone.ZoneID);
            CollectionAssert.AreEqual(expected, OwnerRows(restored));
            Assert.AreSame(restored, loaded.ZoneManager.GetZone(zone.ZoneID));
            Assert.IsFalse(new StumpCompositionBuilder(64).BuildZone(restored, content.Factory, new NoDrawRandom()));
            CollectionAssert.AreEqual(expected, OwnerRows(restored));
        }

        [Test] public void MissingBushBlueprintRejectsFreshScrubBeforeAnyGroundOrReservationIsPlaced()
        {
            Assert.IsTrue(content.Factory.Blueprints.Remove("Bush"));
            var zone = new Zone(SummitIds[0]);
            var builder = new StumpCompositionBuilder(64);
            Assert.IsFalse(builder.BuildZone(zone, content.Factory, new NoDrawRandom()));
            Assert.IsNull(builder.Plan);
            Assert.AreEqual(0, zone.EntityCount);
            Assert.IsEmpty(zone.GenReservedCells);
        }

        private Zone Build(string id, int seed, out StumpCompositionBuilder builder)
        {
            var zone = new Zone(id); builder = new StumpCompositionBuilder(seed);
            Assert.IsTrue(builder.BuildZone(zone, content.Factory, new Random(seed)));
            Assert.AreEqual(Formation.SummitScrub, builder.Plan.Formation);
            return zone;
        }
        private static List<(int x, int y)> Cells(StumpCompositionPlan plan, string blueprint)
        {
            var result = new List<(int x, int y)>();
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (plan.ObjectAt(x, y) == blueprint) result.Add((x, y));
            return result;
        }
        private static bool Near(StumpCompositionPlan plan, (int x, int y) c, string blueprint, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                if (plan.ObjectAt(c.x + dx, c.y + dy) == blueprint) return true;
            return false;
        }
        private static List<List<(int x, int y)>> Components(IEnumerable<(int x, int y)> cells)
        {
            var remaining = new HashSet<(int x, int y)>(cells);
            var groups = new List<List<(int x, int y)>>();
            var queue = new Queue<(int x, int y)>();
            while (remaining.Count > 0)
            {
                var first = remaining.First(); remaining.Remove(first); queue.Enqueue(first);
                var group = new List<(int x, int y)>();
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue(); group.Add(c);
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    {
                        var next = (c.x + dx, c.y + dy);
                        if (remaining.Remove(next)) queue.Enqueue(next);
                    }
                }
                groups.Add(group);
            }
            return groups;
        }
        private static string[] AddedPopulation(Zone zone, HashSet<Entity> original)
            => zone.GetAllEntities().Where(e => !original.Contains(e)).Select(e =>
            { var c = zone.GetEntityCell(e); return e.BlueprintName + "@" + c.X + "," + c.Y; }).OrderBy(s => s).ToArray();
        private static string[] OwnerRows(Zone zone)
            => zone.GetAllEntities().Select(e =>
            { var c = zone.GetEntityCell(e); return e.ID + ":" + e.BlueprintName + "@" + c.X + "," + c.Y + ":" + e.GetPart<DestructiblePart>()?.HP; })
                .OrderBy(s => s).ToArray();
        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        private sealed class NoDrawRandom : Random
        {
            public override int Next() => throw new InvalidOperationException("Terrain consumed the pipeline RNG.");
            public override int Next(int maxValue) => Next();
            public override int Next(int minValue, int maxValue) => Next();
            public override double NextDouble() => Next();
            public override void NextBytes(byte[] buffer) => throw new InvalidOperationException("Terrain consumed the pipeline RNG.");
        }
    }
}
