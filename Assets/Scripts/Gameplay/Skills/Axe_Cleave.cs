using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class on-hit cleave power. Self-contained per WSP3.3.
    ///
    /// <para><b>Mechanic:</b> on every Axe-attribute hit,
    /// <see cref="CHANCE_PERCENT"/> chance to find the first adjacent
    /// Creature to the defender (in direction-iteration order
    /// N → NE → E → SE → S → SW → W → NW) and deal half the original
    /// damage to it. Direction-iteration is deterministic so seeded
    /// tests can pin the cleave victim.</para>
    ///
    /// <para>Stacks independently of <see cref="AxeSkill"/>'s
    /// force-cleave-on-crit hook — both can fire on the same Critical
    /// Axe hit (gated cleave + force cleave both find the same first
    /// adjacent target; the second cleave attempt damages the same
    /// neighbor again).</para>
    /// </summary>
    public class Axe_Cleave : BaseSkillPart
    {
        public override string Name => nameof(Axe_Cleave);

        public const int CHANCE_PERCENT = 30;

        public override void OnAttackerAfterAttack(SkillEventContext ctx)
        {
            if (ctx?.Damage == null || !ctx.Damage.HasAttribute("Axe")) return;
            if (ctx.ActualDamage <= 0) return;
            if (ctx.Rng == null || ctx.Zone == null) return;

            // Chance roll FIRST (symmetry with Bludgeon/Lacerate/Jab/Bloodletter
            // helpers — see WSP.4b cold-eye finding for the rationale).
            if (ctx.Rng.Next(100) >= CHANCE_PERCENT) return;

            SkillCombatHelpers.ExecuteCleave(
                ctx.ActualDamage, ctx.Defender, ctx.Attacker, ctx.Zone);
        }
    }
}
