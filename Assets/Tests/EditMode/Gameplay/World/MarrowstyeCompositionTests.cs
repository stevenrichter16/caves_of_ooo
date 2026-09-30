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
            var factory=GrovelandsCompositionTests.Factory();var z=new OverworldZoneManager(factory,seed).GetZone(Id);var p=MarrowstyeCompositionPlan.Create(Id,seed);
            foreach(var bp in new[]{"FilerClerk","Merchant","Quartermaster","Elder"})
            {
                var owners=z.GetAllEntities().Where(e=>e.BlueprintName==bp).ToArray();
                Assert.AreEqual(1,owners.Count(e=>!e.HasPart<HouseDramaPart>()),bp+" exact generic service");
                var extra=owners.Where(e=>e.HasPart<HouseDramaPart>()).ToArray();
                CollectionAssert.AllItemsAreUnique(extra.Select(e=>e.GetPart<HouseDramaPart>().DramaID+"/"+e.GetPart<HouseDramaPart>().NpcId));
                foreach(var owner in extra)
                {
                    var mark=owner.GetPart<HouseDramaPart>();Assert.AreSame(owner,mark.ParentEntity);
                    Assert.NotNull(HouseDramaRuntime.GetDrama(mark.DramaID),"Extra service must belong to a registered drama.");
                    var drama=HouseDramaLoader.Get(mark.DramaID);Assert.NotNull(drama);
                    var role=drama.NpcRoles.SingleOrDefault(r=>r.Id==mark.NpcId&&r.Role==mark.NpcRole&&r.Alive);
                    Assert.NotNull(role,"Extra service must be the exact living authored role, not an arbitrary duplicate.");
                    string expected=!string.IsNullOrEmpty(role.BlueprintOverride)&&factory.Blueprints.ContainsKey(role.BlueprintOverride)
                        ?role.BlueprintOverride:role.Role=="NamedAntagonist"?"Merchant":role.Role=="DiminishedHead"?"Elder":null;
                    Assert.AreEqual(bp,expected,"The authored drama role must resolve to this actual blueprint.");
                }
            }
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="StoneCoffer"));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="SaltCuredBody"));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="Well"));
            var services=z.GetAllEntities().Where(e=>new[]{"FilerClerk","Merchant","Quartermaster","Well","Shrine","StoneCoffer","SaltCuredBody"}.Contains(e.BlueprintName)).Select(e=>z.GetEntityPosition(e)).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
            var reach=FormationReachability.FloodFromWest(z);foreach(var room in p.Rooms)Assert.IsTrue(reach[room.DoorX,room.DoorY]);
            var enclosed=ClosedQuarantineInterior(z,p);
            int unreachable=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                if(!FormationReachability.IsOpenGround(z,x,y))continue;
                if(!reach[x,y]){unreachable++;Assert.IsTrue(enclosed.Contains((x,y)),"Public walkable cell disconnected: "+x+","+y);}
                if(enclosed.Contains((x,y)))Assert.IsFalse(reach[x,y],"The closed quarantine must stay physically enclosed.");
            }
            Assert.AreEqual(6,unreachable,"Only six walkable cage cells are isolated; original rubble is passable terrain.");
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
        public void DisusedWingContainsOnlyItsQuarantinedCaseInsteadOfVillagePopulation(int seed)
        {
            var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(Id);var p=MarrowstyeCompositionPlan.Create(Id,seed);var r=p.Rooms.Single(a=>a.Role=="DisusedWing");
            var enclosed=ClosedQuarantineInterior(z,p);var threat=z.GetAllEntities().Single(e=>e.BlueprintName=="CurationHalfSet");
            Assert.AreEqual((r.X+15,r.Y+2),z.GetEntityPosition(threat));Assert.IsTrue(enclosed.Contains(z.GetEntityPosition(threat)));
            Assert.IsNotNull(threat.GetPart<BrainPart>());Assert.IsFalse(threat.HasTag("CanOpenDoors"));
            int actors=0;
            for(int y=r.Y+1;y<r.Y+r.Height-1;y++)for(int x=r.X+1;x<r.X+r.Width-1;x++)
            {
                Assert.IsTrue(p.IsReserved(x,y),"Disused interior must be excluded from initial service/drama placement.");
                foreach(var actor in z.GetCell(x,y).Objects.Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")))
                {actors++;Assert.AreSame(threat,actor,"Disused wing received an ordinary village resident.");}
            }
            Assert.AreEqual(1,actors,"The one deliberately contained case is the entire initial population of this wing.");
        }
        static System.Collections.Generic.HashSet<(int x,int y)> ClosedQuarantineInterior(Zone z,MarrowstyeCompositionPlan plan)
        {
            var room=plan.Rooms.Single(r=>r.Role=="DisusedWing");var gate=z.GetAllEntities().Single(e=>e.BlueprintName=="CurationQuarantineGate");
            var at=z.GetEntityPosition(gate);Assert.AreEqual((room.X+13,room.Y+2),at);Assert.AreEqual(1,gate.GetPart<DoorPart>().QuarterTurns);
            Assert.IsTrue(gate.GetPart<DoorPart>().IsClosed);Assert.IsTrue(gate.GetPart<LockPart>().IsLocked);
            var interior=new System.Collections.Generic.HashSet<(int x,int y)>();int rails=0;
            for(int y=at.y-1;y<=at.y+2;y++)for(int x=at.x;x<=at.x+4;x++)
            {
                if(x>at.x&&x<at.x+4&&y>at.y-1&&y<at.y+2){interior.Add((x,y));continue;}
                var owners=z.GetCell(x,y).Objects.Where(e=>e.BlueprintName=="CurationQuarantineGate"||e.BlueprintName=="CurationQuarantineRail").ToArray();
                Assert.AreEqual(1,owners.Length,"An excluded interior requires its actual closed boundary: "+x+","+y);
                Assert.IsTrue(owners[0].GetPart<PhysicsPart>().Solid);Assert.IsTrue(z.GetCell(x,y).BlocksMovement());
                if(owners[0]!=gate){rails++;Assert.AreEqual("CurationQuarantineRail",owners[0].BlueprintName);}
            }
            Assert.AreEqual(13,rails);Assert.AreEqual(6,interior.Count);return interior;
        }
    }
}
