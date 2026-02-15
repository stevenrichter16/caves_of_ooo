using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates a 20x20 world map with biome placement using noise,
    /// then scatters Points of Interest (villages, lairs, merchant camps).
    /// Center tile is always Cave with a starting village. All 4 biomes guaranteed present.
    /// </summary>
    public static class WorldGenerator
    {
        private static readonly string[] VillageNames = {
            "Kyakukya", "Ezra", "Brinestone", "Grit Gate",
            "Shimmerwell", "Dusthaven", "Thornwall", "Roothollow",
            "Ashveil", "Palesanctum"
        };

        private static readonly string[] LairNames = {
            "Snapjaw Lair", "Prowler Den", "Spider Nest", "Ruined Vault",
            "Stalker Cave", "Wurm Burrow", "Golem Crypt", "Bandit Hideout"
        };

        public static WorldMap Generate(int seed)
        {
            var map = new WorldMap(seed);
            var rng = new Random(seed);

            // Generate noise field for biome assignment
            var noise = SimpleNoise.GenerateField(WorldMap.Width, WorldMap.Height, rng, octaves: 2);

            int centerX = WorldMap.Width / 2;
            int centerY = WorldMap.Height / 2;

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
            map.Tiles[centerX, centerY] = BiomeType.Cave;

            // Ensure all 4 biomes are present
            EnsureAllBiomes(map, noise, centerX, centerY);

            // Place points of interest
            PlacePOIs(map, rng, centerX, centerY);

            return map;
        }

        /// <summary>
        /// If any biome is missing, find the tile closest to its threshold range
        /// and flip it to that biome.
        /// </summary>
        private static void EnsureAllBiomes(WorldMap map, float[,] noise, int centerX, int centerY)
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
                        if (x == centerX && y == centerY) continue;

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

        private static void PlacePOIs(WorldMap map, Random rng, int centerX, int centerY)
        {
            var placed = new List<(int x, int y)>();

            // 1. Starting village at center
            map.SetPOI(centerX, centerY, new PointOfInterest(
                POIType.Village, VillageNames[0], "Villagers", 1));
            placed.Add((centerX, centerY));

            // 2. Place 4-6 additional villages spread across the map
            int villageCount = rng.Next(4, 7);
            int nameIdx = 1;
            for (int attempt = 0; attempt < 200 && nameIdx <= villageCount; attempt++)
            {
                int x = rng.Next(1, WorldMap.Width - 1);
                int y = rng.Next(1, WorldMap.Height - 1);

                if (!IsSpacedFrom(x, y, placed, 4)) continue;

                BiomeType biome = map.GetBiome(x, y);
                string faction = GetFactionForBiome(biome);
                int tier = GetTierByDistance(x, y, centerX, centerY);

                map.SetPOI(x, y, new PointOfInterest(
                    POIType.Village,
                    nameIdx < VillageNames.Length ? VillageNames[nameIdx] : $"Village_{nameIdx}",
                    faction, tier));
                placed.Add((x, y));
                nameIdx++;
            }

            // 3. Place 3-5 lairs
            int lairCount = rng.Next(3, 6);
            int lairIdx = 0;
            for (int attempt = 0; attempt < 200 && lairIdx < lairCount; attempt++)
            {
                int x = rng.Next(0, WorldMap.Width);
                int y = rng.Next(0, WorldMap.Height);

                if (!IsSpacedFrom(x, y, placed, 3)) continue;

                BiomeType biome = map.GetBiome(x, y);
                string boss = GetBossForBiome(biome);
                int tier = GetTierByDistance(x, y, centerX, centerY);

                map.SetPOI(x, y, new PointOfInterest(
                    POIType.Lair,
                    lairIdx < LairNames.Length ? LairNames[lairIdx] : $"Lair_{lairIdx}",
                    null, tier, boss));
                placed.Add((x, y));
                lairIdx++;
            }

            // 4. Place 2-3 merchant camps
            int campCount = rng.Next(2, 4);
            int campIdx = 0;
            for (int attempt = 0; attempt < 200 && campIdx < campCount; attempt++)
            {
                int x = rng.Next(1, WorldMap.Width - 1);
                int y = rng.Next(1, WorldMap.Height - 1);

                if (!IsSpacedFrom(x, y, placed, 3)) continue;

                BiomeType biome = map.GetBiome(x, y);
                int tier = GetTierByDistance(x, y, centerX, centerY);

                map.SetPOI(x, y, new PointOfInterest(
                    POIType.MerchantCamp,
                    $"Merchant Camp",
                    "Villagers", tier));
                placed.Add((x, y));
                campIdx++;
            }
        }

        private static bool IsSpacedFrom(int x, int y, List<(int x, int y)> existing, int minDist)
        {
            foreach (var (ex, ey) in existing)
            {
                if (Math.Abs(x - ex) + Math.Abs(y - ey) < minDist)
                    return false;
            }
            return true;
        }

        private static int GetTierByDistance(int x, int y, int centerX, int centerY)
        {
            int dist = Math.Abs(x - centerX) + Math.Abs(y - centerY);
            if (dist <= 4) return 1;
            if (dist <= 8) return 2;
            return 3;
        }

        private static string GetFactionForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return "SaccharineConcord";
                case BiomeType.Jungle: return "RotChoir";
                case BiomeType.Ruins: return "Palimpsest";
                case BiomeType.Cave:
                default: return "Villagers";
            }
        }

        private static string GetBossForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave: return "SnapjawChieftain";
                case BiomeType.Desert: return "DesertProwler";
                case BiomeType.Jungle: return "JungleStalker";
                case BiomeType.Ruins: return "AncientGuardian";
                default: return "SnapjawChieftain";
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
