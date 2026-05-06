using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Long-Blade-class on-hit Bleed power. Self-contained per WSP3.3.
    ///
    /// <para><b>Mechanic:</b> on every LongBlades-attribute hit,
    /// <see cref="CHANCE_PERCENT"/> chance to apply
    /// <see cref="BleedingEffect"/> with stronger dice
    /// (<see cref="DAMAGE_DICE"/> — "1d3") than the universal Cutting
    /// class hook ("1d2"). The Rng is forwarded into the BleedingEffect
    /// ctor so its tick rolls are deterministic for seeded tests.</para>
    ///
    /// <para>Stacks ON TOP of <see cref="LongBladesSkill"/>'s force-Bleed
    /// crit hook (1d4 dice on Critical) and the universal 25% Cutting→Bleed
    /// class hook. BleedingEffect.OnStack semantics determine how multiple
    /// Bleeds combine.</para>
    /// </summary>
    public class LongBlades_Lacerate : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Lacerate);

        public const int CHANCE_PERCENT = 35;
        public const int SAVE_TARGET = 15;
        public const string DAMAGE_DICE = "1d3";

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("LongBlades")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            ctx.Defender.ApplyEffect(
                new BleedingEffect(SAVE_TARGET, DAMAGE_DICE, ctx.Rng),
                ctx.Attacker, ctx.Zone);
        }
    }
}
