using System.Runtime.CompilerServices;

namespace CavesOfOoo.Core
{
    /// <summary>Derived visual eligibility for the current native world graph.
    /// This never generates terrain, repairs owners, creates lair records, or
    /// changes the fixed reference glade's separate layout authority.</summary>
    public static class SpreadPresentationScope
    {
        private sealed class Address
        {
            internal string Id;
            internal int X, Y, Depth;
            internal bool Valid;
        }
        private static readonly ConditionalWeakTable<Zone, Address> Addresses =
            new ConditionalWeakTable<Zone, Address>();

        public static bool IsActive(Zone zone)
        {
            if (zone == null)
                return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || zone.ZoneID == null
                || !manager.CachedZones.TryGetValue(zone.ZoneID, out var cached)
                || !ReferenceEquals(cached, zone))
                return false;
            var address = Addresses.GetValue(zone, _ => new Address());
            if (address.Id != zone.ZoneID)
            {
                address.Id = zone.ZoneID;
                var parsed = WorldMap.FromZoneID(address.Id);
                address.X = parsed.x;
                address.Y = parsed.y;
                address.Depth = parsed.z;
                address.Valid = WorldMapAuthoring.InBounds(parsed.x, parsed.y) && parsed.z >= 0
                    && address.Id == WorldMap.ToZoneID(parsed.x, parsed.y, parsed.z);
            }
            var map = manager.WorldMap;
            if (!address.Valid || map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height
                || map.GetBiome(address.X, address.Y) != BiomeType.Spread)
                return false;
            if (address.Depth == 0)
                return true;
            // The glade's shallow store shares its inhabitants and material art.
            // This is visual scope only: a legacy saved cave keeps its owners,
            // and destroyed stairs never cause the remaining scene to turn 2D.
            if (zone.ZoneID == GleanersCellarBuilder.ZoneID && map.GetPOI(address.X, address.Y) == null)
                return true;
            return map.GetPOI(address.X, address.Y)?.Type == POIType.Lair
                && LairStacks.IsCommittedFloor(manager, zone, BiomeType.Spread);
        }
    }
}
