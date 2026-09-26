using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensityEverydayIndependentReviewTests
 {
  EntityFactory factory, oldFactory; Entity actor; Zone zone;
  [SetUp] public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));oldFactory=MaterialReactionResolver.Factory;MaterialReactionResolver.Factory=factory;actor=factory.CreateEntity("Player");zone=new Zone("independent-everyday-review");zone.AddEntity(actor,10,10);var fire=factory.CreateEntity("Campfire");zone.AddEntity(fire,11,10);var water=new Entity();water.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});zone.AddEntity(water,10,11);}
  [TearDown] public void Cleanup(){MaterialReactionResolver.Factory=oldFactory;}
  Entity Carry(string name){var e=factory.CreateEntity(name);Assert.True(actor.GetPart<InventoryPart>().AddObject(e));return e;}
  [TestCase("FillWaterskin",false)][TestCase("DrinkWaterskin",false)][TestCase("Cook",false)]
  [TestCase("FillWaterskin",true)][TestCase("DrinkWaterskin",true)][TestCase("Cook",true)]
  public void DeadActorCannotConsumeOrTransformResources(string command,bool committed)
  {var e=Carry(command=="Cook"?"RawMeat":"Waterskin");if(command=="DrinkWaterskin")e.GetPart<WaterskinPart>().Charges=1;if(committed)actor.SetTag("_DeathHandled");else actor.GetStat("Hitpoints").BaseValue=0;Assert.False(InventorySystem.PerformAction(actor,e,command,zone));Assert.True(actor.GetPart<InventoryPart>().Objects.Contains(e));if(command=="DrinkWaterskin")Assert.AreEqual(1,e.GetPart<WaterskinPart>().Charges);}
  [TestCase("FillWaterskin")][TestCase("DrinkWaterskin")][TestCase("Cook")]
  public void LivingActorCountercheck(string command)
  {var e=Carry(command=="Cook"?"RawMeat":"Waterskin");if(command=="DrinkWaterskin")e.GetPart<WaterskinPart>().Charges=1;Assert.True(InventorySystem.PerformAction(actor,e,command,zone));}
  [TestCase(false)][TestCase(true)] public void NextBandCannotReviveDeadActor(bool committed)
  {var old=TurnManager.Active;try{var t=new TurnManager();if(committed)actor.SetTag("_DeathHandled");else actor.GetStat("Hitpoints").BaseValue=0;int hp=actor.GetStatValue("Hitpoints");Assert.False(RestSystem.TryRestUntilNextBand(actor,zone,"review",out _));Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));Assert.Zero(t.TickCount);}finally{typeof(TurnManager).GetProperty("Active").SetValue(null,old);}}
  [TestCase(false)][TestCase(true)] public void OrdinaryRestCannotReviveDeadActor(bool committed)
  {var old=TurnManager.Active;try{var t=new TurnManager();if(committed)actor.SetTag("_DeathHandled");else actor.GetStat("Hitpoints").BaseValue=0;int hp=actor.GetStatValue("Hitpoints");Assert.False(RestSystem.TryRest(actor,zone,"review",out _));Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));Assert.Zero(t.TickCount);}finally{typeof(TurnManager).GetProperty("Active").SetValue(null,old);}}
  [TestCase(1)][TestCase(2)][TestCase(3)] public void DrinkRollbackPreservesNewParchedExposureWithoutDuplicateOrPenaltyDrift(int stacks)
  {var e=Carry("Waterskin");e.GetPart<WaterskinPart>().Charges=1;actor.ApplyEffect(new ParchedEffect{Stacks=stacks});var effects=actor.GetPart<StatusEffectsPart>();var tx=new InventoryTransaction();try{Assert.True(WaterVesselService.TryAct(actor,e,zone,"DrinkWaterskin",tx));effects.ForceApplyEffect(new ParchedEffect());tx.Rollback();Assert.AreEqual(1,effects.GetAllEffects().Count(x=>x is ParchedEffect));Assert.AreEqual(Math.Min(3,stacks+1),effects.GetEffect<ParchedEffect>().Stacks);Assert.AreEqual(Math.Min(3,stacks+1),actor.GetStat("Strength").Penalty);Assert.AreEqual(Math.Min(3,stacks+1),actor.GetStat("Agility").Penalty);}finally{tx.Rollback();}}
 }
}
