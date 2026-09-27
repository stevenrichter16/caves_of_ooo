using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityGeneratedDoorTests
    {
        Zone zone; Entity actor, door;
        [SetUp] public void SetUp()
        { MessageLog.Clear(); zone=new Zone("door-test"); actor=Actor("actor",true); zone.AddEntity(actor,10,10); }
        static Entity Actor(string id,bool player)
        {
            var e=new Entity{ID=id,BlueprintName=player?"Player":"Villager"};
            e.SetTag("Creature");if(player)e.SetTag("Player");
            e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart{DisplayName=player?"you":"villager"});e.AddPart(new InventoryPart());
            e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Min=0,Max=40};return e;
        }
        Part Door(bool open=true)
        {
            var type=typeof(PhysicsPart).Assembly.GetType("CavesOfOoo.Core.DoorPart");
            Assert.NotNull(type,"ordinary generated doors need a real saved Part");
            var part=(Part)Activator.CreateInstance(type);type.GetField("IsOpen").SetValue(part,open);
            door=new Entity{ID="door",BlueprintName="VillageDoor"};door.SetTag("Furniture");
            door.AddPart(new PhysicsPart());door.AddPart(new RenderPart{DisplayName="door",RenderString=open?"/":"+"});door.AddPart(part);
            Assert.True(zone.AddEntity(door,11,10));return part;
        }
        static bool IsOpen(Entity e)=>(bool)e.GetPart("Door").GetType().GetField("IsOpen").GetValue(e.GetPart("Door"));
        bool Act(string command,Entity who=null,Zone where=null)
        {
            var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)(who??actor));e.SetParameter("Zone",(object)(where??zone));e.SetParameter("Command",command);
            door.FireEvent(e);bool handled=e.Handled;e.Release();return handled;
        }
        [TestCase(false)] [TestCase(true)]
        public void OpenStateControlsRealCollisionAndSight(bool open)
        {
            Door(open);var c=zone.GetCell(11,10);
            Assert.AreEqual(!open,c.BlocksMovement());Assert.AreEqual(!open,c.IsWall());Assert.AreEqual(open,c.IsPassable());
            Assert.AreEqual(open,MovementSystem.TryMove(actor,zone,1,0));Assert.AreEqual(open?(11,10):(10,10),zone.GetEntityPosition(actor));
        }
        [Test] public void MenuOpenAndCloseChangeOneOwnerWithoutMovement()
        {
            Door(false);Assert.True(Act("OpenDoor"));Assert.True(IsOpen(door));Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
            Assert.True(Act("CloseDoor"));Assert.False(IsOpen(door));Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
        [TestCase("same_state")][TestCase("distant")][TestCase("dead")][TestCase("removed")][TestCase("foreign_zone")][TestCase("locked")]
        public void InvalidRequestsAreUnchangedAndUnconsumed(string fault)
        {
            Door(false);Zone requestZone=zone;
            if(fault=="distant")zone.MoveEntity(actor,5,5);
            if(fault=="dead")actor.SetTag("_DeathHandled");
            if(fault=="removed")zone.RemoveEntity(door);
            if(fault=="foreign_zone")requestZone=new Zone("other");
            if(fault=="locked")door.AddPart(new LockPart{KeyId="iron",IsLocked=true});
            Assert.False(Act(fault=="same_state"?"CloseDoor":"OpenDoor",where:requestZone));Assert.False(IsOpen(door));
        }
        [TestCase(true)][TestCase(false)]
        public void ClosingRefusesLooseObjectsButPermitsGround(bool terrain)
        {
            Door();var item=new Entity{ID="occupant"};if(terrain)item.SetTag("Terrain");else item.AddPart(new PhysicsPart{Takeable=true});zone.AddEntity(item,11,10);
            Assert.AreEqual(terrain,Act("CloseDoor"));Assert.AreEqual(!terrain,IsOpen(door));
        }
        [Test]public void ClosingRefusesActorStandingInDoorway()
        {Door();zone.MoveEntity(actor,11,10);Assert.False(Act("CloseDoor"));Assert.True(IsOpen(door));}
        [TestCase(true)][TestCase(false)]
        public void NpcPermissionIsExplicit(bool capable)
        {
            Door(false);var npc=Actor("npc",false);if(capable)npc.SetTag("CanOpenDoors");zone.AddEntity(npc,11,11);
            Assert.AreEqual(capable,Act("OpenDoor",npc));Assert.AreEqual(capable,IsOpen(door));
        }
        [Test]public void DetailedMoveCanReportOpeningWithoutPretendingToMove()
        {
            Door(false);var method=typeof(MovementSystem).GetMethod("TryMoveDetailed",BindingFlags.Public|BindingFlags.Static);
            Assert.NotNull(method,"native input and NPC fallback need an explicit stationary action outcome");
            var first=method.Invoke(null,new object[]{actor,zone,1,0});var type=first.GetType();
            Assert.False((bool)type.GetField("Moved").GetValue(first));Assert.True((bool)type.GetField("ActionPerformed").GetValue(first));
            Assert.True(IsOpen(door));Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
            var second=method.Invoke(null,new object[]{actor,zone,1,0});Assert.True((bool)type.GetField("Moved").GetValue(second));Assert.AreEqual((11,10),zone.GetEntityPosition(actor));
        }
        void Corridor()
        {for(int y=0;y<Zone.Height;y++){if(y==10)continue;var wall=new Entity{ID="wall-"+y};wall.SetTag("Wall");wall.SetTag("Solid");zone.AddEntity(wall,11,y);}}
        [TestCase(true)][TestCase(false)]
        public void ActorAwarePathPlansOnlyThroughOperableDoor(bool capable)
        {
            Door(false);Corridor();if(!capable)actor.Tags.Remove("Player");
            Assert.AreEqual(capable,FindPath.Search(zone,10,10,13,10,actor:actor).Usable);
            Assert.False(FindPath.Search(zone,10,10,13,10).Usable,"actorless topology is physical, not a free-door assumption");
            Assert.False(IsOpen(door));Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
        [Test]public void NpcMoveGoalOpensAndOnlyMovesOnTheFollowingAction()
        {
            Door(false);Corridor();actor.Tags.Remove("Player");actor.SetTag("CanOpenDoors");
            var brain=new BrainPart{CurrentZone=zone};actor.AddPart(brain);var goal=new MoveToGoal(13,10);brain.PushGoal(goal);
            goal.TakeAction();Assert.True(IsOpen(door));Assert.AreEqual((10,10),zone.GetEntityPosition(actor),"opening is the whole NPC action");
            goal.TakeAction();Assert.AreEqual((11,10),zone.GetEntityPosition(actor),"cached path must not skip the door step");
        }
        [Test]public void GreedyNpcFallbackStopsAfterItsDoorAction()
        {
            Door(false);actor.Tags.Remove("Player");actor.SetTag("CanOpenDoors");
            Assert.True(AIHelpers.TryStepToward(actor,zone,10,10,13,10));Assert.True(IsOpen(door));
            Assert.AreEqual((10,10),zone.GetEntityPosition(actor),"fallback must not take a second move after opening");
        }

        [TestCase(false)][TestCase(true)]
        public void PhysicalPlacementNeverAssumesADoorWillBeOpened(bool wide)
        {
            Door(false);if(wide){actor.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});zone.RemoveEntity(actor);Assert.True(zone.AddEntity(actor,10,10));}
            Assert.False(zone.CanPlaceFootprint(actor,11,10),"actual placement must obey the closed door even for capable actors");
            Assert.True(Act("OpenDoor"));Assert.True(zone.CanPlaceFootprint(actor,11,10));
        }
        [TestCase("locked")][TestCase("foreign-owner")][TestCase("solid-companion")]
        public void PathNeverTreatsDoorPermissionAsPermissionForOtherBlockers(string fault)
        {
            var part=Door(false);Corridor();
            if(fault=="locked")door.AddPart(new LockPart{KeyId="iron",IsLocked=true});
            if(fault=="foreign-owner")part.GetType().GetField("OwnerId").SetValue(part,"someone-else");
            if(fault=="solid-companion"){var stone=new Entity{ID="stone"};stone.SetTag("Solid");zone.AddEntity(stone,11,10);}
            Assert.False(FindPath.Search(zone,10,10,13,10,actor:actor).Usable);Assert.False(IsOpen(door));
        }
        [Test]public void ClosedDoorAsGoalStillNeedsPermission()
        {
            Door(false);actor.Tags.Remove("Player");
            Assert.False(FindPath.Search(zone,10,10,11,10,actor:actor).Usable,"goal shortcut cannot bypass a door");
        }
        [Test]public void StepGoalDoesNotConsumeItsMovementWhenItOnlyOpensDoor()
        {
            Door(false);var brain=new BrainPart{CurrentZone=zone};actor.AddPart(brain);var goal=new StepGoal(1,0);brain.PushGoal(goal);
            goal.TakeAction();Assert.True(IsOpen(door));Assert.False(goal.Finished());Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
            goal.TakeAction();Assert.True(goal.Finished());Assert.AreEqual((11,10),zone.GetEntityPosition(actor));
        }

    }
}
