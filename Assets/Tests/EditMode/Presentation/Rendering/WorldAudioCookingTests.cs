using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioCookingTests:WorldAudioWorkFixture
 {
  Entity Raw(int count=1){var e=Carry("RawMeat");e.GetPart<StackerPart>().StackCount=count;return e;}
  Entity Fire(bool finite=false,float heat=200){var e=Place("Campfire");e.GetPart<CampfirePart>().FiniteCooking=finite;e.GetPart<ThermalPart>().Temperature=heat;return e;}
  void One(){Assert.AreEqual(1,Audio.AcceptedSoundCount);CollectionAssert.AreEqual(new[]{"CookPrepare take 0"},Assigned());Assert.AreEqual(16,Audio.AllocatedSources);Assert.AreEqual(0,Audio.CombatFocusCount);}
  [TestCase(1)][TestCase(3)] public void ActualCookingSoundsOncePerStackAfterRealConversion(int count)
  {Fire();var raw=Raw(count);Assert.True(Act(raw,"Cook"));Assert.False(Pack.Objects.Contains(raw));Assert.AreEqual(count,Count("CookedMeat"));One();}
  [TestCase(false)][TestCase(true)] public void ParentCommitControlsCookingPublication(bool commit)
  {Fire();var raw=Raw(2);var tx=new InventoryTransaction();Stage(Actor,raw,Zone,"Cook",tx);Assert.AreEqual(2,Count("CookedMeat"));Assert.IsEmpty(Assigned());if(commit)tx.Commit();else tx.Rollback();Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount);Assert.AreEqual(commit?2:0,Count("CookedMeat"));tx.Commit();Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount);}
  [Test] public void ActualOuterFailureRestoresRawFoodAndIsSilent()
  {Fire();var raw=Raw(2);FailAfter();Assert.False(Act(raw,"Cook"));Assert.True(Pack.Objects.Contains(raw));Assert.AreEqual(2,raw.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,Count("CookedMeat"));Assert.IsEmpty(Assigned());}
  [Test] public void ColdFiniteRefusalHasNoCueAndReheatedCounterDoes()
  {var fire=Fire(true,100);var raw=Raw();Assert.False(Act(raw,"Cook"));Assert.IsEmpty(Assigned());fire.GetPart<ThermalPart>().Temperature=200;Assert.True(Act(raw,"Cook"));One();}
  [Test] public void ColdLegacyStationRemainsAValidPreparationSource()
  {var fire=Fire(false,25);fire.GetPart<FuelPart>().FuelMass=0;var raw=Raw();Assert.True(Act(raw,"Cook"));One();}
  [TestCase("move")][TestCase("hide")][TestCase("cool-finite")][TestCase("part-replace")]
  public void CallbackStaleStationCannotPublishFreshPreparation(string mutation)
  {
   var fire=Fire(true);var raw=Raw();var after=new CookingAudioAfterPart{After=()=>{if(mutation=="move")Zone.MoveEntity(fire,12,10);if(mutation=="hide")fire.GetPart<RenderPart>().Visible=false;if(mutation=="cool-finite")fire.GetPart<ThermalPart>().Temperature=25;if(mutation=="part-replace"){fire.RemovePart(fire.GetPart<CampfirePart>());fire.AddPart(new CampfirePart{FiniteCooking=true});}}};Actor.AddPart(after);
   Assert.True(Act(raw,"Cook"));Assert.AreEqual(1,Count("CookedMeat"));Assert.IsEmpty(Assigned());
   after.After=null;Zone.MoveEntity(fire,11,10);fire.GetPart<RenderPart>().Visible=true;fire.GetPart<ThermalPart>().Temperature=200;Assert.True(Act(Raw(),"Cook"));One();
  }
  [TestCase(false)][TestCase(true)] public void ExplicitPlayerCookingFinishesFromInventoryMenu(bool menu)
  {Router.SetWorld(Zone,Actor,!menu);Fire();Assert.True(Act(Raw(),"Cook"));One();}
  [Test] public void MissingStationRefusesWithoutPretendKitchenSound()
  {var raw=Raw();Assert.False(Act(raw,"Cook"));Assert.IsEmpty(Assigned());Fire();Assert.True(Act(raw,"Cook"));One();}
  [Test] public void GenericSemanticCannotForgePreparation()
  {Assert.True(Enum.TryParse("CookPrepare",out WorldAudioCue cue));WorldAudioHooks.Emit(cue,Actor,Zone,10,10);Assert.IsEmpty(Assigned());Fire();Assert.True(Act(Raw(),"Cook"));One();}
 }
 public sealed class CookingAudioAfterPart:Part
 {public Action After;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")After?.Invoke();return true;}}
}
