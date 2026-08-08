using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// BIOME-OVERHAUL B3 — tree-root marker for the Persuasion skill
    /// tree (AcrobaticsSkill's empty-marker shape). Owning this part
    /// means the actor has "the Persuasion tree", unlocking purchase of
    /// its powers (Recruit, Dismiss). The follower stack itself
    /// (RecruitedEffect, FollowLeaderGoal, companion limits, party
    /// zone-transit) shipped complete long before this tree — the tree
    /// JSON was the single missing link that kept followers at zero.
    /// </summary>
    public class PersuasionSkill : BaseSkillPart
    {
        public override string Name => nameof(PersuasionSkill);
    }
}
