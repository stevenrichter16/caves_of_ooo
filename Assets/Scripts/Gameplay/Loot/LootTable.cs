using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A named, weighted set of possible drops. <see cref="Roll"/> resolves
    /// to a concrete list of blueprint names — every entry with a positive
    /// weight gets an independent chance to drop between MinCount and
    /// MaxCount copies, so a table can yield zero, one, or several items in
    /// a single roll (unlike PopulationTable's "always spawn MinCount"
    /// guarantee — a corpse or a gather node should be able to come up
    /// empty).
    /// </summary>
    [Serializable]
    public class LootTable
    {
        public string ID;
        public List<LootEntry> Entries = new List<LootEntry>();

        /// <summary>
        /// Roll every entry independently: each entry's Weight (0-100) is
        /// its percent chance to drop at all; if it drops, count is a
        /// uniform pick in [MinCount, MaxCount]. Malformed entries (no
        /// blueprint name, non-positive weight, inverted min/max) are
        /// skipped rather than throwing — content typos must not crash a
        /// harvest/death in the middle of play.
        /// </summary>
        public List<string> Roll(Random rng)
        {
            var result = new List<string>();
            if (rng == null || Entries == null)
                return result;

            for (int i = 0; i < Entries.Count; i++)
            {
                LootEntry entry = Entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.BlueprintName))
                    continue;
                if (entry.Weight <= 0)
                    continue;

                int weight = Math.Min(entry.Weight, 100);
                if (rng.Next(100) >= weight)
                    continue;

                int min = Math.Max(0, entry.MinCount);
                int max = Math.Max(min, entry.MaxCount);
                int count = max > min ? rng.Next(min, max + 1) : min;

                for (int c = 0; c < count; c++)
                    result.Add(entry.BlueprintName);
            }

            return result;
        }
    }
}
