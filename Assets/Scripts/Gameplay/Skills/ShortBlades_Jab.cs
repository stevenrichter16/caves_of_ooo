using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class on-hit Confused power. Self-contained per WSP3.3.
    ///
    /// <para><b>Mechanic divergence from Qud:</b> Qud's Jab adds an extra
    /// off-hand attack attempt — a dual-wielding-specific mechanic.
    /// CoO doesn't have a dual-wielding system in v1; reframed as a
    /// status-effect apply that fits the post-damage hook architecture
    /// (parallel to Bludgeon / Lacerate / Bloodletter patterns).</para>
    ///
    /// <para><b>Mechanic (CoO):</b> on every Piercing-attribute hit,
    /// <see cref="CHANCE_PERCENT"/> chance to apply
    /// <see cref="ConfusedEffect"/> for <see cref="DURATION"/> turns.
    /// Stacks on top of the universal Piercing→Confused class hook
    /// (10%, 2T) — a Jab-trained dagger user disorients harder.</para>
    /// </summary>
    public class ShortBlades_Jab : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Jab);

        public const int CHANCE_PERCENT = 30;
        public const int DURATION = 3;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Piercing")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            ctx.Defender.ApplyEffect(new ConfusedEffect(DURATION), ctx.Attacker, ctx.Zone);
        }
    }
}
