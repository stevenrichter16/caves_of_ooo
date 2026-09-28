using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class SpreadExplorationSourcePolicyTests
 {
  static string[] Rewrite(PopulationTable table,string[] rolled,string family)
  {
   var asm=typeof(PopulationBuilder).Assembly;var type=asm.GetType("CavesOfOoo.Core.SpreadExplorationPopulationPolicy");
   Assert.NotNull(type,"Regional source budgets must be applied to actual rolls, not extra spawns after generation.");
   var method=type.GetMethod("Rewrite",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method);
   var role=method.GetParameters()[2].ParameterType;
   return (string[])method.Invoke(null,new object[]{table,rolled,Enum.Parse(role,family)});
  }
  [TestCase("Viper")][TestCase("MarlbackScrabbler")]
  public void OccupiedBankReplacesExactlyTheExistingHostileAllowance(string hostile)
  {
   var original=new[]{"Magpie",hostile,"Hatchet",hostile,"BerryBush","Magpie"};var saved=original.ToArray();
   CollectionAssert.AreEqual(new[]{"Magpie","MarlbackScrabbler","Hatchet","BerryBush","Magpie"},Rewrite(PopulationTable.SpreadTier1(),original,"OccupiedBank"));
   CollectionAssert.AreEqual(saved,original);
   CollectionAssert.AreEqual(original,Rewrite(PopulationTable.SpreadTier1(),original,"None"));
  }
  [Test] public void NoHostileRollDoesNotMintATerritoryHolder()
  {var original=new[]{"Magpie","Hatchet"};CollectionAssert.AreEqual(original,Rewrite(PopulationTable.SpreadTier1(),original,"OccupiedBank"));}
  [Test] public void GrazerUsesOnlyOneActuallyRolledAmbientSlot()
  {
   var original=new[]{"Magpie","PetDog","Viper","Magpie","Hatchet"};
   CollectionAssert.AreEqual(new[]{"ReedbackGrazer","PetDog","Viper","Magpie","Hatchet"},Rewrite(PopulationTable.SpreadTier1(),original,"LastGleanings"));
   CollectionAssert.AreEqual(original,Rewrite(PopulationTable.SpreadTier1(),original,"RoadSpill"));
  }
  [Test] public void NoAmbientRollCannotCreateAnAnimal()
  {var original=new[]{"PetDog","Viper"};CollectionAssert.AreEqual(original,Rewrite(PopulationTable.SpreadTier1(),original,"LastGleanings"));}
  [TestCase("OccupiedBank")][TestCase("LastGleanings")]
  public void ForeignAndAmbiguousTablesRetainTheirRolls(string family)
  {
   var original=new[]{"Magpie","Viper","Viper"};var table=PopulationTable.SpreadTier1();table.Name="Custom";
   CollectionAssert.AreEqual(original,Rewrite(table,original,family));table.Name="SpreadTier1";
   table.Entries.Add(new PopulationEntry{BlueprintName=family=="OccupiedBank"?"Viper":"Magpie",MinCount=0,MaxCount=1});
   CollectionAssert.AreEqual(original,Rewrite(table,original,family));
  }
  [TestCase("RoadSpill")][TestCase("WateringMargin")][TestCase("None")]
  public void ResourceFamiliesDoNotChangeNormalPopulation(string family)
  {var original=new[]{"Magpie","Viper","Hatchet"};CollectionAssert.AreEqual(original,Rewrite(PopulationTable.SpreadTier1(),original,family));}
 }
}
