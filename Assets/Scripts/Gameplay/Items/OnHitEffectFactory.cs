using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Map an <see cref="OnHitEffectSpec"/> to a concrete <see cref="Effect"/>
    /// instance. Mirrors the case-aliased switch pattern from
    /// <c>StatusTonicPart.CreateEffect()</c> (StatusTonicPart.cs:35-84).
    ///
    /// Why a separate switch instead of sharing with StatusTonicPart? The
    /// two contracts differ enough that deduplication isn't trivially clean:
    ///   - StatusTonicPart's effects all derive from a single tonic's static
    ///     fields (EffectName/EffectDuration/EffectDamageDice/EffectMagnitude).
    ///   - OnHit specs add per-spec ChancePercent and per-spec DurationTurns/
    ///     Magnitude; the factory ignores ChancePercent (gating happens in
    ///     OnHitWeaponEffects) and uses the spec's other fields verbatim.
    /// Tracked as 🟡 in the plan doc as a future deduplication opportunity.
    ///
    /// Returns null for unknown EffectName values — caller skips silently.
    /// </summary>
    public static class OnHitEffectFactory
    {
        public static Effect Create(OnHitEffectSpec spec, Entity source, System.Random rng)
        {
            if (spec == null || string.IsNullOrWhiteSpace(spec.EffectName))
                return null;

            string key = spec.EffectName.Trim().ToLowerInvariant();
            switch (key)
            {
                case "burning":
                case "burn":
                case "fire":
                    return new BurningEffect(
                        intensity: spec.Magnitude > 0f ? spec.Magnitude : 1.0f,
                        source: source);

                case "frozen":
                case "freeze":
                case "ice":
                case "frost":
                    return new FrozenEffect(
                        cold: spec.Magnitude > 0f ? spec.Magnitude : 1.0f);

                case "electrified":
                case "electric":
                case "shock":
                case "lightning":
                    return new ElectrifiedEffect(
                        charge: spec.Magnitude > 0f ? spec.Magnitude : 1.0f);

                case "acidic":
                case "acid":
                    return new AcidicEffect(
                        corrosion: spec.Magnitude > 0f ? spec.Magnitude : 1.0f);

                case "wet":
                case "water":
                    return new WetEffect(
                        moisture: spec.Magnitude > 0f ? spec.Magnitude : 1.0f);

                case "poisoned":
                case "poison":
                    return new PoisonedEffect(
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 5,
                        damageDice: string.IsNullOrWhiteSpace(spec.DamageDice) ? "1d3" : spec.DamageDice);

                case "paralyzed":
                case "paralyze":
                    // Round 6 — audit-surfaced: ParalyzedEffect was fully
                    // implemented (action-block + DV penalty + describer
                    // entry) but NOTHING could inflict it. First
                    // inflictor: the giant spider's bite
                    // ("Paralyzed,20,0,2,0" in NaturalWeaponFactory).
                    return new ParalyzedEffect(
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 2);

                case "stunned":
                case "stun":
                    // Use Magnitude as the save-target DC, mirroring the
                    // Bleeding case immediately below (same bug class, same
                    // fix): a future "Stunned,20,,2,16" spec expects 16 to
                    // be the save DC, matching the spec format's own
                    // canonical docstring example
                    // ("Burning,30,,5,1.0;Stunned,5,,1,0"). Default 0 keeps
                    // every existing blueprint's deterministic/unsaveable
                    // stun unchanged.
                    return new StunnedEffect(
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 1,
                        saveTarget: spec.Magnitude > 0f ? (int)spec.Magnitude : 0,
                        rng: rng);

                case "bleeding":
                case "bleed":
                    // Use Magnitude (cast to int) as the save-target DC.
                    // Earlier this used DurationTurns, which has different
                    // semantics (Bleeding has no fixed Duration — it's
                    // indefinite, save-curable). Future content authoring
                    // a "Bleeding,30,1d3,0,18" spec expects 18 to be the
                    // save-target DC, not "duration". Latent bug surfaced
                    // by adversarial tests; no in-game blueprint hits this
                    // path today (class hooks construct BleedingEffect
                    // directly with the proper saveTarget=15).
                    return new BleedingEffect(
                        saveTarget: spec.Magnitude > 0f ? (int)spec.Magnitude : 15,
                        damageDice: string.IsNullOrWhiteSpace(spec.DamageDice) ? "1d2" : spec.DamageDice,
                        rng: rng);

                case "confused":
                case "confuse":
                    return new ConfusedEffect(
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 2);

                case "stoneskin":
                case "stone":
                    return new StoneskinEffect(
                        reduction: spec.Magnitude > 0f ? (int)spec.Magnitude : 2,
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 30);
                case "weakened":
                    return new WeakenedEffect(
                        strPenalty: spec.Magnitude > 0f ? (int)spec.Magnitude : 1,
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 30
                    );
                case "paperskin":
                    return new PaperSkinEffect(
                        increase: spec.Magnitude > 0f ? (int)spec.Magnitude : 2,
                        duration: spec.DurationTurns > 0 ? spec.DurationTurns : 30);
            }

            return null;  // Unknown EffectName — silently skip.
        }
    }
}
