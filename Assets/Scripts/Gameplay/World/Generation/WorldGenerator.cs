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
        // [0] is the starting village. Canon names it Sill — a Tier-1
        // river village where the Felling is a children's story
        // (Lore/History/02_Geography.md:57). Felling W0.3.
        private static readonly string[] VillageNames = {
            "Sill", "Ezra", "Brinestone", "Grit Gate",
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

            // The map is AUTHORED (Felling W0.6). It used to be one
            // 2-octave noise field quartiled into four biome names —
            // which is why the lore could only ever be a label on it:
            // nothing in a bucket of one scalar is a consequence of
            // anything, and canon's geography is entirely consequence
            // (a tree was felled; the stump, the flood, the raw sun and
            // the salt all follow). Canon also settled the SHAPE of the
            // answer: a hand-authored network of named places, after it
            // committed a concentric ring world and deliberately dropped
            // it (Lore/History/02_Geography.md §Phase 2 v2).
            //
            // So biome and tier are tables now. The seed still varies
            // what happens INSIDE every chunk, and still rolls lairs and
            // merchant camps below — those are opportunistic, not
            // historical.
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    map.Tiles[x, y] = WorldMapAuthoring.BiomeAt(x, y);

            PlacePOIs(map, rng);

            return map;
        }

        /// <summary>
        /// Places the authored settlements, then rolls the opportunistic
        /// ones. Villages are canon and hand-placed; lairs and merchant
        /// camps are still seed-varied, because a warband den or a
        /// trader's camp is a thing that happens, not a thing the world
        /// is made of.
        /// </summary>
        private static void PlacePOIs(WorldMap map, Random rng)
        {
            var placed = new List<(int x, int y)>();

            // 1. The canon places. Sill is Places[0] and is the start.
            foreach (var place in WorldMapAuthoring.Places)
            {
                map.SetPOI(place.X, place.Y, new PointOfInterest(
                    POIType.Village, place.Name, place.Faction,
                    WorldMapAuthoring.TierAt(place.X, place.Y),
                    profile: place.Profile));
                placed.Add((place.X, place.Y));
            }

            // 1a2. W5.1 — the authored sinkhole mouths. Canon reserved
            // these cells and said they were waiting for "POI types and
            // zone stacks that W5/W6/W7 build"; this is that placement.
            // Stamped BEFORE the opportunistic rolls and added to
            // `placed`, so a lair can never claim a mouth.
            foreach (var site in SinkholeSites.All)
            {
                map.SetPOI(site.X, site.Y, new PointOfInterest(
                    POIType.Sinkhole, site.Name, null,
                    WorldMapAuthoring.TierAt(site.X, site.Y)));
                placed.Add((site.X, site.Y));
            }

            // 1b. Authored wilderness scenes (the tenth fire, the
            // doll): reserved before any opportunistic roll, so a lair
            // or camp can never take the cell and delete the scene from
            // that world. W4.7 close-out.
            foreach (var authored in OverworldZoneManager.AuthoredWildernessZoneIDs)
            {
                var (ax, ay, _) = WorldMap.FromZoneID(authored);
                placed.Add((ax, ay));
            }

            // 2. Lairs. Not in the Overwrit: nobody dens in a scraped
            // region, and "no ruins where ruins should be" is the whole
            // horror of the place.
            int lairCount = rng.Next(3, 6);
            int lairIdx = 0;
            for (int attempt = 0; attempt < 200 && lairIdx < lairCount; attempt++)
            {
                int x = rng.Next(0, WorldMap.Width);
                int y = rng.Next(0, WorldMap.Height);

                if (!IsSpacedFrom(x, y, placed, 3)) continue;
                BiomeType biome = map.GetBiome(x, y);
                if (biome == BiomeType.Overwrit) continue;
                // W6.2a — nor on the Stump: canon says the mountain is
                // "designed sequence, not garrison" (§3.6), and a lair
                // chunk breaks the Grainfield's one-object contract
                // (found empirically: a lair claimed (2,2) at seed 42
                // and the slope generated as a den, not as grain).
                if (biome == BiomeType.Stump) continue;

                map.SetPOI(x, y, new PointOfInterest(
                    POIType.Lair,
                    lairIdx < LairNames.Length ? LairNames[lairIdx] : $"Lair_{lairIdx}",
                    null, WorldMapAuthoring.TierAt(x, y), GetBossForBiome(biome)));
                placed.Add((x, y));
                lairIdx++;
            }

            // 3. Merchant camps. Also not in the Overwrit — the Concord
            // does not guarantee delivery there, which is a joke its own
            // frontier outpost makes.
            int campCount = rng.Next(2, 4);
            int campIdx = 0;
            for (int attempt = 0; attempt < 200 && campIdx < campCount; attempt++)
            {
                int x = rng.Next(1, WorldMap.Width - 1);
                int y = rng.Next(1, WorldMap.Height - 1);

                if (!IsSpacedFrom(x, y, placed, 3)) continue;
                if (map.GetBiome(x, y) == BiomeType.Overwrit) continue;
                // W6.2a — the Concord does not camp on the mountain
                // either; same designed-sequence rule as the lairs.
                if (map.GetBiome(x, y) == BiomeType.Stump) continue;

                map.SetPOI(x, y, new PointOfInterest(
                    POIType.MerchantCamp, "Merchant Camp", "Villagers",
                    WorldMapAuthoring.TierAt(x, y)));
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

    }
}
