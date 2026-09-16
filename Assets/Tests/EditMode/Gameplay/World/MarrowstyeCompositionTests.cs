using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class MarrowstyeCompositionTests
    {
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ClearNativeLoot()=>LootTableRegistry.ResetForTests();

        public const string Id="Overworld.12.12.0";
        [Test] public void PlanIsExactDeterministicAndChangesFunctionalArchitectureAcrossSeeds()
        {
            Assert.IsTrue(MarrowstyeCompositionPlan.IsSupportedZone(Id));Assert.AreEqual(MarrowstyeCompositionPlan.Create(Id,64).Signature(),MarrowstyeCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(MarrowstyeCompositionPlan.Create(Id,64).Signature(),MarrowstyeCompositionPlan.Create(Id,1729).Signature());
            Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(12,12));Assert.IsTrue(WorldMapAuthoring.IsRoad(12,12));Assert.IsFalse(WorldMapAuthoring.IsRiver(12,12));
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.12.12.1")] [TestCase("Overworld.12.012.0")]
        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.15.6.0")]
        public void ForeignSitesAndDepthsCannotUseIntakePlan(string id)
        {Assert.IsFalse(MarrowstyeCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>MarrowstyeCompositionPlan.Create(id,64));}
        [TestCase(1)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void IntakeHasFourDistinctWingsAndDryAccessibleHaulAisles(int seed)
        {
            var z=new Zone(Id);var b=new MarrowstyeCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));var p=b.Plan;
            CollectionAssert.IsSubsetOf(new[]{"IntakeHall","SupplyWing","DomesticWing","DisusedWing"},p.Rooms.Select(r=>r.Role));
            Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Width*r.Height).Distinct().Count(),3);
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="SaltCuredBody"));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));
            var reached=FormationReachability.FloodFromWest(z);
            foreach(var r in p.Rooms){Assert.IsTrue(reached[r.DoorX,r.DoorY]);Assert.IsTrue(p.IsApproach(r.DoorX,r.DoorY));}
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                Assert.AreEqual(1,z.GetCell(x,y).Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                if(p.IsApproach(x,y)){Assert.IsFalse(z.GetCell(x,y).BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                if(p.IsInterior(x,y))Assert.AreEqual("StoneFloor",p.GroundAt(x,y));
            }
            Assert.IsTrue(FormationReachability.FullyReached(z,reached));Assert.IsFalse(z.GetCell(40,12).BlocksMovement());Assert.IsFalse(p.IsInterior(40,12));
        }
        [Test] public void LateNativeIntakeHasTwoCoffersOneClerkAndNoInventedStorageOrCuringParts()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(Id);var b=new MarrowstyeCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new MarrowstyeProfileBuilder(b);
            Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="StoneCoffer"));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="FilerClerk"));
            foreach(var e in z.GetAllEntities().Where(e=>e.BlueprintName=="StoneCoffer"||e.BlueprintName=="SaltCuredBody"))
            {Assert.IsNull(e.GetPart<ContainerPart>());Assert.IsNull(e.GetPart<DestructiblePart>());Assert.IsFalse(HandlingService.IsCarryable(e));Assert.IsFalse(HandlingService.IsThrowable(e));}
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="PaleCurator"));var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void ActualManagerPreservesIntakeAndGenericServiceFrontagesAfterPopulation(int seed)
        {
            var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(Id);var p=MarrowstyeCompositionPlan.Create(Id,seed);
            foreach(var bp in new[]{"FilerClerk","Merchant","Quartermaster","Elder"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="StoneCoffer"));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="SaltCuredBody"));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="Well"));
            var services=z.GetAllEntities().Where(e=>new[]{"FilerClerk","Merchant","Quartermaster","Well","Shrine","StoneCoffer","SaltCuredBody"}.Contains(e.BlueprintName)).Select(e=>z.GetEntityPosition(e)).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
            var reach=FormationReachability.FloodFromWest(z);foreach(var room in p.Rooms)Assert.IsTrue(reach[room.DoorX,room.DoorY]);
            Assert.IsTrue(FormationReachability.FullyReached(z,reach));
            foreach(var c in services)Assert.IsTrue(new[]{(-1,0),(1,0),(0,-1),(0,1)}.Any(d=>z.InBounds(c.x+d.Item1,c.y+d.Item2)&&reach[c.x+d.Item1,c.y+d.Item2]),"Service frontage "+c);
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void IntakePartitionsCreateThreeBaysWithoutPinchingItsThreeCellHaulAisle(int seed)
        {
            var p=MarrowstyeCompositionPlan.Create(Id,seed);var h=p.Rooms.Single(r=>r.Role=="IntakeHall");
            var walls=new System.Collections.Generic.HashSet<(int x,int y)>();int crates=0;
            for(int y=h.Y+1;y<h.Y+h.Height-1;y++)for(int x=h.X+1;x<h.X+h.Width-1;x++)
            {if(p.ObjectAt(x,y)=="SandstoneWall")walls.Add((x,y));if(p.ObjectAt(x,y)=="Crate")crates++;}
            Assert.GreaterOrEqual(walls.Count,6);Assert.AreEqual(2,crates,"Clerk-side supply belongs to real contextual owners.");
            int groups=0;while(walls.Count>0)
            {
                groups++;var q=new System.Collections.Generic.Queue<(int x,int y)>();var start=walls.First();walls.Remove(start);q.Enqueue(start);
                while(q.Count>0){var c=q.Dequeue();foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)}){var n=(c.x+d.Item1,c.y+d.Item2);if(walls.Remove(n))q.Enqueue(n);}}
            }
            Assert.AreEqual(2,groups,"Two coherent partition fingers make three work bays.");
            for(int y=h.Y+h.Height-4;y<h.Y+h.Height-1;y++)for(int x=h.X+2;x<h.X+h.Width-2;x++)
            {Assert.IsNull(p.ObjectAt(x,y),"Haul aisle "+x+","+y);Assert.IsTrue(p.IsApproach(x,y));}
        }
        [Test] public void MappedRoadAndReceivingCourtUseActualRoadStoneOutsideShade()
        {
            var p=MarrowstyeCompositionPlan.Create(Id,64);int road=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                if(!p.IsInterior(x,y)&&p.IsApproach(x,y)){Assert.AreEqual("RoadStone",p.GroundAt(x,y));road++;}
                if(p.IsInterior(x,y))Assert.AreEqual("StoneFloor",p.GroundAt(x,y));
            }
            Assert.That(road,Is.InRange(150,400));
        }
        [Test] public void DisusedWingHasRealRubbleBreachAndSparseGroupedBorderVegetation()
        {
            var p=MarrowstyeCompositionPlan.Create(Id,64);var r=p.Rooms.Single(a=>a.Role=="DisusedWing");
            Assert.AreEqual("Rubble",p.ObjectAt(r.X,r.Y+2));Assert.AreEqual("Rubble",p.ObjectAt(r.X,r.Y+3));
            int trees=0,bushes=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                string bp=p.ObjectAt(x,y);if(bp!="Tree"&&bp!="Bush")continue;
                if(bp=="Tree")trees++;else bushes++;
                Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsTrue(x<=7||x>=72,"Vegetation frames the site edges.");
                if(bp=="Tree")Assert.IsTrue(Enumerable.Range(-3,7).Any(dx=>Enumerable.Range(-3,7).Any(dy=>p.ObjectAt(x+dx,y+dy)=="Bush")),"Each tree belongs to a colony.");
            }
            Assert.That(trees,Is.InRange(4,6));Assert.That(bushes,Is.InRange(12,20));
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void DisusedWingStartsQuietInsteadOfReceivingTheVillagePopulation(int seed)
        {
            var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(Id);var p=MarrowstyeCompositionPlan.Create(Id,seed);var r=p.Rooms.Single(a=>a.Role=="DisusedWing");
            for(int y=r.Y+1;y<r.Y+r.Height-1;y++)for(int x=r.X+1;x<r.X+r.Width-1;x++)
            {Assert.IsTrue(p.IsReserved(x,y),"Disused interior must be excluded from initial service/drama placement.");Assert.IsFalse(z.GetCell(x,y).Objects.Any(e=>e.HasPart<BrainPart>()),"Disused wing seeded a resident.");}
        }
    }
}
