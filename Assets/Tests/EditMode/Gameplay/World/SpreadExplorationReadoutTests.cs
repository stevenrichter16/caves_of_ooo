using System;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class SpreadExplorationReadoutTests
 {
  [TestCase("current")][TestCase("food-removed")][TestCase("food-moved")][TestCase("food-spent")][TestCase("food-carried")][TestCase("food-foreign")]
  [TestCase("reserve-removed")][TestCase("reserve-moved")][TestCase("reserve-spent")][TestCase("reserve-foreign")]
  [TestCase("actor-removed")][TestCase("wrong-zone")]
  public void PendingGrazerDescriptionRequiresBothActualAllocatedRows(string change)
  {
   using(var f=new SpreadExplorationActorTests.Fixture())
   {
    f.Actor.AddPart(new ExaminablePart());f.Grazer();
    if(change=="food-removed")f.Zone.RemoveEntity(f.Food);
    if(change=="food-moved")f.Move(f.Food,30,15);
    if(change=="food-spent")f.Food.GetPart<FieldHarvestPart>().Harvested=true;
    if(change=="food-carried")f.Food.GetPart<PhysicsPart>().InInventory=f.Player;
    if(change=="food-foreign")f.Food.GetPart<FieldHarvestPart>().ParentEntity=f.Player;
    if(change=="reserve-removed")f.Zone.RemoveEntity(f.Reserve);
    if(change=="reserve-moved")f.Move(f.Reserve,30,15);
    if(change=="reserve-spent")f.Reserve.GetPart<FieldHarvestPart>().Harvested=true;
    if(change=="reserve-foreign")f.Reserve.GetPart<PhysicsPart>().ParentEntity=f.Player;
    if(change=="actor-removed")f.Zone.RemoveEntity(f.Actor);
    if(change=="wrong-zone")f.Role.GetType().GetField("ZoneID").SetValue(f.Role,"different-zone");
    int version=f.Zone.EntityVersion;var text=SpreadExplorationReadout.Describe(f.Actor);
    if(change=="current"){StringAssert.Contains("particular ripe patch",text);StringAssert.Contains(text,f.Actor.GetPart<ExaminablePart>().BuildExamineLine());}
    else Assert.IsNull(text,"No pending-meal claim from stale allocated owners.");
    Assert.AreEqual(version,f.Zone.EntityVersion);Assert.False((bool)f.Get("Fed"));
   }
  }
  [TestCase("current")][TestCase("removed")][TestCase("moved")][TestCase("carried")][TestCase("foreign")]
  [TestCase("actor-removed")][TestCase("both-detached")][TestCase("wrong-zone")]
  public void TerritoryDescriptionTracksItsActualPostAnchor(string change)
  {
   using(var f=new SpreadExplorationActorTests.Fixture())
   {
    f.Territory();if(change=="removed"||change=="both-detached")f.Zone.RemoveEntity(f.Post);
    if(change=="moved")f.Move(f.Post,30,15);if(change=="carried")f.Post.GetPart<PhysicsPart>().InInventory=f.Player;
    if(change=="foreign")f.Post.GetPart<PhysicsPart>().ParentEntity=f.Player;
    if(change=="actor-removed"||change=="both-detached")f.Zone.RemoveEntity(f.Actor);
    if(change=="wrong-zone")f.Role.GetType().GetField("ZoneID").SetValue(f.Role,"different-zone");
    int version=f.Zone.EntityVersion;var text=SpreadExplorationReadout.Describe(f.Actor);
    if(change=="current")StringAssert.Contains("work post",text);else Assert.IsNull(text);
    Assert.AreEqual(version,f.Zone.EntityVersion);Assert.IsNull(f.Get("WarningTarget"));
   }
  }
  [Test]public void SpentFeedingAllowanceRemainsHistoricalAfterRowRemovalAndLoad()
  {
   using(var f=new SpreadExplorationActorTests.Fixture())
   {f.Grazer();f.Turn();f.RoundTrip();f.Zone.RemoveEntity(f.Food);StringAssert.Contains("cropped its fill",SpreadExplorationReadout.Describe(f.Actor));Assert.True((bool)f.Get("Fed"));}
  }
  [TestCase("current")][TestCase("detached")][TestCase("carried")]
  public void FiniteDrawReadoutRequiresAnActualGroundSource(string change)
  {
   var z=new Zone("draw-readout");var e=new Entity{ID="draw",BlueprintName="SpreadDrawPoint"};e.AddPart(new PhysicsPart());e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});z.AddEntity(e,5,5);
   if(change=="detached")z.RemoveEntity(e);if(change=="carried")e.GetPart<PhysicsPart>().InInventory=new Entity();
   if(change=="current")StringAssert.Contains("3 drinks",SpreadExplorationReadout.Describe(e));else Assert.IsNull(SpreadExplorationReadout.Describe(e));
  }
 }
}
