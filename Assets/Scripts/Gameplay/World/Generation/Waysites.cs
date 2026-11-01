using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// One kind of waysite (Docs/CHUNK-ACTIVITY.md §3): a small authored situation that ties a local
    /// need to an action and a payoff. <see cref="Stamp"/> is placed through
    /// <see cref="LandmarkBuilder"/>'s footprint rules. A legend marker of <c>note</c> is resolved per
    /// placement to one of <see cref="Notes"/> (wayside documents, one blueprint each).
    /// </summary>
    public sealed class WaysiteKind
    {
        public string Id;
        public StructureStamp Stamp;
        public int Weight = 10;
        public int MinTier = 1;
        public string[] Notes = Array.Empty<string>();
        public const string NoteMarker = "note";
    }

    /// <summary>
    /// Which waysites go where (Docs/CHUNK-ACTIVITY.md §3.1). Pure functions of the world seed and
    /// the zone ID: never generates a zone, never reads a neighbour's contents, never consumes a
    /// caller's RNG, so selection is order-independent and stable across save and load.
    /// </summary>
    public static class WaysitePlan
    {
        /// <summary>The opening's approved, pinned compositions keep their own content.</summary>
        static readonly HashSet<string> Opening = new HashSet<string>(StringComparer.Ordinal)
        { "Overworld.11.9.0", "Overworld.11.8.0", "Overworld.12.10.0", "Overworld.12.11.0" };

        /// <summary>A wild surface zone that no curated plan already owns.</summary>
        public static bool IsEligible(OverworldZoneManager manager, string zoneId)
        {
            if (manager == null || string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith("Overworld.", StringComparison.Ordinal)) return false;
            var (x, y, z) = WorldMap.FromZoneID(zoneId);
            if (z != 0 || !WorldMapAuthoring.InBounds(x, y) || manager.WorldMap?.GetPOI(x, y) != null) return false;
            if (Opening.Contains(zoneId) || zoneId == ReferenceGladePlan.ZoneID || zoneId == GleanersCellarBuilder.ZoneID) return false;
            if (zoneId == manager.Wayhouse?.ZoneID || zoneId == manager.RareEncounters?.PairZoneID || zoneId == manager.RareEncounters?.ViperZoneID) return false;
            if (zoneId == OverworldZoneManager.TenthFireZoneID || zoneId == OverworldZoneManager.WovenDollZoneID
                || zoneId == OverworldZoneManager.AbandonedCounterZoneA || zoneId == OverworldZoneManager.AbandonedCounterZoneB) return false;
            foreach (var d in RegionalSituations.Definitions)
                if (d.SourceZoneId == zoneId || d.RecipientZoneId == zoneId) return false;
            return true;
        }

        /// <summary>FNV-1a over the salted key, then an avalanche (as SpreadWildernessSituationPlan
        /// does: two salted low-bit selectors otherwise correlate).</summary>
        public static uint Rank(int worldSeed, string zoneId, string salt)
        {
            uint h;
            unchecked
            {
                h = 2166136261 ^ (uint)worldSeed;
                foreach (char c in "Waysite.v1|" + salt + "|" + zoneId) h = (h ^ c) * 16777619;
                h ^= h >> 16; h *= 0x7feb352d; h ^= h >> 15; h *= 0x846ca68b; h ^= h >> 16;
            }
            return h;
        }

        /// <summary>The kinds this zone tries, best first. The first is the zone's entry in the world's
        /// assignment (<see cref="FirstKinds"/>), which never repeats the west or north zone's; the rest
        /// follow as a weighted draw without repetition.</summary>
        public static List<WaysiteKind> Order(int worldSeed, string zoneId, IReadOnlyList<WaysiteKind> kinds, int tier)
        {
            var result = new List<WaysiteKind>();
            var pool = Eligible(kinds, tier);
            if (pool.Count == 0) return result;
            var (x, y, _) = WorldMap.FromZoneID(zoneId);
            var table = FirstKinds(worldSeed, kinds, tier);
            string first = x >= 0 && y >= 0 && x < table.GetLength(0) && y < table.GetLength(1) ? table[x, y] : null;
            var rng = new Random(unchecked((int)Rank(worldSeed, zoneId, "rest")));
            int at = pool.FindIndex(p => p.kind.Id == first);
            if (at >= 0) { result.Add(pool[at].kind); pool.RemoveAt(at); }
            while (pool.Count > 0) { int i = Draw(pool, rng); result.Add(pool[i].kind); pool.RemoveAt(i); }
            return result;
        }

        static List<(WaysiteKind kind, int weight)> Eligible(IReadOnlyList<WaysiteKind> kinds, int tier)
        {
            var pool = new List<(WaysiteKind kind, int weight)>();
            if (kinds != null) foreach (var k in kinds) if (k != null && tier >= k.MinTier && k.Weight > 0) pool.Add((k, k.Weight));
            return pool;
        }

        static int Draw(List<(WaysiteKind kind, int weight)> pool, Random rng)
        {
            int total = 0; foreach (var p in pool) total += p.weight;
            int roll = rng.Next(total), i = 0;
            while (roll >= pool[i].weight) { roll -= pool[i].weight; i++; }
            return i;
        }

        static readonly Dictionary<(int, IReadOnlyList<WaysiteKind>, int), string[,]> assignments = new Dictionary<(int, IReadOnlyList<WaysiteKind>, int), string[,]>();

        /// <summary>Every world cell's first kind for this palette, chosen in raster order so that
        /// each avoids its west and north cells' actual choices (when another kind exists). A pure
        /// function of the seed and the palette, cached; zone generation order cannot change it.</summary>
        public static string[,] FirstKinds(int worldSeed, IReadOnlyList<WaysiteKind> kinds, int tier)
        {
            var key = (worldSeed, kinds, tier);
            lock (assignments)
            {
                if (assignments.TryGetValue(key, out var cached)) return cached;
                var table = new string[WorldMap.Width, WorldMap.Height];
                var all = Eligible(kinds, tier);
                for (int y = 0; y < WorldMap.Height; y++)
                    for (int x = 0; x < WorldMap.Width; x++)
                    {
                        if (all.Count == 0) continue;
                        string west = x > 0 ? table[x - 1, y] : null, north = y > 0 ? table[x, y - 1] : null;
                        var pool = all.FindAll(p => p.kind.Id != west && p.kind.Id != north);
                        if (pool.Count == 0) pool = all.FindAll(p => p.kind.Id != west);
                        if (pool.Count == 0) pool = all;
                        var rng = new Random(unchecked((int)Rank(worldSeed, WorldMap.ToZoneID(x, y, 0), "first")));
                        table[x, y] = pool[Draw(pool, rng)].kind.Id;
                    }
                assignments[key] = table;
                return table;
            }
        }
    }
}

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The shipped waysite palettes (Docs/CHUNK-ACTIVITY.md §3.2). Every owner is a real blueprint with
    /// its own mechanics; every note is a catalogued document. The Overwrit has none: canon makes its
    /// emptiness the point (Lore/Design/THE-OVERWRIT.md §II, "no ruins where ruins should be"), and what
    /// surfaces there does so only at thresholds, which a static placement would contradict.
    /// </summary>
    public static class WaysiteCatalog
    {
        /// <summary>Draws a waysite cistern holds (its blueprint's finite water pool). It catches what
        /// little rain falls on the Beating; endless water in half its wild chunks would undo the
        /// exposure that is the region's point. The Spread's drovers' trough is spring-fed and endless.</summary>
        public const int CisternDraws = 3;

        /// <summary>Sites per wild chunk for the biome.</summary>
        public static int Budget(BiomeType biome, uint rank)
        {
            switch (biome)
            {
                case BiomeType.Spread: return 2;
                case BiomeType.Sodden: return 2;
                case BiomeType.Beating: return 1;
                case BiomeType.Stump: return 1;
                case BiomeType.Grovelands: return 1;
                default: return 0;
            }
        }

        public static IReadOnlyList<WaysiteKind> For(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Spread: return Spread;
                case BiomeType.Sodden: return Sodden;
                case BiomeType.Beating: return Beating;
                case BiomeType.Stump: return Stump;
                case BiomeType.Grovelands: return Grovelands;
                default: return Array.Empty<WaysiteKind>();
            }
        }

        static WaysiteKind Kind(string id, int weight, string[] rows, Dictionary<char, string> legend, params string[] notes)
            => new WaysiteKind { Id = id, Weight = weight, Notes = notes, Stamp = new StructureStamp { Name = "Waysite:" + id, Rows = rows, Legend = legend, Chance = 100 } };

        // Spread: farm country, the recovered world. Ordinary people stop here for ordinary reasons.
        static readonly WaysiteKind[] Spread =
        {
            // A drover's night under the hedge: rest by the fire, sleep, the pack they left, what they wrote.
            Kind("HedgeCamp", 10, new[] { "sn.", "f.b" }, new Dictionary<char, string>
                { { 's', "stock:Sack:WaysiteCampSack" }, { 'n', WaysiteKind.NoteMarker }, { 'f', "Campfire" }, { 'b', "Bedroll" } },
                "WaysiteNoteDrover1", "WaysiteNoteDrover2", "WaysiteNoteDrover3"),
            // Spring-fed water for herds on the move; fill a skin before the Beating.
            Kind("DroversTrough", 8, new[] { "hT" }, new Dictionary<char, string> { { 'h', "HitchingPost" }, { 'T', "DroversTrough" } }),
            // Someone's hives at the field edge, and the berry canes that grow up around them.
            Kind("BeeOrchard", 8, new[] { ".b.", "bHb" }, new Dictionary<char, string> { { 'b', "BerryBush" }, { 'H', "Beehive" } }),
            // A carter's cart over on its side: the load, the strongbox, the carriage slip.
            Kind("FallenCart", 7, new[] { "sKn", ".x." }, new Dictionary<char, string>
                { { 's', "stock:Sack:WaysiteCartGoods" }, { 'K', "OverturnedCart" }, { 'n', WaysiteKind.NoteMarker }, { 'x', "stock:StrongBox:StrongBoxT1" } },
                "WaysiteNoteCarriage1", "WaysiteNoteCarriage2"),
            // A boundary stone: carved directions, and a dish of river-tokens left for the way.
            Kind("BoundaryStone", 6, new[] { "pd" }, new Dictionary<char, string> { { 'p', "Signpost" }, { 'd', "stock:TokenDish:WaysiteTokenDish" } }),
        };

        // The Sodden: peat-cutters, boats, and dry ground that is worth knowing.
        static readonly WaysiteKind[] Sodden =
        {
            Kind("PeatCuttersBothy", 9, new[] { "Bck", "..n" }, new Dictionary<char, string>
                { { 'B', "Bed" }, { 'c', "Chair" }, { 'k', "stock:WovenBasket:WaysiteBothyBasket" }, { 'n', WaysiteKind.NoteMarker } },
                "WaysiteNotePeatTally1", "WaysiteNotePeatTally2"),
            Kind("DryHummockCamp", 9, new[] { "fbk" }, new Dictionary<char, string>
                { { 'f', "Campfire" }, { 'b', "Bedroll" }, { 'k', "stock:WovenBasket:WaysiteCampSack" } }),
            Kind("DuckboardLanding", 8, new[] { "Pkn" }, new Dictionary<char, string>
                { { 'P', "BoatHookPost" }, { 'k', "stock:WovenBasket:WaysiteFrogOilCache" }, { 'n', WaysiteKind.NoteMarker } },
                "WaysiteNoteFerrySlate1", "WaysiteNoteFerrySlate2"),
        };

        // The Beating: shade and water are the sites worth stopping for; salt is the money.
        static readonly WaysiteKind[] Beating =
        {
            Kind("ShadeCistern", 10, new[] { "#C#", "ini" }, new Dictionary<char, string>
                { { '#', "SandstoneWall" }, { 'C', "Cistern" }, { 'i', "interior" }, { 'n', WaysiteKind.NoteMarker } },
                "WaysiteNoteCistern1", "WaysiteNoteCistern2"),
            Kind("SaltPan", 8, new[] { "v.v", ".v." }, new Dictionary<char, string> { { 'v', "PaleSaltVein" } }),
        };

        // The Stump: a watcher's ledge camp.
        static readonly WaysiteKind[] Stump =
        {
            Kind("WatchersCamp", 10, new[] { "fbn" }, new Dictionary<char, string>
                { { 'f', "Campfire" }, { 'b', "Bedroll" }, { 'n', WaysiteKind.NoteMarker } },
                "WaysiteNoteWatcher1", "WaysiteNoteWatcher2"),
        };

        // The Grovelands: no fire under the Choir's law; a gatherer rests cold and warns the next one.
        static readonly WaysiteKind[] Grovelands =
        {
            Kind("GatherersRest", 10, new[] { "bkn" }, new Dictionary<char, string>
                { { 'b', "Bedroll" }, { 'k', "stock:WovenBasket:BasketT1" }, { 'n', WaysiteKind.NoteMarker } },
                "WaysiteNoteGatherer1", "WaysiteNoteGatherer2"),
        };
    }
}
