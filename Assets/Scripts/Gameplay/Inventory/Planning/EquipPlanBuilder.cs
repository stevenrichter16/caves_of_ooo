using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Planning
{
    /// <summary>
    /// Mutable helper API used by equip rules to build one plan.
    /// </summary>
    public sealed class EquipPlanBuilder
    {
        public EquipPlan Plan { get; }

        public EquipPlanBuilder(EquipPlan plan)
        {
            Plan = plan;
        }

        public List<BodyPart> GetCandidateSlots(string slotType)
        {
            if (Plan?.Body == null || string.IsNullOrEmpty(slotType))
                return new List<BodyPart>();

            return Plan.Body.GetEquippableSlots(slotType);
        }

        public int CountRequiredSlots(string slotType)
        {
            if (Plan == null || string.IsNullOrEmpty(slotType))
                return 0;

            int count = 0;
            for (int i = 0; i < Plan.SlotTypes.Count; i++)
            {
                if (Plan.SlotTypes[i] == slotType)
                    count++;
            }
            return count;
        }

        public bool HasRequiredSlotType(string slotType)
        {
            return CountRequiredSlots(slotType) > 0;
        }

        public bool IsClaimed(BodyPart bodyPart)
        {
            if (bodyPart == null || Plan == null)
                return false;
            return Plan.ClaimedParts.Contains(bodyPart);
        }

        public bool ClaimPart(BodyPart bodyPart)
        {
            if (bodyPart == null || Plan == null)
                return false;

            if (Plan.ClaimedParts.Contains(bodyPart))
                return false;

            Plan.ClaimedParts.Add(bodyPart);
            return true;
        }

        /// <summary>
        /// Prefer free slot, then occupied slot; always skip already claimed parts.
        /// </summary>
        public BodyPart FindBestSlot(string slotType)
        {
            var candidates = GetCandidateSlots(slotType);

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate._Equipped == null && !IsClaimed(candidate))
                    return candidate;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (!IsClaimed(candidate))
                    return candidate;
            }

            return null;
        }

        public void AddDisplacementsFromClaimedParts()
        {
            if (Plan == null || Plan.Body == null)
                return;

            Plan.Displacements.Clear();

            var displacedItems = new HashSet<Entity>();
            for (int i = 0; i < Plan.ClaimedParts.Count; i++)
            {
                var claimedPart = Plan.ClaimedParts[i];
                var existing = claimedPart?._Equipped;
                if (existing != null && existing != Plan.Item)
                    displacedItems.Add(existing);
            }

            if (displacedItems.Count == 0)
                return;

            var allParts = Plan.Body.GetParts();
            foreach (var displacedItem in displacedItems)
            {
                for (int i = 0; i < allParts.Count; i++)
                {
                    if (allParts[i]._Equipped == displacedItem)
                    {
                        Plan.Displacements.Add(new InventoryDisplacement
                        {
                            Item = displacedItem,
                            BodyPart = allParts[i]
                        });
                    }
                }
            }
        }
    }
}
