using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class on-hit Bleed power. Self-contained per WSP3.3.
    ///
    /// <para><b>Mechanic:</b> on every Piercing-attribute hit,
    /// <see cref="CHANCE_PERCENT"/> chance to apply
    /// <see cref="BleedingEffect"/> with light dice
    /// (<see cref="DAMAGE_DICE"/>). Per Qud's Bloodletter mechanic;
    /// "1d2-1" can't be represented in DiceRoller, so CoO uses "1d2".</para>
    ///
    /// <para>Stacks ON TOP of <see cref="ShortBladesSkill"/>'s
    /// force-Bleed crit hook (1d2 on Critical) and the universal Piercing
    /// class hook (10% Confused, no Bleed). A fully-trained ShortBlades
    /// character on a critical Dagger hit can apply Confused +
    /// Bleed (Bloodletter) + Bleed (crit) all on the same swing.</para>
    ///
    /// <para><b>Divergence from Qud:</b> the original gates application
    /// on <c>defender.Bleeding-count &lt; 1 + Attacker.AgilityMod</c>
    /// to cap stacking. CoO drops this cap for v1 — BleedingEffect's own
    /// OnStack semantics determine combination. Re-introduce if playtest
    /// shows runaway bleed-stacking on speedy Daggers.</para>
    /// </summary>
    public class ShortBlades_Bloodletter : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Bloodletter);

        public const int CHANCE_PERCENT = 50;
        public const int SAVE_TARGET = 15;
        public const string DAMAGE_DICE = "1d2";

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Piercing")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Defender == null || ctx.Rng == null) return;

            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            ctx.Defender.ApplyEffect(
                new BleedingEffect(SAVE_TARGET, DAMAGE_DICE, ctx.Rng),
                ctx.Attacker, ctx.Zone);
        }
    }
}
