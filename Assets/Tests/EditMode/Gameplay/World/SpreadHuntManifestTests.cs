using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHuntManifestTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  [Test]public void FreshCatalogRetainsHuntAtTwelveWithCurrentVersion()
  {Assert.AreEqual(10,SpreadExplorationPlan.CurrentVersion);Assert.AreEqual(12,Convert.ToInt32(Enum.Parse(typeof(SpreadExplorationFamily),"HuntThroughCover")));Assert.AreEqual(11,(int)SpreadExplorationFamily.HeavySalvage);}
  [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
  public void LiteralOlderFallowKeepsItsOrdinaryFamily(int version)
  {var method=typeof(SpreadExplorationPlan).GetMethod("FamilyFor",All);foreach(int seed in new[]{1,64,1729})for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++){var family=(SpreadExplorationFamily)method.Invoke(null,new object[]{Formation.Fallow,version,seed,WorldMap.ToZoneID(x,y)});Assert.AreEqual(version==2?SpreadExplorationFamily.OccupiedBank:SpreadExplorationFamily.WorkGang,family);}}
  [TestCase(1)][TestCase(64)][TestCase(1729)]public void FreshHuntAssignmentsUseOnlyOrdinaryEligibleFallow(int seed)
  {using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);Assert.Zero(m.CachedZoneCount);var rows=m.Exploration.Entries.Where(e=>e.Family.ToString()=="HuntThroughCover").ToArray();Assert.IsNotEmpty(rows);Assert.True(rows.All(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow));Assert.True(m.Exploration.Entries.Any(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow&&e.Family==SpreadExplorationFamily.WorkGang));}}
  [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]public void SavedLiteralFallowAssignmentsRoundTripWithoutAdoptingHunts(int version)
  {using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,64,false);string id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow).ZoneID;int family=version==2?(int)SpreadExplorationFamily.OccupiedBank:(int)SpreadExplorationFamily.WorkGang;string wire=version+"|64|1\n"+id+"|3|"+family+"|1|0";var world=new Entity();world.Properties[SpreadExplorationPlan.PropertyKey]=wire;var plan=(SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{m,world});typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(m,plan);Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.Zero(m.CachedZoneCount);}}
  [Test]public void VersionSevenCannotSilentlyAdoptTheNewFamily()
  {using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,64,false);string id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Fallow).ZoneID;var w=new Entity();w.Properties[SpreadExplorationPlan.PropertyKey]="7|64|1\n"+id+"|3|12|1|0";var error=Assert.Throws<TargetInvocationException>(()=>typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{m,w}));Assert.IsInstanceOf<InvalidDataException>(error.InnerException);}}
 }
}
