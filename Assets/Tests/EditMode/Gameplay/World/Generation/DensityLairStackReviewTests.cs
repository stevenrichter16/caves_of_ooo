using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairStackReviewTests
    {
        private DensityLootTestScope scope;
        private const string Surface = "Overworld.4.9.0", Bottom = "Overworld.4.9.1";
        [SetUp]
        public void Setup()
        {
            scope = new DensityLootTestScope();
        }
        [TearDown]
        public void Cleanup()
        {
            scope.Dispose();
        }
        private void Configure(OverworldZoneManager manager, BiomeType biome = BiomeType.Spread)
        {
            manager.WorldMap.Tiles[4, 9] = biome;
            manager.WorldMap.SetPOI(4, 9, new PointOfInterest(POIType.Lair, "review lair", null, 2, "MarlbackWallkeeper"));
        }
        public sealed class CallbackManager : OverworldZoneManager
        {
            public Action<Zone> After;
            public CallbackManager(EntityFactory factory, int seed) : base(factory, seed) { }
            protected override void OnZoneGenerated(Zone zone, string id)
            {
                base.OnZoneGenerated(zone, id);
                After?.Invoke(zone);
            }
        }
        [TestCase("boss", "remove")]
        [TestCase("boss", "rename")]
        [TestCase("boss", "replace")]
        [TestCase("boss", "part")]
        [TestCase("reward", "remove")]
        [TestCase("reward", "rename")]
        [TestCase("reward", "replace")]
        [TestCase("reward", "part")]
        [TestCase("up", "remove")]
        [TestCase("up", "rename")]
        [TestCase("up", "replace")]
        [TestCase("up", "part")]
        public void CallbackMutationCannotPublishAFalseOwnerClaim(string role, string mode)
        {
            var manager = new CallbackManager(scope.Factory, 64);
            Configure(manager);
            manager.After = zone =>
            {
                var owner = zone.GetReadOnlyEntities().Single(e => role == "boss" ? e.BlueprintName == "MarlbackWallkeeper" : role == "reward" ? e.HasPart<ContainerPart>() && !e.HasPart<AIAmbushPart>() : e.HasPart<StairsUpPart>());
                var at = zone.GetEntityCell(owner);
                if (mode == "rename")
                    owner.ID = "changed-after-staging";
                else if (mode == "part")
                {
                    if (role == "boss")
                        owner.Tags.Remove("Creature");
                    else if (role == "reward")
                        owner.RemovePart(owner.GetPart<ContainerPart>());
                    else
                        owner.RemovePart(owner.GetPart<StairsUpPart>());
                }
                else
                {
                    zone.RemoveEntity(owner);
                    if (mode == "replace")
                        zone.AddEntity(new Entity { ID = owner.ID, BlueprintName = owner.BlueprintName }, at.X, at.Y);
                }
            };
            Assert.Null(manager.GetZone(Bottom));
            Assert.Null(LairStacks.Inspect(manager, Surface));
            Assert.Zero(manager.CachedZoneCount);
            Assert.Zero(manager.GetConnections(Surface).Count);
        }
        [Test]
        public void CallbackBlockingTheStairCannotPublishAnUnsafeEndpoint()
        {
            var manager = new CallbackManager(scope.Factory, 64);
            Configure(manager);
            manager.After = zone => { var up = zone.GetReadOnlyEntities().Single(e => e.HasPart<StairsUpPart>()); var c = zone.GetEntityCell(up); var wall = new Entity(); wall.SetTag("Solid"); zone.AddEntity(wall, c.X, c.Y); };
            Assert.Null(manager.GetZone(Bottom));
            Assert.Null(LairStacks.Inspect(manager, Surface));
            Assert.Zero(manager.GetConnections(Surface).Count);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void ExistingMovedEndpointUsesItsActualPhysicalCoordinate(bool lowerFirst)
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64);
            Configure(manager);
            var first = manager.GetZone(lowerFirst ? Bottom : Surface);
            var stairs = first.GetReadOnlyEntities().Single(e => lowerFirst ? e.HasPart<StairsUpPart>() : e.HasPart<StairsDownPart>());
            var destination = Enumerable.Range(35, 9).SelectMany(x => Enumerable.Range(9, 6).Select(y => first.GetCell(x, y))).First(c => !c.BlocksMovement() && c.Occupants.All(e => e.HasTag("Terrain")));
            first.RemoveEntity(stairs);
            Assert.True(first.AddEntity(stairs, destination.X, destination.Y));
            Assert.NotNull(manager.GetZone(lowerFirst ? Surface : Bottom));
            var edge = manager.GetConnectionsTo(Bottom, "StairsDown").Single();
            Assert.AreEqual(destination.X, lowerFirst ? edge.TargetX : edge.SourceX);
            Assert.AreEqual(destination.Y, lowerFirst ? edge.TargetY : edge.SourceY);
            Assert.AreSame(stairs, first.GetReadOnlyEntities().Single(e => e.ID == stairs.ID));
        }
        [TestCase("remove")]
        [TestCase("rename")]
        [TestCase("part")]
        [TestCase("solid")]
        public void CallbackInvalidatingCachedCounterpartCannotPublishTheNewFloor(string mode)
        {
            var manager = new CallbackManager(scope.Factory, 64);
            Configure(manager);
            var surface = manager.GetZone(Surface);
            manager.After = zone =>
            {
                var down = surface.GetReadOnlyEntities().Single(e => e.HasPart<StairsDownPart>());
                var cell = surface.GetEntityCell(down);
                if (mode == "remove")
                    surface.RemoveEntity(down);
                else if (mode == "rename")
                    down.ID = "invalidated-counterpart";
                else if (mode == "part")
                    down.RemovePart(down.GetPart<StairsDownPart>());
                else
                {
                    var wall = new Entity();
                    wall.SetTag("Solid");
                    surface.AddEntity(wall, cell.X, cell.Y);
                }
            };
            Assert.Null(manager.GetZone(Bottom));
            Assert.AreEqual(1, LairStacks.Inspect(manager, Surface).GeneratedMask);
            Assert.Null(LairStacks.Inspect(manager, Surface).BossID);
            Assert.AreEqual(1, manager.CachedZoneCount);
        }
        [TestCase("0,0;1,0")][TestCase("2,0;3,0")]
        public void BossAndStairFootprintsStayClearAndEdgesUsePhysicalCells(string cells)
        {
            scope.Factory.Blueprints["MarlbackWallkeeper"].Parts["SpatialFootprint"]=new Dictionary<string,string>{{"CellsRaw",cells}};
            scope.Factory.Blueprints["StairsUp"].Parts["SpatialFootprint"]=new Dictionary<string,string>{{"CellsRaw",cells}};
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64);Configure(manager);var zone=manager.GetZone(Bottom);Assert.NotNull(zone);
            var plan=LairStacks.Inspect(manager,Surface);var boss=zone.GetReadOnlyEntities().Single(e=>e.ID==plan.BossID);
            Assert.AreEqual(2,zone.GetOccupiedCells(boss).Count);foreach(var c in zone.GetOccupiedCells(boss))Assert.False(zone.GenReservedCells.Contains((c.X,c.Y)));
            var up=zone.GetReadOnlyEntities().Single(e=>e.HasPart<StairsUpPart>());var physical=zone.GetOccupiedCells(up)[0];
            var edge=manager.GetConnectionsTo(Bottom,"StairsDown").Single();Assert.AreEqual(physical.X,edge.TargetX);Assert.AreEqual(physical.Y,edge.TargetY);
        }
        [TestCase("0,0;100,0")][TestCase("0,0;-100,0")]
        public void ImpossibleBossFootprintCannotCommitARewardOrClaim(string cells)
        {
            scope.Factory.Blueprints["MarlbackWallkeeper"].Parts["SpatialFootprint"]=new Dictionary<string,string>{{"CellsRaw",cells}};
            var manager=OverworldZoneManager.CreateDetached(scope.Factory,64);Configure(manager);Assert.Null(manager.GetZone(Bottom));
            Assert.Null(LairStacks.Inspect(manager,Surface));Assert.Zero(manager.GetConnections(Surface).Count);
        }
        private HashSet<string> Leaves(string table)
        {
            var result = new HashSet<string>();
            var visited = new HashSet<string>();
            void Visit(string name)
            {
                if (!visited.Add(name))
                    return;
                var t = LootTableRegistry.Get(name);
                Assert.NotNull(t, name);
                foreach (var e in t.Entries)
                {
                    if (e.Blueprint != null)
                        result.Add(e.Blueprint);
                    else if (e.TableRef != null)
                        Visit(e.TableRef);
                }
            }
            Visit(table);
            result.Add("GoldCoin");
            return result;
        }
        [TestCase(BiomeType.Spread)]
        [TestCase(BiomeType.Sodden)]
        [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Grovelands)]
        public void FinalCacheContentsRespectTheSelectedExistingBiomeTable(BiomeType biome)
        {
            for (int seed = 1; seed <= 12; seed++)
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                Configure(manager, biome);
                var zone = manager.GetZone(Bottom);
                var reward = zone.GetReadOnlyEntities().Single(e => e.ID == LairStacks.Inspect(manager, Surface).RewardID);
                var kind = ContainerPlacementService.PoolFor(biome, ContainerPlacementService.ZoneKind.Lair).Single(k => k.Blueprint == reward.BlueprintName);
                var allowed = Leaves(kind.TablePrefix + "2");
                Assert.Greater(reward.GetPart<ContainerPart>().Contents.Count, 0);
                foreach (var item in reward.GetPart<ContainerPart>().Contents)
                    CollectionAssert.Contains(allowed, item.BlueprintName, biome + ":" + reward.BlueprintName);
            }
        }
    }
}
