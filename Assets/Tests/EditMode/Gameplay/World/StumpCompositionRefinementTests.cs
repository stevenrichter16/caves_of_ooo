using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Native-data regressions from the first gameplay-camera look:
    /// clipped spray basins and uniformly scattered summit habitat. These tests
    /// judge generation rules across seeds, never a manually repaired screenshot.</summary>
    public class StumpCompositionRefinementTests
    {
        private EntityFactory factory;
        private static readonly (int x, int y)[] Directions = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        [SetUp] public void SetUp() => factory = GrovelandsCompositionTests.Factory();

        // H1: generic portal routes carve through basin interiors, leaving tiny
        // wet fragments. The revised contract is THREE intact unequal basins,
        // aligned as a drainage chain with dry necks/ledges between them. It does
        // not promise a physical bridge or one continuous native liquid body.
        [TestCase("Overworld.2.1.0")]
        [TestCase("Overworld.1.2.0")]
        [TestCase("Overworld.5.5.0")]
        public void GorgeKeepsThreeIntactUnequalBasinsAcrossSeeds(string id)
        {
            foreach (int seed in Seeds())
            {
                var zone = Build(id, seed, Formation.CascadeGorge, out var plan);
                var sizes = PoolComponents(zone);
                string context = id + " seed " + seed + " component sizes " + string.Join(",", sizes);
                Assert.AreEqual(3, sizes.Count, context);
                Assert.IsTrue(sizes.All(size => size >= 15), "No clipped slivers or decorative one-cell puddles: " + context);
                Assert.Greater(sizes.Distinct().Count(), 1, "Unequal landing basins: " + context);
                foreach (var pool in zone.GetAllEntities().Where(e => e.BlueprintName == "SprayPool"))
                {
                    var cell = zone.GetEntityCell(pool);
                    Assert.IsFalse(plan.IsApproach(cell.X, cell.Y), context);
                    Assert.IsTrue(zone.GenReservedCells.Contains((cell.X, cell.Y)), context);
                    Assert.AreEqual(1, cell.Objects.Count(e => e.BlueprintName == "SprayPool"), context);
                }
            }
            var control = Build("Overworld.2.2.0", 64, Formation.Grainfield, out _);
            Assert.AreEqual(0, PoolComponents(control).Count, "The basin guarantee belongs to CascadeGorge only.");
        }

        // H2: tanks satisfy a population count while missing their place in the
        // landscape. Scrub tanks belong against stone knuckles; rim tanks belong
        // inside the humid tree crack. Every tank must satisfy that relationship,
        // not merely one conveniently close specimen in a uniform scatter.
        [TestCase("Overworld.3.2.0", Formation.SummitScrub, "StoneDome", 2)]
        [TestCase("Overworld.2.3.0", Formation.RimForest, "Tree", 3)]
        public void EverySummitTankBelongsToItsNativeActivityCluster(string id, Formation form, string anchor, int radius)
        {
            foreach (int seed in Seeds())
            {
                var zone = Build(id, seed, form, out var plan);
                var tanks = zone.GetAllEntities().Where(e => e.BlueprintName == "TankBrocchinia").ToArray();
                Assert.AreEqual(8, tanks.Length, form + " seed " + seed + " must retain all eight native cover cells.");
                foreach (var tank in tanks)
                {
                    var cell = zone.GetEntityCell(tank);
                    string context = form + " seed " + seed + " tank " + cell.X + "," + cell.Y;
                    Assert.IsTrue(Near(zone, cell.X, cell.Y, anchor, radius), context + " is detached from " + anchor);
                    Assert.IsFalse(cell.BlocksMovement(), context + " must remain usable cover.");
                    Assert.IsFalse(plan.IsApproach(cell.X, cell.Y), context + " cannot consume a dry approach.");
                    Assert.IsTrue(StumpFaunaHabitat.Allows("BrocchiniaSentinel", cell), context);
                }
            }
            var control = Build("Overworld.2.2.0", 64, Formation.Grainfield, out _);
            Assert.IsFalse(control.GetAllEntities().Any(e => e.BlueprintName == "TankBrocchinia"),
                "The cluster requirement must not introduce summit tanks on the slopes.");
        }

        // H3: rendering richer pools and cups accidentally adds permanent
        // wetness, drinking or combat-liquid contact. Inspect actual generated
        // owners, then run the native source consumer with a real brine control.
        [TestCase("Overworld.2.1.0", Formation.CascadeGorge, "SprayPool")]
        [TestCase("Overworld.3.2.0", Formation.SummitScrub, "TankBrocchinia")]
        [TestCase("Overworld.2.3.0", Formation.RimForest, "TankBrocchinia")]
        public void RefinedWaterSceneryDoesNotInventLiquidOrTileStateMechanics(string id, Formation form, string blueprint)
        {
            var zone = Build(id, 64, form, out _);
            var scenery = zone.GetAllEntities().Where(e => e.BlueprintName == blueprint).ToArray();
            Assert.IsNotEmpty(scenery, "The test must exercise actual generated scenery.");
            Assert.AreEqual(0, zone.TileState.WrittenCount);
            foreach (var owner in scenery)
            {
                Assert.IsNull(owner.GetPart<LiquidPoolPart>(), blueprint + " is not a liquid interaction source.");
                Assert.IsNull(owner.GetPart<TileStateSourcePart>(), blueprint + " is not a native coating source.");
                Assert.IsFalse(owner.GetPart<PhysicsPart>().Solid);
            }
            ZoneTileStateSystem.SeedTerrainSources(zone);
            Assert.AreEqual(0, zone.TileState.WrittenCount, "Art refinement must not write hidden permanent water.");

            var source = factory.CreateEntity("BrinePool");
            Assert.NotNull(source.GetPart<LiquidPoolPart>());
            Assert.NotNull(source.GetPart<TileStateSourcePart>());
            Assert.IsTrue(zone.AddEntity(source, 0, 0));
            ZoneTileStateSystem.SeedTerrainSources(zone);
            Assert.AreEqual(4, zone.TileState.CoatingTurns(0, 0, "water"), "Real native water must still seed normally.");
            Assert.AreEqual(1, zone.TileState.WrittenCount);
            zone.RemoveEntity(source);
            for (int turn = 0; turn < 6; turn++)
            {
                ZoneTileStateSystem.SeedTerrainSources(zone);
                zone.TileState.Tick();
            }
            Assert.AreEqual(0, zone.TileState.WrittenCount, "Scenery must not renew the removed real source.");
            foreach (var owner in scenery) Assert.NotNull(zone.GetEntityCell(owner), "The scenery control survives source removal.");
        }

        private Zone Build(string id, int seed, Formation form, out StumpCompositionPlan plan)
        {
            var zone = new Zone(id);
            var builder = new StumpCompositionBuilder(seed) { FormationOverride = form };
            Assert.IsTrue(builder.BuildZone(zone, factory, new Random(seed)), id + " seed " + seed);
            plan = builder.Plan;
            Assert.NotNull(plan);
            return zone;
        }

        private static IEnumerable<int> Seeds()
        {
            for (int seed = 0; seed < 21; seed++) yield return seed;
            yield return 64;
            yield return 1729;
            yield return 729490642;
        }

        private static bool Near(Zone zone, int x, int y, string blueprint, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
            {
                var cell = zone.GetCell(x + dx, y + dy);
                if (cell != null && cell.Objects.Any(e => e.BlueprintName == blueprint)) return true;
            }
            return false;
        }

        private static List<int> PoolComponents(Zone zone)
        {
            var remaining = new HashSet<(int x, int y)>();
            foreach (var pool in zone.GetAllEntities().Where(e => e.BlueprintName == "SprayPool"))
            {
                var cell = zone.GetEntityCell(pool);
                remaining.Add((cell.X, cell.Y));
            }
            var sizes = new List<int>();
            var queue = new Queue<(int x, int y)>();
            while (remaining.Count > 0)
            {
                var start = remaining.First();
                remaining.Remove(start); queue.Enqueue(start);
                int size = 0;
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue(); size++;
                    foreach (var direction in Directions)
                    {
                        var next = (cell.x + direction.x, cell.y + direction.y);
                        if (remaining.Remove(next)) queue.Enqueue(next);
                    }
                }
                sizes.Add(size);
            }
            sizes.Sort();
            return sizes;
        }
    }
}
