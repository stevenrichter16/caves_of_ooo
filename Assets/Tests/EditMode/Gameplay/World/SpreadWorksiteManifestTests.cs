using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadWorksiteManifestTests
 {
  const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
  static readonly string[] Nearby={"Overworld.11.9.0","Overworld.12.10.0","Overworld.11.11.0"};
  static readonly string[] Families={"FieldAlembic","TemperingShelter","TrappersStore"};
  [TestCase(64)][TestCase(1729)]
  public void FreshManifestRetainsThreeDifferentNearbyWorksitesAndTheirFamilyIds(int seed)
  {
   using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);Assert.AreEqual(SpreadExplorationPlan.CurrentVersion,m.Exploration.Version);
    for(int i=0;i<Nearby.Length;i++){var e=m.Exploration.Entries.Single(x=>x.ZoneID==Nearby[i]);Assert.True(e.PlacementEligible);Assert.AreEqual(Families[i],e.Family.ToString());Assert.AreEqual(13+i,Convert.ToInt32(e.Family));}
    Assert.Zero(m.CachedZoneCount,"Assignment must not generate graphs.");
   }
  }
  [Test]public void LiteralVersionEightQuietNeighborRoundTripsAndDoesNotAdoptANewSite()
  {
   using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,64,false);const string wire="8|64|1\nOverworld.11.9.0|3|0|1|0";
    var world=new Entity();world.Properties[SpreadExplorationPlan.PropertyKey]=wire;
    var restored=(SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{m,world});typeof(OverworldZoneManager).GetProperty("Exploration",All).SetValue(m,restored);
    Assert.AreEqual(wire,SpreadExplorationPlan.BindForSave(m,null).GetProperty(SpreadExplorationPlan.PropertyKey));Assert.AreEqual(8,m.Exploration.Version);Assert.AreEqual(SpreadExplorationFamily.None,m.Exploration.Entries.Single().Family);
    var zone=m.GetZone(Nearby[0]);Assert.NotNull(zone);Assert.False(zone.GetReadOnlyEntities().Any(e=>e.Properties.ContainsKey("SpreadWorksite.Role")));Assert.AreEqual(1,m.Exploration.DispositionFor(Nearby[0]));
   }
  }
  [Test]public void VersionEightRejectsANewWorksiteFamilyInsteadOfSilentlyUpgrading()
  {
   using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,64,false);var w=new Entity();w.Properties[SpreadExplorationPlan.PropertyKey]="8|64|1\nOverworld.11.9.0|3|13|1|0";
    var error=Assert.Throws<TargetInvocationException>(()=>typeof(SpreadExplorationPlan).GetMethod("Restore",All).Invoke(null,new object[]{m,w}));Assert.IsInstanceOf<InvalidDataException>(error.InnerException);Assert.Zero(m.CachedZoneCount);
   }
  }
  [TestCase(64)][TestCase(1729)]
  public void NewAllocationRetainsProtectedAddressesAndCardinalFamilyDiversity(int seed)
  {
   using(var s=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);var rows=m.Exploration.Entries.ToArray();
    var protectedIds=new[]{ReferenceGladePlan.ZoneID,m.Wayhouse?.ZoneID,m.RareEncounters?.PairZoneID,m.RareEncounters?.ViperZoneID}.Concat(RegionalSituations.Definitions.SelectMany(d=>new[]{d.SourceZoneId,d.RecipientZoneId}));
    foreach(string id in protectedIds.Where(id=>id!=null))Assert.False(rows.Any(e=>e.ZoneID==id&&e.PlacementEligible),id);
    foreach(var a in rows.Where(e=>e.Family!=SpreadExplorationFamily.None))foreach(var b in rows.Where(e=>e.Family==a.Family&&e.ZoneID!=a.ZoneID))
    {var x=WorldMap.FromZoneID(a.ZoneID);var y=WorldMap.FromZoneID(b.ZoneID);Assert.AreNotEqual(1,Math.Abs(x.x-y.x)+Math.Abs(x.y-y.y),a.ZoneID+" "+b.ZoneID+" "+a.Family);}
   }
  }
  [TestCase("MarlbackCindercaller","TemperingShelter")][TestCase("MarlbackSoursprayer","TrappersStore")]
  public void BothMixedRolesActuallyOccurInOrdinaryGeneratedWorlds(string caster,string family)
  {
   using(var s=new HaulingContentScope())foreach(int seed in new[]{64,1729})
   {
    var manager=OverworldZoneManager.CreateDetached(s.Factory,seed,true);
    foreach(var entry in manager.Exploration.Entries.Where(e=>e.Family.ToString()==family))
    {
     s.Seed(unchecked(seed^FormationSelector.StableIndex(entry.ZoneID,int.MaxValue)));var z=manager.GetZone(entry.ZoneID);Assert.NotNull(z);
     var actor=z.GetReadOnlyEntities().FirstOrDefault(e=>e.BlueprintName==caster);if(actor==null)continue;
     Assert.AreEqual(2,manager.Exploration.DispositionFor(entry.ZoneID));
     Assert.True(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="MarlbackScrabbler"&&e.GetProperty("SpreadWorksite.Role")=="melee"));
     TestContext.WriteLine(caster+" actual generated witness: seed="+seed+" zone="+entry.ZoneID+" at="+z.GetEntityPosition(actor));return;
    }
   }
   Assert.Fail("No real mixed encounter for "+caster);
  }
  [TestCase(64,0)][TestCase(64,1)][TestCase(64,2)][TestCase(1729,0)][TestCase(1729,1)][TestCase(1729,2)]
  public void EachNearbyUneditedNativeZoneActuallyCommitsItsWorksite(int seed,int index)
  {
   using(var s=new HaulingContentScope()){s.Seed(unchecked(seed^FormationSelector.StableIndex(Nearby[index],int.MaxValue)));var m=OverworldZoneManager.CreateDetached(s.Factory,seed,true);var z=m.GetZone(Nearby[index]);Assert.NotNull(z);
    Assert.AreEqual(2,m.Exploration.DispositionFor(z.ZoneID),"An assignment alone does not expose usable content: "+seed+" "+Nearby[index]);
    string functional=index==0?"AlchemyStill":index==1?"TinkersForge":"SpikeTrap";Assert.True(z.GetReadOnlyEntities().Any(e=>e.BlueprintName==functional&&e.Properties.ContainsKey("SpreadWorksite.Role")));
    var kept=z.GetReadOnlyEntities().ToArray();Assert.AreSame(z,m.GetZone(z.ZoneID));CollectionAssert.AreEquivalent(kept,z.GetReadOnlyEntities());
   }
  }
 }
}
