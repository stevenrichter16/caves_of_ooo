using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy power: BrittleStrike — bonus melee damage when
    /// striking a Frozen target. Frozen flesh shatters; the metaphor
    /// is the cold-mage's spells set up the kill, the melee finishes
    /// it. Works with any weapon; the skill cares about the target's
    /// Frozen status, not the attacker's element.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Magic-melee crossover skill — rewards hybrid
    /// builds (cast IceLance to Freeze a target, then walk up and
    /// melee for bonus damage).</para>
    ///
    /// <para><b>Mechanic:</b> overrides
    /// <see cref="BaseSkillPart.OnAttackerAfterAttack"/>. Fires on
    /// every melee swing that lands damage. If the defender has
    /// <see cref="FrozenEffect"/>, deals
    /// <see cref="BRITTLE_BONUS_PERCENT"/>% of <c>actualDamage</c> as
    /// extra Cold damage (floor 1). The bonus is applied as a
    /// separate <see cref="CombatSystem.ApplyDamage"/> call (not
    /// folded into the original swing) so resistance pipelines fire
    /// for the bonus too — a Cold-resistant Frozen target takes
    /// reduced bonus damage.</para>
    ///
    /// <para>Pairs with <see cref="CryomancySkill"/> (which buffs
    /// Cold-spell damage on Wet/Frozen) — together, an ice-mage build
    /// gets bonuses on both the spell that Froze the target AND the
    /// follow-up melee.</para>
    /// </summary>
    public class Cryomancy_BrittleStrike : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_BrittleStrike);

        /// <summary>
        /// Percent of actualDamage to add as bonus Cold damage when
        /// striking a Frozen target. 50 = +50% bonus melee damage on
        /// Frozen — significant enough to feel impactful, not so big
        /// it trivializes Frozen-specced enemies.
        /// </summary>
        public const int BRITTLE_BONUS_PERCENT = 50;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Defender == null) return;
            if (ctx.ActualDamage <= 0) return;

            var effects = ctx.Defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<FrozenEffect>()) return;

            int bonus = (ctx.ActualDamage * BRITTLE_BONUS_PERCENT) / 100;
            if (bonus <= 0) bonus = 1;

            // Apply as a separate Cold-tagged damage so resistance
            // fires + the damage shows up in the log as a follow-up
            // hit. Mirrors the WSP6 cleave-helper pattern (Axe_Cleave
            // applies an additional damage call on top of the swing).
            var dmg = new Damage(bonus);
            dmg.AddAttribute("Cold");
            CombatSystem.ApplyDamage(ctx.Defender, dmg, ctx.Attacker, ctx.Zone);
        }
    }
}
