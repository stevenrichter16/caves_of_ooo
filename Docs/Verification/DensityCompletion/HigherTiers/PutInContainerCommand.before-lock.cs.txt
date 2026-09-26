namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PutInContainerCommand : IInventoryCommand
    {
        private readonly Entity _container;
        private readonly Entity _item;

        public string Name => "PutInContainer";

        public PutInContainerCommand(Entity container, Entity item)
        {
            _container = container;
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Container put requires a valid actor.");
            }

            if (context.Inventory == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingInventoryPart,
                    "Actor is missing InventoryPart.");
            }

            if (_container == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Container is null.");
            }

            if (_container.GetPart<ContainerPart>() == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Entity is not a container.");
            }

            if (_item == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Item is null.");
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
            var containerPart = _container.GetPart<ContainerPart>();
            var inventory = context.Inventory;
            if (!transaction.TryClaim(_item, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Item transfer is already in progress.");
            if (containerPart == null || inventory == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Container transfer prerequisites are missing.");
            }

            if (containerPart.Locked)
            {
                MessageLog.Add($"The {_container.GetDisplayName()} is locked.");
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Container is locked.");
            }

            var equippedState = UnequipCommand.CaptureEquippedState(context, _item);
            if (equippedState.HasLocation)
            {
                var unequipResult = new UnequipCommand(_item).Execute(context, transaction);
                if (!unequipResult.Success)
                {
                    return InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed,
                        "Unable to unequip item before container transfer.");
                }
            }

            string itemName = _item.GetDisplayName();
            int quantity = _item.GetPart<StackerPart>()?.StackCount ?? 1;
            var source = InventoryTransferSnapshot.Capture(inventory);
            transaction.Do(apply: null, undo: source.Restore);
            if (!source.Apply(() => inventory.RemoveObject(_item)))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not in inventory.");
            }

            var destination = InventoryTransferSnapshot.Capture(containerPart, _item);
            transaction.Do(apply: null, undo: destination.Restore);
            if (!destination.Apply(() => containerPart.AddItem(_item)))
            {
                MessageLog.Add($"The {_container.GetDisplayName()} is full.");
                DispositionDiagnostics.Record(context, _item, Name, quantity, _container.ID, "container_full");
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Container is full.");
            }

            if (!destination.ClaimChanges(transaction, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "A destination stack is already being transferred.");
            MessageLog.Add($"You put {itemName} {containerPart.Preposition} the {_container.GetDisplayName()}.");
            DispositionDiagnostics.Record(context, _item, Name, quantity, _container.ID);
            return InventoryCommandResult.Ok();
        }
    }
}
