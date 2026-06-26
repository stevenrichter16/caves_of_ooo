using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The pure heart of emergent alchemy: given a set of reagents (each a
    /// bag of <see cref="BrewPropertyAmount"/>), deterministically resolve
    /// what they brew into. NO world side effects — no inventory, no zone, no
    /// RNG. The M1.2 BrewingService wraps this with validation, consumption,
    /// item creation, and diag emission.
    ///
    /// Resolution algorithm (Docs/CRAFTING-ALCHEMY-SYSTEM.md §2, §6):
    ///   1. Merge reagents into one property→potency profile, combining by
    ///      MAX (never summing) so duplicate/stacked reagents can't out-scale
    ///      a single better reagent (§6.4).
    ///   2. Every rule whose RequireAll is fully present and whose ForbidAny
    ///      is fully absent fires, contributing one effect. Multiple rules can
    ///      fire → emergent multi-effect brews.
    ///   3. No rule fired → Mishap if the mix is volatile (volatility needs a
    ///      partner), else InertSludge. Never a silent fizzle (§6.3).
    ///   4. Form = highest-priority firing rule's form, overridden to
    ///      "Throwable" if the mix is volatile.
    /// </summary>
    public static class BrewResolver
    {
        private const string DefaultForm = "Tonic";
        private static readonly char[] ListSeparators = { ' ', ',', ';', '|' };

        public static BrewResult Resolve(IEnumerable<IReadOnlyList<BrewPropertyAmount>> reagents)
        {
            var result = new BrewResult();

            Dictionary<string, int> profile = MergeProfile(reagents, out int reagentCount);
            if (reagentCount == 0)
            {
                result.Kind = BrewOutcomeKind.Invalid;
                result.Reason = "No reagents supplied.";
                return result;
            }

            if (profile.Count == 0)
            {
                result.Kind = BrewOutcomeKind.InertSludge;
                result.Reason = "These reagents carry no usable properties.";
                return result;
            }

            int bestFormPriority = int.MinValue;
            string form = null;

            IReadOnlyList<BrewRule> rules = BrewRuleRegistry.GetAllRules();
            for (int i = 0; i < rules.Count; i++)
            {
                BrewRule rule = rules[i];
                if (!RuleMatches(rule, profile))
                    continue;

                result.Effects.Add(new BrewEffect
                {
                    Effect = rule.Effect,
                    Potency = ComputeMagnitude(rule, profile)
                });

                if (rule.Priority >= bestFormPriority)
                {
                    bestFormPriority = rule.Priority;
                    form = rule.Form;
                }
            }

            bool volatileMix = profile.ContainsKey(BrewProperties.Volatile);

            if (result.Effects.Count == 0)
            {
                if (volatileMix)
                {
                    result.Kind = BrewOutcomeKind.Mishap;
                    result.Reason = "The volatile mix has nothing to stabilize it — it cracks and spits.";
                }
                else
                {
                    result.Kind = BrewOutcomeKind.InertSludge;
                    result.Reason = "These reagents don't react into anything — inert sludge.";
                }

                return result;
            }

            result.Kind = BrewOutcomeKind.Brew;
            result.Form = volatileMix
                ? "Throwable"
                : (string.IsNullOrWhiteSpace(form) ? DefaultForm : form);
            result.Reason = "The reagents react.";
            return result;
        }

        /// <summary>Convenience overload for callers/tests with concrete arrays.</summary>
        public static BrewResult Resolve(params IReadOnlyList<BrewPropertyAmount>[] reagents)
        {
            return Resolve((IEnumerable<IReadOnlyList<BrewPropertyAmount>>)reagents);
        }

        private static Dictionary<string, int> MergeProfile(
            IEnumerable<IReadOnlyList<BrewPropertyAmount>> reagents,
            out int reagentCount)
        {
            var profile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            reagentCount = 0;

            if (reagents == null)
                return profile;

            foreach (IReadOnlyList<BrewPropertyAmount> reagent in reagents)
            {
                if (reagent == null)
                    continue;

                reagentCount++;

                for (int i = 0; i < reagent.Count; i++)
                {
                    BrewPropertyAmount pa = reagent[i];
                    if (pa == null || string.IsNullOrWhiteSpace(pa.Property) || pa.Potency <= 0)
                        continue;

                    string key = pa.Property.Trim().ToLowerInvariant();

                    // Combine by MAX, never sum — quantity must not scale potency.
                    if (!profile.TryGetValue(key, out int current) || pa.Potency > current)
                        profile[key] = pa.Potency;
                }
            }

            return profile;
        }

        private static bool RuleMatches(BrewRule rule, Dictionary<string, int> profile)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Effect) || string.IsNullOrWhiteSpace(rule.RequireAll))
                return false;

            bool requiredAny = false;
            foreach (string required in SplitProperties(rule.RequireAll))
            {
                requiredAny = true;
                if (!profile.ContainsKey(required))
                    return false;
            }

            // A rule with no parseable required property must not match everything.
            if (!requiredAny)
                return false;

            foreach (string forbidden in SplitProperties(rule.ForbidAny))
            {
                if (profile.ContainsKey(forbidden))
                    return false;
            }

            return true;
        }

        private static int ComputeMagnitude(BrewRule rule, Dictionary<string, int> profile)
        {
            int max = 0;
            foreach (string required in SplitProperties(rule.RequireAll))
            {
                if (profile.TryGetValue(required, out int potency) && potency > max)
                    max = potency;
            }

            float scale = rule.MagnitudeScale <= 0f ? 1f : rule.MagnitudeScale;
            int magnitude = (int)Math.Round(max * scale, MidpointRounding.AwayFromZero);
            return Math.Max(1, magnitude);
        }

        private static IEnumerable<string> SplitProperties(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                yield break;

            string[] parts = raw.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                yield return parts[i].Trim().ToLowerInvariant();
        }
    }
}
