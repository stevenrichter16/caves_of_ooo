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
 public sealed class SpreadCollectorReplacementReceiptTests
 {
  sealed class Fixture:IDisposable
  {
   readonly SpreadExplorationActorTests.Scope globals=new SpreadExplorationActorTests.Scope();
   public readonly EntityFactory Factory=new EntityFactory();public readonly OverworldZoneManager Manager;public readonly Zone Z;public readonly PopulationBuilder Population;
   public Fixture(string fault="",bool extra=false){Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));if(fault=="missing")Factory.Blueprints.Remove("Tatterjay");Manager=OverworldZoneManager.CreateDetached(Factory,64,true);var entry=Manager.Exploration.Entries.First(e=>e.PlacementEligible&&e.Family==SpreadExplorationFamily.CollectorReturn);Z=new Zone(entry.ZoneID);Guard(Z);var table=new PopulationTable{Name=fault=="foreign"?"other":"SpreadTier1",Entries=new List<PopulationEntry>()};if(fault!="no-source")table.Entries.Add(new PopulationEntry{BlueprintName="Magpie",MinCount=3,MaxCount=3});if(fault=="duplicate")table.Entries.Add(new PopulationEntry{BlueprintName="Magpie",MinCount=1,MaxCount=1});if(extra)table.Entries.Add(new PopulationEntry{BlueprintName="Tatterjay",MinCount=1,MaxCount=1});Population=new PopulationBuilder(table){CaptureSourceReceipts=fault!="disabled",ExplorationManager=Manager};}
   public void Guard(Zone z){var m=typeof(SpreadExplorationPlan).GetMethod("Guard",BindingFlags.Instance|BindingFlags.NonPublic);var guard=(IZoneBuilder)m.Invoke(Manager.Exploration,new object[]{Manager,z.ZoneID});Assert.NotNull(guard);Assert.True(guard.BuildZone(z,Factory,new Random(1)));}
   public void Build(Zone z=null){Assert.True(Population.BuildZone(z??Z,Factory,new Random(31)));}
   public void Dispose()=>globals.Dispose();
  }
  static SpreadGenerationReceipt Receipt(PopulationBuilder b){var p=typeof(PopulationBuilder).GetProperty("AmbientReplacementReceipt");Assert.NotNull(p,"Separate exact substituted-owner receipt must survive ordinary stocking of other ambient owners.");return (SpreadGenerationReceipt)p.GetValue(b);}
  static bool Consume(SpreadGenerationReceipt r)=>(bool)typeof(SpreadGenerationReceipt).GetMethod("TryConsume",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(r,null);
  [Test]public void ActualMagpieStockingKeepsOnlyTheSubstitutedOwnerReceiptCurrent()
  {using(var f=new Fixture()){f.Build();var original=f.Population.AmbientSourceReceipt;var r=Receipt(f.Population);Assert.True(original.IsCurrent);Assert.True(r.IsCurrent);Assert.AreEqual(1,r.Owners.Count);Assert.AreEqual("Tatterjay",r.Owners.Single().BlueprintName);var birds=original.Owners.Where(e=>e.BlueprintName=="Magpie").ToArray();Assert.AreEqual(2,birds.Length);Assert.True(birds.All(e=>e.GetPart<InventoryPart>().Objects.Count==0));Assert.True(new TradeStockBuilder().BuildZone(f.Z,f.Factory,new Random(9)));Assert.True(birds.All(e=>e.GetPart<InventoryPart>().Objects.Count>=2));Assert.False(original.IsCurrent,"Do not silently refresh the stale original packet.");Assert.True(r.IsCurrent);Assert.AreEqual(0,r.Owners.Single().GetPart<InventoryPart>().Objects.Count);Assert.True(Consume(r));Assert.False(Consume(r));}}
  [TestCase("missing")][TestCase("foreign")][TestCase("duplicate")][TestCase("no-source")][TestCase("disabled")]
  public void NoActualSubstitutionCannotAuthorizeAReplacement(string fault)
  {using(var f=new Fixture(fault)){f.Build();var r=Receipt(f.Population);if(fault=="disabled")Assert.IsNull(r);else{Assert.NotNull(r);Assert.Zero(r.Owners.Count);}if(fault=="missing")Assert.AreEqual(3,f.Z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="Magpie"));}}
  [Test]public void MatchingDecoyAndAdditionalAuthoredCollectorAreNotTheSubstitutedRoll()
  {using(var f=new Fixture(extra:true)){var decoy=f.Factory.CreateEntity("Tatterjay");Assert.True(f.Z.AddEntity(decoy,1,1));var witness=new Random(31);Assert.AreEqual("Magpie",f.Population.Table.Roll(witness,f.Z.ZoneID)[0]);var cells=new List<(int x,int y)>();f.Z.ForEachCell((cell,x,y)=>{if(!cell.BlocksMovement()&&!f.Z.GenReservedCells.Contains((x,y)))cells.Add((x,y));});var expected=cells[witness.Next(cells.Count)];f.Build();var r=Receipt(f.Population);Assert.True(r.IsCurrent);Assert.AreEqual(1,r.Owners.Count);Assert.AreEqual(3,f.Z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="Tatterjay"));Assert.AreNotSame(decoy,r.Owners.Single());Assert.AreEqual(expected,f.Z.GetEntityPosition(r.Owners.Single()),"Only the actual first Magpie roll position is authorized, independently of dictionary enumeration.");}}
  [Test]public void CapturingTheReplacementDoesNotChangeRollOrderCoordinatesOrRng()
  {using(var a=new Fixture())using(var b=new Fixture()){b.Population.CaptureSourceReceipts=false;var ar=new Random(31);var br=new Random(31);Assert.True(a.Population.BuildZone(a.Z,a.Factory,ar));Assert.True(b.Population.BuildZone(b.Z,b.Factory,br));Assert.AreEqual(br.Next(),ar.Next());CollectionAssert.AreEqual(b.Z.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+b.Z.GetEntityPosition(e)),a.Z.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+a.Z.GetEntityPosition(e)));Assert.AreEqual(1,Receipt(a.Population).Owners.Count);}}
 }
}
