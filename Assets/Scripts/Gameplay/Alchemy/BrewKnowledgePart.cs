using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The crafter's permanent alchemy discovery log. Keyed by BREW-RULE ID,
    /// not by reagent combination: discovering "heat + combustible = fire"
    /// once covers every reagent pair that satisfies it — knowledge
    /// transfers, which is the RPG-progression heart of the system
    /// (Docs/CRAFTING-ALCHEMY-SYSTEM.md §2.2).
    ///
    /// Storage + restore shim mirror BitLockerPart's known-recipes pattern
    /// so the save layer treats both identically.
    /// </summary>
    public class BrewKnowledgePart : Part
    {
        public override string Name => "BrewKnowledge";

        private readonly HashSet<string> _discoveredRules =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Returns true only when the rule was NEWLY discovered.</summary>
        public bool Discover(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                return false;

            return _discoveredRules.Add(ruleId);
        }

        public bool Knows(string ruleId)
        {
            return !string.IsNullOrWhiteSpace(ruleId) && _discoveredRules.Contains(ruleId);
        }

        public IReadOnlyCollection<string> GetDiscoveredRules()
        {
            return _discoveredRules;
        }

        public void RestoreDiscoveredRules(IEnumerable<string> ruleIds)
        {
            _discoveredRules.Clear();
            if (ruleIds == null)
                return;

            foreach (string ruleId in ruleIds)
            {
                if (!string.IsNullOrWhiteSpace(ruleId))
                    _discoveredRules.Add(ruleId);
            }
        }
    }
}
