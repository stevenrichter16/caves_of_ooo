using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensityPopulationReachabilityTests
    {
        private EntityFactory factory;
        [SetUp] public void SetUp()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            Diag.ResetAll();
        }
        [TearDown] public void TearDown() { Diag.ResetAll(); }

        private sealed class FirstCellRandom : Random
        {
            public int PlacementRolls;
            public override int Next(int maxValue) { PlacementRolls++; return 0; }
        }

        private Zone Floor(params (int x, int y)[] open)
        {
            var cells = new HashSet<(int, int)>(open);
            var z = new Zone("population-passages");
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++)
                    if (!cells.Contains((x, y))) z.AddEntity(factory.CreateEntity("SandstoneWall"), x, y);
            return z;
        }

        private void PlaceOnlyAt(Zone zone, string blueprint, int x, int y, FirstCellRandom rng = null)
        {
            zone.ForEachCell((cell, cx, cy) => { if (cx != x || cy != y) zone.GenReservedCells.Add((cx, cy)); });
            var table = new PopulationTable { Name = "passage", Entries = new List<PopulationEntry> {
                new PopulationEntry { BlueprintName = blueprint, MinCount = 1, MaxCount = 1 } } };
            Assert.IsTrue(new PopulationBuilder(table).BuildZone(zone, factory, rng ?? new FirstCellRandom()));
        }

        [TestCase(false)] [TestCase(true)]
        public void SceneryCannotCloseASingleCellPassageEvenBesideAMobileCreature(bool mobileNeighbor)
        {
            var z = Floor((1, 12), (2, 12), (3, 12), (4, 12), (5, 12), (6, 12));
            if (mobileNeighbor) z.AddEntity(factory.CreateEntity("MarlbackScrabbler"), 3, 12);
            var rng = new FirstCellRandom();
            PlaceOnlyAt(z, "Stalagmite", 4, 12, rng);
            Assert.IsFalse(z.GetCell(4, 12).BlocksMovement(), "Late scenery must preserve the corridor already connected by terrain.");
            Assert.AreEqual(1, rng.PlacementRolls, "Rejecting scenery must not perturb later generation rolls.");
            var record = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "PopulationPlacementRejected" }).Records.Single();
            StringAssert.Contains("blocks_static_passage", record.PayloadJson);
            StringAssert.Contains("Stalagmite", record.PayloadJson);
        }

        [Test]
        public void DiagonalPassageIsAlsoPreserved()
        {
            var z = Floor((1, 1), (2, 2), (3, 3));
            PlaceOnlyAt(z, "Stalagmite", 2, 2);
            Assert.IsFalse(z.GetCell(2, 2).BlocksMovement());
        }

        [Test]
        public void SceneryCanOccupyADeadEnd()
        {
            var z = Floor((1, 12), (2, 12), (3, 12));
            PlaceOnlyAt(z, "Stalagmite", 3, 12);
            Assert.IsTrue(z.GetCell(3, 12).Objects.Any(e => e.BlueprintName == "Stalagmite"));
        }

        [Test]
        public void SceneryCanOccupyARoomWithAnotherRouteEvenWhenAnUnrelatedPocketExists()
        {
            var z = Floor((1, 12), (2, 12), (3, 12), (4, 12), (5, 12),
                (2, 11), (3, 11), (4, 11), (30, 20));
            PlaceOnlyAt(z, "Stalagmite", 3, 12);
            Assert.IsTrue(z.GetCell(3, 12).Objects.Any(e => e.BlueprintName == "Stalagmite"),
                "The guard preserves existing connectivity; it does not require an already perfect zone.");
        }

        [TestCase("MarlbackScrabbler")] [TestCase("Torch")]
        public void MobileCreaturesAndNonblockingItemsCanOccupyAPassage(string blueprint)
        {
            var z = Floor((1, 12), (2, 12), (3, 12));
            PlaceOnlyAt(z, blueprint, 2, 12);
            Assert.IsTrue(z.GetCell(2, 12).Objects.Any(e => e.BlueprintName == blueprint));
        }

        [TestCase(1729)] [TestCase(64)] [TestCase(2048)] [TestCase(729490642)]
        public void CompletedCathedralRetainsAccessToEveryStaticChamber(int seed)
        {
            var z = new OverworldZoneManager(factory, seed).GetZone("Overworld.5.4.2");
            foreach (var e in z.GetAllEntities().Where(e => e.HasPart<BrainPart>() || e.HasTag("Creature")).ToArray()) z.RemoveEntity(e);
            Assert.IsTrue(FormationReachability.FullyReached(z, FormationReachability.FloodFromWest(z)), "seed " + seed);
            Assert.IsTrue(z.GetAllEntities().Any(e => e.BlueprintName == "Stalagmite"), "Safe scenery remains in the Cathedral.");
        }
    }
}
