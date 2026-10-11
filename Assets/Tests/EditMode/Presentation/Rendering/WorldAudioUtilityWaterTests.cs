using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Uses the existing real-action fixture and reflection-free string assertions
    // so root can import/execute this fixture before the three enum values exist.
    public sealed class WorldAudioUtilityWaterTests : WorldAudioWorkFixture
    {
        void One(string cue)
        {
            CollectionAssert.AreEqual(new[] { cue + " take 0" }, Assigned());
            Assert.AreEqual(1, Audio.AcceptedSoundCount);
            Assert.AreEqual(16, Audio.AllocatedSources);
            Assert.AreEqual(0, Audio.CombatFocusCount, "water does not activate melee focus");
        }
        string Fill(Entity flask, Entity pool) => Choice(flask, "FillLiquidVessel|", a => a.Command.Split('|')[1] == Id(pool));
        string Transfer(Entity from, Entity to) => Choice(from, "TransferWater|", a => a.Command.EndsWith("|" + Id(to)));
        [Test] public void ActualSkinFillHasOneWaterCueAndConservesTheSource()
        {
            var skin=Skin(); var pool=Pool(8); Assert.True(Act(skin,"FillWaterskin"));
            Assert.AreEqual(3,Units(skin)); Assert.AreEqual(5,pool.GetPart<LiquidPoolPart>().Volume); One("WaterFill");
            Audio.Update(1); Assert.False(Act(skin,"FillWaterskin")); Assert.AreEqual(1,Audio.AcceptedSoundCount);
        }
        [TestCase("pool",false)][TestCase("pool",true)]
        [TestCase("well",false)][TestCase("well",true)]
        [TestCase("spring",false)][TestCase("spring",true)]
        public void RenderedLiteralWaterSourcesRetainOptionalPhysicsContract(string kind,bool physical)
        {
            var source=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="LiteralWaterAudio"};
            source.AddPart(new RenderPart());if(physical)source.AddPart(new PhysicsPart());
            if(kind=="pool")source.AddPart(new LiquidPoolPart{LiquidId="water",Volume=8});
            if(kind=="well")source.AddPart(new WellPart());
            if(kind=="spring")source.AddPart(new TileStateSourcePart{Coating="water",CoatingTurns=8});
            Assert.True(Zone.AddEntity(source,11,10));var skin=Skin();Assert.True(Act(skin,"FillWaterskin"));
            Assert.AreEqual(3,Units(skin));One("WaterFill");
        }
        [Test] public void ActualDrinkUsesWaterRatherThanClothAndSpendsOnlyOne()
        {
            var skin=Skin(1); Actor.ApplyEffect(new ParchedEffect()); Assert.True(Act(skin,"DrinkWaterskin"));
            Assert.AreEqual(0,Units(skin)); Assert.IsNull(Actor.GetEffect<ParchedEffect>()); One("WaterDrink");
            Audio.Update(1); Assert.False(Act(skin,"DrinkWaterskin")); Assert.AreEqual(1,Audio.AcceptedSoundCount);
        }
        [TestCase("refund")][TestCase("relocate")]
        public void FinalParchedRemovalCannotRedefineTheOriginalDrinkReceipt(string change)
        {
            var skin=Skin(2);Actor.ApplyEffect(new ParchedEffect());
            int callbacks=0;var hook=new WaterAudioEffectPart();Actor.AddPart(hook);
            hook.Removed=()=>
            {
                callbacks++;
                if(change=="refund")skin.GetPart<WaterskinPart>().Charges=2;
                else Assert.True(Zone.MoveEntity(Actor,10,9));
            };
            Assert.True(Act(skin,"DrinkWaterskin"),"presentation does not veto existing callback semantics");
            Assert.AreEqual(1,callbacks);Assert.IsNull(Actor.GetEffect<ParchedEffect>());
            Assert.AreEqual(change=="refund"?2:1,Units(skin));Assert.AreEqual(0,Audio.AcceptedSoundCount);
            hook.Removed=()=>callbacks++;Assert.True(Zone.MoveEntity(Actor,10,10));Actor.ApplyEffect(new ParchedEffect());
            Assert.True(Act(skin,"DrinkWaterskin"));Assert.AreEqual(2,callbacks);One("WaterDrink");
        }
        [TestCase(false)][TestCase(true)] public void ActualFreshWaterFlaskFillSoundsWithRetainedOrRetiredPool(bool exhaust)
        {
            var flask=Flask(0,capacity:3); var pool=Pool(exhaust?2:8); Assert.True(Act(flask,Fill(flask,pool)));
            Assert.AreEqual(exhaust?2:3,Units(flask)); Assert.AreEqual(exhaust?0:5,pool.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(exhaust,Zone.GetEntityCell(pool)==null); One("WaterFill");
        }
        [TestCase(false)][TestCase(true)] public void FreshWaterPourUsesTheActualCommittedGroundAmount(bool one)
        {
            var flask=Flask(3); Assert.True(Act(flask,Pour(flask,one)));
            Assert.AreEqual(one?2:0,Units(flask)); Assert.AreEqual(one?1:3,PoolUnits()); One("WaterPour");
        }
        [TestCase(false)][TestCase(true)] public void ActualTransferBetweenCarriedVesselsSoundsOnce(bool toSkin)
        {
            var from=Skin(3); var to=toSkin?Skin(0,2):Flask(0,capacity:2);
            Assert.True(Act(from,Transfer(from,to))); Assert.AreEqual(1,Units(from)); Assert.AreEqual(2,Units(to)); One("WaterPour");
            Audio.Update(1); Assert.False(Act(from,"TransferWater|"+Id(to))); Assert.AreEqual(1,Audio.AcceptedSoundCount);
        }
        [TestCase("fill",false)][TestCase("fill",true)]
        [TestCase("drink",false)][TestCase("drink",true)]
        [TestCase("transfer",false)][TestCase("transfer",true)]
        public void NoWaterSoundUntilTheOwningTransactionCommits(string kind,bool commit)
        {
            var source=Skin(kind=="fill"?0:3); var target=Skin(0,2); Pool(8);
            string command=kind=="fill"?"FillWaterskin":kind=="drink"?"DrinkWaterskin":Transfer(source,target);
            int before=Units(source); var tx=new InventoryTransaction(); Stage(Actor,source,Zone,command,tx);
            Assert.AreNotEqual(before,Units(source)); Assert.IsEmpty(Assigned()); Assert.AreEqual(0,Audio.AcceptedSoundCount);
            if(commit)tx.Commit();else tx.Rollback();
            Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount); Assert.AreEqual(!commit,Units(source)==before);
            tx.Commit(); Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount,"finished transaction cannot replay presentation");
        }
        [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")]
        public void OuterCommandFailureRollsBackWaterAndPublishesNothing(string command)
        {
            var skin=Skin(command=="FillWaterskin"?0:2); var pool=Pool(8); int before=Units(skin);
            FailAfter(); Assert.False(Act(skin,command)); Assert.AreEqual(before,Units(skin)); Assert.AreEqual(8,pool.GetPart<LiquidPoolPart>().Volume);
            Assert.IsEmpty(Assigned()); Assert.AreEqual(0,Audio.AcceptedSoundCount);
        }
        [TestCase("oil")][TestCase("acid")][TestCase("brine")]
        public void NonWaterFlasksDoNotPretendToBeTheWaterFamily(string liquid)
        {
            var flask=Flask(0,capacity:3); var pool=Pool(8,liquid); Assert.True(Act(flask,Fill(flask,pool)));
            Assert.AreEqual(liquid,flask.GetPart<LiquidVesselPart>().LiquidId); Assert.IsEmpty(Assigned());
            Assert.True(Act(flask,Pour(flask,true,10,11))); Assert.IsEmpty(Assigned());
            var skin=Skin(); Pool(8,"water",9,10); Assert.True(Act(skin,"FillWaterskin")); One("WaterFill");
        }
        [TestCase("source-hidden")][TestCase("source-moved")][TestCase("actor-moved")]
        [TestCase("vessel-alias")][TestCase("actor-hidden")]
        public void CallbackChangingCurrentEvidenceSuppressesTheStaleReceipt(string change)
        {
            var skin=Skin(); var pool=Pool(8); var observer=new WaterAudioAfterPart(); Actor.AddPart(observer);
            observer.After=()=>
            {
                if(change=="source-hidden")pool.GetPart<RenderPart>().Visible=false;
                if(change=="source-moved")Zone.MoveEntity(pool,12,10);
                if(change=="actor-moved")Zone.MoveEntity(Actor,10,9);
                if(change=="vessel-alias")skin.GetPart<PhysicsPart>().InInventory=null;
                if(change=="actor-hidden")Actor.GetPart<RenderPart>().Visible=false;
            };
            Assert.True(Act(skin,"FillWaterskin"),"presentation proof does not change gameplay success");
            Assert.AreEqual(3,Units(skin)); Assert.AreEqual(0,Audio.AcceptedSoundCount);
            observer.After=null; pool.GetPart<RenderPart>().Visible=true; Actor.GetPart<RenderPart>().Visible=true;
            Zone.MoveEntity(pool,11,10); Zone.MoveEntity(Actor,10,10); skin.GetPart<PhysicsPart>().InInventory=Actor;
            Assert.True(Act(skin,"DrinkWaterskin")); One("WaterDrink");
        }
        [TestCase(false)][TestCase(true)] public void CommittedPlayerVesselWorkMayFinishInItsMenu(bool menu)
        {
            Router.SetWorld(Zone,Actor,!menu); var skin=Skin(1); Assert.True(Act(skin,"DrinkWaterskin")); One("WaterDrink");
        }
        [TestCase("mute")][TestCase("pause")][TestCase("detached-listener")]
        public void PresentationRefusalDoesNotUndoTheDrinkOrQueueAReplay(string refusal)
        {
            var skin=Skin(2);
            if(refusal=="mute")WorldAudioSettings.SfxVolume=0;
            if(refusal=="pause")AudioListener.pause=true;
            if(refusal=="detached-listener")Router.SetWorld(null,null,true);
            Assert.True(Act(skin,"DrinkWaterskin")); Assert.AreEqual(1,Units(skin)); Assert.AreEqual(0,Audio.AcceptedSoundCount);
            WorldAudioSettings.SfxVolume=1; AudioListener.pause=false; Router.SetWorld(Zone,Actor,true); Audio.Update(1);
            Assert.AreEqual(0,Audio.AcceptedSoundCount); Assert.True(Act(skin,"DrinkWaterskin")); One("WaterDrink");
        }
        [Test] public void AnotherActorsVesselActionCannotUseTheHiddenWorldMenuAllowance()
        {
            var listener=Place("Player",12,10);var skin=Skin(2);
            Router.SetWorld(Zone,listener,false);Assert.True(Act(skin,"DrinkWaterskin"));Assert.AreEqual(1,Units(skin));Assert.AreEqual(0,Audio.AcceptedSoundCount);
            Router.SetWorld(Zone,listener,true);Assert.True(Act(skin,"DrinkWaterskin"));One("WaterDrink");
        }
        [TestCase("WaterFill")][TestCase("WaterDrink")][TestCase("WaterPour")]
        public void WaterCuesCannotBeForgedThroughTheGenericSemanticHook(string name)
        {
            Assert.True(Enum.TryParse(name,out WorldAudioCue cue),"new water cue exists");
            WorldAudioHooks.Emit(cue,Actor,Zone,10,10); Assert.AreEqual(0,Audio.AcceptedSoundCount);
            var skin=Skin(1); Assert.True(Act(skin,"DrinkWaterskin")); One("WaterDrink");
        }
    }
    public sealed class WaterAudioEffectPart : Part
    {
        public Action Removed;
        public override bool HandleEvent(GameEvent e)
        {if(e.ID=="EffectRemoved"&&e.GetParameter<Effect>("Effect") is ParchedEffect)Removed?.Invoke();return true;}
    }
    public sealed class WaterAudioAfterPart : Part
    {
        public Action After;
        public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")After?.Invoke();return true;}
    }
}
