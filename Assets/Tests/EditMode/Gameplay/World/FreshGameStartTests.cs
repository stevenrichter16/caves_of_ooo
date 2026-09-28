using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class FreshGameStartTests
 {
  DensityLootTestScope scope;OverworldZoneManager manager;Entity player;
  [SetUp]public void Setup(){scope=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(scope.Factory,64);player=scope.Factory.CreateEntity("Player");}
  [TearDown]public void Teardown(){scope.Dispose();}
  static Type API{get{var t=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.FreshGameStart");Assert.NotNull(t);return t;}}
  string[] Candidates(string configured="")=>(string[])API.GetMethod("CandidateZoneIds").Invoke(null,new object[]{manager,configured});
  bool Place(string configured,out Zone zone){object[] args={manager,player,configured,null};bool result=(bool)API.GetMethod("TryPlace").Invoke(null,args);zone=(Zone)args[3];return result;}
  void MapOnly(params (int x,int y)[] spread){for(int y=0;y<20;y++)for(int x=0;x<20;x++){manager.WorldMap.Tiles[x,y]=BiomeType.Beating;manager.WorldMap.SetPOI(x,y,null);}foreach(var p in spread)manager.WorldMap.Tiles[p.x,p.y]=BiomeType.Spread;}
  [TestCase(1)][TestCase(64)][TestCase(1729)][TestCase(2026)][TestCase(729490642)]public void DefaultGladeIsCurrentActualSpreadWithoutFixedWorldSeed(int seed){manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);var ids=Candidates();Assert.AreEqual(ReferenceGladePlan.ZoneID,ids[0]);Assert.AreEqual(seed,manager.WorldSeed);Assert.That(ids.Distinct().Count(),Is.EqualTo(ids.Length));foreach(var id in ids){var p=WorldMap.FromZoneID(id);Assert.AreEqual(BiomeType.Spread,manager.WorldMap.GetBiome(p.x,p.y));}}
  [Test]public void ForeignBiomeAtGladeAddressFallsBackDeterministicallyToActualSpread(){MapOnly((12,10),(9,10));var a=Candidates();CollectionAssert.AreEqual(a,Candidates());Assert.AreEqual("Overworld.12.10.0",a[0]);Assert.False(a.Contains(ReferenceGladePlan.ZoneID));Assert.AreEqual(0,manager.CachedZones.Count);}
  [Test]public void UnsupportedGladeContentCanUseNormalSpreadWithoutInventingOwners(){scope.Factory.Blueprints.Remove("GlowQuartzVein");MapOnly((11,10),(12,10));Assert.AreEqual("Overworld.12.10.0",Candidates()[0]);var zone=new Zone("Overworld.12.10.0");manager.CachedZones[zone.ZoneID]=zone;Assert.True(Place("",out var placed));Assert.AreSame(zone,placed);Assert.NotNull(zone.GetEntityCell(player));Assert.AreEqual(1,zone.EntityCount);}
  [Test]public void UnsafeFirstZoneIsPreservedWhileSafeSecondSpreadZoneIsChosen(){MapOnly((12,10),(9,10));var blocked=new Zone("Overworld.12.10.0");blocked.ForEachCell((c,x,y)=>{var b=new Entity();b.SetTag("Solid");blocked.AddEntity(b,x,y);});var open=new Zone("Overworld.9.10.0");manager.CachedZones[blocked.ZoneID]=blocked;manager.CachedZones[open.ZoneID]=open;Assert.True(Place("",out var placed));Assert.AreSame(open,placed);Assert.AreEqual(2000,blocked.EntityCount);Assert.IsNull(blocked.GetEntityCell(player));}
  [Test]public void MissingSpreadFailsWithoutGeneratingForeignTerrain(){MapOnly();Assert.IsEmpty(Candidates());Assert.False(Place("",out var placed));Assert.IsNull(placed);Assert.AreEqual(0,manager.CachedZones.Count);}
  [Test]public void ExplicitScenarioAddressKeepsItsBiomeAndDoesNotGainFallback(){MapOnly((12,10));string id="Overworld.1.1.0";CollectionAssert.AreEqual(new[]{id},Candidates(id));var zone=new Zone(id);manager.CachedZones[id]=zone;Assert.True(Place(id,out var placed));Assert.AreSame(zone,placed);Assert.AreEqual(BiomeType.Beating,manager.WorldMap.GetBiome(1,1));}
  [Test]public void UnsafeExplicitScenarioFailsRatherThanRelocatingToDefaultSpread(){MapOnly((12,10));string id="Overworld.1.1.0";var z=new Zone(id);z.ForEachCell((c,x,y)=>{var e=new Entity();e.SetTag("Solid");z.AddEntity(e,x,y);});manager.CachedZones[id]=z;Assert.False(Place(id,out var placed));Assert.IsNull(placed);Assert.AreEqual(1,manager.CachedZones.Count);}
  [Test]public void AlreadyPlacedSavedActorCannotBeRepositionedByDefaultPolicy(){var saved=new Zone("Overworld.19.19.0");saved.AddEntity(player,3,7);manager.CachedZones[saved.ZoneID]=saved;Assert.False(Place("",out var placed));Assert.IsNull(placed);Assert.AreEqual((3,7),saved.GetEntityPosition(player));Assert.AreEqual(1,manager.CachedZones.Count);}
  sealed class FailingFirstManager:OverworldZoneManager
  {
   public readonly List<string> Attempts=new List<string>();public FailingFirstManager(CavesOfOoo.Data.EntityFactory factory):base(factory,64){}
   protected override ZoneGenerationPipeline GetPipelineForZone(string id)
   {
    Attempts.Add(id);var p=new ZoneGenerationPipeline{MaxRetries=1};
    // Retain new-world generation authority while this test controls only the
    // success/failure result; no ordinary gameplay builders run in this fixture.
    foreach(var guard in base.GetPipelineForZone(id).Builders.Where(b=>b.Name=="SpreadExplorationAttempt"))p.AddBuilder(guard);
    p.AddBuilder(new ResultBuilder(id!="Overworld.12.10.0"));return p;
   }
   protected override void OnZoneGenerated(Zone z,string id){}
  }
  sealed class ResultBuilder:IZoneBuilder{readonly bool result;public ResultBuilder(bool value){result=value;}public string Name=>"controlled-generation-result";public int Priority=>0;public bool BuildZone(Zone z,CavesOfOoo.Data.EntityFactory f,Random r)=>result;}
  [Test]public void FailedFirstGenerationIsNeverCachedOrAcceptedAsAnEmptyZone()
  {
   var old=SettlementManager.Current;try{var candidate=new FailingFirstManager(scope.Factory);manager=candidate;MapOnly((12,10),(9,10));Assert.True(Place("",out var chosen));Assert.AreEqual("Overworld.9.10.0",chosen.ZoneID);CollectionAssert.AreEqual(new[]{"Overworld.12.10.0","Overworld.9.10.0"},candidate.Attempts);Assert.False(manager.CachedZones.ContainsKey("Overworld.12.10.0"));Assert.AreSame(chosen,manager.CachedZones[chosen.ZoneID]);}finally{typeof(SettlementManager).GetProperty("Current").SetValue(null,old);}
  }
  [TestCase(1)][TestCase(64)][TestCase(1729)][TestCase(2026)][TestCase(729490642)]public void ActualGeneratedDefaultHasSafeConnectedPlacementWithoutChangingOwnersOrStats(int seed){manager=OverworldZoneManager.CreateDetached(scope.Factory,seed);var z=manager.GetZone(ReferenceGladePlan.ZoneID);Assert.NotNull(z);var owners=z.GetReadOnlyEntities().ToDictionary(e=>e,e=>z.GetEntityPosition(e));int hp=player.GetStatValue("Hitpoints");Assert.True(Place("",out var chosen));Assert.AreSame(z,chosen);Assert.AreEqual(seed,manager.WorldSeed);Assert.AreEqual(hp,player.GetStatValue("Hitpoints"));Assert.False(player.HasPart<BitLockerPart>());Assert.AreEqual(owners.Count+1,z.EntityCount);foreach(var pair in owners)Assert.AreEqual(pair.Value,z.GetEntityPosition(pair.Key));var at=z.GetEntityCell(player);Assert.False(at.BlocksMovement(player));var seen=ConnectivityBuilder.FloodFill(z,at.X,at.Y);Assert.That(Enumerable.Range(0,Zone.Width).Any(x=>seen[x,0]||seen[x,Zone.Height-1])||Enumerable.Range(0,Zone.Height).Any(y=>seen[0,y]||seen[Zone.Width-1,y]));}
  [TestCase(true)][TestCase(false)]public void CachedAliasCannotPlaceDefaultActorInAForeignZone(bool forgedAlias){MapOnly((12,10));string requested="Overworld.12.10.0";var z=new Zone(forgedAlias?"Overworld.19.19.0":requested);manager.CachedZones[requested]=z;Assert.AreEqual(!forgedAlias,Place("",out var selected));if(forgedAlias){Assert.IsNull(selected);Assert.IsNull(z.GetEntityCell(player));}else Assert.AreSame(z,selected);}
  [Test]public void UnsupportedReferenceContentMayStillUseItsOrdinarySpreadFallbackAsLastValidCell(){scope.Factory.Blueprints.Remove("GlowQuartzVein");MapOnly((11,10));CollectionAssert.AreEqual(new[]{ReferenceGladePlan.ZoneID},Candidates());Assert.True(Place("",out var z));Assert.AreEqual(ReferenceGladePlan.ZoneID,z.ZoneID);Assert.False(ReferenceGladePlan.IsActive(z));Assert.NotNull(z.GetEntityCell(player));}
 }
}
