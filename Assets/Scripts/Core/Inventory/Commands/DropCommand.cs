namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class DropCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Drop";

        public DropCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Drop requires a valid actor.");
            }

            if (context.Zone == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidZone,
                    "Drop requires a valid zone.");
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
                    "Drop requires a valid item.");
            }

            if (!context.Inventory.Contains(_item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotOwned,
                    "Actor does not own this item.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var zone = context.Zone;
            var inventory = context.Inventory;

            // If equipped, unequip first and register rollback to re-equip.
            bool wasEquipped = InventorySystem.IsEquipped(actor, _item);
            if (wasEquipped)
            {
                var unequipResult = new UnequipCommand(_item).Execute(context, transaction);
                if (!unequipResult.Success)
                {
                    return InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed,
                        "Unable to unequip item before drop.");
                }
            }

            // Fire BeforeDrop.
            var beforeDrop = GameEvent.New("BeforeDrop");
            beforeDrop.SetParameter("Actor", (object)actor);
            beforeDrop.SetParameter("Item", (object)_item);
            if (!actor.FireEvent(beforeDrop))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Drop was cancelled.");
            }

            if (!inventory.RemoveObject(_item))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not in inventory.");
            }

            transaction.Do(
                apply: null,
                undo: () => inventory.AddObject(_item));

            var cell = zone.GetEntityCell(actor);
            if (cell != null)
            {
                zone.AddEntity(_item, cell.X, cell.Y);
                transaction.Do(
                    apply: null,
                    undo: () => zone.RemoveEntity(_item));
            }

            MessageLog.Add($"{actor.GetDisplayName()} drops {_item.GetDisplayName()}.");

            // Fire AfterDrop.
            var afterDrop = GameEvent.New("AfterDrop");
            afterDrop.SetParameter("Actor", (object)actor);
            afterDrop.SetParameter("Item", (object)_item);
            actor.FireEvent(afterDrop);

            return InventoryCommandResult.Ok();
        }
    }
}
