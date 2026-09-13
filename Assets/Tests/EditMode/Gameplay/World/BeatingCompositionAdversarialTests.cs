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
    /// <summary>Player-flow hypotheses for Beating composition. These tests
    /// exercise native occupancy, water leases and the complete builder chain;
    /// visual screenshots cannot prove those contracts.</summary>
    public class BeatingCompositionAdversarialTests
    {
        private EntityFactory factory;
        private EntityFactory oldHarvestFactory, oldDestructionFactory;
        private static readonly (int x,int y)[] Directions = { (1,0),(-1,0),(0,1),(0,-1) };
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp] public void SetUp()
        {
            factory = GrovelandsCompositionTests.Factory();
            oldHarvestFactory = HarvestablePart.Factory; oldDestructionFactory = DestructionSystem.EntityFactoryRef;
            HarvestablePart.Factory = factory; DestructionSystem.EntityFactoryRef = factory;
            BeatingGlareSystem.ResetForTests();
            Diag.ResetAll();
            MessageLog.Clear();
        }
        [TearDown] public void TearDown()
        {
            HarvestablePart.Factory = oldHarvestFactory; DestructionSystem.EntityFactoryRef = oldDestructionFactory;
            BeatingGlareSystem.ResetForTests();
            Diag.ResetAll();
            MessageLog.Clear();
        }

        // H1: malformed or underground addresses accidentally become valid
        // surface plans when permissive coordinate parsing supplies defaults.
        [TestCase(null)] [TestCase("")] [TestCase("garbage")]
        [TestCase("Overworld.-1.4.0")] [TestCase("Overworld.20.4.0")]
        [TestCase("Overworld.16.16.1")]
        public void Adversarial_InvalidAddressesCannotProduceASurfaceWasteland(string id)
            => Assert.Throws<ArgumentException>(() => BeatingCompositionPlan.Create(id, 64));

        [TestCase(Formation.Grove)] [TestCase(Formation.OpenMire)] [TestCase((Formation)999)]
        public void Adversarial_ForeignFormationCannotSilentlyProduceAnEmptyWasteland(Formation form)
            => Assert.Throws<ArgumentException>(() => BeatingCompositionPlan.Create(BeatingCompositionTests.Id, 64, form));

        // H2: an art/composition expansion accidentally takes over authored
        // villages or sinkhole mouths. Enumerate the entire finite map.
        [Test] public void Adversarial_EligibilityMatchesAuthoredWildernessAndRejectsOtherAddressShapes()
        {
            int eligible = 0, protectedBeating = 0;
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    bool beating = WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Beating;
                    string specialId = WorldMap.ToZoneID(x, y);
                    bool protectedSite = WorldMapAuthoring.PlaceAt(x, y).HasValue || SinkholeSites.IsMouth(x, y)
                        || specialId == OverworldZoneManager.TenthFireZoneID || specialId == OverworldZoneManager.AbandonedCounterZoneA
                        || specialId == OverworldZoneManager.AbandonedCounterZoneB;
                    string id = WorldMap.ToZoneID(x, y);
                    Assert.AreEqual(beating && !protectedSite, BeatingCompositionPlan.IsWildernessZone(id), id);
                    if (beating && !protectedSite) eligible++;
                    if (beating && protectedSite) protectedBeating++;
                    Assert.IsFalse(BeatingCompositionPlan.IsWildernessZone("Overworld." + x + "." + y + ".1"));
                }
            Assert.Greater(eligible, 10);
            Assert.Greater(protectedBeating, 0, "The protected-site countercheck must exercise real Beating sites.");
            foreach (string id in new[] { null, "", "garbage", "Overworld.20.0.0", "Overworld.-1.0.0", "Overworld.8.16.0", "Overworld.5.17.0", "Overworld.15.15.0", "Overworld.18.18.0",
                OverworldZoneManager.TenthFireZoneID, OverworldZoneManager.AbandonedCounterZoneA, OverworldZoneManager.AbandonedCounterZoneB })
                Assert.IsFalse(BeatingCompositionPlan.IsWildernessZone(id), id);
        }

        [Test] public void Adversarial_ActualManagerRoutesOnlyOrdinaryBeatingThroughComposition()
        {
            var manager = new OverworldZoneManager(factory, 64);
            var get = typeof(OverworldZoneManager).GetMethod("GetPipelineForZone", PrivateInstance);
            var camp = typeof(OverworldZoneManager).GetMethod("CreateMerchantCampPipeline", PrivateInstance);
            Assert.NotNull(get); Assert.NotNull(camp);
            var shared = (ZoneGenerationPipeline)camp.Invoke(manager, new object[] { BiomeType.Beating, 1 });
            Assert.IsTrue(shared.Builders.Any(b => b is BeatingFormationBuilder));
            Assert.IsFalse(shared.Builders.Any(b => b is BeatingCompositionBuilder));
            int composed = 0;
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Beating) continue;
                    string id = WorldMap.ToZoneID(x, y);
                    bool expected = BeatingCompositionPlan.IsWildernessZone(id) && manager.WorldMap.GetPOI(x, y) == null;
                    var pipeline = (ZoneGenerationPipeline)get.Invoke(manager, new object[] { id });
                    Assert.AreEqual(expected, pipeline.Builders.Any(b => b is BeatingCompositionBuilder), id);
                    if (!expected) continue;
                    composed++;
                    Assert.IsFalse(pipeline.Builders.Any(b => b is BeatingFormationBuilder), "Do not stamp a second wasteland over the composed one: " + id);
                    Assert.IsFalse(pipeline.Builders.Any(b => b is DesertBuilder), "The legacy desert terrain generator must not run underneath: " + id);
                }
            Assert.Greater(composed, 0);
        }

        // H3: a rejected second build erases player destruction or exposes
        // its old Plan as if a new build had succeeded.
        [Test] public void Adversarial_RebuildPreservesPlayerChangesAndClearsStalePlan()
        {
            var builder = new BeatingCompositionBuilder(64) { FormationOverride = Formation.RuinField };
            var zone = new Zone(BeatingCompositionTests.Id);
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
            var bank = zone.GetAllEntities().First(e => e.BlueprintName == "SandstoneWall");
            zone.RemoveEntity(bank);
            var before = zone.GetAllEntities().ToArray();
            Assert.IsFalse(builder.BuildZone(zone, factory, new System.Random(2)));
            Assert.IsNull(builder.Plan);
            CollectionAssert.AreEquivalent(before, zone.GetAllEntities());
            Assert.IsNull(zone.GetEntityCell(bank));
        }

        // H4: one missing required blueprint leaves a half-built desert or
        // consumes RNG/entity IDs before failing softly.
        [TestCase("Sand", Formation.BrineLens)]
        [TestCase("BrinePool", Formation.BrineLens)]
        [TestCase("SandstoneFloor", Formation.RuinField)]
        [TestCase("PaleSaltVein", Formation.SaltPan)]
        public void Adversarial_MissingRequiredContentRejectsBeforeAnyMutation(string missing, Formation form)
        {
            Assert.IsTrue(factory.Blueprints.Remove(missing));
            var zone = new Zone(BeatingCompositionTests.Id);
            var builder = new BeatingCompositionBuilder(64) { FormationOverride = form };
            Assert.IsFalse(builder.BuildZone(zone, factory, new System.Random(64)));
            Assert.IsNull(builder.Plan);
            Assert.AreEqual(0, zone.EntityCount);
            Assert.AreEqual(0, zone.GenReservedCells.Count);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                Assert.IsNull(zone.TileState.Get(x, y));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "BeatingCompositionRejected", Limit = 10 }).Records;
            Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("missing-blueprint:" + missing, rejected[0].PayloadJson);
            // Same input with the shipped content restored must actually build.
            factory = GrovelandsCompositionTests.Factory();
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(64)));
            Assert.Greater(zone.EntityCount, Zone.Width * Zone.Height);
        }

        // H5: a permanent generation coating outlives a removed native
        // pool. BrinePool has no Destructible part; exercise actual removal,
        // while the identical neighboring surviving pool renews its lease.
        [Test] public void Adversarial_RemovedPoolDriesWhileSurvivingPoolKeepsItsNativeLease()
        {
            var zone = Build(Formation.BrineLens, 64);
            var pools = zone.GetAllEntities().Where(e => e.BlueprintName == "BrinePool").Take(2).ToArray();
            Assert.AreEqual(2, pools.Length);
            var removed = zone.GetEntityCell(pools[0]); var survivor = zone.GetEntityCell(pools[1]);
            Assert.AreEqual(4, zone.TileState.CoatingTurns(removed.X, removed.Y, "water"));
            Assert.AreEqual(4, zone.TileState.CoatingTurns(survivor.X, survivor.Y, "water"));
            zone.RemoveEntity(pools[0]);
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
            foreach (var form in BeatingCompositionTests.Forms)
            {
                var random = new System.Random(777); for (int i = 0; i < 500; i++) random.Next();
                var a = new Zone(BeatingCompositionTests.Id); var b = new Zone(BeatingCompositionTests.Id);
                Assert.IsTrue(new BeatingCompositionBuilder(17) { FormationOverride = form }.BuildZone(a, factory, random));
                Assert.IsTrue(new BeatingCompositionBuilder(17) { FormationOverride = form }.BuildZone(b, factory, new System.Random(1)));
                CollectionAssert.AreEqual(Snapshot(a), Snapshot(b), form.ToString());
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    Assert.LessOrEqual(a.GetCell(x, y).Objects.Count(e => e.HasPart<LiquidPoolPart>()), 1, form + " " + x + "," + y);
            }
        }

        // H7: an edge crossing passes while a small walkable pocket remains
        // imprisoned. This independent four-neighbor flood includes residents.
        [TestCase(Formation.BrineLens)] [TestCase(Formation.RuinField)]
        [TestCase(Formation.SaltPan)] [TestCase(Formation.DuneBelt)]
        [TestCase(Formation.CaravanRoad)] [TestCase(Formation.WindBarrens)]
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

        // H8: salt survives aesthetically but loses its actual economy or
        // becomes inaccessible behind the newly composed crust/obstacles.
        [Test] public void Adversarial_ScarceSaltIsHarvestableAccessibleAndPanOnly()
        {
            var counts = new HashSet<int>();
            foreach (var form in BeatingCompositionTests.Forms) for (int seed = 0; seed < 32; seed++)
            {
                var zone = Build(form, seed); var reached = Flood(zone, 0, 0);
                var veins = zone.GetAllEntities().Where(e => e.BlueprintName == "PaleSaltVein").ToArray();
                if (form != Formation.SaltPan) { Assert.AreEqual(0, veins.Length, form.ToString()); continue; }
                Assert.That(veins.Length, Is.InRange(2, 4)); counts.Add(veins.Length);
                foreach (var vein in veins)
                {
                    var harvest = vein.GetPart<HarvestablePart>(); Assert.NotNull(harvest);
                    Assert.AreEqual("PaleSalt", harvest.YieldBlueprint);
                    Assert.AreEqual(1, harvest.YieldMin); Assert.AreEqual(2, harvest.YieldMax);
                    var cell = zone.GetEntityCell(vein);
                    Assert.IsTrue(Directions.Any(d => reached[cell.X+d.x,cell.Y+d.y]), "Salt is physically inaccessible.");
                }
            }
            Assert.Greater(counts.Count, 1, "Scarcity variation must exercise a real branch.");
        }

        // H9: harvesting a generated salt model cannot merely hide its art;
        // it must consume its native owner and deliver the real trade good,
        // including the full-inventory overflow path.
        [TestCase(-1)] [TestCase(0)]
        public void Adversarial_HarvestedSaltBecomesRealGoodsAndLeavesItsNeighbor(int maxWeight)
        {
            var zone = Build(Formation.SaltPan, 64);
            var veins = zone.GetAllEntities().Where(e => e.BlueprintName == "PaleSaltVein").Take(2).ToArray();
            Assert.AreEqual(2, veins.Length); var origin = zone.GetEntityCell(veins[0]);
            var actor = Traveller(zone, 0, 0); actor.AddPart(new InventoryPart { MaxWeight = maxWeight });
            var inventory = actor.GetPart<InventoryPart>();
            FireAction(veins[0], actor, zone, "NotHarvest");
            Assert.NotNull(zone.GetEntityCell(veins[0])); Assert.AreEqual(0, inventory.Objects.Count);
            FireAction(veins[0], actor, zone, "Harvest");
            Assert.IsNull(zone.GetEntityCell(veins[0])); Assert.NotNull(zone.GetEntityCell(veins[1]));
            var packed = inventory.Objects.Where(e => e.BlueprintName == "PaleSalt").ToArray();
            var dropped = zone.GetCell(origin.X, origin.Y).Objects.Where(e => e.BlueprintName == "PaleSalt").ToArray();
            Assert.That(packed.Length + dropped.Length, Is.InRange(1, 2));
            if (maxWeight == 0) Assert.AreEqual(0, packed.Length); else Assert.AreEqual(0, dropped.Length);
            Assert.IsTrue(zone.GetCell(origin.X, origin.Y).IsPassable(), "Harvesting opens the source cell.");
        }

        // H10: ruin destruction leaves an invisible blocker, stale wall art,
        // or unmapped rubble after the original wall's native owner is gone.
        [Test] public void Adversarial_DestroyedRuinWallLeavesOwnedRubbleAndAnOpenCell()
        {
            var zone = Build(Formation.RuinField, 64);
            var walls = zone.GetAllEntities().Where(e => e.BlueprintName == "SandstoneWall").Take(2).ToArray();
            Assert.AreEqual(2, walls.Length); var cell = zone.GetEntityCell(walls[0]);
            var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath); Assert.NotNull(library);
            Assert.IsNotEmpty(SpawnRing3DRecipes.Resolve(zone,walls[0],library.Definition).ModelId);
            Assert.IsTrue(cell.BlocksMovement());
            Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(walls[0],1000,null,zone));
            Assert.IsNull(zone.GetEntityCell(walls[0])); Assert.NotNull(zone.GetEntityCell(walls[1]));
            Assert.IsNull(SpawnRing3DRecipes.Resolve(zone,walls[0],library.Definition).ModelId);
            Assert.IsFalse(cell.BlocksMovement());
            var rubble = cell.Objects.Single(e => e.BlueprintName == "Rubble");
            var recipe = SpawnRing3DRecipes.Resolve(zone,rubble,library.Definition);
            Assert.IsNotEmpty(recipe.ModelId,recipe.Failure); Assert.AreSame(rubble,recipe.Owner);
            Assert.AreSame(walls[1],SpawnRing3DRecipes.Resolve(zone,walls[1],library.Definition).Owner);
        }

        // H11: roofless room geometry accidentally grants shelter. Numeric
        // control flips only the native IsInterior flag, then dawn independently
        // proves that exposure still belongs to the real Height-band mechanic.
        [Test] public void Adversarial_RooflessRuinParsesAsExposureAndOnlyNativeShadeStopsGlare()
        {
            var zone = Build(Formation.RuinField,64);
            var plan = BeatingCompositionPlan.Create(zone.ZoneID,64,Formation.RuinField);
            var room = plan.GetRuin(0); int x=room.X+room.Width/2,y=room.Y+room.Height/2;
            var cell = zone.GetCell(x,y); Assert.IsFalse(cell.IsInterior); Assert.IsFalse(cell.BlocksMovement());
            var traveller = Traveller(zone,x,y);
            for(int i=0;i<10;i++)BeatingGlareSystem.OnPlayerTurnEnd(traveller,zone,350);
            Assert.AreEqual(1,traveller.GetStat("Strength").Penalty);
            Assert.IsTrue(traveller.HasEffect<ParchedEffect>());
            traveller.GetPart<StatusEffectsPart>().RemoveEffect<ParchedEffect>(); BeatingGlareSystem.ResetForTests();
            cell.IsInterior=true;
            for(int i=0;i<30;i++)BeatingGlareSystem.OnPlayerTurnEnd(traveller,zone,350);
            Assert.IsFalse(traveller.HasEffect<ParchedEffect>()); Assert.AreEqual(0,traveller.GetStat("Strength").Penalty);
            cell.IsInterior=false;
            for(int i=0;i<30;i++)BeatingGlareSystem.OnPlayerTurnEnd(traveller,zone,100);
            Assert.IsFalse(traveller.HasEffect<ParchedEffect>());
        }

        // H12: bespoke art bypasses the existing native ownership, hidden,
        // removed, or portable-object gates. Both Takeable states are tested
        // on the same instance, including native post-destruction rubble.
        [TestCase("Sand")] [TestCase("DuneCrest")] [TestCase("PaleSaltVein")]
        [TestCase("BrinePool")] [TestCase("Bones")] [TestCase("Rubble")] [TestCase("Saltbriar")]
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

        // H13: a reserved dry route looks safe in the plan but a later
        // structure treats it as unclaimed floor and seals the crossing.
        [Test] public void Adversarial_RealLandmarkGuardRejectsDryApproachAndAcceptsUnreservedControl()
        {
            var zone = new Zone(BeatingCompositionTests.Id);
            var builder = new BeatingCompositionBuilder(64) { FormationOverride = Formation.BrineLens };
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
            var method = typeof(LandmarkBuilder).GetMethod("FootprintClear", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method); var stamp = new StructureStamp { Rows = new[] { "#" } };
            int x = builder.Plan.FocalX, y = builder.Plan.FocalY;
            Assert.IsTrue(builder.Plan.IsApproach(x, y)); Assert.IsFalse(builder.Plan.IsWater(x, y));
            Assert.IsFalse(zone.GetCell(x, y).BlocksMovement());
            Assert.IsFalse((bool)method.Invoke(null, new object[] { zone, stamp, x, y }));
            Assert.IsTrue(zone.GenReservedCells.Remove((x, y)));
            Assert.IsTrue((bool)method.Invoke(null, new object[] { zone, stamp, x, y }), "The reservation itself must protect this otherwise buildable cell.");
            int pools = 0;
            for(int py=0;py<Zone.Height;py++)for(int px=0;px<Zone.Width;px++)
                if(builder.Plan.IsWater(px,py))
                {
                    pools++;
                    Assert.IsTrue(zone.GenReservedCells.Contains((px,py)),"A native pool also reserves its basin.");
                    Assert.IsFalse((bool)method.Invoke(null,new object[]{zone,stamp,px,py}));
                }
            Assert.Greater(pools,0,"The pool protection precondition must be present.");
        }

        // H14: late hazards, containers, residents or landmarks block one
        // of the four semantic entries despite a passing terrain-only test.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void Adversarial_CompleteNativePipelineConnectsAllFourApproachesForEveryFormation(int seed)
        {
            var manager = new OverworldZoneManager(factory, seed);
            var seenForms = new HashSet<Formation>();
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (!BeatingCompositionPlan.IsWildernessZone(id) || manager.WorldMap.GetPOI(x, y) != null) continue;
                var form = FormationSelector.For(BiomeType.Beating, id);
                if (seenForms.Contains(form)) continue;
                var plan = BeatingCompositionPlan.Create(id, seed);
                var zone = manager.GetZone(id); Assert.NotNull(zone);
                var reached = Flood(zone, 0, plan.WestY);
                foreach (var entry in new[] { (0, plan.WestY), (Zone.Width - 1, plan.EastY), (plan.NorthX, 0), (plan.SouthX, Zone.Height - 1) })
                {
                    Assert.IsFalse(zone.GetCell(entry.Item1, entry.Item2).BlocksMovement(), id + " seed " + seed + " obstructed entry " + entry);
                    Assert.IsTrue(reached[entry.Item1, entry.Item2], id + " seed " + seed + " disconnected entry " + entry);
                }
                Assert.IsTrue(reached[plan.FocalX, plan.FocalY], id + " central passing place disconnected");
                for (int cy = 0; cy < Zone.Height; cy++) for (int cx = 0; cx < Zone.Width; cx++)
                    if (plan.IsApproach(cx, cy))
                    {
                        var cell = zone.GetCell(cx, cy);
                        Assert.IsFalse(cell.Objects.Any(e => e.HasPart<LiquidPoolPart>()), id + " hazardous approach " + cx + "," + cy);
                    }
                // Later stamps and solid containers can isolate an outcrop
                // without breaking any edge-to-edge crossing. Verify the native
                // salt economy after the complete pipeline, not merely the plan.
                if(form==Formation.SaltPan)
                {
                    var veins=zone.GetAllEntities().Where(e=>e.BlueprintName=="PaleSaltVein").ToArray();
                    Assert.That(veins.Length,Is.InRange(2,4),id+" later builders erased scarce salt supply");
                    foreach(var vein in veins)
                    {
                        var cell=zone.GetEntityCell(vein);
                        Assert.IsTrue(Directions.Any(d=>
                        {
                            int nx=cell.X+d.x,ny=cell.Y+d.y;
                            return zone.GetCell(nx,ny)!=null&&reached[nx,ny];
                        }),id+" seed "+seed+" salt at "+cell.X+","+cell.Y+" has no reachable harvest side");
                    }
                }
                seenForms.Add(form);
            }
            CollectionAssert.AreEquivalent(BeatingCompositionTests.Forms, seenForms, "Every formation needs a real eligible runtime address.");
        }

        // H15: a failed rebuild reuses the success diagnostic and falsely
        // suggests a player's modified zone was regenerated.
        [Test] public void Adversarial_SuccessAndRejectedRebuildEmitDistinctDiagnostics()
        {
            var zone = new Zone(BeatingCompositionTests.Id); var builder = new BeatingCompositionBuilder(64);
            Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
            Assert.IsFalse(builder.BuildZone(zone, factory, new System.Random(1)));
            var success = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "BeatingCompositionPlanned", Limit = 10 }).Records;
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "worldgen", Kind = "BeatingCompositionRejected", Limit = 10 }).Records;
            Assert.AreEqual(1, success.Count); Assert.AreEqual(1, rejected.Count);
            StringAssert.Contains("nonempty-zone", rejected[0].PayloadJson);
            StringAssert.Contains("64", success[0].PayloadJson);
        }

        // H16: the generically traversable world masks a broken painted
        // road, particularly where north/south portals are far apart in X.
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_ThePaintedRoadItselfConnectsItsPrimaryEntries(bool eastWest)
        {
            int exercised=0;
            for(int seed=0;seed<128;seed++)
            {
                var plan=BeatingCompositionPlan.Create(BeatingCompositionTests.Id,seed,Formation.CaravanRoad);
                if(plan.RoadEastWest!=eastWest)continue;
                var zone=Build(Formation.CaravanRoad,seed);
                int sx=eastWest?0:plan.NorthX,sy=eastWest?plan.WestY:0;
                int tx=eastWest?Zone.Width-1:plan.SouthX,ty=eastWest?plan.EastY:Zone.Height-1;
                var reached=FloodRoad(zone,sx,sy);
                Assert.IsTrue(reached[tx,ty],"Painted road disconnected: seed "+seed+" eastWest="+eastWest);
                exercised++;
            }
            Assert.Greater(exercised,10,"Both orientation branches must actually be exercised.");
        }

        // H19: the documented passing place clears an off-road oval but
        // never becomes visible packed road. A complete 5x5 paved patch is
        // slope-independent: a steep three-cell-wide DDA road may have a wide
        // row cross-section, but cannot contain this two-dimensional core.
        [Test] public void Adversarial_CaravanPassingPlaceVisiblyWidensTheConnectedNativeRoad()
        {
            foreach(bool eastWest in new[]{true,false})
            {
                int exercised=0;
                for(int seed=0;seed<128&&exercised<4;seed++)
                {
                    var plan=BeatingCompositionPlan.Create(BeatingCompositionTests.Id,seed,Formation.CaravanRoad);
                    if(plan.RoadEastWest!=eastWest)continue;
                    var zone=Build(Formation.CaravanRoad,seed);
                    var paving=new bool[Zone.Width,Zone.Height];
                    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                        paving[x,y]=zone.GetCell(x,y).Objects.Any(e=>e.BlueprintName=="RoadStone");
                    var widePositions=new HashSet<int>();
                    for(int y=2;y<Zone.Height-2;y++)for(int x=2;x<Zone.Width-2;x++)
                    {
                        bool complete=true;
                        for(int dy=-2;dy<=2&&complete;dy++)for(int dx=-2;dx<=2;dx++)
                            if(!paving[x+dx,y+dy]){complete=false;break;}
                        if(!complete)continue;
                        widePositions.Add(eastWest?x:y);
                        for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                        {
                            int px=x+dx,py=y+dy;var cell=zone.GetCell(px,py);
                            Assert.IsTrue(zone.GenReservedCells.Contains((px,py)),"Passing-place paving lost its reservation.");
                            Assert.IsFalse(cell.BlocksMovement(),"Passing-place paving contains a blocker.");
                            Assert.IsFalse(cell.Objects.Any(e=>e.HasPart<LiquidPoolPart>()),"Passing-place paving became wet.");
                        }
                    }
                    Assert.Greater(widePositions.Count,0,"No complete 5x5 paved passing place on the native road: seed "+seed+" eastWest="+eastWest);
                    Assert.Less(widePositions.Count,(eastWest?Zone.Width:Zone.Height)/2,"Widening must remain local rather than turning the entire road into a plaza.");
                    int sx=eastWest?0:plan.NorthX,sy=eastWest?plan.WestY:0;
                    var reached=FloodRoad(zone,sx,sy);
                    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                        if(paving[x,y])Assert.IsTrue(reached[x,y],"Passing-place paving detached from the main road: seed "+seed);
                    exercised++;
                }
                Assert.AreEqual(4,exercised,"Both road orientations need several actual seeds.");
            }
        }

        // H17: reservation succeeds so aggressively that an 8x6 Tent-Right
        // camp can no longer fit anywhere, silently erasing hospitality.
        [Test] public void Adversarial_EachFormationLeavesARealUnreservedCampFootprint()
        {
            var guard=typeof(LandmarkBuilder).GetMethod("FootprintClear",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.NotNull(guard);
            var camp=new StructureStamp{Rows=Enumerable.Repeat("........",6).ToArray(),ClearsVegetation=true};
            foreach(var form in BeatingCompositionTests.Forms)for(int seed=0;seed<24;seed++)
            {
                var zone=Build(form,seed);bool fits=false;
                for(int y=2;y<=Zone.Height-8&&!fits;y++)for(int x=2;x<=Zone.Width-10;x++)
                    if((bool)guard.Invoke(null,new object[]{zone,camp,x,y})){fits=true;break;}
                Assert.IsTrue(fits,form+" seed "+seed+" has no 8x6 unreserved camp site");
            }
        }

        // H18: special-case routing survives the new wilderness branch;
        // the untended fire retains its deliberate empty pan and the two
        // abandoned counters keep their extra authored landmark pass.
        [Test] public void Adversarial_SpecialFireAndPredecessorCountersKeepTheirLegacyPipelines()
        {
            var manager=new OverworldZoneManager(factory,64);
            var get=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",PrivateInstance);
            foreach(string id in new[]{OverworldZoneManager.TenthFireZoneID,OverworldZoneManager.AbandonedCounterZoneA,OverworldZoneManager.AbandonedCounterZoneB})
            {
                var pipeline=(ZoneGenerationPipeline)get.Invoke(manager,new object[]{id});
                Assert.IsFalse(pipeline.Builders.Any(b=>b is BeatingCompositionBuilder),id);
                Assert.IsTrue(pipeline.Builders.Any(b=>b is BeatingFormationBuilder),id);
                if(id==OverworldZoneManager.TenthFireZoneID)
                {
                    var formation=(BeatingFormationBuilder)pipeline.Builders.Single(b=>b is BeatingFormationBuilder);
                    Assert.AreEqual(Formation.SaltPan,formation.Override);Assert.IsTrue(formation.OmitSaltVeins);
                    Assert.AreEqual(1,pipeline.Builders.Count(b=>b is LandmarkBuilder));
                    Assert.IsFalse(pipeline.Builders.Any(b=>b is ContainerBuilder || b is HazardTerrainBuilder));
                }
                else Assert.AreEqual(2,pipeline.Builders.Count(b=>b is LandmarkBuilder),"The authored predecessor stamp must remain in addition to ambient dressing.");
            }
        }

        // H20: safe routes cut straight through intended brine basins,
        // splitting three recognizable lenses into thin unrelated strips.
        // Count native pool components independently of plan geometry and
        // repeat at the preview address plus three natural BrineLens addresses.
        [Test] public void Adversarial_ApproachesSkirtThreeIntactNativeBrineBasins()
        {
            var ids=new List<string>{BeatingCompositionTests.Id};
            for(int y=0;y<WorldMap.Height&&ids.Count<4;y++)for(int x=0;x<WorldMap.Width&&ids.Count<4;x++)
            {
                string id=WorldMap.ToZoneID(x,y);
                if(id!=BeatingCompositionTests.Id&&BeatingCompositionPlan.IsWildernessZone(id)
                    &&FormationSelector.For(BiomeType.Beating,id)==Formation.BrineLens)ids.Add(id);
            }
            Assert.AreEqual(4,ids.Count,"Need the explicit preview control and three natural basin addresses.");
            var seeds=Enumerable.Range(0,22).Concat(new[]{64,1729});
            foreach(string id in ids)foreach(int seed in seeds)
            {
                var zone=new Zone(id);var builder=new BeatingCompositionBuilder(seed){FormationOverride=Formation.BrineLens};
                Assert.IsTrue(builder.BuildZone(zone,factory,new System.Random(seed)));
                var pools=new bool[Zone.Width,Zone.Height];
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var native=zone.GetCell(x,y).Objects.FirstOrDefault(e=>e.BlueprintName=="BrinePool");
                    if(native==null)continue;
                    pools[x,y]=true;
                    Assert.AreEqual("brine",native.GetPart<LiquidPoolPart>().LiquidId);
                    Assert.AreEqual(4,zone.TileState.CoatingTurns(x,y,"water"),"Composition must seed the native renewable pool lease, not permanent water.");
                    Assert.IsFalse(builder.Plan.IsApproach(x,y),"Dry routes must skirt native basin cells.");
                }
                var seen=new bool[Zone.Width,Zone.Height];var areas=new List<int>();var queue=new Queue<(int x,int y)>();
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(!pools[x,y]||seen[x,y])continue;
                    int area=0;seen[x,y]=true;queue.Enqueue((x,y));
                    while(queue.Count>0)
                    {
                        var c=queue.Dequeue();area++;
                        foreach(var d in Directions)
                        {
                            int nx=c.x+d.x,ny=c.y+d.y;
                            if(nx<0||ny<0||nx>=Zone.Width||ny>=Zone.Height||seen[nx,ny]||!pools[nx,ny])continue;
                            seen[nx,ny]=true;queue.Enqueue((nx,ny));
                        }
                    }
                    areas.Add(area);
                }
                Assert.AreEqual(3,areas.Count,id+" seed "+seed+" fragmented lenses; areas="+string.Join(",",areas));
                foreach(int area in areas)Assert.GreaterOrEqual(area,20,id+" seed "+seed+" basin reduced to a sliver");
                var dry=FloodDry(zone,0,builder.Plan.WestY);
                foreach(var entry in new[]{(Zone.Width-1,builder.Plan.EastY),(builder.Plan.NorthX,0),(builder.Plan.SouthX,Zone.Height-1)})
                    Assert.IsTrue(dry[entry.Item1,entry.Item2],id+" seed "+seed+" preserving basins disconnected a dry entry");
            }
            // Identical native builder with its formation switched should not
            // manufacture brine lenses in an exposed salt pan.
            Assert.IsFalse(Build(Formation.SaltPan,64).GetAllEntities().Any(e=>e.BlueprintName=="BrinePool"));
        }

        // H21: ruin rooms are individually readable but their promised old
        // street is only an invisible clearance in sand. Native paved ground
        // must connect every room center through passable breaks; floor-only
        // traversal is the countercheck proving the inter-room paving matters.
        [Test] public void Adversarial_RuinStreetConnectsRoomCentresWithNativePaving()
        {
            foreach(int seed in Enumerable.Range(0,7).Concat(new[]{64}))
            {
                var zone=Build(Formation.RuinField,seed);
                var plan=BeatingCompositionPlan.Create(zone.ZoneID,seed,Formation.RuinField);
                Assert.AreEqual(3,plan.RuinCount);
                var first=plan.GetRuin(0);int sx=first.X+first.Width/2,sy=first.Y+first.Height/2;
                var connected=FloodRuinPaving(zone,sx,sy,true);
                var withoutStreet=FloodRuinPaving(zone,sx,sy,false);
                for(int i=0;i<plan.RuinCount;i++)
                {
                    var room=plan.GetRuin(i);int x=room.X+room.Width/2,y=room.Y+room.Height/2;
                    Assert.IsTrue(connected[x,y],"Ruin room "+i+" seed "+seed+" has no continuous paved connection.");
                    if(i>0)Assert.IsFalse(withoutStreet[x,y],"The countercheck must separate rooms when connecting RoadStone is disallowed.");
                }
                int outside=0;
                foreach(var road in zone.GetAllEntities().Where(e=>e.BlueprintName=="RoadStone"))
                {
                    var cell=zone.GetEntityCell(road);
                    bool inRoom=false;for(int i=0;i<plan.RuinCount;i++)if(plan.GetRuin(i).Contains(cell.X,cell.Y)){inRoom=true;break;}
                    if(inRoom)continue;
                    outside++;
                    Assert.IsTrue(zone.GenReservedCells.Contains((cell.X,cell.Y)),"Old street outside the rooms must stay reserved.");
                    Assert.IsFalse(cell.BlocksMovement());
                    Assert.IsFalse(cell.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
                }
                Assert.Greater(outside,0,"RuinField seed "+seed+" paints no old street between buildings.");
            }
        }

        private static bool[,] FloodDry(Zone zone,int sx,int sy)
            => FloodSurface(zone,sx,sy,cell=>!cell.BlocksMovement()&&!cell.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
        private static bool[,] FloodRuinPaving(Zone zone,int sx,int sy,bool includeRoad)
            => FloodSurface(zone,sx,sy,cell=>!cell.BlocksMovement()&&cell.Objects.Any(e=>e.BlueprintName=="SandstoneFloor"||(includeRoad&&e.BlueprintName=="RoadStone")));
        private static bool[,] FloodSurface(Zone zone,int sx,int sy,Func<Cell,bool> accepts)
        {
            var seen=new bool[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();
            if(!accepts(zone.GetCell(sx,sy)))return seen;
            seen[sx,sy]=true;queue.Enqueue((sx,sy));
            while(queue.Count>0)
            {
                var c=queue.Dequeue();foreach(var d in Directions)
                {
                    int x=c.x+d.x,y=c.y+d.y;var cell=zone.GetCell(x,y);
                    if(cell==null||seen[x,y]||!accepts(cell))continue;
                    seen[x,y]=true;queue.Enqueue((x,y));
                }
            }
            return seen;
        }

        private static Entity Traveller(Zone zone,int x,int y)
        {
            var actor=new Entity{ID="beating-traveller",BlueprintName="Player"}; actor.Tags["Creature"]="";
            actor.AddPart(new RenderPart{DisplayName="traveller"});
            actor.Statistics["Strength"]=new Stat{Owner=actor,Name="Strength",BaseValue=16};
            actor.Statistics["Agility"]=new Stat{Owner=actor,Name="Agility",BaseValue=16};
            zone.AddEntity(actor,x,y);return actor;
        }
        private static void FireAction(Entity source,Entity actor,Zone zone,string command)
        {
            var e=GameEvent.New("InventoryAction");e.SetParameter("Command",command);e.SetParameter("Actor",actor);
            e.SetParameter("Zone",zone);e.SetParameter("Random",new System.Random(64));
            try{source.FireEvent(e);}finally{e.Release();}
        }
        private static bool[,] FloodRoad(Zone zone,int sx,int sy)
        {
            var seen=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y)>();
            Assert.IsTrue(zone.GetCell(sx,sy).Objects.Any(e=>e.BlueprintName=="RoadStone"),"Road entry lacks packed road.");
            seen[sx,sy]=true;q.Enqueue((sx,sy));
            while(q.Count>0)
            {
                var c=q.Dequeue();foreach(var d in Directions)
                {
                    int x=c.x+d.x,y=c.y+d.y;var cell=zone.GetCell(x,y);
                    if(cell==null||seen[x,y]||!cell.Objects.Any(e=>e.BlueprintName=="RoadStone"))continue;
                    seen[x,y]=true;q.Enqueue((x,y));
                }
            }
            return seen;
        }

        private Zone Build(Formation form, int seed)
        {
            var zone = new Zone(BeatingCompositionTests.Id);
            Assert.IsTrue(new BeatingCompositionBuilder(seed) { FormationOverride = form }.BuildZone(zone, factory, new System.Random(seed)));
            return zone;
        }
        private static string FirstEligibleAddress()
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (BeatingCompositionPlan.IsWildernessZone(id)) return id;
            }
            Assert.Fail("No eligible Beating address."); return null;
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
                foreach (var d in Directions)
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
