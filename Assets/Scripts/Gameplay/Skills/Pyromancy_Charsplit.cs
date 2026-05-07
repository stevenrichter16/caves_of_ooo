using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Pyromancy power: Charsplit — bonus melee damage when striking
    /// a Charred target. Charred flesh is brittle and split easily;
    /// the metaphor is the fire-mage's spells weakened the structure,
    /// the melee shears through the brittle layer.
    ///
    /// <para><b>Classification (CLAUDE.md §4.2): CoO-Original
    /// Extension.</b> Magic-melee crossover — completes the
    /// Pyromancy ladder: PyromancySkill rewards Heat→Burning,
    /// Pyromancy_Cinder rewards Heat→Charred, Charsplit rewards
    /// melee→Charred. Hybrid players who chain "Burning expires →
    /// Charred residue → walk-up melee" get bonuses at every step.</para>
    ///
    /// <para><b>Mechanic:</b> on every melee swing that lands damage,
    /// if the defender has <see cref="CharredEffect"/>, deals
    /// <see cref="CHARSPLIT_BONUS_PERCENT"/>% of <c>actualDamage</c>
    /// as extra Heat damage (floor 1).</para>
    /// </summary>
    public class Pyromancy_Charsplit : BaseSkillPart
    {
        public override string Name => nameof(Pyromancy_Charsplit);

        public const int CHARSPLIT_BONUS_PERCENT = 50;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx == null || ctx.Defender == null) return;
            if (ctx.ActualDamage <= 0) return;

            var effects = ctx.Defender.GetPart<StatusEffectsPart>();
            if (effects == null || !effects.HasEffect<CharredEffect>()) return;

            int bonus = (ctx.ActualDamage * CHARSPLIT_BONUS_PERCENT) / 100;
            if (bonus <= 0) bonus = 1;

            var dmg = new Damage(bonus);
            dmg.AddAttribute("Heat");
            CombatSystem.ApplyDamage(ctx.Defender, dmg, ctx.Attacker, ctx.Zone);
        }
    }
}
