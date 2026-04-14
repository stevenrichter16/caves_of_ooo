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

            var physics = _item.GetPart<PhysicsPart>();
            if (physics == null || !physics.Takeable)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotTakeable,
                    "Item is not takeable.");
            }

            var handling = _item.GetPart<HandlingPart>();
            if (handling != null && !handling.Carryable)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotCarryable,
                    $"{_item.GetDisplayName()} cannot be carried.");
            }

            int requiredStrength = HandlingService.GetLiftStrengthRequirement(_item);
            int strength = context.Actor.GetStatValue("Strength", 0);
            if (strength < requiredStrength)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InsufficientStrength,
                    $"You can't carry {_item.GetDisplayName()}: it requires Strength {requiredStrength}.");
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

            if (!HandlingService.CanLift(context.Actor, _item, out string liftFailure))
            {
                MessageLog.Add(liftFailure);
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    liftFailure);
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
