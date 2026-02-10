using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates a 10x10 world map with biome placement using noise.
    /// Center tile (5,5) is always Cave. All 4 biomes guaranteed present.
    /// </summary>
    public static class WorldGenerator
    {
        public static WorldMap Generate(int seed)
        {
            var map = new WorldMap(seed);
            var rng = new Random(seed);

            // Generate noise field for biome assignment
            var noise = SimpleNoise.GenerateField(WorldMap.Width, WorldMap.Height, rng, octaves: 2);

            // Assign biomes based on noise thresholds
            for (int x = 0; x < WorldMap.Width; x++)
            {
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    float n = noise[x, y];
                    if (n < 0.25f)
                        map.Tiles[x, y] = BiomeType.Cave;
                    else if (n < 0.50f)
                        map.Tiles[x, y] = BiomeType.Desert;
                    else if (n < 0.75f)
                        map.Tiles[x, y] = BiomeType.Jungle;
                    else
                        map.Tiles[x, y] = BiomeType.Ruins;
                }
            }

            // Force center to Cave (player starting zone)
            map.Tiles[5, 5] = BiomeType.Cave;

            // Ensure all 4 biomes are present
            EnsureAllBiomes(map, noise);

            return map;
        }

        /// <summary>
        /// If any biome is missing, find the tile closest to its threshold range
        /// and flip it to that biome.
        /// </summary>
        private static void EnsureAllBiomes(WorldMap map, float[,] noise)
        {
            var biomes = (BiomeType[])Enum.GetValues(typeof(BiomeType));

            foreach (var target in biomes)
            {
                if (HasBiome(map, target)) continue;

                // Find the tile whose noise is closest to the target range center
                float targetCenter = GetBiomeCenter(target);
                float bestDist = float.MaxValue;
                int bestX = 0, bestY = 0;

                for (int x = 0; x < WorldMap.Width; x++)
                {
                    for (int y = 0; y < WorldMap.Height; y++)
                    {
                        // Don't overwrite the center Cave
                        if (x == 5 && y == 5) continue;

                        float dist = Math.Abs(noise[x, y] - targetCenter);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestX = x;
                            bestY = y;
                        }
                    }
                }

                map.Tiles[bestX, bestY] = target;
            }
        }

        private static bool HasBiome(WorldMap map, BiomeType biome)
        {
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    if (map.Tiles[x, y] == biome) return true;
            return false;
        }

        private static float GetBiomeCenter(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave: return 0.125f;
                case BiomeType.Desert: return 0.375f;
                case BiomeType.Jungle: return 0.625f;
                case BiomeType.Ruins: return 0.875f;
                default: return 0.5f;
            }
        }
    }
}
