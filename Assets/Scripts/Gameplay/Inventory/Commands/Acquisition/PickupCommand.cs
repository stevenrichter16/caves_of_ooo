namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PickupCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly bool _autoEquip = true;
        private readonly bool _retainIdentity;
        private readonly int _expectedQuantity;
        private readonly Entity _retrievalRecipient;
        private readonly bool _requireAlliedRecipient;

        internal static PickupCommand ForRetrieval(Entity item, int expectedQuantity, Entity recipient, bool requireAlliedRecipient) =>
            new PickupCommand(item, expectedQuantity, recipient, requireAlliedRecipient);

        private PickupCommand(Entity item, int expectedQuantity, Entity recipient, bool requireAlliedRecipient)
        {
            _item = item;
            _retainIdentity = true;
            _expectedQuantity = expectedQuantity;
            _retrievalRecipient = recipient;
            _requireAlliedRecipient = requireAlliedRecipient;
        }

        public string Name => "Pickup";

        public PickupCommand(Entity item) : this(item, autoEquip: true) { }

        /// <summary>Ordinary pickup defaults to auto-equip. Recovery AI can reserve
        /// equipping for its next action without bypassing the transfer transaction.</summary>
        public PickupCommand(Entity item, bool autoEquip)
        {
            _item = item;
            _autoEquip = autoEquip;
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
            var actor = context.Actor;
            var zone = context.Zone;
            var inventory = context.Inventory;

            if (!transaction.TryClaim(_item, actor, Name))
                return Refuse(context, "transfer_in_progress", "Item transfer is already in progress.");
            if (!IsGroundSource(context) || (_retainIdentity && !RetrievalSourceCurrent(context)))
                return Refuse(context, "invalid_ground_source", "That item is no longer on this ground.");
            if ((_item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
                return Refuse(context, "empty_stack", "There is no positive unit to pick up.");

            if (!HandlingService.CanLift(actor, _item, out string liftFailure))
            {
                MessageLog.Add(liftFailure);
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    liftFailure);
            }

            // Fire BeforePickup on actor.
            var beforePickup = GameEvent.New("BeforePickup");
            beforePickup.SetParameter("Actor", (object)actor);
            beforePickup.SetParameter("Item", (object)_item);
            if (!actor.FireEventAndRelease(beforePickup))
            {
                return Refuse(context, "actor_veto", "Pickup was cancelled.");
            }

            // Fire BeforeBeingPickedUp on item.
            var beforeBeing = GameEvent.New("BeforeBeingPickedUp");
            beforeBeing.SetParameter("Actor", (object)actor);
            beforeBeing.SetParameter("Item", (object)_item);
            if (!_item.FireEventAndRelease(beforeBeing))
            {
                return Refuse(context, "item_veto", "Item pickup was cancelled.");
            }

            // Veto hooks can move/remove the source or change its quantity.
            if (!IsGroundSource(context) || (_retainIdentity && !RetrievalSourceCurrent(context)))
                return Refuse(context, "source_changed", "That item is no longer on this ground.");
            var stacker = _item.GetPart<StackerPart>();
            int quantity = stacker?.StackCount ?? 1;
            if (quantity <= 0) return Refuse(context, "empty_stack", "There is no positive unit to pick up.");
            bool gold = !_retainIdentity && _item.BlueprintName == "GoldCoin";
            long credit = gold ? (long)quantity * 5 : 0;
            int purse = TradeSystem.GetDrams(actor);
            if (gold && (long)purse + credit > int.MaxValue)
                return Refuse(context, "currency_overflow", "You cannot carry that much currency.");
            string itemName = _item.GetDisplayName();
            var originalCell = zone.GetEntityCell(_item);
            int originalX = originalCell.X, originalY = originalCell.Y;
            var gathering = LocalGatheringClaims.CaptureTake(actor, _item, zone);
            var progress = ConnectedSpreadProgress.CaptureTransfer(actor, _item, zone);
            if (!zone.RemoveEntity(_item))
                return Refuse(context, "removal_refused", "The item could not be removed from the ground.");
            transaction.Do(apply: null, undo: () => zone.AddEntity(_item, originalX, originalY));

            if (gold)
            {
                // Spend the source before success hooks. Currency is only available
                // when the transaction commits, so independent work cannot spend
                // provisional gold or lose its own payment during outer rollback.
                transaction.Do(
                    apply: () => { if (stacker != null) stacker.StackCount = 0; },
                    undo: () => { if (stacker != null) stacker.StackCount = quantity; });
                transaction.DeferCurrencyCredit(actor, (int)credit);
            }
            else
            {
                var destination = InventoryTransferSnapshot.Capture(inventory, _item);
                transaction.Do(apply: null, undo: destination.Restore);
                if (!destination.Apply(() => _retainIdentity ? inventory.AddRetrievedObject(_item) : inventory.AddObject(_item)))
                {
                    MessageLog.Add($"You can't carry {itemName}: too heavy!");
                    return Refuse(context, "weight_limit", "Weight limit exceeded.");
                }
                if (!destination.ClaimChanges(transaction, actor, Name))
                    return Refuse(context, "destination_in_progress", "A destination stack is already being transferred.");
            }

            // Fire item-side Taken AFTER successful acquisition (CoO analog of Qud's
            // TakenEvent). World-object quest Parts (CompleteObjectiveOnTaken /
            // QuestStarter) live on the item and hook this. Fires before
            // AutoEquip; ground gold has a spent source and a pending commit credit.
            var taken = GameEvent.New("Taken");
            taken.SetParameter("Actor", (object)actor);
            taken.SetParameter("Item", (object)_item);
            _item.FireEventAndRelease(taken);

            MessageLog.Add(gold ? $"You pocket {quantity} gold ({credit} drams)."
                : $"{actor.GetDisplayName()} picks up {itemName}.");

            // Preserve auto-equip-on-pickup behavior through command-native flow.
            // Failures are non-fatal for pickup and simply mean "left carried".
            if (!gold && !_retainIdentity && _autoEquip) new AutoEquipCommand(_item).Execute(context, transaction);

            // Fire AfterPickup on actor.
            var afterPickup = GameEvent.New("AfterPickup");
            afterPickup.SetParameter("Actor", (object)actor);
            afterPickup.SetParameter("Item", (object)_item);
            actor.FireEventAndRelease(afterPickup);

            AcquisitionDiagnostics.Record(context, _item, Name, zone.ZoneID, quantity);
            LocalGatheringClaims.RecordTake(gathering, transaction);
            ConnectedSpreadProgress.RecordTransfer(progress, transaction);
            return InventoryCommandResult.Ok();
        }

        private bool RetrievalSourceCurrent(InventoryContext context)
        {
            var actor = context.Actor;
            return GoFetchGoal.LiveMember(actor, context.Zone)
                && _retrievalRecipient != actor && _retrievalRecipient != _item
                && GoFetchGoal.LiveMember(_retrievalRecipient, context.Zone)
                && (!_requireAlliedRecipient || FactionManager.IsAllied(actor, _retrievalRecipient))
                && actor.GetPart<InventoryPart>() == context.Inventory && context.Inventory.ParentEntity == actor
                && GoFetchGoal.GroundItem(_item, context.Zone, null, _expectedQuantity)
                && SpatialQuery.Distance(context.Zone, actor, _item) <= 1
                && HandlingService.CanLift(actor, _item, out _);
        }

        private bool IsGroundSource(InventoryContext context)
        {
            var physics = _item.GetPart<PhysicsPart>();
            return context.Zone.GetEntityCell(_item) != null && physics != null
                && physics.InInventory == null && physics.Equipped == null;
        }
        private InventoryCommandResult Refuse(InventoryContext context, string reason, string message) =>
            AcquisitionDiagnostics.Refuse(context, _item, Name, context.Zone.ZoneID, reason, message);
    }
}
