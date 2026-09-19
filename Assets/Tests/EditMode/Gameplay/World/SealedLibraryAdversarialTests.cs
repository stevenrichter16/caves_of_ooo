using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Player-flow hypotheses: sealed entry, saved interiors, accidental
    // relocation, stale identity, missing content and structural removal.
    public sealed class SealedLibraryAdversarialTests
    {
        private EntityFactory _factory;
        [SetUp] public void Setup()
        {
            _factory=new EntityFactory();_factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear();
        }
        private Entity Place(Zone z,string bp,int x=10,int y=10)
        {var e=_factory.CreateEntity(bp);Assert.IsNotNull(e,bp);Assert.IsTrue(z.AddEntity(e,x,y));return e;}
        private static HashSet<(int x,int y)> Flood(Zone z,(int x,int y) start)
        {
            var seen=new HashSet<(int x,int y)>{start};var q=new Queue<(int x,int y)>();q.Enqueue(start);
            while(q.Count>0)
            {
                var p=q.Dequeue();for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {var n=(p.x+dx,p.y+dy);var c=z.GetCell(n.Item1,n.Item2);if(c!=null&&!c.BlocksMovement()&&seen.Add(n))q.Enqueue(n);}
            }
            return seen;
        }
        [TestCase(1)] [TestCase(66)] [TestCase(913)]
        public void Adversarial_ClosedBoxSeparatesInteriorFromEveryApproachAndOpenDoorConnectsIt(int seed)
        {
            var z=new OverworldZoneManager(_factory,seed).GetZone(SealedLibraryBuilder.ZoneID);
            var door=z.GetAllEntities().Single(e=>e.BlueprintName=="SealedLibraryDoor");var pos=z.GetEntityPosition(door);
            var seen=Flood(z,(pos.x-1,pos.y));var interior=z.GetAllEntities().Where(e=>e.HasTag("ExcludeZoneArrival")).Select(z.GetEntityPosition).ToList();
            Assert.Greater(interior.Count,100);Assert.IsFalse(interior.Any(seen.Contains));
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()))Assert.IsTrue(seen.Contains(z.GetEntityPosition(e)));
            foreach(var p in new[]{(0,12),(79,12),(40,0),(40,24)})Assert.IsTrue(seen.Contains(p));
            door.GetPart<LockPart>().IsLocked=false;door.GetPart<PhysicsPart>().Solid=false;
            seen=Flood(z,(pos.x-1,pos.y));Assert.IsTrue(interior.Where(p=>!z.GetCell(p.x,p.y).BlocksMovement()).All(seen.Contains));
        }
        [TestCase("LibraryTepuiboneWall","Tepuibone")] [TestCase("LibraryMemoryMarbleWall","MemoryMarble")]
        [TestCase("LibraryChoirIronWall","ChoirIron")] [TestCase("SealedLibraryDoor","ChoirIron")]
        public void Adversarial_RealFixedMaterialsCannotBeDemolishedOrHauled(string bp,string material)
        {
            var z=new Zone();var e=Place(z,bp);var actor=Place(z,"Player",9,10);var d=e.GetPart<DestructiblePart>();
            Assert.AreEqual(material,e.GetPart<MaterialPart>().MaterialID);Assert.IsTrue(d.Indestructible);
            Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable);Assert.IsNull(e.GetPart<HandlingPart>());Assert.IsNull(e.GetPart<HarvestablePart>());
            Assert.IsFalse(e.HasTag("Creature"));Assert.IsNull(e.GetStat("Hitpoints"));
            Assert.AreEqual(DragVerdict.Rooted,DragRules.CanDrag(actor,e));
            int hp=d.HP;Assert.AreEqual(DestroyVerdict.Indestructible,DestructionSystem.Damage(e,int.MaxValue,actor,z));
            Assert.AreEqual(DestroyVerdict.Indestructible,DestructionSystem.Destroy(e,actor,z,"test"));
            Assert.AreEqual(hp,d.HP);Assert.AreEqual((10,10),z.GetEntityPosition(e));Assert.IsTrue(z.GetCell(10,10).HasClosedArchiveBarrier());
            Assert.IsFalse(e.GetPart<MaterialPart>().MaterialTags.Overlaps(new[]{"Ice","RawMeat","RawStarapple"}));
        }
        [Test] public void Adversarial_OrdinaryBreakableWallRemainsBreakable()
        {
            var z=new Zone();var wall=Place(z,"Wall");Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(wall,999,null,z));Assert.IsNull(z.GetEntityCell(wall));
        }
        [TestCase("SealedLibraryFloor")] [TestCase("SealedLibraryDoor")] [TestCase("LibraryTepuiboneWall")]
        [TestCase("LibraryMemoryMarbleWall")] [TestCase("LibraryChoirIronWall")] [TestCase("SealedArchiveShelf")] [TestCase("StoneFloor")]
        public void Adversarial_MissingBlueprintCannotClearExistingContent(string missing)
        {
            var z=new Zone();var kept=new Entity();z.AddEntity(kept,40,12);_factory.Blueprints.Remove(missing);
            Assert.IsFalse(new SealedLibraryBuilder().BuildZone(z,_factory,new System.Random(1)));
            CollectionAssert.AreEqual(new[]{kept},z.GetAllEntities());Assert.IsEmpty(z.GenReservedCells);
        }
        [TestCase(28,12,2,12)] [TestCase(40,8,64,16)] [TestCase(1,7,78,17)]
        public void Adversarial_AwkwardStairsRetainIdentityAndExteriorApproaches(int x,int y,int x2,int y2)
        {
            var z=new Zone();var up=Place(z,"StairsUp",x,y);var down=Place(z,"StairsDown",x2,y2);
            Assert.IsTrue(new SealedLibraryBuilder().BuildZone(z,_factory,new System.Random(1)));
            Assert.AreEqual((x,y),z.GetEntityPosition(up));Assert.AreEqual((x2,y2),z.GetEntityPosition(down));
            Assert.IsFalse(z.GetCell(x,y).HasObjectWithTag("ExcludeZoneArrival"));Assert.IsFalse(z.GetCell(x2,y2).HasObjectWithTag("ExcludeZoneArrival"));
            Assert.IsTrue(Flood(z,(x,y)).Contains((x2,y2)));
        }
        [Test] public void Adversarial_NoFreeAnchorAndNullInputsRefuseBeforeMutation()
        {
            var z=new Zone();foreach(int x in new[]{2,28,54})Place(z,"StairsUp",x,12);var before=z.GetAllEntities();
            var b=new SealedLibraryBuilder();Assert.IsFalse(b.BuildZone(z,_factory,new System.Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
            Assert.IsFalse(b.BuildZone(null,_factory,null));Assert.IsFalse(b.BuildZone(z,null,null));
        }
        [Test] public void Adversarial_ReapplyingBuilderPreservesObjectsAndReservations()
        {
            var z=new Zone();var b=new SealedLibraryBuilder();Assert.IsTrue(b.BuildZone(z,_factory,new System.Random(1)));
            var before=z.GetAllEntities();var cells=z.GenReservedCells.ToArray();Assert.IsTrue(b.BuildZone(z,_factory,new System.Random(2)));
            CollectionAssert.AreEquivalent(before,z.GetAllEntities());CollectionAssert.AreEquivalent(cells,z.GenReservedCells);
        }
        [TestCase("Olderdeep")] [TestCase("the Deepest Cathedral")]
        public void Adversarial_ProfileSurvivesAStoryRenamingTheMouth(string renamed)
        {
            var m=new OverworldZoneManager(_factory,66);m.WorldMap.GetPOI(2,4).Name=renamed;
            Assert.AreEqual(1,m.GetZone(SealedLibraryBuilder.ZoneID).GetAllEntities().Count(e=>e.BlueprintName=="SealedLibraryDoor"));
            m=new OverworldZoneManager(_factory,66);m.WorldMap.GetPOI(2,4).Profile=null;m.WorldMap.GetPOI(2,4).Name=renamed;
            Assert.IsFalse(m.GetZone(SealedLibraryBuilder.ZoneID).GetAllEntities().Any(e=>e.BlueprintName=="SealedLibraryDoor"));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_SaveRestoresBarrierClosureAndInteriorPlacementWithoutRebuilding(bool unlocked)
        {
            var m=new OverworldZoneManager(_factory,66);var z=m.GetZone(SealedLibraryBuilder.ZoneID);
            var door=z.GetAllEntities().Single(e=>e.BlueprintName=="SealedLibraryDoor");var p=z.GetEntityPosition(door);
            door.GetPart<LockPart>().IsLocked=!unlocked;door.GetPart<PhysicsPart>().Solid=!unlocked;
            var actor=Place(z,"Player",p.x+1,p.y);var item=Place(z,"Tepuibone",p.x+1,p.y);m.SetActiveZone(z);
            var turns=new TurnManager();turns.AddEntity(actor);var state=GameSessionState.Capture("sealed-test","test",m,turns,actor);
            using(var stream=new MemoryStream())
            {
                state.Save(new SaveWriter(stream));stream.Position=0;var loaded=GameSessionState.Load(new SaveReader(stream,_factory));
                var lz=loaded.ZoneManager.GetZone(z.ZoneID);var ld=lz.GetAllEntities().Single(e=>e.ID==door.ID);
                Assert.AreEqual(!unlocked,ld.GetPart<SealedLibraryBarrierPart>().IsClosed);
                Assert.AreEqual((p.x+1,p.y),lz.GetEntityPosition(loaded.Player));Assert.IsTrue(lz.GetEntityCell(loaded.Player).HasObjectWithTag("ExcludeZoneArrival"));
                Assert.IsTrue(lz.GetEntityCell(loaded.Player).Objects.Any(e=>e.ID==item.ID));
                Assert.IsTrue(lz.MoveEntity(loaded.Player,p.x+2,p.y),"arrival exclusion does not prevent ordinary interior movement");
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_FollowerPlacementSkipsInteriorOrLeavesItBehind(bool outsideAvailable)
        {
            var old=new Zone();var next=new Zone();var leader=Place(old,"Player");var follower=Place(old,"Snapjaw",11,10);
            var brain=leader.GetPart<BrainPart>();if(brain==null){brain=new BrainPart();leader.AddPart(brain);}
            brain.PartyMembers.Add(follower);follower.GetPart<BrainPart>().CurrentZone=old;
            for(int x=6;x<=14;x++)for(int y=6;y<=14;y++)Place(next,"SealedLibraryFloor",x,y);
            if(outsideAvailable)foreach(var e in next.GetCell(14,10).Objects.ToArray())next.RemoveEntity(e);
            ZoneTransitionSystem.TransitPartyMembers(leader,old,next,10,10);
            if(outsideAvailable){Assert.AreEqual((14,10),next.GetEntityPosition(follower));Assert.AreSame(next,follower.GetPart<BrainPart>().CurrentZone);}
            else{Assert.AreEqual((11,10),old.GetEntityPosition(follower));Assert.AreSame(old,follower.GetPart<BrainPart>().CurrentZone);}
        }
        [Test] public void Adversarial_ClosureIsDerivedPerInstanceAndDoesNotCacheAnOldLock()
        {
            var z=new Zone();var a=Place(z,"SealedLibraryDoor");var b=Place(z,"SealedLibraryDoor",11,10);
            a.GetPart<LockPart>().IsLocked=false;a.GetPart<PhysicsPart>().Solid=false;
            Assert.IsFalse(z.GetCell(10,10).HasClosedArchiveBarrier());Assert.IsTrue(z.GetCell(11,10).HasClosedArchiveBarrier());
            a.GetPart<LockPart>().IsLocked=true;Assert.IsTrue(z.GetCell(10,10).HasClosedArchiveBarrier());
            Assert.IsFalse(new SealedLibraryBarrierPart().IsClosed);
        }
        [Test] public void Adversarial_NoAmbientCreaturesContainersOrHazardsLandInsideTheArchive()
        {
            var z=new OverworldZoneManager(_factory,66).GetZone(SealedLibraryBuilder.ZoneID);
            // Stillleaf Archive SA.1: the one AUTHORED record (by id) is the
            // only permitted non-architecture object; nothing ambient lands.
            int authored=0;
            foreach(var floor in z.GetAllEntities().Where(e=>e.HasTag("ExcludeZoneArrival")))
            {
                var c=z.GetEntityCell(floor);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                authored+=c.Objects.Count(e=>e.ID==StillleafArchive.RegisterId);
                Assert.IsTrue(c.Objects.All(e=>e.BlueprintName=="SealedLibraryFloor"||e.BlueprintName=="SealedArchiveShelf"
                    ||e.ID==StillleafArchive.RegisterId));
            }
            Assert.AreEqual(1,authored,"exactly one authored register");
        }
        [Test] public void Adversarial_ExistingMapsGainOnlyAnEmptyMissingMouth()
        {
            var m=WorldGenerator.Generate(66);m.SetPOI(2,4,null);m.RehydrateAuthoredSinkholes();Assert.AreEqual("Stillleaf",m.GetPOI(2,4).Name);
            m.SetPOI(2,4,new PointOfInterest(POIType.Village,"Saved place"));m.RehydrateAuthoredSinkholes();Assert.AreEqual("Saved place",m.GetPOI(2,4).Name);
        }
        [TestCase(2,4)] [TestCase(4,6)]
        public void Adversarial_RenamedProfileCannotAcquireOpenSimaDaylight(int x,int y)
        {
            var m=new OverworldZoneManager(_factory,66);m.WorldMap.GetPOI(x,y).Name="Ginmere";
            Assert.AreEqual(OverworldZoneManager.GetDepthAmbient(2),m.GetZone($"Overworld.{x}.{y}.2").AmbientLevel);
            Assert.AreEqual(OverworldZoneManager.ShaftLightAmbient,m.GetZone("Overworld.2.7.2").AmbientLevel);
        }
        [TestCase("SealedLibrary")] [TestCase(null)]
        public void Adversarial_WaterPairGenerationUsesAuthoredProfileBeforeDisplayName(string profile)
        {
            int seed=0;while(FormationSelector.StableIndex("HelmwoodDoor|"+seed+"|2|4",8)!=0)seed++;
            var m=new OverworldZoneManager(_factory,seed);m.WorldMap.GetPOI(2,4).Name="Ginmere";m.WorldMap.GetPOI(2,4).Profile=profile;
            var surface=new Zone{ZoneID="Overworld.2.4.0"};var floor=new Zone{ZoneID="Overworld.2.4.2"};
            m.CachedZones[surface.ZoneID]=surface;m.CachedZones[floor.ZoneID]=floor;
            Place(surface,"WaterPuddle");Place(floor,"WaterPuddle");
            HelmwoodPassages.OnZoneGenerated(floor,m);
            Assert.AreEqual(profile==null,floor.GetAllEntities().Any(e=>e.HasPart<WaterPassagePart>()));
            Assert.AreEqual(profile==null,surface.GetAllEntities().Any(e=>e.HasPart<WaterPassagePart>()));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_MovementFacadeCannotReportEntryWhenTheZoneRefuses(bool extended)
        {
            // A mover without Physics exercises the lower-level refusal:
            // no BeforeMove part hides whether the facade honors its result.
            var z=new Zone();var actor=new Entity();var probe=new MoveProbe();actor.AddPart(probe);z.AddEntity(actor,10,10);
            Place(z,"SealedLibraryDoor",11,10);
            bool moved=extended?MovementSystem.TryMoveEx(actor,z,1,0).moved:MovementSystem.TryMoveTo(actor,z,11,10);
            Assert.IsFalse(moved);Assert.AreEqual((10,10),z.GetEntityPosition(actor));Assert.AreEqual(0,probe.AfterMoves);
            moved=extended?MovementSystem.TryMoveEx(actor,z,0,1).moved:MovementSystem.TryMoveTo(actor,z,10,11);
            Assert.IsTrue(moved);Assert.AreEqual(1,probe.AfterMoves);
        }
        private sealed class MoveProbe : Part
        {
            public override string Name=>"ArchiveTestMoveProbe";
            public int AfterMoves;
            public override bool HandleEvent(GameEvent e){if(e.ID=="AfterMove")AfterMoves++;return true;}
        }
        [Test] public void Adversarial_RelockingTheDoorDoesNotRequireASecondCollisionLatch()
        {
            var z=new Zone();var actor=Place(z,"Player");var door=Place(z,"SealedLibraryDoor",11,10);
            door.GetPart<PhysicsPart>().Solid=false; // stale mirror after an authored relock
            Assert.IsFalse(MovementSystem.TryMove(actor,z,1,0));Assert.AreEqual((10,10),z.GetEntityPosition(actor));
            var key=new Entity();key.AddPart(new KeyPart{KeyId=SealedLibraryBuilder.KeyID});actor.GetPart<InventoryPart>().AddObject(key);
            Assert.IsFalse(MovementSystem.TryMove(actor,z,1,0));Assert.IsFalse(door.GetPart<LockPart>().IsLocked);
            Assert.IsTrue(MovementSystem.TryMove(actor,z,1,0));
        }
    }
}
