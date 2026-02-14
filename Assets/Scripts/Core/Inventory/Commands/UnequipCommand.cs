using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

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

            if (!CaptureEquippedState(context, _item).HasLocation)
            {
                return InventoryValidationResult.Invalid(
                    InventoryValidationErrorCode.BlockedByRule,
                    "Item is not currently equipped.");
            }

            return InventoryValidationResult.Valid();
        }

        public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
        {
            var actor = context.Actor;
            var rollbackState = CaptureEquippedState(context, _item);
            if (!rollbackState.HasLocation)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not currently equipped.");
            }

            var equippable = _item.GetPart<EquippablePart>();

            // Fire BeforeUnequip on actor (can veto).
            var beforeUnequip = GameEvent.New("BeforeUnequip");
            beforeUnequip.SetParameter("Actor", (object)actor);
            beforeUnequip.SetParameter("Item", (object)_item);
            if (!actor.FireEvent(beforeUnequip))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unequip was cancelled.");
            }

            // Remove equip stat bonuses before detaching item.
            if (equippable != null)
            {
                EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: false);
                transaction.Do(
                    apply: null,
                    undo: () => EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: true));
            }

            if (!TryForceUnequip(context, _item, rollbackState))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unequip failed.");
            }

            MessageLog.Add($"{actor.GetDisplayName()} unequips {_item.GetDisplayName()}.");

            // Fire AfterUnequip on actor.
            var afterUnequip = GameEvent.New("AfterUnequip");
            afterUnequip.SetParameter("Actor", (object)actor);
            afterUnequip.SetParameter("Item", (object)_item);
            actor.FireEvent(afterUnequip);

            transaction.Do(
                apply: null,
                undo: () =>
                {
                    TryForceRestore(context, _item, rollbackState);
                });

            return InventoryCommandResult.Ok();
        }

        internal static EquippedStateSnapshot CaptureEquippedState(
            InventoryContext context,
            Entity item)
        {
            var bodyParts = CaptureEquippedBodyParts(context?.Body, item);
            string legacySlot = bodyParts.Count == 0
                ? context?.Inventory?.FindEquippedSlot(item)
                : null;

            return new EquippedStateSnapshot(bodyParts, legacySlot);
        }

        internal static bool TryForceUnequip(
            InventoryContext context,
            Entity item,
            EquippedStateSnapshot snapshot = null)
        {
            if (context?.Inventory == null || item == null)
                return false;

            var effectiveSnapshot = snapshot ?? CaptureEquippedState(context, item);
            if (!effectiveSnapshot.HasLocation)
                return false;

            if (effectiveSnapshot.BodyParts.Count > 0)
            {
                var firstPart = effectiveSnapshot.BodyParts[0];
                if (firstPart == null)
                    return false;

                return context.Inventory.UnequipFromBodyPart(firstPart);
            }

            if (!string.IsNullOrEmpty(effectiveSnapshot.LegacySlot))
                return context.Inventory.Unequip(effectiveSnapshot.LegacySlot);

            return false;
        }

        internal static bool TryForceRestore(
            InventoryContext context,
            Entity item,
            EquippedStateSnapshot snapshot)
        {
            if (context?.Inventory == null || item == null || snapshot == null || !snapshot.HasLocation)
                return false;

            var inventory = context.Inventory;

            if (snapshot.BodyParts.Count > 0)
            {
                for (int i = 0; i < snapshot.BodyParts.Count; i++)
                {
                    var part = snapshot.BodyParts[i];
                    if (part == null)
                        return false;

                    var existing = part._Equipped;
                    if (existing != null && existing != item)
                        return false;
                }

                if (snapshot.BodyParts.Count == 1)
                    return inventory.EquipToBodyPart(item, snapshot.BodyParts[0]);

                return inventory.EquipToBodyParts(item, snapshot.BodyParts);
            }

            if (!string.IsNullOrEmpty(snapshot.LegacySlot))
            {
                var existing = inventory.GetEquipped(snapshot.LegacySlot);
                if (existing != null && existing != item)
                    return false;

                return inventory.Equip(item, snapshot.LegacySlot);
            }

            return false;
        }

        private static List<BodyPart> CaptureEquippedBodyParts(Body body, Entity item)
        {
            var result = new List<BodyPart>();
            if (body == null || item == null)
                return result;

            var parts = body.GetParts();
            BodyPart firstSlotPart = null;
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part._Equipped != item)
                    continue;

                if (firstSlotPart == null && part.FirstSlotForEquipped)
                    firstSlotPart = part;
                else
                    result.Add(part);
            }

            if (firstSlotPart == null && result.Count > 0)
            {
                firstSlotPart = result[0];
                result.RemoveAt(0);
            }

            if (firstSlotPart != null)
                result.Insert(0, firstSlotPart);

            return result;
        }

        internal sealed class EquippedStateSnapshot
        {
            public List<BodyPart> BodyParts { get; }

            public string LegacySlot { get; }

            public bool HasLocation => BodyParts.Count > 0 || !string.IsNullOrEmpty(LegacySlot);

            public EquippedStateSnapshot(List<BodyPart> bodyParts, string legacySlot)
            {
                BodyParts = bodyParts ?? new List<BodyPart>();
                LegacySlot = legacySlot;
            }
        }
    }
}
