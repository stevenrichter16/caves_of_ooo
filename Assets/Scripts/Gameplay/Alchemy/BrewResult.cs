using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>How a brew attempt resolved.</summary>
    public enum BrewOutcomeKind
    {
        /// <summary>No usable reagents were supplied.</summary>
        Invalid = 0,

        /// <summary>
        /// The reagents reacted into nothing useful — produces a junk
        /// "inert sludge" item. NEVER a silent fizzle: the player is told
        /// why (Docs/CRAFTING-ALCHEMY-SYSTEM.md §6.3).
        /// </summary>
        InertSludge = 1,

        /// <summary>
        /// A volatile mix with nothing to stabilize it — a small, telegraphed
        /// mishap (capped, non-lethal). Distinct from inert sludge so the
        /// player learns that volatility specifically needs a partner.
        /// </summary>
        Mishap = 2,

        /// <summary>A real brew with one or more effects.</summary>
        Brew = 3
    }

    /// <summary>One effect contributed to a brew by a matched rule.</summary>
    public struct BrewEffect
    {
        public string Effect;
        public int Potency;
    }

    /// <summary>
    /// The pure output of <see cref="BrewResolver"/>: what the reagent mix
    /// resolves into, with no world side effects. The M1.2 BrewingService
    /// turns this into an actual item + applies/consumes inventory; M1.1 is
    /// just the deterministic resolution.
    /// </summary>
    public class BrewResult
    {
        public BrewOutcomeKind Kind = BrewOutcomeKind.Invalid;

        /// <summary>All effects the matched rules contributed (empty unless Kind == Brew).</summary>
        public List<BrewEffect> Effects = new List<BrewEffect>();

        /// <summary>Resolved output form ("Tonic"/"Coating"/"Throwable"/"Food"); null unless Kind == Brew.</summary>
        public string Form;

        /// <summary>Human-readable explanation — always populated, especially for non-Brew outcomes.</summary>
        public string Reason = string.Empty;

        public bool IsBrew => Kind == BrewOutcomeKind.Brew;

        public bool HasEffect(string effect)
        {
            if (string.IsNullOrEmpty(effect))
                return false;

            for (int i = 0; i < Effects.Count; i++)
            {
                if (string.Equals(Effects[i].Effect, effect, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
