using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class EquipCommand : IInventoryCommand
    {
        private readonly Entity _item;
        private readonly BodyPart _targetBodyPart;

        public string Name => "Equip";

        public EquipCommand(Entity item, BodyPart targetBodyPart = null)
        {
            _item = item;
            _targetBodyPart = targetBodyPart;
        }

        public InventoryValidationResult Validate(InventoryContext context)
        {
            if (context == null || context.Actor == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.InvalidActor,
                    "Equip requires a valid actor.");
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
                    "Equip requires a valid item.");
            }

            if (!context.Inventory.Contains(_item))
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.NotOwned,
                    "Actor does not own this item.");
            }

            if (_item.GetPart<EquippablePart>() == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingEquippablePart,
                    "Item is not equippable.");
            }

            if (_targetBodyPart != null && context.Body == null)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.MissingBodyPart,
                    "Cannot target a body part when actor has no Body.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            bool success = InventorySystem.Equip(context.Actor, _item, _targetBodyPart);
            return success
                ? InventoryCommandResult.Ok()
                : InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Equip failed.");
        }
    }
}
