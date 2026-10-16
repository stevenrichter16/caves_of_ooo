using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    public abstract class ThrowingSkillPart : BaseSkillPart
    {
        public override bool AddSkill(Entity entity) { ThrowingRules.EnsureState(entity); return true; }
        public override bool RemoveSkill(Entity entity)
        {
            if (this is Throwing_MeasuredThrow) entity.GetPart<ThrowingStatePart>()?.ClearPreparation();
            return true;
        }
    }
    public sealed class ThrowingSkill : ThrowingSkillPart
    {
        public override string Name => nameof(ThrowingSkill);
        public override string DescribeRank(int rank) => "One retry after a physical throw's accuracy miss. Payloads do not roll physical accuracy.";
    }
    public sealed class Throwing_MeasuredThrow : ThrowingSkillPart
    {
        public override string Name => nameof(Throwing_MeasuredThrow);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor) => new ActivatedAbilitySpec
        { DisplayName = "Measured Throw", Command = "CommandMeasuredThrow", Class = "Skills", TargetingMode = AbilityTargetingMode.SelfCentered, Cooldown = 6 };
        public override string DescribeRank(int rank) => $"Spend one action preparing an exact physical item and target. Its next-action throw gains +{ThrowingRules.MeasuredPenetrationBonus} penetration and cap against that target. Moving or another action interrupts. Cooldown 6.";
    }
    public sealed class Throwing_DrivingThrow : ThrowingSkillPart
    {
        public override string Name => nameof(Throwing_DrivingThrow);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor) => new ActivatedAbilitySpec
        { DisplayName = "Driving Throw", Command = "CommandDrivingThrow", Class = "Skills", TargetingMode = AbilityTargetingMode.SelfCentered, Cooldown = 12 };
        public override string DescribeRank(int rank) => "Throw a physical item of weight 8 or more. Positive damage can push the same surviving creature one cell outward. Braces and obstacles resist; the item stays at impact. Cooldown 12.";
    }
    public sealed class Throwing_RecoveringStep : ThrowingSkillPart
    {
        public override string Name => nameof(Throwing_RecoveringStep);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor) => new ActivatedAbilitySpec
        { DisplayName = "Recovering Step", Command = "CommandRecoveringStep", Class = "Skills", TargetingMode = AbilityTargetingMode.SelfCentered, Cooldown = 6 };
        public override string DescribeRank(int rank) => "Step onto one adjacent item you threw and secure that exact unit without equipping. Entry hazards still happen; a completed step costs the action even if collection fails. Cooldown 6.";
    }
    public sealed class Throwing_FocusedSplash : ThrowingSkillPart
    {
        public override string Name => nameof(Throwing_FocusedSplash);
        public override string DescribeRank(int rank) => "Optionally narrow a tonic's initial splash to one cell. No extra dose or matter; later reactions can spread. Broad is the default. Gas grenades are unchanged.";
    }
}
