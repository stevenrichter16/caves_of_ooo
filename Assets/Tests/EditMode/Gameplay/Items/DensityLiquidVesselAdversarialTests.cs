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
 public sealed class DensityLiquidVesselAdversarialTests
 {
  EntityFactory factory,oldFactory; Entity actor; Zone zone; Dictionary<string,LiquidDefinition> saved;bool initialized;
  static FieldInfo Registry=>typeof(LiquidRegistry).GetField("_byId",BindingFlags.Static|BindingFlags.NonPublic);
  static FieldInfo Init=>typeof(LiquidRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
  [SetUp]public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
   factory.LoadBlueprints("{\"Objects\":[{\"Name\":\"PouredLiquidPool\",\"Parts\":[{\"Name\":\"Render\",\"Params\":[{\"Key\":\"DisplayName\",\"Value\":\"poured liquid\"}]},{\"Name\":\"Physics\",\"Params\":[]},{\"Name\":\"LiquidPool\",\"Params\":[]}],\"Tags\":[{\"Key\":\"Terrain\",\"Value\":\"\"}]}]}");
   oldFactory=MaterialReactionResolver.Factory;MaterialReactionResolver.Factory=factory;saved=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Registry.GetValue(null));initialized=(bool)Init.GetValue(null);
   LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));actor=factory.CreateEntity("Player");zone=new Zone("liquid-vessel-adversarial");Assert.True(zone.AddEntity(actor,10,10));}
  [TearDown]public void Cleanup(){FactoryProbe.Callback=null;MaterialReactionResolver.Factory=oldFactory;var d=(Dictionary<string,LiquidDefinition>)Registry.GetValue(null);d.Clear();foreach(var p in saved)d[p.Key]=p.Value;Init.SetValue(null,initialized);}
  Entity Flask(string id="",int volume=0,int capacity=12){var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.SetTag("Item");e.AddPart(new PhysicsPart{Takeable=true});e.AddPart(new LiquidVesselPart{LiquidId=id,Volume=volume,Capacity=capacity});Assert.True(actor.GetPart<InventoryPart>().AddObject(e));return e;}
  Entity Pool(string id,int volume,int x=11,int y=10){var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.AddPart(new PhysicsPart());e.AddPart(new LiquidPoolPart{LiquidId=id,Volume=volume});Assert.True(zone.AddEntity(e,x,y));return e;}
  string Fill(Entity flask,Entity pool)=>InventorySystem.GetActions(actor,flask).Single(a=>a.Name=="FillLiquid"&&a.Command.Split('|')[1]==Uri.EscapeDataString(pool.ID)).Command;
  string Pour(Entity flask,int x,int y)=>InventorySystem.GetActions(actor,flask).Single(a=>a.Name=="PourLiquid"&&a.Command.EndsWith("|"+x+"|"+y)).Command;
  bool Act(Entity item,string command)=>InventorySystem.PerformAction(actor,item,command,zone);
  [TestCase("acid")][TestCase("oil")]
  public void RenewingWaterSpringCannotBypassPollutedCellSafety(string unsafeId){var skin=factory.CreateEntity("Waterskin");Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));var spring=new Entity();spring.AddPart(new TileStateSourcePart{Coating="water",CoatingTurns=3});zone.AddEntity(spring,11,10);Pool(unsafeId,5);Assert.False(Act(skin,"FillWaterskin"));Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges);}
  [Test]public void WellDrawRemainsProtectedFromUnrelatedGroundSpill(){var skin=factory.CreateEntity("Waterskin");Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));var well=new Entity();well.AddPart(new WellPart());zone.AddEntity(well,11,10);Pool("acid",5);Assert.True(Act(skin,"FillWaterskin"));Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Charges);}
  [Test]public void PreparedGroundBlueprintCannotMintItsOwnInitialVolume(){var f=Flask("water",5);string pour=Pour(f,10,11);factory.Blueprints[LiquidVesselService.PoolBlueprint].Parts["LiquidPool"]["Volume"]="7";Assert.False(Act(f,pour));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.False(zone.GetCell(10,11).Objects.Any());}
  [TestCase(true)][TestCase(false)]public void OuterRollbackRestoresBothSidesAndDoesNotPublishContact(bool existing){var f=Flask("water",5);var p=existing?Pool("water",7,10,10):null;string command=Pour(f,10,10);var tx=new InventoryTransaction();Assert.True(LiquidVesselService.TryAct(actor,f,zone,command,tx));Assert.Null(actor.GetEffect<WetEffect>());tx.Rollback();Assert.AreEqual(existing,zone.TileState.HasCoating(10,10,"water"),"rollback restores the pool-owned projection");Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual("water",f.GetPart<LiquidVesselPart>().LiquidId);Assert.AreEqual(existing?1:0,zone.GetCell(10,10).Objects.Count(e=>e.HasPart<LiquidPoolPart>()));if(existing)Assert.AreEqual(7,p.GetPart<LiquidPoolPart>().Volume);}
  [TestCase("takeable")][TestCase("inventory")][TestCase("equipped")]
  public void StaleGroundDestinationCannotBeCarriedOrTakeable(string changed){var f=Flask("water",5);var p=Pool("water",7,10,11);string command=Pour(f,10,11);var physics=p.GetPart<PhysicsPart>();if(changed=="takeable")physics.Takeable=true;if(changed=="inventory")physics.InInventory=new Entity();if(changed=="equipped")physics.Equipped=new Entity();Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(7,p.GetPart<LiquidPoolPart>().Volume);}
  [Test]public void PreparedGroundBlueprintMustBeAnEmptyUnidentifiedShell(){var f=Flask("water",5);string command=Pour(f,10,11);factory.Blueprints[LiquidVesselService.PoolBlueprint].Parts["LiquidPool"]["LiquidId"]="oil";Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.IsEmpty(zone.GetCell(10,11).Objects);}
  [Test]public void RolledBackNewPoolRestoresPriorCoatingLease(){var f=Flask("water",5);zone.TileState.WriteCoating(10,11,"water",2);var tx=new InventoryTransaction();Assert.True(LiquidVesselService.TryAct(actor,f,zone,Pour(f,10,11),tx));tx.Rollback();Assert.AreEqual(2,zone.TileState.CoatingTurns(10,11,"water"));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.IsEmpty(zone.GetCell(10,11).Objects);}
  [Test]public void FillOuterRollbackRestoresSourceIdentityAndAmount(){var f=Flask();var p=Pool("acid",7);var tx=new InventoryTransaction();Assert.True(LiquidVesselService.TryAct(actor,f,zone,Fill(f,p),tx));tx.Rollback();Assert.AreEqual("",f.GetPart<LiquidVesselPart>().LiquidId);Assert.AreEqual(0,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(7,p.GetPart<LiquidPoolPart>().Volume);}
  [Test]public void DestinationIntegerOverflowCannotEmptyVessel(){var f=Flask("oil",12);var p=Pool("oil",int.MaxValue,10,11);Assert.False(Act(f,Pour(f,10,11)));Assert.AreEqual(12,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(int.MaxValue,p.GetPart<LiquidPoolPart>().Volume);}
  [TestCase("before")][TestCase("after")]
  public void ActorCallbackRefusalOrExceptionDoesNotLeaveLiquidBehind(string when){var f=Flask("water",5);string command=Pour(f,10,10);actor.AddPart(new Veto{When=when});Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.Null(actor.GetEffect<WetEffect>());Assert.False(zone.GetCell(10,10).Objects.Any(e=>e.HasPart<LiquidPoolPart>()));}
  [TestCase(-1,12,"water")][TestCase(13,12,"water")][TestCase(0,0,"")][TestCase(1,12,"")][TestCase(0,12,"water")][TestCase(1,12,"missing-liquid")]
  public void MalformedStateRefusesWithoutRepair(int volume,int capacity,string id){var f=Flask(id,volume,capacity);Assert.IsEmpty(InventorySystem.GetActions(actor,f));Assert.False(Act(f,"PourLiquidVessel|water|1|liquid-vessel-adversarial|10|11"));Assert.AreEqual(volume,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(id,f.GetPart<LiquidVesselPart>().LiquidId);}
  [Test]public void StalePourAfterContentsChangeDoesNotRedirectOrSpend(){var f=Flask("oil",5);string command=Pour(f,10,11);f.GetPart<LiquidVesselPart>().LiquidId="acid";Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.IsEmpty(zone.GetCell(10,11).Objects);}
  [TestCase("dead")][TestCase("detached")][TestCase("stacked")][TestCase("ownership")]
  public void StaleVesselOrActorAuthorityIsRevalidated(string change){var f=Flask("water",5);string command=Pour(f,10,11);if(change=="dead")actor.Tags["_DeathHandled"]="";if(change=="detached")zone.RemoveEntity(actor);if(change=="stacked")f.AddPart(new StackerPart{StackCount=2});if(change=="ownership")f.GetPart<PhysicsPart>().InInventory=new Entity();Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.IsEmpty(zone.GetCell(10,11).Objects);}
  [Test]public void SpillSelectionMovedOutOfReachCannotPourRemotely(){var f=Flask("water",5);string command=Pour(f,10,11);Assert.True(zone.MoveEntity(actor,20,20));Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);}
  [TestCase("inventory")][TestCase("equipped")][TestCase("placed")]
  public void FactoryCannotReturnAnAlreadyOwnedGroundPool(string change)
  {
   var f=Flask("water",5); string command=Pour(f,10,11);Entity prepared=null;
   PrepareFactory(e=>{prepared=e;if(change=="inventory")e.GetPart<PhysicsPart>().InInventory=new Entity();if(change=="equipped")e.GetPart<PhysicsPart>().Equipped=new Entity();if(change=="placed")zone.AddEntity(e,20,20);});
   Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(0,prepared.GetPart<LiquidPoolPart>().Volume);Assert.IsEmpty(zone.GetCell(10,11).Objects);
  }
  [TestCase("contents")][TestCase("actor-moved")][TestCase("new-pool")][TestCase("vessel-owner")]
  public void FactoryCallbackCannotRedirectTheCapturedPour(string change)
  {
   var f=Flask("water",5);string command=Pour(f,10,11);
   PrepareFactory(e=>{if(change=="contents")f.GetPart<LiquidVesselPart>().LiquidId="acid";if(change=="actor-moved")zone.MoveEntity(actor,20,20);if(change=="new-pool")Pool("oil",8,10,11);if(change=="vessel-owner")f.GetPart<PhysicsPart>().InInventory=new Entity();});
   Assert.False(Act(f,command));Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.False(zone.GetCell(10,11).Objects.Any(e=>e.BlueprintName==LiquidVesselService.PoolBlueprint));
  }
  [TestCase("water",5,12)][TestCase("acid",7,20)][TestCase("",0,12)]
  public void SavedFlaskKeepsExactSeparateIdentityVolumeAndCapacity(string id,int volume,int capacity)
  {
   var original=Flask(id,volume,capacity);var control=Flask("oil",3);var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(original);var p=loaded.GetPart<LiquidVesselPart>();
   Assert.NotNull(p);Assert.AreEqual(id,p.LiquidId);Assert.AreEqual(volume,p.Volume);Assert.AreEqual(capacity,p.Capacity);Assert.AreNotSame(original,loaded);
   p.Volume=1;Assert.AreEqual(volume,original.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(3,control.GetPart<LiquidVesselPart>().Volume);
  }
  [Test]public void ReadingActionsDoesNotTransferOrCoat()
  {
   var f=Flask("water",5);var pool=Pool("water",8);int before=zone.EntityCount;var first=InventorySystem.GetActions(actor,f);var second=InventorySystem.GetActions(actor,f);
   Assert.AreEqual(first.Count,second.Count);Assert.AreEqual(5,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(8,pool.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(before,zone.EntityCount);Assert.Null(actor.GetEffect<WetEffect>());Assert.False(zone.TileState.HasCoating(10,10,"water"));
  }
  [TestCase("takeable")][TestCase("creature")]
  public void GroundSamplingDoesNotDrainPortableOrLivingPoolOwners(string change)
  {
   var f=Flask();var pool=Pool("water",8);string command=Fill(f,pool);if(change=="takeable")pool.GetPart<PhysicsPart>().Takeable=true;else pool.SetTag("Creature");
   Assert.False(Act(f,command));Assert.AreEqual(0,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(8,pool.GetPart<LiquidPoolPart>().Volume);
  }
  [TestCase("takeable")][TestCase("creature")][TestCase("inventory")]
  public void CleanWaterSkinCannotDrawMalformedOwnedPool(string change)
  {var skin=factory.CreateEntity("Waterskin");actor.GetPart<InventoryPart>().AddObject(skin);var pool=Pool("water",8);if(change=="takeable")pool.GetPart<PhysicsPart>().Takeable=true;if(change=="creature")pool.SetTag("Creature");if(change=="inventory")pool.GetPart<PhysicsPart>().InInventory=new Entity();Assert.False(Act(skin,"FillWaterskin"));Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(8,pool.GetPart<LiquidPoolPart>().Volume);}
  [Test]public void UnpollutedRenewingSpringStillFillsTheCleanWaterSkin()
  {var skin=factory.CreateEntity("Waterskin");actor.GetPart<InventoryPart>().AddObject(skin);var spring=new Entity();spring.AddPart(new TileStateSourcePart{Coating="water",CoatingTurns=3});zone.AddEntity(spring,11,10);Assert.True(Act(skin,"FillWaterskin"));Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Charges);}
  [TestCase("",0,"empty")][TestCase("water",5,"water")][TestCase("oil",7,"oil")][TestCase("acid",2,"acid")]
  public void ExamineReportsLiveFlaskContentsWithoutMutation(string id,int volume,string label)
  {var f=Flask(id,volume);Assert.True(ItemExamineService.TryDescribeDetails(f,out string text));StringAssert.Contains("Contents: "+label,text);StringAssert.Contains(volume+"/12",text);Assert.AreEqual(volume,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(id,f.GetPart<LiquidVesselPart>().LiquidId);}
  [TestCase(13,"water")][TestCase(1,"missing-liquid")]
  public void MalformedFlaskExamineDoesNotOfferUsableContents(int volume,string id)
  {var f=Flask(id,volume);Assert.True(ItemExamineService.TryDescribeDetails(f,out string text));StringAssert.Contains("unavailable",text);Assert.AreEqual(volume,f.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(id,f.GetPart<LiquidVesselPart>().LiquidId);}
  [Test]public void RemovingItemTagKeepsTheExistingExamineBoundary()
  {var f=Flask("water",5);f.Tags.Remove("Item");Assert.False(ItemExamineService.TryDescribeDetails(f,out _));}

  [TestCase("water",5,true)][TestCase("oil",5,true)][TestCase("acid",5,true)]
  [TestCase("water",13,false)][TestCase("oil",13,false)][TestCase("acid",13,false)]
  public void FinitePouredOwnerAndProjectionEndOnlyWhenFullyCollected(string id,int amount,bool exhausted)
  {
   var flask=Flask(id,amount,20);Assert.True(Act(flask,Pour(flask,11,10)));
   var pool=zone.GetCell(11,10).Objects.Single(e=>e.BlueprintName==LiquidVesselService.PoolBlueprint);
   Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(11,10,id));
   flask.GetPart<LiquidVesselPart>().Capacity=12;Assert.True(Act(flask,Fill(flask,pool)));
   Assert.AreEqual(Math.Min(12,amount),flask.GetPart<LiquidVesselPart>().Volume);Assert.AreEqual(exhausted,zone.GetEntityCell(pool)==null);
   Assert.AreEqual(!exhausted,zone.TileState.HasCoating(11,10,id));
   if(!exhausted)Assert.AreEqual(1,pool.GetPart<LiquidPoolPart>().Volume);
  }
  [TestCase(true)][TestCase(false)]
  public void ExhaustedPouredCleanupWaitsForOuterTransactionCommit(bool commit)
  {
   var flask=Flask("water",5);Assert.True(Act(flask,Pour(flask,11,10)));var pool=zone.GetCell(11,10).Objects.Single(e=>e.BlueprintName==LiquidVesselService.PoolBlueprint);
   var tx=new InventoryTransaction();Assert.True(LiquidVesselService.TryAct(actor,flask,zone,Fill(flask,pool),tx));Assert.NotNull(zone.GetEntityCell(pool));
   if(commit)tx.Commit();else tx.Rollback();Assert.AreEqual(commit,zone.GetEntityCell(pool)==null);Assert.AreEqual(!commit,zone.TileState.HasCoating(11,10,"water"));Assert.AreEqual(commit?5:0,flask.GetPart<LiquidVesselPart>().Volume);
   if(!commit)Assert.AreEqual(5,pool.GetPart<LiquidPoolPart>().Volume);
  }
  [Test]public void CleanWaterskinAlsoRemovesItsExhaustedPouredOwner()
  {
   var flask=Flask("water",3);Assert.True(Act(flask,Pour(flask,11,10)));var pool=zone.GetCell(11,10).Objects.Single(e=>e.BlueprintName==LiquidVesselService.PoolBlueprint);
   var skin=factory.CreateEntity("Waterskin");Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));Assert.True(Act(skin,"FillWaterskin"));Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Charges);Assert.Null(zone.GetEntityCell(pool));Assert.False(zone.TileState.HasCoating(11,10,"water"));
  }
  [Test]public void AuthoredNaturalPoolIdentityIsNotRemovedByFinitePouredPolicy()
  {var flask=Flask();var pool=Pool("water",3);pool.BlueprintName="AuthoredNaturalSource";Assert.True(Act(flask,Fill(flask,pool)));Assert.AreEqual(0,pool.GetPart<LiquidPoolPart>().Volume);Assert.NotNull(zone.GetEntityCell(pool));Assert.True(zone.TileState.HasCoating(11,10,"water"));}
  [Test]public void AnotherLiveSameLiquidOwnerRetainsItsProjectionAfterPouredCollection()
  {
   var flask=Flask("water",3);Assert.True(Act(flask,Pour(flask,11,10)));var pool=zone.GetCell(11,10).Objects.Single(e=>e.BlueprintName==LiquidVesselService.PoolBlueprint);var natural=Pool("water",9);natural.BlueprintName="AuthoredNaturalSource";
   Assert.True(Act(flask,Fill(flask,pool)));Assert.Null(zone.GetEntityCell(pool));Assert.NotNull(zone.GetEntityCell(natural));Assert.AreEqual(9,natural.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(11,10,"water"));
  }
  void PrepareFactory(Action<Entity> callback)
  {FactoryProbe.Callback=callback;factory.RegisterPartType<FactoryProbe>();factory.Blueprints[LiquidVesselService.PoolBlueprint].Parts[nameof(FactoryProbe)]=new Dictionary<string,string>();}
  public sealed class FactoryProbe:Part
  {public static Action<Entity> Callback;public override string Name=>"LiquidVesselFactoryProbe";public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
  public sealed class Veto:Part{public string When;public override string Name=>"LiquidVeto";public override bool HandleEvent(GameEvent e){if(When=="before"&&e.ID=="BeforeInventoryAction")return false;if(When=="after"&&e.ID=="AfterInventoryAction")throw new InvalidOperationException("rollback probe");return true;}}
 }
}
