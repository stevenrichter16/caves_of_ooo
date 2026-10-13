using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationWorksitesTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  sealed class FirstRoll:Random { public override int Next(int max)=>max>1?1:0;public override int Next(int min,int max)=>min;public override double NextDouble()=>.1; }
  sealed class Fixture:IDisposable
  {
   internal readonly HaulingContentScope Scope=new HaulingContentScope();internal EntityFactory Factory=>Scope.Factory;
   internal readonly Zone Zone;internal readonly SpreadCompositionBuilder Terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.OldRoad};
   internal readonly PopulationBuilder Population;internal readonly ContainerBuilder Containers=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness){CaptureSourceReceipts=true};
   internal Entity[] Owners;internal Func<bool> Final;
   internal Fixture(int count=2,string zoneId="Overworld.11.9.0")
   {
    Zone=new Zone(zoneId);Scope.Seed(64);Assert.True(Terrain.BuildZone(Zone,Factory,new Random(64)));
    // Controlled geometry only; the final test separately exercises unedited native generation.
    foreach(var e in Zone.GetReadOnlyEntities().Where(e=>!(bool)typeof(DoorPart).GetMethod("IsBareGround",All).Invoke(null,new object[]{e})).ToArray())Zone.RemoveEntity(e);
    Zone.GenReservedCells.Clear();
    Population=new PopulationBuilder(new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName="Viper",EncounterGroup="SpreadTier1Encounter",MinCount=count,MaxCount=count}}}){CaptureSourceReceipts=true};
    Assert.True(Population.BuildZone(Zone,Factory,new Random(17)));Assert.True(Containers.BuildZone(Zone,Factory,new FirstRoll()));
    Assert.True(Population.SourceReceipt.IsCurrent);Assert.True(Containers.SourceReceipt.IsCurrent);Assert.IsNotEmpty(Containers.SourceReceipt.Owners.Where(e=>e.BlueprintName=="Crate"||e.BlueprintName=="Sack"));
   }
   internal bool Place(string name,Func<bool> authority=null,EntityFactory factory=null,bool opening=false)
   {
    var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationWorksites");Assert.NotNull(type,"Selected small worksites must be realized by ordinary generation.");
    var family=(SpreadExplorationFamily)Enum.Parse(typeof(SpreadExplorationFamily),name);
    object[] a={Zone,factory??Factory,Terrain,Population,Containers,family,authority??(()=>true),null,null,opening};
    bool ok=(bool)type.GetMethod("TryPlace").Invoke(null,a);Owners=(Entity[])a[7];Final=(Func<bool>)a[8];return ok;
   }
   internal Func<bool> Proof(IEnumerable<Entity> owners)=>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",All).Invoke(null,new object[]{Zone,owners});
   public void Dispose()=>Scope.Dispose();
  }
  [TestCase(1)][TestCase(2)] public void OpeningGuardUsesOnlyAdmittedSourceBudget(int count)
  {using(var f=new Fixture(count,"Overworld.11.11.0")){Assert.True(f.Place("TrappersStore",opening:true));Assert.True(f.Final());Assert.AreEqual(count,f.Zone.GetReadOnlyEntities().Count(e=>e.HasTag("Creature")));var actor=f.Owners.Single(e=>e.BlueprintName=="MarlbackScrabbler");var p=actor.GetPart<WorksiteGuardPart>();Assert.NotNull(p);Assert.AreEqual(f.Zone.ZoneID,p.ZoneID);Assert.AreEqual(f.Zone.GetEntityPosition(actor),(p.HomeX,p.HomeY));Assert.AreEqual(4,p.PursuitRadius);Assert.False(f.Factory.CreateEntity("MarlbackScrabbler").HasPart<WorksiteGuardPart>());p.HomeX++;Assert.False(f.Final());}}
  [TestCase("Overworld.12.10.0",true)][TestCase("Overworld.15.10.0",false)] public void ForgeLanePolicyIsScopedAndMeleeKeepsAdvancing(string zone,bool expected)
  {using(var f=new Fixture(2,zone)){Assert.True(f.Place("TemperingShelter"));Assert.True(f.Final());var caster=f.Owners.Single(e=>e.BlueprintName=="MarlbackCindercaller");Assert.AreEqual(expected,caster.GetPart<CombatTacticsPart>().RepositionForShot);Assert.False(f.Owners.Any(e=>e.HasPart<WorksiteGuardPart>()));Assert.False(f.Factory.CreateEntity("MarlbackCindercaller").GetPart<CombatTacticsPart>().RepositionForShot);if(expected){caster.GetPart<CombatTacticsPart>().RepositionForShot=false;Assert.False(f.Final());}}}
  [Test] public void LaterFactoryCallbackCannotRedefineStagedGuardDuty()
  {using(var f=new Fixture(2,"Overworld.11.11.0")){var old=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(old);Entity staged=null;f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");foreach(var bp in new[]{"MarlbackSoursprayer","MarlbackScrabbler"})f.Factory.Blueprints[bp].Parts["ReceiptCreated"]=new Dictionary<string,string>();SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{if(e.BlueprintName=="MarlbackSoursprayer")staged=e;else if(staged!=null)staged.GetPart<CombatTacticsPart>().RepositionForShot=false;};try{Assert.False(f.Place("TrappersStore",opening:true));Assert.True(proof());CollectionAssert.AreEquivalent(old,f.Zone.GetReadOnlyEntities());Assert.True(f.Population.SourceReceipt.IsCurrent);}finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}}}
  [TestCase("FieldAlembic","AlchemyStill")][TestCase("TemperingShelter","TinkersForge")][TestCase("TrappersStore","SpikeTrap")]
  public void ThreeSitesHaveRealFunctionalOwnersFiniteGatheringAndSafeArrival(string family,string functional)
  {
   using(var f=new Fixture()){var old=f.Zone.GetReadOnlyEntities().ToArray();var stock=f.Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToArray();
    Assert.True(f.Place(family));Assert.True(f.Final());Assert.IsNotEmpty(f.Owners);Assert.True(f.Owners.Any(e=>e.BlueprintName==functional));
    var patches=f.Owners.Where(e=>e.BlueprintName=="StoneburrPatch"||e.BlueprintName=="FrostLichenPatch").ToArray();Assert.IsNotEmpty(patches);
    foreach(var e in patches){var h=e.GetPart<HarvestablePart>();Assert.NotNull(h);Assert.False(h.Harvested);Assert.That(h.YieldMin,Is.InRange(1,2));Assert.That(h.YieldMax,Is.InRange(h.YieldMin,2));}
    CollectionAssert.AreEquivalent(stock,f.Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents));
    foreach(var e in f.Owners){var p=f.Zone.GetEntityPosition(e);Assert.False(f.Zone.GenReservedCells.Contains(p));Assert.True(f.Zone.TileState.Get(p.x,p.y)?.IsEmpty!=false);Assert.Greater(Math.Max(Math.Abs(p.x-40),Math.Abs(p.y-12)),6);}
    Assert.False(f.Zone.GetCell(40,12).BlocksMovement());Assert.True(old.Where(e=>!f.Population.SourceReceipt.Owners.Contains(e)).All(e=>f.Zone.GetEntityCell(e)!=null));
   }
  }
  [TestCase("TemperingShelter","MarlbackCindercaller")][TestCase("TrappersStore","MarlbackSoursprayer")]
  public void TwoOriginalHostilesBecomeOneMixedPairWithoutAnotherGroup(string family,string caster)
  {
   using(var f=new Fixture()){var source=f.Population.SourceReceipt.Owners.ToArray();var others=f.Zone.GetReadOnlyEntities().Except(source).Except(f.Containers.SourceReceipt.Owners).ToArray();var unchanged=f.Proof(others);
    Assert.True(f.Place(family));Assert.True(unchanged());Assert.True(source.All(e=>f.Zone.GetEntityCell(e)==null));
    var pair=f.Owners.Where(e=>e.HasTag("Creature")).ToArray();CollectionAssert.AreEquivalent(new[]{caster,"MarlbackScrabbler"},pair.Select(e=>e.BlueprintName));Assert.AreEqual(2,f.Zone.GetReadOnlyEntities().Count(e=>e.HasTag("Creature")));
    Assert.LessOrEqual(pair.Single(e=>e.BlueprintName==caster).GetPart<BrainPart>().SightRadius,6);Assert.True(pair.All(e=>e.GetPart<CombatTacticsPart>()?.AssistAllies==true));
   }
  }
  [TestCase("TemperingShelter")][TestCase("TrappersStore")]
  public void SingleHostileIsRetainedAndDoesNotPreventAFunctionalSite(string family)
  {using(var f=new Fixture(1)){var source=f.Population.SourceReceipt.Owners.Single();var proof=f.Proof(new[]{source});Assert.True(f.Place(family));Assert.True(proof());Assert.AreEqual(1,f.Zone.GetReadOnlyEntities().Count(e=>e.HasTag("Creature")));Assert.False(f.Owners.Any(e=>e.HasTag("Creature")));}}
  [TestCase("authority")][TestCase("factory")][TestCase("missing")][TestCase("capture")][TestCase("stock")][TestCase("reserved")]
  public void InvalidSourcesOrMissingSpaceLeaveTheOriginalGraphUntouched(string fault)
  {
   using(var f=new Fixture()){if(fault=="missing")f.Factory.Blueprints.Remove("FrostLichenPatch");if(fault=="capture")f.Population.CaptureSourceReceipts=false;
    if(fault=="stock")f.Containers.SourceReceipt.Owners[0].GetPart<ContainerPart>().Locked=true;
    if(fault=="reserved")f.Zone.ForEachCell((c,x,y)=>f.Zone.GenReservedCells.Add((x,y)));
    var old=f.Zone.GetReadOnlyEntities().ToArray();var unchanged=f.Proof(old);Assert.False(f.Place("TrappersStore",()=>fault!="authority",fault=="factory"?new EntityFactory():null));
    Assert.True(unchanged());CollectionAssert.AreEquivalent(old,f.Zone.GetReadOnlyEntities());Assert.Null(f.Owners);Assert.Null(f.Final);
   }
  }
  [Test]public void FactoryCallbackRevokingAuthorityCannotLeaveAPartialSite()
  {
   using(var f=new Fixture()){bool allowed=true;f.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");f.Factory.Blueprints["AlchemyStill"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
    var before=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(before);SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>allowed=false;
    try{Assert.False(f.Place("FieldAlembic",()=>allowed));Assert.True(proof());CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());}finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
   }
  }
  [Test]public void FinalValidatorPinsHarvestStateAndCannotBeReusedForAnotherSite()
  {using(var f=new Fixture()){Assert.True(f.Place("FieldAlembic"));var proof=f.Final;var patch=f.Owners.First(e=>e.HasPart<HarvestablePart>());Assert.True(proof());Assert.False(f.Place("FieldAlembic"));patch.GetPart<HarvestablePart>().Harvested=true;Assert.False(proof());}}

  sealed class CensusProbe:IZoneBuilder
  {
   internal Action<Zone> Observe;
   public string Name=>"OpeningWorksiteCensus";
   public int Priority{get;set;}
   public bool BuildZone(Zone zone,EntityFactory factory,Random rng){Observe(zone);return true;}
  }
  sealed class CensusManager:OverworldZoneManager
  {
   internal Entity[] Before,Sources,Caches,RetainedSources,AddedActors;
   internal int CreatureCountBefore,CreatureCountAfter,PipelineBuilds;
   internal bool OriginalUnchanged,OthersUnchanged,StockPreserved,ObservedAfter;
   internal SpreadExplorationBuilder Composer;
   internal CensusManager(EntityFactory factory,int seed):base(factory,seed){}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id)
   {
    PipelineBuilds++;
    var pipeline=base.GetPipelineForZone(id);
    var population=pipeline.Builders.OfType<PopulationBuilder>().Single();
    var containers=pipeline.Builders.OfType<ContainerBuilder>().Single();
    Composer=pipeline.Builders.OfType<SpreadExplorationBuilder>().Single();
    Func<bool> original=null,others=null;
    Dictionary<Entity,Entity[]> stock=null;
    Dictionary<Entity,int> quantities=null;
    pipeline.AddBuilder(new CensusProbe{Priority=4299,Observe=zone=>
    {
     ObservedAfter=false;
     Before=zone.GetReadOnlyEntities().ToArray();
     Assert.NotNull(population.SourceReceipt,"Observe the real producer receipt, never infer an allowance from nearby actors.");
     Assert.NotNull(containers.SourceReceipt);
     Sources=population.SourceReceipt.Owners.ToArray();
     Caches=containers.SourceReceipt.Owners.ToArray();
     original=CensusProof(zone,Before);
     others=CensusProof(zone,Before.Except(Sources).Except(Caches));
     stock=Caches.ToDictionary(e=>e,e=>e.GetPart<ContainerPart>().Contents.ToArray());
     quantities=stock.Values.SelectMany(v=>v).ToDictionary(e=>e,e=>e.GetPart<StackerPart>()?.StackCount??1);
     CreatureCountBefore=Before.Count(e=>e.HasTag("Creature"));
    }});
    // Check the transaction before later crop/narrative builders legitimately amend the zone.
    pipeline.AddBuilder(new CensusProbe{Priority=4301,Observe=zone=>
    {
     OriginalUnchanged=original()&&new HashSet<Entity>(Before).SetEquals(zone.GetReadOnlyEntities());
     OthersUnchanged=others();
     RetainedSources=Sources.Where(e=>zone.GetEntityCell(e)!=null).ToArray();
     AddedActors=zone.GetReadOnlyEntities().Except(Before).Where(e=>e.HasTag("Creature")).ToArray();
     CreatureCountAfter=zone.GetReadOnlyEntities().Count(e=>e.HasTag("Creature"));
     StockPreserved=stock.All(pair=>zone.GetEntityCell(pair.Key)!=null&&pair.Value.All(item=>
      pair.Key.GetPart<ContainerPart>().Contents.Contains(item)&&(item.GetPart<StackerPart>()?.StackCount??1)==quantities[item]));
     ObservedAfter=true;
    }});
    return pipeline;
   }
  }
  static Func<bool> CensusProof(Zone zone,IEnumerable<Entity> owners)
   =>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",All).Invoke(null,new object[]{zone,owners});
  static string CensusOwners(IEnumerable<Entity> owners)
   =>string.Join(",",owners.Select(e=>e.ID+":"+e.BlueprintName+":"+e.GetProperty(SpreadExplorationWorksites.RoleKey)
    +":guard="+e.HasPart<WorksiteGuardPart>()+":lane="+(e.GetPart<CombatTacticsPart>()?.RepositionForShot==true)));

  [Test]public void ThreeSeedOpeningYardAndForgeCensusPreservesActualSourceBudgetsAndReturns()
  {
   // Generated-graph evidence, authoritative in native Unity; not an ordinary-input or tactical-comparison journey.
   string path=Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath),"Docs/Verification/ComposableBuilds/worksite-three-seed-census.tsv");
   Directory.CreateDirectory(Path.GetDirectoryName(path));
   var rows=new List<string>{"seed\tzone\tfamily\tdisposition\tresult\tsourceActors\tretainedSources\taddedActors\tcacheOwners\tcreaturesBefore\tcreaturesAfter\tstockPreserved\toriginalUnchanged\tdepletion\tcachedReturn"};
   int admitted=0,refused=0;
   try
   {
    using(var scope=new HaulingContentScope())
    foreach(int seed in new[]{1,64,1729})
    {
     var manager=new CensusManager(scope.Factory,seed);
     Assert.AreEqual(seed,manager.WorldSeed);
     foreach(var site in new[]{(id:"Overworld.11.11.0",family:SpreadExplorationFamily.TrappersStore),(id:"Overworld.12.10.0",family:SpreadExplorationFamily.TemperingShelter)})
     {
      string context=seed+" "+site.id;
      var entry=manager.Exploration.Find(site.id);
      Assert.NotNull(entry,context);
      Assert.AreEqual(site.family,entry.Family,context);
      scope.Seed(unchecked(seed^FormationSelector.StableIndex(site.id,int.MaxValue)));
      var zone=manager.GetZone(site.id);
      Assert.NotNull(zone,context);
      Assert.True(manager.ObservedAfter,context);
      int disposition=manager.Exploration.DispositionFor(site.id);
      string sourceActors=CensusOwners(manager.Sources),retainedSources=CensusOwners(manager.RetainedSources),addedActors=CensusOwners(manager.AddedActors);
      string cacheOwners=string.Join(",",manager.Caches.Select(e=>e.ID+":"+e.BlueprintName));
      string Row(string depletion,string returned)=>string.Join("\t",seed,site.id,site.family,disposition,manager.Composer.LastResult,sourceActors,retainedSources,addedActors,cacheOwners,
       manager.CreatureCountBefore,manager.CreatureCountAfter,manager.StockPreserved,manager.OriginalUnchanged,depletion,returned);
      rows.Add(Row("not-tested","not-tested"));
      File.WriteAllLines(path,rows);
      Assert.True(disposition==1||disposition==2,context+": an unaccepted generation is not a source refusal.");
      Assert.True(manager.StockPreserved,context+": original rolled items and quantities must survive composition.");
      Assert.True(manager.OthersUnchanged,context);
      Assert.AreEqual(manager.CreatureCountBefore,manager.CreatureCountAfter,context+": composition cannot add a free hostile group.");
      var roles=zone.GetReadOnlyEntities().Where(e=>e.HasTag("Creature")&&e.Properties.ContainsKey(SpreadExplorationWorksites.RoleKey)).ToArray();
      if(disposition==2)
      {
       admitted++;
       Assert.AreEqual(site.family.ToString(),manager.Composer.LastResult,context);
       Assert.That(manager.Sources.Length,Is.InRange(1,2),context);
       Assert.AreEqual(manager.Sources.Length,manager.RetainedSources.Length+manager.AddedActors.Length,context);
       bool yard=site.family==SpreadExplorationFamily.TrappersStore,pair=manager.Sources.Length==2;
       Assert.AreEqual(yard||pair?manager.Sources.Length:0,roles.Length,context);
       Assert.AreEqual(yard?1:0,roles.Count(e=>e.HasPart<WorksiteGuardPart>()),context);
       Assert.AreEqual(pair?1:0,roles.Count(e=>e.GetProperty(SpreadExplorationWorksites.RoleKey)=="ranged"&&e.GetPart<CombatTacticsPart>()?.RepositionForShot==true),context);
      }
      else
      {
       refused++;
       StringAssert.StartsWith("refused:",manager.Composer.LastResult,context);
       Assert.True(manager.OriginalUnchanged,context+": a refused transaction must retain its complete original graph.");
       Assert.IsEmpty(roles,context);
      }
      // Deplete a real generated resource before cached return; no source is granted for this check.
      string depletion="none-available";
      var patch=zone.GetReadOnlyEntities().FirstOrDefault(e=>e.Properties.ContainsKey(SpreadExplorationWorksites.RoleKey)&&e.HasPart<HarvestablePart>());
      if(patch!=null){patch.GetPart<HarvestablePart>().Harvested=true;depletion="harvested:"+patch.ID;}
      else
      {
       var cache=manager.Caches.FirstOrDefault(e=>e.GetPart<ContainerPart>().Contents.Count>0);
       if(cache!=null){var item=cache.GetPart<ContainerPart>().Contents[0];cache.GetPart<ContainerPart>().Contents.Remove(item);item.GetPart<PhysicsPart>().InInventory=null;depletion="removed-stock:"+item.ID;}
      }
      var retained=zone.GetReadOnlyEntities().ToArray();var proof=CensusProof(zone,retained);int builds=manager.PipelineBuilds;
      manager.UnloadZone(site.id);
      Assert.AreSame(zone,manager.GetZone(site.id),context);
      Assert.AreEqual(builds,manager.PipelineBuilds,context);
      Assert.AreEqual(disposition,manager.Exploration.DispositionFor(site.id),context);
      CollectionAssert.AreEquivalent(retained,zone.GetReadOnlyEntities(),context);
      Assert.True(proof(),context+": return must preserve depletion and the accepted/refused source graph.");
      rows[rows.Count-1]=Row(depletion,"preserved");
     }
    }
    TestContext.WriteLine("Opening worksite census: admitted="+admitted+", refused="+refused+"; no admission split is forced.");
   }
   finally{File.WriteAllLines(path,rows);TestContext.WriteLine(string.Join("\n",rows));}
  }

  [TestCase("FieldAlembic")][TestCase("TemperingShelter")][TestCase("TrappersStore")]
  public void OrdinaryEnabledPipelineCommitsAndCachedVisitDoesNotReplenish(string family)
  {
   using(var scope=new HaulingContentScope()){const int seed=64;var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var entries=manager.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family.ToString()==family).ToArray();Assert.IsNotEmpty(entries,"Native manifest must actually select the family.");Zone committed=null;
    foreach(var entry in entries){scope.Seed(unchecked(seed^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue)));var z=manager.GetZone(entry.ZoneID);Assert.NotNull(z,entry.ZoneID);if(manager.Exploration.DispositionFor(entry.ZoneID)==2){committed=z;break;}}
    Assert.NotNull(committed,"At least one actual native site must fit.");var patch=committed.GetReadOnlyEntities().First(e=>e.BlueprintName=="StoneburrPatch"||e.BlueprintName=="FrostLichenPatch");patch.GetPart<HarvestablePart>().Harvested=true;
    var owners=committed.GetReadOnlyEntities().ToArray();Assert.AreSame(committed,manager.GetZone(committed.ZoneID));CollectionAssert.AreEquivalent(owners,committed.GetReadOnlyEntities());Assert.True(patch.GetPart<HarvestablePart>().Harvested);
   }
  }
 }
}
