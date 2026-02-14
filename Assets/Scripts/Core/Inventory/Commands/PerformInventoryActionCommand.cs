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
            var actor = context.Actor;
            var zone = context.Zone;

            // Fire BeforeInventoryAction on actor (can veto).
            var before = GameEvent.New("BeforeInventoryAction");
            before.SetParameter("Actor", (object)actor);
            before.SetParameter("Item", (object)_item);
            before.SetParameter("Command", _actionCommand);
            if (!actor.FireEvent(before))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    $"Inventory action '{_actionCommand}' was cancelled.");
            }

            // Fire InventoryAction on the item.
            var actionEvent = GameEvent.New("InventoryAction");
            actionEvent.SetParameter("Actor", (object)actor);
            actionEvent.SetParameter("Item", (object)_item);
            actionEvent.SetParameter("Command", _actionCommand);
            if (zone != null)
                actionEvent.SetParameter("Zone", (object)zone);

            bool handled = !_item.FireEvent(actionEvent) || actionEvent.Handled;
            if (!handled)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    $"Inventory action '{_actionCommand}' failed.");
            }

            // Fire AfterInventoryAction on actor.
            var after = GameEvent.New("AfterInventoryAction");
            after.SetParameter("Actor", (object)actor);
            after.SetParameter("Item", (object)_item);
            after.SetParameter("Command", _actionCommand);
            actor.FireEvent(after);

            return InventoryCommandResult.Ok();
        }
    }
}
