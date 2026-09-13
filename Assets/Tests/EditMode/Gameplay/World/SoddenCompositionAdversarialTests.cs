using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow hypotheses for Sodden composition. These tests
    /// exercise native occupancy, water leases and the complete builder chain;
    /// visual screenshots cannot prove those contracts.</summary>
    public class SoddenCompositionAdversarialTests
    {
        private EntityFactory factory;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp] public void SetUp()
        {
            factory = GrovelandsCompositionTests.Factory();
            Diag.ResetAll();
            MessageLog.Clear();
        }
        [TearDown] public void TearDown()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        // H1: malformed or underground addresses accidentally become valid
        // surface plans when permissive coordinate parsing supplies defaults.
        [TestCase(null)] [TestCase("")] [TestCase("garbage")]
        [TestCase("Overworld.-1.4.0")] [TestCase("Overworld.20.4.0")]
        [TestCase("Overworld.16.4.1")]
        public void Adversarial_InvalidAddressesCannotProduceASurfaceBog(string id)
            => Assert.Throws<ArgumentException>(() => SoddenCompositionPlan.Create(id, 64));

        [TestCase(Formation.Grove)] [TestCase(Formation.Hedgerow)] [TestCase((Formation)999)]
        public void Adversarial_ForeignFormationCannotSilentlyProduceAnEmptyBog(Formation form)
            => Assert.Throws<ArgumentException>(() => SoddenCompositionPlan.Create(SoddenCompositionTests.Id, 64, form));

        // H2: an art/composition expansion accidentally takes over authored
        // villages or sinkhole mouths. Enumerate the entire finite map.
        [Test] public void Adversarial_EligibilityMatchesAuthoredWildernessAndRejectsOtherAddressShapes()
        {
            int eligible = 0, protectedSodden = 0;
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    bool sodden = WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Sodden;
                    bool protectedSite = WorldMapAuthoring.PlaceAt(x, y).HasValue || SinkholeSites.IsMouth(x, y);
                    string id = WorldMap.ToZoneID(x, y);
                    Assert.AreEqual(sodden && !protectedSite, SoddenCompositionPlan.IsWildernessZone(id), id);
                    if (sodden && !protectedSite) eligible++;
                    if (sodden && protectedSite) protectedSodden++;
                    Assert.IsFalse(SoddenCompositionPlan.IsWildernessZone("Overworld." + x + "." + y + ".1"));
                }
            Assert.Greater(eligible, 10);
            Assert.Greater(protectedSodden, 0, "The protected-site countercheck must exercise real Sodden sites.");
            foreach (string id in new[] { null, "", "garbage", "Overworld.20.0.0", "Overworld.-1.0.0", "Overworld.15.6.0", OverworldZoneManager.DrownedLedgerZoneID })
                Assert.IsFalse(SoddenCompositionPlan.IsWildernessZone(id), id);
        }

        [Test] public void Adversarial_ActualManagerRoutesOnlyOrdinarySoddenThroughComposition()
        {
            var manager = new OverworldZoneManager(factory, 64);
            var get = typeof(OverworldZoneManager).GetMethod("GetPipelineForZone", PrivateInstance);
            var camp = typeof(OverworldZoneManager).GetMethod("CreateMerchantCampPipeline", PrivateInstance);
            Assert.NotNull(get); Assert.NotNull(camp);
            var shared = (ZoneGenerationPipeline)camp.Invoke(manager, new object[] { BiomeType.Sodden, 1 });
            Assert.IsTrue(shared.Builders.Any(b => b is SoddenFormationBuilder));
            Assert.IsFalse(shared.Builders.Any(b => b is SoddenCompositionBuilder));
            int composed = 0;
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Sodden) continue;
                    string id = WorldMap.ToZoneID(x, y);
                    bool expected = SoddenCompositionPlan.IsWildernessZone(id) && manager.WorldMap.GetPOI(x, y) == null;
                    var pipeline = (ZoneGenerationPipeline)get.Invoke(manager, new object[] { id });
                    Assert.AreEqual(expected, pipeline.Builders.Any(b => b is SoddenCompositionBuilder), id);
                    if (!expected) continue;
                    composed++;
                    Assert.IsFalse(pipeline.Builders.Any(b => b is SoddenFormationBuilder), "Do not stamp a second bog over the composed one: " + id);
                    Assert.IsFalse(pipeline.Builders.Any(b => b is JungleBuilder), "The woodland terrain generator must not run underneath: " + id);
                }
            Assert.Greater(composed, 0);
        }

        // H3: a rejected second build erases player destruction or exposes
        // its old Plan as if a new build had succeeded.
        [Test] public void Adversarial_RebuildPreservesPlayerChangesAndClearsStalePlan()
        {
            var builder = new SoddenCompositionBuilder(64) { FormationOverride = Formation.PeatCuts };
            var zone = new Zone(SoddenCompositionTests.Id);
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
            var bank = zone.GetAllEntities().First(e => e.BlueprintName == "PeatBank");
            zone.RemoveEntity(bank);
            var before = zone.GetAllEntities().ToArray();
            Assert.IsFalse(builder.BuildZone(zone, factory, new System.Random(2)));
            Assert.IsNull(builder.Plan);
            CollectionAssert.AreEquivalent(before, zone.GetAllEntities());
            Assert.IsNull(zone.GetEntityCell(bank));
        }

        // H4: one missing required blueprint leaves a half-built swamp or
        // consumes RNG/entity IDs before failing softly.
        [TestCase("Grass", Formation.OpenMire)]
        [TestCase("MirePool", Formation.OpenMire)]
        [TestCase("PeatBank", Formation.PeatCuts)]
        [TestCase("DeadTree", Formation.DrownedCopse)]
        public void Adversarial_MissingRequiredContentRejectsBeforeAnyMutation(string missing, Formation form)
        {
            Assert.IsTrue(factory.Blueprints.Remove(missing));
            var zone = new Zone(SoddenCompositionTests.Id);
            var builder = new SoddenCompositionBuilder(64) { FormationOverride = form };
            Assert.IsFalse(builder.BuildZone(zone, factory, new System.Random(64)));
            Assert.IsNull(builder.Plan);
            Assert.AreEqual(0, zone.EntityCount);
            Assert.AreEqual(0, zone.GenReservedCells.Count);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                Assert.IsNull(zone.TileState.Get(x, y));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "SoddenCompositionRejected", Limit = 10 }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("missing-blueprint:" + missing, rejected[0].PayloadJson);
            // Same input with the shipped content restored must actually build.
            factory = GrovelandsCompositionTests.Factory();
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(64)));
            Assert.Greater(zone.EntityCount, Zone.Width * Zone.Height);
        }

        // H5: a permanent generation coating outlives a destroyed native
        // pool. Exercise both ordinary removal and structural destruction,
        // while the identical neighboring surviving pool renews its lease.
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_RemovedPoolDriesWhileSurvivingPoolKeepsItsNativeLease(bool destroy)
        {
            var zone = Build(Formation.OpenMire, 64);
            var pools = zone.GetAllEntities().Where(e => e.BlueprintName == "MirePool").Take(2).ToArray();
            Assert.AreEqual(2, pools.Length);
            var removed = zone.GetEntityCell(pools[0]); var survivor = zone.GetEntityCell(pools[1]);
            Assert.AreEqual(4, zone.TileState.CoatingTurns(removed.X, removed.Y, "water"));
            Assert.AreEqual(4, zone.TileState.CoatingTurns(survivor.X, survivor.Y, "water"));
            if (destroy) Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(pools[0], 1000, null, zone));
            else zone.RemoveEntity(pools[0]);
            for (int turn = 0; turn < 6; turn++)
            {
                ZoneTileStateSystem.SeedTerrainSources(zone);
                zone.TileState.Tick();
            }
            Assert.IsNull(zone.GetEntityCell(pools[0]));
            Assert.IsFalse(zone.TileState.HasCoating(removed.X, removed.Y, "water"));
            Assert.IsTrue(zone.TileState.HasCoating(survivor.X, survivor.Y, "water"));
            Assert.AreEqual(3, zone.TileState.CoatingTurns(survivor.X, survivor.Y, "water"));
        }

        // H6: changing the caller's RNG history changes a supposedly
        // world-seed-owned layout, or overlapping basins duplicate pools.
        [Test] public void Adversarial_CallerRngHistoryCannotChangeOrDoubleBookNativeRealization()
        {
            foreach (var form in SoddenCompositionTests.Forms)
            {
                var random = new System.Random(777); for (int i = 0; i < 500; i++) random.Next();
                var a = new Zone(SoddenCompositionTests.Id); var b = new Zone(SoddenCompositionTests.Id);
                Assert.IsTrue(new SoddenCompositionBuilder(17) { FormationOverride = form }.BuildZone(a, factory, random));
                Assert.IsTrue(new SoddenCompositionBuilder(17) { FormationOverride = form }.BuildZone(b, factory, new System.Random(1)));
                CollectionAssert.AreEqual(Snapshot(a), Snapshot(b), form.ToString());
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    Assert.LessOrEqual(a.GetCell(x, y).Objects.Count(e => e.HasPart<LiquidPoolPart>()), 1, form + " " + x + "," + y);
            }
        }

        // H7: an edge crossing passes while a small walkable pocket remains
        // imprisoned. This independent four-neighbor flood includes residents.
        [TestCase(Formation.OpenMire)] [TestCase(Formation.PeatCuts)]
        [TestCase(Formation.ReedMaze)] [TestCase(Formation.DrownedCopse)]
        [TestCase(Formation.Causeway)] [TestCase(Formation.BogFace)]
        public void Adversarial_EveryOpenCellConnectsBeforeLegacyConnectivityRepair(Formation form)
        {
            for (int seed = 0; seed < 64; seed++)
            {
                var zone = Build(form, seed);
                var seen = Flood(zone, 0, 0);
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    if (!zone.GetCell(x, y).BlocksMovement())
                        Assert.IsTrue(seen[x, y], form + " seed " + seed + " stranded " + x + "," + y);
            }
        }

        // H8: repair or route clearing erases the habitat guarantee, leaves
        // a body floating alone, or spreads protected pre-Felling witnesses.
        [Test] public void Adversarial_ResidentsRemainBesideHabitatAndWitnessesStayAuthored()
        {
            int sawBody = 0, sawNoBody = 0;
            foreach (var form in SoddenCompositionTests.Forms) for (int seed = 0; seed < 32; seed++)
            {
                var zone = Build(form, seed);
                var toads = zone.GetAllEntities().Where(e => e.BlueprintName == "MawToad").ToArray();
                var bodies = zone.GetAllEntities().Where(e => e.BlueprintName == "BogTakenBody").ToArray();
                if (form == Formation.DrownedCopse)
                {
                    Assert.That(toads.Length, Is.InRange(1, 2));
                    foreach (var toad in toads) AssertAdjacent(zone, toad, "DeadTree");
                }
                else Assert.AreEqual(0, toads.Length, form.ToString());
                if (form == Formation.PeatCuts || form == Formation.BogFace)
                {
                    Assert.That(bodies.Length, Is.InRange(0, 2));
                    if (bodies.Length == 0) sawNoBody++; else sawBody++;
                    foreach (var body in bodies)
                    {
                        AssertAdjacent(zone, body, "PeatBank");
                        Assert.IsFalse(body.GetPart<PhysicsPart>().Solid);
                        Assert.IsFalse(body.GetPart<PhysicsPart>().Takeable);
                        Assert.NotNull(body.GetPart<ExaminablePart>());
                    }
                }
                else Assert.AreEqual(0, bodies.Length, form.ToString());
                Assert.IsFalse(zone.GetAllEntities().Any(e => e.BlueprintName == "PreFellingBody"));
            }
            Assert.Greater(sawBody, 0); Assert.Greater(sawNoBody, 0, "The bog gives sparingly; absence is a real branch.");
        }

        // H9: bespoke art bypasses the existing native ownership, hidden,
        // removed, or portable-object gates. Both Takeable states are tested
        // on the same instance, including the borrowed Spread reed family.
        [TestCase("MirePool")] [TestCase("Duckboard")] [TestCase("DeadTree")]
        [TestCase("PeatBank")] [TestCase("BogTakenBody")] [TestCase("Reeds")]
        public void Adversarial_RecipesFollowNativeOwnerVisibilityMembershipAndTakeability(string blueprint)
        {
            string id = FirstEligibleAddress();
            var zone = new Zone(id); var entity = factory.CreateEntity(blueprint); zone.AddEntity(entity, 10, 10);
            var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            Assert.NotNull(library);
            var physics = entity.GetPart<PhysicsPart>(); Assert.NotNull(physics);
            var render = entity.GetPart<RenderPart>(); Assert.NotNull(render);
            int count = zone.EntityCount;
            foreach (bool takeable in new[] { false, true })
            {
                physics.Takeable = takeable; render.Visible = true;
                var recipe = SpawnRing3DRecipes.Resolve(zone, entity, library.Definition);
                Assert.IsNotEmpty(recipe.ModelId, recipe.Failure);
                Assert.AreSame(entity, recipe.Owner);
                Assert.AreEqual(takeable, recipe.Transient);
                Assert.AreEqual(!takeable, recipe.Batched);
                Assert.NotNull(library.Definition.FindModel(recipe.ModelId), "Catalog metadata must resolve the actual art.");
                render.Visible = false;
                Assert.IsNull(SpawnRing3DRecipes.Resolve(zone, entity, library.Definition).ModelId);
                render.Visible = true;
            }
            Assert.AreEqual(count, zone.EntityCount, "Recipe queries must be read-only.");
            zone.RemoveEntity(entity);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone, entity, library.Definition).ModelId);
            var other = new Zone(id); other.AddEntity(entity, 10, 10);
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone, entity, library.Definition).ModelId, "Same coordinates in another Zone are not membership.");
            Assert.IsNotEmpty(SpawnRing3DRecipes.Resolve(other, entity, library.Definition).ModelId);
        }

        // H10: a reserved dry route looks safe in the plan but a later
        // structure treats it as unclaimed floor and seals the crossing.
        [Test] public void Adversarial_RealLandmarkGuardRejectsDryApproachAndAcceptsUnreservedControl()
        {
            var zone = new Zone(SoddenCompositionTests.Id);
            var builder = new SoddenCompositionBuilder(64) { FormationOverride = Formation.OpenMire };
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
            var method = typeof(LandmarkBuilder).GetMethod("FootprintClear", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); var stamp = new StructureStamp { Rows = new[] { "#" } };
            int x = builder.Plan.FocalX, y = builder.Plan.FocalY;
            Assert.IsTrue(builder.Plan.IsApproach(x, y)); Assert.IsFalse(builder.Plan.IsWet(x, y));
            Assert.IsFalse(zone.GetCell(x, y).BlocksMovement());
            Assert.IsFalse((bool)method.Invoke(null, new object[] { zone, stamp, x, y }));
            Assert.IsTrue(zone.GenReservedCells.Remove((x, y)));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { zone, stamp, x, y }), "The reservation itself must protect this otherwise buildable cell.");
        }

        // H11: late hazards, containers, residents or landmarks block one
        // of the four semantic entries despite a passing terrain-only test.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void Adversarial_CompleteNativePipelineConnectsAllFourApproachesForEveryFormation(int seed)
        {
            var manager = new OverworldZoneManager(factory, seed);
            var seenForms = new HashSet<Formation>();
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (!SoddenCompositionPlan.IsWildernessZone(id) || manager.WorldMap.GetPOI(x, y) != null) continue;
                var form = FormationSelector.For(BiomeType.Sodden, id);
                if (seenForms.Contains(form)) continue;
                var plan = SoddenCompositionPlan.Create(id, seed);
                var zone = manager.GetZone(id); Assert.NotNull(zone);
                var reached = Flood(zone, 0, plan.WestY);
                foreach (var entry in new[] { (0, plan.WestY), (Zone.Width - 1, plan.EastY), (plan.NorthX, 0), (plan.SouthX, Zone.Height - 1) })
                {
                    Assert.IsFalse(zone.GetCell(entry.Item1, entry.Item2).BlocksMovement(), id + " seed " + seed + " obstructed entry " + entry);
                    Assert.IsTrue(reached[entry.Item1, entry.Item2], id + " seed " + seed + " disconnected entry " + entry);
                }
                Assert.IsTrue(reached[plan.FocalX, plan.FocalY], id + " central dry island disconnected");
                for (int cy = 0; cy < Zone.Height; cy++) for (int cx = 0; cx < Zone.Width; cx++)
                    if (plan.IsApproach(cx, cy))
                    {
                        var cell = zone.GetCell(cx, cy);
                        Assert.IsFalse(cell.Objects.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GreatdewSnarePart>()), id + " hazardous approach " + cx + "," + cy);
                    }
                seenForms.Add(form);
            }
            CollectionAssert.AreEquivalent(SoddenCompositionTests.Forms, seenForms, "Every formation needs a real eligible runtime address.");
        }

        // H12: a failed rebuild reuses the success diagnostic and falsely
        // suggests a player's modified zone was regenerated.
        [Test] public void Adversarial_SuccessAndRejectedRebuildEmitDistinctDiagnostics()
        {
            var zone = new Zone(SoddenCompositionTests.Id); var builder = new SoddenCompositionBuilder(64);
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
            Assert.IsFalse(builder.BuildZone(zone, factory, new System.Random(1)));
            var success = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "SoddenCompositionPlanned", Limit = 10 }).Records;
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "SoddenCompositionRejected", Limit = 10 }).Records;
            Assert.AreEqual(1, success.Count); Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("nonempty-zone", rejected[0].PayloadJson);
            StringAssert.Contains("64", success[0].PayloadJson);
        }

        private Zone Build(Formation form, int seed)
        {
            var zone = new Zone(SoddenCompositionTests.Id);
            Assert.IsTrue(new SoddenCompositionBuilder(seed) { FormationOverride = form }.BuildZone(zone, factory, new System.Random(seed)));
            return zone;
        }
        private static string FirstEligibleAddress()
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (SoddenCompositionPlan.IsWildernessZone(id)) return id;
            }
            Assert.Fail("No eligible Sodden address."); return null;
        }
        private static void AssertAdjacent(Zone zone, Entity entity, string blueprint)
        {
            var cell = zone.GetEntityCell(entity); Assert.NotNull(cell);
            Assert.IsTrue(new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.Any(d =>
                zone.GetCell(cell.X + d.Item1, cell.Y + d.Item2)?.Objects.Any(e => e.BlueprintName == blueprint) == true),
                entity.BlueprintName + " no longer borders " + blueprint);
        }
        private static bool[,] Flood(Zone zone, int sx, int sy)
        {
            var reached = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            if (zone.GetCell(sx, sy).BlocksMovement()) return reached;
            reached[sx, sy] = true; queue.Enqueue((sx, sy));
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int x = c.x + d.Item1, y = c.y + d.Item2;
                    var cell = zone.GetCell(x, y);
                    if (cell == null || reached[x, y] || cell.BlocksMovement()) continue;
                    reached[x, y] = true; queue.Enqueue((x, y));
                }
            }
            return reached;
        }
        private static string[] Snapshot(Zone zone) => zone.GetAllEntities()
            .Select(e => e.BlueprintName + ":" + zone.GetEntityPosition(e))
            .OrderBy(s => s, StringComparer.Ordinal).ToArray();
    }
}
