using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// H1 — tree-root marker for Hospitality, the sworn host's discipline
    /// (PersuasionSkill's empty-marker shape). It is never for sale: the tree
    /// and its power cost -1 (DISCOVERY) and are taught only by swearing the
    /// Tent-Right oath (<see cref="TentRightOath.Swear"/>), and lost on oathbreak.
    /// </summary>
    public class HospitalitySkill : BaseSkillPart
    {
        public override string Name => nameof(HospitalitySkill);
    }
}
