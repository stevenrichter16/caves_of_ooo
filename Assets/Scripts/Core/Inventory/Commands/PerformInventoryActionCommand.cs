namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class PerformInventoryActionCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly string _actionCommand;

        public string Name => "PerformInventoryAction";

        public PerformInventoryActionCommand(Entity item, string actionCommand)
        {
            _item = item;
            _actionCommand = actionCommand;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Perform action requires a valid actor.");
            }

            if (_item == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidItem,
                    "Perform action requires a valid item.");
            }

            if (string.IsNullOrEmpty(_actionCommand))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Action command is empty.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.PerformAction(context.Actor, _item, _actionCommand, context.Zone);
            return success
                ? InventoryCommandResult.Ok()
                : InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    $"Inventory action '{_actionCommand}' failed.");
        }
    }
}
