using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 internal sealed class HuntFixture:IDisposable
 {
  internal const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  readonly HaulingContentScope scope=new HaulingContentScope();internal EntityFactory Factory=>scope.Factory;internal readonly Zone Zone=new Zone("Overworld.10.4.0");internal readonly SpreadCompositionBuilder Terrain;internal readonly PopulationBuilder Population;internal Entity Hunter,Grazer;internal Func<bool> Final;
  sealed class Roll:Random{readonly double choice;internal Roll(bool marl,int seed):base(seed){choice=marl?.99:0;}public override double NextDouble()=>choice;}
  internal HuntFixture(int count=2,bool marl=true,int placementSeed=17)
  {
   scope.Seed(64);Terrain=new SpreadCompositionBuilder(1){FormationOverride=Formation.Fallow,Topology=SpreadExplorationTopology.OffsetLanes};var capture=typeof(SpreadCompositionBuilder).GetField("CaptureCoverSources",All);Assert.NotNull(capture);capture.SetValue(Terrain,true);Assert.True(Terrain.BuildZone(Zone,Factory,new Random(64)));
   var table=PopulationTable.SpreadTier1();foreach(var row in table.Entries){if(row.EncounterGroup=="SpreadTier1Encounter")row.MinCount=row.MaxCount=count;else if(row.BlueprintName=="Magpie")row.MinCount=row.MaxCount=2;else if(row.BlueprintName=="PetDog")row.MinCount=row.MaxCount=1;}
   Population=new PopulationBuilder(table){CaptureSourceReceipts=true};Assert.True(Population.BuildZone(Zone,Factory,new Roll(marl,placementSeed)));Assert.True(Population.SourceReceipt.IsCurrent);Assert.True(Population.AmbientSourceReceipt.IsCurrent);
   // Fixture-only content until the separately owned actual Furrowstalker
   // blueprint lands. Actual-content census must not use this supplement.
   var original=Factory.Blueprints["ReedbackGrazer"];var bp=new Blueprint{Name="Furrowstalker",Baked=true};foreach(var kv in original.Parts)bp.Parts[kv.Key]=new Dictionary<string,string>(kv.Value);foreach(var kv in original.Stats)bp.Stats[kv.Key]=new StatBlueprint{Name=kv.Value.Name,Value=kv.Value.Value,Min=kv.Value.Min,Max=kv.Value.Max,Boost=kv.Value.Boost,sValue=kv.Value.sValue};foreach(var kv in original.Tags)bp.Tags[kv.Key]=kv.Value;foreach(var kv in original.Props)bp.Props[kv.Key]=kv.Value;
   bp.Parts.Remove("SpreadGrazer");bp.Parts["SpreadPredator"]=new Dictionary<string,string>();bp.Parts["Brain"]["Passive"]="false";bp.Stats["Speed"].Value=110;Factory.Blueprints[bp.Name]=bp;
  }
  internal Entity Bird=>Population.AmbientSourceReceipt.Owners.First(e=>e.BlueprintName=="Magpie");
  internal Entity[] Sources=>Population.SourceReceipt.Owners.Concat(new[]{Bird}).ToArray();
  internal bool Place(bool covered=true,Func<bool> authority=null,EntityFactory factory=null)
  {var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationHunt");Assert.NotNull(type,"Implement the early exact-source pair transaction and meaningful cover layout.");var method=type.GetMethod("TryPlace",All);Assert.NotNull(method);object[] args={Zone,factory??Factory,Terrain,Population,covered,authority??(()=>true),null,null,null,false};bool ok=(bool)method.Invoke(null,args);Hunter=(Entity)args[6];Grazer=(Entity)args[7];Final=(Func<bool>)args[8];return ok;}
  internal Func<bool> Proof(IEnumerable<Entity> owners)=>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",All).Invoke(null,new object[]{Zone,owners});
  // Unity compiles tests separately from gameplay; read the exact internal
  // members without widening production visibility or replacing ownership checks.
  internal static Zone Spatial(Entity owner)=>(Zone)typeof(Entity).GetField("SpatialZone",All).GetValue(owner);
  internal static bool IsBareGround(Entity owner)=>(bool)typeof(DoorPart).GetMethod("IsBareGround",All).Invoke(null,new object[]{owner});
  internal static uint Rank(int seed,string id,string salt)=>(uint)typeof(SpreadExplorationPlan).GetMethod("Rank",All).Invoke(null,new object[]{seed,id,salt});
  public void Dispose()=>scope.Dispose();
 }
 public sealed class SpreadHuntLayoutTests
 {
  [TestCase(1,false,false)][TestCase(1,false,true)][TestCase(2,false,false)][TestCase(2,false,true)][TestCase(1,true,false)][TestCase(1,true,true)][TestCase(2,true,false)][TestCase(2,true,true)]
  public void WholeOrdinaryGroupAndOneExactMagpieBecomeOneBoundedPair(int count,bool marl,bool covered)
  {using(var f=new HuntFixture(count,marl)){var originals=f.Sources;var others=f.Zone.GetReadOnlyEntities().Except(originals).ToArray();var unchanged=f.Proof(others);int before=f.Zone.EntityCount;Assert.True(f.Place(covered));Assert.AreEqual(before-originals.Length+2,f.Zone.EntityCount);Assert.True(originals.All(e=>HuntFixture.Spatial(e)==null&&f.Zone.GetEntityCell(e)==null));Assert.True(unchanged());Assert.AreEqual("Furrowstalker",f.Hunter.BlueprintName);Assert.AreEqual("ReedbackGrazer",f.Grazer.BlueprintName);Assert.IsNotNull(f.Final);Assert.True(f.Final());var role=f.Hunter.Parts.Single(p=>p.Name=="SpreadPredator");Assert.AreSame(f.Grazer,role.GetType().GetField("Prey").GetValue(role));Assert.AreSame(f.Hunter,f.Grazer.Parts.Single(p=>p.Name=="SpreadGrazer").GetType().GetField("Hunter").GetValue(f.Grazer.Parts.Single(p=>p.Name=="SpreadGrazer")));Assert.IsNull(f.Hunter.GetPart<BrainPart>().CurrentZone);Assert.IsNull(f.Grazer.GetPart<BrainPart>().CurrentZone);}}
  [TestCase(17)][TestCase(73)]public void OpenHuntUsesTheOriginalEncounterNeighborhoodInsteadOfARepeatedCorner(int seed)
  {using(var f=new HuntFixture(placementSeed:seed)){var before=f.Zone.GetEntityPosition(f.Population.SourceReceipt.Owners[0]);Assert.True(f.Place(false));var after=f.Zone.GetEntityPosition(f.Hunter);Assert.LessOrEqual(Math.Max(Math.Abs(before.x-after.x),Math.Abs(before.y-after.y)),8,"Admit a useful lane near this original roll, not a fixed corner in every chunk.");}}
  [Test]public void MissingOptionalActorBlueprintLeavesEveryOriginalOwnerAndReceiptUntouched()
  {using(var f=new HuntFixture()){var graph=f.Proof(f.Zone.GetReadOnlyEntities());var owners=f.Zone.GetReadOnlyEntities().ToArray();f.Factory.Blueprints.Remove("Furrowstalker");Assert.False(f.Place());Assert.True(graph());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());Assert.True(f.Population.SourceReceipt.IsCurrent);Assert.True(f.Population.AmbientSourceReceipt.IsCurrent);}}
  [Test]public void ForeignFactoryCannotClaimOtherwiseMatchingOriginalSources()
  {using(var f=new HuntFixture()){var graph=f.Proof(f.Zone.GetReadOnlyEntities());Assert.False(f.Place(factory:new EntityFactory()));Assert.True(graph());Assert.True(f.Population.SourceReceipt.IsCurrent);}}
  [Test]public void MissingActualCoverNeverUsesALateMatchingTreeDecoy()
  {using(var f=new HuntFixture()){foreach(var e in f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").ToArray())f.Zone.RemoveEntity(e);var decoy=f.Factory.CreateEntity("Tree");Assert.True(f.Zone.AddEntity(decoy,68,4));var graph=f.Proof(f.Zone.GetReadOnlyEntities());Assert.False(f.Place(true));Assert.True(graph());Assert.True(f.Population.SourceReceipt.IsCurrent);}}
  [Test]public void NewPairCreationDoesNotSpendUndeclaredLoadoutOrDeathRandomness()
  {using(var f=new HuntFixture()){LoadoutPart.Rng=new Random(991);TraderPart.Rng=new Random(992);LootDropSystem.Rng=new Random(993);Assert.True(f.Place());Assert.AreEqual(new Random(991).Next(),LoadoutPart.Rng.Next());Assert.AreEqual(new Random(992).Next(),TraderPart.Rng.Next());Assert.AreEqual(new Random(993).Next(),LootDropSystem.Rng.Next());}}
 }
}
