namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.1 — the authored sinkhole mouths. Canon reserved these cells
    /// and said so in <see cref="WorldMapAuthoring"/>'s Places
    /// docstring: "Sinkhole mouths … are reserved but NOT placed here —
    /// they need POI types and zone stacks that W5/W6/W7 build. Their
    /// cells are already the right biome." This is that placement.
    ///
    /// <para>One source of truth: WorldGenerator stamps the POIs from
    /// this table, the POI reservation reads it so an opportunistic
    /// lair can never claim a mouth, and the tests pin it.</para>
    /// </summary>
    public static class SinkholeSites
    {
        /// <summary>Name, world X, world Y. Biomes (verified): Olderdeep
        /// and the Deepest Cathedral open in Grovelands, Lampwell in the
        /// Spread, Spivenor in the Sodden.</summary>
        public static readonly (string Name, int X, int Y)[] All =
        {
            ("Olderdeep",             4, 6),
            ("the Deepest Cathedral", 5, 4),
            ("Lampwell",             12, 3),
            ("Spivenor",             16, 4),
        };

        /// <summary>True when this world cell is an authored mouth.</summary>
        public static bool IsMouth(int x, int y)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].X == x && All[i].Y == y) return true;
            return false;
        }
    }
}
