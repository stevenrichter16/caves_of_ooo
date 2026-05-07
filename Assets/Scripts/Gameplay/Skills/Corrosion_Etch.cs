using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Corrosion power: Etch — bonus melee damage when striking an
    /// Acidic target. The acid weakens armor and flesh; the melee
    /// blade etches through the dissolved layer.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Magic-melee crossover — completes the
    /// Corrosion / Cryomancy / Galvanism / Pyromancy crossover suite
    /// (one melee bonus power per damage-elemental tree, all gating
    /// on the corresponding status effect). Hybrid spell+melee
    /// players get ladder bonuses across all four damage elements.</para>
    ///
    /// <para><b>Mechanic:</b> on every melee swing that lands damage,
    /// if the defender has <see cref="AcidicEffect"/>, deals
    /// <see cref="ETCH_BONUS_PERCENT"/>% of <c>actualDamage</c> as
    /// extra Acid damage (floor 1).</para>
    /// </summary>
    public class Corrosion_Etch : BaseSkillPart
    {
        public override string Name => nameof(Corrosion_Etch);

        public const int ETCH_BONUS_PERCENT = 50;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Defender == null) return;
            if (ctx.ActualDamage <= 0) return;

            var effects = ctx.Defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<AcidicEffect>()) return;

            int bonus = (ctx.ActualDamage * ETCH_BONUS_PERCENT) / 100;
            if (bonus <= 0) bonus = 1;

            var dmg = new Damage(bonus);
            dmg.AddAttribute("Acid");
            CombatSystem.ApplyDamage(ctx.Defender, dmg, ctx.Attacker, ctx.Zone);
        }
    }
}
