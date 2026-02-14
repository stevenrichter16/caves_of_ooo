using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Planning;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class EquipCommand : IInventoryCommand
    {
        private static readonly EquipPlanner Planner = new EquipPlanner();

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
            return ExecuteInternal(
                context,
                transaction,
                _item,
                _targetBodyPart,
                allowDisplacements: true,
                emitPlanFailureMessage: true);
        }

        internal static InventoryCommandResult ExecuteInternal(
            InventoryContext context,
            InventoryTransaction transaction,
            Entity item,
            BodyPart targetBodyPart,
            bool allowDisplacements,
            bool emitPlanFailureMessage,
            EquipPlan prebuiltBodyPlan = null)
        {
            if (context == null || context.Actor == null || context.Inventory == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Equip context is invalid.");
            }

            if (item == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Equip item is null.");
            }

            var actor = context.Actor;
            var inventory = context.Inventory;
            var equippable = item.GetPart<EquippablePart>();
            if (equippable == null)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not equippable.");
            }

            Entity itemToEquip = item;
            var sourceStacker = item.GetPart<StackerPart>();
            if (sourceStacker != null && sourceStacker.StackCount > 1)
            {
                itemToEquip = sourceStacker.RemoveOne();
                var splitItem = itemToEquip;
                transaction.Do(
                    apply: null,
                    undo: () => RestoreSplitStack(sourceStacker, splitItem));

                equippable = itemToEquip.GetPart<EquippablePart>();
                if (equippable == null)
                {
                    return InventoryCommandResult.Fail(
                        InventoryCommandErrorCode.ExecutionFailed,
                        "Split stack item is missing EquippablePart.");
                }
            }

            var beforeEquip = GameEvent.New("BeforeEquip");
            beforeEquip.SetParameter("Actor", (object)actor);
            beforeEquip.SetParameter("Item", (object)itemToEquip);
            beforeEquip.SetParameter("Slot", equippable.Slot);
            if (!actor.FireEvent(beforeEquip))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Equip was cancelled.");
            }

            bool equipped = context.Body != null
                ? TryEquipBodyPartAware(
                    context,
                    transaction,
                    itemToEquip,
                    targetBodyPart,
                    allowDisplacements,
                    emitPlanFailureMessage,
                    prebuiltBodyPlan)
                : TryEquipLegacy(context, transaction, itemToEquip, equippable, allowDisplacements);

            if (!equipped)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Equip failed.");
            }

            transaction.Do(
                apply: null,
                undo: () =>
                {
                    var rollback = new UnequipCommand(itemToEquip).Execute(
                        context,
                        new InventoryTransaction());
                    if (!rollback.Success)
                    {
                        // Best-effort rollback for inventory consistency.
                        inventory.RemoveObject(itemToEquip);
                    }
                });

            EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: true);
            transaction.Do(
                apply: null,
                undo: () => EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: false));

            MessageLog.Add($"{actor.GetDisplayName()} equips {itemToEquip.GetDisplayName()}.");

            var afterEquip = GameEvent.New("AfterEquip");
            afterEquip.SetParameter("Actor", (object)actor);
            afterEquip.SetParameter("Item", (object)itemToEquip);
            afterEquip.SetParameter("Slot", equippable.Slot);
            actor.FireEvent(afterEquip);

            return InventoryCommandResult.Ok();
        }

        private static bool TryEquipBodyPartAware(
            InventoryContext context,
            InventoryTransaction transaction,
            Entity item,
            BodyPart targetBodyPart,
            bool allowDisplacements,
            bool emitPlanFailureMessage,
            EquipPlan prebuiltPlan)
        {
            var actor = context.Actor;
            var inventory = context.Inventory;

            var plan = prebuiltPlan ?? Planner.Build(actor, item, targetBodyPart);
            if (!plan.IsValid)
            {
                if (emitPlanFailureMessage && !string.IsNullOrEmpty(plan.FailureReason))
                    MessageLog.Add(plan.FailureReason);
                return false;
            }

            if (!allowDisplacements && plan.Displacements.Count > 0)
                return false;

            var claimedParts = plan.ClaimedParts;
            if (claimedParts.Count == 0)
                return false;

            var unequipped = new HashSet<Entity>();
            for (int i = 0; i < claimedParts.Count; i++)
            {
                var existing = claimedParts[i]._Equipped;
                if (existing == null || !unequipped.Add(existing))
                    continue;

                var unequipResult = new UnequipCommand(existing).Execute(context, transaction);
                if (!unequipResult.Success)
                    return false;
            }

            if (claimedParts.Count == 1)
                inventory.EquipToBodyPart(item, claimedParts[0]);
            else
                inventory.EquipToBodyParts(item, claimedParts);

            return true;
        }

        private static bool TryEquipLegacy(
            InventoryContext context,
            InventoryTransaction transaction,
            Entity item,
            EquippablePart equippable,
            bool allowDisplacements)
        {
            var inventory = context.Inventory;
            string slot = equippable.Slot;

            var existing = inventory.GetEquipped(slot);
            if (existing != null)
            {
                if (!allowDisplacements)
                    return false;

                var unequipResult = new UnequipCommand(existing).Execute(context, transaction);
                if (!unequipResult.Success)
                    return false;
            }

            inventory.Equip(item, slot);
            return true;
        }

        private static void RestoreSplitStack(StackerPart sourceStacker, Entity splitItem)
        {
            if (sourceStacker == null || splitItem == null)
                return;

            var splitStacker = splitItem.GetPart<StackerPart>();
            if (splitStacker == null || splitStacker.StackCount <= 0)
                return;

            sourceStacker.StackCount += splitStacker.StackCount;
            splitStacker.StackCount = 0;
        }
    }
}
