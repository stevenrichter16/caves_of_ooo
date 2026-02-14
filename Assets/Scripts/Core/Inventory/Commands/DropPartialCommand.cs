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

            if (context.Zone.GetEntityCell(context.Actor) == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Actor has no valid position to drop items.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var zone = context.Zone;

            var stacker = _item.GetPart<StackerPart>();
            if (stacker == null || _count >= stacker.StackCount)
            {
                // Fallback to normal drop behavior.
                return new DropCommand(_item).Execute(context, transaction);
            }

            var cell = zone.GetEntityCell(actor);
            if (cell == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Actor has no valid position to drop items.");
            }

            var split = stacker.SplitStack(_count);
            if (split == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Could not split stack for partial drop.");
            }

            transaction.Do(
                apply: null,
                undo: () =>
                {
                    var splitStacker = split.GetPart<StackerPart>();
                    if (splitStacker == null || splitStacker.StackCount <= 0)
                        return;

                    stacker.StackCount += splitStacker.StackCount;
                    splitStacker.StackCount = 0;
                });

            zone.AddEntity(split, cell.X, cell.Y);
            transaction.Do(
                apply: null,
                undo: () => zone.RemoveEntity(split));

            MessageLog.Add($"{actor.GetDisplayName()} drops {split.GetDisplayName()}.");
            return InventoryCommandResult.Ok();
        }
    }
}
