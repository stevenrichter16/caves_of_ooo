using System.Runtime.CompilerServices;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only regional art authority. Only the exact graph owned by
    /// its current world receives new art; old standalone previews remain native.</summary>
    public static class SoddenPresentationScope
    {
        sealed class Address
        {
            internal string Id;
            internal int X, Y;
            internal bool Surface;
        }
        static readonly ConditionalWeakTable<Zone, Address> Addresses = new ConditionalWeakTable<Zone, Address>();

        public static bool IsActive(Zone zone)
        {
            if (zone == null || zone.ZoneID == null) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var address = Addresses.GetValue(zone, _ => new Address());
            if (address.Id != zone.ZoneID)
            {
                address.Id = zone.ZoneID;
                var parsed = WorldMap.FromZoneID(zone.ZoneID);
                address.X = parsed.x; address.Y = parsed.y;
                address.Surface = WorldMapAuthoring.InBounds(parsed.x, parsed.y) && parsed.z == 0
                    && zone.ZoneID == WorldMap.ToZoneID(parsed.x, parsed.y, 0);
            }
            var map = manager.WorldMap;
            if (!address.Surface || map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height
                || !AreaCompositionScope.Allows(zone)) return false;
            var poi = map.GetPOI(address.X, address.Y);
            if (SumpholdCompositionPlan.IsSupportedZone(zone.ZoneID))
                return poi?.Type == POIType.Village && poi.Profile == "Boatyard";
            if (map.GetBiome(address.X, address.Y) != BiomeType.Sodden) return false;
            if (DrownedLedgerCompositionPlan.IsSupportedZone(zone.ZoneID))
                return poi?.Type == POIType.Village && poi.Profile == "ExcavationCamp";
            // Named Sodden surfaces and lair mouths share the region's art.
            // Static authoring indexes describe generation choices, not current
            // world authority. Underground floors remain outside this contract.
            return true;
        }
    }
}
