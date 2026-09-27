using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class DensityLiquidVesselTests
 {
  EntityFactory factory,oldFactory;Entity actor;Zone zone;Dictionary<string,LiquidDefinition> saved;bool initialized;
  static FieldInfo Registry=>typeof(LiquidRegistry).GetField("_byId",BindingFlags.Static|BindingFlags.NonPublic);
  static FieldInfo Init=>typeof(LiquidRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
  [SetUp] public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
   factory.LoadBlueprints("{\"Objects\":[{\"Name\":\"PouredLiquidPool\",\"Parts\":[{\"Name\":\"Render\",\"Params\":[{\"Key\":\"DisplayName\",\"Value\":\"poured liquid\"}]},{\"Name\":\"Physics\",\"Params\":[]},{\"Name\":\"LiquidPool\",\"Params\":[]}],\"Tags\":[{\"Key\":\"Terrain\",\"Value\":\"\"}]}]}");
   oldFactory=MaterialReactionResolver.Factory;MaterialReactionResolver.Factory=factory;
   saved=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Registry.GetValue(null));initialized=(bool)Init.GetValue(null);
   LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
   actor=factory.CreateEntity("Player");zone=new Zone("liquid-vessel-tests");Assert.True(zone.AddEntity(actor,10,10));}
  [TearDown]public void Cleanup(){MaterialReactionResolver.Factory=oldFactory;var d=(Dictionary<string,LiquidDefinition>)Registry.GetValue(null);d.Clear();foreach(var p in saved)d[p.Key]=p.Value;Init.SetValue(null,initialized);}
  Entity Flask(string id="",int volume=0,int capacity=12){var e=factory.CreateEntity("PhysicalObject");e.SetTag("Item");e.GetPart<PhysicsPart>().Takeable=true;
   var type=typeof(Part).Assembly.GetType("CavesOfOoo.Core.LiquidVesselPart");Assert.NotNull(type,"general liquid carry/pour Part is missing");var p=(Part)Activator.CreateInstance(type);Set(p,"LiquidId",id);Set(p,"Volume",volume);Set(p,"Capacity",capacity);e.AddPart(p);Assert.True(actor.GetPart<InventoryPart>().AddObject(e));return e;}
  Entity Pool(string id,int volume,int x=11,int y=10){var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="fixture-pool"};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());e.AddPart(new LiquidPoolPart{LiquidId=id,Volume=volume});Assert.True(zone.AddEntity(e,x,y));return e;}
  static void Set(Part p,string n,object v)=>p.GetType().GetField(n).SetValue(p,v);
  static T Get<T>(Entity e,string n)=>(T)e.GetPart("LiquidVessel").GetType().GetField(n).GetValue(e.GetPart("LiquidVessel"));
  string Fill(Entity flask,Entity pool)=>InventorySystem.GetActions(actor,flask).Single(a=>a.Name=="FillLiquid"&&a.Command.Split('|')[1]==Uri.EscapeDataString(pool.ID)).Command;
  string Pour(Entity flask,int x,int y)=>InventorySystem.GetActions(actor,flask).Single(a=>a.Name=="PourLiquid"&&a.Command.EndsWith("|"+x+"|"+y)).Command;
  bool Act(Entity e,string command)=>InventorySystem.PerformAction(actor,e,command,zone);
  [TestCase("water")][TestCase("oil")][TestCase("acid")]
  public void ActualSelectedPoolFillsVesselWithoutChangingIdentityOrCreatingVolume(string id){var f=Flask();var p=Pool(id,20);Assert.True(Act(f,Fill(f,p)));Assert.AreEqual(id,Get<string>(f,"LiquidId"));Assert.AreEqual(12,Get<int>(f,"Volume"));Assert.AreEqual(8,p.GetPart<LiquidPoolPart>().Volume);Assert.False(InventorySystem.GetActions(actor,f).Any(a=>a.Command.Contains("Drink")));}
  [Test]public void FillPartialVolumeAndPourGroundConserveBothSides(){var f=Flask();var p=Pool("oil",5);Assert.True(Act(f,Fill(f,p)));Assert.True(Act(f,Pour(f,10,11)));var poured=zone.GetCell(10,11).Objects.Single(e=>e.BlueprintName=="PouredLiquidPool");Assert.AreEqual("oil",poured.GetPart<LiquidPoolPart>().LiquidId);Assert.AreEqual(5,poured.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(0,p.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(0,Get<int>(f,"Volume"));Assert.AreEqual("",Get<string>(f,"LiquidId"));Assert.True(zone.TileState.HasCoating(10,11,"oil"));}
  [Test]public void LikeLiquidPourMergesWithoutDuplicateGroundPool(){var f=Flask("water",5);var p=Pool("water",7,10,11);Assert.True(Act(f,Pour(f,10,11)));Assert.AreEqual(12,p.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(1,zone.GetCell(10,11).Objects.Count(e=>e.HasPart<LiquidPoolPart>()));}
  [Test]public void UnlikeLiquidCannotReplaceOrCleanseVessel(){var f=Flask("acid",5);var p=Pool("water",20);Assert.False(InventorySystem.GetActions(actor,f).Any(a=>a.Name=="FillLiquid"));Assert.False(Act(f,"FillLiquidVessel|"+p.ID+"|water"));Assert.AreEqual("acid",Get<string>(f,"LiquidId"));Assert.AreEqual(5,Get<int>(f,"Volume"));Assert.AreEqual(20,p.GetPart<LiquidPoolPart>().Volume);}
  [Test]public void StaleSourceSelectionRefusesInsteadOfUsingDifferentNearbyPool(){var f=Flask();var p=Pool("oil",7);string command=Fill(f,p);zone.RemoveEntity(p);Pool("water",30);Assert.False(Act(f,command));Assert.AreEqual(0,Get<int>(f,"Volume"));Assert.AreEqual("",Get<string>(f,"LiquidId"));}
  [Test]public void SourceIdentityChangeCannotSilentlySubstituteAnotherLiquid(){var f=Flask();var p=Pool("oil",7);string command=Fill(f,p);p.GetPart<LiquidPoolPart>().LiquidId="acid";Assert.False(Act(f,command));Assert.AreEqual(0,Get<int>(f,"Volume"));Assert.AreEqual(7,p.GetPart<LiquidPoolPart>().Volume);}
  [Test]public void PureWaterPourCannotBecomePickableWaterInsideAnAcidPool(){var f=Flask("water",5);string command=Pour(f,10,11);var acid=Pool("acid",7,10,11);Assert.False(Act(f,command));Assert.AreEqual(5,Get<int>(f,"Volume"));Assert.AreEqual(7,acid.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(1,zone.GetCell(10,11).Objects.Count);}
  [Test]public void PourAtFeetUsesExistingContactLiquidAndDoesNotDrink(){var f=Flask("water",5);actor.ApplyEffect(new ParchedEffect());Assert.True(Act(f,Pour(f,10,10)));Assert.AreEqual("water",actor.GetEffect<LiquidCoveredEffect>()?.LiquidId);Assert.NotNull(actor.GetEffect<WetEffect>());Assert.AreEqual(0,Get<int>(f,"Volume"));}
  [TestCase(false)][TestCase(true)]public void FreshWaterSkinCannotDrawAnOverlappingUnsafePool(bool tileOnly){var skin=factory.CreateEntity("Waterskin");Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));var water=Pool("water",5);if(tileOnly)zone.TileState.WriteCoating(11,10,"acid",5);else Pool("acid",5);Assert.False(Act(skin,"FillWaterskin"));Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(5,water.GetPart<LiquidPoolPart>().Volume);}
  [Test]public void ExistingPureWaterSkinLoopRemainsValid(){var skin=factory.CreateEntity("Waterskin");Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));var water=Pool("water",5);Assert.True(Act(skin,"FillWaterskin"));Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Charges);Assert.True(Act(skin,"DrinkWaterskin"));Assert.AreEqual(2,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(2,water.GetPart<LiquidPoolPart>().Volume);}
 }
}
