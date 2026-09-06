using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>Separate one carried unit without immediately merging it back.
    /// Total mass and world time stay unchanged. Subsequent ordinary transfers may merge it.
    /// Create a command per attempt; validation queries do not clear prior results.</summary>
    public sealed class SeparateOneCommand : IInventoryCommand
    {
        private readonly Entity _source;
        public string Name => "SeparateOne";

        /// <summary>The exact new carried singleton for a successful execution. Read only when its
        /// corresponding result succeeds; rollback clears it. A validation query does not mutate it.</summary>
        public Entity SeparatedItem { get; private set; }

        public SeparateOneCommand(Entity source) { _source = source; }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context?.Actor == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidActor, "Separate one requires a valid actor.");
            var inventory = context.Inventory;
            if (inventory == null || context.Actor.GetPart<InventoryPart>() != inventory)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.MissingInventoryPart, "That inventory is no longer available.");
            if (_source == null)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.InvalidItem, "That item is no longer available.");
            var physics = _source.GetPart<PhysicsPart>();
            if (inventory.Objects == null || !inventory.Objects.Contains(_source)
                || inventory.EquippedItems.ContainsValue(_source) || inventory.FindEquippedBodyPart(_source) != null
                || (physics != null && (physics.InInventory != context.Actor || physics.Equipped != null)))
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.NotOwned, "Separate one requires an item carried in your pack.");
            if ((_source.GetPart<StackerPart>()?.StackCount ?? 1) <= 1)
                return InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, "You need at least two items in the stack.");
            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            SeparatedItem = null;
            var validation = Validate(context);
            if (!validation.IsValid) return InventoryCommandResult.ValidationFailure(validation);
            if (!transaction.TryClaim(_source, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "That item is already in use.");

            // Initialize may run extension code. Prepare before changing quantity;
            // independent work completed during preparation is not ours to rewind.
            var stacker = _source.GetPart<StackerPart>();
            var unit = _source.CloneForStack();
            unit.GetPart<StackerPart>().StackCount = 1;
            var mark = unit.GetPart<CraftingMarkPart>();
            if (mark != null) unit.RemovePart(mark); // Keep the source's existing station selection.
            validation = Validate(context);
            if (!validation.IsValid) return InventoryCommandResult.ValidationFailure(validation);
            if (_source.GetPart<StackerPart>() != stacker)
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "The selected stack changed while preparing the item.");

            var inventory = context.Inventory; int weight = inventory.GetCarriedWeight();
            var receipt = InventoryTransferSnapshot.Capture(inventory, unit);
            transaction.Do(null, receipt.Restore);
            bool applied = receipt.Apply(() =>
            {
                stacker.StackCount--;
                // Intentional no-merge append: ordinary AddObject would erase the separation.
                inventory.Objects.Add(unit);
                var physics = unit.GetPart<PhysicsPart>();
                if (physics != null) { physics.InInventory = context.Actor; physics.Equipped = null; }
                inventory.RefreshHandlingCarryPenalty();
                return inventory.GetCarriedWeight() == weight;
            });
            if (!applied || !receipt.ClaimChanges(transaction, context.Actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Could not separate that item without changing your carried load.");

            transaction.Do(null, () => SeparatedItem = null);
            SeparatedItem = unit;
            MessageLog.Add(context.Actor.GetDisplayName() + " separates one " + InventoryPart.GetUnitDisplayName(unit) + ".");
            Diag.Record("event", "ItemSeparated", actor: context.Actor, target: unit,
                payload: new { sourceId = _source.ID, remaining = stacker.StackCount });
            return InventoryCommandResult.Ok();
        }
    }
}
