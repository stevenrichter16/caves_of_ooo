using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Cudgel-class on-hit Stun power. Self-contained per WSP3.3 — all
    /// behavior + tunables live in this file; modifying the skill means
    /// editing only this class.
    ///
    /// <para><b>Mechanic (Qud-verbatim):</b> on every Cudgel-attribute
    /// hit (post-damage, defender survived), <see cref="CHANCE_PERCENT"/>
    /// chance to apply <see cref="StunnedEffect"/> for a random
    /// <see cref="DURATION_MIN"/>-<see cref="DURATION_MAX"/> turn duration.
    /// Per Qud's <c>Cudgel_Bludgeon.cs:56</c> — base 50%
    /// <c>"Skill Bludgeon"</c> chance, <c>Stat.Random(3, 4)</c> duration.</para>
    ///
    /// <para>Stacks independently of: the universal Bludgeoning class
    /// hook (15% / 2T), the CudgelSkill tree-root crit hook (100% / 1-4T
    /// on crit). StunnedEffect.OnStack sums durations, so a Mace crit
    /// by an actor owning Cudgel + Cudgel_Bludgeon can pile up 8-10T
    /// of stun from a single swing.</para>
    /// </summary>
    public class Cudgel_Bludgeon : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Bludgeon);

        public const int CHANCE_PERCENT = 50;
        public const int DURATION_MIN = 3;
        public const int DURATION_MAX = 4;  // inclusive — per Qud Stat.Random(3, 4)

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Cudgel")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            int duration = ctx.Rng.Next(DURATION_MIN, DURATION_MAX + 1);
            ctx.Defender.ApplyEffect(new StunnedEffect(duration), ctx.Attacker, ctx.Zone);
        }
    }
}
