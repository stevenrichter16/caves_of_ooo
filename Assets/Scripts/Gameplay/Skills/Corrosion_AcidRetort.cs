using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Corrosion power: Acid Retort — when an attack against you
    /// MISSES, retaliate with a small Acid damage burst at the
    /// attacker. The mage's caustic aura splashes anyone who lunges
    /// past.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Completes the 4-element retort suite
    /// (FrostRetort, ScorchRetort, ShockRetort, AcidRetort) — one
    /// per damage-elemental tree.</para>
    /// </summary>
    public class Corrosion_AcidRetort : BaseSkillPart
    {
        public override string Name => nameof(Corrosion_AcidRetort);

        public const int RETORT_DAMAGE = 3;

        public override void OnDefenderAfterAttackMissed(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Attacker == null || ctx.Defender == null) return;

            var dmg = new Damage(RETORT_DAMAGE);
            dmg.AddAttribute("Acid");
            CombatSystem.ApplyDamage(ctx.Attacker, dmg, ctx.Defender, ctx.Zone);
            MessageLog.Add(ctx.Defender.GetDisplayName() + "'s caustic aura sears " +
                           ctx.Attacker.GetDisplayName() + "!");
        }
    }
}
