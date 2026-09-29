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
 internal sealed class HaulingContentScope:IDisposable
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  readonly List<(FieldInfo field,object value,DictionaryEntry[] rows)> globals=new List<(FieldInfo,object,DictionaryEntry[])>();
  readonly DensityLootTestScope inner;readonly object settlement=SettlementManager.Current;
  readonly List<MessageLog.Entry> messages=MessageLog.GetAllEntries();readonly List<string> announcements=MessageLog.GetPendingAnnouncementsSnapshot();readonly int flash=MessageLog.FlashStamp,serial=(int)typeof(MessageLog).GetField("NextSerial",All).GetValue(null);
  public EntityFactory Factory=>inner.Factory;public void Seed(int seed)=>inner.Seed(seed);
  public HaulingContentScope()
  {foreach(var field in typeof(LootTableRegistry).GetFields(All)){if(!field.IsStatic)continue;var value=field.GetValue(null);var rows=new List<DictionaryEntry>();if(value is IDictionary d)foreach(DictionaryEntry row in d)rows.Add(row);globals.Add((field,value,rows.ToArray()));}inner=new DensityLootTestScope();}
  public void Dispose()
  {try{inner.Dispose();}finally{foreach(var g in globals){if(!g.field.IsLiteral&&!g.field.IsInitOnly)g.field.SetValue(null,g.value);if(g.value is IDictionary d){d.Clear();foreach(var row in g.rows)d.Add(row.Key,row.Value);}}typeof(SettlementManager).GetProperty("Current",All).SetValue(null,settlement);MessageLog.Restore(messages,announcements,flash,serial);}}
 }
 public sealed class SpreadHaulingSourceTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  HaulingContentScope scope;
  [SetUp]public void Setup(){scope=new HaulingContentScope();scope.Seed(64);}
  [TearDown]public void Cleanup(){scope.Dispose();}
  static void Capture(HaulablePropBuilder b,bool enabled){var f=typeof(HaulablePropBuilder).GetField("CaptureSourceReceipts",All);Assert.NotNull(f,"Capture actual ordinary 4200 source without granting scan authority.");f.SetValue(b,enabled);}
  static SpreadGenerationReceipt Receipt(HaulablePropBuilder b){var p=typeof(HaulablePropBuilder).GetProperty("SourceReceipt",All);Assert.NotNull(p);return(SpreadGenerationReceipt)p.GetValue(b);}
  sealed class Roll:Random{readonly int item;public int Calls;public Roll(int item){this.item=item;}public override int Next(int max){Calls++;return max==1000?0:item;}public override int Next(int min,int max){Calls++;return min==1?20:min;}}
  [TestCase(0,"HaulBarrel")][TestCase(1,"FallenBeam")][TestCase(2,"MillStone")]
  public void EnabledCaptureOwnsExactlyTheActualRolledOwnerWithoutExtraRng(int item,string bp)
  {
   var z=new Zone("Overworld.4.4.0");var b=new HaulablePropBuilder(BiomeType.Spread);Capture(b,true);var rng=new Roll(item);Assert.True(b.BuildZone(z,scope.Factory,rng));
   var receipt=Receipt(b);Assert.NotNull(receipt);Assert.True(receipt.IsCurrent);Assert.AreSame(z,receipt.Zone);Assert.AreSame(scope.Factory,receipt.Factory);Assert.AreEqual(1,receipt.Owners.Count);Assert.AreEqual(bp,receipt.Owners.Single().BlueprintName);Assert.AreEqual(4,rng.Calls);Assert.AreEqual(1,z.EntityCount);
   Assert.True((bool)typeof(SpreadGenerationReceipt).GetMethod("TryConsume",All).Invoke(receipt,null));Assert.False(receipt.IsCurrent);Assert.False((bool)typeof(SpreadGenerationReceipt).GetMethod("TryConsume",All).Invoke(receipt,null));
  }
  [TestCase(false)][TestCase(true)]public void DisabledAndEnabledProducerAreOwnerQuantityPositionAndRngEquivalent(bool capture)
  {var z=new Zone("Overworld.4.4.0");var b=new HaulablePropBuilder(BiomeType.Spread);Capture(b,capture);var rng=new Roll(1);Assert.True(b.BuildZone(z,scope.Factory,rng));Assert.AreEqual(1,z.EntityCount);var e=z.GetReadOnlyEntities().Single();Assert.AreEqual("FallenBeam",e.BlueprintName);Assert.AreEqual((20,20),z.GetEntityPosition(e));Assert.AreEqual(4,rng.Calls);Assert.AreEqual(capture,Receipt(b)!=null);}
  [TestCase("disabled")][TestCase("rebuild")][TestCase("moved")][TestCase("replaced")][TestCase("weight")]
  public void ReceiptCannotSurviveChangedAuthorityOrExactOwner(string fault)
  {
   var z=new Zone("Overworld.4.4.0");var b=new HaulablePropBuilder(BiomeType.Spread);Capture(b,true);Assert.True(b.BuildZone(z,scope.Factory,new Roll(1)));var r=Receipt(b);Assert.True(r.IsCurrent);var e=r.Owners.Single();
   if(fault=="disabled")Capture(b,false);if(fault=="rebuild"){b.ChancePerMille=0;Assert.True(b.BuildZone(new Zone(z.ZoneID),scope.Factory,new Roll(1)));Assert.Null(Receipt(b));}
   if(fault=="moved")Assert.True(z.MoveEntity(e,21,20));if(fault=="replaced"){Assert.True(z.RemoveEntity(e));var other=scope.Factory.CreateEntity(e.BlueprintName);other.ID=e.ID;Assert.True(z.AddEntity(other,20,20));}
   if(fault=="weight")e.GetPart<PhysicsPart>().Weight++;Assert.False(r.IsCurrent);
  }
  [TestCase("no-roll")][TestCase("late-capture")][TestCase("missing-blueprint")][TestCase("no-space")]
  public void MatchingPreexistingOwnerDoesNotBecomeAProducedClaim(string fault)
  {
   var z=new Zone("Overworld.4.4.0");var old=scope.Factory.CreateEntity("FallenBeam");Assert.True(z.AddEntity(old,3,3));var b=new HaulablePropBuilder(BiomeType.Spread);Capture(b,fault!="late-capture");
   if(fault=="no-roll")b.ChancePerMille=0;if(fault=="missing-blueprint")scope.Factory.Blueprints.Remove("FallenBeam");if(fault=="no-space")z.GenReservedCells.Add((20,20));
   Assert.True(b.BuildZone(z,scope.Factory,new Roll(1)));if(fault=="late-capture")Capture(b,true);Assert.Null(Receipt(b));Assert.AreSame(z.GetCell(3,3),z.GetEntityCell(old));
  }
 }
}
