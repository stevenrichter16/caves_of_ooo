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
 public sealed class SpreadCoolingPlacementTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  DensityLootTestScope scope; Zone zone; SpreadCompositionBuilder terrain; Entity row;
  readonly List<(FieldInfo field,object value,List<DictionaryEntry> entries)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
  object oldSettlement;Action<Entity> oldProbe;
  [SetUp] public void Setup()
  {
   foreach(var f in new[]{typeof(LootTableRegistry),typeof(PlayerReputation)}.SelectMany(t=>t.GetFields(All))){if(!f.IsStatic)continue;var v=f.GetValue(null);var entries=new List<DictionaryEntry>();if(v is IDictionary d)foreach(DictionaryEntry e in d)entries.Add(e);globals.Add((f,v,entries));}
   oldSettlement=SettlementManager.Current;oldProbe=CreationProbe.Callback;CreationProbe.Callback=null;
   scope=new DensityLootTestScope();scope.Seed(64);Build(true);
  }
  void Build(bool capture)
  {
   zone=new Zone("Overworld.10.5.0");terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.FieldStrips,Topology=SpreadExplorationTopology.BrokenEnclosures};
   typeof(SpreadCompositionBuilder).GetField("CaptureCookingSources",All)?.SetValue(terrain,capture);
   Assert.True(terrain.BuildZone(zone,scope.Factory,new Random(64)));
   row=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow").OrderBy(e=>Math.Abs(zone.GetEntityPosition(e).x-Zone.Width/2)).First();
  }
  [TearDown] public void Cleanup()
  {CreationProbe.Callback=oldProbe;try{scope?.Dispose();}finally{foreach(var g in globals){if(!g.field.IsLiteral&&!g.field.IsInitOnly)g.field.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var e in g.entries)d.Add(e.Key,e.Value);}}globals.Clear();typeof(SettlementManager).GetProperty("Current",All).SetValue(null,oldSettlement);}}
  bool Place(out Entity made,out Func<bool> final,Func<bool> authority=null,Entity selected=null,SpreadCompositionBuilder producer=null)
  {
   var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationCooking");Assert.NotNull(type,"Exact produced-row cooking helper must exist.");
   var method=type.GetMethod("TryPlace",All,null,new[]{typeof(Zone),typeof(EntityFactory),typeof(SpreadCompositionBuilder),typeof(Entity),typeof(Func<bool>),typeof(Entity).MakeByRefType(),typeof(Func<bool>).MakeByRefType()},null);Assert.NotNull(method);
   object[] args={zone,scope.Factory,producer??terrain,selected??row,authority??(()=>true),null,null};bool ok=(bool)method.Invoke(null,args);made=(Entity)args[5];final=(Func<bool>)args[6];return ok;
  }
  void NoCoals()=>Assert.False(zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="SpreadCookingCoals"));
  [Test] public void OriginalRipeRowRemainsOneFiniteRealIngredient()
  {Assert.AreEqual(1,row.GetPart<FieldHarvestPart>().YieldCount);Assert.AreEqual("Emberwheat",row.GetPart<FieldHarvestPart>().YieldBlueprint);Assert.False(row.GetPart<FieldHarvestPart>().Harvested);Assert.AreSame(row,row.GetPart<FieldHarvestPart>().ParentEntity);}
  [TestCase(-200,true)][TestCase(0,false)][TestCase(200,false)]
  public void ColdScreenMatchesRealFreshPlayerFactionFeeling(int reputation,bool hostile)
  {
   var actor=scope.Factory.CreateEntity("MarlbackScrabbler");var player=scope.Factory.CreateEntity("Player");
   Assert.True(zone.AddEntity(actor,0,0));Assert.True(zone.AddEntity(player,1,0));PlayerReputation.Set(FactionManager.GetFaction(actor),reputation);
   Assert.AreEqual(hostile,FactionManager.GetFeeling(actor,player)<=FactionManager.HOSTILE_THRESHOLD);
   var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationCooking");Assert.NotNull(type);var method=type.GetMethod("ColdHostile",All);Assert.NotNull(method);
   Assert.AreEqual(hostile,(bool)method.Invoke(null,new object[]{zone,actor}));Assert.True(zone.RemoveEntity(actor));Assert.False((bool)method.Invoke(null,new object[]{zone,actor}));
  }
  [Test] public void KnownDistantActualRowRefusesBeforeAnyNewSourceFactoryCall()
  {
   zone=new Zone("Overworld.10.15.0");terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.FieldStrips,Topology=SpreadExplorationTopology.BrokenEnclosures};
   typeof(SpreadCompositionBuilder).GetField("CaptureCookingSources",All)?.SetValue(terrain,true);Assert.True(terrain.BuildZone(zone,scope.Factory,new Random(64)));
   row=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow").OrderBy(e=>Math.Abs(zone.GetEntityPosition(e).x-Zone.Width/2)).First();Assert.AreEqual((65,16),zone.GetEntityPosition(row));
   int calls=0;InstallProbe(e=>calls++);Assert.False(Place(out var made,out var final));Assert.Zero(calls);Assert.Null(made);Assert.Null(final);NoCoals();
  }
  [Test] public void ActualCoalsContentIsFiniteBareUtilityWithoutRestLightOrFood()
  {
   Assert.True(scope.Factory.Blueprints.ContainsKey("SpreadCookingCoals"));var e=scope.Factory.CreateEntity("SpreadCookingCoals");
   Assert.False(e.GetPart<PhysicsPart>().Solid);Assert.False(e.GetPart<PhysicsPart>().Takeable);Assert.False(e.HasTag("Solid"));
   var c=e.GetPart<CampfirePart>();Assert.True(c.FiniteCooking);var rest=typeof(CampfirePart).GetField("AllowRest");Assert.NotNull(rest);Assert.False((bool)rest.GetValue(c));
   var t=e.GetPart<ThermalPart>();Assert.AreEqual(500,t.Temperature);Assert.AreEqual(25,t.AmbientTemperature);Assert.AreEqual(300,t.FlameTemperature);Assert.AreEqual(.8f,t.HeatCapacity);Assert.AreEqual(.02f,t.AmbientDecayRate);
   var f=e.GetPart<FuelPart>();Assert.AreEqual(25,f.FuelMass);Assert.AreEqual(25,f.MaxFuel);Assert.AreEqual(1,f.BurnRate);Assert.AreEqual(1,f.HeatOutput);Assert.IsEmpty(f.ExhaustProduct);
   CollectionAssert.AreEquivalent(new[]{"Physics","Render","Examinable","Campfire","Thermal","Fuel"},e.Parts.Select(p=>p.Name));Assert.False(e.HasPart<StatusEffectsPart>());
   var r=e.GetPart<RenderPart>();Assert.AreEqual("*",r.RenderString);Assert.AreEqual("&K",r.ColorString);Assert.AreEqual("cooking coals",r.DisplayName);Assert.AreEqual(4,r.RenderLayer);
  }
  [Test] public void RealProducedRowAdmitsOneUtilityPreservingEveryExistingOwnerAndFourPorts()
  {
   var owners=zone.GetReadOnlyEntities().ToArray();var at=owners.ToDictionary(e=>e,zone.GetEntityPosition);Assert.True(Place(out var made,out var final));Assert.True(final());
   Assert.AreEqual(owners.Length+1,zone.EntityCount);Assert.AreEqual("SpreadCookingCoals",made.BlueprintName);foreach(var e in owners){Assert.AreSame(zone.GetCell(at[e].x,at[e].y),zone.GetEntityCell(e));}
   Assert.False(row.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(1,row.GetPart<FieldHarvestPart>().YieldCount);
   Assert.True(Useful(zone.GetEntityPosition(made)),"Independent four-port model must fit at most60 material passes and two local approaches.");
   Assert.False(Place(out var again,out var other));Assert.Null(again);Assert.Null(other);Assert.AreEqual(owners.Length+1,zone.EntityCount);
  }
  // Independent cardinal route oracle. It neither calls the placement helper's
  // geometry implementation nor advances heat, scheduler, RNG or source state.
  bool Useful((int x,int y) source)
  {
   bool Walk(int x,int y)=>x>=0&&y>=0&&x<Zone.Width&&y<Zone.Height&&zone.TileState.Get(x,y)?.IsEmpty!=false&&!zone.GetCell(x,y).Occupants.Any(e=>e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true||e.GetPart<DoorPart>()?.IsClosed==true||e.HasTag("Creature")||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>());
   IEnumerable<(int x,int y)> Adj((int x,int y) p)=>new[]{(p.x-1,p.y),(p.x+1,p.y),(p.x,p.y-1),(p.x,p.y+1)}.Where(q=>Walk(q.Item1,q.Item2));
   int Distance((int x,int y) a,(int x,int y) b){var seen=new HashSet<(int,int)>();var q=new Queue<(int x,int y,int d)>();if(!Walk(a.x,a.y))return -1;q.Enqueue((a.x,a.y,0));seen.Add(a);while(q.Count>0){var p=q.Dequeue();if((p.x,p.y)==b)return p.d;foreach(var n in Adj((p.x,p.y)))if(seen.Add(n))q.Enqueue((n.x,n.y,p.d+1));}return -1;}
   var a=zone.GetEntityPosition(row);var ports=new[]{(0,terrain.Plan.WestY),(Zone.Width-1,terrain.Plan.EastY),(terrain.Plan.NorthX,0),(terrain.Plan.SouthX,Zone.Height-1)};
   if(Adj(a).Count()<2||Adj(source).Count()<2)return false;
   return Adj(a).Any(r=>Adj(source).Any(s=>{int last=Distance(r,s);return last>=0&&last<=8&&ports.All(p=>{int d=Distance(p,r);return d>=0&&d+last+3<=60;});}));
  }
  [TestCase("disabled")][TestCase("late-capture")][TestCase("rebuilt")][TestCase("denied")][TestCase("harvested")][TestCase("yield")][TestCase("count")][TestCase("moved")][TestCase("same-id")][TestCase("owned")]
  public void InvalidOrUncapturedRowRefusesWithoutCreatingAUtility(string fault)
  {
   Entity selected=row;
   if(fault=="disabled"){var f=typeof(SpreadCompositionBuilder).GetField("CaptureCookingSources",All);Assert.NotNull(f);f.SetValue(terrain,false);}
   if(fault=="late-capture"){Build(false);selected=row;var f=typeof(SpreadCompositionBuilder).GetField("CaptureCookingSources",All);Assert.NotNull(f);f.SetValue(terrain,true);}
   if(fault=="rebuilt")Assert.True(terrain.BuildZone(new Zone(zone.ZoneID),scope.Factory,new Random(64)));
   if(fault=="harvested")row.GetPart<FieldHarvestPart>().Harvested=true;if(fault=="yield")row.GetPart<FieldHarvestPart>().YieldBlueprint="RawMeat";if(fault=="count")row.GetPart<FieldHarvestPart>().YieldCount=2;
   if(fault=="moved")Assert.True(zone.MoveEntity(row,1,1));if(fault=="owned")row.SetTag("Owned");
   if(fault=="same-id"){var at=zone.GetEntityPosition(row);Assert.True(zone.RemoveEntity(row));selected=scope.Factory.CreateEntity("RipeCropRow");selected.ID=row.ID;Assert.True(zone.AddEntity(selected,at.x,at.y));}
   var owners=zone.GetReadOnlyEntities().ToArray();Assert.False(Place(out var made,out var final,()=>fault!="denied",selected));Assert.Null(made);Assert.Null(final);NoCoals();CollectionAssert.AreEquivalent(owners,zone.GetReadOnlyEntities());
  }
  [TestCase(false)][TestCase(true)] public void ABlockedActualEntryPortRefusesBeforeFactory(bool blocked)
  {
   if(blocked)Assert.True(zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),0,terrain.Plan.WestY));
   int calls=0;InstallProbe(e=>calls++);Assert.AreEqual(!blocked,Place(out var made,out var final));Assert.AreEqual(blocked?0:1,calls);if(!blocked)Assert.True(final());else NoCoals();
  }
  public sealed class CreationProbe:Part
  {public static Action<Entity> Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated"){var owner=ParentEntity;Callback?.Invoke(owner);owner.RemovePart(this);}return true;}}
  void InstallProbe(Action<Entity> callback){Assert.True(scope.Factory.Blueprints.ContainsKey("SpreadCookingCoals"));scope.Factory.RegisterPartType<CreationProbe>();scope.Factory.Blueprints["SpreadCookingCoals"].Parts[nameof(CreationProbe)]=new Dictionary<string,string>();CreationProbe.Callback=callback;}
  [TestCase("unchanged",true)][TestCase("row",false)][TestCase("temperature",false)][TestCase("id",false)][TestCase("foreign",false)]
  public void FactoryCallbacksCannotLaunderChangedOriginalOrForeignOutput(string fault,bool expected)
  {
   Entity foreign=null;int calls=0;InstallProbe(e=>{calls++;if(fault=="row")row.GetPart<FieldHarvestPart>().Harvested=true;if(fault=="temperature")e.GetPart<ThermalPart>().Temperature=100;if(fault=="id")e.ID=row.ID;if(fault=="foreign"){foreign=e;Assert.True(zone.AddEntity(e,0,0));}});
   Assert.AreEqual(expected,Place(out var made,out var final));Assert.AreEqual(1,calls);if(expected)Assert.True(final());else Assert.Null(made);
   if(foreign!=null)Assert.AreSame(zone.GetCell(0,0),zone.GetEntityCell(foreign));if(fault=="row")Assert.True(row.GetPart<FieldHarvestPart>().Harvested);
  }
  [TestCase("unchanged",true)][TestCase("hidden",false)][TestCase("burning",false)]
  public void NewOutputCannotHideOrBeginWithBurningDespiteUnchangedHeatAndFuel(string fault,bool expected)
  {
   Entity output=null;InstallProbe(e=>{output=e;if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;
    if(fault=="burning"){e.SetTag("Flammable");Assert.True(e.ApplyEffect(new BurningEffect(rng:new Random(7)),zone:zone));e.Tags.Remove("Flammable");Assert.True(e.HasEffect<BurningEffect>());}
    Assert.AreEqual(500,e.GetPart<ThermalPart>().Temperature);Assert.AreEqual(25,e.GetPart<FuelPart>().FuelMass);});
   Assert.AreEqual(expected,Place(out var made,out var final));Assert.NotNull(output);
   if(expected)Assert.True(final());else{Assert.Null(made);Assert.Null(final);Assert.Null(zone.GetEntityCell(output));}
   if(fault=="hidden")Assert.False(output.GetPart<RenderPart>().Visible);if(fault=="burning")Assert.True(output.HasEffect<BurningEffect>());
  }
  [TestCase("unchanged",true)][TestCase("offroute",true)][TestCase("heat",false)][TestCase("spent-row",false)][TestCase("moved",false)][TestCase("port",false)][TestCase("overlap",false)]
  public void FinalProofPinsFiniteSourceAndEveryPortButAllowsHarmlessUnrelatedState(string fault,bool expected)
  {
   Assert.True(Place(out var made,out var final));Assert.True(final());
   if(fault=="offroute")Assert.True(zone.AddEntity(scope.Factory.CreateEntity("Cudgel"),0,0));if(fault=="heat")made.GetPart<ThermalPart>().Temperature=100;if(fault=="spent-row")row.GetPart<FieldHarvestPart>().Harvested=true;
   if(fault=="moved")Assert.True(zone.MoveEntity(made,1,1));if(fault=="port")Assert.True(zone.AddEntity(scope.Factory.CreateEntity("StoneWall"),0,terrain.Plan.WestY));
   if(fault=="overlap"){var at=zone.GetEntityPosition(made);Assert.True(zone.AddEntity(scope.Factory.CreateEntity("Cudgel"),at.x,at.y));}Assert.AreEqual(expected,final());
  }
 }
}
