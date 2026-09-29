using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationPipelineTests
 {
  static object Property(object o,string n){var p=o.GetType().GetProperty(n);Assert.NotNull(p,n);return p.GetValue(o);}
  static OverworldZoneManager Fresh(EntityFactory factory,int seed)
  {var method=typeof(OverworldZoneManager).GetMethod("CreateDetached",new[]{typeof(EntityFactory),typeof(int),typeof(bool)});Assert.NotNull(method,"New generation must be explicit and must not retrofit old saves.");return (OverworldZoneManager)method.Invoke(null,new object[]{factory,seed,true});}
  [TestCase(1,"RoadSpill")]
  [TestCase(1,"OccupiedBank")]
  [TestCase(1,"LastGleanings")]
  [TestCase(1,"WateringMargin")]
  [TestCase(64,"RoadSpill")]
  [TestCase(64,"OccupiedBank")]
  [TestCase(64,"LastGleanings")]
  [TestCase(64,"WateringMargin")]
  [TestCase(1729,"RoadSpill")]
  [TestCase(1729,"OccupiedBank")]
  [TestCase(1729,"LastGleanings")]
  [TestCase(1729,"WateringMargin")]
  public void ActualColdPipelineCommitsEveryFirstTrancheFamilyWithRealOwners(int seed,string family)
  {
   using(var scope=new DensityLootTestScope())
   {
    var m=Fresh(scope.Factory,seed);var plan=Property(m,"Exploration");var rows=((IEnumerable)Property(plan,"Entries")).Cast<object>().ToArray();
    var mark=plan.GetType().GetMethod("DispositionFor");Assert.NotNull(mark,"A family assignment must be distinguished from actual realization.");
     int committed=0;
     foreach(var row in rows.Where(e=>Property(e,"Family").ToString()==family))
     {
      string id=(string)Property(row,"ZoneID");scope.Seed(unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue)));
      var z=m.GetZone(id);Assert.NotNull(z,id);if((int)mark.Invoke(plan,new object[]{id})!=2)continue;
      committed++;var entities=z.GetReadOnlyEntities();
      if(family=="OccupiedBank")
      {
       var holders=entities.Where(e=>e.HasPart("SpreadTerritory")).ToArray();Assert.AreEqual(1,holders.Length,id);
       Assert.AreEqual("MarlbackScrabbler",holders[0].BlueprintName);Assert.True((bool)holders[0].GetPart("SpreadTerritory").GetType().GetField("Configured").GetValue(holders[0].GetPart("SpreadTerritory")));
      }
      if(family=="LastGleanings")
      {
       var grazers=entities.Where(e=>e.BlueprintName=="ReedbackGrazer").ToArray();Assert.AreEqual(1,grazers.Length,id);
       var role=grazers[0].GetPart("SpreadGrazer");Assert.NotNull(role);Assert.True((bool)role.GetType().GetField("Configured").GetValue(role));
       Assert.GreaterOrEqual(entities.Count(e=>e.BlueprintName=="RipeCropRow"&&!e.GetPart<FieldHarvestPart>().Harvested),2);
      }
      if(family=="WateringMargin")
      {
       var sources=entities.Where(e=>e.BlueprintName=="SpreadDrawPoint").ToArray();Assert.AreEqual(1,sources.Length,id);
       Assert.AreEqual(3,sources[0].GetPart<LiquidPoolPart>().Volume);Assert.AreEqual("water",sources[0].GetPart<LiquidPoolPart>().LiquidId);
       sources[0].GetPart<LiquidPoolPart>().Volume=0;Assert.AreSame(z,m.GetZone(id));Assert.AreEqual(0,sources[0].GetPart<LiquidPoolPart>().Volume);
      }
      Assert.AreSame(z,m.GetZone(id));
     }
     Assert.Greater(committed,0,family+" must be realized by actual ordinary generation, not only declared.");
   }
  }
  [Test]public void ProtectedPlacesKeepTheirAuthoredDrawPointsWithoutNewRoles()
  {
   using(var scope=new DensityLootTestScope())
   {
    foreach(bool enabled in new[]{false,true})
    {
     var m=enabled?Fresh(scope.Factory,64):OverworldZoneManager.CreateDetached(scope.Factory,64);
     foreach(string id in new[]{ReferenceGladePlan.ZoneID,m.Wayhouse.ZoneID,m.RareEncounters.PairZoneID,m.RareEncounters.ViperZoneID}.Where(s=>!string.IsNullOrEmpty(s)).Distinct())
     {
      var z=m.GetZone(id);Assert.NotNull(z);
      Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="ReedbackGrazer"||e.HasPart("SpreadTerritory")),id);
      var basins=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SpreadDrawPoint").ToArray();
      Assert.AreEqual(id==ReferenceGladePlan.ZoneID?1:0,basins.Length,id);
      if(id==ReferenceGladePlan.ZoneID)Assert.AreEqual((36,9),z.GetEntityPosition(basins[0]));
     }
    }
   }
  }
 }
}
