namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PutInContainerCommand : IInventoryCommand
    {
        private readonly Entity _container;
        private readonly Entity _item;
        private readonly PublicCargoStoragePart _cargo;
        private readonly ContainerPart _cargoContents;
        private readonly Cell _cargoCell;

        public string Name => "PutInContainer";

        public PutInContainerCommand(Entity container, Entity item)
        {
            _container = container;
            _item = item;
            _cargo = container?.GetPart<PublicCargoStoragePart>();
            if (_cargo == null) return;
            _cargoContents = container.GetPart<ContainerPart>();
            _cargoCell = container.SpatialZone?.GetEntityCell(container);
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

            var destination = _container.GetPart<ContainerPart>();
            if (destination == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Entity is not a container.");
            }

            if (destination.RepairAccessFailure(context.Actor, context.Zone ?? context.Actor.SpatialZone) is string failure)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, failure);

            if (_item == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Item is null.");
            }

            if (LocalPorterReservationPart.Blocks(_container) || LocalPorterReservationPart.Blocks(_item))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "That box is reserved for a delivery.");
            if (_container.HasPart<ContainedLiquidSourcePart>())
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "This recovery tray does not accept deposits.");

            var delivery = _container.GetPart<DeliveryStoragePart>();
            if (delivery?.RefuseDeposit(context.Actor, _item, context.Zone ?? context.Actor.SpatialZone) is string deliveryFailure)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, deliveryFailure);
            if (CargoRefusal(context) is string cargoFailure)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, cargoFailure);

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
            if (CargoRefusal(context) is string cargoFailure)
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, cargoFailure);
            if (_container.HasPart<ContainedLiquidSourcePart>())
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "This recovery tray does not accept deposits.");
            var containerPart = _container.GetPart<ContainerPart>();
            var inventory = context.Inventory;
            var delivery = _container.GetPart<DeliveryStoragePart>();
            var repair = containerPart?.AccessRepair;
            if ((delivery != null || _cargo != null || repair != null) && !transaction.TryClaim(_container, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "That storage is already in use.");
            if (!transaction.TryClaim(_item, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Item transfer is already in progress.");
            if (containerPart == null || inventory == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Container transfer prerequisites are missing.");
            }

            var zone = context.Zone ?? context.Actor.SpatialZone;
            if (containerPart.RepairAccessFailure(context.Actor, zone) is string failure)
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, failure);

            if (containerPart.IsLocked)
            {
                if (CanNarrate(context)) MessageLog.Add($"The {_container.GetDisplayName()} is locked.");
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Container is locked.");
            }

            if (repair != null)
            {
                var contents = containerPart.Contents;
                var pack = inventory.Objects;
                transaction.BeforeCommit(() => containerPart.RepairTransferCurrent(context.Actor, zone, repair, contents)
                    && context.Actor.GetPart<InventoryPart>() == inventory && inventory.Objects == pack);
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
            if (delivery?.WatchLooseTransfer(context.Actor, context.Zone ?? context.Actor.SpatialZone, transaction, quantity) == false)
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The tray's stock or access has changed.");
            if (_cargo?.WatchTransfer(context.Actor, _item, context.Zone ?? context.Actor.SpatialZone, transaction, deposit: true) == false)
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The cargo crate's stock or access has changed.");
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
                if (CanNarrate(context)) MessageLog.Add($"The {_container.GetDisplayName()} is full.");
                DispositionDiagnostics.Record(context, _item, Name, quantity, _container.ID, "container_full");
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Container is full.");
            }

            if (!destination.ClaimChanges(transaction, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "A destination stack is already being transferred.");
            if (_cargo != null || repair != null) destination.WatchChangedOwners(transaction);
            if (delivery != null || _cargo != null || repair != null) transaction.AfterCommit(Announce);
            else Announce();
            return InventoryCommandResult.Ok();

            void Announce()
            {
                if (CanNarrate(context))
                {
                    string subject = context.Actor.HasTag("Player") ? "You put" : context.Actor.GetDisplayName() + " puts";
                    MessageLog.Add($"{subject} {itemName} {containerPart.Preposition} the {_container.GetDisplayName()}.");
                }
                DispositionDiagnostics.Record(context, _item, Name, quantity, _container.ID);
            }
        }

        private string CargoRefusal(InventoryContext context)
        {
            if (!ReferenceEquals(_container?.GetPart<PublicCargoStoragePart>(), _cargo))
                return "The cargo crate has changed. Open the pack again to see its current options.";
            if (_cargo == null) return null;
            var zone = context.Zone ?? context.Actor.SpatialZone;
            if (_cargoContents != _container.GetPart<ContainerPart>() || _cargoCell != zone?.GetEntityCell(_container))
                return "The cargo crate has moved or changed. Choose it again from your pack.";
            return _cargo.RefuseDeposit(context.Actor, _item, zone);
        }

        // NPC feedback follows current visible world ownership, never an invented
        // audible event. Keep player feedback and transfer/rollback semantics intact.
        private bool CanNarrate(InventoryContext context)
        {
            if (context.Actor.HasTag("Player")) return true;
            var zone = context.Zone;
            if (zone == null || context.Actor.SpatialZone != zone || _container.SpatialZone != zone) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager != null && manager.ActiveZone != zone) return false;
            var actorRender = context.Actor.GetPart<RenderPart>();
            var homeRender = _container.GetPart<RenderPart>();
            if (actorRender == null || !actorRender.Visible || actorRender.ParentEntity != context.Actor
                || homeRender == null || !homeRender.Visible || homeRender.ParentEntity != _container) return false;
            var actorCell = zone.GetEntityCell(context.Actor);
            var homeCell = zone.GetEntityCell(_container);
            return actorCell != null && homeCell != null && actorCell.IsVisible && homeCell.IsVisible
                && actorCell.Objects.Contains(context.Actor) && homeCell.Objects.Contains(_container);
        }
    }
}
