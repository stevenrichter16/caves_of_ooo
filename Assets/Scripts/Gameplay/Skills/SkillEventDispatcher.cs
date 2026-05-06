using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// WSP3 — Static dispatcher that fires combat events to an actor's
    /// owned skills. Replaces the WS.1-6b / WSP.1-4b
    /// <c>OnHitSkillEffects.Apply</c> central switch with a per-skill
    /// virtual-override pattern mirroring Qud's
    /// <c>FireEvent</c>/<c>HandleEvent</c>/<c>WantEvent</c> architecture.
    ///
    /// <para>Hot-path discipline: each method iterates
    /// <see cref="SkillsPart.SkillList"/> by index (no LINQ), null-guards
    /// the context, and short-circuits if the actor has no SkillsPart.
    /// Skills are dispatched in registration order — same as Qud's part
    /// list ordering — so an authored skill can rely on deterministic
    /// fire order for chained mechanics (e.g. Backswing-triggered
    /// re-attack fires AFTER the primary swing's Cudgel_Bludgeon).</para>
    ///
    /// <para>CombatSystem call sites (one each at canonical Qud event
    /// timing): see <see cref="CombatSystem.PerformSingleAttack"/>'s
    /// post-hit / post-miss / pre-hit hook points.</para>
    /// </summary>
    public static class SkillEventDispatcher
    {
        /// <summary>
        /// Fired AFTER a successful melee hit + damage application,
        /// inside the survivor block (<c>hpAfter &gt; 0</c>). Routed to
        /// every owned skill via <see cref="BaseSkillPart.OnAttackerAfterAttack"/>.
        /// Mirrors Qud's <c>"AttackerAfterAttack"</c> event.
        /// </summary>
        public static void AttackerAfterAttack(Entity attacker, SkillEventContext ctx)
        {
            if (ctx == null) return;
            var skills = attacker?.GetPart<SkillsPart>();
            if (skills == null) return;
            var list = skills.SkillList;
            for (int i = 0; i < list.Count; i++)
            {
                var skill = list[i];
                if (skill == null) continue;
                skill.OnAttackerAfterAttack(ctx);
            }
        }

        /// <summary>
        /// Fired ONCE PER MISSED MELEE SWING, after the message is
        /// logged and before <c>PerformSingleAttack</c> returns. Routed
        /// to every owned skill via <see cref="BaseSkillPart.OnAttackerMeleeMiss"/>.
        /// Mirrors Qud's <c>"AttackerMeleeMiss"</c> event. Used by
        /// Cudgel_Backswing for re-attack-on-miss.
        /// </summary>
        public static void AttackerMeleeMiss(Entity attacker, SkillEventContext ctx)
        {
            if (ctx == null) return;
            var skills = attacker?.GetPart<SkillsPart>();
            if (skills == null) return;
            var list = skills.SkillList;
            for (int i = 0; i < list.Count; i++)
            {
                var skill = list[i];
                if (skill == null) continue;
                skill.OnAttackerMeleeMiss(ctx);
            }
        }

        /// <summary>
        /// Fired on the DEFENDER's skill list when an incoming attack
        /// missed them. Mirrors Qud's <c>"DefenderAfterAttackMissed"</c>
        /// event. Used by ShortBlades_Rejoinder for free counter-attacks.
        /// </summary>
        public static void DefenderAfterAttackMissed(Entity defender, SkillEventContext ctx)
        {
            if (ctx == null) return;
            var skills = defender?.GetPart<SkillsPart>();
            if (skills == null) return;
            var list = skills.SkillList;
            for (int i = 0; i < list.Count; i++)
            {
                var skill = list[i];
                if (skill == null) continue;
                skill.OnDefenderAfterAttackMissed(ctx);
            }
        }

        /// <summary>
        /// Fired AFTER a critical hit lands. Routed to every owned skill
        /// via <see cref="BaseSkillPart.OnWeaponMadeCriticalHit"/>. Mirrors
        /// Qud's <c>WeaponMadeCriticalHit</c> virtual on tree-root
        /// skill classes (Cudgel.cs:17, Axe.cs:15, LongBlades.cs:13,
        /// ShortBlades.cs:19). Tree-roots typically override this to
        /// apply their per-class crit effect (Cudgel: stun, Axe: cleave,
        /// LongBlades: extra damage, ShortBlades: bleed).
        /// </summary>
        public static void WeaponMadeCriticalHit(Entity attacker, SkillEventContext ctx)
        {
            if (ctx == null) return;
            var skills = attacker?.GetPart<SkillsPart>();
            if (skills == null) return;
            var list = skills.SkillList;
            for (int i = 0; i < list.Count; i++)
            {
                var skill = list[i];
                if (skill == null) continue;
                skill.OnWeaponMadeCriticalHit(ctx);
            }
        }

        /// <summary>
        /// Returns the SUM of <see cref="BaseSkillPart.OnGetToHitModifier"/>
        /// across all owned skills. Used by <see cref="CombatSystem.PerformSingleAttack"/>
        /// during the hit-roll calculation — feeds into <c>totalHit</c>
        /// alongside <c>agilityMod</c> and <c>weapon.HitBonus</c>. Mirrors
        /// Qud's <c>GetToHitModifierEvent</c> aggregation pattern (the
        /// Expertise skills add +2 each via this hook).
        /// </summary>
        public static int GetSkillHitModifier(Entity attacker, MeleeWeaponPart weapon)
        {
            if (attacker == null) return 0;
            var skills = attacker.GetPart<SkillsPart>();
            if (skills == null) return 0;
            var list = skills.SkillList;
            int total = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var skill = list[i];
                if (skill == null) continue;
                total += skill.OnGetToHitModifier(attacker, weapon);
            }
            return total;
        }
    }
}
