using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Galvanism power: GroundStrike — bonus melee damage when striking
    /// an Electrified target. The electricity already running through
    /// the target couples to the metal blade and discharges. Works with
    /// any melee weapon; gates on target state, not attacker element.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Magic-melee crossover — rewards
    /// "ArcBolt → close → melee" play patterns.</para>
    ///
    /// <para><b>Mechanic:</b> on every melee swing that lands damage,
    /// if the defender has <see cref="ElectrifiedEffect"/>, deals
    /// <see cref="GROUND_BONUS_PERCENT"/>% of <c>actualDamage</c> as
    /// extra Electric damage (floor 1). Applied as a separate
    /// <see cref="CombatSystem.ApplyDamage"/> call so
    /// ElectricResistance fires for the bonus too.</para>
    /// </summary>
    public class Galvanism_GroundStrike : BaseSkillPart
    {
        public override string Name => nameof(Galvanism_GroundStrike);

        public const int GROUND_BONUS_PERCENT = 50;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Defender == null) return;
            if (ctx.ActualDamage <= 0) return;

            var effects = ctx.Defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<ElectrifiedEffect>()) return;

            int bonus = (ctx.ActualDamage * GROUND_BONUS_PERCENT) / 100;
            if (bonus <= 0) bonus = 1;

            var dmg = new Damage(bonus);
            dmg.AddAttribute("Electric");
            CombatSystem.ApplyDamage(ctx.Defender, dmg, ctx.Attacker, ctx.Zone);
        }
    }
}
