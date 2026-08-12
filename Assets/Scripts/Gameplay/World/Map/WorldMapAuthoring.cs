namespace CavesOfOoo.Core
{
    /// <summary>
    /// The world, as authored.
    ///
    /// <para><b>Why the noise had to go.</b> The overworld used to be one
    /// 2-octave noise field quartiled into four biome names — so a desert
    /// could border a jungle could border a cave, because the four names
    /// were four buckets of one scalar. A map made of noise buckets cannot
    /// express a cosmology, because nothing in it is a consequence of
    /// anything. Canon's geography is causal: a god-tree was felled, and
    /// the stump, the flood, the raw sun and the salt all follow from
    /// that one event (<c>Lore/History/01_Spine.md</c> §IV).</para>
    ///
    /// <para>Canon also settled the <i>shape</i> of the answer. Phase 2
    /// committed a concentric ring world and then <b>deliberately dropped
    /// it</b> — a global geometry "optimizes for coherence viewed from
    /// orbit, a problem the player does not have"
    /// (<c>Lore/History/02_Geography.md</c> §Phase 2 v2). What replaced it
    /// is a hand-authored network of named places. So this is a table, not
    /// a generator: the map is decided, and the seed varies only what
    /// happens inside a chunk.</para>
    ///
    /// <para><b>What is still rolled:</b> lairs and merchant camps (they
    /// are opportunistic, not historical), and every zone's interior.
    /// What is authored: biome, tier, roads, rivers, and the canon
    /// places.</para>
    ///
    /// <para>Design source: <c>Docs/FELLING-WORLD-DESIGN.md</c> §2.
    /// Implementation plan: <c>Docs/FELLING-IMPLEMENTATION-PLAN.md</c>
    /// §2 W0.6.</para>
    /// </summary>
    public static class WorldMapAuthoring
    {
        // ════════════════════════════════════════════════════════
        // The biome table
        // ════════════════════════════════════════════════════════
        //
        //   S  the Spread      recovered river country; the Tier-1 world
        //   D  the Sodden      where the flood never fully drained
        //   B  the Beating     raw sun, salt pans, exposed old ruins
        //   G  the Grovelands  Choir country, thickening toward the Stump
        //   O  the Overwrit    the scraped region; a hole in the map
        //   T  the Stump       the petrified tepui and its slopes
        //
        // Read as [y][x]: row 0 is the north edge, column 0 the west.
        // The Stump sits north-west with the Grovelands wrapped around
        // it (roots were densest there); the Sodden fills the north-east
        // where the flood pooled; the Beating burns across the south;
        // the Spread is the habitable band between them, and the
        // Overwrit is the bite taken out of the west.

        public static readonly string[] BiomeRows =
        {
            "GGGGGGGGSSSSSDDDDDDD", // 0
            "GGTTTGGSSSSSSDDDDDDD", // 1   (16,1) the Quiet's Door
            "GTTTTTGSSSSSDDDDDDDD", // 2
            "GTTTTTGGSSSSSDDDDDDD", // 3   (3,3) the Root · (12,3) Lampwell
            "GTTTTGGGSSSSSDDDDDDD", // 4   (5,4) the Deepest Cathedral · (16,4) Spivenor
            "GGTTTTGGSSSSSSDDDDDD", // 5   (3,5) the Felling-Site · (17,5) the Drowned Ledger
            "GGGGGGGSSSSSSSSSDDDD", // 6   (4,6) Olderdeep · (6,6) Cinderhold · (15,6) Sumphold
            "GGGGGGSSSSSSSSSDDDDD", // 7   (13,7) Tine
            "GGGGGSSSSSSSSSSDDDDD", // 8   (7,8) Gantry
            "OOOGSSSSSSSSSSSSDDDD", // 9   (5,9) Posy · (14,9) Quillhold
            "OOOOSSSSSSSSSSSSSDDD", // 10  (10,10) SILL — the start
            "OOOOOSSSSSSSSSSSSSSS", // 11  (2,11) the Unsaying · (16,11) Slip
            "OOOOOSSSSSSSSSSSSSSB", // 12  (12,12) Marrowstye
            "OOOOSSSSSSSSSSSBBBBB", // 13
            "BBOOSSSSSSSSSBBBBBBB", // 14  (10,14) Tally
            "BBBBBSSSSSSSBBBBBBBB", // 15  (15,15) the Salt-Vault
            "BBBBBBBSBBBBBBBBBBBB", // 16  (8,16) Wellmeet
            "BBBBBBBBBBBBBBBBBBBB", // 17  (5,17) the First Tent
            "BBBBBBBBBBBBBBBBBBBB", // 18  (18,18) the Last Counter
            "BBBBBBBBBBBBBBBBBBBB", // 19
        };

        // ════════════════════════════════════════════════════════
        // The tier table — Strangeness, authored
        // ════════════════════════════════════════════════════════
        //
        // Canon's rule, kept exactly: "tier is not tied to travel
        // distance" (02_Geography.md). The player never sees the number;
        // they feel it — stranger NPCs, louder sari, denser substrate.
        // Which is why this is a table and not a radius: Slip is a
        // Tier-3 wound a day's walk from Tier-1 farmland, and that is
        // the point.

        public static readonly string[] TierRows =
        {
            "33333333111112222222", // 0
            "33444331111112222222", // 1
            "34444431111122222222", // 2
            "34454433111112222222", // 3   5 = the Root
            "34444433111112222222", // 4
            "33454433111111222222", // 5   5 = the Felling-Site
            "33334331111111112222", // 6
            "33333311111111122222", // 7
            "33333111111111122222", // 8
            "44431111111111112222", // 9
            "44441111111111111222", // 10
            "44444111111111111111", // 11
            "44444111111111111112", // 12
            "44441111111111122222", // 13
            "22441111111112222222", // 14
            "22222111111122222222", // 15
            "22222221222222222222", // 16
            "33333333333333333333", // 17
            "33333333333333333333", // 18
            "33333333333333333333", // 19
        };

        // ════════════════════════════════════════════════════════
        // Roads and the river
        // ════════════════════════════════════════════════════════
        //
        // Roads radiate from TALLY, not from the Root. That separation is
        // canon's own fix: "the economic hub and the narrative centre are
        // different places" (02_Geography.md §III) — v1's named error was
        // making the cosmological centre also the traversal centre.
        //
        // W0 authors the data and paints it on the world map. Zone
        // pipelines start consuming it in W1 (a road cell's chunk gets a
        // lane formation; a river cell's gets the shipped river builder).

        public static readonly string[] RoadRows =
        {
            "....................", // 0
            "....................", // 1
            "....................", // 2
            "....................", // 3
            "....................", // 4
            "....................", // 5
            "......=.............", // 6  Cinderhold
            "......=.....==......", // 7  → Tine
            ".......=====........", // 8  Gantry
            "......=....==.=.....", // 9  Posy · Quillhold
            "......==============", // 10 the long east-west road through Sill
            "..........=.........", // 11
            "..........===.......", // 12 → Marrowstye
            "..........=.........", // 13
            "........===.........", // 14 Tally — the hub
            "........=...===.....", // 15 → the Salt-Vault
            "........=...........", // 16 Wellmeet
            ".....===............", // 17 the First Tent
            "....................", // 18
            "....................", // 19
        };

        public static readonly string[] RiverRows =
        {
            "....................", // 0
            "....................", // 1
            "....................", // 2
            "....................", // 3
            "....................", // 4
            "....................", // 5
            "....................", // 6
            "....................", // 7
            "....................", // 8
            ".........~..........", // 9
            ".......~~~.~~~......", // 10 the slow river Sill sits on
            "....................", // 11
            "....................", // 12
            "....................", // 13
            "....................", // 14
            "....................", // 15
            "....................", // 16
            "....................", // 17
            "....................", // 18
            "....................", // 19
        };

        // ════════════════════════════════════════════════════════
        // The authored places
        // ════════════════════════════════════════════════════════

        /// <summary>One authored place: where it is, what it is called,
        /// and whose ground it is.</summary>
        public readonly struct Place
        {
            public readonly int X, Y;
            public readonly string Name;
            public readonly string Faction;

            public Place(int x, int y, string name, string faction)
            { X = x; Y = y; Name = name; Faction = faction; }
        }

        /// <summary>
        /// The canon settlements, hand-placed. Sill is first and is the
        /// player's start.
        ///
        /// <para>Sinkhole mouths (Olderdeep 4,6 · the Deepest Cathedral
        /// 5,4 · Lampwell 12,3 · Spivenor 16,4), the Root (3,3), the
        /// Felling-Site (3,5) and the Unsaying (2,11) are reserved but
        /// NOT placed here — they need POI types and zone stacks that
        /// W5/W6/W7 build. Their cells are already the right biome, so
        /// they slot in without moving anything.</para>
        /// </summary>
        public static readonly Place[] Places =
        {
            new Place(10, 10, "Sill",               "Villagers"),
            new Place( 7,  8, "Gantry",             "Villagers"),
            new Place(13,  7, "Tine",               "Villagers"),
            new Place( 6,  6, "Cinderhold",         "SaccharineConcord"),
            new Place( 5,  9, "Posy",               "BowerFolk"),
            new Place(12, 12, "Marrowstye",         "PaleCuration"),
            new Place(14,  9, "Quillhold",          "Palimpsest"),
            new Place(10, 14, "Tally",              "SaccharineConcord"),
            new Place( 8, 16, "Wellmeet",           "TentRight"),
            new Place( 5, 17, "the First Tent",     "TentRight"),
            new Place(15, 15, "the Salt-Vault",     "PaleCuration"),
            new Place(15,  6, "Sumphold",           "Villagers"),
            new Place(17,  5, "the Drowned Ledger", "Palimpsest"),
            new Place(16, 11, "Slip",               "Villagers"),
            new Place(18, 18, "the Last Counter",   "SaccharineConcord"),
            new Place(16,  1, "the Quiet's Door",   "CatacombFolk"),
        };

        // ════════════════════════════════════════════════════════
        // Lookups
        // ════════════════════════════════════════════════════════

        public static BiomeType BiomeAt(int x, int y)
        {
            if (!InBounds(x, y)) return BiomeType.Spread;
            switch (BiomeRows[y][x])
            {
                case 'S': return BiomeType.Spread;
                case 'D': return BiomeType.Sodden;
                case 'B': return BiomeType.Beating;
                case 'G': return BiomeType.Grovelands;
                case 'O': return BiomeType.Overwrit;
                case 'T': return BiomeType.Stump;
                default:  return BiomeType.Spread;
            }
        }

        /// <summary>Authored Strangeness Tier, 1-5. The single authority —
        /// both POI stamping and pipeline routing read this, so the two
        /// can no longer disagree the way the duplicated distance
        /// formulas could.</summary>
        public static int TierAt(int x, int y)
        {
            if (!InBounds(x, y)) return 1;
            char c = TierRows[y][x];
            return c >= '1' && c <= '5' ? c - '0' : 1;
        }

        public static bool IsRoad(int x, int y)
            => InBounds(x, y) && RoadRows[y][x] == '=';

        public static bool IsRiver(int x, int y)
            => InBounds(x, y) && RiverRows[y][x] == '~';

        public static bool InBounds(int x, int y)
            => x >= 0 && y >= 0 && x < WorldMap.Width && y < WorldMap.Height;

        /// <summary>The authored place at a cell, or null.</summary>
        public static Place? PlaceAt(int x, int y)
        {
            for (int i = 0; i < Places.Length; i++)
                if (Places[i].X == x && Places[i].Y == y) return Places[i];
            return null;
        }
    }
}
