using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// One weighted entry in a LootTable: "this many of this blueprint,
    /// with this likelihood." Mirrors the shape of
    /// <see cref="CavesOfOoo.Data.PopulationEntry"/> (weight + min/max count)
    /// but lives independently — LootTable is an item-drop tool (creature
    /// corpses, gather nodes), PopulationTable is a zone-population tool
    /// (free-standing entities at zone-gen). Unifying them is a future
    /// cleanup, not done here (see Docs/GATHER-LOOT-SYSTEM.md scope-prune).
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        public string BlueprintName;

        /// <summary>
        /// Independent percent chance (0-100) that THIS entry drops at all.
        /// Unlike PopulationEntry's Weight (relative share among competing
        /// entries), every LootEntry rolls on its own — a table can yield
        /// several entries at once, or none. Values above 100 clamp to 100.
        /// </summary>
        public int Weight = 100;
        public int MinCount = 1;
        public int MaxCount = 1;
    }
}
