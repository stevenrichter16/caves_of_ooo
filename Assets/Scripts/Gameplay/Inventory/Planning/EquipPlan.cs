using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Planning
{
    /// <summary>
    /// Immutable context + mutable planning output for one equip attempt.
    /// </summary>
    public sealed class EquipPlan
    {
        public Entity Actor { get; }

        public Entity Item { get; }

        public Body Body { get; }

        public EquippablePart Equippable { get; }

        public BodyPart TargetBodyPart { get; }

        public List<string> SlotTypes { get; }

        public List<BodyPart> ClaimedParts { get; } = new List<BodyPart>();

        public List<InventoryDisplacement> Displacements { get; } = new List<InventoryDisplacement>();

        public bool IsValid { get; private set; } = true;

        public string FailureReason { get; private set; } = string.Empty;

        public EquipPlan(Entity actor, Entity item, Body body, EquippablePart equippable, BodyPart targetBodyPart)
        {
            Actor = actor;
            Item = item;
            Body = body;
            Equippable = equippable;
            TargetBodyPart = targetBodyPart;
            SlotTypes = BuildSlotTypes(equippable);
        }

        public void Invalidate(string failureReason)
        {
            if (!IsValid)
                return;

            IsValid = false;
            FailureReason = string.IsNullOrEmpty(failureReason) ? "Equip plan failed." : failureReason;
        }

        private static List<string> BuildSlotTypes(EquippablePart equippable)
        {
            var result = new List<string>();
            if (equippable == null)
                return result;

            string[] slotArray = equippable.GetSlotArray();
            for (int i = 0; i < slotArray.Length; i++)
            {
                string slot = slotArray[i]?.Trim();
                if (!string.IsNullOrEmpty(slot))
                    result.Add(slot);
            }

            return result;
        }
    }
}
