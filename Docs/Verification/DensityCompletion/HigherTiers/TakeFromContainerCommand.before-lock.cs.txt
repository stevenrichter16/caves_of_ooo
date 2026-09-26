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
            if (!transaction.TryClaim(_item, context.Actor, Name))
                return Refuse(context, "transfer_in_progress", "Item transfer is already in progress.");
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

            if (!containerPart.Contents.Contains(_item) || (_item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
                return Refuse(context, "invalid_container_source", "There is no positive unit in that container.");
            string itemName = _item.GetDisplayName();
            int quantity = _item.GetPart<StackerPart>()?.StackCount ?? 1;
            var source = InventoryTransferSnapshot.Capture(containerPart);
            transaction.Do(apply: null, undo: source.Restore);
            if (!source.Apply(() => containerPart.RemoveItem(_item)))
                return Refuse(context, "removal_refused", "Item is not in the container.");
            var destination = InventoryTransferSnapshot.Capture(inventory, _item);
            transaction.Do(apply: null, undo: destination.Restore);
            if (!destination.Apply(() => inventory.AddObject(_item)))
            {
                MessageLog.Add($"You can't carry {itemName}: too heavy!");
                return Refuse(context, "weight_limit", "Weight limit exceeded.");
            }
            if (!destination.ClaimChanges(transaction, context.Actor, Name))
                return Refuse(context, "destination_in_progress", "A destination stack is already being transferred.");

            // Fire item-side Taken AFTER a successful add — same contract as
            // PickupCommand so world-object quest Parts react to acquisition
            // from a container/corpse, not just the ground.
            var taken = GameEvent.New("Taken");
            taken.SetParameter("Actor", (object)context.Actor);
            taken.SetParameter("Item", (object)_item);
            _item.FireEventAndRelease(taken);

            MessageLog.Add($"You take {itemName} from the {_container.GetDisplayName()}.");
            AcquisitionDiagnostics.Record(context, _item, Name, _container.ID, quantity);
            return InventoryCommandResult.Ok();
        }
        private InventoryCommandResult Refuse(InventoryContext context, string reason, string message) =>
            AcquisitionDiagnostics.Refuse(context, _item, Name, _container.ID, reason, message);
    }
}
