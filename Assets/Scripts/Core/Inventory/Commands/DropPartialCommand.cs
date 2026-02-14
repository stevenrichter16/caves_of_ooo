namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class DropPartialCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly int _count;

        public string Name => "DropPartial";

        public DropPartialCommand(Entity item, int count)
        {
            _item = item;
            _count = count;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Partial drop requires a valid actor.");
            }

            if (context.Zone == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidZone,
                    "Partial drop requires a valid zone.");
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
                    "Partial drop requires a valid item.");
            }

            if (_count <= 0)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Drop count must be positive.");
            }

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
            bool success = InventorySystem.DropPartial(context.Actor, _item, _count, context.Zone);
            return success
                ? InventoryCommandResult.Ok()
                : InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Partial drop failed.");
        }
    }
}
