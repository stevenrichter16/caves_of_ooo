using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One natural dose per prepared bed during an active regional rain window.</summary>
    internal static class CropRain
    {
        private const int MoistureTopUp = 10;

        internal static void Water(CropPart crop, Zone zone, int tick, int window)
        {
            if (WorldClock.CurrentTick != tick || WorldLocationContext.For(zone)?.ActiveZone != zone
                || crop.GrowthStage >= 2 || crop.MoistureTicks >= MoistureTopUp
                || !CropTime.IsCurrentOwner(crop, zone) || crop.LastGrowthWorldTick != tick) return;
            var owner = crop.ParentEntity;
            var cell = zone.GetEntityCell(owner);
            if (cell.IsInterior) return;
            var bed = CultivatedSoilPart.ExactBed(zone, cell);
            if (bed == null || bed.LastNaturalRainWindow < -1 || bed.LastNaturalRainWindow >= window) return;
            foreach (var other in cell.Objects)
                if (other != owner && other.HasPart<CropPart>()) return;

            bed.LastNaturalRainWindow = window;
            crop.LastNaturalRainWorldTick = tick;
            crop.ApplyWater(MoistureTopUp);
            if (Diag.IsRecordEnabled("crop", "NaturalRainWatered"))
                Diag.Record("crop", "NaturalRainWatered", target: owner,
                    payload: new { bed = bed.ParentEntity.ID, window, tick, moisture = crop.MoistureTicks });
        }

        internal static string Examine(CropPart crop, Zone zone, Cell cell)
        {
            int tick = WorldClock.CurrentTick;
            var weather = RegionalWeather.Current(zone, tick, RegionalWeather.PolicyVersion);
            if (!weather.Valid || CultivatedSoilPart.ExactBed(zone, cell) is not { } bed) return "";
            if (crop.LastNaturalRainWorldTick >= 0 && crop.LastNaturalRainWorldTick <= tick)
                return " Rain reached this crop.";
            if (!weather.Raining) return "";
            if (cell.IsInterior) return " The roof keeps rain off this bed.";
            if (bed.LastNaturalRainWindow == weather.Window) return " Rain has already watered this bed.";
            return " This bed is open to the shower.";
        }

        internal static bool Retain(OverworldZoneManager manager, string zoneId)
        {
            if (!manager.CachedZones.TryGetValue(zoneId, out var zone)) return false;
            foreach (var terrain in zone.GetReadOnlyEntitiesWithPart<CultivatedSoilPart>())
            {
                var bed = terrain.GetPart<CultivatedSoilPart>();
                // Temporary flooding or a barren state must not erase a
                // previous draw by causing this physical bed to regenerate.
                if (bed is { LastNaturalRainWindow: >= 0 } && bed.ParentEntity == terrain
                    && WorldResourceActions.Ground(terrain, zone))
                    return true;
            }
            return false;
        }
    }
}
