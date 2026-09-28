using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationEncounterPipelineTests
 {
  static bool Consume(SpreadGenerationReceipt receipt)=>(bool)typeof(SpreadGenerationReceipt).GetMethod("TryConsume",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(receipt,null);

  DensityLootTestScope scope; readonly List<(FieldInfo field,object value,List<DictionaryEntry> entries)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
  [SetUp] public void Setup(){foreach(var f in typeof(LootTableRegistry).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)){var v=f.GetValue(null);var rows=new List<DictionaryEntry>();if(v is IDictionary d)foreach(DictionaryEntry e in d)rows.Add(e);globals.Add((f,v,rows));}scope=new DensityLootTestScope();}
  [TearDown] public void Cleanup(){try{scope?.Dispose();}finally{foreach(var g in globals){if(!g.field.IsInitOnly)g.field.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var e in g.entries)d.Add(e.Key,e.Value);}}globals.Clear();}}
  sealed class Probe:IZoneBuilder { readonly int priority;readonly Action<Zone,Random> action;public Probe(int p,Action<Zone,Random>a){priority=p;action=a;}public string Name=>"EncounterPipelineProbe";public int Priority=>priority;public bool BuildZone(Zone z,EntityFactory f,Random r){action(z,r);return true;}}
  sealed class ObservedManager:OverworldZoneManager
  {
   readonly bool baseline;readonly Action<ObservedManager,Zone> after;internal Entity[] Before;internal (int x,int y)[] Positions;internal string[] Facts;internal Part[][] Parts;internal Entity[] After;internal string[] LateFacts;internal (int x,int y)[] LatePositions;internal Part[][] LateParts;
   internal PopulationBuilder Population;internal SpreadExplorationBuilder Composer;internal int NextRng,Callbacks;
   internal ObservedManager(EntityFactory f,int seed,bool baseline=false,Action<ObservedManager,Zone> after=null):base(f,seed){this.baseline=baseline;this.after=after;}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id){var p=base.GetPipelineForZone(id);Population=p.Builders.OfType<PopulationBuilder>().SingleOrDefault();Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();if(baseline)p.RemoveBuilders<SpreadExplorationBuilder>();p.AddBuilder(new Probe(4299,(z,r)=>{Before=z.GetReadOnlyEntities().ToArray();Positions=Before.Select(z.GetEntityPosition).ToArray();Facts=Before.Select(Exact).ToArray();Parts=Before.Select(e=>e.Parts.ToArray()).ToArray();}));p.AddBuilder(new Probe(int.MaxValue,(z,r)=>{After=z.GetReadOnlyEntities().ToArray();LateFacts=After.Select(Exact).ToArray();LatePositions=After.Select(z.GetEntityPosition).ToArray();LateParts=After.Select(e=>e.Parts.ToArray()).ToArray();NextRng=r.Next();}));return p;}
   protected override void OnZoneGenerated(Zone z,string id){base.OnZoneGenerated(z,id);if(Composer?.LastResult=="SnakeForage"||Composer?.LastResult=="WorkGang"){Callbacks++;after?.Invoke(this,z);}}
  }
  static Entity[] Gear(Entity e){var i=e.GetPart<InventoryPart>();var b=e.GetPart<Body>();return (i?.Objects??new List<Entity>()).Concat(i?.EquippedItems.Values??Enumerable.Empty<Entity>()).Concat(b?.GetParts().Select(p=>p.Equipped)??Enumerable.Empty<Entity>()).Where(x=>x!=null).Distinct().ToArray();}
  static string Item(Entity e)=>e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":"+string.Join(",",e.Statistics.OrderBy(s=>s.Key).Select(s=>s.Key+":"+s.Value.BaseValue+":"+s.Value.Bonus+":"+s.Value.Penalty))+":"+string.Join(",",e.Properties.OrderBy(s=>s.Key).Select(s=>s.Key+"="+s.Value));
  static string Shape(Entity e)=>Item(e)+";stock="+string.Join("/",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(Item))+";gear="+string.Join("/",Gear(e).Select(Item));
  static string Exact(Entity e)=>e.ID+":"+Shape(e)+";ids="+string.Join("/",Gear(e).Concat(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(i=>i.ID+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID+":"+i.GetPart<PhysicsPart>()?.Equipped?.ID));
  void Seed(int seed,string id)=>scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
  static bool Eligible(ObservedManager m,string family)=>m.Population?.SourceReceipt?.Owners is IReadOnlyList<Entity> owners && (family=="WorkGang"?owners.Count==2&&owners.All(e=>e.BlueprintName=="MarlbackScrabbler"):owners.Count>=1&&owners.Count<=2&&owners.All(e=>e.BlueprintName=="Viper")&&m.Population.ForageSourceReceipt.Owners.Count>0);
  static string[] Selected(OverworldZoneManager m,string family)=>m.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family.ToString()==family).Select(e=>e.ZoneID).ToArray();
  [TestCase(1,"SnakeForage")][TestCase(64,"SnakeForage")][TestCase(1729,"SnakeForage")]
  [TestCase(1,"WorkGang")][TestCase(64,"WorkGang")][TestCase(1729,"WorkGang")]
  public void RealPipelineReusesOnlyTheSelectedSourceAndPreservesStockAndRandomStream(int seed,string family)
  {
   var selection=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);int committed=0,missing=0,eligible=0;
   foreach(string id in Selected(selection,family))
   {
    Seed(seed,id);var baseline=new ObservedManager(scope.Factory,seed,true);var original=baseline.GetZone(id);Assert.NotNull(original,id+" baseline");var shapes=original.GetReadOnlyEntities().Select(Shape).ToArray();
    Seed(seed,id);var current=new ObservedManager(scope.Factory,seed);var zone=current.GetZone(id);Assert.NotNull(zone,id+" current");Assert.NotNull(current.Composer,id);
    var owners=zone.GetReadOnlyEntities().ToArray();CollectionAssert.AreEqual(shapes,owners.Select(Shape),id+" original stock/stats/quantities/order");Assert.AreEqual(baseline.NextRng,current.NextRng,id+" caller RNG");Assert.AreEqual(original.TileState.ToSaveString(),zone.TileState.ToSaveString(),id+" terrain state");
    CollectionAssert.AreEqual(current.Before,current.After,id+" actual references across composition");CollectionAssert.AreEqual(current.Facts,current.LateFacts,id+" original exact IDs/gear/backlinks across composition");
    bool done=current.Exploration.DispositionFor(id)==2;var actors=current.Population.SourceReceipt.Owners.ToArray();var allowed=new HashSet<Entity>(done?actors:Array.Empty<Entity>());
    if(Eligible(current,family))eligible++;else{missing++;Assert.False(done,id+" missing source cannot be manufactured");}
    for(int i=0;i<owners.Length;i++){
     if(current.LatePositions[i]!=current.Positions[i])Assert.True(allowed.Contains(owners[i]),id+" unauthorized move: "+owners[i].BlueprintName);
     var extra=current.LateParts[i].Except(current.Parts[i]).ToArray();CollectionAssert.IsSubsetOf(current.Parts[i],current.LateParts[i],id+" removed original part");
     if(done&&family=="WorkGang"&&allowed.Contains(owners[i])){Assert.AreEqual(1,extra.Length);var t=extra.Single() as CombatTacticsPart;Assert.NotNull(t);Assert.True(t.AssistAllies);Assert.Zero(t.AbilityChance);Assert.AreEqual("",t.SkillClasses);Assert.IsNull(owners[i].GetPart<ActivatedAbilitiesPart>());}
     else Assert.Zero(extra.Length,id+" unexpected role");
    }
    foreach(var food in current.Population.ForageSourceReceipt.Owners){int i=Array.IndexOf(current.Before,food);Assert.AreEqual(current.Positions[i],zone.GetEntityPosition(food));Assert.False(food.GetPart<HarvestablePart>().Harvested);}
    if(done){committed++;Assert.AreEqual(family,current.Composer.LastResult);Assert.AreEqual(1,current.Callbacks);Assert.False(Consume(current.Population.SourceReceipt));if(family=="SnakeForage")Assert.False(Consume(current.Population.ForageSourceReceipt));}
    var finalFacts=owners.Select(Exact).ToArray();Assert.AreSame(zone,current.GetZone(id));CollectionAssert.AreEqual(finalFacts,zone.GetReadOnlyEntities().Select(Exact));
    TestContext.WriteLine(seed+" "+family+" "+id+" "+current.Composer.LastResult+" actors="+string.Join(",",actors.Select(a=>a.BlueprintName))+" food="+current.Population.ForageSourceReceipt.Owners.Count);
   }
   Assert.Greater(eligible,0,"Actual ordinary rolls must supply at least one bounded candidate");Assert.Greater(committed,0,family+" needs actual pipeline realization, not only assignments");TestContext.WriteLine("SUMMARY committed="+committed+" eligible="+eligible+" sourceAbsent="+missing);
  }
  string FindCommitted(int seed,string family){var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);foreach(string id in Selected(m,family)){Seed(seed,id);var one=new ObservedManager(scope.Factory,seed);if(one.GetZone(id)!=null&&one.Exploration.DispositionFor(id)==2)return id;}Assert.Fail("Need a real committed "+family+" before measuring callbacks");return null;}
  [TestCase("SnakeForage",false)][TestCase("SnakeForage",true)][TestCase("WorkGang",false)][TestCase("WorkGang",true)]
  public void FinalGenerationCallbackAcceptsUnchangedAndRefusesMutatedSource(string family,bool mutate)
  {
   const int seed=64;string id=FindCommitted(seed,family);Seed(seed,id);int observed=0;
   var m=new ObservedManager(scope.Factory,seed,after:(self,z)=>{observed++;if(!mutate)return;if(family=="SnakeForage")self.Population.ForageSourceReceipt.Owners[0].GetPart<HarvestablePart>().YieldMax++;else Gear(self.Population.SourceReceipt.Owners[0])[0].Properties["late-change"]="retained";});
   var zone=m.GetZone(id);Assert.Greater(observed,0);if(mutate){Assert.IsNull(zone);Assert.AreEqual(0,m.Exploration.DispositionFor(id));Assert.Zero(m.CachedZoneCount);}else{Assert.NotNull(zone);Assert.AreEqual(2,m.Exploration.DispositionFor(id));}
  }
  [Test] public void ActualLoadedVersionTwoKeepsItsFamilyAndDoesNotRetrofitAnE3Role()
  {
   const int seed=64;var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);string id=m.Exploration.Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow).ZoneID;
   var world=new Entity{BlueprintName="World"};world.Properties[SpreadExplorationPlan.PropertyKey]="2|64|1\n"+id+"|3|2|1|0";
   var restore=typeof(SpreadExplorationPlan).GetMethod("Restore",BindingFlags.Static|BindingFlags.NonPublic);typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(m,restore.Invoke(null,new object[]{m,world}));Seed(seed,id);var zone=m.GetZone(id);Assert.NotNull(zone);m.SetActiveZone(zone);var player=scope.Factory.CreateEntity("Player");var at=zone.GetReadOnlyEntities().Select(zone.GetEntityPosition).First(p=>zone.CanPlaceFootprint(player,p.x,p.y));Assert.True(zone.AddEntity(player,at.x,at.y));
   var state=GameSessionState.Capture("v2-integration","fixture",m,new TurnManager(),player,0);GameSessionState loaded;using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
   var manager=(OverworldZoneManager)loaded.ZoneManager;Assert.AreEqual(2,manager.Exploration.Version);Assert.AreEqual("OccupiedBank",manager.Exploration.Entries.Single().Family.ToString());Assert.AreNotSame(zone,manager.ActiveZone);Assert.False(manager.ActiveZone.GetReadOnlyEntities().Any(e=>e.GetPart<CombatTacticsPart>()?.SkillClasses==""&&e.GetPart<CombatTacticsPart>()?.AssistAllies==true));Assert.True(SpreadExplorationPlan.BindForSave(manager,new Entity()).Properties[SpreadExplorationPlan.PropertyKey].StartsWith("2|64|1\n"));
  }
  [Test] public void ProtectedPlacesCannotAcquireEitherNewFamily()
  {
   var m=OverworldZoneManager.CreateDetached(scope.Factory,64,true);foreach(string id in new[]{ReferenceGladePlan.ZoneID,m.Wayhouse.ZoneID,m.RareEncounters.PairZoneID,m.RareEncounters.ViperZoneID,"Overworld.10.10.0"}.Where(x=>!string.IsNullOrEmpty(x)).Distinct()){
    Assert.False(m.Exploration.Entries.Any(e=>e.ZoneID==id&&e.PlacementEligible&&(e.Family.ToString()=="SnakeForage"||e.Family.ToString()=="WorkGang")),id);Seed(64,id);var z=m.GetZone(id);Assert.NotNull(z);Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="MarlbackScrabbler"&&e.GetPart<CombatTacticsPart>()!=null),id);}
  }
 }
}
