using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// Axe-class on-hit cleave power. <b>Identity-only stub</b> — the
    /// actual cleave logic lives in <see cref="OnHitSkillEffects.Apply"/>,
    /// which checks <c>SkillsPart.HasSkill(nameof(Axe_Cleave))</c> and
    /// rolls a <see cref="OnHitSkillEffects.AXE_CLEAVE_CHANCE_PERCENT"/>
    /// chance to deal half the original hit's damage to one Creature
    /// entity adjacent to the defender (excluding the attacker
    /// themselves).
    ///
    /// <para>Cleave selection: deterministic by direction-iteration
    /// order (N → NE → E → SE → S → SW → W → NW), so seeded RNG
    /// tests still pin the chosen target. The first adjacent Creature
    /// in that order is the cleave victim. If no adjacent Creature
    /// exists, the cleave roll succeeds but no damage is applied
    /// (silent no-op — matches Qud's "cleave with no target" path).</para>
    /// </summary>
    public class Axe_Cleave : BaseSkillPart
    {
        public override string Name => nameof(Axe_Cleave);
    }
}
