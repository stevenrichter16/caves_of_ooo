namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class AutoEquipCommand : IInventoryCommand
    {
        private readonly Entity _item;

        public string Name => "AutoEquip";

        public AutoEquipCommand(Entity item)
        {
            _item = item;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Auto-equip requires a valid actor.");
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
                    "Auto-equip requires a valid item.");
            }

            if (_item.GetPart<EquippablePart>() == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingEquippablePart,
                    "Item is not equippable.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.AutoEquip(context.Actor, _item);
            return success
                ? InventoryCommandResult.Ok()
                : InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Auto-equip failed.");
        }
    }
}
