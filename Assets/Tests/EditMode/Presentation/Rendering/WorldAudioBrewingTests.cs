using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioBrewingTests:WorldAudioWorkFixture
 {
  Dictionary<string,BrewRule> oldRules;BrewRule[] oldOrder;bool oldInitialized;
  static FieldInfo Field(string name)=>typeof(BrewRuleRegistry).GetField(name,BindingFlags.NonPublic|BindingFlags.Static);
  [SetUp] public void Rules()
  {oldRules=new Dictionary<string,BrewRule>((Dictionary<string,BrewRule>)Field("RulesById").GetValue(null));oldOrder=((List<BrewRule>)Field("RulesInOrder").GetValue(null)).ToArray();oldInitialized=(bool)Field("_initialized").GetValue(null);BrewRuleRegistry.InitializeFromJson(Resources.Load<TextAsset>("Content/Data/Alchemy/BrewRules").text);}
  [TearDown]public void RestoreRules()
  {var d=(Dictionary<string,BrewRule>)Field("RulesById").GetValue(null);d.Clear();foreach(var p in oldRules)d[p.Key]=p.Value;var list=(List<BrewRule>)Field("RulesInOrder").GetValue(null);list.Clear();list.AddRange(oldOrder);Field("_initialized").SetValue(null,oldInitialized);}
  Entity Reagent(string properties,int count=1)
  {var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="audio-reagent"};e.AddPart(new PhysicsPart{Takeable=true});e.AddPart(new RenderPart());e.AddPart(new ReagentPart{PropertiesRaw=properties});e.AddPart(new StackerPart{StackCount=count});Assert.True(Pack.AddObject(e));return e;}
  bool Brew(Entity reagent,int count=1)=>InventorySystem.ExecuteCommand(new BrewReagentsCommand(new[]{reagent},Factory,count),Actor,Zone).Success;
  void One(string cue="BrewPrepare")
  {CollectionAssert.AreEqual(new[]{cue+" take 0"},Assigned());Assert.AreEqual(1,Audio.AcceptedSoundCount);Assert.AreEqual(16,Audio.AllocatedSources);Assert.AreEqual(0,Audio.CombatFocusCount);}
  [Test]public void SingleFieldBrewDoesNotNeedAStationToHaveARealVesselCue()
  {var r=Reagent("binding:1");Assert.True(Brew(r));Assert.False(Pack.Objects.Contains(r));Assert.True(Pack.Objects.Any(e=>e.HasPart<BrewItemPart>()));One();}
  [TestCase(1)][TestCase(3)]public void CommittedBatchHasOnePhraseNotOnePerUnit(int count)
  {Place("AlchemyStill");var r=Reagent("binding:1",count);Assert.True(Brew(r,count));Assert.False(Pack.Objects.Contains(r));One();}
  [Test]public void PaidPartialBatchSoundsOnceAndKeepsItsRealYield()
  {Place("AlchemyStill");var r=Reagent("binding:1",2);Assert.True(Brew(r,4));Assert.False(Pack.Objects.Contains(r));Assert.AreEqual(2,Pack.Objects.Where(e=>e.HasPart<BrewItemPart>()).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));One();}
  [Test]public void MissingBatchStationRefusesBeforeAnyMaterialOrSound()
  {var r=Reagent("binding:1",2);Assert.False(Brew(r,2));Assert.AreEqual(2,r.GetPart<StackerPart>().StackCount);Assert.IsEmpty(Assigned());Place("AlchemyStill");Assert.True(Brew(r,2));One();}
  [Test]public void FoodOnlyFieldBatchUsesPreparationRatherThanPretendGlass()
  {var r=Reagent("sweet:1",2);Assert.True(Brew(r,2));Assert.False(Pack.Objects.Contains(r));One("CookPrepare");}
  [Test]public void ActualMishapConsumesAndUsesNeutralPreparationWithoutCombatFocus()
  {var r=Reagent("volatile:1");int hp=Actor.GetStatValue("Hitpoints");Assert.True(Brew(r));Assert.False(Pack.Objects.Contains(r));Assert.Less(Actor.GetStatValue("Hitpoints"),hp);Assert.Greater(Actor.GetStatValue("Hitpoints"),0);One();}
  [Test]public void EmptySelectionHasNoPreparationCue()
  {Assert.False(InventorySystem.ExecuteCommand(new BrewReagentsCommand(Array.Empty<Entity>(),Factory),Actor,Zone).Success);Assert.IsEmpty(Assigned());Assert.True(Brew(Reagent("binding:1")));One();}
  [TestCase(false)][TestCase(true)]public void ActualPlayerPackBrewCanFinishInItsMenu(bool menu)
  {Router.SetWorld(Zone,Actor,!menu);Assert.True(Brew(Reagent("binding:1")));One();}
  [Test]public void GenericSemanticCannotForgeTheBrewingReceipt()
  {Assert.True(Enum.TryParse("BrewPrepare",out WorldAudioCue cue));WorldAudioHooks.Emit(cue,Actor,Zone,10,10);Assert.IsEmpty(Assigned());Assert.True(Brew(Reagent("binding:1")));One();}
 }
}
