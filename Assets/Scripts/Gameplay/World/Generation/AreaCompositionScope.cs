using System.Runtime.CompilerServices;

namespace CavesOfOoo.Core
{
    /// <summary>Derived authority for the two new composed areas. A weak zone
    /// key ties each managed graph to its own map, including restored graphs;
    /// no global active-world lookup, entity marker or saved field is required.</summary>
    public static class AreaCompositionScope
    {
        private sealed class Source { public WorldMap Map;public string Id;public int X,Y; }
        private static readonly ConditionalWeakTable<Zone,Source> sources=new ConditionalWeakTable<Zone,Source>();
        internal static void Attach(Zone zone,WorldMap map)
        {
            if(zone==null||!IsCandidate(zone.ZoneID))return;
            var source=sources.GetValue(zone,_=>new Source());
            var at=WorldMap.FromZoneID(zone.ZoneID);
            source.Map=map;source.Id=zone.ZoneID;source.X=at.x;source.Y=at.y;
        }
        private static bool IsCandidate(string id)=>OverwritCompositionPlan.IsWildernessZone(id)||GinmereCompositionPlan.IsSupportedZone(id);
        /// <summary>Standalone native graphs use the finite address contract.
        /// Managed graphs additionally honor their own current biome and POI.
        /// A destroyed floor never changes this authority or regenerates content.</summary>
        public static bool Allows(Zone zone)
        {
            if(zone==null)return false;
            if(!IsCandidate(zone.ZoneID)||!sources.TryGetValue(zone,out var source)||source.Id!=zone.ZoneID)return true;
            if(source.Map==null)return false;
            var poi=source.Map.GetPOI(source.X,source.Y);
            if(OverwritCompositionPlan.IsWildernessZone(zone.ZoneID))
                return poi==null&&source.Map.GetBiome(source.X,source.Y)==BiomeType.Overwrit;
            return poi!=null&&poi.Type==POIType.Sinkhole&&poi.Name=="Ginmere"
                &&SinkholeArchetypes.ForSite(poi)==SinkholeArchetype.DrownedSima;
        }
    }
}
