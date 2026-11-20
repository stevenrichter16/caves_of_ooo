using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Core
{
 /// <summary>The empty fresh-start setting selects the ordinary Spread policy.
 /// Explicit scenario addresses remain exact. This never selects a new seed,
 /// edits generated contents, or moves a saved/previously placed actor.</summary>
 public static class FreshGameStart
 {
  public static string[] CandidateZoneIds(OverworldZoneManager manager,string configured)
  {
   if(manager==null)return Array.Empty<string>();
   if(!string.IsNullOrWhiteSpace(configured))return new[]{configured};
   var map=manager.WorldMap;if(map?.Tiles==null||map.POIs==null||map.Tiles.GetLength(0)!=WorldMap.Width||map.Tiles.GetLength(1)!=WorldMap.Height||map.POIs.GetLength(0)!=WorldMap.Width||map.POIs.GetLength(1)!=WorldMap.Height)return Array.Empty<string>();
   var choices=new List<(string id,int tier,int distance,int x,int y)>();
   bool supported=ReferenceGladeBuilder.SupportsContent(manager.Factory);
   for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
   {
    if(map.GetBiome(x,y)!=BiomeType.Spread)continue;var poi=map.GetPOI(x,y);
    if(poi!=null&&poi.Type!=POIType.Village&&poi.Type!=POIType.MerchantCamp)continue;
    string id=WorldMap.ToZoneID(x,y,0);
    choices.Add((id,WorldMapAuthoring.TierAt(x,y),Math.Abs(x-11)+Math.Abs(y-10),x,y));
   }
   return choices.OrderBy(c=>c.id!=ReferenceGladePlan.ZoneID?1:supported&&map.GetPOI(c.x,c.y)==null?0:!supported?2:1).ThenBy(c=>c.tier).ThenBy(c=>c.distance).ThenBy(c=>c.y).ThenBy(c=>c.x).Select(c=>c.id).ToArray();
  }
  /// <summary>
  /// The surface zone of <paramref name="biome"/> (the Spread by default) nearest the usual start (11,10) that the held-place rules call wild ground, or null when there is none: where a
  /// hand-test of the camp kits can set things down at once instead of walking away from the opening, or a biome's own rule (the Beating's glare) can be tried where it applies. Asked only by
  /// a scenario that requests a wild start; an ordinary fresh game never calls it. With <paramref name="depth"/> above zero the answer is the ordinary cave <paramref name="depth"/> floors
  /// below that same wild ground, so a tool that works only underground (the sounding rod) can be tried at once.
  /// </summary>
  public static string WildStartZoneId(OverworldZoneManager manager,BiomeType biome=BiomeType.Spread,int depth=0)
  {
   var map=manager?.WorldMap;if(map==null)return null;
   string best=null;int bestDistance=int.MaxValue;
   for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)
   {
    if(map.GetBiome(x,y)!=biome)continue;
    string id=WorldMap.ToZoneID(x,y,0);if(HeldPlaceZones.Classify(manager,id)!=HeldPlaceZoneClass.WildSurface)continue;
    if(depth>0)
    {
     // One floor (or more) down the same column, and only an ordinary cave: not a bespoke cellar, a site or a village's undercroft.
     id=WorldMap.ToZoneID(x,y,depth);if(id==SoundingRodPart.GleanersCellarZone)continue;
     if(!UndergroundLayoutPlan.Select(manager.WorldSeed,id,biome,map.GetPOI(x,y),manager.Factory).IsOrdinaryColumn)continue;
    }
    int distance=Math.Abs(x-11)+Math.Abs(y-10);
    if(distance<bestDistance){best=id;bestDistance=distance;}
   }
   return best;
  }
  /// <summary>The fresh-start destination to use: the scene's own setting if it has one, else wild ground when a pending scenario asked for it, else empty (the ordinary Spread policy).</summary>
  public static string ResolveStartZone(string sceneZoneId,bool scenarioPending,bool wildRequested,OverworldZoneManager manager,BiomeType wildBiome=BiomeType.Spread,int wildDepth=0)
  {
   if(!string.IsNullOrWhiteSpace(sceneZoneId))return sceneZoneId;
   return scenarioPending&&wildRequested?WildStartZoneId(manager,wildBiome,wildDepth)??"":"";
  }
  public static bool TryPlace(OverworldZoneManager manager,Entity actor,string configured,out Zone placedZone)
  {
   placedZone=null;
   if(manager==null||actor==null||actor.SpatialZone!=null||CombatSystem.IsDeathHandled(actor)||actor.GetStat("Hitpoints") is Stat hp&&hp.Value<=0||actor.GetPart<PhysicsPart>()?.InInventory!=null||actor.GetPart<PhysicsPart>()?.Equipped!=null)return Reject(manager,actor,"invalid-fresh-actor");
   bool ordinary=string.IsNullOrWhiteSpace(configured);
   foreach(string id in CandidateZoneIds(manager,configured))
   {
    var zone=manager.GetZone(id);if(zone==null||!string.Equals(zone.ZoneID,id,StringComparison.Ordinal)||!manager.CachedZones.TryGetValue(id,out var current)||!ReferenceEquals(zone,current))continue;
    if(ordinary){var p=WorldMap.FromZoneID(id);if(manager.WorldMap.GetBiome(p.x,p.y)!=BiomeType.Spread)continue;}
    bool morrowfast=MorrowfastSceneRuntime.IsActive(zone);
    if(!FreshGamePlacement.TryPlace(zone,actor,40,morrowfast?23:12))continue;
    placedZone=zone;Diag.Record("worldgen","FreshGameStartSelected",actor,payload:new{zoneId=id,ordinary,seed=manager.WorldSeed});return true;
   }
   return Reject(manager,actor,"no-safe-configured-start");
  }
  private static bool Reject(OverworldZoneManager manager,Entity actor,string reason)
  {Diag.Record("worldgen","FreshGameStartRejected",actor,payload:new{seed=manager?.WorldSeed??0,reason});return false;}
 }
}
