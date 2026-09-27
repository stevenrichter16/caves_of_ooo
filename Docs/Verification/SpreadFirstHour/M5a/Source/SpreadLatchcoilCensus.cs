using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests {
 public sealed class SpreadLatchcoilCensus {
  [Test] public void ObserveThreePredeclaredGeneratedSourcesWithoutForcingAdmission() {
   using(var f=new DensityLootTestScope()) {
    var rows=new System.Collections.Generic.List<string>();
    foreach(int seed in new[]{1,64,1729}) {
     f.Seed(seed);var m=OverworldZoneManager.CreateDetached(f.Factory,seed);string id=m.RareEncounters.ViperZoneID;
     Assert.IsNotEmpty(id);var z=m.GetZone(id);var owned=z.GetReadOnlyEntities().Where(e=>e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).ToArray();
     int snakes=owned.Count(e=>e.BlueprintName=="SpreadLatchcoil"),signs=owned.Count(e=>e.BlueprintName=="Signpost");Assert.That(snakes,Is.InRange(0,1));Assert.AreEqual(snakes,signs);Assert.AreEqual(snakes*2,owned.Length);
     string row="{\"seed\":"+seed+",\"zone\":\""+id+"\",\"pair\":\""+m.RareEncounters.PairZoneID+"\",\"latchcoils\":"+snakes+",\"warnings\":"+signs+",\"owners\":"+z.GetReadOnlyEntities().Count+",\"placements\":\""+string.Join(";",owned.Select(e=>e.BlueprintName+":"+z.GetEntityPosition(e)))+"\"}";rows.Add(row);TestContext.WriteLine(row);
    }
    string output=Environment.GetEnvironmentVariable("COO_LATCHCOIL_CENSUS");if(!string.IsNullOrEmpty(output))File.WriteAllText(output,"[\n"+string.Join(",\n",rows)+"\n]\n");
   }
  }
 }
}
