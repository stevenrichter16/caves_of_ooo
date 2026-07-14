using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// FUN-P0 M2.a — the single source of truth for where every grimoire
    /// lives in the world.
    ///
    /// Pre-M2 every village carried a copy of the same chest stuffed with
    /// all 18 grimoires (of which ContainerPart's MaxItems=10 silently
    /// dropped 8) — the game's whole magic-discovery arc was served as a
    /// menu at spawn, and the second village proved the world a photocopy.
    /// Now: the starting chest teaches (damage, utility, rite), scribes
    /// sell by biome, lair boss chests hold the element keyed to their
    /// biome, and two faction ambassadors gift the rest.
    ///
    /// The totality test (GrimoireDistributionTests) pins that every
    /// grimoire blueprint appears in exactly one source — nothing is
    /// unreachable, nothing is double-sourced by accident. If you add a
    /// grimoire, place it here or that test fails on purpose.
    /// </summary>
    public static class GrimoireDistribution
    {
        /// <summary>The teaching trio in the starting village's chest.</summary>
        public static readonly string[] StartingChest =
        {
            "KindleGrimoire",       // damage: light something on fire
            "ConjureWaterGrimoire", // utility: make the counter-element
            "PurifyWaterGrimoire",  // rite: fix the village well
        };

        /// <summary>
        /// Grimoires each village's Scribe carries for sale, keyed by the
        /// village's faction (biome-mapped at worldgen: Desert=Concord,
        /// Jungle=RotChoir, Ruins=Palimpsest, Cave=Villagers).
        /// </summary>
        public static readonly Dictionary<string, string[]> ScribeStockByFaction =
            new Dictionary<string, string[]>
            {
                ["Villagers"] = new[] { "HearthwarmGrimoire", "MendingRiteGrimoire", "KindleFlameGrimoire" },
                ["SaccharineConcord"] = new[] { "EmberVeinGrimoire", "DryingBreezeGrimoire" },
                ["RotChoir"] = new[] { "QuenchGrimoire", "KindleRiteGrimoire" },
                ["Palimpsest"] = new[] { "WardGleamGrimoire", "ChillDraftGrimoire" },
            };

        /// <summary>Element-keyed grimoire in each biome's lair boss chest (M2.b).</summary>
        public static readonly Dictionary<BiomeType, string> LairChestByBiome =
            new Dictionary<BiomeType, string>
            {
                [BiomeType.Cave] = "ThunderclapGrimoire",
                [BiomeType.Desert] = "ConflagrationGrimoire",
                [BiomeType.Jungle] = "AcidSprayGrimoire",
                [BiomeType.Ruins] = "RimeNovaGrimoire",
            };

        /// <summary>One-time ambassador conversation gifts (M2.c).</summary>
        public static readonly Dictionary<string, string> AmbassadorGiftByFaction =
            new Dictionary<string, string>
            {
                ["RotChoir"] = "IceLanceGrimoire",
                ["Palimpsest"] = "ArcBoltGrimoire",
            };

        /// <summary>Scribe stock for a faction, defaulting to the Villagers pool.</summary>
        public static string[] ScribeStockForFaction(string faction)
        {
            if (faction != null && ScribeStockByFaction.TryGetValue(faction, out var pool))
                return pool;
            return ScribeStockByFaction["Villagers"];
        }

        /// <summary>Every grimoire this table places, across all sources.</summary>
        public static IEnumerable<string> AllPlacedGrimoires()
        {
            foreach (var g in StartingChest) yield return g;
            foreach (var pool in ScribeStockByFaction.Values)
                foreach (var g in pool) yield return g;
            foreach (var g in LairChestByBiome.Values) yield return g;
            foreach (var g in AmbassadorGiftByFaction.Values) yield return g;
        }
    }
}
