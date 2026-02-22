namespace CavesOfOoo.Core.Inventory.Commands
{
    /// <summary>
    /// Command wrapper for item disassembly through the inventory command pipeline.
    /// </summary>
    public sealed class DisassembleCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "Disassemble";

        public DisassembleCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Disassemble requires a valid actor.");
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
                    "Disassemble requires a valid item.");
            }

            if (context.Actor.GetPart<BitLockerPart>() == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Actor is missing BitLockerPart.");
            }

            if (!context.Inventory.Contains(_item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotOwned,
                    "Actor does not own this item.");
            }

            if (!TinkeringService.CanDisassemble(_item, out string reason))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    reason);
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = TinkeringService.TryDisassemble(
                context.Actor,
                _item,
                out _,
                out string reason);

            if (!success)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    reason);
            }

            return InventoryCommandResult.Ok();
        }
    }
}
