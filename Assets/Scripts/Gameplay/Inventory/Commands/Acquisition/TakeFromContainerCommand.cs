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

            if (containerPart.IsLocked)
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
            var stacker = _item.GetPart<StackerPart>();
            int quantity = stacker?.StackCount ?? 1;
            bool gold = _item.BlueprintName == "GoldCoin";
            var physics = _item.GetPart<PhysicsPart>();
            if (gold && !CoinSourceCurrent(containerPart, inventory, physics))
                return Refuse(context, "invalid_coin_source", "The coin's container ownership is no longer valid.");
            long credit = gold ? (long)quantity * 5 : 0;
            if (gold && (long)TradeSystem.GetDrams(context.Actor) + credit > int.MaxValue)
                return Refuse(context, "currency_overflow", "You cannot carry that much currency.");
            var gathering = LocalGatheringClaims.CaptureTake(context.Actor, _item, context.Zone, _container);
            var progress = ConnectedSpreadProgress.CaptureTransfer(context.Actor, _item, context.Zone, _container);
            var source = InventoryTransferSnapshot.Capture(containerPart);
            transaction.Do(apply: null, undo: source.Restore);
            if (!source.Apply(() => containerPart.RemoveItem(_item)))
                return Refuse(context, "removal_refused", "Item is not in the container.");
            if (gold)
            {
                // Match ordinary ground acquisition: spend the exact source now,
                // but publish currency only when every outer callback commits.
                transaction.Do(
                    apply: () => { if (stacker != null) stacker.StackCount = 0; },
                    undo: () => { if (stacker != null) stacker.StackCount = quantity; });
                transaction.DeferCurrencyCredit(context.Actor, (int)credit);
                // Taken callbacks may mutate raw Parts or reinsert the owner. A
                // credit can commit only while this exact source remains spent.
                transaction.BeforeCommit(() => _container.GetPart<ContainerPart>() == containerPart
                    && context.Actor.GetPart<InventoryPart>() == inventory
                    && _item.BlueprintName == "GoldCoin" && _item.GetPart<PhysicsPart>() == physics
                    && physics.ParentEntity == _item && physics.InInventory == null && physics.Equipped == null
                    && _item.SpatialZone == null && _item.GetPart<StackerPart>() == stacker
                    && (stacker == null || stacker.ParentEntity == _item && stacker.StackCount == 0)
                    && !containerPart.Contents.Contains(_item) && !inventory.Objects.Contains(_item)
                    && !inventory.EquippedItems.ContainsValue(_item) && inventory.FindEquippedBodyPart(_item) == null);
            }
            else
            {
                var destination = InventoryTransferSnapshot.Capture(inventory, _item);
                transaction.Do(apply: null, undo: destination.Restore);
                if (!destination.Apply(() => inventory.AddObject(_item)))
                {
                    MessageLog.Add($"You can't carry {itemName}: too heavy!");
                    return Refuse(context, "weight_limit", "Weight limit exceeded.");
                }
                if (!destination.ClaimChanges(transaction, context.Actor, Name))
                    return Refuse(context, "destination_in_progress", "A destination stack is already being transferred.");
            }

            // Fire item-side Taken AFTER successful acquisition — same contract as
            // PickupCommand so world-object quest Parts react to acquisition
            // from a container/corpse, not just the ground. Gold is already spent
            // and its purse credit is still deferred during these callbacks.
            var taken = GameEvent.New("Taken");
            taken.SetParameter("Actor", (object)context.Actor);
            taken.SetParameter("Item", (object)_item);
            _item.FireEventAndRelease(taken);

            MessageLog.Add(gold ? $"You pocket {quantity} gold ({credit} drams) from the {_container.GetDisplayName()}."
                : $"You take {itemName} from the {_container.GetDisplayName()}.");
            AcquisitionDiagnostics.Record(context, _item, Name, _container.ID, quantity);
            LocalGatheringClaims.RecordTake(gathering, transaction);
            ConnectedSpreadProgress.RecordTransfer(progress, transaction);
            return InventoryCommandResult.Ok();
        }
        private bool CoinSourceCurrent(ContainerPart source, InventoryPart destination, PhysicsPart physics)
        {
            if (source.ParentEntity != _container || physics?.ParentEntity != _item
                || physics.InInventory != _container || physics.Equipped != null || _item.SpatialZone != null
                || destination.Objects.Contains(_item) || destination.EquippedItems.ContainsValue(_item)
                || destination.FindEquippedBodyPart(_item) != null || string.IsNullOrEmpty(_item.ID)) return false;
            int matches = 0;
            foreach (var entry in source.Contents)
                if (entry == _item || entry?.ID == _item.ID) matches++;
            return matches == 1;
        }

        private InventoryCommandResult Refuse(InventoryContext context, string reason, string message) =>
            AcquisitionDiagnostics.Refuse(context, _item, Name, _container.ID, reason, message);
    }
}
