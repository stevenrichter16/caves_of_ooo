using System;

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
