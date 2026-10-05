using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// CoO passive control: a damaging cudgel hit can create a brief opening.
    /// A two-turn ceiling and Toughness save keep this passive distinct from
    /// deliberate Conk/Slam control. It never adds time to an existing stun.
    /// Weapon-class and tree-root effects retain their own behavior.
    /// </summary>
    public class Cudgel_Bludgeon : BaseSkillPart
    {
        public override string Name => nameof(Cudgel_Bludgeon);

        public const int CHANCE_PERCENT = 50;
        public const int DURATION_MIN = 2;
        public const int DURATION_MAX = 2;
        public const int SAVE_TARGET = 16;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Cudgel")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            // A passive opening must not bank extra disabled turns or extend
            // a deliberate Conk/Slam. Other stun sources retain their contracts.
            if (ctx.Defender.HasEffect<StunnedEffect>()) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            int duration = ctx.Rng.Next(DURATION_MIN, DURATION_MAX + 1);
            ctx.Defender.ApplyEffect(new StunnedEffect(duration, SAVE_TARGET, ctx.Rng), ctx.Attacker, ctx.Zone);
        }
    }
}
