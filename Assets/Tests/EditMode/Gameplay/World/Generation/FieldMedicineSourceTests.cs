using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>One carried medicine user replaces an existing member of a real
    /// gleaner encounter. No additional roll, surface crowd or medicine grant.</summary>
    public sealed class FieldMedicineSourceTests
    {
        const string Patchbearer = "MarlbackPatchbearer";
        const string Gleaner = "MarlbackGleaner";
        static PopulationTable Table(int depth, bool enabled)
        {
            var method = typeof(PopulationTable).GetMethod("UndergroundTier", new[] { typeof(int), typeof(bool) });
            Assert.NotNull(method, "The ordinary cave caller must explicitly opt into field medicine.");
            return (PopulationTable)method.Invoke(null, new object[] { depth, enabled });
        }
        static PopulationTable NativeTable(OverworldZoneManager manager, string id) =>
            CinderholdCompositionTests.Pipeline(manager, id).Builders.OfType<PopulationBuilder>().Single().Table;
        static bool CanRollMedicine(PopulationTable table) => Enumerable.Range(0, 256)
            .Any(seed => table.Roll(new Random(seed)).Contains(Patchbearer));
        static IEnumerable<string> Columns(OverworldZoneManager manager)
        {
            var origin = WorldMap.FromZoneID(ReferenceGladePlan.ZoneID);
            return from y in Enumerable.Range(0, WorldMap.Height)
                   from x in Enumerable.Range(0, WorldMap.Width)
                   let plan = UndergroundLayoutPlan.Select(manager.WorldSeed, WorldMap.ToZoneID(x, y, 3),
                       manager.WorldMap.GetBiome(x, y), manager.WorldMap.GetPOI(x, y), manager.Factory)
                   where plan.IsOrdinaryColumn && plan.SurfaceBiome == BiomeType.Spread
                   orderby Math.Abs(x - origin.x) + Math.Abs(y - origin.y), y, x
                   select WorldMap.ToZoneID(x, y, 0);
        }

        [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void TierTwoVariantReplacesExactlyOneRolledGleanerWithoutChangingCountsOrRandomStream(int depth)
        {
            int appearances = 0, multipleGleaners = 0, otherEncounters = 0;
            for (int seed = 0; seed < 512; seed++)
            {
                var beforeRng = new Random(seed); var afterRng = new Random(seed);
                var before = Table(depth, false).Roll(beforeRng);
                var after = Table(depth, true).Roll(afterRng);
                CollectionAssert.AreEqual(before, after.Select(id => id == Patchbearer ? Gleaner : id), "seed " + seed);
                Assert.AreEqual(beforeRng.Next(), afterRng.Next(), "Medicine substitution must not shift placements or later rolls.");
                Assert.AreEqual(before.Contains(Gleaner) ? 1 : 0, after.Count(id => id == Patchbearer));
                if (before.Contains(Gleaner)) appearances++; else otherEncounters++;
                if (before.Count(id => id == Gleaner) > 1)
                { multipleGleaners++; Assert.IsTrue(after.Contains(Gleaner), "Companions remain ordinary gleaners."); }
            }
            Assert.Greater(appearances, 15); Assert.Less(appearances, 90, "The existing 1/12 branch stays uncommon.");
            Assert.Greater(multipleGleaners, 0); Assert.Greater(otherEncounters, 0);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(6)] [TestCase(9)]
        public void OptInCannotAddMedicineOutsideTheAuthoredDepthBand(int depth)
        {
            for (int seed = 0; seed < 128; seed++)
            {
                var a = new Random(seed); var b = new Random(seed);
                CollectionAssert.AreEqual(Table(depth, false).Roll(a), Table(depth, true).Roll(b));
                Assert.AreEqual(a.Next(), b.Next());
            }
        }

        [Test] public void DefaultAndNameOnlyTablesNeverGainFieldMedicine()
        {
            foreach (int depth in new[] { 1, 3, 4, 5, 6, 9 })
                Assert.IsFalse(CanRollMedicine(PopulationTable.UndergroundTier(depth)));
            Assert.IsFalse(CanRollMedicine(new PopulationTable { Name = "Underground_Depth3", Entries = new List<PopulationEntry>
            { new PopulationEntry { BlueprintName = Gleaner, MinCount = 2, MaxCount = 2 } } }));
            foreach (BiomeType biome in Enum.GetValues(typeof(BiomeType)))
                for (int tier = 1; tier <= 3; tier++) Assert.IsFalse(CanRollMedicine(PopulationTable.GetBiomeTable(biome, tier)));
        }

        [Test] public void AnAmbientGleanerCannotStandInForTheSelectedDepthEncounter()
        {
            var table = Table(3, true);
            table.Entries.RemoveAll(entry => entry.BlueprintName == Gleaner);
            table.Entries.Add(new PopulationEntry { BlueprintName = Gleaner, MinCount = 1, MaxCount = 1 });
            for (int seed = 0; seed < 256; seed++)
            {
                var rolled = table.Roll(new Random(seed));
                Assert.Contains(Gleaner, rolled);
                Assert.IsFalse(rolled.Contains(Patchbearer));
            }
        }

        [Test] public void ManagedOrdinarySpreadDepthsOptInWhileSurfaceShallowDeepAndOtherBiomesDoNot()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var at = WorldMap.FromZoneID(Columns(manager).First());
                foreach (int depth in new[] { 1, 2, 3, 4, 5, 6 })
                    Assert.AreEqual(depth >= 3 && depth <= 5, CanRollMedicine(NativeTable(manager, WorldMap.ToZoneID(at.x, at.y, depth))), "depth " + depth);
                Assert.IsFalse(CanRollMedicine(NativeTable(manager, WorldMap.ToZoneID(at.x, at.y, 0))));
                manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Sodden;
                Assert.IsFalse(CanRollMedicine(NativeTable(manager, WorldMap.ToZoneID(at.x, at.y, 3))));
            }
        }

        [Test] public void MissingOptionalBlueprintPreservesTheOrdinaryGleanerEncounter()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var at = WorldMap.FromZoneID(Columns(manager).First());
                Assert.IsTrue(CanRollMedicine(NativeTable(manager, WorldMap.ToZoneID(at.x, at.y, 3))), "Loaded content is the positive control.");
                Assert.IsTrue(scope.Factory.Blueprints.Remove(Patchbearer));
                var actual = NativeTable(manager, WorldMap.ToZoneID(at.x, at.y, 3));
                for (int seed = 0; seed < 256; seed++)
                {
                    var before = new Random(seed); var after = new Random(seed);
                    CollectionAssert.AreEqual(PopulationTable.UndergroundTier(3).Roll(before), actual.Roll(after));
                    Assert.AreEqual(before.Next(), after.Next());
                }
            }
        }

        [TestCase(POIType.Village)] [TestCase(POIType.Lair)] [TestCase(POIType.MerchantCamp)]
        public void ARealSurfacePointOfInterestRefusesTheVariantInItsColumn(POIType type)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var at = WorldMap.FromZoneID(Columns(manager).First());
                manager.WorldMap.SetPOI(at.x, at.y, new PointOfInterest(type, "source control", tier: 2));
                var builders = CinderholdCompositionTests.Pipeline(manager, WorldMap.ToZoneID(at.x, at.y, 3)).Builders.OfType<PopulationBuilder>();
                foreach (var builder in builders) Assert.IsFalse(CanRollMedicine(builder.Table));
            }
        }

        [Test] public void AuthoredSpreadColumnsRemainExcluded()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                foreach (string surface in OverworldZoneManager.AuthoredWildernessZoneIDs.Concat(new[] { MultiCellPilotRuntime.ZoneID }))
                {
                    var at = WorldMap.FromZoneID(surface);
                    foreach (var source in CinderholdCompositionTests.Pipeline(manager, WorldMap.ToZoneID(at.x, at.y, 3)).Builders.OfType<PopulationBuilder>())
                        Assert.IsFalse(CanRollMedicine(source.Table), surface);
                }
            }
        }

        [Test] public void ActualPatchbearerOwnsExactlyOneHealingTonicAndOrdinaryGleanersRemainUnchanged()
        {
            using (var scope = new HaulingContentScope())
            {
                Assert.IsTrue(scope.Factory.Blueprints.ContainsKey(Patchbearer));
                foreach (int seed in new[] { 1, 64, 1729 })
                {
                    scope.Seed(seed); var actor = scope.Factory.CreateEntity(Patchbearer);
                    Assert.AreEqual(20, actor.GetStat("Hitpoints").Max);
                    var tonic = actor.GetPart<InventoryPart>().Objects.Single(item => item.BlueprintName == "HealingTonic");
                    Assert.AreEqual(1, tonic.GetPart<StackerPart>()?.StackCount ?? 1);
                    Assert.AreSame(actor, tonic.GetPart<PhysicsPart>().InInventory); Assert.IsNull(tonic.GetPart<PhysicsPart>().Equipped);
                    var ordinary = scope.Factory.CreateEntity(Gleaner);
                    Assert.IsFalse(ordinary.GetPart<InventoryPart>().Objects.Any(item => item.BlueprintName == "HealingTonic"));
                }
            }
        }

        // Fixed seeds and complete nearest32 pools. A rare weighted encounter
        // is not guaranteed in each pool; retain absence rather than rerolling.
        // These are generated-route observations, not ordinary keyboard play.
        [Test] public void ThreePredeterminedWorldsHaveRealCaveRoutesToFiniteMedicineInTheAggregate()
        {
            int witnesses = 0;
            foreach (int seed in new[] { 64, 1, 1729 }) witnesses += Census(seed, 32);
            Assert.Greater(witnesses, 0, "The complete predeclared census must exercise a real native source.");
        }

        [Test, Explicit("One-time full seed64 source audit; generation is intentionally more expensive than the bounded regression census.")]
        public void Seed64WholeOrdinarySpreadCensus()
        {
            Census(64, int.MaxValue);
        }

        static int Census(int seed, int limit)
        {
            Assert.NotNull(Table(3, true));
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(seed); Assert.IsTrue(scope.Factory.Blueprints.ContainsKey(Patchbearer));
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
                var candidates = Columns(manager).Take(limit).ToArray();
                TestContext.WriteLine("FieldMedicine candidates seed=" + seed + ": " + string.Join(",", candidates));
                int witnesses = 0, entrances = 0;
                var reached = new int[6]; var breaks = new List<string>();
                foreach (string surface in candidates)
                {
                    var current = manager.GetZone(surface); Assert.NotNull(current);
                    var route = new List<string> { surface };
                    for (int depth = 1; depth <= 5; depth++)
                    {
                        string below = WorldMap.GetZoneBelow(current.ZoneID);
                        var edge = manager.GetConnections(current.ZoneID).FirstOrDefault(e => e.SourceZoneID == current.ZoneID && e.TargetZoneID == below && e.Type == "StairsDown");
                        if (edge == null)
                        {
                            breaks.Add(current.ZoneID + " no-down-connection");
                            break;
                        }
                        if (!current.GetCell(edge.SourceX, edge.SourceY).Objects.Any(e => e.HasPart<StairsDownPart>()))
                        {
                            breaks.Add(current.ZoneID + " missing-physical-down-stair=" + edge.SourceX + "," + edge.SourceY);
                            break;
                        }
                        if (depth == 1) entrances++;
                        route.Add("down=" + edge.SourceX + "," + edge.SourceY);
                        current = manager.GetZone(below); Assert.NotNull(current); reached[depth]++;
                        Assert.IsTrue(current.GetCell(edge.TargetX, edge.TargetY).Objects.Any(e => e.HasPart<StairsUpPart>()));
                        route.Add(current.ZoneID + " up=" + edge.TargetX + "," + edge.TargetY);
                        var actors = current.GetAllEntities().Where(e => e.BlueprintName == Patchbearer).ToArray();
                        Assert.LessOrEqual(actors.Length, 1);
                        if (actors.Length == 0) continue;
                        Assert.That(depth, Is.InRange(3, 5)); var actor = actors[0];
                        var medicine = actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "HealingTonic");
                        Assert.AreSame(actor, medicine.GetPart<PhysicsPart>().InInventory);
                        TestContext.WriteLine("FieldMedicine witness seed=" + seed
                            + " route=" + string.Join(" -> ", route) + " actor=" + current.GetEntityPosition(actor));
                        witnesses++;
                    }
                }
                TestContext.WriteLine("FieldMedicine summary seed=" + seed + " candidates=" + candidates.Length
                    + " entrances=" + entrances + " reached-depth1..5=" + string.Join(",", reached.Skip(1))
                    + " patchbearer-floors=" + witnesses + " breaks=" + string.Join(";", breaks));
                Assert.Greater(entrances, 0, "The declared pool must actually exercise stair-following generation.");
                Assert.Greater(reached[3], 0, "The declared pool must actually reach the eligible depth band.");
                return witnesses;
            }
        }
    }
}
