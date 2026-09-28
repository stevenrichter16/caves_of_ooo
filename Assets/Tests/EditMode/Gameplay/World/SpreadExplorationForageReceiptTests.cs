using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationForageReceiptTests
 {
  static bool Consume(SpreadGenerationReceipt receipt)=>(bool)typeof(SpreadGenerationReceipt).GetMethod("TryConsume",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(receipt,null);

  static PopulationBuilder Builder(bool enabled=true,string table="SpreadTier1",bool duplicate=false){var t=new PopulationTable{Name=table,Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName="BerryBush",MinCount=1,MaxCount=1},new PopulationEntry{BlueprintName="Beehive",MinCount=1,MaxCount=1},new PopulationEntry{BlueprintName="Viper",MinCount=1,MaxCount=1,EncounterGroup="SpreadTier1Encounter"}}};if(duplicate)t.Entries.Add(new PopulationEntry{BlueprintName="BerryBush",MinCount=1,MaxCount=1});return new PopulationBuilder(t){CaptureSourceReceipts=enabled};}
  static SpreadGenerationReceipt Receipt(PopulationBuilder b){var p=typeof(PopulationBuilder).GetProperty("ForageSourceReceipt");Assert.NotNull(p,"A separately rolled finite food owner needs its own exact source receipt.");return (SpreadGenerationReceipt)p.GetValue(b);}
  [Test]public void OnlyActualFiniteFoodRollsAuthorizeTheSourceAndConsumptionIsOneShot()
  {using(var s=new DensityLootTestScope()){
   var z=new Zone("forage-receipt");var decoy=s.Factory.CreateEntity("BerryBush");z.AddEntity(decoy,2,2);var b=Builder();b.BuildZone(z,s.Factory,new Random(64));var r=Receipt(b);
   Assert.NotNull(r);Assert.True(r.IsCurrent);Assert.AreEqual(2,r.Owners.Count);CollectionAssert.AreEquivalent(new[]{"BerryBush","Beehive"},r.Owners.Select(e=>e.BlueprintName));Assert.False(r.Owners.Contains(decoy));
   Assert.True(b.SourceReceipt.Owners.All(e=>e.BlueprintName=="Viper"));Assert.True(Consume(r));Assert.False(Consume(r));
  }}
  [Test]public void AlteredFoodInvalidatesOnlyTheFoodReceiptAndEarlierRevisionCannotAuthorizeNewZone()
  {using(var s=new DensityLootTestScope()){
   var z=new Zone("forage-stale");var b=Builder();b.BuildZone(z,s.Factory,new Random(64));var r=Receipt(b);Assert.True(r.IsCurrent);r.Owners[0].GetPart<HarvestablePart>().Harvested=true;Assert.False(r.IsCurrent);Assert.True(b.SourceReceipt.IsCurrent);
   b.BuildZone(new Zone("forage-new"),s.Factory,new Random(64));Assert.False(r.IsCurrent);Assert.True(Receipt(b).IsCurrent);
  }}
  [TestCase(false,"SpreadTier1",false,0)][TestCase(true,"foreign",false,0)][TestCase(true,"SpreadTier1",true,1)]
  public void DisabledForeignAndAmbiguousRowsCannotPromoteMatchingFood(bool enabled,string table,bool duplicate,int count)
  {using(var s=new DensityLootTestScope()){var b=Builder(enabled,table,duplicate);b.BuildZone(new Zone("forage-counter"),s.Factory,new Random(64));var r=Receipt(b);if(!enabled)Assert.IsNull(r);else{Assert.NotNull(r);Assert.AreEqual(count,r.Owners.Count);if(duplicate)Assert.AreEqual("Beehive",r.Owners.Single().BlueprintName);}}}
 }
}
