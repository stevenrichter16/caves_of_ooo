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

            if (context.Zone.GetEntityCell(context.Actor) == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Actor has no valid position to drop items.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var zone = context.Zone;
            var inventory = context.Inventory;
            if (!transaction.TryClaim(_item, actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Item transfer is already in progress.");

            // If equipped, unequip first and register rollback to re-equip.
            var equippedState = UnequipCommand.CaptureEquippedState(context, _item);
            if (equippedState.HasLocation)
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
            if (!actor.FireEventAndRelease(beforeDrop))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Drop was cancelled.");
            }

            var cell = zone.GetEntityCell(actor);
            if (cell == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Actor has no valid position to drop items.");
            }

            var source = InventoryTransferSnapshot.Capture(inventory);
            transaction.Do(apply: null, undo: source.Restore);
            if (!source.Apply(() => inventory.RemoveObject(_item)))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not in inventory.");
            }

            if (!zone.AddEntity(_item, cell.X, cell.Y))
            {
                MessageLog.Add("There is no room to drop that here.");
                DispositionDiagnostics.Record(context, _item, Name, _item.GetPart<StackerPart>()?.StackCount ?? 1,
                    zone.ZoneID, "ground_placement_refused");
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,
                    "Ground placement refused.");
            }
            transaction.Do(
                apply: null,
                undo: () => zone.RemoveEntity(_item));

            MessageLog.Add($"{actor.GetDisplayName()} drops {_item.GetDisplayName()}.");

            // Fire AfterDrop.
            var afterDrop = GameEvent.New("AfterDrop");
            afterDrop.SetParameter("Actor", (object)actor);
            afterDrop.SetParameter("Item", (object)_item);
            actor.FireEventAndRelease(afterDrop);

            DispositionDiagnostics.Record(context, _item, Name, _item.GetPart<StackerPart>()?.StackCount ?? 1, zone.ZoneID);

            return InventoryCommandResult.Ok();
        }
    }
}
