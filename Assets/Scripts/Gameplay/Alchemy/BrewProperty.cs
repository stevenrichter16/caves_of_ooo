using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// One (property, potency) pair carried by a reagent.
    /// Properties are the atoms of the emergent-alchemy system: a reagent
    /// is just a bag of these, and brews emerge from how the union of
    /// properties across the combined reagents interacts (see
    /// <see cref="BrewResolver"/>).
    ///
    /// Potency is a small integer tier (typically 1-3). Per the M1 design
    /// lockdown (Docs/CRAFTING-ALCHEMY-SYSTEM.md §6.4) potency is combined
    /// by MAX across reagents, never summed — so stacking duplicate
    /// reagents never out-scales finding a better single reagent.
    /// </summary>
    [Serializable]
    public class BrewPropertyAmount
    {
        public string Property;
        public int Potency;

        public BrewPropertyAmount()
        {
        }

        public BrewPropertyAmount(string property, int potency)
        {
            Property = property;
            Potency = potency;
        }

        /// <summary>
        /// Parse a raw blueprint-friendly list like "heat:2, volatile" into
        /// (name, potency) pairs. Shared by <c>ReagentPart.PropertiesRaw</c>
        /// (lowercased names — resolver property atoms) and
        /// <c>BrewItemPart.EffectsRaw</c> (case-preserved names — effect
        /// display/dispatch keys).
        ///
        /// Grammar: entries split on ',' / ';' / '|'; each entry is
        /// "name:potency" or bare "name" (potency defaults to 1). Malformed
        /// potency or potency &lt;= 0 drops the entry — a parse failure must
        /// never masquerade as a valid potency (the int.TryParse-writes-0
        /// pitfall from CLAUDE.md).
        /// </summary>
        public static List<BrewPropertyAmount> ParseList(string raw, bool lowerCaseNames)
        {
            var result = new List<BrewPropertyAmount>();
            if (string.IsNullOrWhiteSpace(raw))
                return result;

            string[] entries = raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i].Trim();
                if (entry.Length == 0)
                    continue;

                string name;
                int potency;

                int colon = entry.IndexOf(':');
                if (colon < 0)
                {
                    name = entry;
                    potency = 1;
                }
                else
                {
                    name = entry.Substring(0, colon).Trim();
                    string potencyRaw = entry.Substring(colon + 1).Trim();
                    if (!int.TryParse(potencyRaw, out potency))
                        continue;
                }

                if (name.Length == 0 || potency <= 0)
                    continue;

                if (lowerCaseNames)
                    name = name.ToLowerInvariant();

                result.Add(new BrewPropertyAmount(name, potency));
            }

            return result;
        }
    }

    /// <summary>
    /// The canonical M1 property vocabulary (Docs/CRAFTING-ALCHEMY-SYSTEM.md
    /// §5). These are not an enum on purpose: content (reagents + rules) can
    /// introduce new property strings together without a code change, and an
    /// unrecognized property simply matches no rule (→ inert sludge), which
    /// is the correct, forward-compatible behavior. The constants exist for
    /// legibility and to avoid typo-drift in shipped rules/tests.
    /// </summary>
    public static class BrewProperties
    {
        public const string Heat = "heat";
        public const string Cold = "cold";
        public const string Combustible = "combustible";
        public const string Conductive = "conductive";
        public const string Corrosive = "corrosive";
        public const string Volatile = "volatile";
        public const string Viscous = "viscous";
        public const string Vital = "vital";
        public const string Toxic = "toxic";
        public const string Bitter = "bitter";
        public const string Sweet = "sweet";
        public const string Luminous = "luminous";
        public const string Numbing = "numbing";
        public const string Binding = "binding";
    }
}
