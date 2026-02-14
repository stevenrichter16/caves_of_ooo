using System.Collections.Generic;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Rules
{
    /// <summary>
    /// Resolves concrete body parts to claim for this equip attempt.
    /// </summary>
    public sealed class SlotAvailabilityRule : IEquipRule
    {
        public EquipRuleResult Evaluate(EquipPlanBuilder builder)
        {
            var plan = builder?.Plan;
            if (plan == null)
                return EquipRuleResult.Fail("No equip plan.");

            plan.ClaimedParts.Clear();

            var remainingSlots = new List<string>(plan.SlotTypes);

            if (plan.TargetBodyPart != null)
            {
                if (!builder.ClaimPart(plan.TargetBodyPart))
                {
                    return EquipRuleResult.Fail(
                        $"Could not claim target slot {plan.TargetBodyPart.GetDisplayName()}.");
                }

                // Consume exactly one requirement for the targeted slot type.
                bool consumed = false;
                for (int i = 0; i < remainingSlots.Count; i++)
                {
                    if (remainingSlots[i] == plan.TargetBodyPart.Type)
                    {
                        remainingSlots.RemoveAt(i);
                        consumed = true;
                        break;
                    }
                }

                if (!consumed)
                {
                    return EquipRuleResult.Fail(
                        $"{plan.Item.GetDisplayName()} cannot be equipped to {plan.TargetBodyPart.GetDisplayName()}.");
                }
            }

            for (int i = 0; i < remainingSlots.Count; i++)
            {
                string slotType = remainingSlots[i];
                var slot = builder.FindBestSlot(slotType);
                if (slot == null)
                {
                    return EquipRuleResult.Fail(
                        $"No available {slotType} slot for {plan.Item.GetDisplayName()}.");
                }

                builder.ClaimPart(slot);
            }

            return EquipRuleResult.Pass();
        }
    }
}
