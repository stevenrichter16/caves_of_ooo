using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy power: Scorch Retort — when an attack against you
    /// MISSES, retaliate with a small Heat damage burst at the
    /// attacker. The mage's natural fire aura punishes whiffs.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Defender-side magic skill — first power to use
    /// the existing <see cref="BaseSkillPart.OnDefenderAfterAttackMissed"/>
    /// hook for an elemental retaliation (rather than a melee
    /// counter-attack like <see cref="ShortBlades_Rejoinder"/>).</para>
    ///
    /// <para><b>Mechanic:</b> on every melee attack against the
    /// defender that misses, deal <see cref="RETORT_DAMAGE"/> Heat
    /// damage to the attacker. No chance roll (it's a small flat
    /// damage; reliability is the appeal). Tagged Heat so
    /// HeatResistance fires + the damage compounds with Pyromancy
    /// tree's other "+Heat damage to Burning" bonuses if a previous
    /// hit set the attacker on fire.</para>
    ///
    /// <para>The recursion-guard pattern (a retort-on-miss could in
    /// theory cascade if the retort itself misses — but Heat-damage
    /// retorts don't go through PerformSingleAttack, so no recursion
    /// risk; just direct ApplyDamage).</para>
    /// </summary>
    public class Pyromancy_ScorchRetort : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_ScorchRetort);

        public const int RETORT_DAMAGE = 3;

        public override void OnDefenderAfterAttackMissed(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;

            var dmg = new Damage(RETORT_DAMAGE);
            dmg.AddAttribute("Heat");
            CombatSystem.ApplyDamage(ctx.Attacker, dmg, ctx.Defender, ctx.Zone);
            MessageLog.Add(ctx.Defender.GetDisplayName() + "'s flames lash back at " +
                           ctx.Attacker.GetDisplayName() + "!");
        }
    }
}
