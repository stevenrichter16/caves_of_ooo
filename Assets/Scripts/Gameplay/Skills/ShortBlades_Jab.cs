using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Piercing-class on-hit Confused power. <b>Identity-only stub</b> —
    /// behavior lives in <see cref="OnHitSkillEffects.Apply"/>.
    ///
    /// <para><b>Mechanic divergence note:</b> The original genre
    /// archetype this skill is named after typically grants extra
    /// off-hand attack attempts (a dual-wielding-specific mechanic).
    /// CoO doesn't have a dual-wielding system in v1, so the skill
    /// is reframed as a status-effect apply that fits CoO's
    /// post-damage hook architecture — same shape as the Cudgel and
    /// LongBlades powers in this ship.</para>
    ///
    /// <para>Effect: rolls
    /// <see cref="OnHitSkillEffects.SHORTBLADES_JAB_CHANCE_PERCENT"/>
    /// chance to apply <see cref="ConfusedEffect"/> for
    /// <see cref="OnHitSkillEffects.SHORTBLADES_JAB_DURATION"/> turns.
    /// Stacks on top of the universal Piercing→Confused class hook
    /// (10%, 2T) — a dagger-trained character disorients harder.</para>
    /// </summary>
    public class ShortBlades_Jab : BaseSkillPart
    {
        public override string Name => nameof(ShortBlades_Jab);
    }
}
