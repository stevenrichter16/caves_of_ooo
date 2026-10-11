using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioMineralHarvestTests:WorldAudioWorkFixture
 {
  Entity Vein(){var e=Place("GlowQuartzVein");var h=e.GetPart<HarvestablePart>();h.YieldMin=h.YieldMax=1;h.YieldChance=100;return e;}
  void One(string cue="HarvestMineral"){CollectionAssert.AreEqual(new[]{cue+" take 0"},Assigned());Assert.AreEqual(1,Audio.AcceptedSoundCount);Assert.AreEqual(16,Audio.AllocatedSources);Assert.AreEqual(0,Audio.CombatFocusCount);}
  [Test]public void RealQuartzHarvestUsesStoneHandlingAfterConsumingItsOriginalSeam()
  {var source=Vein();Assert.True(Act(source,"Harvest"));Assert.True(source.GetPart<HarvestablePart>().Harvested);Assert.Null(Zone.GetEntityCell(source));Assert.AreEqual(1,Count("GlowQuartz"));One();Assert.False(Act(source,"Harvest"));Assert.AreEqual(1,Audio.AcceptedSoundCount);}
  [TestCase("StoneburrPatch")][TestCase("FrostLichenPatch")]
  public void ActualPlantPatchUsesThePlantFamilyNotStone(string name)
  {var source=Place(name);var h=source.GetPart<HarvestablePart>();h.YieldChance=100;h.YieldMin=h.YieldMax=1;Assert.True(Act(source,"Harvest"));Assert.True(h.Harvested);Assert.AreEqual(1,Count(h.YieldBlueprint));One("HarvestPlant");}
  [TestCase(false)][TestCase(true)]public void ParentTransactionControlsPublicationAndSpentState(bool commit)
  {var source=Vein();var tx=new InventoryTransaction();Stage(Actor,source,Zone,"Harvest",tx);Assert.Null(Zone.GetEntityCell(source));Assert.AreEqual(0,Audio.AcceptedSoundCount);if(commit)tx.Commit();else tx.Rollback();Assert.AreEqual(commit,source.GetPart<HarvestablePart>().Harvested);Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount);Assert.AreEqual(commit?1:0,Count("GlowQuartz"));tx.Commit();Assert.AreEqual(commit?1:0,Audio.AcceptedSoundCount);}
  [Test]public void OuterFailureRestoresTheActualSeamAndIsSilent()
  {var source=Vein();FailAfter();Assert.False(Act(source,"Harvest"));Assert.False(source.GetPart<HarvestablePart>().Harvested);Assert.NotNull(Zone.GetEntityCell(source));Assert.AreEqual(0,Count("GlowQuartz"));Assert.IsEmpty(Assigned());}
  [Test]public void AValidUnproductiveAttemptSoundsOnceButDoesNotInventAReward()
  {var source=Vein();source.GetPart<HarvestablePart>().YieldChance=0;Assert.True(Act(source,"Harvest"));Assert.True(source.GetPart<HarvestablePart>().Harvested);Assert.AreEqual(0,Count("GlowQuartz"));One();Assert.False(Act(source,"Harvest"));Assert.AreEqual(1,Audio.AcceptedSoundCount);}
  [TestCase("hidden")][TestCase("material")][TestCase("relocated-actor")][TestCase("restored-source")]
  public void LaterCallbackCannotPublishStaleConsumedSourceEvidence(string mutation)
  {
   var source=Vein();var hook=new HarvestAudioAfterPart{After=()=>{if(mutation=="hidden")source.GetPart<RenderPart>().Visible=false;if(mutation=="material")source.AddPart(new MaterialPart{MaterialID="Wood"});if(mutation=="relocated-actor")Zone.MoveEntity(Actor,10,9);if(mutation=="restored-source")Assert.True(Zone.AddEntity(source,11,10));}};Actor.AddPart(hook);
   Assert.True(Act(source,"Harvest"));Assert.AreEqual(1,Count("GlowQuartz"));Assert.IsEmpty(Assigned());hook.After=null;Zone.MoveEntity(Actor,10,10);var fresh=Vein();Assert.True(Act(fresh,"Harvest"));One();
  }
  [TestCase("Flesh")][TestCase("unknown")]
  public void UnsupportedSourceDoesNotPretendToBeMineralOrPlant(string material)
  {var source=Vein();source.AddPart(new MaterialPart{MaterialID=material});Assert.True(Act(source,"Harvest"));Assert.IsEmpty(Assigned());Assert.True(Act(Vein(),"Harvest"));One();}
  [TestCase(false)][TestCase(true)]public void ActualPlayerHarvestCanFinishInItsMenu(bool menu)
  {Router.SetWorld(Zone,Actor,!menu);Assert.True(Act(Vein(),"Harvest"));One();}
  [TestCase(false)][TestCase(true)]public void PhysicalEdgeContactMustItselfBeVisible(bool visible)
  {
   var source=Factory.CreateEntity("GlowQuartzVein");source.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0;-2,0"});var h=source.GetPart<HarvestablePart>();h.YieldMin=h.YieldMax=1;h.YieldChance=100;Assert.True(Zone.AddEntity(source,13,10));
   Assert.AreEqual(1,SpatialQuery.Distance(Zone,Actor,source));Assert.True(Zone.GetCell(11,10).Occupants.Contains(source));Zone.GetCell(11,10).IsVisible=visible;
   Assert.True(Act(source,"Harvest"));Assert.AreEqual(1,Count("GlowQuartz"));Assert.Null(Zone.GetEntityCell(source));Assert.AreEqual(visible?1:0,Audio.AcceptedSoundCount);
   if(!visible){Zone.GetCell(11,10).IsVisible=true;Assert.True(Act(Vein(),"Harvest"));One();}else One();
  }
  [Test]public void GenericSemanticCannotInventAMineralHarvest()
  {Assert.True(Enum.TryParse("HarvestMineral",out WorldAudioCue cue));WorldAudioHooks.Emit(cue,Actor,Zone,10,10);Assert.IsEmpty(Assigned());Assert.True(Act(Vein(),"Harvest"));One();}
 }
 public sealed class HarvestAudioAfterPart:Part
 {public Action After;public override bool HandleEvent(GameEvent e){if(e.ID=="AfterInventoryAction")After?.Invoke();return true;}}
}
