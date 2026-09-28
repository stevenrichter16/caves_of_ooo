using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
namespace CavesOfOoo.Core
{
 /// <summary>Finite replacement of actual ordinary roll allowances. This pure
 /// policy creates no owners and reads no RNG; callers must establish cold-plan authority.</summary>
 public static class SpreadExplorationPopulationPolicy
 {
  const string Group="SpreadTier1Encounter";
  public static string[] Rewrite(PopulationTable table,IReadOnlyList<string> rolled,SpreadExplorationFamily family)
  {
   if(rolled==null)throw new ArgumentNullException(nameof(rolled));
   var result=rolled.ToArray();
   if(table?.Name!="SpreadTier1"||table.Entries==null)return result;
   if(family==SpreadExplorationFamily.OccupiedBank)
   {
    var entries=table.Entries.Where(e=>e!=null&&e.EncounterGroup==Group).ToArray();
    if(entries.Length!=2||entries.Count(e=>e.BlueprintName=="Viper")!=1||entries.Count(e=>e.BlueprintName=="MarlbackScrabbler")!=1
      ||table.Entries.Any(e=>e!=null&&e.EncounterGroup!=Group&&(e.BlueprintName=="Viper"||e.BlueprintName=="MarlbackScrabbler")))return result;
    var output=new List<string>();bool replaced=false;
    foreach(string bp in result)
    {
     if(bp!="Viper"&&bp!="MarlbackScrabbler"){output.Add(bp);continue;}
     if(!replaced){output.Add("MarlbackScrabbler");replaced=true;}
    }
    return output.ToArray();
   }
   if(family==SpreadExplorationFamily.LastGleanings)
   {
    var entries=table.Entries.Where(e=>e!=null&&e.BlueprintName=="Magpie").ToArray();
    if(entries.Length!=1||!string.IsNullOrEmpty(entries[0].EncounterGroup))return result;
    int index=Array.IndexOf(result,"Magpie");if(index>=0)result[index]="ReedbackGrazer";
   }
   return result;
  }
 }
}
