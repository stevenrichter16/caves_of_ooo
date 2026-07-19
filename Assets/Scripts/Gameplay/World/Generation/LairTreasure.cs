using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// FUN-P0 M2.b — what a lair is worth. Pre-M2 a lair's loot was 1-2
    /// items from the same five-entry pool as village merchant stock,
    /// bosses carried nothing, and the game's 11 unique weapons had no
    /// world spawn path. Now every lair holds a locked boss chest (key on
    /// the boss, delivered by the existing death-drop) stocked from this
    /// table: one unique weapon, one element grimoire keyed to the biome
    /// (GrimoireDistribution), one enhancement mineral.
    ///
    /// LairTreasureTests pins totality: each entry resolves to a real
    /// blueprint, and the placed-uniques list matches this table.
    /// </summary>
    public static class LairTreasure
    {
        /// <summary>Chest contents per lair biome (weapon, mineral; grimoire comes from GrimoireDistribution).</summary>
        public static readonly Dictionary<BiomeType, string[]> ChestLootByBiome =
            new Dictionary<BiomeType, string[]>
            {
                [BiomeType.Cave] = new[] { "ThunderHammer", "GlowQuartz" },
                [BiomeType.Desert] = new[] { "EmberSpear", "PaleSalt" },
                [BiomeType.Jungle] = new[] { "AcidicDagger", "VenomDagger", "ChoirIron" },
                [BiomeType.Ruins] = new[] { "CryoLance", "PaleSalt" },
            };

        /// <summary>
        /// Sidearm carried (and death-dropped) by specific bosses — loot,
        /// not wielded: boss combat strength stays budgeted by its natural
        /// weapon; the blade is the prize.
        /// </summary>
        public static readonly Dictionary<string, string> BossSidearmByBlueprint =
            new Dictionary<string, string>
            {
                ["AncientGuardian"] = "SeveranceEdge",
            };

        /// <summary>Full chest manifest for a biome: weapons + mineral + element grimoire.</summary>
        public static IEnumerable<string> ChestManifest(BiomeType biome)
        {
            if (ChestLootByBiome.TryGetValue(biome, out var loot))
                foreach (var item in loot) yield return item;
            if (GrimoireDistribution.LairChestByBiome.TryGetValue(biome, out var grimoire))
                yield return grimoire;
        }
    }
}
