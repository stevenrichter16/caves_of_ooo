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
        public const string SealedLibraryProfile = "SealedLibrary";
        public const string FoundingVillageProfile = "FoundingVillage";

        /// <summary>Authored identity, re-derived after loading. Keep the
        /// three-field site table compatible with existing map consumers.</summary>
        public static string ProfileFor(string name)
            => name == "Olderdeep" ? FoundingVillageProfile
                : name == SealedLibraryBuilder.SiteName ? SealedLibraryProfile : null;

        /// <summary>Name, world X, world Y. Biomes (verified): Olderdeep,
        /// the Deepest Cathedral and Ginmere open in Grovelands, Lampwell
        /// in the Spread, Spivenor in the Sodden.
        ///
        /// <para><b>Ginmere (W5.6):</b> the W5.4 canon correction made
        /// all four original mouths villages or the Cathedral, which
        /// orphaned the Drowned Sima — W5.3's whole floor was reachable
        /// only "by the hash for unnamed holes", and the shipped world
        /// has no unnamed holes. Ginmere is its named instance: a
        /// water-filled sima at the tepui's foot (canon sites simas in
        /// tepui country — Lore/History/00_Canon.md:63), two cells west
        /// of Olderdeep on clean ground (no place, road or river; both
        /// pinned). The name is coined, not canon — canon names no
        /// drowned sima — in the register of Lampwell and Wellmeet:
        /// a mere is a standing pool, and the gin frogs live in it.</para></summary>
        public static readonly (string Name, int X, int Y)[] All =
        {
            ("Olderdeep",             4, 6),
            ("the Deepest Cathedral", 5, 4),
            ("Lampwell",             12, 3),
            ("Spivenor",             16, 4),
            ("Ginmere",               2, 7),
            // W6.6 coined archive mouth, on an unclaimed western slope.
            (SealedLibraryBuilder.SiteName, 2, 4),
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
