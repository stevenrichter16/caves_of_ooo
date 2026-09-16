using System.Runtime.CompilerServices;

namespace CavesOfOoo.Core
{
    /// <summary>Derived authority for composed wilderness and named sinkhole areas. A weak zone
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
        private static bool IsCandidate(string id)=>OverwritCompositionPlan.IsWildernessZone(id)||GinmereCompositionPlan.IsSupportedZone(id)
            ||CathedralCompositionPlan.IsSupportedZone(id)||StillleafCompositionPlan.IsSupportedZone(id)
            ||OlderdeepCompositionPlan.IsSupportedZone(id)||WellmeetCompositionPlan.IsSupportedZone(id)
            ||CinderholdCompositionPlan.IsSupportedZone(id)||SumpholdCompositionPlan.IsSupportedZone(id)
            ||DrownedLedgerCompositionPlan.IsSupportedZone(id)||MarrowstyeCompositionPlan.IsSupportedZone(id)
            ||FirstTentCompositionPlan.IsSupportedZone(id)||LastCounterCompositionPlan.IsSupportedZone(id)
            ||GantryCompositionPlan.IsSupportedZone(id)||TineCompositionPlan.IsSupportedZone(id)||QuillholdCompositionPlan.IsSupportedZone(id)||TallyCompositionPlan.IsSupportedZone(id);
        internal static bool IsCathedralSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Sinkhole&&poi.Name=="the Deepest Cathedral"
                &&SinkholeArchetypes.ForSite(poi)==SinkholeArchetype.ChoirCathedral;
        internal static bool IsStillleafSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Sinkhole
                &&SinkholeArchetypes.ForSite(poi)==SinkholeArchetype.SealedLibrary;
        internal static bool IsOlderdeepSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Sinkhole&&poi.Profile==SinkholeSites.FoundingVillageProfile
                &&SinkholeArchetypes.ForSite(poi)==SinkholeArchetype.StrandedSettlement;
        internal static bool IsWellmeetSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="TentCamp";
        internal static bool IsCinderholdSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="PruningPost";
        internal static bool IsSumpholdSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="Boatyard";
        internal static bool IsDrownedLedgerSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="ExcavationCamp";
        internal static bool IsMarrowstyeSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="Intake";
        internal static bool IsFirstTentSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="TentCampFirst";
        internal static bool IsLastCounterSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile=="ConcordPost";
        internal static bool IsGantrySite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile==GantryCompositionPlan.ProfileID;
        internal static bool IsTineSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile==TineCompositionPlan.ProfileID;
        internal static bool IsQuillholdSite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile==QuillholdCompositionPlan.ProfileID;
        internal static bool IsTallySite(PointOfInterest poi)
            =>poi!=null&&poi.Type==POIType.Village&&poi.Profile==TallyCompositionPlan.ProfileID;
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
            if(GantryCompositionPlan.IsSupportedZone(zone.ZoneID))return IsGantrySite(poi);
            if(TineCompositionPlan.IsSupportedZone(zone.ZoneID))return IsTineSite(poi);
            if(QuillholdCompositionPlan.IsSupportedZone(zone.ZoneID))return IsQuillholdSite(poi);
            if(TallyCompositionPlan.IsSupportedZone(zone.ZoneID))return IsTallySite(poi);
            if(FirstTentCompositionPlan.IsSupportedZone(zone.ZoneID))return IsFirstTentSite(poi);
            if(LastCounterCompositionPlan.IsSupportedZone(zone.ZoneID))return IsLastCounterSite(poi);
            if(OlderdeepCompositionPlan.IsSupportedZone(zone.ZoneID))return IsOlderdeepSite(poi);
            if(WellmeetCompositionPlan.IsSupportedZone(zone.ZoneID))return IsWellmeetSite(poi);
            if(CinderholdCompositionPlan.IsSupportedZone(zone.ZoneID))return IsCinderholdSite(poi);
            if(SumpholdCompositionPlan.IsSupportedZone(zone.ZoneID))return IsSumpholdSite(poi);
            if(DrownedLedgerCompositionPlan.IsSupportedZone(zone.ZoneID))return IsDrownedLedgerSite(poi);
            if(MarrowstyeCompositionPlan.IsSupportedZone(zone.ZoneID))return IsMarrowstyeSite(poi);
            if(CathedralCompositionPlan.IsSupportedZone(zone.ZoneID))return IsCathedralSite(poi);
            if(StillleafCompositionPlan.IsSupportedZone(zone.ZoneID))return IsStillleafSite(poi);
            return poi!=null&&poi.Type==POIType.Sinkhole&&poi.Name=="Ginmere"
                &&SinkholeArchetypes.ForSite(poi)==SinkholeArchetype.DrownedSima;
        }
    }
}
