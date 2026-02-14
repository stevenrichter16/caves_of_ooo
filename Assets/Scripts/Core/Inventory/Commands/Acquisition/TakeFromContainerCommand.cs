namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class TakeFromContainerCommand : IInventoryCommand
    {
        private readonly Entity _container;
        private readonly Entity _item;

        public string Name => "TakeFromContainer";

        public TakeFromContainerCommand(Entity container, Entity item)
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
                    "Container take requires a valid actor.");
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

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var containerPart = _container.GetPart<ContainerPart>();
            var inventory = context.Inventory;
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

            if (!containerPart.RemoveItem(_item))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not in the container.");
            }

            transaction.Do(
                apply: null,
                undo: () => containerPart.AddItem(_item));

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

            MessageLog.Add($"You take {_item.GetDisplayName()} from the {_container.GetDisplayName()}.");
            return InventoryCommandResult.Ok();
        }
    }
}
