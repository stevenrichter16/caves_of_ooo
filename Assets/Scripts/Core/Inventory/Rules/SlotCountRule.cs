using System.Collections.Generic;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    /// <summary>
    /// Validates that the actor has enough slots of each required type.
    /// </summary>
    public sealed class SlotCountRule : IEquipRule
    {
        public EquipRuleResult Evaluate(EquipPlanBuilder builder)
        {
            var plan = builder?.Plan;
            if (plan == null)
                return EquipRuleResult.Fail("No equip plan.");

            if (plan.SlotTypes.Count == 0)
            {
                return EquipRuleResult.Fail(
                    $"{plan.Item.GetDisplayName()} has no valid equip slots.");
            }

            var requiredCounts = new Dictionary<string, int>();
            for (int i = 0; i < plan.SlotTypes.Count; i++)
            {
                string slotType = plan.SlotTypes[i];
                if (!requiredCounts.ContainsKey(slotType))
                    requiredCounts[slotType] = 0;
                requiredCounts[slotType]++;
            }

            foreach (var kvp in requiredCounts)
            {
                string slotType = kvp.Key;
                int required = kvp.Value;
                int available = builder.GetCandidateSlots(slotType).Count;

                if (available < required)
                {
                    return EquipRuleResult.Fail(
                        $"No available {slotType} slot for {plan.Item.GetDisplayName()}.");
                }
            }

            return EquipRuleResult.Pass();
        }
    }
}
