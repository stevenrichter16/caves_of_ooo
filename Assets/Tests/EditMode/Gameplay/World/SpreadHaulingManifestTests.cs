using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHaulingManifestTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  [Test]public void NewCatalogAppendsHaulingWithoutRenumberingExistingFamilies()
  {Assert.GreaterOrEqual(SpreadExplorationPlan.CurrentVersion,7);Assert.AreEqual(11,Convert.ToInt32(Enum.Parse(typeof(SpreadExplorationFamily),"HeavySalvage")));Assert.AreEqual(10,(int)SpreadExplorationFamily.CoolingWorkPatch);Assert.AreEqual(9,(int)SpreadExplorationFamily.FieldPassage);}
  [TestCase(2,4)][TestCase(3,4)][TestCase(4,4)][TestCase(5,4)][TestCase(6,4)]
  public void LiteralOldFamilyMappingIsUnchangedAndDoesNotAdoptHauling(int version,int unused)
  {
   var method=typeof(SpreadExplorationPlan).GetMethod("FamilyFor",All);Assert.NotNull(method);
   foreach(int seed in new[]{1,64,1729})for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
   {
    string id=WorldMap.ToZoneID(x,y);var family=(SpreadExplorationFamily)method.Invoke(null,new object[]{Formation.Hedgerow,version,seed,id});
    uint rank=(uint)typeof(SpreadExplorationPlan).GetMethod("Rank",All).Invoke(null,new object[]{seed,id,"family-variant"});
    var expected=version>=5?(rank%3==0?SpreadExplorationFamily.OccupiedBank:rank%3==1?SpreadExplorationFamily.CollectorReturn:SpreadExplorationFamily.FieldPassage):version>=4&&rank%2==0?SpreadExplorationFamily.CollectorReturn:SpreadExplorationFamily.OccupiedBank;
    Assert.AreEqual(expected,family,id+" literal v"+version);
   }
  }
  [TestCase(1)][TestCase(64)][TestCase(1729)]public void FreshHedgerowAssignmentsContainTheFourthDistinctFamily(int seed)
  {using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);Assert.Zero(m.CachedZoneCount);var e=m.Exploration.Entries.Where(x=>x.PlacementEligible&&x.Family.ToString()=="HeavySalvage").ToArray();Assert.IsNotEmpty(e);Assert.True(e.All(x=>FormationSelector.For(BiomeType.Spread,x.ZoneID)==Formation.Hedgerow));}}

  [TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
  public void LiteralOlderWireRoundTripsWithoutAdoptingAnyNewAssignments(int version)
  {using(var scope=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(scope.Factory,64,false);var id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Hedgerow).ZoneID;int family=(int)(SpreadExplorationFamily)typeof(SpreadExplorationPlan).GetMethod("FamilyFor",All).Invoke(null,new object[]{Formation.Hedgerow,version,64,id});string wire=version+"|64|1\n"+id+"|3|"+family+"|1|0";var world=new Entity();world.Properties[SpreadExplorationPlan.PropertyKey]=wire;var restored=(SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{m,world});typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(m,restored);Assert.AreEqual(version,restored.Version);Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.Zero(m.CachedZoneCount);}}
  [TestCase(6,11)][TestCase(7,12)]
  public void ANewFamilyCannotLeakIntoAnOlderWireOrOutsideTheCurrentCatalog(int version,int family)
  {using(var scope=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(scope.Factory,64,false);var id=SpreadExplorationPlan.Create(m).Entries.First(e=>e.PlacementEligible&&FormationSelector.For(BiomeType.Spread,e.ZoneID)==Formation.Hedgerow).ZoneID;var world=new Entity();world.Properties[SpreadExplorationPlan.PropertyKey]=version+"|64|1\n"+id+"|3|"+family+"|1|0";var error=Assert.Throws<TargetInvocationException>(()=>typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{m,world}));Assert.IsInstanceOf<InvalidDataException>(error.InnerException);Assert.Zero(m.CachedZoneCount);}}
 }
}
