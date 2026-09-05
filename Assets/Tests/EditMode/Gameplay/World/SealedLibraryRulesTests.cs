using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SealedLibraryRulesTests
    {
        private EntityFactory _factory;
        [SetUp] public void Setup()
        {
            _factory=new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear();SkillRegistry.ResetForTests();
        }
        private Entity Place(Zone z,string bp,int x=10,int y=10)
        {var e=_factory.CreateEntity(bp);Assert.IsNotNull(e,bp);Assert.IsTrue(z.AddEntity(e,x,y));return e;}
        private static SkillEventContext Context(Zone z,Entity actor)
            => new SkillEventContext { Attacker=actor,Defender=actor,Zone=z,Rng=new System.Random(1),DirectionX=1,DirectionY=0 };
        private static void Unlock(Entity door,Entity actor)
        {
            var key=new Entity();key.AddPart(new KeyPart { KeyId="coo.sealed-library.stillleaf" });
            actor.GetPart<InventoryPart>().AddObject(key);
            var e=GameEvent.New("InventoryAction");e.SetParameter("Command","Unlock");e.SetParameter("Actor",(object)actor);door.FireEventAndRelease(e);
            Assert.IsFalse(door.GetPart<LockPart>().IsLocked);
        }
        [TestCase("LibraryTepuiboneWall")] [TestCase("LibraryMemoryMarbleWall")] [TestCase("LibraryChoirIronWall")] [TestCase("SealedLibraryDoor")]
        public void ClosedArchitectureBlocksEveryCellPredicateAndRawDisplacement(string bp)
        {
            var z=new Zone();var barrier=Place(z,bp,11,10);var actor=Place(z,"Player");
            Assert.IsNotNull(barrier.GetPart("SealedLibraryBarrier"));
            var cell=z.GetCell(11,10);Assert.IsTrue(cell.IsSolid());Assert.IsTrue(cell.IsWall());Assert.IsTrue(cell.BlocksMovement());
            int version=z.EntityVersion;Assert.IsFalse(z.MoveEntity(actor,11,10));
            Assert.AreEqual((10,10),z.GetEntityPosition(actor));Assert.AreEqual(version,z.EntityVersion);
            Assert.IsTrue(z.MoveEntity(actor,10,11),"ordinary open ground remains usable");
        }
        [Test] public void ATestOnlyKeyOpensTheSameDoorForWalkingAndAllQueries()
        {
            var z=new Zone();var actor=Place(z,"Player");var door=Place(z,"SealedLibraryDoor",11,10);
            Assert.IsFalse(MovementSystem.TryMove(actor,z,1,0));Assert.IsTrue(door.GetPart<LockPart>().IsLocked);
            Unlock(door,actor);Assert.IsFalse(z.GetCell(11,10).IsSolid());Assert.IsFalse(z.GetCell(11,10).IsWall());
            Assert.IsFalse(z.GetCell(11,10).BlocksMovement());Assert.IsTrue(MovementSystem.TryMove(actor,z,1,0));
        }
        [TestCase("LibraryTepuiboneWall",false)] [TestCase("SealedLibraryDoor",false)]
        [TestCase("Wall",true)] [TestCase("SealedLibraryDoor",true)]
        public void VaultCannotSkipTheSealButKeepsOrdinaryWallAndUnlockedDoorBehavior(string bp,bool allowed)
        {
            var z=new Zone();var actor=Place(z,"Player");var barrier=Place(z,bp,11,10);
            if(bp=="SealedLibraryDoor"&&allowed)Unlock(barrier,actor);
            Assert.AreEqual(allowed,new Acrobatics_Vault().OnCommand(Context(z,actor)));
            Assert.AreEqual((allowed?12:10,10),z.GetEntityPosition(actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void TumbleRefusesEitherBlockedSwapEndpointWithoutPartialMutation(bool actorEnd)
        {
            var z=new Zone();var actor=Place(z,"Player");var target=Place(z,"Snapjaw",11,10);
            Place(z,"SealedLibraryDoor",actorEnd?10:11,10);
            int v=z.EntityVersion;Assert.IsFalse(new Acrobatics_Tumble().OnCommand(Context(z,actor)));
            Assert.AreEqual((10,10),z.GetEntityPosition(actor));Assert.AreEqual((11,10),z.GetEntityPosition(target));
            Assert.AreEqual(v,z.EntityVersion);Assert.IsFalse(target.HasEffect<ConfusedEffect>());
        }
        [Test] public void OrdinaryTumbleStillSwapsBothActors()
        {
            var z=new Zone();var actor=Place(z,"Player");var target=Place(z,"Snapjaw",11,10);
            Assert.IsTrue(new Acrobatics_Tumble().OnCommand(Context(z,actor)));
            Assert.AreEqual((11,10),z.GetEntityPosition(actor));Assert.AreEqual((10,10),z.GetEntityPosition(target));
        }
        private (OverworldZoneManager m,Zone old,Zone next,Entity actor) Travel(string oldID="Overworld.2.4.1",string nextID="Overworld.2.4.2")
        {
            var m=new OverworldZoneManager(_factory,66);var old=new Zone{ZoneID=oldID};var next=new Zone{ZoneID=nextID};
            m.CachedZones[oldID]=old;m.CachedZones[nextID]=next;var actor=Place(old,"Player");return(m,old,next,actor);
        }
        [Test] public void MatchedStairsAndFallbackNeverDropThePlayerInsideTheSeal()
        {
            var f=Travel();Place(f.next,"StairsUp",10,10);Place(f.next,"SealedLibraryFloor");Place(f.next,"StairsUp",20,10);
            var r=ZoneTransitionSystem.TransitionPlayerVertical(f.actor,f.old,true,10,10,f.m);
            Assert.IsTrue(r.Success);Assert.AreEqual((20,10),f.next.GetEntityPosition(f.actor));
        }
        [Test] public void NoEligibleVerticalArrivalLeavesTheSourceIntact()
        {
            var f=Travel();for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)Place(f.next,"SealedLibraryFloor",x,y);
            int v=f.old.EntityVersion;var r=ZoneTransitionSystem.TransitionPlayerVertical(f.actor,f.old,true,10,10,f.m);
            Assert.IsFalse(r.Success);Assert.AreEqual((10,10),f.old.GetEntityPosition(f.actor));
            Assert.IsNull(f.next.GetEntityCell(f.actor));Assert.AreEqual(v,f.old.EntityVersion);
        }
        [Test] public void OrdinaryStairsStillArriveAtTheirMatchedCell()
        {
            var f=Travel();Place(f.next,"StairsUp",10,10);
            Assert.IsTrue(ZoneTransitionSystem.TransitionPlayerVertical(f.actor,f.old,true,10,10,f.m).Success);
            Assert.AreEqual((10,10),f.next.GetEntityPosition(f.actor));
        }
        [Test] public void LateralArrivalSkipsAnExcludedExactDestination()
        {
            var f=Travel("Overworld.1.4.2","Overworld.2.4.2");Place(f.next,"SealedLibraryFloor",0,10);
            var r=ZoneTransitionSystem.TransitionPlayer(f.actor,f.old,TransitionDirection.East,79,10,f.m,f.m.WorldMap);
            Assert.IsTrue(r.Success);Assert.AreNotEqual((0,10),f.next.GetEntityPosition(f.actor));
        }
        [Test] public void TheActualFloorUsesTierThreeContentWithoutChangingTheDescent()
        {
            var m=new OverworldZoneManager(_factory,66);
            var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic);
            var p=(ZoneGenerationPipeline)method.Invoke(m,new object[]{"Overworld.2.4.2"});
            Assert.AreEqual("CaveTier3",p.Builders.OfType<PopulationBuilder>().Single().Table.Name);
            var cb=p.Builders.OfType<ContainerBuilder>().Single();
            Assert.AreEqual(3,typeof(ContainerBuilder).GetField("_tier",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(cb));
            p=(ZoneGenerationPipeline)method.Invoke(m,new object[]{"Overworld.2.4.1"});
            Assert.AreNotEqual("CaveTier3",p.Builders.OfType<PopulationBuilder>().Single().Table.Name);
        }
    }
}
