using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Registry of available tinkering modifications.
    /// Recipe.Blueprint acts as the lookup key (e.g. "mod_sharp" or "[mod]mod_sharp").
    /// </summary>
    public static class TinkerModificationRegistry
    {
        private static readonly Dictionary<string, Func<ITinkerModification>> Factories =
            new Dictionary<string, Func<ITinkerModification>>(StringComparer.OrdinalIgnoreCase)
            {
                { "mod_sharp", () => new SharpTinkerModification() },
                { "sharp", () => new SharpTinkerModification() },
                { "mod_reinforced_plating", () => new ReinforcedPlatingTinkerModification() },
                { "reinforced_plating", () => new ReinforcedPlatingTinkerModification() },
                { "mod_flexweave", () => new FlexweaveTinkerModification() },
                { "flexweave", () => new FlexweaveTinkerModification() },
                { "mod_hardened_shell", () => new HardenedShellTinkerModification() },
                { "hardened_shell", () => new HardenedShellTinkerModification() },
                { "mod_duelist_cut", () => new DuelistCutTinkerModification() },
                { "duelist_cut", () => new DuelistCutTinkerModification() }
            };

        public static bool TryCreate(string id, out ITinkerModification modification)
        {
            modification = null;
            string key = NormalizeId(id);
            if (string.IsNullOrEmpty(key))
                return false;

            if (!Factories.TryGetValue(key, out Func<ITinkerModification> factory))
                return false;

            modification = factory();
            return modification != null;
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return string.Empty;

            string normalized = id.Trim();
            if (normalized.StartsWith("[mod]", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(5);

            return normalized.Trim().ToLowerInvariant();
        }
    }
}
