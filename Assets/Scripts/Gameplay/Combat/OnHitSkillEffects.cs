using System;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// WS — Skill-driven on-hit effect hooks. Reads the attacker's
    /// owned skills (via <see cref="SkillsPart"/>) and applies the
    /// matching effects on a successful melee hit. Mirrors the shape
    /// of <see cref="OnHitClassEffects"/> exactly — same parameter
    /// list, same null-safety guards, same actualDamage&gt;0 gate.
    ///
    /// Order in <c>CombatSystem.PerformSingleAttack</c> (post-<c>ApplyDamage</c>,
    /// inside the <c>if (hpAfter &gt; 0)</c> block):
    /// <list type="number">
    /// <item><see cref="OnHitClassEffects.Apply"/> — universal class
    ///   tags (Bludgeoning→Stun, Cutting→Bleed, Piercing→Confuse).</item>
    /// <item><see cref="OnHitWeaponEffects.Apply"/> — per-weapon overrides
    ///   (FlamingSword→Burning, IceSword→Frozen, etc.).</item>
    /// <item><see cref="OnHitSkillEffects.Apply"/> — owner's skills
    ///   (Cudgel_Bludgeon→Stun, LongBlades_Lacerate→Bleed, etc.).</item>
    /// </list>
    ///
    /// <para>Stacks independently of the prior two: a Mace
    /// (Bludgeoning + Cudgel) wielded by an actor with
    /// <c>Cudgel_Bludgeon</c> rolls TWO stun chances on the same hit
    /// — 15% from class hooks, 35% from skill hooks. Stunned's
    /// <c>OnStack</c> extends duration, so a player can land
    /// multi-effect mauls without the second roll being wasted.</para>
    ///
    /// <para>Per-skill behavior is documented inline at the matching
    /// <c>TrySkillName</c> private method. The branch tree in
    /// <see cref="Apply"/> is intentionally kept FLAT (one if-block per
    /// skill) for grep-ability when a future content ship adds a 5th
    /// or 6th weapon-class power.</para>
    /// </summary>
    public static class OnHitSkillEffects
    {
        // ─────────────────────────────────────────────────────────────────
        // Per-skill tunables. WS.2-5 fill in. WS.1 ships the empty Apply.
        // ─────────────────────────────────────────────────────────────────

        // Cudgel_Bludgeon (WS.2): Cudgel-class hit → chance to Stun.
        // Higher than the universal Bludgeoning→Stun (15%) since this
        // requires a deliberate skill purchase + a Cudgel-attribute
        // weapon (Mace / Warhammer / Cudgel / OldWorldPipe). Stacks
        // with the class hook on the same hit; StunnedEffect.OnStack
        // extends duration so the second roll isn't wasted.
        public const int CUDGEL_BLUDGEON_CHANCE_PERCENT = 35;
        public const int CUDGEL_BLUDGEON_DURATION = 3;

        /// <summary>
        /// Apply skill-driven on-hit effects. Same contract as
        /// <see cref="OnHitClassEffects.Apply"/>: short-circuits if any
        /// argument is null, if actualDamage&lt;=0 (vetoed/fully-resisted
        /// hits don't trigger on-hit), or if the attacker has no
        /// <see cref="SkillsPart"/> (nobody to query for owned skills).
        /// </summary>
        /// <param name="damage">Damage object inspected for class attributes (Cudgel/Axe/etc.).</param>
        /// <param name="actualDamage">Real HP delta applied. 0 = vetoed/resisted; skip.</param>
        /// <param name="defender">The entity that took the hit.</param>
        /// <param name="attacker">The entity that swung — read its SkillsPart for owned skills.</param>
        /// <param name="zone">Live zone for effect application + adjacent-target lookups (Cleave).</param>
        /// <param name="rng">Deterministic RNG, shared with the attack pipeline.</param>
        public static void Apply(Damage damage, int actualDamage,
            Entity defender, Entity attacker, Zone zone, Random rng)
        {
            // Null-safety: any of these missing → silently no-op rather
            // than throw. Mirrors OnHitClassEffects.Apply contract exactly.
            if (damage == null || defender == null || attacker == null || rng == null) return;

            // Vetoed / fully-resisted hits don't trigger on-hit effects.
            // Same threshold as OnHitClassEffects: an attack that did
            // 0 actual damage (e.g. Glowmaw fully absorbing fire) shouldn't
            // also fire skill-tier effects on the same swing.
            if (actualDamage <= 0) return;

            // No SkillsPart on the attacker → no owned skills to check.
            // This is the common path for non-player attackers (creatures
            // don't have SkillsPart by default), so the early-out is fast.
            var skills = attacker.GetPart<SkillsPart>();
            if (skills == null) return;

            // Cudgel_Bludgeon (WS.2): Cudgel-attribute hit → chance to
            // Stun for 3T. Distinct from the OnHitClassEffects 15%
            // Bludgeoning→Stun roll: this fires on the Cudgel sub-class
            // attribute specifically (so a wholly-Bludgeoning weapon
            // like the basic Cudgel — yes the weapon is also named that
            // — won't trigger this branch). Mace / Warhammer / OldWorldPipe
            // carry both attributes and roll BOTH chances.
            if (skills.HasSkill(nameof(Cudgel_Bludgeon))
                && damage.HasAttribute("Cudgel"))
            {
                TryCudgelBludgeon(defender, attacker, zone, rng);
            }

            // (additional skill branches added in WS.3-5)
        }

        // ─────────────────────────────────────────────────────────────────
        // Per-skill apply helpers
        // ─────────────────────────────────────────────────────────────────

        private static void TryCudgelBludgeon(Entity defender, Entity attacker,
            Zone zone, Random rng)
        {
            int roll = rng.Next(100);
            if (roll >= CUDGEL_BLUDGEON_CHANCE_PERCENT) return;

            var stun = new StunnedEffect(CUDGEL_BLUDGEON_DURATION);
            defender.ApplyEffect(stun, attacker, zone);
        }
    }
}
