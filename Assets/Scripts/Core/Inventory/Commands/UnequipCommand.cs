namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class UnequipCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Unequip";

        public UnequipCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Unequip requires a valid actor.");
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
                    "Unequip requires a valid item.");
            }

            if (!InventorySystem.IsEquipped(context.Actor, _item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Item is not currently equipped.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.UnequipItem(context.Actor, _item);
            return success
                ? InventoryCommandResult.Ok()
                : InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Unequip failed.");
        }
    }
}
