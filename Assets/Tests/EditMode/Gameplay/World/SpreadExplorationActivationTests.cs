using System;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationActivationTests
 {
  [TestCase(1)][TestCase(64)][TestCase(1729)]
  public void OrdinaryNewGameCreatesThePlayableManifestWithoutADeveloperOptIn(int seed)
  {
   var previous=SettlementManager.Current;
   try {using(var scope=new DensityLootTestScope()) {
    var manager=new OverworldZoneManager(scope.Factory,seed);
    Assert.True(manager.Exploration.Enabled,"GameBootstrap's normal constructor must enable the delivered exploration tranche.");
    Assert.Greater(manager.Exploration.Entries.Count,0);Assert.AreEqual(0,manager.CachedZoneCount);
    Assert.AreSame(manager.SettlementManager,SettlementManager.Current);
   }} finally {typeof(SettlementManager).GetProperty("Current").GetSetMethod(true).Invoke(null,new object[]{previous});}
  }
  [Test]public void ExplicitLegacyPreviewCannotInheritTheFreshGameDefault()
  {using(var scope=new DensityLootTestScope()){
   var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,false);
   Assert.False(manager.Exploration.Enabled);Assert.IsEmpty(manager.Exploration.Entries);
   var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("legacy-preview","fixture",manager,null,null));
   Assert.False(loaded.ZoneManager.Exploration.Enabled);
  }}
 }
}
