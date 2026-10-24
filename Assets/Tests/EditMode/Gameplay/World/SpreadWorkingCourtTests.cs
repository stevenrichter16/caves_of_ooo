using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cold kitchen framing uses actual walkable stones and solid masonry,
    /// preserving the original plantable grass and existing finite services.</summary>
    public sealed class SpreadWorkingCourtTests
    {
        const string Kitchen = "Overworld.12.11.0", Role = "SpreadResident.Role";
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        static readonly (int x, int y)[] Walls = { (-3,-4),(-2,-4),(-1,-4),(0,-4),(1,-4),(2,-4),(-3,-3),(-3,-2),(-3,-1) };
        static readonly (int x, int y)[] Stones = { (-1,-3),(0,-3),(1,-3),(2,-3),(-1,-2),(-1,-1),(0,-1),(1,-1),(1,0),(0,1),(1,1),(2,1),(3,1),(3,-1),(3,0) };
        static readonly (int x, int y)[] Court = { (0,-3),(1,-3),(2,-3),(0,-1),(1,-1),(3,-2),(3,-1),(3,0),(1,0),(0,1) };
        static readonly (string role, int x, int y)[] Services = { ("resident",0,0),("oven",-2,0),("cot",2,0),("chair",2,-1),
            ("pan",0,-2),("escrow",1,-2),("pickup",2,-2),("kitchen-crop",-2,2) };

        sealed class Fixture : IDisposable
        {
            internal readonly HaulingContentScope Scope = new HaulingContentScope();
            internal EntityFactory Factory => Scope.Factory;
            internal readonly Zone Zone;
            internal readonly SpreadCompositionBuilder Terrain;
            internal Entity[] Owners;
            internal Func<bool> Final;
            readonly Action<Entity> oldCallback = SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback;
            internal Fixture(string id = Kitchen, SpreadExplorationTopology topology = SpreadExplorationTopology.BrokenEnclosures,
                Formation formation = Formation.Hedgerow)
            {
                Scope.Seed(64); Zone = new Zone(id);
                Terrain = new SpreadCompositionBuilder(64) { FormationOverride = formation, Topology = topology };
                Assert.True(Terrain.BuildZone(Zone, Factory, new Random(64)));
                // Controlled callback/negative fixture only; the positive geometry tests
                // below generate wholly unedited native seed64/65/60 worlds.
                var bare = typeof(DoorPart).GetMethod("IsBareGround", All);
                foreach (var owner in Zone.GetReadOnlyEntities().Where(e => !(bool)bare.Invoke(null, new object[] { e })).ToArray()) Zone.RemoveEntity(owner);
                Zone.GenReservedCells.Clear();
            }
            internal bool Place(bool connected = true, Func<bool> authority = null)
            {
                var type = typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationResidents");
                object[] args = { Zone, Factory, Terrain, SpreadExplorationFamily.WaysideKitchen,
                    authority ?? (() => true), null, null, connected, "0123456789abcdef0123456789abcdef" };
                bool result = (bool)type.GetMethod("TryPlaceConnected", All).Invoke(null, args);
                Owners = (Entity[])args[5]; Final = (Func<bool>)args[6]; return result;
            }
            internal void OnCreate(string blueprint, Action<Entity> callback)
            {
                Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");
                Factory.Blueprints[blueprint].Parts["ReceiptCreated"] = new Dictionary<string, string>();
                SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = callback;
            }
            public void Dispose() { SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback = oldCallback; Scope.Dispose(); }
        }
        static Entity One(Zone zone, string role) => zone.GetReadOnlyEntities().Single(e => e.GetProperty(Role) == role);
        static Entity[] Named(Zone zone, string role) => zone.GetReadOnlyEntities().Where(e => e.GetProperty(Role) == role).ToArray();
        static Func<int, int, (int x, int y)> Transform(Zone zone)
        {
            var origin = zone.GetEntityPosition(One(zone, "resident")); var oven = zone.GetEntityPosition(One(zone, "oven"));
            int ex = (origin.x - oven.x) / 2, ey = (origin.y - oven.y) / 2;
            Assert.AreEqual(1, Math.Abs(ex) + Math.Abs(ey));
            return (x, y) => (origin.x + x * ex - y * ey, origin.y + x * ey + y * ex);
        }
        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Zone.Width && y < Zone.Height;
        static IEnumerable<(int x, int y)> Neighbors((int x, int y) p)
        { yield return (p.x-1,p.y); yield return (p.x+1,p.y); yield return (p.x,p.y-1); yield return (p.x,p.y+1); }
        static bool Walk(Zone zone, (int x, int y) p) => In(p.x,p.y) && !zone.GetCell(p.x,p.y).BlocksMovement()
            && zone.TileState.Get(p.x,p.y)?.IsEmpty != false && !zone.GetCell(p.x,p.y).Occupants.Any(e => e.HasTag("Creature")
                || e.HasPart<TriggerOnStepPart>() || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>());
        static HashSet<(int x, int y)> Reach(Zone zone)
        {
            var seen = new HashSet<(int x, int y)>(); var queue = new Queue<(int x, int y)>(); queue.Enqueue((40,12));
            while (queue.Count > 0) { var p = queue.Dequeue(); if (!Walk(zone,p) || !seen.Add(p)) continue; foreach (var n in Neighbors(p)) queue.Enqueue(n); }
            return seen;
        }
        static Zone Generate(HaulingContentScope scope, int seed, out OverworldZoneManager manager)
        {
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(Kitchen, int.MaxValue)));
            manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var zone = manager.GetZone(Kitchen);
            Assert.NotNull(zone); Assert.AreEqual(2, manager.Exploration.DispositionFor(Kitchen), "The required real kitchen must commit."); return zone;
        }
        static void AssertStamp(Zone zone)
        {
            var at = Transform(zone); var walls = Named(zone,"shelter"); var floors = Named(zone,"court-floor");
            Assert.AreEqual(9, walls.Length, "A continuous back edge replaces the two isolated cold walls.");
            Assert.AreEqual(15, floors.Length, "A real sparse working surface must be present.");
            CollectionAssert.AreEquivalent(Walls.Select(p => at(p.x,p.y)), walls.Select(zone.GetEntityPosition));
            CollectionAssert.AreEquivalent(Stones.Select(p => at(p.x,p.y)), floors.Select(zone.GetEntityPosition));
            foreach (var s in Services) Assert.AreEqual(at(s.x,s.y), zone.GetEntityPosition(One(zone,s.role)), s.role);
            foreach (var wall in walls)
            {
                Assert.AreEqual("StoneWall",wall.BlueprintName); Assert.True(wall.HasTag("Wall")); Assert.True(wall.HasTag("Solid"));
                Assert.True(wall.GetPart<PhysicsPart>().Solid); Assert.AreEqual("Rubble",wall.GetPart<DestructiblePart>().WreckageBlueprint);
            }
            foreach (var floor in floors)
            {
                var cell = zone.GetEntityCell(floor); var physics = floor.GetPart<PhysicsPart>();
                Assert.AreEqual("StoneFloor",floor.BlueprintName); Assert.AreEqual("court-floor",floor.GetProperty("ConnectedSpread.Role"));
                Assert.True(floor.HasTag("Terrain")); Assert.False(floor.HasTag("Barren")); Assert.False(floor.HasTag("Vegetation"));
                Assert.False(physics.Solid); Assert.False(physics.Takeable); Assert.IsNull(physics.InInventory); Assert.IsNull(physics.Equipped);
                Assert.False(floor.HasPart<CultivatedSoilPart>()); Assert.False(floor.HasPart<CropPart>()); Assert.False(floor.HasPart<RepairablePart>());
                Assert.False(floor.HasPart<ContainerPart>()); Assert.False(floor.HasTag("Plantable"));
                Assert.AreEqual("worn stepping stones",floor.GetPart<RenderPart>().DisplayName);
                StringAssert.Contains("grass",floor.GetPart<ExaminablePart>().GetDescriptionText().ToLowerInvariant());
                Assert.AreEqual(1,cell.Objects.Count(e => e.BlueprintName == "Grass" && e.HasTag("Plantable")));
                Assert.False(BarrenGroundRules.IsBarren(cell)); Assert.False(CultivatedSoilPart.IsCultivated(zone,cell));
                Assert.False(cell.IsInterior); Assert.False(zone.GenReservedCells.Contains((cell.X,cell.Y)));
                Assert.False(cell.Objects.Any(e => e != floor && e.GetProperty(Role) != null), "No stones under service, wall or crop.");
            }
            var crop = One(zone,"kitchen-crop"); Assert.True(CultivatedSoilPart.IsCultivated(zone,zone.GetEntityCell(crop)));
            Assert.False(zone.GetEntityCell(crop).Objects.Any(e => e.BlueprintName == "StoneFloor"));
            Assert.AreEqual(2,crop.GetPart<CropPart>().GrowthStage); Assert.AreEqual(2,crop.GetPart<CropPart>().YieldCount); Assert.AreEqual(1,crop.GetPart<CropPart>().SeedYieldCount);
            var cook = One(zone,"resident"); Assert.AreEqual(35,TradeSystem.GetDrams(cook));
            Assert.AreEqual(5,cook.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            var batch = One(zone,"pan").GetPart<KitchenBatchPart>(); Assert.True(batch.Configured);
            Assert.AreSame(cook,batch.Worker); Assert.AreSame(One(zone,"escrow"),batch.Escrow); Assert.AreSame(One(zone,"pickup"),batch.Pickup);
            Assert.AreEqual("Idle",batch.State); Assert.IsEmpty(batch.Escrow.GetPart<ContainerPart>().Contents); Assert.IsEmpty(batch.Pickup.GetPart<ContainerPart>().Contents);
        }
        [TestCase(64)] [TestCase(65)] [TestCase(60)]
        public void ActualColdKitchenHasTruthfulFramingAndReachableServices(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var zone = Generate(scope,seed,out var manager); AssertStamp(zone); var at = Transform(zone); var reached = Reach(zone);
                Assert.True(reached.Contains((40,12)));
                foreach (var point in Court) Assert.True(reached.Contains(at(point.x,point.y)), "Court " + point);
                for (int x=-2;x<=3;x++) Assert.True(reached.Contains(at(x,-3)), "Walking row behind trays " + x);
                foreach (var s in Services)
                {
                    var p = zone.GetEntityPosition(One(zone,s.role));
                    Assert.GreaterOrEqual(Neighbors(p).Count(reached.Contains),2,s.role + " needs reachable standing cells.");
                }
                Assert.True(reached.Contains(zone.GetEntityPosition(One(zone,"cot"))), "The bed is used by standing on it.");
                var plan = SpreadCompositionPlan.Create(Kitchen,seed,Formation.Hedgerow,manager.Exploration.Entries.Single(e => e.ZoneID == Kitchen).Topology);
                foreach (var p in new[] { (0,plan.WestY),(79,plan.EastY),(plan.NorthX,0),(plan.SouthX,24) }) Assert.True(reached.Contains(p), "Port " + p);
            }
        }
        [Test]
        public void OverlayRetainsExactGrassOwnersAndPublicationProofTracksEveryNewStone()
        {
            using (var f = new Fixture())
            {
                var ground = f.Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Grass").ToDictionary(e => f.Zone.GetEntityPosition(e));
                Assert.True(f.Place()); AssertStamp(f.Zone); Assert.True(f.Final());
                foreach (var floor in Named(f.Zone,"court-floor"))
                {
                    var cell = f.Zone.GetEntityCell(floor); Assert.True(cell.Objects.Contains(ground[(cell.X,cell.Y)]));
                    Assert.AreSame(cell,f.Zone.GetEntityCell(ground[(cell.X,cell.Y)]));
                }
                var removed = Named(f.Zone,"court-floor")[0]; var at = f.Zone.GetEntityPosition(removed);
                Assert.True(f.Zone.RemoveEntity(removed)); Assert.False(f.Final()); Assert.True(f.Zone.GetCell(at.x,at.y).Objects.Contains(ground[at]));
                Assert.False(f.Place(), "A missing stone must not mint a second resident packet.");
            }
        }
        [TestCase("legacy")] [TestCase("unconnected")] [TestCase("outside")] [TestCase("other-formation")]
        public void LegacyAndUnselectedResidentStampsKeepTheirTwoWalls(string mode)
        {
            using (var f = new Fixture(mode == "outside" ? "Overworld.13.11.0" : Kitchen,
                mode == "legacy" ? SpreadExplorationTopology.Legacy : SpreadExplorationTopology.BrokenEnclosures,
                mode == "other-formation" ? Formation.OldRoad : Formation.Hedgerow))
            {
                Assert.True(f.Place(mode != "unconnected")); Assert.AreEqual(2,Named(f.Zone,"shelter").Length);
                Assert.IsEmpty(Named(f.Zone,"court-floor"));
                var at = Transform(f.Zone); CollectionAssert.AreEquivalent(new[] { at(-2,-2),at(-1,-2) },Named(f.Zone,"shelter").Select(f.Zone.GetEntityPosition));
            }
        }
        [TestCase("barren")] [TestCase("solid")] [TestCase("takeable")] [TestCase("crop")] [TestCase("pool")]
        [TestCase("closed-door")] [TestCase("closed-barrier")] [TestCase("tile-source")]
        public void MalformedCourtFloorRefusesBeforeItCanRemoveAnyGrass(string fault)
        {
            using (var f = new Fixture())
            {
                var original = f.Zone.GetReadOnlyEntities().ToArray(); var cells = original.ToDictionary(e => e,e => f.Zone.GetEntityCell(e)); int created=0;
                f.OnCreate("StoneFloor",e => { created++;
                    if (fault == "barren") e.SetTag("Barren");
                    if (fault == "solid") e.GetPart<PhysicsPart>().Solid=true;
                    if (fault == "takeable") e.GetPart<PhysicsPart>().Takeable=true;
                    if (fault == "crop") e.AddPart(new CropPart());
                    if (fault == "pool") e.AddPart(new LiquidPoolPart());
                    if (fault == "closed-door") e.AddPart(new DoorPart { IsOpen = false });
                    if (fault == "closed-barrier") e.AddPart(new SealedLibraryBarrierPart());
                    if (fault == "tile-source") e.AddPart(new TileStateSourcePart { Coating = "water" });
                });
                Assert.False(f.Place()); Assert.Greater(created,0,"The negative must reach an actual staged floor.");
                CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());
                foreach (var pair in cells) { Assert.AreSame(pair.Value,f.Zone.GetEntityCell(pair.Key)); Assert.True(pair.Value.Objects.Contains(pair.Key)); }
                Assert.IsNull(f.Owners); Assert.IsNull(f.Final);
            }
        }
        [TestCase("revoke")] [TestCase("source-change")]
        public void FloorCreationCallbackCannotCommitAgainstChangedAuthorityOrOriginalSources(string fault)
        {
            using (var f = new Fixture())
            {
                bool allowed=true; int created=0; var original=f.Zone.GetReadOnlyEntities().ToArray(); var source=original[0];
                f.OnCreate("StoneFloor",e => { created++; if(fault=="revoke") allowed=false; else source.SetIntProperty("court-callback",19); });
                Assert.False(f.Place(authority:()=>allowed)); Assert.Greater(created,0); CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());
                if(fault=="source-change") Assert.AreEqual(19,source.GetIntProperty("court-callback"),"Caller-owned state is not ours to undo.");
            }
        }
        [Test]
        public void AStagedFloorMutatedDuringPublicationCannotClearOriginalGrass()
        {
            using (var f = new Fixture())
            {
                var original = f.Zone.GetReadOnlyEntities().ToArray();
                var cells = original.ToDictionary(e => e, e => f.Zone.GetEntityCell(e));
                Entity pendingFloor = null; bool mutated = false;
                f.OnCreate("StoneFloor", e => { if (pendingFloor == null) pendingFloor = e; });
                bool Authority()
                {
                    if (pendingFloor != null && !mutated && f.Zone.GetReadOnlyEntities().Any(e => e.GetProperty(Role) == "resident"))
                    { pendingFloor.SetTag("Barren"); mutated = true; }
                    return true;
                }
                Assert.False(f.Place(authority: Authority)); Assert.True(mutated, "Mutation must occur after publication begins.");
                CollectionAssert.AreEquivalent(original, f.Zone.GetReadOnlyEntities());
                foreach (var pair in cells) { Assert.AreSame(pair.Value, f.Zone.GetEntityCell(pair.Key)); Assert.True(pair.Value.Objects.Contains(pair.Key)); }
            }
        }

        [Test]
        public void ACounterfeitSolidCotCannotPassTheBedStandingContract()
        {
            using (var f = new Fixture())
            {
                var original=f.Zone.GetReadOnlyEntities().ToArray(); int created=0;
                f.OnCreate("Bed",e=>{created++;e.GetPart<PhysicsPart>().Solid=true;});
                Assert.False(f.Place()); Assert.Greater(created,0); CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());
            }
        }
        [Test]
        public void VisitedSavedCourtKeepsRemovedMasonryStonesAndFiniteServiceStateLiteral()
        {
            using(var scope=new HaulingContentScope())
            {
                var zone=Generate(scope,64,out var manager); AssertStamp(zone);
                var wall=Named(zone,"shelter")[0]; var stone=Named(zone,"court-floor")[0]; var floorAt=zone.GetEntityPosition(stone);
                var grass=zone.GetCell(floorAt.x,floorAt.y).Objects.Single(e=>e.BlueprintName=="Grass");
                Assert.True(zone.RemoveEntity(wall)); Assert.True(zone.RemoveEntity(stone));
                var crop=One(zone,"kitchen-crop"); Assert.True(zone.RemoveEntity(crop)); One(zone,"pan").GetPart<RepairablePart>().Repaired=true;
                var pack=One(zone,"resident").GetPart<InventoryPart>(); var item=pack.Objects[0]; Assert.True(pack.RemoveObject(item));
                var player=scope.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(player,40,12)); manager.SetActiveZone(zone);
                string Snapshot(Zone z)=>string.Join(";",z.GetReadOnlyEntities().OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.BlueprintName+":"+z.GetEntityPosition(e)+":"+e.GetProperty(Role)));
                string before=Snapshot(zone); manager.UnloadZone(Kitchen); Assert.AreSame(zone,manager.GetZone(Kitchen)); Assert.AreEqual(before,Snapshot(zone));
                var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("working-court","literal-court",manager,null,player)); var restored=loaded.ZoneManager.ActiveZone;
                Assert.AreEqual(before,Snapshot(restored)); Assert.AreEqual(8,Named(restored,"shelter").Length); Assert.AreEqual(14,Named(restored,"court-floor").Length);
                Assert.True(restored.GetCell(floorAt.x,floorAt.y).Objects.Any(e=>e.ID==grass.ID&&e.HasTag("Plantable")));
                Assert.True(One(restored,"pan").GetPart<RepairablePart>().Repaired); Assert.IsEmpty(Named(restored,"kitchen-crop"));
                Assert.False(One(restored,"resident").GetPart<InventoryPart>().Objects.Any(e=>e.ID==item.ID));
                Assert.AreSame(restored,loaded.ZoneManager.GetZone(Kitchen)); Assert.AreEqual(before,Snapshot(restored));
            }
        }
    }
}
