using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Long-Blade-class on-hit Bleed power. <b>Identity-only stub</b> —
    /// behavior lives in <see cref="OnHitSkillEffects.Apply"/>, which
    /// rolls a <see cref="OnHitSkillEffects.LONGBLADES_LACERATE_CHANCE_PERCENT"/>
    /// chance to apply <see cref="BleedingEffect"/> with stronger damage
    /// dice (<see cref="OnHitSkillEffects.LONGBLADES_LACERATE_DAMAGE_DICE"/>)
    /// than the universal Cutting class hook ("1d2").
    ///
    /// <para>Stacks ON TOP of the OnHitClassEffects 25% Cutting→Bleed
    /// roll: a longsword swing can apply two Bleeding effects on the
    /// same hit. <c>BleedingEffect</c>'s OnStack semantics determine
    /// how that combines (extends duration / refreshes / picks higher
    /// dice); the per-effect detail is inside that class.</para>
    /// </summary>
    public class LongBlades_Lacerate : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Lacerate);
    }
}
