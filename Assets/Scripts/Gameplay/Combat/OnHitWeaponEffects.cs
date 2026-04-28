using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Per-weapon on-hit status effects. Reads
    /// <see cref="MeleeWeaponPart.OnHitEffectsRaw"/>, parses it into
    /// <see cref="OnHitEffectSpec"/> entries, rolls each spec's
    /// ChancePercent independently, and applies the matching status
    /// effect via <see cref="OnHitEffectFactory"/>.
    ///
    /// Called from <c>CombatSystem.PerformSingleAttack</c> immediately
    /// after <see cref="OnHitClassEffects.Apply"/>, inside the same
    /// <c>if (hpAfter > 0)</c> block. Stack ordering: class hooks fire
    /// first, then per-weapon overrides — so a Bludgeoning ThunderHammer
    /// rolls the 15% Stun chance first, then independently rolls the
    /// 30% Electrified chance from its per-weapon spec.
    ///
    /// Caches parsed specs on the <see cref="MeleeWeaponPart"/> via a
    /// per-instance field <see cref="MeleeWeaponPart.OnHitEffectsRaw"/>
    /// re-parse — current implementation re-parses on every hit.
    /// (See 🟡 finding in plan: cache if profiling shows it matters.)
    /// </summary>
    public static class OnHitWeaponEffects
    {
        public static void Apply(MeleeWeaponPart weapon, Damage damage, int actualDamage,
            Entity defender, Entity attacker, Zone zone, Random rng)
        {
            // Null-safety / no-op gates: matches OnHitClassEffects's contract.
            if (weapon == null || defender == null || rng == null) return;
            if (actualDamage <= 0) return;
            if (string.IsNullOrWhiteSpace(weapon.OnHitEffectsRaw)) return;

            List<OnHitEffectSpec> specs = OnHitEffectSpec.Parse(weapon.OnHitEffectsRaw);
            for (int i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];

                // Independent chance roll per spec — each effect can fire
                // independently of others on the same weapon.
                if (rng.Next(100) >= spec.ChancePercent) continue;

                Effect effect = OnHitEffectFactory.Create(spec, attacker, rng);
                if (effect == null) continue;  // Unknown EffectName — skip silently.

                defender.ApplyEffect(effect, attacker, zone);
            }
        }
    }
}
