using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class on-hit Hobble passive. <b>Reframed from Qud's
    /// active-ability version</b> (Qud's Hobble is a 30-cooldown
    /// command-driven attack that applies 16-20T Hobbled). CoO doesn't
    /// have ActivatedAbility-skill integration in this ship (Tier 3
    /// follow-on); ported as a passive on-hit chance with shorter
    /// duration since "fires every hit" is balanced differently than
    /// "opt-in cost".
    ///
    /// <para><b>Mechanic (CoO):</b>
    /// <see cref="CHANCE_PERCENT"/> chance per Piercing hit to apply
    /// <see cref="HobbledEffect"/> for <see cref="DURATION"/> turns.</para>
    /// </summary>
    public class ShortBlades_Hobble : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Hobble);

        public const int CHANCE_PERCENT = 15;
        public const int DURATION = 8;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Piercing")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            ctx.Defender.ApplyEffect(new HobbledEffect(DURATION), ctx.Attacker, ctx.Zone);
        }
    }
}
