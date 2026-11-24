using System;

namespace CavesOfOoo.Core
{
    /// <summary>Versioned, finite regional opportunity pattern over the saved
    /// world clock. Does not change wind, gases, heat, exposure or game time.</summary>
    public static class RegionalWeather
    {
        public const int PolicyVersion = 1, WindowTicks = 600, CycleWindows = 64;
        static readonly string[] Pattern = { "R..R.R..R..R..R.", "..R.........R...", ".R...R...R...R..", "..R....R....R..." };

        public readonly struct Observation
        {
            public readonly bool Valid, Raining;
            public readonly int Window, Season;
            public readonly long UntilTick;
            internal Observation(int window, bool raining)
            { Valid = true; Window = window; Season = window % CycleWindows / 16; Raining = raining; UntilTick = ((long)window + 1) * WindowTicks; }
        }
        public static bool Supports(BiomeType biome) => biome == BiomeType.Spread || biome == BiomeType.Sodden
            || biome == BiomeType.Beating || biome == BiomeType.Grovelands;
        public static Observation At(int seed, BiomeType biome, int tick, int policy)
            => tick < 0 || policy != PolicyVersion || !Supports(biome) ? default
                : new Observation(tick / WindowTicks, IsRainWindow(seed, biome, tick / WindowTicks));
        public static Observation Current(Zone zone, int tick, int policy)
        {
            var manager = WorldLocationContext.For(zone);
            if (manager?.WorldMap == null || !WorldMap.IsOverworldZoneID(zone?.ZoneID)) return default;
            var p = WorldMap.FromZoneID(zone.ZoneID);
            return p.z != 0 || !WorldMapAuthoring.InBounds(p.x, p.y) ? default
                : At(manager.WorldSeed, manager.WorldMap.GetBiome(p.x, p.y), tick, policy);
        }
        public static bool IsRainWindow(int seed, BiomeType biome, int window)
        {
            if (window < 0 || !Supports(biome)) return false;
            uint hash; unchecked { hash = ((uint)seed ^ (uint)biome * 16777619u) * 2166136261u; hash ^= hash >> 16; }
            int phase = window % CycleWindows / 16, slot = (window % 16 + (int)(hash % 16)) % 16;
            bool wet = Pattern[phase][slot] == 'R';
            return biome == BiomeType.Sodden ? wet || slot % 4 == 0
                : biome == BiomeType.Beating ? wet && slot % 3 == 0 : wet;
        }
        public static int CountRainWindows(int seed, BiomeType biome, int first, int end)
        {
            if (first < 0 || end < first) throw new ArgumentOutOfRangeException(nameof(first));
            if (!Supports(biome)) return 0;
            int cycle = 0, startPartial = 0, endPartial = 0;
            for (int i = 0; i < CycleWindows; i++)
            {
                if (!IsRainWindow(seed, biome, i)) continue;
                cycle++;
                if (i < first % CycleWindows) startPartial++;
                if (i < end % CycleWindows) endPartial++;
            }
            return checked((int)(((long)end / CycleWindows - first / CycleWindows) * cycle + endPartial - startPartial));
        }
        public static int NextRainWindow(int seed, BiomeType biome, int first)
        {
            if (first < 0 || !Supports(biome)) return -1;
            for (int i = 0; i < CycleWindows && (long)first + i <= int.MaxValue; i++)
                if (IsRainWindow(seed, biome, first + i)) return first + i;
            return -1;
        }
    }
}
