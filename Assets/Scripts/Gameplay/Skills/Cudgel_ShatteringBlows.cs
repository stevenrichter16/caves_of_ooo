using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class on-hit ShatterArmor passive. Per Qud's
    /// <c>Cudgel_ShatteringBlows.cs:11-25</c> — 10% chance per Cudgel
    /// hit to apply <c>ShatterArmor</c>. Qud's magnitude (2000) is a
    /// per-armor accumulator; CoO's <see cref="ShatterArmorEffect"/>
    /// uses a fixed AV reduction per stack with a duration.
    /// </summary>
    public class Cudgel_ShatteringBlows : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_ShatteringBlows);

        public const int CHANCE_PERCENT = 10;
        public const int DURATION = 4;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Cudgel")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            ctx.Defender.ApplyEffect(new ShatterArmorEffect(DURATION), ctx.Attacker, ctx.Zone);
        }
    }
}
