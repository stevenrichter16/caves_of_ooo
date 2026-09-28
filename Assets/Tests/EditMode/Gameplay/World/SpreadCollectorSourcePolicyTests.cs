using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCollectorSourcePolicyTests
 {
  static SpreadExplorationFamily Collector=>(SpreadExplorationFamily)7;
  [Test]public void ExactlyOneActuallyRolledMagpieChangesAndEverythingElseKeepsOrder()
  {var table=PopulationTable.SpreadTier1();var rolls=new[]{"Magpie","PetDog","Viper","Magpie","Hatchet","Tatterjay"};var before=rolls.ToArray();CollectionAssert.AreEqual(new[]{"Tatterjay","PetDog","Viper","Magpie","Hatchet","Tatterjay"},SpreadExplorationPopulationPolicy.Rewrite(table,rolls,Collector));CollectionAssert.AreEqual(before,rolls);}
  [TestCase("absent")][TestCase("foreign-table")][TestCase("duplicate")][TestCase("grouped")]
  public void NoUnambiguousActualMagpieAllowanceMeansNoNewBird(string fault)
  {var table=PopulationTable.SpreadTier1();var rolls=fault=="absent"?new[]{"PetDog","Viper"}:new[]{"Magpie","PetDog","Viper"};if(fault=="foreign-table")table.Name="foreign";if(fault=="duplicate")table.Entries.Add(new PopulationEntry{BlueprintName="Magpie",MinCount=1,MaxCount=1});if(fault=="grouped")table.Entries.Single(e=>e.BlueprintName=="Magpie").EncounterGroup="foreign";CollectionAssert.AreEqual(rolls,SpreadExplorationPopulationPolicy.Rewrite(table,rolls,Collector));}
 }
}
