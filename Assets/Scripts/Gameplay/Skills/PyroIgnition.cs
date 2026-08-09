using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Shared ignition rule for the SM4 Pyromancy actives: heat a target,
    /// and set it alight only if it is not already soaked.
    ///
    /// <para><b>Why this exists.</b> The canonical ignition path,
    /// <c>ThermalPart.TryIgnite</c> (ThermalPart.cs:110-122), refuses to
    /// light a target whose <see cref="WetEffect.Moisture"/> exceeds
    /// <see cref="MoistureSuppressionThreshold"/> and boils off some of
    /// the water instead. Every existing spell that applies fire —
    /// <c>FireBoltMutation</c>, <c>ConflagrationMutation</c>, the
    /// FlamingSword on-hit — bypasses that path and calls
    /// <c>ApplyEffect(new BurningEffect(...))</c> directly, so a drenched
    /// target catches fire anyway. <see cref="BurningEffect"/> itself has
    /// no moisture check at all.</para>
    ///
    /// <para><b>Why that matters here.</b> The status grammar this
    /// feature teaches (Docs/SPELLCRAFT-STATUS-SYNERGY.md §4.2) depends
    /// on water being fire's counter: soaking a target sets it up for
    /// lightning AND protects it from flame. A combo that deliberately
    /// does NOT work teaches the element rules as sharply as one that
    /// does. If Flame Jet lit a soaked target, the player would learn
    /// that soaking is free — the opposite of the intended lesson, and a
    /// contradiction of what <c>EffectDescriber</c> already tells them
    /// ("douses flame, conducts shock").</para>
    ///
    /// <para><b>Known divergence.</b> Only the SM4 powers route through
    /// here. The older direct-appliers listed above still ignite through
    /// water. Unifying them means touching several shipped mutations and
    /// their tests, so it is deliberately deferred and recorded in the
    /// living doc rather than smuggled into this milestone.</para>
    /// </summary>
    internal static class PyroIgnition
    {
        /// <summary>Mirrors ThermalPart.cs:117. Above this, water wins.</summary>
        internal const float MoistureSuppressionThreshold = 0.35f;

        /// <summary>Moisture boiled off by a suppressed ignition.
        /// Mirrors ThermalPart.cs:120 — repeated fire eventually dries a
        /// target out, so water is a delay, not an immunity.</summary>
        internal const float SuppressionEvaporation = 0.1f;

        /// <summary>
        /// Applies <see cref="BurningEffect"/> unless the target is too
        /// wet to catch. Returns true if it caught fire.
        /// </summary>
        internal static bool TryIgnite(
            Entity target, float intensity, Entity source, Zone zone, System.Random rng)
        {
            if (target == null) return false;

            var wet = target.GetEffect<WetEffect>();
            if (wet != null && wet.Moisture > MoistureSuppressionThreshold)
            {
                // Boil some of it off instead. Hitting the same soaked
                // target twice will eventually light it.
                wet.Moisture -= SuppressionEvaporation;
                MessageLog.Add(target.GetDisplayName()
                    + " steams — too wet to catch.");
                return false;
            }

            target.ApplyEffect(
                new BurningEffect(intensity: intensity, source: source, rng: rng),
                source, zone);
            return true;
        }
    }
}
