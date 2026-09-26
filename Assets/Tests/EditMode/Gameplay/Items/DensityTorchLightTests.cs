using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityTorchLightTests
    {
        EntityFactory factory; Entity actor,torch; InventoryPart inventory; Zone zone;
        [SetUp] public void Setup()
        {
            factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            actor=new Entity{ID="torch-player"};actor.SetTag("Player");inventory=new InventoryPart();actor.AddPart(inventory);actor.AddPart(new PhysicsPart());actor.AddPart(new StatusEffectsPart());
            actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=20,Max=20};
            zone=new Zone("TorchTest"){AmbientLevel=0.1f};zone.AddEntity(actor,10,10);
            torch=factory.CreateEntity("Torch");Assert.True(inventory.AddObject(torch));MessageLog.Clear();
        }
        void Hold()=>inventory.Equip(torch,"Hand");
        bool Act(string command)=>InventorySystem.PerformAction(actor,torch,command,zone);
        void End(){var e=GameEvent.New("EndTurn");e.SetParameter("Zone",(object)zone);actor.FireEventAndRelease(e);}
        Entity Fire(int x=11){var fire=factory.CreateEntity("Campfire");zone.AddEntity(fire,x,10);return fire;}
        [Test] public void FreshTorch_HasExplicitLightPartAndHandEquipment()
        {Assert.NotNull(torch.GetPart("TorchLight"));Assert.AreEqual("Hand",torch.GetPart<EquippablePart>()?.Slot);}
        [Test] public void OrdinaryEquip_SplitsOneTorchFromStack()
        {torch.GetPart<StackerPart>().StackCount=3;Assert.True(InventorySystem.Equip(actor,torch));Assert.AreEqual(2,torch.GetPart<StackerPart>().StackCount);var held=inventory.EquippedItems.Values.First();Assert.AreNotSame(torch,held);Assert.AreEqual(1,held.GetPart<StackerPart>().StackCount);Assert.NotNull(held.GetPart("TorchLight"));}
        [Test] public void HeldTorch_OffersExtinguish_ThenRelight()
        {Hold();Assert.True(InventorySystem.GetActions(actor,torch).Any(a=>a.Command=="ExtinguishTorch"));Assert.True(Act("ExtinguishTorch"));Assert.True(InventorySystem.GetActions(actor,torch).Any(a=>a.Command=="LightTorch"));}
        [Test] public void Extinguish_ChangesCachedLightWithoutMovement_AndFlickerCannotRelight()
        {
            Hold();var map=new LightMap();map.Compute(zone);Assert.Greater(map.GetBrightness(10,10),0.1f);int version=zone.EntityVersion;
            Assert.True(Act("ExtinguishTorch"));torch.GetPart<LightSourceFlickerPart>().UpdateIntensityAt(5);map.Compute(zone);
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(0.1f,map.GetBrightness(10,10),0.0001f);Assert.AreEqual(25,torch.GetPart<ThermalPart>().Temperature);
        }
        [Test] public void ExtinguishedTorch_RelightsOnlyAtActualHotFire()
        {Hold();Assert.True(Act("ExtinguishTorch"));Assert.False(Act("LightTorch"));var fire=Fire();Assert.True(Act("LightTorch"));Assert.True(torch.GetPart<ThermalPart>().IsAflame);Assert.AreEqual(50,torch.GetPart<FuelPart>().FuelMass);}
        [Test] public void RemoteOrColdFire_DoesNotRelight()
        {Hold();Assert.True(Act("ExtinguishTorch"));var fire=Fire(15);Assert.False(Act("LightTorch"));zone.MoveEntity(fire,11,10);fire.GetPart<ThermalPart>().Temperature=25;Assert.False(Act("LightTorch"));}
        [TestCase(false)][TestCase(true)] public void WetOrExhaustedTorch_RefusesRelight(bool wet)
        {Hold();Assert.True(Act("ExtinguishTorch"));Fire();if(wet)torch.ApplyEffect(new WetEffect(1));else torch.GetPart<FuelPart>().FuelMass=0;Assert.False(Act("LightTorch"));Assert.AreEqual(25,torch.GetPart<ThermalPart>().Temperature);}
        [Test] public void StowedTorch_DoesNotLightTickOrOfferAction()
        {var map=new LightMap();map.Compute(zone);Assert.AreEqual(0.1f,map.GetBrightness(10,10),0.0001f);float fuel=torch.GetPart<FuelPart>().FuelMass;End();Assert.AreEqual(fuel,torch.GetPart<FuelPart>().FuelMass);Assert.False(InventorySystem.GetActions(actor,torch).Any(a=>a.Command=="ExtinguishTorch"));Assert.False(Act("ExtinguishTorch"));}
        [Test] public void HeldTick_ConsumesExactlyOneBurnRateWithoutDamagingTheTool()
        {Hold();End();Assert.AreEqual(49.7f,torch.GetPart<FuelPart>().FuelMass,0.0001f);Assert.AreEqual(5,torch.GetStatValue("Hitpoints"));Assert.AreEqual(20,actor.GetStatValue("Hitpoints"));Assert.False(torch.HasEffect<BurningEffect>());}
        [Test] public void DuplicateEquipmentSlot_TicksOnePhysicalTorchOnlyOnce()
        {Hold();inventory.EquippedItems["OtherHand"]=torch;End();Assert.AreEqual(49.7f,torch.GetPart<FuelPart>().FuelMass,0.0001f);}
        [Test] public void ExtinguishedTick_DoesNotConsumeFuelOrHeat()
        {Hold();Assert.True(Act("ExtinguishTorch"));End();Assert.AreEqual(50,torch.GetPart<FuelPart>().FuelMass);Assert.AreEqual(25,torch.GetPart<ThermalPart>().Temperature);}
        [Test] public void LastFuelTick_ExtinguishesAndCannotRelight()
        {Hold();torch.GetPart<FuelPart>().FuelMass=0.1f;End();Assert.AreEqual(0,torch.GetPart<FuelPart>().FuelMass);Assert.AreEqual(25,torch.GetPart<ThermalPart>().Temperature);var map=new LightMap();map.Compute(zone);Assert.AreEqual(0.1f,map.GetBrightness(10,10),0.0001f);Fire();Assert.False(Act("LightTorch"));}
        [Test] public void LooseTorch_MaterialTickConsumesFuel_AndReachIsRechecked()
        {inventory.RemoveObject(torch);zone.AddEntity(torch,11,10);MaterialSimSystem.TickMaterialEntities(zone);Assert.AreEqual(49.7f,torch.GetPart<FuelPart>().FuelMass,0.0001f);Assert.True(Act("ExtinguishTorch"));zone.MoveEntity(torch,20,10);Assert.False(Act("LightTorch"));}
        [TestCase(false)][TestCase(true)] public void DeadActor_CannotExtinguish(bool handled)
        {Hold();if(handled)actor.SetTag("_DeathHandled");else actor.GetStat("Hitpoints").BaseValue=0;Assert.False(Act("ExtinguishTorch"));Assert.AreEqual(450,torch.GetPart<ThermalPart>().Temperature);}
        [Test] public void ForeignOrStaleEquipmentReference_Refuses()
        {torch.GetPart<PhysicsPart>().Equipped=actor;Assert.False(Act("ExtinguishTorch"));inventory.RemoveObject(torch);var other=new Entity();other.AddPart(new InventoryPart());other.GetPart<InventoryPart>().AddObject(torch);other.GetPart<InventoryPart>().Equip(torch,"Hand");Assert.False(Act("ExtinguishTorch"));}
        [Test] public void PostActionFailure_RestoresLightHeatAndFuel()
        {Hold();actor.AddPart(new ThrowAfterPart());Assert.False(Act("ExtinguishTorch"));Assert.AreEqual(450,torch.GetPart<ThermalPart>().Temperature);Assert.AreEqual(50,torch.GetPart<FuelPart>().FuelMass);var map=new LightMap();map.Compute(zone);Assert.Greater(map.GetBrightness(10,10),0.1f);}
        [Test] public void SaveGraph_PreservesOffStateAndRemainingFuel()
        {Hold();End();Assert.True(Act("ExtinguishTorch"));var copy=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);var held=copy.GetPart<InventoryPart>().EquippedItems["Hand"];Assert.AreEqual(49.7f,held.GetPart<FuelPart>().FuelMass,0.0001f);var newZone=new Zone("Loaded"){AmbientLevel=0.1f};newZone.AddEntity(copy,10,10);var map=new LightMap();held.GetPart<LightSourceFlickerPart>().UpdateIntensityAt(1);map.Compute(newZone);Assert.AreEqual(0.1f,map.GetBrightness(10,10),0.0001f);}
        [TestCase("fuel")][TestCase("thermal")][TestCase("light")]
        public void DifferentTorchPayload_DoesNotMerge(string change)
        {var other=factory.CreateEntity("Torch");if(change=="fuel")other.GetPart<FuelPart>().FuelMass=1;else if(change=="thermal")other.GetPart<ThermalPart>().Temperature=25;else{Hold();Assert.True(Act("ExtinguishTorch"));}Assert.False(torch.GetPart<StackerPart>().CanStackWith(other));}
        [Test] public void IdenticalFreshTorches_StillStack()
        {Assert.True(torch.GetPart<StackerPart>().CanStackWith(factory.CreateEntity("Torch")));}
        [Test] public void OtherLights_RemainUntouched()
        {var sword=factory.CreateEntity("FlamingSword");Assert.Null(sword.GetPart("TorchLight"));Assert.False(InventorySystem.GetActions(actor,sword).Any(a=>a.Command=="ExtinguishTorch"));}
        public sealed class ThrowAfterPart:Part{public override string Name=>"TorchRollback";public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")throw new InvalidOperationException("torch rollback");return true;}}
    }
}
