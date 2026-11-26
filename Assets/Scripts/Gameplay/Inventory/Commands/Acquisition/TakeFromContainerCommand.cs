namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class TakeFromContainerCommand : IInventoryCommand
    {
        private readonly Entity _container;
        private readonly Entity _item;
        private readonly ContainedLiquidSourcePart.RecoveryPermission _wetRecovery;

        public string Name => "TakeFromContainer";

        public TakeFromContainerCommand(Entity container, Entity item)
        {
            _container = container;
            _item = item;
        }

        internal TakeFromContainerCommand(Entity container, Entity item, ContainedLiquidSourcePart.RecoveryPermission permission)
            : this(container, item) { _wetRecovery = permission; }

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

            if (LocalPorterReservationPart.Blocks(_container) || LocalPorterReservationPart.Blocks(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "That box is reserved for a delivery.");
            var contained = _container.GetPart<ContainedLiquidSourcePart>();
            if (contained != null && !contained.CanTake(context.Actor, _item, context.Zone ?? context.Actor.SpatialZone,
                _wetRecovery, _wetRecovery?.Transaction))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule,
                    "Drain the tray first, or explicitly recover through the liquid.");

            var delivery = _container.GetPart<DeliveryStoragePart>();
            if (delivery != null && !delivery.CanWithdraw(context.Actor, _item, context.Zone ?? context.Actor.SpatialZone))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule,
                    "The contents are unavailable. Check that the storage is beside you and the stock is intact.");

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

            if (CarriedStorage.UsesCarriedContents(_item))
            {
                var container = _container.GetPart<ContainerPart>();
                if (!StorageSourceCurrent(context, container, physics))
                    return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule,
                        "That storage is no longer available in the nearby container.");
                if (!CarriedStorage.CanAcceptNewLoad(context.Inventory, _item))
                    return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule,
                        "The storage and its contents would put you over your carry limit.");
            }
            else
            {
                int requiredStrength = HandlingService.GetLiftStrengthRequirement(_item);
                int strength = context.Actor.GetStatValue("Strength", 0);
                if (strength < requiredStrength)
                    return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InsufficientStrength,
                        $"You can't carry {_item.GetDisplayName()}: it requires Strength {requiredStrength}.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var containerPart = _container.GetPart<ContainerPart>();
            var inventory = context.Inventory;
            bool storageVessel = CarriedStorage.UsesCarriedContents(_item);
            System.Action containedCleanup = null, containedGuard = null;
            var contained = _container.GetPart<ContainedLiquidSourcePart>();
            if (contained != null && !contained.PrepareTake(context.Actor, _item, context.Zone ?? context.Actor.SpatialZone, transaction, _wetRecovery, out containedCleanup, out containedGuard))
                return Refuse(context, "contained_recovery_refused", "Drain the tray first, or explicitly recover through the liquid.");
            var delivery = _container.GetPart<DeliveryStoragePart>();
            if ((delivery != null || storageVessel) && !transaction.TryClaim(_container, context.Actor, Name))
                return Refuse(context, "delivery_storage_busy", "That storage is already in use.");
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

            var physics = _item.GetPart<PhysicsPart>();
            if (storageVessel && (!StorageSourceCurrent(context, containerPart, physics)
                || physics?.Takeable != true || !HandlingService.IsCarryable(_item)))
                return Refuse(context, "invalid_container_source", "That storage is no longer available in the nearby container.");
            if (storageVessel && !CarriedStorage.CanAcceptNewLoad(inventory, _item))
                return Refuse(context, "weight_limit", "The storage and its contents would put you over your carry limit.");

            if (!storageVessel && !HandlingService.CanLift(context.Actor, _item, out string liftFailure))
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
            if (delivery?.WatchLooseTransfer(context.Actor, context.Zone ?? context.Actor.SpatialZone, transaction, -quantity) == false)
                return Refuse(context, "delivery_storage_changed", "The tray's stock or access has changed.");
            bool gold = _item.BlueprintName == "GoldCoin";
            if (gold && !ExactContainedSource(containerPart, inventory, physics))
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
                if (storageVessel)
                    transaction.BeforeCommit(() => StorageContainerCurrent(context, containerPart)
                        && _item.GetPart<PhysicsPart>() == physics && !containerPart.Contents.Contains(_item)
                        && WorldResourceActions.Carried(context.Actor, _item) && CarriedStorage.FitsCombinedLoad(inventory, 0));
            }

            // Fire item-side Taken AFTER successful acquisition — same contract as
            // PickupCommand so world-object quest Parts react to acquisition
            // from a container/corpse, not just the ground. Gold is already spent
            // and its purse credit is still deferred during these callbacks.
            if (contained != null)
                transaction.Do(null, containedCleanup);
            containedGuard?.Invoke();
            if (delivery != null)
                delivery.RecordWithdrawal(transaction);
            if (delivery != null || storageVessel)
                transaction.AfterCommit(Announce);
            else Announce();
            LocalGatheringClaims.RecordTake(gathering, transaction);
            ConnectedSpreadProgress.RecordTransfer(progress, transaction);
            return InventoryCommandResult.Ok();

            void Announce()
            {
                var taken = GameEvent.New("Taken");
                taken.SetParameter("Actor", (object)context.Actor);
                taken.SetParameter("Item", (object)_item);
                _item.FireEventAndRelease(taken);
                MessageLog.Add(gold ? $"You pocket {quantity} gold ({credit} drams) from the {_container.GetDisplayName()}."
                    : $"You take {itemName} from the {_container.GetDisplayName()}.");
                AcquisitionDiagnostics.Record(context, _item, Name, _container.ID, quantity);
            }
        }
        private bool StorageContainerCurrent(InventoryContext context, ContainerPart source)
        {
            var actor = context.Actor;
            var zone = context.Zone ?? actor.SpatialZone;
            var manager = WorldLocationContext.For(zone);
            return source?.ParentEntity == _container && _container.GetPart<ContainerPart>() == source
                && source.Contents != null && !source.IsLocked && !LocalPorterReservationPart.Blocks(_container)
                && actor.GetPart<InventoryPart>() == context.Inventory
                && PreparationActions.CurrentActor(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && (manager == null || manager.ActiveZone == zone)
                && WorldResourceActions.Nearby(actor, _container, zone);
        }

        private bool StorageSourceCurrent(InventoryContext context, ContainerPart source, PhysicsPart physics)
            => StorageContainerCurrent(context, source) && source.Contents.Contains(_item)
                && !LocalPorterReservationPart.Blocks(_item) && ExactContainedSource(source, context.Inventory, physics);

        private bool ExactContainedSource(ContainerPart source, InventoryPart destination, PhysicsPart physics)
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
