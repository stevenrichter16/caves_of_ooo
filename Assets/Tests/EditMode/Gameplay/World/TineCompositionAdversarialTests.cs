using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class TineCompositionAdversarialTests
    {
        private const string Id=TineCompositionTests.Id;
        [SetUp] public void LoadLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetLoot()=>LootTableRegistry.ResetForTests();
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("RoadStone")] [TestCase("SandstoneWall")]
        [TestCase("WaterPuddle")] [TestCase("Duckboard")] [TestCase("Reeds")] [TestCase("Tree")] [TestCase("Bush")]
        [TestCase("Bed")] [TestCase("Chair")] [TestCase("Crate")] [TestCase("BoatFrame")]
        public void MissingEarlyOrLateDependencyCannotLeaveAPartialShore(string bp)
        {
            var f=TineCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(Id);z.GenReservedCells.Add((79,24));var b=new TineCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);Assert.IsNull(b.RealizedZone);Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }
        [TestCase("Floor","Render","RenderString","?")] [TestCase("StoneFloor","Physics","Takeable","true")]
        [TestCase("RoadStone","Physics","Solid","true")] [TestCase("SandstoneWall","Destructible","HP","0")]
        [TestCase("WaterPuddle","LiquidPool","LiquidId","oil")] [TestCase("WaterPuddle","LiquidPool","Volume","0")]
        [TestCase("WaterPuddle","Thermal",null,null)] [TestCase("Duckboard","Material",null,null)]
        [TestCase("BoatFrame","Physics","Takeable","true")] [TestCase("Reeds","Physics","Solid","true")]
        [TestCase("Tree","Destructible",null,null)] [TestCase("Bush","Thermal",null,null)]
        [TestCase("Bed","Bed",null,null)] [TestCase("Chair","Chair",null,null)] [TestCase("Crate","Container",null,null)]
        public void FailSoftContentFieldsCannotPublishFalseNativeOwners(string bp,string part,string key,string value)
        {
            var f=TineCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(Id);Assert.IsFalse(new TineCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.IsEmpty(z.GenReservedCells);
        }
        [TestCase(-1,0)] [TestCase(80,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void InvalidCoordinatesNeverClampToWetOrInteriorGround(int x,int y)
        {var p=TineCompositionPlan.Create(Id,64);Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsReserved(x,y));Assert.IsFalse(p.IsWet(x,y));}
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingZoneFactoryOrRandomCannotMutateFlags(int missing)
        {var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsFalse(b.BuildZone(missing==0?null:z,missing==1?null:TineCompositionTests.Factory(),missing==2?null:new Random(1)));Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);}
        [TestCase(false)] [TestCase(true)]
        public void RejectedReuseCannotInvalidateAnAlreadySuccessfulOwnersPendingProfile(bool foreign)
        {
            var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;var other=foreign?new Zone(Id):z;
            Assert.IsFalse(b.BuildZone(other,f,new Random(2)));Assert.AreSame(p,b.Plan);Assert.AreSame(z,b.RealizedZone);
            if(foreign){Assert.IsFalse(new TineProfileBuilder(b).BuildZone(other,f,new Random(1)));Assert.IsFalse(new TineArrivalReservationBuilder(b).BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);}
            Assert.IsTrue(new TineProfileBuilder(b).BuildZone(z,f,new Random(3)));
        }
        [Test] public void RemovingAHostNeverResurrectsItButAnExplicitEmptyRetryGetsFreshOwners()
        {
            var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);var late=new TineProfileBuilder(b);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var original=z.GetAllEntities().ToArray();z.RemoveEntity(original.First(e=>e.BlueprintName=="BoatFrame"));var survivors=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(survivors,z.GetAllEntities());
            foreach(var e in survivors)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(3)));Assert.IsTrue(late.BuildZone(z,f,new Random(3)));Assert.IsFalse(z.GetAllEntities().Any(original.Contains));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="BoatFrame"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LastOwnerFailureCannotPublishHalfTheWorkYard(bool malformed)
        {
            var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var last=b.Plan.Profile.Last();
            if(malformed)f.Blueprints[last.Blueprint].Parts["Physics"]["Takeable"]="true";else Assert.IsTrue(z.AddEntity(f.CreateEntity("SandstoneWall"),last.X,last.Y));var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(new TineProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(POIType.Village,"Boatyard")] [TestCase(POIType.Village,null)] [TestCase(POIType.Lair,"LakesideVillage")] [TestCase(POIType.Village,"lakesidevillage")]
        public void ActualMapProfileAndPlaceTypeVetoTheComposition(POIType type,string profile)
        {
            var m=new OverworldZoneManager(TineCompositionTests.Factory(),64);Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<TineCompositionBuilder>().Any());
            m.WorldMap.SetPOI(13,7,new PointOfInterest(type,"Tine",profile:profile));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<TineCompositionBuilder>().Any());
        }
        [Test] public void RenamingThePlaceCannotRemoveItsProfileOrSpreadItToAnotherAddress()
        {
            var m=new OverworldZoneManager(TineCompositionTests.Factory(),64);m.WorldMap.GetPOI(13,7).Name="the reed shore";Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<TineCompositionBuilder>().Any());
            m.WorldMap.SetPOI(14,7,new PointOfInterest(POIType.Village,"Tine",profile:"LakesideVillage"));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.14.7.0").Builders.OfType<TineCompositionBuilder>().Any());
        }
        [Test] public void ThirtyTwoSeedsAndExtremeSeedsKeepEveryOpenBaseCellReachable()
        {
            var f=TineCompositionTests.Factory();foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var z=new Zone(Id);var b=new TineCompositionBuilder(seed);Assert.DoesNotThrow(()=>Assert.IsTrue(b.BuildZone(z,f,new Random(1))),"native base seed "+seed);var seen=TineCompositionTests.DryReach(z);TineCompositionTests.AssertFourDirections(seen);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement()&&!z.GetCell(x,y).Objects.Any(e=>e.HasPart<LiquidPoolPart>()))Assert.IsTrue(seen.Contains((x,y)),"seed "+seed+" pocket "+x+","+y);
            }
        }
        [TestCase("WaterPuddle")] [TestCase("SandstoneWall")]
        public void NativeCaveGateTestsLiveNeighborsNotJustAnEmptyCandidateCentre(string obstacle)
        {
            var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var at=Candidate(b,z);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
            Assert.IsFalse(b.CanPlaceCaveEntrance(new Zone(Id),z.GetCell(at.x,at.y)));Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(Id).GetCell(at.x,at.y)));
            var blocked=f.CreateEntity(obstacle);Assert.IsTrue(z.AddEntity(blocked,at.x+1,at.y));Assert.IsFalse(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));z.RemoveEntity(blocked);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
        }
        public static (int x,int y) Candidate(TineCompositionBuilder b,Zone z)
        {for(int y=1;y<24;y++)for(int x=1;x<79;x++)if(b.CanPlaceCaveEntrance(z,z.GetCell(x,y)))return(x,y);Assert.Fail("A native cave roll must retain eligible ground.");return(-1,-1);}
        [Test] public void ActualCaveRollsAvoidCourtAndSheltersAndReserveRealDryArrivals()
        {
            int stairs=0;var f=TineCompositionTests.Factory();foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(Id);var p=TineCompositionPlan.Create(Id,seed);
                foreach(var owner in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    stairs++;var c=z.GetEntityCell(owner);Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(p.IsReserved(c.X,c.Y));Assert.IsFalse(p.IsInterior(c.X,c.Y));Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {var n=z.GetCell(c.X+dx,c.Y+dy);Assert.NotNull(n);Assert.IsFalse(n.IsInterior);Assert.IsFalse(n.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((n.X,n.Y)));Assert.IsFalse(n.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));}
                }
            }Assert.Greater(stairs,0,"Exercise actual successful native rolls, not an empty loop.");
        }
        [Test] public void ServiceAnchorsAreActualFreeRetreatFloorsAndNotInitialPopulationReservations()
        {
            foreach(int seed in Enumerable.Range(0,32))
            {
                var p=TineCompositionPlan.Create(Id,seed);var cells=new System.Collections.Generic.HashSet<(int,int)>();
                foreach(var bp in new[]{"Scribe","Merchant","Quartermaster","Innkeeper"})
                {
                    Assert.IsTrue(p.TryGetServiceCell(bp,out int x,out int y));Assert.IsTrue(cells.Add((x,y)));Assert.IsTrue(p.IsInterior(x,y));Assert.AreEqual("StoneFloor",p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsReserved(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.Profile.Any(o=>o.X==x&&o.Y==y));
                    Assert.IsTrue(CinderholdCompositionTests.Neighbors(x,y).Any(n=>p.IsReserved(n.x,n.y)&&p.ObjectAt(n.x,n.y)==null&&p.IsInterior(n.x,n.y)),"Protect an adjacent standing cell from later generic population.");
                }
                foreach(var bp in new[]{null,"","scribe","FishingMaster"}){Assert.IsFalse(p.TryGetServiceCell(bp,out int x,out int y));Assert.AreEqual(-1,x);Assert.AreEqual(-1,y);}
            }
        }
        [Test] public void NativeWaterProjectionDisappearsWithItsLastOwnerWhileDryBoardsHaveNone()
        {
            var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var pool=z.GetAllEntities().First(e=>e.BlueprintName=="WaterPuddle");var c=z.GetEntityCell(pool);var state=z.TileState.Get(c.X,c.Y);Assert.IsTrue(state.Coatings.Any(o=>o.Id=="water"&&o.Turns==ZoneTileState.Permanent));
            z.RemoveEntity(pool);state=z.TileState.Get(c.X,c.Y);Assert.IsFalse(state!=null&&state.Coatings.Any(o=>o.Id=="water"));Assert.IsTrue(c.Objects.Any(e=>e.BlueprintName==b.Plan.GroundAt(c.X,c.Y)));
            foreach(var board in z.GetAllEntities().Where(e=>e.BlueprintName=="Duckboard"))
            {var at=z.GetEntityCell(board);var dry=z.TileState.Get(at.X,at.Y);Assert.IsFalse(dry!=null&&dry.Coatings.Any(o=>o.Id=="water"));}
        }
        [Test] public void ActualNativeWaterContactCoatsThePlayerWhileDryBoardsDoNot()
        {
            LiquidRegistry.InitializeFromJsonSources(UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>("Content/Data/LiquidDefinitions").Select(a=>a.text));
            try
            {
                var f=GrovelandsCompositionTests.Factory();var z=new Zone(TineCompositionTests.Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                foreach(var bp in new[]{"WaterPuddle","Duckboard"})
                {
                    var target=z.GetAllEntities().Where(e=>e.BlueprintName==bp).Select(e=>z.GetEntityCell(e)).First(c=>
                        z.InBounds(c.X-1,c.Y)&&!b.Plan.IsWet(c.X-1,c.Y)&&!z.GetCell(c.X-1,c.Y).BlocksMovement());
                    var player=new Entity{BlueprintName="Player"};player.SetTag("Creature");player.SetTag("Player");player.AddPart(new PhysicsPart{Solid=false});player.AddPart(new StatusEffectsPart());
                    player.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=16};player.Statistics["Toughness"]=new Stat{Name="Toughness",BaseValue=14};
                    z.AddEntity(player,target.X-1,target.Y);Assert.IsTrue(MovementSystem.TryMove(player,z,1,0));
                    Assert.AreEqual(bp=="WaterPuddle",player.HasEffect<WetEffect>());Assert.AreEqual(bp=="WaterPuddle",player.HasEffect<LiquidCoveredEffect>());
                    Assert.IsFalse(target.Objects.Any(e=>e.HasPart<TileStateSourcePart>()),"No fictional renewal source.");
                    var state=z.TileState.Get(target.X,target.Y);
                    Assert.AreEqual(bp=="WaterPuddle",state!=null&&state.Coatings.Any(c=>c.Id=="water"&&c.Turns==ZoneTileState.Permanent),"Native Zone.ProjectPool mirrors only the live owner.");
                    if(bp=="WaterPuddle")
                    {
                        var pool=target.Objects.Single(e=>e.BlueprintName=="WaterPuddle");z.RemoveEntity(pool);
                        var after=z.TileState.Get(target.X,target.Y);Assert.IsFalse(after!=null&&after.Coatings.Any(c=>c.Id=="water"));
                        Assert.IsTrue(target.Objects.Any(e=>e.BlueprintName==b.Plan.GroundAt(target.X,target.Y)));
                    }
                    z.RemoveEntity(player);
                }
            }
            finally{LiquidRegistry.ResetForTests();}
        }
        [TestCase("Duckboard")] [TestCase("SandstoneWall")] [TestCase("Tree")]
        public void DestroyingNativeMaterialPreservesGroundDrynessAndDistantWorkFrames(string bp)
        {
            var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new TineProfileBuilder(b).BuildZone(z,f,new Random(1)));
            var e=z.GetAllEntities().First(o=>o.BlueprintName==bp);var c=z.GetEntityCell(e);var frames=z.GetAllEntities().Where(o=>o.BlueprintName=="BoatFrame").ToArray();var ground=c.Objects.Single(o=>o.BlueprintName==b.Plan.GroundAt(c.X,c.Y));
            Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(e,1000,null,z));Assert.IsFalse(c.Objects.Contains(e));Assert.IsTrue(c.Objects.Contains(ground));Assert.IsFalse(c.Objects.Any(o=>o.HasPart<LiquidPoolPart>()));CollectionAssert.AreEquivalent(frames,z.GetAllEntities().Where(o=>o.BlueprintName=="BoatFrame"));
            Assert.IsFalse(new TineProfileBuilder(b).BuildZone(z,f,new Random(2)));
        }
        [Test] public void RejectedAndSuccessfulArrivalsReportTheirActualGateWithoutMutatingOwners()
        {
            Diag.ResetAll();Diag.SetChannel("worldgen",true);try
            {
                var f=TineCompositionTests.Factory();var z=new Zone(Id);var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var arrivals=new TineArrivalReservationBuilder(b);var before=z.GetAllEntities().ToArray();
                Assert.IsFalse(arrivals.BuildZone(new Zone(Id),f,new Random(1)));Assert.IsTrue(arrivals.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="TineArrivalsRejected",Limit=10}).Records.Count);Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="TineArrivalsReserved",Limit=10}).Records.Count);
            }finally{Diag.ResetAll();}
        }
    }
}
