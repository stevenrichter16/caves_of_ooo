using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class DensityEverydayAdversarialTests
    {
        EntityFactory factory,oldFactory; Entity actor; Zone zone; Action<string> oldMessage;
        [SetUp] public void Setup()
        {
            factory=new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            oldFactory=MaterialReactionResolver.Factory; MaterialReactionResolver.Factory=factory; oldMessage=MessageLog.OnMessage;
            actor=new Entity { ID="adversarial-cook",BlueprintName="Player" }; actor.AddPart(new InventoryPart()); actor.AddPart(new StatusEffectsPart());
            foreach(string n in new[]{"Strength","Agility","Speed"}) actor.Statistics[n]=new Stat {Owner=actor,Name=n,BaseValue=20,Max=200};
            zone=new Zone("EverydayAdversarial"); zone.AddEntity(actor,10,10); MessageLog.Clear(); Diag.ResetAll();
        }
        [TearDown] public void Cleanup(){MaterialReactionResolver.Factory=oldFactory;MessageLog.OnMessage=oldMessage;}
        Entity Carry(string name,int count=1){var item=factory.CreateEntity(name);if(item.HasPart<StackerPart>())item.GetPart<StackerPart>().StackCount=count;Assert.True(actor.GetPart<InventoryPart>().AddObject(item));return item;}
        Entity Fire(){var fire=factory.CreateEntity("Campfire");zone.AddEntity(fire,11,10);return fire;}
        Entity Water(int units=3){var e=new Entity { ID="pool" };e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=units});zone.AddEntity(e,11,10);return e;}
        bool Act(Entity item,string command)=>InventorySystem.PerformAction(actor,item,command,zone);
        int Count(string name)=>actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==name).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        int Records(string kind)=>DiagQuery.Apply(new DiagQuery.Filter{Kind=kind,Limit=100}).Records.Count;

        [TestCase("FillWaterskin","WaterskinFilled")][TestCase("DrinkWaterskin","WaterskinDrunk")][TestCase("Cook","FoodCooked")]
        public void CommitMessageObserverThrows_ResultAndReceiptStillPublish(string command,string record)
        {
            var item=Carry(command=="Cook"?"RawMeat":"Waterskin"); if(command=="Cook")Fire();else{Water();item.GetPart<WaterskinPart>().Charges=1;}
            MessageLog.OnMessage=text=>{if(text.StartsWith("You fill")||text.StartsWith("You drink")||text.StartsWith("You cook"))throw new InvalidOperationException("receipt callback");};
            Assert.True(Act(item,command)); Assert.AreEqual(1,Records(record)); Assert.AreEqual(1,Records("InventoryCommitObserverFailed"));
            Assert.AreEqual(command=="Cook"?1:0,Count("CookedMeat"));
        }
        [TestCase(-1,3)][TestCase(4,3)][TestCase(0,0)][TestCase(0,-1)]
        public void MalformedVesselState_RefusesWithoutRepairingOrDuplicating(int charges,int capacity)
        {var skin=Carry("Waterskin");var part=skin.GetPart<WaterskinPart>();part.Charges=charges;part.Capacity=capacity;var water=Water();Assert.False(Act(skin,"FillWaterskin"));Assert.False(Act(skin,"DrinkWaterskin"));Assert.AreEqual(charges,part.Charges);Assert.AreEqual(capacity,part.Capacity);Assert.AreEqual(3,water.GetPart<LiquidPoolPart>().Volume);}
        [Test] public void MalformedStackedVessel_CannotMultiplyCharges()
        {var skin=Carry("Waterskin");skin.AddPart(new StackerPart{StackCount=2});skin.GetPart<WaterskinPart>().Charges=2;Water();Assert.False(Act(skin,"DrinkWaterskin"));Assert.False(Act(skin,"FillWaterskin"));Assert.AreEqual(2,skin.GetPart<WaterskinPart>().Charges);}
        [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")][TestCase("Cook")]
        public void ActorVeto_LeavesAllResourcesAndSuccessReceiptsUntouched(string command)
        {var skin=Carry("Waterskin");skin.GetPart<WaterskinPart>().Charges=1;var raw=Carry("RawMeat",2);var pool=Water();Fire();actor.AddPart(new VetoPart());Assert.False(Act(command=="Cook"?raw:skin,command));Assert.AreEqual(1,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(3,pool.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(2,Count("RawMeat"));Assert.AreEqual(0,Records("WaterskinFilled")+Records("WaterskinDrunk")+Records("FoodCooked"));}
        [TestCase(false)][TestCase(true)] public void ReentrantDifferentVessel_CannotSpendDuringAnUncommittedAction(bool throws)
        {
            var first=Carry("Waterskin");var second=Carry("Waterskin");first.GetPart<WaterskinPart>().Charges=2;second.GetPart<WaterskinPart>().Charges=2;
            bool? nested=null; actor.AddPart(new CallbackPart{Callback=()=>{nested=Act(second,"DrinkWaterskin");if(throws)throw new InvalidOperationException("rollback");}});
            Assert.AreEqual(!throws,Act(first,"DrinkWaterskin")); Assert.AreEqual(false,nested);
            Assert.AreEqual(throws?2:1,first.GetPart<WaterskinPart>().Charges); Assert.AreEqual(2,second.GetPart<WaterskinPart>().Charges);
        }
        [Test] public void Fill_RollsBackPoolAndVesselUnderOuterTransaction()
        {var skin=Carry("Waterskin");var pool=Water();var tx=new InventoryTransaction();Assert.True(WaterVesselService.TryAct(actor,skin,zone,"FillWaterskin",tx));Assert.AreEqual(0,Records("WaterskinFilled"));tx.Rollback();Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(3,pool.GetPart<LiquidPoolPart>().Volume);Assert.True(Act(skin,"FillWaterskin"));}
        [Test] public void DrinkRollback_RestoresParchedIdentityWithoutRemovingUnrelatedEffect()
        {
            var skin=Carry("Waterskin");skin.GetPart<WaterskinPart>().Charges=1;var effects=actor.GetPart<StatusEffectsPart>();var parched=new ParchedEffect();effects.ForceApplyEffect(parched);
            var tx=new InventoryTransaction();Assert.True(WaterVesselService.TryAct(actor,skin,zone,"DrinkWaterskin",tx));
            var poison=new PoisonedEffect(5);effects.ForceApplyEffect(poison);tx.Rollback();Assert.AreSame(parched,effects.GetEffect<ParchedEffect>());Assert.AreSame(poison,effects.GetEffect<PoisonedEffect>());Assert.AreEqual(1,actor.GetStat("Strength").Penalty);Assert.AreEqual(1,skin.GetPart<WaterskinPart>().Charges);
        }
        [TestCase(1)][TestCase(3)] public void DrinkOuterRollback_PreservesIndependentStrengthPenalty(int stacks)
        {
            var skin=Carry("Waterskin");skin.GetPart<WaterskinPart>().Charges=1;var effects=actor.GetPart<StatusEffectsPart>();effects.ForceApplyEffect(new ParchedEffect{Stacks=stacks});
            var tx=new InventoryTransaction();Assert.True(WaterVesselService.TryAct(actor,skin,zone,"DrinkWaterskin",tx));
            effects.ForceApplyEffect(new WeakenedEffect(2,5));tx.Rollback();
            Assert.AreEqual(stacks+2,actor.GetStat("Strength").Penalty);Assert.AreEqual(stacks,actor.GetStat("Agility").Penalty);
            Assert.AreEqual(stacks,effects.GetEffect<ParchedEffect>().Stacks);Assert.NotNull(effects.GetEffect<WeakenedEffect>());
        }
        [Test] public void StandingSourceReach_UsesFootprintRatherThanRemoteAnchor()
        {var skin=Carry("Waterskin");var well=new Entity();well.AddPart(new WellPart());well.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0;2,0;3,0"});Assert.True(zone.AddEntity(well,6,10));Assert.True(Act(skin,"FillWaterskin"));Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Charges);}
        [Test] public void PoolWithRenewingWaterCoating_StillConservesItsFiniteVolume()
        {var skin=Carry("Waterskin");var water=Water(1);water.AddPart(new TileStateSourcePart{Coating="water"});Assert.True(Act(skin,"FillWaterskin"));Assert.AreEqual(1,skin.GetPart<WaterskinPart>().Charges);Assert.False(Act(skin,"FillWaterskin"));}
        [Test] public void SaveLoadInventoryGraph_PreservesSeparateVesselIdentitiesAndCharges()
        {var a=Carry("Waterskin");var b=Carry("Waterskin");a.GetPart<WaterskinPart>().Charges=1;b.GetPart<WaterskinPart>().Charges=3;var restored=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);var all=restored.GetPart<InventoryPart>().Objects;Assert.AreEqual(2,all.Count);CollectionAssert.AreEqual(new[]{1,3},all.Select(x=>x.GetPart<WaterskinPart>().Charges));Assert.AreNotSame(all[0],all[1]);foreach(var item in all)Assert.AreSame(restored,item.GetPart<PhysicsPart>().InInventory);}
        [Test] public void Cooking_SplitsAcrossMaxStackWithoutLosingQuantity()
        {Carry("CookedMeat",98);var raw=Carry("RawMeat",4);Fire();Assert.True(Act(raw,"Cook"));Assert.AreEqual(102,Count("CookedMeat"));CollectionAssert.AreEqual(new[]{99,3},actor.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="CookedMeat").Select(e=>e.GetPart<StackerPart>().StackCount));}
        [Test] public void CookingOuterRollback_RestoresCountsOwnershipAndHandling()
        {var raw=Carry("RawMeat",3);var old=Carry("CookedMeat",98);Fire();var inventory=actor.GetPart<InventoryPart>();var before=inventory.Objects.ToArray();var tx=new InventoryTransaction();Assert.True(CookingService.TryCook(actor,raw,zone,factory,tx));Assert.AreEqual(101,Count("CookedMeat"));tx.Rollback();CollectionAssert.AreEqual(before,inventory.Objects);Assert.AreEqual(3,Count("RawMeat"));Assert.AreEqual(98,Count("CookedMeat"));Assert.AreSame(actor,raw.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(0,Records("FoodCooked"));}
        [TestCase(0)][TestCase(-1)][TestCase(100)][TestCase(int.MaxValue)]
        public void CookingMalformedQuantity_RefusesBeforeOutputAllocation(int count)
        {var raw=Carry("RawMeat");raw.GetPart<StackerPart>().StackCount=count;Fire();Assert.False(Act(raw,"Cook"));Assert.AreEqual(count,raw.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Count("CookedMeat"));}
        [Test] public void CookingNoFoodOutput_RefusesAndLeavesRecipeUntouched()
        {var raw=Carry("RawMeat",2);raw.GetPart<CookablePart>().Into="Dagger";Fire();Assert.False(Act(raw,"Cook"));Assert.AreEqual(2,Count("RawMeat"));Assert.AreEqual(0,Count("Dagger"));Assert.AreEqual("Dagger",raw.GetPart<CookablePart>().Into);}
        [Test] public void OldSavedFoodWithoutCookablePart_IsNotSilentlyMigrated()
        {var raw=Carry("RawMeat",2);raw.RemovePart(raw.GetPart<CookablePart>());var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(raw);Assert.Null(loaded.GetPart<CookablePart>());Assert.False(InventorySystem.GetActions(actor,loaded).Any(x=>x.Command=="Cook"));}
        [Test] public void ClockAdvanceDoesNotSimulateAdditionalHungerOrActorTurns()
        {var previous=TurnManager.Active;try{var tm=new TurnManager();actor.SetTag("Player");actor.AddPart(new PhysicsPart());actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=1,Max=20};var spy=new TurnSpyPart();actor.AddPart(spy);Assert.True(RestSystem.TryRestUntilNextBand(actor,zone,"test",out _));Assert.AreEqual(300,tm.TickCount);Assert.AreEqual(0,spy.TurnEvents);}finally{typeof(TurnManager).GetProperty("Active").SetValue(null,previous);}}
        public class VetoPart:Part{public override string Name=>"EverydayVeto";public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeInventoryAction";}
        public class CallbackPart:Part{public override string Name=>"EverydayCallback";public Action Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")Callback();return true;}}
        public class TurnSpyPart:Part{public override string Name=>"EverydayTurnSpy";public int TurnEvents;public override bool HandleEvent(GameEvent e){if(e.ID=="EndTurn"||e.ID=="TickEnd")TurnEvents++;return true;}}
    }
}
