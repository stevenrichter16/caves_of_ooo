using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cryomancy power: Frost Retort — when an attack against you
    /// MISSES, retaliate with a small Cold damage burst at the
    /// attacker. The mage's chilled aura nips at fools who whiff.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Parallel to <see cref="Pyromancy_ScorchRetort"/>
    /// — same shape, different element.</para>
    ///
    /// <para><b>Mechanic:</b> on every missed-attack against the
    /// defender, deal <see cref="RETORT_DAMAGE"/> Cold damage to the
    /// attacker. Tagged Cold so ColdResistance fires + bonuses
    /// compound (e.g., if attacker is Wet, Cryomancy tree's bonus on
    /// Wet/Frozen targets stacks if you ALSO own CryomancySkill —
    /// though Cryomancy's tree-root buffs only Cold SPELLS, not
    /// retorts, so the stack is effect-aura-style not direct).</para>
    /// </summary>
    public class Cryomancy_FrostRetort : BaseSkillPart
    {
        public override string Name => nameof(Cryomancy_FrostRetort);

        public const int RETORT_DAMAGE = 3;

        public override void OnDefenderAfterAttackMissed(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;

            var dmg = new Damage(RETORT_DAMAGE);
            dmg.AddAttribute("Cold");
            CombatSystem.ApplyDamage(ctx.Attacker, dmg, ctx.Defender, ctx.Zone);
            MessageLog.Add(ctx.Defender.GetDisplayName() + "'s chill bites at " +
                           ctx.Attacker.GetDisplayName() + "!");
        }
    }
}
