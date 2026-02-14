namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PickupCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Pickup";

        public PickupCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Pickup requires a valid actor.");
            }

            if (context.Zone == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidZone,
                    "Pickup requires a valid zone.");
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
                    "Pickup requires a valid item.");
            }

            var physics = _item.GetPart<PhysicsPart>();
            if (physics == null || !physics.Takeable)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotTakeable,
                    "Item is not takeable.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var zone = context.Zone;
            var inventory = context.Inventory;

            // Fire BeforePickup on actor.
            var beforePickup = GameEvent.New("BeforePickup");
            beforePickup.SetParameter("Actor", (object)actor);
            beforePickup.SetParameter("Item", (object)_item);
            if (!actor.FireEvent(beforePickup))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Pickup was cancelled.");
            }

            // Fire BeforeBeingPickedUp on item.
            var beforeBeing = GameEvent.New("BeforeBeingPickedUp");
            beforeBeing.SetParameter("Actor", (object)actor);
            beforeBeing.SetParameter("Item", (object)_item);
            if (!_item.FireEvent(beforeBeing))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item pickup was cancelled.");
            }

            var originalCell = zone.GetEntityCell(_item);
            bool hadZonePosition = originalCell != null;
            int originalX = hadZonePosition ? originalCell.X : -1;
            int originalY = hadZonePosition ? originalCell.Y : -1;

            zone.RemoveEntity(_item);
            transaction.Do(
                apply: null,
                undo: () =>
                {
                    if (hadZonePosition)
                        zone.AddEntity(_item, originalX, originalY);
                });

            if (!inventory.AddObject(_item))
            {
                MessageLog.Add($"You can't carry {_item.GetDisplayName()}: too heavy!");
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Weight limit exceeded.");
            }

            transaction.Do(
                apply: null,
                undo: () => inventory.RemoveObject(_item));

            MessageLog.Add($"{actor.GetDisplayName()} picks up {_item.GetDisplayName()}.");

            // Auto-equip preserves previous behavior and now participates in rollback.
            bool autoEquipped = InventorySystem.AutoEquip(actor, _item);
            if (autoEquipped)
            {
                transaction.Do(
                    apply: null,
                    undo: () => InventorySystem.UnequipItem(actor, _item));
            }

            // Fire AfterPickup on actor.
            var afterPickup = GameEvent.New("AfterPickup");
            afterPickup.SetParameter("Actor", (object)actor);
            afterPickup.SetParameter("Item", (object)_item);
            actor.FireEvent(afterPickup);

            return InventoryCommandResult.Ok();
        }
    }
}
