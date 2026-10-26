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
  public static string[] Rewrite(PopulationTable table,IReadOnlyList<string> rolled,SpreadExplorationFamily family,bool connectedKitchen=false)
   => Rewrite(table,rolled,family,connectedKitchen,false);

  /// <summary>Rewrites unpublished ordinary rolls. The caller must establish
  /// exact cold manager, version and zone authority before either service flag
  /// is true; this method additionally checks family and encounter-table shape.</summary>
  public static string[] Rewrite(PopulationTable table,IReadOnlyList<string> rolled,SpreadExplorationFamily family,
   bool connectedKitchen,bool connectedKeeper)
  {
   bool inhabitedService=connectedKitchen&&family==SpreadExplorationFamily.WaysideKitchen
    ||connectedKeeper&&family==SpreadExplorationFamily.SeedKeepersPlot;
   if(rolled==null)throw new ArgumentNullException(nameof(rolled));
   var result=rolled.ToArray();
   if(table?.Name!="SpreadTier1"||table.Entries==null)return result;
   if(family==SpreadExplorationFamily.OccupiedBank||inhabitedService)
   {
    var entries=table.Entries.Where(e=>e!=null&&e.EncounterGroup==Group).ToArray();
    if(entries.Length!=2||entries.Count(e=>e.BlueprintName=="Viper")!=1||entries.Count(e=>e.BlueprintName=="MarlbackScrabbler")!=1
      ||table.Entries.Any(e=>e!=null&&e.EncounterGroup!=Group&&(e.BlueprintName=="Viper"||e.BlueprintName=="MarlbackScrabbler")))return result;
    // The exact inhabited opening services borrow each actual encounter allowance.
    // No roll means no grazer; other domestic sites keep their ordinary danger.
    if(inhabitedService)
     return result.Select(bp=>bp=="Viper"||bp=="MarlbackScrabbler"?"ReedbackGrazer":bp).ToArray();
    var output=new List<string>();bool replaced=false;
    foreach(string bp in result)
    {
     if(bp!="Viper"&&bp!="MarlbackScrabbler"){output.Add(bp);continue;}
     if(!replaced){output.Add("MarlbackScrabbler");replaced=true;}
    }
    return output.ToArray();
   }
   if(family==SpreadExplorationFamily.LastGleanings||family==SpreadExplorationFamily.CollectorReturn)
   {
    var entries=table.Entries.Where(e=>e!=null&&e.BlueprintName=="Magpie").ToArray();
    if(entries.Length!=1||!string.IsNullOrEmpty(entries[0].EncounterGroup))return result;
    int index=Array.IndexOf(result,"Magpie");if(index>=0)result[index]=family==SpreadExplorationFamily.CollectorReturn?"Tatterjay":"ReedbackGrazer";
   }
   return result;
  }
 }
}
