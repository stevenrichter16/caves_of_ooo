using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class AutoEquipCommand : IInventoryCommand
    {
        private static readonly EquipPlanner Planner = new EquipPlanner();

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
            if (context == null || context.Actor == null || context.Inventory == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Auto-equip context is invalid.");
            }

            var equippable = _item.GetPart<EquippablePart>();
            if (equippable == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not equippable.");
            }

            var stacker = _item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Auto-equip does not apply to stacked items.");
            }

            if (context.Body != null)
            {
                var plan = Planner.Build(context.Actor, _item);
                if (!plan.IsValid)
                {
                    return InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed,
                        "Auto-equip failed.");
                }

                if (plan.Displacements.Count > 0)
                {
                    return InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed,
                        "Auto-equip would displace equipped items.");
                }

                return EquipCommand.ExecuteInternal(
                    context,
                    transaction,
                    _item,
                    targetBodyPart: null,
                    allowDisplacements: false,
                    emitPlanFailureMessage: false,
                    prebuiltBodyPlan: plan);
            }

            if (context.Inventory.GetEquipped(equippable.Slot) != null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Auto-equip slot is occupied.");
            }

            return EquipCommand.ExecuteInternal(
                context,
                transaction,
                _item,
                targetBodyPart: null,
                allowDisplacements: false,
                emitPlanFailureMessage: false);
        }
    }
}
