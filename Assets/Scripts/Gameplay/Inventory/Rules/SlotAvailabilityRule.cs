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

            if (plan.ExactParts != null)
            {
                if (plan.ExactParts.Count != remainingSlots.Count || plan.ExactParts.Count == 0
                    || plan.TargetBodyPart != null && plan.ExactParts[0] != plan.TargetBodyPart)
                    return EquipRuleResult.Fail("The saved equipment slots no longer match this item.");
                foreach (var part in plan.ExactParts)
                {
                    if (part == null || part.Abstract || !builder.GetCandidateSlots(part.Type).Contains(part)
                        || !remainingSlots.Remove(part.Type) || !builder.ClaimPart(part))
                        return EquipRuleResult.Fail("A saved equipment slot is unavailable or incompatible.");
                }
                return EquipRuleResult.Pass();
            }

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
