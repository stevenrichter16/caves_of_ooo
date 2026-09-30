using System;
using System.Collections.Generic;
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
   internal readonly Zone Zone=new Zone("Overworld.11.9.0");internal readonly SpreadCompositionBuilder Terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.OldRoad};
   internal readonly PopulationBuilder Population;internal readonly ContainerBuilder Containers=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness){CaptureSourceReceipts=true};
   internal Entity[] Owners;internal Func<bool> Final;
   internal Fixture(int count=2)
   {
    Scope.Seed(64);Assert.True(Terrain.BuildZone(Zone,Factory,new Random(64)));
    // Controlled geometry only; the final test separately exercises unedited native generation.
    foreach(var e in Zone.GetReadOnlyEntities().Where(e=>!(bool)typeof(DoorPart).GetMethod("IsBareGround",All).Invoke(null,new object[]{e})).ToArray())Zone.RemoveEntity(e);
    Zone.GenReservedCells.Clear();
    Population=new PopulationBuilder(new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName="Viper",EncounterGroup="SpreadTier1Encounter",MinCount=count,MaxCount=count}}}){CaptureSourceReceipts=true};
    Assert.True(Population.BuildZone(Zone,Factory,new Random(17)));Assert.True(Containers.BuildZone(Zone,Factory,new FirstRoll()));
    Assert.True(Population.SourceReceipt.IsCurrent);Assert.True(Containers.SourceReceipt.IsCurrent);Assert.IsNotEmpty(Containers.SourceReceipt.Owners.Where(e=>e.BlueprintName=="Crate"||e.BlueprintName=="Sack"));
   }
   internal bool Place(string name,Func<bool> authority=null,EntityFactory factory=null)
   {
    var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationWorksites");Assert.NotNull(type,"Selected small worksites must be realized by ordinary generation.");
    var family=(SpreadExplorationFamily)Enum.Parse(typeof(SpreadExplorationFamily),name);
    object[] a={Zone,factory??Factory,Terrain,Population,Containers,family,authority??(()=>true),null,null};
    bool ok=(bool)type.GetMethod("TryPlace").Invoke(null,a);Owners=(Entity[])a[7];Final=(Func<bool>)a[8];return ok;
   }
   internal Func<bool> Proof(IEnumerable<Entity> owners)=>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",All).Invoke(null,new object[]{Zone,owners});
   public void Dispose()=>Scope.Dispose();
  }
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
