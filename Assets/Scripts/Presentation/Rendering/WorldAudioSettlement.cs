using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
    /// <summary>Known authored place identity, not a guess from NPCs or a scan for inhabitants.
    /// A fixed metadata lookup plus the existing installed scene marker cannot discover hidden work.</summary>
    public static class WorldAudioSettlement
    {
        public static bool IsPreparationStop(Zone zone,WorldMap map)
        {
            if(zone?.ZoneID!=MorrowfastSceneRuntime.ZoneID||map?.Tiles==null)return false;
            var place=map.GetPOI(MorrowfastSceneRuntime.WorldX,MorrowfastSceneRuntime.WorldY);
            if(place?.Type!=POIType.Village||place.Profile!="Morrowfast"||!MorrowfastSceneRuntime.IsActive(zone))return false;
            var state=MorrowfastSceneRuntime.GetState(zone);var owner=state?.ParentEntity;if(owner==null)return false;var at=zone.GetEntityCell(owner);
            return owner!=null&&owner.GetPart<MorrowfastSceneStatePart>()==state&&owner.SpatialZone==zone
                &&at==zone.GetCell(0,0)&&at.Objects.Contains(owner);
        }
    }
}
