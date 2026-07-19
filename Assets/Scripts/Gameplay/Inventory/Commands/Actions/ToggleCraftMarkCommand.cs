namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Toggle the "set aside for crafting" mark on a carried item (M3-L3).
    /// Executed by the inventory popup's "Set aside"/"Take back" row; the
    /// crafting stations later read the marked set via
    /// <see cref="CraftingMarkPart.CollectMarked"/>. Validation rejects
    /// items the crafting flows can't consume so the mark never silently
    /// accumulates on irrelevant loot.
    /// </summary>
    public sealed class ToggleCraftMarkCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "ToggleCraftMark";

        public ToggleCraftMarkCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Marking requires a valid actor.");
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
                    "No item to mark.");
            }

            if (!context.Inventory.Contains(_item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotOwned,
                    "You can only set aside items you are carrying.");
            }

            if (!CraftingMarkPart.IsMarkable(_item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    _item.GetDisplayName() + " has no use at a crafting station.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool marked = CraftingMarkPart.Toggle(_item);

            MessageLog.Add(marked
                ? "You set " + _item.GetDisplayName() + " aside for crafting."
                : "You take " + _item.GetDisplayName() + " back.");

            return InventoryCommandResult.Ok();
        }
    }
}
