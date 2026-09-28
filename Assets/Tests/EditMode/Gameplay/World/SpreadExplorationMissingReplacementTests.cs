using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class SpreadExplorationMissingReplacementTests
 {
  [TestCase("OccupiedBank",false)][TestCase("OccupiedBank",true)]
  [TestCase("LastGleanings",false)][TestCase("LastGleanings",true)]
  public void MissingRequiredBlueprintKeepsActualOriginalAllowance(string family,bool hasReplacement)
  {
   using(var scope=new SpreadExplorationActorTests.Scope())
   {
    string replacement=family=="OccupiedBank"?"MarlbackScrabbler":"ReedbackGrazer";
    var names=new[]{"Viper","MarlbackScrabbler","Magpie","ReedbackGrazer","Hatchet"}.Where(n=>hasReplacement||n!=replacement);
    var factory=new EntityFactory();factory.LoadBlueprints("{\"Objects\":["+string.Join(",",names.Select(n=>"{\"Name\":\""+n+"\",\"Parts\":[{\"Name\":\"Physics\"}]}"))+"]}");
    var manager=OverworldZoneManager.CreateDetached(factory,64,true);var plan=manager.Exploration;
    var entry=plan.Entries.First(e=>e.PlacementEligible&&e.Family.ToString()==family);var z=new Zone(entry.ZoneID);
    var guard=(IZoneBuilder)typeof(SpreadExplorationPlan).GetMethod("Guard",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(plan,new object[]{manager,z.ZoneID});Assert.NotNull(guard);Assert.True(guard.BuildZone(z,factory,new Random(1)));
    var table=new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{
     new PopulationEntry{BlueprintName="Hatchet",MinCount=1,MaxCount=1},
     new PopulationEntry{BlueprintName="Magpie",MinCount=2,MaxCount=2},
     new PopulationEntry{BlueprintName="Viper",MinCount=2,MaxCount=2,EncounterGroup="SpreadTier1Encounter"},
     new PopulationEntry{BlueprintName="MarlbackScrabbler",Weight=0,MinCount=1,MaxCount=1,EncounterGroup="SpreadTier1Encounter"}}};
    var rng=new Random(17);var builder=new PopulationBuilder(table){ExplorationManager=manager,CaptureSourceReceipts=true};Assert.True(builder.BuildZone(z,factory,rng));
    var actual=z.GetReadOnlyEntities().Select(e=>e.BlueprintName).ToArray();Assert.AreEqual(1,actual.Count(n=>n=="Hatchet"));
    if(hasReplacement)
    {Assert.AreEqual(1,actual.Count(n=>n==replacement));Assert.AreEqual(family=="OccupiedBank"?0:2,actual.Count(n=>n=="Viper"));Assert.AreEqual(family=="LastGleanings"?1:2,actual.Count(n=>n=="Magpie"));}
    else
    {
     CollectionAssert.AreEquivalent(new[]{"Hatchet","Magpie","Magpie","Viper","Viper"},actual);
     var baseline=new Zone(z.ZoneID);var baselineRng=new Random(17);Assert.True(new PopulationBuilder(table).BuildZone(baseline,factory,baselineRng));
     Assert.AreEqual(baselineRng.Next(),rng.Next(),"Refusing substitution adds no random draws.");
     CollectionAssert.AreEqual(baseline.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+baseline.GetEntityPosition(e)),z.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+z.GetEntityPosition(e)));
    }
    Assert.True(builder.SourceReceipt.IsCurrent);Assert.True(builder.AmbientSourceReceipt.IsCurrent);Assert.AreEqual(0,plan.DispositionFor(z.ZoneID));
   }
  }
 }
}
