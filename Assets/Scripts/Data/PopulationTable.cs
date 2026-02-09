using System.Collections.Generic;

namespace CavesOfOoo.Data
{
    public class PopulationEntry
    {
        public string BlueprintName;
        public int Weight = 1;
        public int MinCount = 0;
        public int MaxCount = 1;
    }

    /// <summary>
    /// Data-driven encounter table for zone population.
    /// Mirrors Qud's PopulationManager tables: weighted entries
    /// with count ranges. Roll() produces a list of blueprint names to spawn.
    /// </summary>
    public class PopulationTable
    {
        public string Name;
        public List<PopulationEntry> Entries = new List<PopulationEntry>();

        /// <summary>
        /// Roll all entries: guaranteed MinCount spawns for each entry,
        /// plus weight-based chance for additional spawns up to MaxCount.
        /// </summary>
        public List<string> Roll(System.Random rng)
        {
            var result = new List<string>();
            int totalWeight = 0;
            foreach (var e in Entries) totalWeight += e.Weight;
            if (totalWeight == 0) return result;

            foreach (var entry in Entries)
            {
                // Always spawn MinCount
                int count = entry.MinCount;

                // Roll for additional spawns up to MaxCount
                if (entry.MaxCount > entry.MinCount)
                {
                    float chance = (float)entry.Weight / totalWeight;
                    if (rng.NextDouble() <= chance)
                        count += rng.Next(1, entry.MaxCount - entry.MinCount + 1);
                }

                for (int i = 0; i < count; i++)
                    result.Add(entry.BlueprintName);
            }
            return result;
        }

        /// <summary>
        /// Standard cave population table for tier 1 zones.
        /// </summary>
        public static PopulationTable CaveTier1()
        {
            return new PopulationTable
            {
                Name = "CaveTier1",
                Entries = new List<PopulationEntry>
                {
                    new PopulationEntry { BlueprintName = "Snapjaw", Weight = 5, MinCount = 2, MaxCount = 5 },
                    new PopulationEntry { BlueprintName = "SnapjawScavenger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "SnapjawHunter", Weight = 1, MinCount = 0, MaxCount = 2 },
                    new PopulationEntry { BlueprintName = "Dagger", Weight = 3, MinCount = 1, MaxCount = 3 },
                    new PopulationEntry { BlueprintName = "LongSword", Weight = 1, MinCount = 0, MaxCount = 1 },
                    new PopulationEntry { BlueprintName = "Stalagmite", Weight = 4, MinCount = 3, MaxCount = 8 },
                }
            };
        }
    }
}
