using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Hypotheses about interrupted generation, malformed inherited
    /// content, real cave arrivals, later services, and the limits of hospitality.</summary>
    public class GantryCompositionAdversarialTests
    {
        private const string Id=GantryCompositionTests.Id;
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeLoot()=>LootTableRegistry.ResetForTests();
        [TestCase("Grass")] [TestCase("StoneFloor")] [TestCase("RoadStone")] [TestCase("GantryTimberWall")]
        [TestCase("Bed")] [TestCase("Chair")] [TestCase("Crate")] [TestCase("Bush")] [TestCase("Rock")]
        [TestCase("TentRightHost")] [TestCase("GantryRegistrar")] [TestCase("GuestClothPole")] [TestCase("GantryRegistryDesk")] [TestCase("GantryExchangeCounter")] [TestCase("GantryWayboard")]
        public void MissingEarlyOrLateDependencyCannotLeaveAnUnfinishedCrossroads(string bp)
        {
            var f=GantryCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(Id);z.GenReservedCells.Add((79,24));var b=new GantryCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);Assert.IsNull(b.RealizedZone);Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }
        [TestCase("Grass","Render","RenderString","?")] [TestCase("StoneFloor","Physics","Takeable","true")]
        [TestCase("RoadStone","Physics","Solid","true")] [TestCase("GantryTimberWall","Destructible","HP","0")]
        [TestCase("GantryTimberWall","Material",null,null)] [TestCase("GantryTimberWall","Thermal",null,null)]
        [TestCase("Bed","Bed",null,null)] [TestCase("Chair","Chair",null,null)] [TestCase("Crate","Container",null,null)]
        [TestCase("GantryExchangeCounter","Container",null,null)] [TestCase("Rock","Material",null,null)]
        [TestCase("TentRightHost","Conversation","ConversationID","Merchant_1")]
        [TestCase("TentRightHost","Render","RenderString","?")] [TestCase("TentRightHost","AISelfPreservation",null,null)]
        [TestCase("GantryRegistrar","Conversation","ConversationID","FilerClerk_1")]
        [TestCase("GantryRegistryDesk","Destructible","Indestructible","true")] [TestCase("GuestClothPole","Physics","Solid","true")]
        [TestCase("GantryWayboard","Destructible",null,null)]
        public void FailSoftPartFieldsCannotPublishVisibleButMechanicallyFalseOwners(string bp,string part,string key,string value)
        {
            var f=GantryCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(Id);z.GenReservedCells.Add((79,24));Assert.IsFalse(new GantryCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase(-1,0)] [TestCase(80,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void InvalidCoordinatesNeverClampToWorkingOrShadedGround(int x,int y)
        {var p=GantryCompositionPlan.Create(Id,64);Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsReserved(x,y));}
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingZoneFactoryOrRandomCannotMutateFlags(int missing)
        {var z=new Zone(Id);var b=new GantryCompositionBuilder(64);Assert.IsFalse(b.BuildZone(missing==0?null:z,missing==1?null:GantryCompositionTests.Factory(),missing==2?null:new Random(1)));Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);}
        [TestCase(false)] [TestCase(true)]
        public void RejectedReuseCannotInvalidateAnAlreadySuccessfulOwnersPendingProfile(bool foreign)
        {
            var f=GantryCompositionTests.Factory();var z=new Zone(Id);var b=new GantryCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;var other=foreign?new Zone(Id):z;
            Assert.IsFalse(b.BuildZone(other,f,new Random(2)));Assert.AreSame(p,b.Plan);Assert.AreSame(z,b.RealizedZone);
            if(foreign){Assert.IsFalse(new GantryProfileBuilder(b).BuildZone(other,f,new Random(1)));Assert.IsFalse(new GantryArrivalReservationBuilder(b).BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);}
            Assert.IsTrue(new GantryProfileBuilder(b).BuildZone(z,f,new Random(3)));
        }
        [Test] public void RemovingAHostNeverResurrectsItButAnExplicitEmptyRetryGetsFreshOwners()
        {
            var f=GantryCompositionTests.Factory();var z=new Zone(Id);var b=new GantryCompositionBuilder(64);var late=new GantryProfileBuilder(b);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var original=z.GetAllEntities().ToArray();z.RemoveEntity(original.First(e=>e.BlueprintName=="TentRightHost"));var survivors=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(survivors,z.GetAllEntities());
            foreach(var e in survivors)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(3)));Assert.IsTrue(late.BuildZone(z,f,new Random(3)));Assert.IsFalse(z.GetAllEntities().Any(original.Contains));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="TentRightHost"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LastOwnerFailureCannotPublishPartialPublicServices(bool malformed)
        {
            var f=GantryCompositionTests.Factory();var z=new Zone(Id);var b=new GantryCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var last=b.Plan.Profile.Last();
            if(malformed)f.Blueprints[last.Blueprint].Parts["Physics"]["Takeable"]="true";else Assert.IsTrue(z.AddEntity(f.CreateEntity("SandstoneWall"),last.X,last.Y));var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(new GantryProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(POIType.Village,"TentCamp")] [TestCase(POIType.Village,null)] [TestCase(POIType.Lair,"CrossroadsExchange")] [TestCase(POIType.Village,"crossroadsexchange")]
        public void ActualMapProfileAndPlaceTypeVetoTheComposition(POIType type,string profile)
        {
            var m=new OverworldZoneManager(GantryCompositionTests.Factory(),64);Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<GantryCompositionBuilder>().Any());
            m.WorldMap.SetPOI(7,8,new PointOfInterest(type,"the Gantry",profile:profile));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<GantryCompositionBuilder>().Any());
        }
        [Test] public void RenamingThePlaceCannotRemoveItsProfileOrSpreadItToAnotherAddress()
        {
            var m=new OverworldZoneManager(GantryCompositionTests.Factory(),64);m.WorldMap.GetPOI(7,8).Name="the mortal promise";Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<GantryCompositionBuilder>().Any());
            m.WorldMap.SetPOI(8,8,new PointOfInterest(POIType.Village,"the Gantry",profile:"CrossroadsExchange"));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.8.8.0").Builders.OfType<GantryCompositionBuilder>().Any());
        }
        [Test] public void ThirtyTwoSeedsAndExtremeSeedsKeepEveryOpenBaseCellReachable()
        {
            var f=GantryCompositionTests.Factory();foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var z=new Zone(Id);var b=new GantryCompositionBuilder(seed);Assert.DoesNotThrow(()=>Assert.IsTrue(b.BuildZone(z,f,new Random(1))),"native base seed "+seed);var seen=DrownedLedgerCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(seen);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(seen.Contains((x,y)),"seed "+seed+" pocket "+x+","+y);
            }
        }
        [TestCase("WaterPuddle")] [TestCase("SandstoneWall")]
        public void NativeCaveGateTestsLiveNeighborsNotJustAnEmptyCandidateCentre(string obstacle)
        {
            var f=GantryCompositionTests.Factory();var z=new Zone(Id);var b=new GantryCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var at=Candidate(b,z);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
            Assert.IsFalse(b.CanPlaceCaveEntrance(new Zone(Id),z.GetCell(at.x,at.y)));Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(Id).GetCell(at.x,at.y)));
            var blocked=f.CreateEntity(obstacle);Assert.IsTrue(z.AddEntity(blocked,at.x+1,at.y));Assert.IsFalse(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));z.RemoveEntity(blocked);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
        }
        public static (int x,int y) Candidate(GantryCompositionBuilder b,Zone z)
        {for(int y=1;y<24;y++)for(int x=1;x<79;x++)if(b.CanPlaceCaveEntrance(z,z.GetCell(x,y)))return(x,y);Assert.Fail("A native cave roll must retain eligible ground.");return(-1,-1);}
        [Test] public void ActualCaveRollsAvoidCourtAndSheltersAndReserveRealDryArrivals()
        {
            int stairs=0;var f=GantryCompositionTests.Factory();foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(Id);var p=GantryCompositionPlan.Create(Id,seed);
                foreach(var owner in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    stairs++;var c=z.GetEntityCell(owner);Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(p.IsReserved(c.X,c.Y));Assert.IsFalse(p.IsInterior(c.X,c.Y));Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {var n=z.GetCell(c.X+dx,c.Y+dy);Assert.NotNull(n);Assert.IsFalse(n.IsInterior);Assert.IsFalse(n.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((n.X,n.Y)));Assert.IsFalse(n.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));}
                }
            }Assert.Greater(stairs,0,"Exercise actual successful native rolls, not an empty loop.");
        }
        [Test] public void FailedArrivalOwnershipReportsRejectionAndKeepsAllOwnersUntouched()
        {
            Diag.ResetAll();Diag.SetChannel("worldgen",true);try
            {
                var f=GantryCompositionTests.Factory();var z=new Zone(Id);var b=new GantryCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var arrivals=new GantryArrivalReservationBuilder(b);Assert.AreEqual(3870,arrivals.Priority);var before=z.GetAllEntities().ToArray();
                Assert.IsFalse(arrivals.BuildZone(new Zone(Id),f,new Random(1)));Assert.IsTrue(arrivals.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="GantryArrivalsRejected",Limit=10}).Records.Count);
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="GantryArrivalsReserved",Limit=10}).Records.Count);
            }finally{Diag.ResetAll();}
        }
    }
}
