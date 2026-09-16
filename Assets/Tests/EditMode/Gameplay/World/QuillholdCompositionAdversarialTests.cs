using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Hypotheses about interrupted generation, malformed inherited
    /// content, real cave arrivals, later services, and the ownership of archive stock.</summary>
    public class QuillholdCompositionAdversarialTests
    {
        private const string Id=QuillholdCompositionTests.Id;
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeLoot()=>LootTableRegistry.ResetForTests();
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("RoadStone")] [TestCase("QuillholdArchiveWall")]
        [TestCase("QuillholdRefectoryTable")] [TestCase("Bed")] [TestCase("Chair")] [TestCase("Crate")] [TestCase("QuillholdCopyDesk")] [TestCase("QuillholdArchiveShelf")]
        [TestCase("Scribe")] [TestCase("WardGleamGrimoire")] [TestCase("DryingBreezeGrimoire")] [TestCase("InkVial")]
        public void MissingEarlyOrLateDependencyCannotLeaveAnUnfinishedArchive(string bp)
        {
            var f=QuillholdCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(Id);z.GenReservedCells.Add((79,24));var b=new QuillholdCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);Assert.IsNull(b.RealizedZone);Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }
        [TestCase("Floor","Render","RenderString","?")] [TestCase("StoneFloor","Physics","Takeable","true")]
        [TestCase("RoadStone","Physics","Solid","true")] [TestCase("QuillholdArchiveWall","Destructible","HP","0")]
        [TestCase("QuillholdArchiveWall","Material",null,null)] [TestCase("QuillholdArchiveShelf","Container",null,null)]
        [TestCase("QuillholdArchiveShelf","Destructible","Indestructible","true")]
        [TestCase("QuillholdRefectoryTable","Destructible",null,null)] [TestCase("QuillholdCopyDesk","Destructible",null,null)] [TestCase("Scribe","Conversation","ConversationID","Merchant_1")]
        [TestCase("Scribe","Render","RenderString","?")] [TestCase("Scribe","Trader",null,null)]
        public void FailSoftPartFieldsCannotPublishVisibleButMechanicallyFalseOwners(string bp,string part,string key,string value)
        {
            var f=QuillholdCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(Id);z.GenReservedCells.Add((79,24));Assert.IsFalse(new QuillholdCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase(-1,0)] [TestCase(80,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void InvalidCoordinatesNeverClampToArchiveInteriors(int x,int y)
        {var p=QuillholdCompositionPlan.Create(Id,64);Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsReserved(x,y));}
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingZoneFactoryOrRandomCannotMutateFlags(int missing)
        {var z=new Zone(Id);var b=new QuillholdCompositionBuilder(64);Assert.IsFalse(b.BuildZone(missing==0?null:z,missing==1?null:QuillholdCompositionTests.Factory(),missing==2?null:new Random(1)));Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);}
        [TestCase(false)] [TestCase(true)]
        public void RejectedReuseCannotInvalidateAnAlreadySuccessfulOwnersPendingProfile(bool foreign)
        {
            var f=QuillholdCompositionTests.Factory();var z=new Zone(Id);var b=new QuillholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;var other=foreign?new Zone(Id):z;
            Assert.IsFalse(b.BuildZone(other,f,new Random(2)));Assert.AreSame(p,b.Plan);Assert.AreSame(z,b.RealizedZone);
            if(foreign){Assert.IsFalse(new QuillholdProfileBuilder(b).BuildZone(other,f,new Random(1)));Assert.IsFalse(new QuillholdArrivalReservationBuilder(b).BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);}
            Assert.IsTrue(new QuillholdProfileBuilder(b).BuildZone(z,f,new Random(3)));
        }
        [Test] public void RemovingAShelfNeverResurrectsItButAnExplicitEmptyRetryGetsFreshOwners()
        {
            var f=QuillholdCompositionTests.Factory();var z=new Zone(Id);var b=new QuillholdCompositionBuilder(64);var late=new QuillholdProfileBuilder(b);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var original=z.GetAllEntities().ToArray();z.RemoveEntity(original.First(e=>e.BlueprintName=="QuillholdArchiveShelf"));var survivors=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(survivors,z.GetAllEntities());
            foreach(var e in survivors)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(3)));Assert.IsTrue(late.BuildZone(z,f,new Random(3)));Assert.IsFalse(z.GetAllEntities().Any(original.Contains));Assert.AreEqual(6,z.GetAllEntities().Count(e=>e.BlueprintName=="QuillholdArchiveShelf"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LastOwnerFailureCannotPublishHalfTheArchive(bool malformed)
        {
            var f=QuillholdCompositionTests.Factory();var z=new Zone(Id);var b=new QuillholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var last=b.Plan.Profile.Last();
            if(malformed)f.Blueprints[last.Blueprint].Parts["Physics"]["Takeable"]="true";else Assert.IsTrue(z.AddEntity(f.CreateEntity("SandstoneWall"),last.X,last.Y));var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(new QuillholdProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(POIType.Village,"CentralExchange")] [TestCase(POIType.Village,null)] [TestCase(POIType.Lair,"PrimaryArchive")] [TestCase(POIType.Village,"primaryarchive")]
        public void ActualMapProfileAndPlaceTypeVetoTheComposition(POIType type,string profile)
        {
            var m=new OverworldZoneManager(QuillholdCompositionTests.Factory(),64);Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<QuillholdCompositionBuilder>().Any());
            m.WorldMap.SetPOI(14,9,new PointOfInterest(type,"Quillhold",profile:profile));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<QuillholdCompositionBuilder>().Any());
        }
        [Test] public void RenamingThePlaceCannotRemoveItsProfileOrSpreadItToAnotherAddress()
        {
            var m=new OverworldZoneManager(QuillholdCompositionTests.Factory(),64);m.WorldMap.GetPOI(14,9).Name="renamed archive";Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<QuillholdCompositionBuilder>().Any());
            m.WorldMap.SetPOI(13,9,new PointOfInterest(POIType.Village,"Quillhold",profile:"PrimaryArchive"));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.13.9.0").Builders.OfType<QuillholdCompositionBuilder>().Any());
        }
        [Test] public void ThirtyTwoSeedsAndExtremeSeedsKeepEveryOpenBaseCellReachable()
        {
            var f=QuillholdCompositionTests.Factory();foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var z=new Zone(Id);var b=new QuillholdCompositionBuilder(seed);Assert.DoesNotThrow(()=>Assert.IsTrue(b.BuildZone(z,f,new Random(1))),"native base seed "+seed);var seen=QuillholdCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(seen);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(seen.Contains((x,y)),"seed "+seed+" pocket "+x+","+y);
            }
        }
        [TestCase("WaterPuddle")] [TestCase("SandstoneWall")]
        public void NativeCaveGateTestsLiveNeighborsNotJustAnEmptyCandidateCentre(string obstacle)
        {
            var f=QuillholdCompositionTests.Factory();var z=new Zone(Id);var b=new QuillholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var at=Candidate(b,z);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
            Assert.IsFalse(b.CanPlaceCaveEntrance(new Zone(Id),z.GetCell(at.x,at.y)));Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(Id).GetCell(at.x,at.y)));
            var blocked=f.CreateEntity(obstacle);Assert.IsTrue(z.AddEntity(blocked,at.x+1,at.y));Assert.IsFalse(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));z.RemoveEntity(blocked);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
        }
        public static (int x,int y) Candidate(QuillholdCompositionBuilder b,Zone z)
        {for(int y=1;y<24;y++)for(int x=1;x<79;x++)if(b.CanPlaceCaveEntrance(z,z.GetCell(x,y)))return(x,y);Assert.Fail("A native cave roll must retain eligible ground.");return(-1,-1);}
        [Test] public void ActualCaveRollsAvoidStacksAndCopyingHallAndReserveRealDryArrivals()
        {
            int stairs=0;var f=QuillholdCompositionTests.Factory();foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(Id);var p=QuillholdCompositionPlan.Create(Id,seed);
                foreach(var owner in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    stairs++;var c=z.GetEntityCell(owner);Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(p.IsReserved(c.X,c.Y));Assert.IsFalse(p.IsInterior(c.X,c.Y));Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {var n=z.GetCell(c.X+dx,c.Y+dy);Assert.NotNull(n);Assert.IsFalse(n.IsInterior);Assert.IsFalse(n.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((n.X,n.Y)));Assert.IsFalse(n.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));}
                }
            }Assert.Greater(stairs,0,"Exercise actual successful native rolls, not an empty loop.");
        }
        // Damage must remove this owner and release actual books once, rather
        // than erasing only art or cloning the shelf on the next profile pass.
        [TestCase("QuillholdArchiveWall")] [TestCase("QuillholdArchiveShelf")] [TestCase("QuillholdCopyDesk")] [TestCase("QuillholdRefectoryTable")]
        public void NewArchiveFurnitureIsDestructibleAndStoredContentsSpillOnlyOnce(string blueprint)
        {
            var f=QuillholdCompositionTests.Factory();var z=new Zone(Id);var b=new QuillholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new QuillholdProfileBuilder(b);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var owner=z.GetAllEntities().First(e=>e.BlueprintName==blueprint);var at=z.GetEntityPosition(owner);var stored=owner.GetPart<ContainerPart>()?.Contents.ToArray()??Array.Empty<Entity>();var survivor=z.GetAllEntities().First(e=>e.BlueprintName==blueprint&&e!=owner);
            Assert.IsTrue(DestructionSystem.IsBreakable(owner));DestructionSystem.Damage(owner,10000,null,z);Assert.IsNull(z.GetEntityCell(owner));Assert.NotNull(z.GetEntityCell(survivor));Assert.IsFalse(z.GetCell(at.x,at.y).BlocksMovement());
            foreach(var item in stored){Assert.AreEqual(at,z.GetEntityPosition(item));Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);}
            var remaining=z.GetAllEntities().ToArray();DestructionSystem.Damage(owner,10000,null,z);Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(remaining,z.GetAllEntities());
            var protectedWall=f.CreateEntity("LibraryMemoryMarbleWall");Assert.IsFalse(DestructionSystem.IsBreakable(protectedWall),"Stillleaf's sealed barrier is the opposite control, never the ordinary archive wall.");
        }
        [TestCase(false)] [TestCase(true)]
        public void PalimpsestScribesKeepActualRenewingInkStockAndItsFactoryGate(bool noFactory)
        {
            var oldFactory=TraderPart.Factory;var oldRng=TraderPart.Rng;var oldRestock=TraderRestockSystem.Factory;
            try
            {
                var f=QuillholdCompositionTests.Factory();TraderPart.Factory=f;TraderPart.Rng=new Random(64);TraderRestockSystem.Factory=noFactory?null:f;
                var z=new OverworldZoneManager(f,64).GetZone(Id);var scribe=z.GetAllEntities().Single(e=>e.BlueprintName=="Scribe");var inv=scribe.GetPart<InventoryPart>();Assert.AreEqual("Palimpsest",scribe.GetTag("Faction"));Assert.AreEqual("ScribeStock",scribe.GetPart<TraderPart>().StockTable);Assert.IsTrue(inv.Objects.Any(e=>e.BlueprintName=="InkVial"));
                foreach(var item in inv.Objects.ToArray())inv.RemoveObject(item);scribe.SetIntProperty(TraderRestockSystem.LastRestockProp,0);
                TraderRestockSystem.RestockZone(z,300);Assert.AreEqual(0,inv.Objects.Count);TraderRestockSystem.RestockZone(z,301);Assert.AreEqual(!noFactory,inv.Objects.Any(e=>e.BlueprintName=="InkVial"));
            }finally{TraderPart.Factory=oldFactory;TraderPart.Rng=oldRng;TraderRestockSystem.Factory=oldRestock;}
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void LaterPopulationAndContainersCannotSealArchiveAislesOrServiceFrontages(int seed)
        {
            var z=new OverworldZoneManager(QuillholdCompositionTests.Factory(),seed).GetZone(Id);var plan=QuillholdCompositionPlan.Create(Id,seed);
            var services=z.GetAllEntities().Where(e=>e.BlueprintName=="Scribe"||e.BlueprintName=="QuillholdArchiveShelf"||e.BlueprintName=="Merchant"||e.BlueprintName=="Quartermaster").Select(e=>(e.BlueprintName,z.GetEntityPosition(e))).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasTag("Creature")||e.HasPart<BrainPart>()).ToArray())z.RemoveEntity(e);
            var reached=QuillholdCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(reached);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(reached.Contains((x,y)),"seed "+seed+" late pocket "+x+","+y);
            foreach(var service in services)Assert.IsTrue(CinderholdCompositionTests.Neighbors(service.Item2.x,service.Item2.y).Any(reached.Contains),service.BlueprintName);
            foreach(var room in plan.Rooms)Assert.IsFalse(z.GetCell(room.DoorX,room.DoorY).BlocksMovement());
        }
        [Test] public void CopyingServiceRetainsAnExplicitReservedStandingApronAgainstLaterFurniture()
        {
            foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var p=QuillholdCompositionPlan.Create(Id,seed);Assert.IsTrue(p.TryGetServiceCell("Scribe",out int x,out int y));Assert.IsFalse(p.IsReserved(x,y),"Native population needs the service cell eligible.");
                Assert.IsTrue(CinderholdCompositionTests.Neighbors(x,y).Any(n=>p.IsInterior(n.x,n.y)&&p.IsReserved(n.x,n.y)&&p.ObjectAt(n.x,n.y)==null&&!p.Profile.Any(o=>o.X==n.x&&o.Y==n.y)),"Seed "+seed+": at least one real standing cell needs protection from later furniture, independently of the actor cell.");
            }
        }
        // The initial render showed six isolated cabinets in a vacant hall.
        // Keep the existing six containers/loot budget, but make bookcase runs
        // define an aisle rather than scattering them as unrelated ornaments.
        [Test] public void SixShelvesFormTwoCoherentRunsWithAnOpenCrossingAisle()
        {
            foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var p=QuillholdCompositionPlan.Create(Id,seed);var shelves=p.Profile.Where(o=>o.Blueprint=="QuillholdArchiveShelf").Select(o=>(x:o.X,y:o.Y)).ToArray();Assert.AreEqual(6,shelves.Length);
                var remaining=new System.Collections.Generic.HashSet<(int x,int y)>(shelves);var runs=new System.Collections.Generic.List<System.Collections.Generic.List<(int x,int y)>>();
                while(remaining.Count>0)
                {
                    var first=remaining.First();remaining.Remove(first);var run=new System.Collections.Generic.List<(int x,int y)>{first};var queue=new System.Collections.Generic.Queue<(int x,int y)>();queue.Enqueue(first);
                    while(queue.Count>0){var at=queue.Dequeue();foreach(var n in CinderholdCompositionTests.Neighbors(at.x,at.y))if(remaining.Remove(n)){run.Add(n);queue.Enqueue(n);}}runs.Add(run);
                }
                Assert.AreEqual(2,runs.Count,"Seed "+seed+": two deliberate bookcase runs, not six far-apart cabinets.");
                foreach(var run in runs){Assert.AreEqual(3,run.Count);Assert.IsTrue(run.Select(n=>n.x).Distinct().Count()==1||run.Select(n=>n.y).Distinct().Count()==1,"A run is a readable straight modular shelf, not a random corner.");}
                var room=p.Rooms.Single(r=>r.Role=="Stacks");bool horizontal=runs[0].Select(n=>n.y).Distinct().Count()==1;
                int crossing=horizontal?(runs[0][0].y+runs[1][0].y)/2:(runs[0][0].x+runs[1][0].x)/2;
                for(int i=horizontal?room.X+1:room.Y+1;i<(horizontal?room.X+room.Width-1:room.Y+room.Height-1);i++)
                {var cell=horizontal?(x:i,y:crossing):(x:crossing,y:i);Assert.IsNull(p.ObjectAt(cell.x,cell.y));Assert.IsFalse(shelves.Contains(cell));Assert.IsTrue(p.IsInterior(cell.x,cell.y));}
            }
        }
    }
}
