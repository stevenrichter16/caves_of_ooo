using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class UnequipCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Unequip";

        public UnequipCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Unequip requires a valid actor.");
            }

            if (context.Inventory == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingInventoryPart,
                    "Actor is missing InventoryPart.");
            }

            if (_item == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Unequip requires a valid item.");
            }

            if (!InventorySystem.IsEquipped(context.Actor, _item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Item is not currently equipped.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var inventory = context.Inventory;

            BodyPart rollbackBodyPart = inventory.FindEquippedBodyPart(_item);
            string rollbackLegacySlot = inventory.FindEquippedSlot(_item);

            var equippable = _item.GetPart<EquippablePart>();

            // Fire BeforeUnequip on actor (can veto).
            var beforeUnequip = GameEvent.New("BeforeUnequip");
            beforeUnequip.SetParameter("Actor", (object)actor);
            beforeUnequip.SetParameter("Item", (object)_item);
            if (!actor.FireEvent(beforeUnequip))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unequip was cancelled.");
            }

            // Remove equip stat bonuses before detaching item.
            if (equippable != null)
                EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: false);

            bool unequipped = false;
            var body = context.Body;
            if (body != null)
            {
                var bodyPart = rollbackBodyPart;
                if (bodyPart == null)
                {
                    var parts = body.GetParts();
                    for (int i = 0; i < parts.Count; i++)
                    {
                        if (parts[i]._Equipped == _item)
                        {
                            bodyPart = parts[i];
                            break;
                        }
                    }
                }

                if (bodyPart != null)
                    unequipped = inventory.UnequipFromBodyPart(bodyPart);
            }
            else if (!string.IsNullOrEmpty(rollbackLegacySlot))
            {
                unequipped = inventory.Unequip(rollbackLegacySlot);
            }

            if (!unequipped)
            {
                // Best-effort rollback of bonus removal.
                if (equippable != null)
                    EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: true);

                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unequip failed.");
            }

            MessageLog.Add($"{actor.GetDisplayName()} unequips {_item.GetDisplayName()}.");

            // Fire AfterUnequip on actor.
            var afterUnequip = GameEvent.New("AfterUnequip");
            afterUnequip.SetParameter("Actor", (object)actor);
            afterUnequip.SetParameter("Item", (object)_item);
            actor.FireEvent(afterUnequip);

            transaction.Do(
                apply: null,
                undo: () =>
                {
                    if (rollbackBodyPart != null)
                        InventorySystem.Equip(actor, _item, rollbackBodyPart);
                    else if (!string.IsNullOrEmpty(rollbackLegacySlot))
                        InventorySystem.Equip(actor, _item);
                });

            return InventoryCommandResult.Ok();
        }
    }
}
