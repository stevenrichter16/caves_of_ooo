using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 // Explicit constructed flat OldRoad geometry, real accepted-generation token,
 // real traveller roll/factory/stock. Seed3 is the first positive in the frozen
 // exhaustive1..64 source census; seeds1/64/1729 have zero OldRoad roll winners.
 public sealed class SpreadRoadsideExchangeTests
 {
  const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
  const string SourceID="Overworld.11.7.0"; const int Seed=3;
  DensityLootTestScope scope;IDictionary loot;DictionaryEntry[] oldLoot;object oldInitialized,oldSettlement;
  readonly List<(FieldInfo field,object value)> factions=new List<(FieldInfo,object)>();
  Manager manager;Zone zone;Entity player;SpreadExplorationPlan plan;Entity made;Entity[] stock;string stockFacts;
  [SetUp] public void Setup()
  {
   loot=(IDictionary)typeof(LootTableRegistry).GetField("_byName",All).GetValue(null);var list=new List<DictionaryEntry>();foreach(DictionaryEntry e in loot)list.Add(e);oldLoot=list.ToArray();oldInitialized=typeof(LootTableRegistry).GetField("_initialized",All).GetValue(null);
   made=null;stock=null;stockFacts=null;oldSettlement=SettlementManager.Current;scope=new DensityLootTestScope();factions.Clear();foreach(var f in typeof(FactionManager).GetFields(All)){if(f.IsStatic&&!f.IsLiteral&&!f.IsInitOnly){factions.Add((f,f.GetValue(null)));f.SetValue(null,Activator.CreateInstance(f.FieldType));}}FactionManager.Initialize();
   scope.Factory.RegisterPartType<ExchangeCreationProbe>();scope.Factory.Blueprints["Merchant"].Parts[nameof(ExchangeCreationProbe)]=new Dictionary<string,string>();
   ExchangeCreationProbe.Callback=e=>{made=e;stock=e.GetPart<InventoryPart>().Objects.ToArray();stockFacts=Goods(e);};
   manager=new Manager(scope.Factory);plan=manager.Exploration;Assert.AreEqual("RoadsideExchange",plan.Find(SourceID).Family.ToString(),"Frozen source must remain actual F8, not a rerolled test source.");
   zone=manager.GetZone(SourceID);Assert.NotNull(zone);Assert.AreEqual(1,plan.DispositionFor(SourceID));manager.SetActiveZone(zone);
   player=scope.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,40,12));
  }
  [TearDown] public void Cleanup()
  {ExchangeCreationProbe.Callback=null;try{scope?.Dispose();}finally{if(oldLoot!=null){loot.Clear();foreach(var e in oldLoot)loot.Add(e.Key,e.Value);typeof(LootTableRegistry).GetField("_initialized",All).SetValue(null,oldInitialized);}typeof(SettlementManager).GetProperty("Current",All).SetValue(null,oldSettlement);foreach(var saved in factions)saved.field.SetValue(null,saved.value);}}
  sealed class Manager:OverworldZoneManager
  {
   internal Manager(EntityFactory f):base(f,Seed){}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id){var result=new ZoneGenerationPipeline{MaxRetries=1};foreach(var guard in base.GetPipelineForZone(id).Builders.Where(b=>b.Name=="SpreadExplorationAttempt"))result.AddBuilder(guard);result.AddBuilder(new Flat());return result;}
   protected override void OnZoneGenerated(Zone z,string id){}
  }
  sealed class Flat:IZoneBuilder
  {
   public string Name=>"constructed-road";public int Priority=>0;
   public bool BuildZone(Zone z,EntityFactory f,Random r){for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(!z.AddEntity(f.CreateEntity(y==12?"RoadStone":"Grass"),x,y))return false;return true;}
  }
  public sealed class ExchangeCreationProbe:Part
  {
   public static Action<Entity> Callback;
   public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}
  }
  static string Goods(Entity e)=>TradeSystem.GetDrams(e)+"|"+string.Join(";",DensityLootTestScope.Gear(e).OrderBy(x=>x.ID).Select(x=>x.ID+":"+x.BlueprintName+":"+(x.GetPart<StackerPart>()?.StackCount??1)+":"+(x.GetPart<CommercePart>()?.Value??0)));
  static void Set(object target,string property,object value)=>typeof(OverworldZoneManager).GetProperty(property,All).SetValue(target,value);
  Entity Enter(){Assert.True(WorldTravellers.OnZoneEntered(player,zone),"Existing ordinary traveller entry must succeed.");Assert.NotNull(made);Assert.AreSame(made,zone.GetReadOnlyEntities().Single(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null));return made;}
  void FreshStock(Entity merchant){Assert.That(stock.Length,Is.GreaterThan(0));CollectionAssert.AreEquivalent(stock,merchant.GetPart<InventoryPart>().Objects);Assert.AreEqual(stockFacts,Goods(merchant));foreach(var i in stock)Assert.AreSame(merchant,i.GetPart<PhysicsPart>().InInventory);}
  bool RoadAdjacent(Entity merchant){var c=zone.GetEntityCell(merchant);return new[]{(c.X-1,c.Y),(c.X+1,c.Y),(c.X,c.Y-1),(c.X,c.Y+1)}.Any(p=>zone.GetCell(p.Item1,p.Item2)?.Objects.Any(e=>e.BlueprintName=="RoadStone")==true);}
  [Test]public void ActualFreshMerchantCommitsOnVisibleReachableVergeWithoutChangingStock()
  {
   var sourceOwners=zone.GetReadOnlyEntities().ToDictionary(e=>e,e=>zone.GetEntityPosition(e));var e=Enter();FreshStock(e);
   Assert.AreEqual(2,plan.DispositionFor(SourceID),"Real accepted entry must commit F8, not only ordinary traveller spawn.");Assert.True(RoadAdjacent(e));var at=zone.GetEntityCell(e);Assert.False(at.Objects.Any(x=>x.BlueprintName=="RoadStone"));Assert.That(Math.Max(Math.Abs(at.X-40),Math.Abs(at.Y-12)),Is.InRange(4,7));Assert.True(AIHelpers.HasLineOfSight(zone,40,12,at.X,at.Y));
   Assert.AreEqual(sourceOwners.Count+1,zone.EntityCount);foreach(var p in sourceOwners)Assert.AreEqual(p.Value,zone.GetEntityPosition(p.Key));Assert.AreEqual(1,player.GetIntProperty("TravellerCount:3"));
  }
  [Test]public void ArrivalDoesNotNeedStaleRendererVisibilityFlags(){zone.ForEachCell((c,x,y)=>{c.IsVisible=false;c.Explored=false;});Enter();Assert.AreEqual(2,plan.DispositionFor(SourceID));Assert.False(zone.GetCell(40,12).IsVisible);}
  [TestCase("no-road")][TestCase("reserved")][TestCase("interior")][TestCase("wet")][TestCase("sealed-border")]
  public void MissingOptionalVergePreservesOrdinaryActorStockAndPosition(string fault)
  {
   if(fault=="no-road"){foreach(var road in zone.GetReadOnlyEntities().Where(road=>road.BlueprintName=="RoadStone").ToArray())zone.RemoveEntity(road);}
   if(fault=="sealed-border"){for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1){var wall=new Entity();wall.SetTag("Solid");zone.AddEntity(wall,x,y);}}
   else if(fault!="no-road")for(int x=2;x<Zone.Width-2;x++)foreach(int y in new[]{11,13}){if(fault=="reserved")zone.GenReservedCells.Add((x,y));if(fault=="interior")zone.GetCell(x,y).IsInterior=true;if(fault=="wet")zone.TileState.WriteCoating(x,y,"water",5);}
   var expected=OrdinaryPosition();var e=Enter();Assert.AreEqual(1,plan.DispositionFor(SourceID));Assert.AreEqual(expected,zone.GetEntityPosition(e));FreshStock(e);
  }
  (int,int) OrdinaryPosition(){object[] args={player,zone,WorldRemarksHash(Seed+":traveller:"+SourceID),0,0};Assert.True((bool)typeof(WorldTravellers).GetMethod("FindStandingCell",All).Invoke(null,args));return((int)args[3],(int)args[4]);}
  static uint WorldRemarksHash(string text)=>(uint)typeof(WorldRemarks).GetMethod("Hash",All).Invoke(null,new object[]{text});
  [TestCase("plan")][TestCase("map")][TestCase("rare")][TestCase("wayhouse")][TestCase("player-moved")][TestCase("same-id-cache")]
  public void FactoryCallbackCannotAdoptAnotherEntryAuthority(string fault)
  {
   var capture=ExchangeCreationProbe.Callback;Zone foreign=null;ExchangeCreationProbe.Callback=e=>{capture(e);if(fault=="plan")Set(manager,"Exploration",OverworldZoneManager.CreateDetached(scope.Factory,Seed,true).Exploration);if(fault=="map")Set(manager,"WorldMap",OverworldZoneManager.CreateDetached(scope.Factory,Seed).WorldMap);if(fault=="rare")Set(manager,"RareEncounters",SpreadRareEncounterPlan.Create(manager));if(fault=="wayhouse")Set(manager,"Wayhouse",SpreadWayhousePlan.Create(manager));if(fault=="player-moved"){zone.MoveEntity(player,41,12);Assert.AreEqual((41,12),zone.GetEntityPosition(player));}if(fault=="same-id-cache"){foreign=new Zone(SourceID);manager.CachedZones[SourceID]=foreign;}};
   var expected=OrdinaryPosition();var e=Enter();Assert.AreEqual(1,plan.DispositionFor(SourceID));Assert.AreEqual(expected,zone.GetEntityPosition(e));FreshStock(e);if(foreign!=null)Assert.AreSame(foreign,manager.CachedZones[SourceID]);if(fault=="player-moved")Assert.AreEqual((41,12),zone.GetEntityPosition(player));
  }
  [Test] public void ThreeEncounterCapDoesNotRunFactoryOrOptionalComposition(){player.IntProperties["TravellerCount:3"]=3;Assert.False(WorldTravellers.OnZoneEntered(player,zone));Assert.IsNull(made);Assert.AreEqual(1,plan.DispositionFor(SourceID));}
  [Test] public void ExistingSameIdentityCannotBeAdoptedOrRestocked(){var existing=scope.Factory.CreateEntity("Merchant");existing.ID="traveller:3:"+SourceID;Assert.True(zone.AddEntity(existing,36,11));made=null;string facts=Goods(existing);Assert.False(WorldTravellers.OnZoneEntered(player,zone));Assert.IsNull(made);Assert.AreEqual(1,plan.DispositionFor(SourceID));Assert.AreEqual(facts,Goods(existing));}
  [Test]public void RemovalAndRepeatedEntryCannotRetryCommitOrMintAnotherMerchant(){var e=Enter();Assert.AreEqual(2,plan.DispositionFor(SourceID));zone.RemoveEntity(e);made=null;for(int n=0;n<3;n++)Assert.False(WorldTravellers.OnZoneEntered(player,zone));Assert.IsNull(made);Assert.AreEqual(2,plan.DispositionFor(SourceID));}
  [Test]public void DepletedStockAndCommittedGraphSurviveActualSaveWithoutEntryReplay()
  {
   var e=Enter();Assert.AreEqual(2,plan.DispositionFor(SourceID));var inv=e.GetPart<InventoryPart>();var item=inv.Objects[0];Assert.True(inv.RemoveObject(item));TradeSystem.SetDrams(e,17);string goods=Goods(e);var position=zone.GetEntityPosition(e);
   var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("F8","constructed",manager,null,player));var z=loaded.ZoneManager.ActiveZone;var merchant=z.GetReadOnlyEntities().Single(x=>x.ID==e.ID);Assert.AreNotSame(e,merchant);Assert.AreEqual(position,z.GetEntityPosition(merchant));Assert.AreEqual(goods,Goods(merchant));Assert.AreEqual(2,loaded.ZoneManager.Exploration.DispositionFor(SourceID));Assert.False(WorldTravellers.OnZoneEntered(loaded.Player,z));Assert.AreEqual(goods,Goods(merchant));Assert.False(merchant.GetPart<InventoryPart>().Objects.Any(x=>x.ID==item.ID));
  }
  [Test]public void CallerRandomIsNotUsedByOptionalPlacement(){var priorL=LoadoutPart.Rng;var priorT=TraderPart.Rng;var forbidden=new ForbiddenRandom();try{LoadoutPart.Rng=TraderPart.Rng=forbidden;Enter();Assert.AreEqual(2,plan.DispositionFor(SourceID));Assert.AreSame(forbidden,LoadoutPart.Rng);Assert.AreSame(forbidden,TraderPart.Rng);}finally{LoadoutPart.Rng=priorL;TraderPart.Rng=priorT;}}
  [TestCase("hidden")][TestCase("blueprint")][TestCase("party")][TestCase("dead")][TestCase("stock-backlink")][TestCase("stock-ground")][TestCase("duplicate-stock")][TestCase("inventory-parent")][TestCase("death-handled")][TestCase("duplicate-owner-id")][TestCase("body-foreign-gear")][TestCase("body-valid-gear")]
  public void MalformedFreshFactoryOwnerCannotGainExchangeAuthority(string fault)
  {
   var capture=ExchangeCreationProbe.Callback;Entity first=null;Entity foreign=new Entity();string afterCallback=null;
   ExchangeCreationProbe.Callback=actor=>{capture(actor);var inv=actor.GetPart<InventoryPart>();first=inv.Objects[0];
    if(fault=="hidden")actor.GetPart<RenderPart>().Visible=false;
    if(fault=="blueprint")actor.BlueprintName="Villager";
    if(fault=="party")actor.GetPart<BrainPart>().PartyLeader=player;
    if(fault=="dead")actor.GetStat("Hitpoints").BaseValue=0;
    if(fault=="death-handled")actor.SetTag("_DeathHandled");
    if(fault=="duplicate-owner-id"){foreign.ID="traveller:3:"+SourceID;Assert.True(zone.AddEntity(foreign,3,3));}
    if(fault=="stock-backlink")first.GetPart<PhysicsPart>().InInventory=foreign;
    if(fault=="stock-ground")Assert.True(zone.AddEntity(first,4,4));
    if(fault=="duplicate-stock")inv.Objects.Add(first);
    if(fault=="inventory-parent")inv.ParentEntity=foreign;
    if(fault=="body-foreign-gear"||fault=="body-valid-gear"){var body=actor.GetPart<Body>();Assert.NotNull(body);var slot=body.GetParts().First(x=>x.Type=="Hand");var weapon=scope.Factory.CreateEntity("Cudgel");if(fault=="body-valid-gear"){Assert.True(inv.AddObject(weapon));Assert.True(inv.EquipToBodyPart(weapon,slot));}else{weapon.GetPart<PhysicsPart>().Equipped=foreign;slot._Equipped=weapon;}Assert.AreSame(weapon,slot.Equipped);}
    afterCallback=Goods(actor);
   };
   var expected=OrdinaryPosition();var result=Enter();Assert.AreEqual(fault=="body-valid-gear"?2:1,plan.DispositionFor(SourceID));if(fault!="body-valid-gear")Assert.AreEqual(expected,zone.GetEntityPosition(result));Assert.AreEqual(afterCallback,Goods(result),"Optional refusal must preserve callback-owned graph changes.");
   if(fault=="stock-backlink")Assert.AreSame(foreign,first.GetPart<PhysicsPart>().InInventory);
   if(fault=="stock-ground")Assert.AreEqual((4,4),zone.GetEntityPosition(first));
  }
  sealed class ForbiddenRandom:Random{public override int Next()=>throw new Exception("caller RNG");public override int Next(int m)=>throw new Exception("caller RNG");public override int Next(int a,int b)=>throw new Exception("caller RNG");}
 }
}
