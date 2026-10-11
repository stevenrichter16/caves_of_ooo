using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioEventTests
    {
        GameObject root; AudioClip clip; WorldAudioPlayer audio; WorldAudioEventRouter router;
        Zone zone; Entity actor, foe; float oldSfx; bool oldPause;
        [SetUp] public void Setup()
        {
            oldSfx=WorldAudioSettings.SfxVolume;oldPause=AudioListener.pause;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            root=new GameObject("world-audio-event-test");clip=AudioClip.Create("test cue",48000,1,48000,false);
            var library=new WorldAudioLibrary();foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))library.AddSound(cue,clip,.4f);
            audio=new WorldAudioPlayer(root.transform,library);router=new WorldAudioEventRouter(audio);
            zone=new Zone("Overworld.10.10.0");foreach(var cell in zone.Cells){cell.Explored=true;cell.IsVisible=true;}
            actor=Actor("player");actor.SetTag("Player");foe=Actor("foe");
            Assert.True(zone.AddEntity(actor,10,10));Assert.True(zone.AddEntity(foe,11,10));router.SetWorld(zone,actor,true);
        }
        [TearDown] public void Cleanup()
        {
            router.Dispose();audio.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);
            WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;
        }
        static Entity Actor(string id)
        {
            var e=new Entity{ID=id};e.SetTag("Creature");e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());e.AddPart(new InventoryPart());
            e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Min=0,Max=20};
            e.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=16};return e;
        }
        [Test] public void ActualMovementPublishesOneStepButBlockedMovementPublishesNone()
        {
            Assert.True(MovementSystem.TryMove(actor,zone,0,1));Assert.AreEqual(1,audio.AcceptedSoundCount);
            var wall=new Entity{ID="wall"};wall.SetTag("Wall");wall.AddPart(new PhysicsPart{Solid=true});Assert.True(zone.AddEntity(wall,10,12));
            audio.Update(.3f);Assert.False(MovementSystem.TryMove(actor,zone,0,1));Assert.AreEqual(1,audio.AcceptedSoundCount);
        }
        [TestCase("forced")][TestCase("unchanged")][TestCase("hidden")][TestCase("off_zone")][TestCase("removed")][TestCase("world_hidden")]
        public void InvalidMoveSignalCannotRevealOrInventStep(string fault)
        {
            int x=9,y=10;Zone at=zone;bool forced=false;
            if(fault=="forced")forced=true;if(fault=="unchanged")x=10;
            if(fault=="hidden")zone.GetCell(10,10).IsVisible=false;
            if(fault=="off_zone")at=new Zone("other");if(fault=="removed")zone.RemoveEntity(actor);
            if(fault=="world_hidden")router.SetWorld(zone,actor,false);
            EntityVisualHooks.EmitMoved(actor,at,x,y,10,10,forced);Assert.AreEqual(0,audio.AcceptedSoundCount);
        }
        [Test] public void HitRequiresExplicitMeleeOutcomeAndDoesNotDuplicateGenericSpellDamage()
        {
            EntityVisualHooks.EmitDamage(foe,actor,zone,3,false);Assert.AreEqual(0,audio.AcceptedSoundCount);
            EntityVisualHooks.EmitAttack(actor,foe,zone);Assert.AreEqual(1,audio.AcceptedSoundCount);
            EntityVisualHooks.EmitDamage(foe,actor,zone,3,false);Assert.AreEqual(1,audio.AcceptedSoundCount);
            WorldAudioHooks.Emit(WorldAudioCue.Hit,actor,zone,11,10);Assert.AreEqual(2,audio.AcceptedSoundCount);
            audio.Update(.3f);EntityVisualHooks.EmitDamage(foe,actor,zone,3,false);Assert.AreEqual(2,audio.AcceptedSoundCount);
        }
        [Test] public void AdditiveSubscriptionSurvivesRouterDisposeWithoutStealingExistingObserver()
        {
            int observed=0;EntityMovedVisualHandler handler=(e,z,ox,oy,nx,ny,forced)=>observed++;
            EntityVisualHooks.MovedCallback+=handler;
            try
            {
                router.Dispose();EntityVisualHooks.EmitMoved(actor,zone,9,10,10,10);
                Assert.AreEqual(1,observed);Assert.AreEqual(0,audio.AcceptedSoundCount);
            }
            finally{EntityVisualHooks.MovedCallback-=handler;}
        }
        [Test] public void OrdinaryDoorSoundFollowsActualStateChangeOnly()
        {
            zone.RemoveEntity(foe);var door=new Entity{ID="door"};door.SetTag("Furniture");door.AddPart(new PhysicsPart());door.AddPart(new RenderPart());
            var part=new DoorPart{IsOpen=false};door.AddPart(part);Assert.True(zone.AddEntity(door,11,10));
            Assert.True(part.TrySetOpen(actor,zone,true));Assert.AreEqual(1,audio.AcceptedSoundCount);
            audio.Update(.3f);Assert.False(part.TrySetOpen(actor,zone,true));Assert.AreEqual(1,audio.AcceptedSoundCount);
            Assert.True(part.TrySetOpen(actor,zone,false));Assert.AreEqual(2,audio.AcceptedSoundCount);
        }
        [Test] public void InventoryCueRequiresSuccessfulRealPickupAndStillWorksOverHiddenWorld()
        {
            var item=new Entity{ID="item"};item.AddPart(new PhysicsPart{Takeable=true});Assert.True(zone.AddEntity(item,10,10));
            Assert.GreaterOrEqual(actor.GetStatValue("Strength"),HandlingService.GetLiftStrengthRequirement(item),"real pickup needs the ordinary one-hand lift allowance");
            router.SetWorld(zone,actor,false);Assert.True(InventorySystem.Pickup(actor,item,zone));Assert.AreEqual(1,audio.AcceptedSoundCount);
            audio.Update(.3f);Assert.False(InventorySystem.Pickup(actor,item,zone));Assert.AreEqual(1,audio.AcceptedSoundCount);
        }
    }
}
