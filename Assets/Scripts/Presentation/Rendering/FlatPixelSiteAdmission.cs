using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Ordinary special-site surfaces (2026-10-30): the current cached surface graph of a sinkhole or a
    /// merchant camp in the given biome. Every biome adapter admits only point-of-interest-free wilderness, so these
    /// sites drew almost entirely glyph ground. Authored Places (settlements and Morrowfast), lairs,
    /// unique sites (the Root, the Felling-Site), caves below and saved lair columns are never sites.
    /// Merchant camps were admitted by user decision (2026-10-30); authority pins that used a camp as
    /// their revoking POI now use a lair, which still revokes.
    /// Metadata only: no generation, adoption or state is touched.
    /// </summary>
    public static class FlatPixelSiteAdmission
    {
        // First Cathedral shelf level only; current site authority is never cached.
        internal static bool IsCathedralDescent(Zone zone)
        {
            if (zone?.ZoneID != "Overworld.5.4.1") return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(5, 4);
            return map.GetBiome(5, 4) == BiomeType.Grovelands && AreaCompositionScope.IsCathedralSite(poi)
                && string.IsNullOrEmpty(poi.Profile) && !LairStacks.HasSavedColumn(manager, "Overworld.5.4.0");
        }

        // Exact current Lampwell sandstone foundation, preserving its authored village owners.
        internal static bool IsLampwellStone(Zone zone)
        {
            if (zone?.ZoneID != "Overworld.12.3.1" && zone?.ZoneID != "Overworld.12.3.2") return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(12, 3);
            return map.GetBiome(12, 3) == BiomeType.Spread && poi != null && poi.Type == POIType.Sinkhole
                && poi.Name == "Lampwell" && string.IsNullOrEmpty(poi.Profile)
                && !LairStacks.HasSavedColumn(manager, "Overworld.12.3.0");
        }

        // Exact current Spivenor sandstone foundation, preserving its authored village owners.
        internal static bool IsSpivenorStone(Zone zone)
        {
            if (zone?.ZoneID != "Overworld.16.4.1" && zone?.ZoneID != "Overworld.16.4.2") return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(16, 4);
            return map.GetBiome(16, 4) == BiomeType.Sodden && poi != null && poi.Type == POIType.Sinkhole
                && poi.Name == "Spivenor" && string.IsNullOrEmpty(poi.Profile)
                && !LairStacks.HasSavedColumn(manager, "Overworld.16.4.0");
        }

        // Exact authored first descent; material presentation does not enroll other depths.
        internal static bool IsGinmereDescent(Zone zone)
        {
            if (zone?.ZoneID != "Overworld.2.7.1") return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(2, 7);
            return poi != null && poi.Type == POIType.Sinkhole && poi.Name == "Ginmere"
                && SinkholeArchetypes.ForSite(poi) == SinkholeArchetype.DrownedSima
                && !LairStacks.HasSavedColumn(manager, "Overworld.2.7.0");
        }

        internal static bool IsGroveSinkholeFloor(Zone zone)
        {
            bool ginmere = zone?.ZoneID == "Overworld.2.7.0";
            if (!ginmere && zone?.ZoneID != "Overworld.5.4.0") return false;
            if (!IsOrdinarySite(zone, BiomeType.Grovelands)) return false;
            var poi = WorldLocationContext.For(zone).WorldMap.GetPOI(ginmere ? 2 : 5, ginmere ? 7 : 4);
            return ginmere
                ? poi.Type == POIType.Sinkhole && poi.Name == "Ginmere"
                    && SinkholeArchetypes.ForSite(poi) == SinkholeArchetype.DrownedSima
                : AreaCompositionScope.IsCathedralSite(poi);
        }

        /// <summary>Bounded material pilot for Stillleaf's authored first descent.
        /// Current graph and persisted site identity are rechecked on every use;
        /// no generation, ledger initialization or deeper-floor admission.</summary>
        internal static bool IsStillleafDescent(Zone zone)
        {
            if (zone?.ZoneID != "Overworld.2.4.1") return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            return map?.Tiles != null && map.POIs != null
                && map.Tiles.GetLength(0) == WorldMap.Width && map.Tiles.GetLength(1) == WorldMap.Height
                && map.POIs.GetLength(0) == WorldMap.Width && map.POIs.GetLength(1) == WorldMap.Height
                && AreaCompositionScope.IsStillleafSite(map.GetPOI(2, 4))
                && !LairStacks.HasSavedColumn(manager, "Overworld.2.4.0");
        }

        public static bool IsOrdinarySite(Zone zone, BiomeType biome)
        {
            if (zone?.ZoneID == null) return false;
            var (x, y, z) = WorldMap.FromZoneID(zone.ZoneID);
            if (x < 0 || z != 0 || WorldMapAuthoring.PlaceAt(x, y).HasValue) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            var poi = map.GetPOI(x, y);
            return poi != null && (poi.Type == POIType.Sinkhole || poi.Type == POIType.MerchantCamp)
                && map.GetBiome(x, y) == biome && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }
    }
}
