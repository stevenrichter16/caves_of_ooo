using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Class-based on-hit status effect hooks. Reads the physical-class
    /// attributes on a successful melee hit's <see cref="Damage"/> object
    /// and rolls per-class probabilities to apply matching status effects.
    ///
    /// Wiring (Tier-2 ship after weapon-attribute backfill):
    ///   Bludgeoning damage → <see cref="StunnedEffect"/> at <see cref="BLUDGEONING_STUN_CHANCE_PERCENT"/>
    ///   Cutting damage     → <see cref="BleedingEffect"/> at <see cref="CUTTING_BLEED_CHANCE_PERCENT"/>
    ///   Piercing damage    → <see cref="ConfusedEffect"/> at <see cref="PIERCING_CONFUSE_CHANCE_PERCENT"/>
    ///
    /// Called from <c>CombatSystem.PerformSingleAttack</c> post-<c>ApplyDamage</c>
    /// inside the existing <c>if (hpAfter > 0)</c> block, so dead targets are
    /// not effect-applied. Uses the same <see cref="Random"/> instance as the
    /// rest of the attack pipeline for test determinism.
    ///
    /// Why class-based and not per-weapon-only? The weapon-attribute backfill
    /// (<c>9c34cb0</c>) declared physical-class tags on every melee weapon
    /// but those tags were inert metadata. This system gives them universal
    /// behavior — every Bludgeoning weapon can stun, every Cutting weapon
    /// can bleed, etc. — without per-weapon JSON. Per-weapon overrides are
    /// handled separately by <see cref="OnHitWeaponEffects"/> for elemental
    /// weapons that need their own thematic hook on top.
    /// </summary>
    public static class OnHitClassEffects
    {
        // ---- Bludgeoning → Stunned ----
        public const int BLUDGEONING_STUN_CHANCE_PERCENT = 15;
        public const int BLUDGEONING_STUN_DURATION = 1;

        // ---- Cutting → Bleeding ----
        public const int CUTTING_BLEED_CHANCE_PERCENT = 25;
        public const int CUTTING_BLEED_SAVE_TARGET = 15;
        public const string CUTTING_BLEED_DAMAGE_DICE = "1d2";

        // ---- Piercing → Confused ----
        public const int PIERCING_CONFUSE_CHANCE_PERCENT = 10;
        public const int PIERCING_CONFUSE_DURATION = 2;

        /// <summary>
        /// Apply class-based on-hit effects after a successful melee hit.
        /// </summary>
        /// <param name="damage">The damage object whose attributes will be inspected.</param>
        /// <param name="actualDamage">Real HP delta applied (post-resistance, post-Phase-F).
        /// If 0, the hit was vetoed or fully resisted — skip all effect application.</param>
        /// <param name="defender">The entity that took the hit. May be null (no-op).</param>
        /// <param name="attacker">The entity that swung. May be null (effect's source becomes null).</param>
        /// <param name="zone">Live zone for effect application.</param>
        /// <param name="rng">Deterministic RNG, shared with the attack pipeline.</param>
        public static void Apply(Damage damage, int actualDamage,
            Entity defender, Entity attacker, Zone zone, Random rng)
        {
            // Null-safety guards: any of these missing → no-op rather than throw.
            if (damage == null || defender == null || rng == null) return;

            // Vetoed/fully-resisted hits don't trigger on-hit effects. Mirrors
            // the contract that "no damage = no on-hit." (Glowmaw fully absorbing
            // Fire damage shouldn't also stun the player from the Bludgeoning
            // physical class on the same swing.)
            if (actualDamage <= 0) return;

            // Bludgeoning → Stunned. Uses Damage.IsBludgeoningDamage() which
            // matches both "Bludgeoning" and "Cudgel" attributes (Damage.cs:131).
            if (damage.IsBludgeoningDamage())
                TryApplyStunned(defender, attacker, zone, rng);

            // Cutting → Bleeding. No IsCuttingDamage() helper exists; check
            // attribute directly. Bleeding ticks at start-of-turn and saves out.
            if (damage.HasAttribute("Cutting"))
                TryApplyBleeding(defender, attacker, zone, rng);

            // Piercing → Confused. ConfusedEffect.CanApply rejects stacking,
            // so re-applying to an already-confused target is a silent no-op.
            if (damage.HasAttribute("Piercing"))
                TryApplyConfused(defender, attacker, zone, rng);
        }

        private static void TryApplyStunned(Entity defender, Entity source, Zone zone, Random rng)
        {
            if (rng.Next(100) >= BLUDGEONING_STUN_CHANCE_PERCENT) return;
            defender.ApplyEffect(new StunnedEffect(BLUDGEONING_STUN_DURATION), source, zone);
        }

        private static void TryApplyBleeding(Entity defender, Entity source, Zone zone, Random rng)
        {
            if (rng.Next(100) >= CUTTING_BLEED_CHANCE_PERCENT) return;
            defender.ApplyEffect(
                new BleedingEffect(CUTTING_BLEED_SAVE_TARGET, CUTTING_BLEED_DAMAGE_DICE, rng),
                source, zone);
        }

        private static void TryApplyConfused(Entity defender, Entity source, Zone zone, Random rng)
        {
            if (rng.Next(100) >= PIERCING_CONFUSE_CHANCE_PERCENT) return;
            defender.ApplyEffect(new ConfusedEffect(PIERCING_CONFUSE_DURATION), source, zone);
        }
    }
}
