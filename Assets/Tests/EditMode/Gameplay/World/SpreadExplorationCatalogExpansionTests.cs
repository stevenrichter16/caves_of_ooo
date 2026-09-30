using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationCatalogExpansionTests
 {
  [TestCase(1)][TestCase(64)][TestCase(1729)]
  public void FreshCatalogAddsActualForageAndWorkGangAssignments(int seed)
  {using(var s=new DensityLootTestScope()){
   var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);
   Assert.AreEqual(9,m.Exploration.Version,"New distribution has an explicit version; existing v2/v3 worlds must retain theirs.");
   foreach(string family in new[]{"SnakeForage","WorkGang"})Assert.True(m.Exploration.Entries.Any(e=>e.Family.ToString()==family),family);
   foreach(var e in m.Exploration.Entries.Where(e=>e.Family.ToString()=="SnakeForage"))Assert.AreEqual(Formation.FlowerMeadow,FormationSelector.For(BiomeType.Spread,e.ZoneID));
   foreach(var e in m.Exploration.Entries.Where(e=>e.Family.ToString()=="WorkGang"))Assert.AreEqual(Formation.Fallow,FormationSelector.For(BiomeType.Spread,e.ZoneID));
   Assert.AreEqual(0,m.CachedZoneCount);
  }}
  [TestCase(2,"Fallow",2,true)][TestCase(2,"Fallow",6,false)]
  [TestCase(2,"FlowerMeadow",5,false)][TestCase(3,"Fallow",6,true)]
  [TestCase(3,"Fallow",2,false)][TestCase(3,"FlowerMeadow",5,true)]
  [TestCase(3,"FlowerMeadow",6,false)][TestCase(4,"FlowerMeadow",5,true)][TestCase(999,"FlowerMeadow",5,false)]
  [TestCase(1,"Fallow",2,false)][TestCase(3,"FlowerMeadow",7,false)]
  public void SavedCatalogVersionOwnsItsFamilyHabitat(int version,string formation,int family,bool accepted)
  {using(var s=new DensityLootTestScope()){
   var m=OverworldZoneManager.CreateDetached(s.Factory,64,false);
   string id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID).ToString()==formation).ZoneID;
   var world=new Entity{BlueprintName="World"};string wire=version+"|64|1\n"+id+"|3|"+family+"|1|0";world.Properties[SpreadExplorationPlan.PropertyKey]=wire;
   var method=typeof(SpreadExplorationPlan).GetMethod("Restore",BindingFlags.Static|BindingFlags.NonPublic);
   if(!accepted){var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{m,world}));Assert.IsInstanceOf<System.IO.InvalidDataException>(error.InnerException);Assert.False(m.Exploration.Enabled);return;}
   var restored=(SpreadExplorationPlan)method.Invoke(null,new object[]{m,world});typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(m,restored);
   Assert.AreEqual(version,restored.Version);Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,world).Properties[SpreadExplorationPlan.PropertyKey]);Assert.AreEqual(0,m.CachedZoneCount);
  }}
  [Test]public void AVersionTwoFallowTerritoryRemainsVersionTwoWhenSavedAgain()
  {using(var s=new DensityLootTestScope()){
   var m=OverworldZoneManager.CreateDetached(s.Factory,64,false);
   var candidates=SpreadExplorationPlan.Create(m);
   string id=candidates.Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow).ZoneID;
   var world=new Entity{BlueprintName="World"};string wire="2|64|1\n"+id+"|3|2|1|0";world.Properties[SpreadExplorationPlan.PropertyKey]=wire;
   var restore=typeof(SpreadExplorationPlan).GetMethod("Restore",BindingFlags.Static|BindingFlags.NonPublic);
   var restored=(SpreadExplorationPlan)restore.Invoke(null,new object[]{m,world});
   typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(m,restored);
   Assert.AreEqual(2,restored.Version);Assert.AreEqual("OccupiedBank",restored.Entries.Single().Family.ToString());
   Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,world).Properties[SpreadExplorationPlan.PropertyKey]);
   Assert.AreEqual(0,m.CachedZoneCount);
  }}
 }
}
