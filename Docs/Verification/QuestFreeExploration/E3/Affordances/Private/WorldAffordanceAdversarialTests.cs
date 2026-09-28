using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAffordanceAdversarialTests
 {
  [TestCase(true)][TestCase(false)]public void ManagedZoneMustBeTheCurrentInstalledGraph(bool active)
  {using(var f=new WorldAffordanceQueryTests.Fixture()){f.Row();var m=OverworldZoneManager.CreateDetached(HarvestablePart.Factory,64);m.SetActiveZone(f.Zone);if(!active)m.SetActiveZone(new Zone("other"));Assert.AreEqual(active,f.Find()!=null);}}
  [Test]public void ReplacedCacheGraphRefusesOldVisibleOwner()
  {using(var f=new WorldAffordanceQueryTests.Fixture()){f.Row();var m=OverworldZoneManager.CreateDetached(HarvestablePart.Factory,64);m.SetActiveZone(f.Zone);m.CachedZones[f.Zone.ZoneID]=new Zone(f.Zone.ZoneID);Assert.IsNull(f.Find());}}
  [TestCase("spent")][TestCase("move")][TestCase("hidden")][TestCase("part-owner")]
  public void RetainedCueMustRevalidateActualSource(string mutation)
  {using(var f=new WorldAffordanceQueryTests.Fixture()){var r=f.Row();var q=f.Find();Assert.NotNull(q);if(mutation=="spent")r.GetPart<FieldHarvestPart>().Harvested=true;if(mutation=="move")Assert.True(f.Zone.MoveEntity(r,12,10));if(mutation=="hidden")r.GetPart<RenderPart>().Visible=false;if(mutation=="part-owner")r.GetPart<FieldHarvestPart>().ParentEntity=f.Player;var t=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.WorldAffordanceQuery");Assert.False((bool)t.GetMethod("Current").Invoke(null,new[]{f.Player,(object)f.Zone,q}));}}
  [TestCase(false)][TestCase(true)]public void FrozenPlayerDoesNotReceiveAnActionOpportunity(bool frozen)
  {using(var f=new WorldAffordanceQueryTests.Fixture()){f.Row();var p=new StatusEffectsPart();f.Player.AddPart(p);p.RestoreEffectsForLoad(new List<Effect>{new FrozenEffect{Cold=frozen?10:0,Owner=f.Player,Duration=10}});Assert.AreEqual(!frozen,f.Find()!=null);}}
  [TestCase(true)][TestCase(false)]public void InconsistentPlayerPhysicsIsNotInputAuthority(bool equipped)
  {using(var f=new WorldAffordanceQueryTests.Fixture()){f.Row();if(equipped)f.Player.GetPart<PhysicsPart>().Equipped=f.Player;else f.Player.GetPart<PhysicsPart>().InInventory=f.Player;Assert.IsNull(f.Find());}}
 }
}
