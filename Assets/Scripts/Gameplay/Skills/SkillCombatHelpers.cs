using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// WSP3.3 — Shared helper utilities for skill combat-event handlers.
    /// Each helper is a single-responsibility static method that multiple
    /// skill classes can call from their event-override implementations
    /// without forcing inheritance hierarchies.
    ///
    /// <para>The cleave-target lookup lives here because both
    /// <see cref="Axe_Cleave"/> (gated by 30% chance) and
    /// <see cref="AxeSkill"/> (force-cleave on crit) need identical
    /// adjacent-Creature-pickup semantics. Extracting kept the previous
    /// <c>OnHitSkillEffects.cs</c> 22-line cleave loop from being
    /// duplicated between the two skills' virtual overrides — same
    /// rationale as the WSP.4b cold-eye-fix extraction.</para>
    /// </summary>
    public static class SkillCombatHelpers
    {
        /// <summary>
        /// Finds the first Creature entity adjacent to defender (in
        /// direction-iteration order N → NE → E → SE → S → SW → W → NW)
        /// that isn't the attacker themselves. Null if none found or
        /// if zone/position lookup fails.
        ///
        /// <para>Direction-iteration order is deterministic so seeded
        /// tests can pin the cleave victim. The first-found-Creature
        /// wins; no random target selection.</para>
        /// </summary>
        public static Entity FindAdjacentCleaveTarget(Entity defender,
            Entity attacker, Zone zone)
        {
            if (zone == null) return null;
            var defPos = zone.GetEntityPosition(defender);
            if (defPos.x < 0) return null;

            for (int dir = 0; dir < 8; dir++)
            {
                var cell = zone.GetCellInDirection(defPos.x, defPos.y, dir);
                if (cell == null) continue;
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var e = cell.Objects[i];
                    if (e == null || e == attacker || e == defender) continue;
                    if (!e.Tags.ContainsKey("Creature")) continue;
                    return e;
                }
            }
            return null;
        }

        /// <summary>
        /// Convenience wrapper: find an adjacent cleave target and, if
        /// one exists, deal max(1, actualDamage/2) damage to it. Returns
        /// true if cleave landed on a target. Used by Axe_Cleave (gated
        /// by chance) and AxeSkill (force-cleave on crit).
        /// </summary>
        public static bool ExecuteCleave(int actualDamage, Entity defender,
            Entity attacker, Zone zone)
        {
            var target = FindAdjacentCleaveTarget(defender, attacker, zone);
            if (target == null) return false;

            int cleaveDamage = System.Math.Max(1, actualDamage / 2);
            CombatSystem.ApplyDamage(target, cleaveDamage, attacker, zone);
            return true;
        }
    }
}
