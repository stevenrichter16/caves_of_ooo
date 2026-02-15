namespace CavesOfOoo.Core
{
    public enum BiomeType
    {
        Cave,
        Desert,
        Jungle,
        Ruins
    }

    /// <summary>
    /// 20x20 grid of biome types representing the overworld.
    /// Each cell maps to one zone. Zone IDs use format "Overworld.X.Y.Z"
    /// where Z=0 is the surface and Z>0 are underground levels.
    /// </summary>
    public class WorldMap
    {
        public const int Width = 20;
        public const int Height = 20;

        public BiomeType[,] Tiles;
        public PointOfInterest[,] POIs;
        public int Seed;

        public WorldMap(int seed)
        {
            Seed = seed;
            Tiles = new BiomeType[Width, Height];
            POIs = new PointOfInterest[Width, Height];
        }

        public BiomeType GetBiome(int x, int y)
        {
            if (!InBounds(x, y)) return BiomeType.Cave;
            return Tiles[x, y];
        }

        public PointOfInterest GetPOI(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return POIs[x, y];
        }

        public void SetPOI(int x, int y, PointOfInterest poi)
        {
            if (InBounds(x, y))
                POIs[x, y] = poi;
        }

        public bool HasPOI(int x, int y)
        {
            return InBounds(x, y) && POIs[x, y] != null;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Convert world coordinates to a zone ID string.
        /// Format: "Overworld.X.Y.Z" where Z=0 is the surface.
        /// </summary>
        public static string ToZoneID(int x, int y, int z = 0)
        {
            return $"Overworld.{x}.{y}.{z}";
        }

        /// <summary>
        /// Parse a zone ID back to world coordinates.
        /// Returns (-1, -1, -1) if the ID is not a valid overworld zone.
        /// Handles legacy format "Overworld.X.Y" (assumes z=0).
        /// </summary>
        public static (int x, int y, int z) FromZoneID(string zoneID)
        {
            if (string.IsNullOrEmpty(zoneID) || !zoneID.StartsWith("Overworld."))
                return (-1, -1, -1);

            string[] parts = zoneID.Split('.');

            if (parts.Length == 3)
            {
                // Legacy format: "Overworld.X.Y" → z=0
                if (int.TryParse(parts[1], out int lx) && int.TryParse(parts[2], out int ly))
                    return (lx, ly, 0);
                return (-1, -1, -1);
            }

            if (parts.Length == 4)
            {
                if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y)
                    && int.TryParse(parts[3], out int z))
                    return (x, y, z);
            }

            return (-1, -1, -1);
        }

        /// <summary>
        /// Check if a zone ID belongs to the overworld.
        /// </summary>
        public static bool IsOverworldZoneID(string zoneID)
        {
            if (string.IsNullOrEmpty(zoneID)) return false;
            return zoneID.StartsWith("Overworld.");
        }

        /// <summary>
        /// Get the zone ID adjacent to the given zone in direction (dx, dy).
        /// Returns null if the result would be outside world bounds.
        /// Preserves the Z coordinate.
        /// </summary>
        public static string GetAdjacentZoneID(string zoneID, int dx, int dy)
        {
            var (x, y, z) = FromZoneID(zoneID);
            if (x < 0) return null;

            int nx = x + dx;
            int ny = y + dy;

            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                return null;

            return ToZoneID(nx, ny, z);
        }

        /// <summary>
        /// Get the zone ID one level below (deeper underground).
        /// </summary>
        public static string GetZoneBelow(string zoneID)
        {
            var (x, y, z) = FromZoneID(zoneID);
            if (x < 0) return null;
            return ToZoneID(x, y, z + 1);
        }

        /// <summary>
        /// Get the zone ID one level above.
        /// Returns null if already at the surface (z=0).
        /// </summary>
        public static string GetZoneAbove(string zoneID)
        {
            var (x, y, z) = FromZoneID(zoneID);
            if (x < 0 || z <= 0) return null;
            return ToZoneID(x, y, z - 1);
        }

        /// <summary>
        /// Get the depth (Z coordinate) of a zone. 0 = surface.
        /// </summary>
        public static int GetDepth(string zoneID)
        {
            return FromZoneID(zoneID).z;
        }
    }
}
