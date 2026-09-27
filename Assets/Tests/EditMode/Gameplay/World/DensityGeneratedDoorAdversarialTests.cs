using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensityGeneratedDoorAdversarialTests
    {
        Zone zone;Entity actor,door;DoorPart part;
        [SetUp]public void Setup()
        {
            MessageLog.Clear();zone=new Zone("door-edge");actor=new Entity{ID="actor"};actor.SetTag("Player");actor.SetTag("Creature");actor.AddPart(new PhysicsPart{Solid=true});actor.AddPart(new StatusEffectsPart());actor.AddPart(new InventoryPart());actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Max=40};zone.AddEntity(actor,10,10);
            door=new Entity{ID="door",BlueprintName="VillageDoor"};door.AddPart(new PhysicsPart());door.AddPart(new RenderPart{DisplayName="door"});part=new DoorPart{IsOpen=false};door.AddPart(part);zone.AddEntity(door,11,10);
        }
        [TestCase("morrowfast")][TestCase("archive")]
        public void HybridAuthoredAuthorityCannotBeClaimedByOrdinaryDoor(string kind)
        {
            if(kind=="morrowfast")door.AddPart(new MorrowfastDoorPart());else door.AddPart(new SealedLibraryBarrierPart());
            Assert.False(part.CanOperate(actor,zone));Assert.False(part.TrySetOpen(actor,zone,true));Assert.False(part.IsOpen);
        }
        [TestCase("takeable")][TestCase("carried")][TestCase("equipped")][TestCase("creature")][TestCase("removed")][TestCase("part-removed")][TestCase("part-replaced")][TestCase("actor-removed")][TestCase("actor-clone")][TestCase("dead")][TestCase("zero-hp")][TestCase("stunned")]
        public void InvalidAuthorityDoesNotOpenOrMove(string fault)
        {
            if(fault=="takeable")door.GetPart<PhysicsPart>().Takeable=true;
            if(fault=="carried")door.GetPart<PhysicsPart>().InInventory=actor;
            if(fault=="equipped")door.GetPart<PhysicsPart>().Equipped=actor;
            if(fault=="creature")door.SetTag("Creature");
            if(fault=="removed")zone.RemoveEntity(door);
            if(fault=="part-removed"||fault=="part-replaced")door.RemovePart(part);
            if(fault=="part-replaced")door.AddPart(new DoorPart{IsOpen=false});
            if(fault=="actor-removed")zone.RemoveEntity(actor);
            if(fault=="actor-clone")actor=new Entity{ID="actor"};
            if(fault=="dead")actor.SetTag("_DeathHandled");
            if(fault=="zero-hp")actor.Statistics["Hitpoints"].BaseValue=0;
            if(fault=="stunned")actor.ApplyEffect(new StunnedEffect(3));
            Assert.False(part.TrySetOpen(actor,zone,true));Assert.False(part.IsOpen);
        }
        [TestCase(true)][TestCase(false)]
        public void ExactOwnerPermissionIsNotADisplayNameMatch(bool actualOwner)
        {
            part.OwnerId=actualOwner?actor.ID:"someone-else";
            Assert.AreEqual(actualOwner,part.TrySetOpen(actor,zone,true));Assert.AreEqual(actualOwner,part.IsOpen);
        }
        [TestCase(false)][TestCase(true)]
        public void OffsetCreatureBodyPreventsClosingEvenWithAnchorElsewhere(bool overlap)
        {
            part.IsOpen=true;zone.MoveEntity(actor,10,9);var wide=new Entity{ID="wide"};wide.SetTag("Creature");wide.AddPart(new PhysicsPart{Solid=true});wide.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.True(zone.AddEntity(wide,overlap?10:15,10));
            Assert.AreEqual(!overlap,part.TrySetOpen(actor,zone,false));Assert.AreEqual(overlap,part.IsOpen);
        }
        [Test]public void MovementRootDoesNotForbidExplicitStationaryDoorAction()
        {actor.ApplyEffect(new RootedEffect(4));Assert.True(part.TrySetOpen(actor,zone,true));Assert.False(MovementSystem.TryMove(actor,zone,1,0));Assert.AreEqual((10,10),zone.GetEntityPosition(actor));}
        [TestCase(true)][TestCase(false)]
        public void LegacyUnlockDetailedResultIsOneStationaryAction(bool matchingKey)
        {
            door.RemovePart(part);door.GetPart<PhysicsPart>().Solid=true;door.AddPart(new LockPart{IsLocked=true,KeyId="iron"});
            var key=new Entity{ID="key"};key.AddPart(new PhysicsPart{Takeable=true});key.AddPart(new KeyPart{KeyId=matchingKey?"iron":"other"});actor.GetPart<InventoryPart>().AddObject(key);
            var move=MovementSystem.TryMoveDetailed(actor,zone,1,0);Assert.False(move.Moved);Assert.AreEqual(matchingKey,move.ActionPerformed);Assert.AreEqual(!matchingKey,door.GetPart<LockPart>().IsLocked);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
        [Test]public void GreedyAwayStopsAfterOpening()
        {
            actor.Tags.Remove("Player");actor.SetTag("CanOpenDoors");Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,10));Assert.True(part.IsOpen);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
        [Test]public void ApproachPathStopsAfterOpening()
        {for(int y=0;y<Zone.Height;y++){if(y==10)continue;var wall=new Entity{ID="wall-"+y};wall.SetTag("Solid");zone.AddEntity(wall,11,y);}Assert.True(AIHelpers.TryApproachWithPathfinding(actor,zone,10,10,13,10));Assert.True(part.IsOpen);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));}
        [TestCase(true,true)][TestCase(false,true)][TestCase(true,false)][TestCase(false,false)]
        public void UnlockRefreshesGlyphWithoutChangingTheSavedLatch(bool open,bool bump)
        {
            part.IsOpen=open;door.AddPart(new LockPart{IsLocked=true,KeyId=""});part.Initialize();Assert.AreEqual("+",door.GetPart<RenderPart>().RenderString);
            if(bump)Assert.True(MovementSystem.TryMoveDetailed(actor,zone,1,0).ActionPerformed);
            else{var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)zone);e.SetParameter("Command","Unlock");door.FireEventAndRelease(e);}
            Assert.False(door.GetPart<LockPart>().IsLocked);Assert.AreEqual(open,part.IsOpen);Assert.AreEqual(open?"/":"+",door.GetPart<RenderPart>().RenderString);Assert.AreEqual(!open,part.IsClosed);
        }
        [TestCase("distant")][TestCase("foreign-owner")][TestCase("removed")][TestCase("stunned")]
        public void OptionalOrdinaryDoorLockCannotBypassDoorAuthority(string fault)
        {
            door.AddPart(new LockPart{IsLocked=true,KeyId=""});
            if(fault=="distant")zone.MoveEntity(actor,2,2);
            if(fault=="foreign-owner")part.OwnerId="other";
            if(fault=="removed")zone.RemoveEntity(door);
            if(fault=="stunned")actor.ApplyEffect(new StunnedEffect(3));
            var e=GameEvent.New("InventoryAction");e.SetParameter("Actor",(object)actor);e.SetParameter("Zone",(object)zone);e.SetParameter("Command","Unlock");door.FireEvent(e);bool handled=e.Handled;e.Release();
            Assert.True(door.GetPart<LockPart>().IsLocked);Assert.False(handled,"refused ordinary door action must remain free");Assert.False(part.IsOpen);
        }

    }
}
