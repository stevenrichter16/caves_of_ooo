using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Ordinary special-site surfaces (2026-10-30): the current cached surface graph of a sinkhole in
    /// the given biome. Every biome adapter admits only point-of-interest-free wilderness, so these
    /// sites drew almost entirely glyph ground. Authored Places (settlements and Morrowfast), lairs,
    /// unique sites (the Root, the Felling-Site), caves below and saved lair columns are never sites.
    /// Merchant camps are not sites yet: existing authority pins use a camp POI as the mutation that
    /// revokes wilderness art, so admitting camps is a separate, deliberate decision.
    /// Metadata only: no generation, adoption or state is touched.
    /// </summary>
    public static class FlatPixelSiteAdmission
    {
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
            return poi != null && poi.Type == POIType.Sinkhole
                && map.GetBiome(x, y) == biome && !LairStacks.HasSavedColumn(manager, zone.ZoneID);
        }
    }
}
