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
    /// 10x10 grid of biome types representing the overworld.
    /// Each cell maps to one zone. Zone IDs use format "Overworld.X.Y".
    /// </summary>
    public class WorldMap
    {
        public const int Width = 10;
        public const int Height = 10;

        public BiomeType[,] Tiles;
        public int Seed;

        public WorldMap(int seed)
        {
            Seed = seed;
            Tiles = new BiomeType[Width, Height];
        }

        public BiomeType GetBiome(int x, int y)
        {
            if (!InBounds(x, y)) return BiomeType.Cave;
            return Tiles[x, y];
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Convert world coordinates to a zone ID string.
        /// Format: "Overworld.X.Y"
        /// </summary>
        public static string ToZoneID(int x, int y)
        {
            return $"Overworld.{x}.{y}";
        }

        /// <summary>
        /// Parse a zone ID back to world coordinates.
        /// Returns (-1, -1) if the ID is not a valid overworld zone.
        /// </summary>
        public static (int x, int y) FromZoneID(string zoneID)
        {
            if (string.IsNullOrEmpty(zoneID) || !zoneID.StartsWith("Overworld."))
                return (-1, -1);

            string[] parts = zoneID.Split('.');
            if (parts.Length != 3) return (-1, -1);

            if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
                return (x, y);

            return (-1, -1);
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
        /// </summary>
        public static string GetAdjacentZoneID(string zoneID, int dx, int dy)
        {
            var (x, y) = FromZoneID(zoneID);
            if (x < 0) return null;

            int nx = x + dx;
            int ny = y + dy;

            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height)
                return null;

            return ToZoneID(nx, ny);
        }
    }
}
