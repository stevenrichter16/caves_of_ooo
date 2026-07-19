namespace CavesOfOoo.Core
{
    /// <summary>
    /// The canonical effect-name → <see cref="Effect"/> mapping for
    /// consumable-applied effects. Extracted verbatim from
    /// StatusTonicPart.CreateEffect (M1.2) so brew items and tonics share ONE
    /// dispatch table — a name that works on a StatusTonic blueprint works in
    /// a BrewRule and vice versa, and the two can never drift apart.
    ///
    /// Contract (unchanged from StatusTonicPart):
    ///   - name matching is trim + case-insensitive, with aliases per effect
    ///   - duration &lt;= 0, empty dice, magnitude &lt;= 0 all fall back to the
    ///     per-effect defaults encoded here
    ///   - unknown/null name returns null (caller skips silently)
    /// </summary>
    public static class TonicEffectFactory
    {
        public static Effect Create(
            string effectName,
            int effectDuration,
            string effectDamageDice,
            float effectMagnitude,
            Entity source)
        {
            if (string.IsNullOrWhiteSpace(effectName))
                return null;

            string effectKey = effectName.Trim().ToLowerInvariant();
            switch (effectKey)
            {
                case "poison":
                case "poisoned":
                case "poisonedeffect":
                    return new PoisonedEffect(
                        duration: effectDuration > 0 ? effectDuration : 5,
                        damageDice: string.IsNullOrWhiteSpace(effectDamageDice) ? "1d3" : effectDamageDice);

                case "fire":
                case "burn":
                case "burning":
                case "burningeffect":
                    return new BurningEffect(
                        intensity: effectMagnitude > 0f ? effectMagnitude : 1.0f,
                        source: source);

                case "wet":
                case "water":
                case "weteffect":
                    return new WetEffect(
                        moisture: effectMagnitude > 0f ? effectMagnitude : 1.0f);

                case "acid":
                case "acidic":
                case "acidiceffect":
                    return new AcidicEffect(
                        corrosion: effectMagnitude > 0f ? effectMagnitude : 1.0f);

                case "shock":
                case "lightning":
                case "electric":
                case "electrified":
                case "electrifiedeffect":
                    return new ElectrifiedEffect(
                        charge: effectMagnitude > 0f ? effectMagnitude : 1.0f);

                case "ice":
                case "frost":
                case "frozen":
                case "frozeneffect":
                    return new FrozenEffect(
                        cold: effectMagnitude > 0f ? effectMagnitude : 1.0f);

                case "stoneskin":
                case "stone":
                case "stoneskineffect":
                    return new StoneskinEffect(
                        reduction: effectMagnitude > 0f ? (int)effectMagnitude : 2,
                        duration: effectDuration > 0 ? effectDuration : 30);

                case "bleed":
                case "bleeding":
                case "bleedingeffect":
                    // BleedingEffect ctor is (saveTarget, damageDice, rng).
                    // The duration int slot maps to saveTarget (the DC for the
                    // per-turn save-vs-bleed roll) — re-using a numeric
                    // content-author field rather than adding a new one.
                    // Default 15 matches the effect's own ctor default.
                    return new BleedingEffect(
                        saveTarget: effectDuration > 0 ? effectDuration : 15,
                        damageDice: string.IsNullOrWhiteSpace(effectDamageDice) ? "1d2" : effectDamageDice);

                case "char":
                case "charred":
                case "charredeffect":
                    // CharredEffect is parameterless — it sets Duration to
                    // DURATION_INDEFINITE and reduces the target's
                    // MaterialPart.Combustibility by 70% on apply (restores
                    // on remove). Magnitude / duration are intentionally
                    // ignored — the Charred state is binary.
                    return new CharredEffect();
            }

            return null;
        }
    }
}
