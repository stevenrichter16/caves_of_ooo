using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Root-approved six-cell right-hand garden. The existing broad floor seeder
    // remains unchanged; only this authored fresh-start terrain is initialized.
    public sealed class GameAuditMorrowfastGardenTests
    {
        private static readonly (int x, int y)[] Garden = {
            (41, 21), (41, 22), (41, 23), (42, 21), (42, 22), (42, 23) };
        private static Entity Terrain(Zone zone, int x, int y) => zone.GetCell(x, y).Objects
            .Single(e => e.HasTag(MorrowfastSceneRuntime.TerrainTag));
        private static HashSet<(int x, int y)> PlantableCells(Zone zone)
        {
            var cells = new HashSet<(int, int)>();
            foreach (var entity in zone.GetAllEntities())
                if (entity.HasTag("Terrain") && entity.HasTag("Plantable")) cells.Add(zone.GetEntityPosition(entity));
            return cells;
        }
        private static void EnsureGarden(Zone zone)
        {
            var type = typeof(MorrowfastSceneRuntime).Assembly.GetType("CavesOfOoo.Core.MorrowfastStartingGarden");
            Assert.NotNull(type, "Narrow authored-garden initializer is required.");
            var method = type.GetMethod("Ensure", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method); method.Invoke(null, new object[] { zone });
        }
        private static void ArrangeAtActualStart(MorrowfastStartFixture f, bool prepareGarden = true)
        {
            f.UseActualVillage();
            // Explicit fixture placement isolates the farming RED from the
            // independently failing preferred-start placement method.
            Assert.IsTrue(f.Zone.AddEntity(f.Player, 40, 23));
            if (prepareGarden) EnsureGarden(f.Zone);
        }
        private static void AssertDefinitionSafe()
        {
            var definition = MorrowfastSceneDefinition.Load();
            foreach (var p in Garden)
            {
                var cell = definition.cells.Single(c => c.x == p.x && c.y == p.y);
                Assert.IsFalse(cell.solid || cell.water || cell.interior, "Unsafe garden terrain " + p);
                Assert.IsNull(definition.RoomAt(p.x, p.y));
                Assert.IsFalse(definition.owners.Any(o => (o.anchorX, o.anchorY) == p || o.footprint.Any(q => (q.x, q.y) == p)), "Authored owner occupies " + p);
                Assert.IsFalse(MorrowfastContent.AdditionalResidents.Any(o => (o.X, o.Y) == p), "Additional resident occupies " + p);
                Assert.LessOrEqual(Math.Max(Math.Abs(p.x - 40), Math.Abs(p.y - 23)), FarmPlotSeeder.PLOT_RADIUS);
            }
        }
        [Test] public void NarrowInitializerAndBootstrapFarmPreserveExactTerrainAndOnlySixGardenCells()
        {
            using (var f = new MorrowfastStartFixture())
            {
                AssertDefinitionSafe(); ArrangeAtActualStart(f, prepareGarden: false);
                var terrain = f.Zone.GetAllEntities().Where(e => e.HasTag(MorrowfastSceneRuntime.TerrainTag)).ToArray();
                Assert.AreEqual(2000, terrain.Length); var ids = terrain.ToDictionary(e => e, e => e.ID);
                var positions = terrain.ToDictionary(e => e, e => f.Zone.GetEntityPosition(e));
                var owners = MorrowfastSceneDefinition.Load().owners.ToDictionary(o => o.id, o => MorrowfastSceneRuntime.FindOwner(f.Zone, o.id));
                EnsureGarden(f.Zone); f.Invoke("EnsureFarmPlotAtSpawn");
                CollectionAssert.AreEquivalent(Garden, PlantableCells(f.Zone));
                foreach (var e in terrain)
                {
                    var at = positions[e]; Assert.AreSame(e, Terrain(f.Zone, at.x, at.y)); Assert.AreEqual(ids[e], e.ID);
                    Assert.AreEqual(Garden.Contains(at), e.HasTag("Plantable"), "Only designated garden changes " + at);
                }
                foreach (var pair in owners) Assert.AreSame(pair.Value, MorrowfastSceneRuntime.FindOwner(f.Zone, pair.Key), pair.Key);
                Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(f.Player));
                var seen = MorrowfastTestWorld.Flood(f.Zone, f.Player, 40, 23);
                Assert.IsTrue(seen.Contains((40, 0))); Assert.IsTrue(seen.Contains((40, 24)));
                foreach (var p in Garden) Assert.IsTrue(seen.Contains(p), "Plantable patch must also be reachable: " + p);
            }
        }
        [Test] public void RepeatedBootstrapFarmSetupDoesNotDuplicateOrExpandTheGarden()
        {
            using (var f = new MorrowfastStartFixture())
            {
                ArrangeAtActualStart(f); f.Invoke("EnsureFarmPlotAtSpawn");
                var all = f.Zone.GetAllEntities().ToArray(); var soils = Garden.Select(p => Terrain(f.Zone, p.x, p.y)).ToArray();
                CollectionAssert.AreEquivalent(soils, f.Zone.GetEntitiesWithTag("Plantable"));
                // Ordinary road terrain must remain outside the plantable index.
                var road = Terrain(f.Zone, 40, 23); Assert.IsFalse(road.HasTag("Plantable"));
                Assert.IsFalse(f.Zone.GetEntitiesWithTag("Plantable").Contains(road));
                EnsureGarden(f.Zone); f.Invoke("EnsureFarmPlotAtSpawn");
                CollectionAssert.AreEquivalent(soils, f.Zone.GetEntitiesWithTag("Plantable"));
                CollectionAssert.AreEquivalent(all, f.Zone.GetAllEntities());
                CollectionAssert.AreEquivalent(Garden, PlantableCells(f.Zone));
                for (int i = 0; i < Garden.Length; ++i) Assert.AreSame(soils[i], Terrain(f.Zone, Garden[i].x, Garden[i].y));
            }
        }
        [TestCase("Overworld.10.10.0")] [TestCase("Overworld.3.6.0")]
        public void SameCoordinatesOutsideAnActiveAuthoredMorrowfastDoNotRetagSpecialStone(string id)
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.UseBareZone(id); Assert.IsFalse(MorrowfastSceneRuntime.IsActive(f.Zone));
                Assert.IsTrue(f.Zone.AddEntity(f.Player, 40, 23));
                var stones = new List<Entity>();
                foreach (var p in Garden)
                {
                    var stone = f.Factory.CreateEntity("TepuiStone"); Assert.NotNull(stone);
                    Assert.IsTrue(f.Zone.AddEntity(stone, p.x, p.y)); stones.Add(stone);
                }
                EnsureGarden(f.Zone); f.Invoke("EnsureFarmPlotAtSpawn");
                foreach (var stone in stones) { Assert.IsFalse(stone.HasTag("Plantable")); Assert.NotNull(f.Zone.GetEntityCell(stone)); }
                // Existing generic farming may create Grass in OTHER bare cells.
                // This control only protects these special stone instances.
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void RealCarriedSeedPlantsOnlyOnTheGardenAndPaysOneUnit(bool garden)
        {
            using (var f = new MorrowfastStartFixture())
            {
                ArrangeAtActualStart(f); f.Invoke("EnsureFarmPlotAtSpawn"); SeedPart.Factory = f.Factory;
                var seed = f.Factory.CreateEntity("CandyCarrotSeed"); Assert.NotNull(seed); var stack = seed.GetPart<StackerPart>();
                Assert.NotNull(stack); stack.StackCount = 2;
                Assert.IsTrue(f.Player.GetPart<InventoryPart>().AddObject(seed));
                var p = garden ? Garden[0] : (x: 40, y: 23); Assert.IsTrue(f.Zone.MoveEntity(f.Player, p.x, p.y));
                Assert.IsFalse(f.Zone.GetCell(p.x, p.y).HasObjectWithPart<CropPart>());
                var result = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(seed, "PlantSeed"), f.Player, f.Zone);
                Assert.AreEqual(garden, result.Success, result.ErrorMessage);
                Assert.AreEqual(garden ? 1 : 2, stack.StackCount);
                Assert.AreEqual(garden ? 1 : 0, f.Zone.GetCell(p.x, p.y).Objects.Count(e => e.HasPart<CropPart>()));
            }
        }
        [Test] public void ActualFreshGenerationWiresGardenBeforeOrdinaryFarmGuarantee()
        {
            using (var f = new MorrowfastStartFixture())
            {
                f.Generate(); f.Place(); Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(f.Player));
                CollectionAssert.AreEquivalent(Garden, PlantableCells(f.Zone));
                var before = f.Zone.GetAllEntities().ToArray();
                f.Invoke("EnsureFarmPlotAtSpawn");
                CollectionAssert.AreEquivalent(before, f.Zone.GetAllEntities(), "Generic seeder should already have six cells and no-op.");
            }
        }
        [Test] public void FullLoadDoesNotReinitializeAPlayerChangedGarden()
        {
            using (var f = new MorrowfastStartFixture())
            {
                ArrangeAtActualStart(f); CollectionAssert.AreEquivalent(Garden, PlantableCells(f.Zone));
                var changed = Garden[0]; Terrain(f.Zone, changed.x, changed.y).Tags.Remove("Plantable");
                var loaded = f.RoundTrip(); f.Bootstrap.ApplyLoadedGame(loaded);
                Assert.IsFalse(Terrain(f.Zone, changed.x, changed.y).HasTag("Plantable"));
                CollectionAssert.AreEquivalent(Garden.Skip(1), PlantableCells(f.Zone));
                Assert.AreEqual((40, 23), f.Zone.GetEntityPosition(f.Player));
            }
        }
        [Test] public void GardenTagsAndTerrainIdentityPersistInNormalSaveWithoutRelocatingPlayer()
        {
            using (var f = new MorrowfastStartFixture())
            {
                ArrangeAtActualStart(f); f.Invoke("EnsureFarmPlotAtSpawn");
                Assert.IsTrue(f.Zone.MoveEntity(f.Player, 40, 22));
                var ids = Garden.Select(p => Terrain(f.Zone, p.x, p.y).ID).ToArray();
                var loaded = f.RoundTrip(); f.Bootstrap.ApplyLoadedGame(loaded);
                CollectionAssert.AreEquivalent(Garden, PlantableCells(f.Zone));
                for (int i = 0; i < Garden.Length; ++i) Assert.AreEqual(ids[i], Terrain(f.Zone, Garden[i].x, Garden[i].y).ID);
                Assert.AreEqual((40, 22), f.Zone.GetEntityPosition(f.Player));
            }
        }
    }
}
