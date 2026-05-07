using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism power: Shock Retort — when an attack against you
    /// MISSES, retaliate with a small Electric damage burst at the
    /// attacker. The mage's static aura zaps anyone who lunges and
    /// whiffs.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Parallel to the other 3 elemental retort
    /// powers (FrostRetort, ScorchRetort, AcidRetort).</para>
    /// </summary>
    public class Galvanism_ShockRetort : BaseSkillPart
    {
        public override string Name => nameof(Galvanism_ShockRetort);

        public const int RETORT_DAMAGE = 3;

        public override void OnDefenderAfterAttackMissed(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;

            var dmg = new Damage(RETORT_DAMAGE);
            dmg.AddAttribute("Electric");
            CombatSystem.ApplyDamage(ctx.Attacker, dmg, ctx.Defender, ctx.Zone);
            MessageLog.Add(ctx.Defender.GetDisplayName() + "'s static jumps to " +
                           ctx.Attacker.GetDisplayName() + "!");
        }
    }
}
