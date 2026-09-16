using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class LastCounterCompositionAdversarialTests
    {
        [SetUp] public void LoadLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ClearLoot()=>LootTableRegistry.ResetForTests();
        [TestCase("DryBrush")] [TestCase("Rubble")]
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("SandstoneWall")]
        [TestCase("SaccharineEnvoy")] [TestCase("LastCounterSign")] [TestCase("Chest")] [TestCase("Campfire")]
        public void MissingLateOrEarlyDependencyRejectsBeforeMutation(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(LastCounterCompositionTests.Id);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new LastCounterCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("SaccharineEnvoy","Trader")] [TestCase("SaccharineEnvoy","Conversation")]
        [TestCase("LastCounterSign","Examinable")] [TestCase("Chest","Container")] [TestCase("Campfire","Campfire")]
        public void FailSoftMissingNativeFunctionCannotCreateVisualOnlyService(string bp,string part)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));var z=new Zone(LastCounterCompositionTests.Id);
            Assert.IsFalse(new LastCounterCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
        }
        [Test] public void ForeignLateGraphAndReplayedSupplyCannotDuplicateLoot()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new LastCounterProfileBuilder(b);
            Assert.IsFalse(late.BuildZone(new Zone(z.ZoneID),f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var chest=z.GetAllEntities().Single(e=>e.BlueprintName=="Chest");var items=chest.GetPart<ContainerPart>().Contents.ToArray();z.RemoveEntity(chest);
            Assert.IsFalse(late.BuildZone(z,f,new Random(1)));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="Chest"));CollectionAssert.AreEquivalent(items,chest.GetPart<ContainerPart>().Contents);
        }
        [Test] public void CaveFilterReadsLiveArrivalNeighborhoodAndGraphIdentity()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            Cell c=null;for(int y=1;y<24&&c==null;y++)for(int x=1;x<61&&c==null;x++)if(b.CanPlaceCaveEntrance(z,z.GetCell(x,y)))c=z.GetCell(x,y);
            Assert.NotNull(c);Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(z.ZoneID).GetCell(c.X,c.Y)));
            var pool=f.CreateEntity("WaterPuddle");z.AddEntity(pool,c.X+1,c.Y);Assert.IsFalse(b.CanPlaceCaveEntrance(z,c));z.RemoveEntity(pool);Assert.IsTrue(b.CanPlaceCaveEntrance(z,c));
            var wall=f.CreateEntity("SandstoneWall");z.AddEntity(wall,c.X+1,c.Y);Assert.IsFalse(b.CanPlaceCaveEntrance(z,c));z.RemoveEntity(wall);Assert.IsTrue(b.CanPlaceCaveEntrance(z,c));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualEnvoyTradeTransfersOnlyWhenTheBuyerCanPay(bool funded)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new LastCounterProfileBuilder(b).BuildZone(z,f,new Random(1)));
            var envoy=z.GetAllEntities().Single(e=>e.BlueprintName=="SaccharineEnvoy");var stock=envoy.GetPart<InventoryPart>();
            var item=stock.Objects.First(e=>e.BlueprintName=="CandyHeartRoot");var buyer=CinderholdCompositionTests.Player();
            int price=TradeSystem.GetBuyPrice(item,TradeSystem.GetTradePerformance(buyer),envoy);Assert.Greater(price,0);
            TradeSystem.SetDrams(buyer,funded?price:0);int purse=TradeSystem.GetDrams(envoy);
            Assert.AreEqual(funded,TradeSystem.BuyFromTrader(buyer,envoy,item));
            Assert.AreEqual(funded,buyer.GetPart<InventoryPart>().Objects.Contains(item));Assert.AreEqual(!funded,stock.Objects.Contains(item));
            Assert.AreEqual(purse+(funded?price:0),TradeSystem.GetDrams(envoy));
        }
        [TestCase(false)] [TestCase(true)]
        public void PlacedFireRestHealsAndCostsTimeOnlyWithoutNearbyHostile(bool hostile)
        {
            var previous=TurnManager.Active;FactionManager.Initialize();var tm=new TurnManager();
            try
            {
                var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new LastCounterProfileBuilder(b).BuildZone(z,f,new Random(1)));
                var fire=z.GetAllEntities().Single(e=>e.BlueprintName=="Campfire");var c=z.GetEntityPosition(fire);var player=CinderholdCompositionTests.Player();player.GetStat("Hitpoints").BaseValue=10;z.AddEntity(player,c.x,c.y-1);
                if(hostile){var enemy=f.CreateEntity("Snapjaw");z.AddEntity(enemy,c.x+1,c.y-1);Assert.IsTrue(FactionManager.IsHostile(enemy,player));}
                var action=new GameEvent("InventoryAction");action.SetParameter("Command","RestAtCampfire");action.SetParameter("Actor",player);action.SetParameter("Zone",z);fire.FireEvent(action);
                Assert.IsTrue(action.Handled);Assert.AreEqual(hostile?10:40,player.GetStatValue("Hitpoints"));Assert.AreEqual(hostile?0:RestSystem.RestClockTurns,tm.TickCount);
            }
            finally{FactionManager.Reset();typeof(TurnManager).GetProperty("Active").SetValue(null,previous);}
        }
        [Test] public void ThirtyTwoPlansKeepAllBaseOpenGroundConnected()
        {
            var f=GrovelandsCompositionTests.Factory();foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(LastCounterCompositionTests.AllOpenCellsReached(z,LastCounterCompositionTests.FloodAllCells(z)),"seed "+seed);}
        }
        [Test] public void ActualPipelineUsesLiveCaveFilterAndReservesEmittedStairs()
        {
            var f=GrovelandsCompositionTests.Factory();int stairs=0;
            foreach(int seed in new[]{1,64,1729,729490642})
            {
                var manager=new OverworldZoneManager(f,seed);var pipeline=CinderholdCompositionTests.Pipeline(manager,LastCounterCompositionTests.Id);
                var terrain=pipeline.Builders.OfType<LastCounterCompositionBuilder>().Single();var cave=pipeline.Builders.OfType<CaveEntranceBuilder>().Single();Assert.NotNull(cave.PlacementFilter);
                var z=manager.GetZone(LastCounterCompositionTests.Id);
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {stairs++;var c=z.GetEntityCell(e);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));Assert.IsFalse(c.BlocksMovement());Assert.Less(c.X,62);}
            }
            Assert.Greater(stairs,0);
        }
        [TestCase(POIType.Village,"PruningPost")] [TestCase(POIType.Village,null)]
        [TestCase(POIType.Lair,"ConcordPost")] [TestCase(POIType.Village,"concordpost")]
        public void CurrentMapPlaceTypeAndProfileControlFiniteRouting(POIType type,string profile)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,LastCounterCompositionTests.Id).Builders.OfType<LastCounterCompositionBuilder>().Any());
            m.WorldMap.SetPOI(18,18,new PointOfInterest(type,"Last Counter",profile:profile));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,LastCounterCompositionTests.Id).Builders.OfType<LastCounterCompositionBuilder>().Any());
        }
        [Test] public void RenamingCannotRelocateTheOccupiedProfileToItsAbandonedNeighbor()
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);m.WorldMap.GetPOI(18,18).Name="Former guarantee";
            Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,LastCounterCompositionTests.Id).Builders.OfType<LastCounterCompositionBuilder>().Any());
            m.WorldMap.SetPOI(19,18,new PointOfInterest(POIType.Village,"Last Counter",profile:"ConcordPost"));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.19.18.0").Builders.OfType<LastCounterCompositionBuilder>().Any());
        }
        [TestCase(false)] [TestCase(true)]
        public void CanonicalEnvoyAppearanceIsValidatedBeforeBothPublicationStages(bool late)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);if(late)Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var before=z.GetAllEntities().ToArray();f.Blueprints["SaccharineEnvoy"].Parts["Render"]["RenderString"]="?";
            Assert.IsFalse(late?new LastCounterProfileBuilder(b).BuildZone(z,f,new Random(1)):b.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
            f.Blueprints["SaccharineEnvoy"].Parts["Render"]["RenderString"]="@";if(!late)Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new LastCounterProfileBuilder(b).BuildZone(z,f,new Random(1)));
        }
        [Test] public void RejectedForeignReuseLeavesOriginalPlanUsableAndClearedRetryGetsFreshOwners()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var plan=b.Plan;
            Assert.IsFalse(b.BuildZone(new Zone(z.ZoneID),f,new Random(1)));Assert.AreSame(plan,b.Plan);Assert.IsTrue(new LastCounterProfileBuilder(b).BuildZone(z,f,new Random(1)));
            var old=z.GetAllEntities().ToArray();Assert.IsFalse(b.BuildZone(z,f,new Random(1)));foreach(var e in old)z.RemoveEntity(e);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new LastCounterProfileBuilder(b).BuildZone(z,f,new Random(1)));Assert.IsFalse(z.GetAllEntities().Any(old.Contains));
        }
        [TestCase("HealingTonic")] [TestCase("CandyHeartRoot")]
        public void MissingRealSupplyCannotSilentlyLeaveAnEmptyPost(string blueprint)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(blueprint));var z=new Zone(LastCounterCompositionTests.Id);
            Assert.IsFalse(new LastCounterCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);
        }
        [Test] public void OwnerScopedDiagnosticsExposeSuccessAndRefusal()
        {
            CavesOfOoo.Diagnostics.Diag.ResetAll();var f=GrovelandsCompositionTests.Factory();var z=new Zone(LastCounterCompositionTests.Id);var b=new LastCounterCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsFalse(b.BuildZone(z,f,new Random(1)));
            var late=new LastCounterProfileBuilder(b);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));Assert.IsFalse(late.BuildZone(z,f,new Random(1)));
            var arrival=new LastCounterArrivalReservationBuilder(b);Assert.IsTrue(arrival.BuildZone(z,f,new Random(1)));Assert.IsFalse(arrival.BuildZone(new Zone(z.ZoneID),f,new Random(1)));
            foreach(var name in new[]{"LastCounterCompositionPlanned","LastCounterCompositionRejected","LastCounterProfilePlaced","LastCounterProfileRejected","LastCounterArrivalsReserved","LastCounterArrivalsRejected"})
                Assert.AreEqual(1,CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter{Category="worldgen",Kind=name,Limit=10}).Records.Count,name);
        }
        [Test] public void InstalledPipelineCaveDelegateRejectsLiveWetNeighborWithDryCentre()
        {
            var f=GrovelandsCompositionTests.Factory();var manager=new OverworldZoneManager(f,64);
            var pipeline=CinderholdCompositionTests.Pipeline(manager,LastCounterCompositionTests.Id);
            var terrain=pipeline.Builders.OfType<LastCounterCompositionBuilder>().Single();var cave=pipeline.Builders.OfType<CaveEntranceBuilder>().Single();
            var z=new Zone(LastCounterCompositionTests.Id);Assert.IsTrue(terrain.BuildZone(z,f,new Random(1)));Cell c=null;
            for(int y=1;y<24&&c==null;y++)for(int x=1;x<61&&c==null;x++)if(terrain.CanPlaceCaveEntrance(z,z.GetCell(x,y)))c=z.GetCell(x,y);
            Assert.NotNull(c);Assert.NotNull(cave.PlacementFilter);Assert.IsTrue(cave.PlacementFilter(z,c));
            var water=f.CreateEntity("WaterPuddle");z.AddEntity(water,c.X+1,c.Y);
            Assert.IsFalse(c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsFalse(c.BlocksMovement());
            Assert.IsFalse(cave.PlacementFilter(z,c),"Installed delegate must inspect the live arrival neighborhood, not only its empty centre.");
            z.RemoveEntity(water);Assert.IsTrue(cave.PlacementFilter(z,c));
        }
        [TestCase(false)] [TestCase(true)]
        public void RealBoundaryCampfireIsReachableAndUsableUnlessItsStandingCellsAreBlocked(bool blocked)
        {
            var previous=TurnManager.Active;var tm=new TurnManager();FactionManager.Initialize();
            try
            {
                var f=GrovelandsCompositionTests.Factory();var z=new OverworldZoneManager(f,64).GetZone(LastCounterCompositionTests.Id);
                var fire=z.GetCell(17,24).Objects.Single(e=>e.BlueprintName=="Campfire");
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
                Assert.IsTrue(z.GetCell(17,23).BlocksMovement());
                if(blocked){z.AddEntity(f.CreateEntity("SandstoneWall"),16,24);z.AddEntity(f.CreateEntity("SandstoneWall"),18,24);}
                var seen=LastCounterCompositionTests.FloodAllCells(z);
                Assert.AreEqual(!blocked,CinderholdCompositionTests.Neighbors(17,24).Any(n=>z.InBounds(n.x,n.y)&&seen[n.x,n.y]));
                var player=CinderholdCompositionTests.Player();player.GetStat("Hitpoints").BaseValue=10;Assert.IsTrue(z.AddEntity(player,0,12));
                var q=new System.Collections.Generic.Queue<(int x,int y)>();var previousCell=new System.Collections.Generic.Dictionary<(int,int),(int,int)>();q.Enqueue((0,12));previousCell[(0,12)]=(0,12);
                while(q.Count>0&&!previousCell.ContainsKey((16,24)))
                {var c=q.Dequeue();foreach(var n in CinderholdCompositionTests.Neighbors(c.x,c.y))if(z.InBounds(n.x,n.y)&&!previousCell.ContainsKey(n)&&!z.GetCell(n.x,n.y).BlocksMovement()){previousCell[n]=c;q.Enqueue(n);}}
                Assert.AreEqual(!blocked,previousCell.ContainsKey((16,24)));
                if(blocked){Assert.AreEqual(10,player.GetStatValue("Hitpoints"));Assert.AreEqual(0,tm.TickCount);return;}
                var path=new System.Collections.Generic.List<(int x,int y)>();for(var c=(16,24);c!=(0,12);c=previousCell[c])path.Add(c);path.Reverse();
                foreach(var step in path){var at=z.GetEntityPosition(player);Assert.IsTrue(MovementSystem.TryMoveEx(player,z,step.x-at.x,step.y-at.y).moved,"Actual native move to "+step);}
                Assert.AreEqual((16,24),z.GetEntityPosition(player));
                Assert.IsTrue(WorldInteractionSystem.GatherActions(fire,player).Any(a=>a.Command=="RestAtCampfire"));
                var action=new GameEvent("InventoryAction");action.SetParameter("Command","RestAtCampfire");action.SetParameter("Actor",player);action.SetParameter("Zone",z);fire.FireEvent(action);
                Assert.IsTrue(action.Handled);Assert.AreEqual(40,player.GetStatValue("Hitpoints"));Assert.AreEqual(RestSystem.RestClockTurns,tm.TickCount);
            }
            finally{FactionManager.Reset();typeof(TurnManager).GetProperty("Active").SetValue(null,previous);}
        }
    }
}
