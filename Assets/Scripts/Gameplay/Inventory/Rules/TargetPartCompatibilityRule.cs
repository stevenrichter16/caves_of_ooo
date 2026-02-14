using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    /// <summary>
    /// Validates that a targeted body part belongs to the actor and matches
    /// one of the required slot types.
    /// </summary>
    public sealed class TargetPartCompatibilityRule : IEquipRule
    {
        public EquipRuleResult Evaluate(EquipPlanBuilder builder)
        {
            var plan = builder?.Plan;
            if (plan == null)
                return EquipRuleResult.Fail("No equip plan.");

            var target = plan.TargetBodyPart;
            if (target == null)
                return EquipRuleResult.Pass();

            if (target.Abstract)
            {
                return EquipRuleResult.Fail(
                    $"Cannot equip to abstract body part '{target.GetDisplayName()}'.");
            }

            var body = plan.Body;
            if (body == null)
                return EquipRuleResult.Fail("Target body part requires an actor body.");

            bool found = false;
            var parts = body.GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == target)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return EquipRuleResult.Fail(
                    $"Target body part '{target.GetDisplayName()}' is not attached to actor.");
            }

            if (!builder.HasRequiredSlotType(target.Type))
            {
                return EquipRuleResult.Fail(
                    $"{plan.Item.GetDisplayName()} cannot be equipped to {target.GetDisplayName()}.");
            }

            return EquipRuleResult.Pass();
        }
    }
}
