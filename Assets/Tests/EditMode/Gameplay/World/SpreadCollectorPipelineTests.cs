using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCollectorPipelineTests
 {
  DensityLootTestScope scope;readonly List<(FieldInfo f,object v,List<DictionaryEntry> rows)> globals=new List<(FieldInfo,object,List<DictionaryEntry>)>();
  [SetUp]public void Setup(){foreach(var f in typeof(LootTableRegistry).GetFields(BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public)){var v=f.GetValue(null);var rows=new List<DictionaryEntry>();if(v is IDictionary d)foreach(DictionaryEntry e in d)rows.Add(e);globals.Add((f,v,rows));}scope=new DensityLootTestScope();}
  [TearDown]public void Cleanup(){try{scope?.Dispose();}finally{foreach(var g in globals){if(!g.f.IsInitOnly)g.f.SetValue(null,g.v);if(g.v is IDictionary d){d.Clear();foreach(var e in g.rows)d.Add(e.Key,e.Value);}}globals.Clear();}}
  sealed class Probe:IZoneBuilder{readonly int p;readonly Action<Zone,Random> a;public Probe(int priority,Action<Zone,Random> act){p=priority;a=act;}public string Name=>"CollectorPipelineProbe";public int Priority=>p;public bool BuildZone(Zone z,EntityFactory f,Random r){a(z,r);return true;}}
  sealed class Manager:OverworldZoneManager
  {
   readonly bool baseline,stockedFixture;readonly string mutation;public SpreadExplorationBuilder Composer;public PopulationBuilder Population;public int NextRng,Callbacks;public Entity[] Before,Birds;public (int x,int y)[] Positions;public string[] Facts;public bool AmbientCurrent;
   public Manager(EntityFactory f,int seed,bool before=false,string mutation="",bool stockedFixture=false):base(f,seed){baseline=before;this.mutation=mutation;this.stockedFixture=stockedFixture;}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id){var p=base.GetPipelineForZone(id);Composer=p.Builders.OfType<SpreadExplorationBuilder>().SingleOrDefault();Population=p.Builders.OfType<PopulationBuilder>().SingleOrDefault();
    // Explicit producer fixture for late ambient-owner counters only. Natural
    // three-seed occurrence and save cases retain the shipped table unchanged.
    if(stockedFixture)Population.Table=new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{
     new PopulationEntry{BlueprintName="Magpie",MinCount=3,MaxCount=3},
     new PopulationEntry{BlueprintName="Hatchet",MinCount=1,MaxCount=1}}};
    if(baseline)p.RemoveBuilders<SpreadExplorationBuilder>();p.AddBuilder(new Probe(4299,(z,r)=>{Before=z.GetReadOnlyEntities().ToArray();Positions=Before.Select(z.GetEntityPosition).ToArray();Facts=Before.Select(Exact).ToArray();Birds=Population.AmbientSourceReceipt.Owners.Where(e=>e.BlueprintName=="Magpie").ToArray();AmbientCurrent=Population.AmbientSourceReceipt.IsCurrent;}));p.AddBuilder(new Probe(int.MaxValue,(z,r)=>{CollectionAssert.AreEqual(Before,z.GetReadOnlyEntities(),"Composer retains all actual owners");CollectionAssert.AreEqual(Facts,z.GetReadOnlyEntities().Select(Exact),"Composer retains exact current stock and gear");NextRng=r.Next();}));return p;}
   protected override void OnZoneGenerated(Zone z,string id){base.OnZoneGenerated(z,id);if(Composer?.LastResult!="CollectorReturn")return;Callbacks++;var collector=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Tatterjay");var role=collector.GetPart<SpreadCollectorPart>();if(mutation=="home-stock")role.Home.GetPart<ContainerPart>().Contents[0].Properties["late-home"]="independent";if(mutation=="cargo")role.Target.GetPart<StackerPart>().StackCount++;if(mutation=="stock")DensityLootTestScope.Gear(Birds[0]).First().Properties["late-stock"]="independent";if(mutation=="position"){var actor=Birds[0];var at=z.GetEntityPosition(actor);var next=Enumerable.Range(1,Zone.Width-2).Select(x=>(x,y:at.y)).First(p=>p.x!=at.x&&z.CanPlaceFootprint(actor,p.x,p.y));Assert.True(z.MoveEntity(actor,next.x,next.y));}}
  }
  static string Shape(Entity e)=>e.BlueprintName+":"+string.Join(",",e.Statistics.OrderBy(x=>x.Key).Select(x=>x.Key+":"+x.Value.BaseValue+":"+x.Value.Bonus+":"+x.Value.Penalty))+";items="+string.Join("/",DensityLootTestScope.Gear(e).Concat(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(i=>i.BlueprintName+":"+(i.GetPart<StackerPart>()?.StackCount??1)));
  static string Exact(Entity e)=>e.ID+":"+Shape(e)+";links="+string.Join("/",DensityLootTestScope.Gear(e).Concat(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(i=>i.ID+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID+":"+i.GetPart<PhysicsPart>()?.Equipped?.ID));
  void Seed(int seed,string id)=>scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
  string[] IDs(int seed)=>OverworldZoneManager.CreateDetached(scope.Factory,seed,true).Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family==SpreadExplorationFamily.CollectorReturn).Select(e=>e.ZoneID).ToArray();
  [TestCase(1)][TestCase(64)][TestCase(1729)]public void ActualColdSourcesYieldOneTripWithoutChangingTheSubstitutedPipelineBudget(int seed)
  {
   int committed=0,stocked=0;foreach(string id in IDs(seed)){Seed(seed,id);var before=new Manager(scope.Factory,seed,true);var bz=before.GetZone(id);Assert.NotNull(bz,id);Seed(seed,id);var current=new Manager(scope.Factory,seed);var z=current.GetZone(id);Assert.NotNull(z,id);CollectionAssert.AreEqual(bz.GetReadOnlyEntities().Select(Shape),z.GetReadOnlyEntities().Select(Shape));Assert.AreEqual(before.NextRng,current.NextRng,id+" caller RNG");var collector=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Tatterjay");bool done=current.Exploration.DispositionFor(id)==2;Assert.AreEqual(done,collector.GetPart<SpreadCollectorPart>().Configured);foreach(var e in current.Before){int i=Array.IndexOf(current.Before,e);if(!done||(e!=collector&&e!=collector.GetPart<SpreadCollectorPart>().Target))Assert.AreEqual(current.Positions[i],z.GetEntityPosition(e),id+" unselected owner position");}if(done){committed++;if(current.Birds.Length>0){stocked++;Assert.False(current.AmbientCurrent,"Original packet remains honestly stale after normal stocking.");foreach(var bird in current.Birds){var goods=DensityLootTestScope.Gear(bird).ToArray();TestContext.WriteLine("STOCK "+bird.ID+" carriedEntries="+bird.GetPart<InventoryPart>().Objects.Count+" actual="+string.Join(",",goods.Select(g=>g.BlueprintName+":"+(g.GetPart<StackerPart>()?.StackCount??1))));Assert.IsNotEmpty(goods,"Actual stocking can merge or autoequip; exact before/after inventory/body identity is the conservation assertion.");}}}TestContext.WriteLine(seed+" "+id+" "+current.Composer.LastResult+" stockedBirds="+current.Birds.Length);}
   Assert.Greater(committed,0);TestContext.WriteLine("SUMMARY committed="+committed+" withStockedNeighbors="+stocked);
  }
  [TestCase("")][TestCase("stock")][TestCase("position")][TestCase("home-stock")][TestCase("cargo")]public void LateCallbackMustPreserveOtherAnimalsAndTheirActualStock(string mutation)
  {
   const int seed=64;bool needsAmbient=mutation=="stock"||mutation=="position";string id=null;
   foreach(string candidate in IDs(seed))
   {
    Seed(seed,candidate);var find=new Manager(scope.Factory,seed,stockedFixture:needsAmbient);var zone=find.GetZone(candidate);
    if(zone==null||find.Exploration.DispositionFor(candidate)!=2)continue;
    if(needsAmbient){Assert.AreEqual(2,find.Birds.Length,"Real population fixture replaces exactly one of three actual Magpie rolls.");Assert.False(find.AmbientCurrent,"Normal TradeStock must run before composition; never refresh this stale whole packet.");Assert.True(find.Birds.All(b=>DensityLootTestScope.Gear(b).Any()),"Actual ordinary stocked owners precede their mutation.");}
    id=candidate;break;
   }
   Assert.NotNull(id,needsAmbient?"Controlled real producer packet must reach composition before ambient callback counter":"Need a real naturally configured source before home/cargo callback witness");
   TestContext.WriteLine(needsAmbient?"Explicit fixture table Magpie3/Hatchet1; real producer receipts, normal stock/container builders, no natural co-occurrence claim.":"Unmodified natural population table; this counter does not require an unrelated ambient companion.");
   Seed(seed,id);var m=new Manager(scope.Factory,seed,mutation:mutation,stockedFixture:needsAmbient);var result=m.GetZone(id);Assert.Greater(m.Callbacks,0);
   if(mutation==""){Assert.NotNull(result);Assert.AreEqual(2,m.Exploration.DispositionFor(id));}
   else{Assert.IsNull(result);Assert.Zero(m.CachedZoneCount);Assert.Zero(m.Exploration.DispositionFor(id));}
  }
  [Test]public void GeneratedTripKeepsExactCarriedOwnerAcrossFullSaveThenDepositsOnlyOnce()
  {
   const int seed=64;Manager manager=null;Zone zone=null;Entity bird=null;
   foreach(string id in IDs(seed)){Seed(seed,id);var m=new Manager(scope.Factory,seed);var z=m.GetZone(id);if(z!=null&&m.Exploration.DispositionFor(id)==2){manager=m;zone=z;bird=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Tatterjay");break;}}
   Assert.NotNull(bird,"Need an actual generated configured collector before lifecycle proof");
   var role=bird.GetPart<SpreadCollectorPart>();var item=role.Target;var home=role.Home;string birdId=bird.ID,itemId=item.ID,homeId=home.ID,kind=item.BlueprintName;int quantity=role.Quantity;
   int Count(Entity h)=>h.GetPart<ContainerPart>().Contents.Where(e=>e.BlueprintName==kind).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
   int prior=Count(home);bird.GetPart<BrainPart>().CurrentZone=zone; // Explicit activation wiring, as the ordinary bootstrap performs.
   void Act(SpreadCollectorPart r){var e=GameEvent.New(AIBoredEvent.ID);try{r.HandleEvent(e);}finally{e.Release();}}
   for(int n=0;n<=SpreadCollectorPart.MaxApproachActions&&role.Phase==SpreadCollectorPhase.Seeking;n++)Act(role);
   Assert.AreEqual(SpreadCollectorPhase.Carrying,role.Phase);Assert.AreSame(item,role.CurrentCarriedItem);Assert.IsNull(zone.GetEntityCell(item));
   var player=scope.Factory.CreateEntity("Player");Cell spot=null;zone.ForEachCell((c,x,y)=>{if(spot==null&&Math.Max(Math.Abs(x-role.HomeX),Math.Abs(y-role.HomeY))>16&&zone.CanPlaceFootprint(player,x,y))spot=c;});Assert.NotNull(spot);Assert.True(zone.AddEntity(player,spot.X,spot.Y));manager.SetActiveZone(zone);
   var session=GameSessionState.Capture("collector","generated-core-fixture",manager,new TurnManager(),player,0);GameSessionState loaded;
   using(var stream=new MemoryStream()){session.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,scope.Factory));}
   var restored=loaded.ZoneManager.ActiveZone;Assert.AreNotSame(zone,restored);var next=restored.GetReadOnlyEntities().Single(e=>e.ID==birdId);var carried=next.GetPart<SpreadCollectorPart>();
   Assert.AreNotSame(bird,next);Assert.AreNotSame(item,carried.Target);Assert.AreNotSame(home,carried.Home);Assert.AreEqual(itemId,carried.Target.ID);Assert.AreEqual(homeId,carried.Home.ID);Assert.AreEqual(SpreadCollectorPhase.Carrying,carried.Phase);Assert.AreEqual(role.Actions,carried.Actions);Assert.AreEqual(quantity,carried.Quantity);Assert.AreSame(next,carried.Target.GetPart<PhysicsPart>().InInventory);Assert.AreSame(carried.Target,carried.CurrentCarriedItem);Assert.AreEqual(prior,Count(carried.Home));
   next.GetPart<BrainPart>().CurrentZone=restored;
   for(int n=0;n<=SpreadCollectorPart.MaxApproachActions&&carried.Phase==SpreadCollectorPhase.Carrying;n++)Act(carried);
   Assert.AreEqual(SpreadCollectorPhase.Deposited,carried.Phase);Assert.IsNull(carried.CurrentCarriedItem);Assert.AreEqual(prior+quantity,Count(carried.Home));int actions=carried.Actions;for(int n=0;n<3;n++)Act(carried);Assert.AreEqual(actions,carried.Actions);Assert.AreEqual(prior+quantity,Count(carried.Home));Assert.AreSame(restored,loaded.ZoneManager.GetZone(restored.ZoneID));
  }

 }
}
