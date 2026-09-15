using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class OverwritCompositionTests
    {
        public static readonly string[] ExampleIds =
        {
            "Overworld.1.10.0", "Overworld.0.9.0",
            "Overworld.0.13.0", "Overworld.4.11.0"
        };

        public static ZoneGenerationPipeline Pipeline(OverworldZoneManager manager, string id)
            => (ZoneGenerationPipeline)typeof(OverworldZoneManager)
                .GetMethod("GetPipelineForZone", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(manager, new object[] { id });

        // The Unsaying is reserved only in authoring comments: a generic POI
        // check alone would silently replace the future authored town.
        [Test]
        public void ExactTwentyTwoOrdinaryAddressesExcludeTheReservedUnsaying()
        {
            var expected = new HashSet<string>();
            int[] widths = { 3, 4, 5, 5, 4, 4 };
            for (int y = 9; y <= 14; y++)
                for (int x = y == 14 ? 2 : 0; x < widths[y - 9]; x++)
                    if (x != 2 || y != 11) expected.Add(WorldMap.ToZoneID(x, y));
            Assert.AreEqual(22, expected.Count);
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    string id = WorldMap.ToZoneID(x, y);
                    Assert.AreEqual(expected.Contains(id), OverwritCompositionPlan.IsWildernessZone(id), id);
                }
            Assert.IsFalse(OverwritCompositionPlan.IsWildernessZone("Overworld.2.11.0"));
        }

        [TestCase(null)] [TestCase("")] [TestCase("Overworld.1.10")]
        [TestCase("Overworld.01.10.0")] [TestCase("Overworld.1.10.-1")]
        [TestCase("Overworld.1.10.1")] [TestCase("Overworld.-1.10.0")]
        [TestCase("Overworld.20.10.0")] [TestCase("Overworld.1.10.0.extra")]
        [TestCase(" Overworld.1.10.0")] [TestCase("Overworld.2.11.0")]
        public void UnsupportedAddressesCannotConstructAPlan(string id)
        {
            Assert.IsFalse(OverwritCompositionPlan.IsWildernessZone(id));
            Assert.Throws<ArgumentException>(() => OverwritCompositionPlan.Create(id, 64));
        }

        [TestCase(0, OverwritFormation.Blank)]
        [TestCase(1, OverwritFormation.GreenRim)]
        [TestCase(2, OverwritFormation.DryRim)]
        [TestCase(3, OverwritFormation.PilgrimageRim)]
        public void SurfaceRoleFollowsTheActualNeighborInsteadOfRandomNewLore(int example, OverwritFormation form)
        {
            foreach (int seed in new[] { int.MinValue, -1, 0, 64, int.MaxValue })
            {
                var p = OverwritCompositionPlan.Create(ExampleIds[example], seed);
                Assert.AreEqual(form, p.Formation);
                Assert.AreEqual(form == OverwritFormation.Blank ? OverwritRole.Blank : OverwritRole.Rim, p.Role);
                Assert.AreEqual(p.Signature(), OverwritCompositionPlan.Create(p.ZoneID, seed).Signature());
            }
        }

        [Test]
        public void SeedsChangeSparsePlacementWithoutChangingTheLevelBlankGround()
        {
            foreach (string id in ExampleIds)
            {
                var placements = new HashSet<string>();
                for (int seed = 0; seed < 8; seed++)
                {
                    var p = OverwritCompositionPlan.Create(id, seed);
                    var occupied = new List<string>();
                    for (int y = 0; y < Zone.Height; y++)
                        for (int x = 0; x < Zone.Width; x++)
                            if (p.ObjectAt(x, y) != null) occupied.Add(x + "," + y + ":" + p.ObjectAt(x, y));
                    placements.Add(string.Join(";", occupied));
                }
                Assert.Greater(placements.Count, 1, id + ": seed must change placement, not only metadata");
            }
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(int.MaxValue)]
        public void SharedEdgesAgreeOnThreeCellApproaches(int seed)
        {
            var p = OverwritCompositionPlan.Create("Overworld.1.10.0", seed);
            Assert.AreEqual(p.EastY, OverwritCompositionPlan.Create("Overworld.2.10.0", seed).WestY);
            Assert.AreEqual(p.SouthX, OverwritCompositionPlan.Create("Overworld.1.11.0", seed).NorthX);
            for (int d = -1; d <= 1; d++)
            {
                Assert.IsTrue(p.IsApproach(0, p.WestY + d));
                Assert.IsTrue(p.IsApproach(Zone.Width - 1, p.EastY + d));
                Assert.IsTrue(p.IsApproach(p.NorthX + d, 0));
                Assert.IsTrue(p.IsApproach(p.SouthX + d, Zone.Height - 1));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void NativeSurfaceIsStonelessSparseAndEveryOpenCellCanBeReached(int example)
        {
            var factory = GrovelandsCompositionTests.Factory();
            for (int seed = 0; seed < 12; seed++)
            {
                var zone = new Zone(ExampleIds[example]); var builder = new OverwritCompositionBuilder(seed);
                Assert.IsTrue(builder.BuildZone(zone, factory, new Random(seed)));
                var p = builder.Plan; Assert.NotNull(p);
                int growth = 0, fixtures = 0;
                for (int y = 0; y < Zone.Height; y++)
                    for (int x = 0; x < Zone.Width; x++)
                    {
                        var cell = zone.GetCell(x, y);
                        Assert.AreEqual(1, cell.Objects.Count(e => e.BlueprintName == "OverwritGround"));
                        foreach (var e in cell.Objects)
                        {
                            Assert.Contains(e.BlueprintName, new[] { "OverwritGround", "OverwritNewGrowth", "OverwritWaymarker", "OverwritPilgrimBench" });
                            if (e.BlueprintName == "OverwritNewGrowth")
                            {
                                growth++;
                                if (p.Role == OverwritRole.Rim) Assert.IsTrue(p.IsRim(x, y), "Growth belongs to the actual edge");
                            }
                            if (e.BlueprintName == "OverwritWaymarker" || e.BlueprintName == "OverwritPilgrimBench")
                            { fixtures++; Assert.AreEqual(OverwritFormation.PilgrimageRim, p.Formation); Assert.IsTrue(p.IsRim(x, y)); }
                        }
                        if (!p.IsApproach(x, y)) continue;
                        Assert.IsFalse(cell.BlocksMovement());
                        Assert.IsTrue(zone.GenReservedCells.Contains((x, y)));
                    }
                Assert.That(growth, Is.InRange(1, p.Role == OverwritRole.Blank ? 12 : 40));
                Assert.LessOrEqual(fixtures, 2);
                Assert.IsTrue(FormationReachability.FullyReached(zone, FormationReachability.FloodFromWest(zone)));
            }
        }

        [Test]
        public void PhysicalContentHasHonestNativeVerbsAndNoInheritedGroundNoise()
        {
            var f = GrovelandsCompositionTests.Factory(); var ground = f.CreateEntity("OverwritGround");
            Assert.NotNull(ground);
            Assert.IsTrue(string.IsNullOrEmpty(ground.GetPart<RenderPart>().GlyphVariants));
            Assert.IsFalse(ground.GetPart<PhysicsPart>().Solid);
            foreach (string bp in new[] { "OverwritGround", "OverwritNewGrowth", "OverwritWaymarker", "OverwritPilgrimBench" })
            {
                var e = f.CreateEntity(bp); Assert.NotNull(e, bp);
                Assert.IsFalse(e.HasPart<LiquidPoolPart>(), bp);
                Assert.IsFalse(e.HasPart<TileStateSourcePart>(), bp);
                Assert.IsFalse(e.HasPart<HarvestablePart>(), bp);
                Assert.IsFalse(e.HasPart("Restable"), bp);
                Assert.IsFalse(e.HasTag("Restable"), bp);
                Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable, bp);
                if (bp != "OverwritGround") Assert.NotNull(e.GetPart<DestructiblePart>(), bp);
            }
        }

        [Test]
        public void MissingContentAndOccupiedZonesRejectBeforeMutation()
        {
            var f = GrovelandsCompositionTests.Factory(); f.Blueprints.Remove("OverwritGround");
            var z = new Zone(ExampleIds[0]); z.GenReservedCells.Add((3, 3));
            var b = new OverwritCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z, f, new Random(1))); Assert.AreEqual(0, z.EntityCount);
            CollectionAssert.AreEquivalent(new[] { (3, 3) }, z.GenReservedCells); Assert.IsNull(b.Plan);
            f = GrovelandsCompositionTests.Factory(); var owner = f.CreateEntity("Dagger"); z.AddEntity(owner, 20, 10);
            Assert.IsFalse(b.BuildZone(z, f, new Random(1))); Assert.AreEqual(1, z.EntityCount);
            Assert.AreSame(owner, z.GetCell(20, 10).Objects.Single()); Assert.IsNull(b.Plan);
        }

        // Pipeline controls prove the new emptiness does not accidentally remove
        // the legacy acquisition route kept at the explicitly excluded Unsaying.
        [Test]
        public void ActualManagerUsesOrdinaryCompositionButRetainsUnsayingCirculation()
        {
            var manager = new OverworldZoneManager(GrovelandsCompositionTests.Factory(), 64);
            var ordinary = Pipeline(manager, ExampleIds[0]); var legacy = Pipeline(manager, "Overworld.2.11.0");
            Assert.IsTrue(ordinary.Builders.Any(b => b is OverwritCompositionBuilder));
            Assert.IsFalse(ordinary.Builders.Any(b => b is LandmarkBuilder || b is HazardTerrainBuilder || b is ContainerBuilder || b is PopulationBuilder || b is HaulablePropBuilder));
            Assert.IsFalse(ordinary.Builders.Any(b => b is CaveEntranceBuilder), "A cave opening would break the interior's level blank sheet.");
            Assert.IsTrue(Pipeline(manager, ExampleIds[3]).Builders.Any(b => b is CaveEntranceBuilder), "Native descent remains possible at the rim.");
            Assert.IsFalse(legacy.Builders.Any(b => b is OverwritCompositionBuilder));
            Assert.IsTrue(legacy.Builders.Any(b => b is DesertBuilder));
            Assert.IsTrue(legacy.Builders.Any(b => b is LandmarkBuilder));
            var population = legacy.Builders.OfType<PopulationBuilder>().Single();
            Assert.AreEqual("RuinsTier3", population.Table.Name);
            Assert.IsTrue(population.Table.Entries.Any(e => e.BlueprintName == "PalimpsestEcho"));
            var stamps = StampCatalog.For(BiomeType.Overwrit);
            foreach (string name in new[] { "CollapsedLibrary", "ClockworkWorkshop", "PalimpsestArchive" })
            {
                var stamp = stamps.Single(s => s.Name == name);
                Assert.Greater(stamp.Chance, 0); Assert.LessOrEqual(stamp.MinTier, 4);
            }
            Assert.IsTrue(stamps.Single(s => s.Name == "CollapsedLibrary").Legend.Values.Contains("chest:LibraryShelfT1"));
            Assert.IsTrue(stamps.Single(s => s.Name == "ClockworkWorkshop").Legend.Values.Contains("chest:WorkshopCacheT2"));
        }

        [TestCase("Overworld.1.10.0", false, 0)]
        [TestCase("Overworld.0.9.0", true, 2)]
        [TestCase("Overworld.0.13.0", true, 0)]
        [TestCase("Overworld.4.11.0", true, 3)]
        public void CachedRimFacingPointsIntoTheBlank(string id, bool rim, int quarterTurns)
        {
            Assert.AreEqual(rim, OverwritCompositionPlan.IsRimZone(id));
            Assert.AreEqual(quarterTurns, OverwritCompositionPlan.InwardQuarterTurns(id));
            Assert.IsFalse(OverwritCompositionPlan.IsRimZone("Overworld.2.11.0"));
            Assert.AreEqual(0, OverwritCompositionPlan.InwardQuarterTurns("Overworld.2.11.0"));
        }

        // A rim WORLD address alone does not protect the blank portion of its
        // local geometry. Native stairs must remain in the actual margin, away
        // from approach lanes and furniture aprons, after the whole pipeline.
        [TestCase(2048)] [TestCase(64)] [TestCase(1729)]
        public void NativeRimCaveEntrancesStayInThePhysicalMargin(int seed)
        {
            var manager = new OverworldZoneManager(GrovelandsCompositionTests.Factory(), seed);
            int stairs = 0;
            for (int wy = 9; wy <= 14; wy++) for (int wx = 0; wx <= 4; wx++)
            {
                string id = WorldMap.ToZoneID(wx, wy);
                if (!OverwritCompositionPlan.IsRimZone(id)) continue;
                var plan = OverwritCompositionPlan.Create(id, seed); var zone = manager.GetZone(id);
                foreach (var e in zone.GetAllEntities())
                {
                    if (!e.HasPart<StairsDownPart>()) continue;
                    stairs++; var c = zone.GetEntityCell(e);
                    Assert.IsTrue(plan.IsRim(c.X, c.Y), id + " has stairs in the blank interior at " + c.X + "," + c.Y);
                    Assert.IsFalse(plan.IsApproach(c.X, c.Y), id + ": stairs replace a reserved arrival lane");
                    Assert.IsFalse(zone.GenReservedCells.Contains((c.X, c.Y)), id + ": stairs occupy a fixture apron");
                    Assert.IsFalse(c.BlocksMovement());
                }
            }
            Assert.Greater(stairs, 0, "The margin filter must not disable native descent entirely.");
        }

        // Closing the accidental ruins circulation must not make everyday
        // utility magic depend on a particular one-time Unsaying stamp roll.
        [Test]
        public void SillsRealArcanistCanRestockAllSixUtilityBooksAndWorkshopSchematics()
        {
            var oldTrader = TraderPart.Factory; var oldRestock = TraderRestockSystem.Factory;
            string loot = File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data/Loot/LootTables.json"));
            try
            {
                var factory = GrovelandsCompositionTests.Factory();
                LootTableRegistry.Initialize(loot); TraderPart.Factory = factory; TraderRestockSystem.Factory = factory;
                var manager = new OverworldZoneManager(factory, 64);
                var sill = manager.GetZone("Overworld.10.10.0");
                var keeper = sill.GetAllEntities().Single(e => e.BlueprintName == "Arcanist");
                Assert.AreEqual("ArcanistStock", keeper.GetProperty(TraderRestockSystem.ShopStockTableProp));
                var inventory = keeper.GetPart<InventoryPart>(); Assert.NotNull(inventory);
                var seen = new HashSet<string>();
                foreach (var item in inventory.Objects) seen.Add(item.BlueprintName);
                foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
                keeper.SetIntProperty(TraderRestockSystem.LastRestockProp, 0);
                TraderRestockSystem.RestockZone(sill, TraderRestockSystem.RestockIntervalTurns);
                Assert.AreEqual(0, inventory.Objects.Count, "At the interval boundary the shelf must not refill early.");
                for (int visit = 1; visit <= 96; visit++)
                {
                    TraderRestockSystem.RestockZone(sill, visit * (TraderRestockSystem.RestockIntervalTurns + 1));
                    foreach (var item in inventory.Objects) seen.Add(item.BlueprintName);
                    foreach (var item in inventory.Objects.ToArray()) inventory.RemoveObject(item);
                }
                foreach (string bp in new[] { "KindleFlameGrimoire", "DryingBreezeGrimoire", "ChillDraftGrimoire", "HearthwarmGrimoire", "ConjureWaterGrimoire", "WardGleamGrimoire", "SchematicHonedEdge", "SchematicReinforcedPlating", "SchematicDuelistCut" })
                    Assert.Contains(bp, seen.ToArray(), "A reachable renewing native merchant must keep " + bp + " in circulation.");
                Assert.AreSame(keeper, sill.GetAllEntities().Single(e => e.BlueprintName == "Arcanist"));
            }
            finally
            {
                TraderPart.Factory = oldTrader; TraderRestockSystem.Factory = oldRestock;
                LootTableRegistry.ResetForTests();
            }
        }
    }
}
