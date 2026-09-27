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
