using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;
namespace CavesOfOoo.Tests
{
 public class SpreadExplorationReceiptTests
 {
  EntityFactory factory;
  [SetUp]public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));}
  PopulationBuilder Builder(bool capture=true)=>new PopulationBuilder(new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{
   new PopulationEntry{BlueprintName="Hatchet",MinCount=1,MaxCount=1},new PopulationEntry{BlueprintName="Magpie",MinCount=1,MaxCount=1},
   new PopulationEntry{BlueprintName="Viper",MinCount=1,MaxCount=1,EncounterGroup="SpreadTier1Encounter"}}}){CaptureSourceReceipts=capture};
  static SpreadGenerationReceipt Receipt(PopulationBuilder b,string name)
  {var property=typeof(PopulationBuilder).GetProperty(name);Assert.NotNull(property,"A creature-group receipt cannot authorize loose items or ambient fauna.");return (SpreadGenerationReceipt)property.GetValue(b);}
  [TestCase("LooseSourceReceipt","Hatchet")][TestCase("AmbientSourceReceipt","Magpie")]
  public void ReceiptNamesOnlyItsOwnActualRollAndIgnoresMatchingDecoys(string name,string bp)
  {
   var z=new Zone("receipt-source");var decoy=factory.CreateEntity(bp);Assert.True(z.AddEntity(decoy,2,2));var b=Builder();Assert.True(b.BuildZone(z,factory,new Random(64)));
   var receipt=Receipt(b,name);Assert.NotNull(receipt);Assert.True(receipt.IsCurrent);Assert.AreEqual(1,receipt.Owners.Count);Assert.AreEqual(bp,receipt.Owners[0].BlueprintName);Assert.AreNotSame(decoy,receipt.Owners[0]);
   Assert.True(b.SourceReceipt.Owners.All(e=>e.BlueprintName=="Viper"));Assert.False(receipt.Owners.Any(e=>e.BlueprintName=="Viper"));
   z.MoveEntity(receipt.Owners[0],3,3);Assert.False(receipt.IsCurrent,"Later relocation invalidates unclaimed provenance.");
  }
  [TestCase("LooseSourceReceipt")][TestCase("AmbientSourceReceipt")]
  public void ReusedBuilderCannotAuthorizeOwnersFromEarlierZone(string name)
  {var b=Builder();var z=new Zone("receipt-before");b.BuildZone(z,factory,new Random(1));var old=Receipt(b,name);Assert.True(old.IsCurrent);b.BuildZone(new Zone("receipt-next"),factory,new Random(1));Assert.False(old.IsCurrent);Assert.True(Receipt(b,name).IsCurrent);}
  [Test]public void DisabledReceiptsDoNotAcquireSources()
  {var b=Builder(false);b.BuildZone(new Zone("receipt-off"),factory,new Random(64));Assert.IsNull(Receipt(b,"LooseSourceReceipt"));Assert.IsNull(Receipt(b,"AmbientSourceReceipt"));Assert.IsNull(b.SourceReceipt);}
  [Test]public void FinalPacketProofSeesLaterStockAndPartChanges()
  {
   var method=typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
   Assert.NotNull(method,"Disposition must remain tied to actual final owners after later generation callbacks.");
   var z=new Zone("final-proof");var item=factory.CreateEntity("Hatchet");z.AddEntity(item,4,5);
   var proof=(Func<bool>)method.Invoke(null,new object[]{z,new[]{item}});Assert.True(proof());
   item.GetPart<CommercePart>().Value++;Assert.False(proof());
  }
  [Test]public void FinalPacketProofRejectsOwnerReplacementAndAcceptsQuietEmptyPacket()
  {
   var method=typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);Assert.NotNull(method);
   var z=new Zone("final-proof-identity");var item=factory.CreateEntity("Hatchet");z.AddEntity(item,4,5);
   var proof=(Func<bool>)method.Invoke(null,new object[]{z,new[]{item}});Assert.True(proof());
   z.RemoveEntity(item);var other=factory.CreateEntity("Hatchet");other.ID=item.ID;z.AddEntity(other,4,5);Assert.False(proof());
   Assert.True(((Func<bool>)method.Invoke(null,new object[]{z,new Entity[0]}))());
  }
  [TestCase(false)][TestCase(true)]public void FinalPacketProofPinsCurrentBrainCollections(bool goals)
  {
   var method=typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);Assert.NotNull(method);
   var z=new Zone("final-brain-proof");var actor=factory.CreateEntity("Magpie");z.AddEntity(actor,4,5);
   var proof=(Func<bool>)method.Invoke(null,new object[]{z,new[]{actor}});Assert.True(proof());var brain=actor.GetPart<BrainPart>();
   if(goals)brain.PushGoal(new BoredGoal());else brain.PersonalEnemies.Add(new Entity{ID="new-personal-enemy"});
   Assert.False(proof(),"In-place behavioral collections cannot evade final source identity checks.");
  }
 }
}
