namespace CavesOfOoo.Core
{
    /// <summary>
    /// One exact, player-readable line per <see cref="Effect"/> instance,
    /// read from the LIVE fields — so clamps, defaults, and remaining
    /// durations are reported as they truly are. Shared by the tonic
    /// Examine popup (constructed-effect preview), the look-mode FOCUS
    /// panel, and entity Examine (live afflictions). Keep the wording
    /// CP437-safe: plain ASCII only.
    /// </summary>
    public static class EffectDescriber
    {
        public static string Describe(Effect effect)
        {
            switch (effect)
            {
                case PoisonedEffect p:
                    return "Poisoned - " + p.DamageDice + " damage a turn for "
                        + p.Duration + " turns.";
                case BurningEffect b:
                    return "Set ablaze - burning at intensity "
                        + b.Intensity.ToString("0.#") + " until doused.";
                case AcidicEffect a:
                {
                    int perTurn = 1 + (int)System.Math.Floor(a.Corrosion * 4f);
                    return "Corroding acid - " + perTurn
                        + " damage a turn while the coating lasts.";
                }
                case ElectrifiedEffect el:
                    return "Electrified - charge " + el.Charge.ToString("0.#")
                        + " for " + el.Duration + " turns.";
                case WetEffect w:
                    return "Soaked - " + (w.Moisture * 100f).ToString("0")
                        + "% drenched (douses flame, conducts shock).";
                case FrozenEffect fz:
                    return "Frozen over - " + (fz.Cold * 100f).ToString("0")
                        + "% iced until it thaws.";
                case StoneskinEffect s:
                    return "Stoneskin - incoming damage reduced by " + s.Reduction
                        + " for " + s.Duration + " turns.";
                case BleedingEffect bl:
                    return "Bleeding - " + bl.DamageDice
                        + " damage a turn until staunched (save " + bl.SaveTarget + ").";
                case CharredEffect _:
                    return "Charred - scorched, and far less combustible.";
                case null:
                    return "Nothing.";
                default:
                    return CleanTypeName(effect) + " afflicts it.";
            }
        }

        /// <summary>"PoisonedEffect" -> "Poisoned" for unknown effect types.</summary>
        private static string CleanTypeName(Effect effect)
        {
            string name = effect.GetType().Name;
            const string suffix = "Effect";
            if (name.EndsWith(suffix) && name.Length > suffix.Length)
                name = name.Substring(0, name.Length - suffix.Length);
            return name;
        }
    }
}
