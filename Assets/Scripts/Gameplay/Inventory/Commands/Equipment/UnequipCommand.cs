using System.Collections.Generic;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core.Inventory.Commands
{
    public sealed class UnequipCommand : IInventoryCommand, IInventoryAudioItem
    {
        private readonly Entity _item;
        private readonly bool _emitSuccessMessage;
        public Entity AudioItem => _item;

        public string Name => "Unequip";

        public UnequipCommand(Entity item, bool emitSuccessMessage = true)
        {
            _item = item;
            _emitSuccessMessage = emitSuccessMessage;
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
            if (!transaction.TryClaim(_item, actor, Name))
                return InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Item transfer is already in progress.");
            var rollbackState = CaptureEquippedState(context, _item);
            if (!rollbackState.HasLocation)
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Item is not currently equipped.");
            }

            var equippable = _item.GetPart<EquippablePart>();
            // The ownership undo runs before the bonus undos. A failed
            // restoration (lost limb or independently occupied slot) must not
            // restore protection or enhancements to an item still in the pack.
            // Early failures before detachment still undo their stat removal.
            bool restoreBonuses = true, bonusesRemoved = false, enhancementRemoved = false, detachAttempted = false;
            var originalCell = context.Zone?.GetEntityCell(actor) ?? actor.SpatialZone?.GetEntityCell(actor);

            // Fire BeforeUnequip on actor (can veto).
            var beforeUnequip = GameEvent.New("BeforeUnequip");
            beforeUnequip.SetParameter("Actor", (object)actor);
            beforeUnequip.SetParameter("Item", (object)_item);
            if (!actor.FireEventAndRelease(beforeUnequip))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unequip was cancelled.");
            }
            string originalBonuses = equippable?.AppliedEquipBonuses ?? equippable?.EquipBonuses ?? "";
            string originalSkillBonuses = equippable?.AppliedEquipSkillBonuses ?? "";

            // Register all recovery before detaching: equipment notifications and
            // AfterUnequip callbacks can throw after ownership has already moved.
            // Reverse order restores the location before any contribution.
            transaction.Do(null, () =>
            {
                if (restoreBonuses && bonusesRemoved && equippable != null)
                    EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: true, restoredBonuses: originalBonuses, restoredSkillBonuses: originalSkillBonuses);
            });
            transaction.Do(null, () =>
            {
                if (restoreBonuses && enhancementRemoved) ItemEnhancementDispatch.DispatchOnEquip(actor, _item);
            });
            transaction.Do(null, () =>
            {
                if (!detachAttempted) return;
                bool original = actor.GetPart<Body>() == context.Body && rollbackState.BodyParts.Count > 0;
                if (original)
                    foreach (var part in rollbackState.BodyParts)
                        original &= part != null && part._Equipped == _item && context.Body?.GetParts().Contains(part) == true;
                else if (rollbackState.BodyParts.Count == 0)
                    original = context.Inventory.GetEquipped(rollbackState.LegacySlot) == _item;
                restoreBonuses = original || TryForceRestore(context, _item, rollbackState);
                if (!restoreBonuses) RetainRefusedUnit(context, _item, originalCell);
            });
            if (equippable != null)
            {
                EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: false);
                bonusesRemoved = true;
            }
            detachAttempted = true;
            if (!TryForceUnequip(context, _item, rollbackState))
            {
                return InventoryCommandResult.Fail(
                    InventoryCommandErrorCode.ExecutionFailed,
                    "Unequip failed.");
            }

            if (_emitSuccessMessage)
                MessageLog.Add($"{actor.GetDisplayName()} unequips {_item.GetDisplayName()}.");

            // Fire AfterUnequip on actor.
            var afterUnequip = GameEvent.New("AfterUnequip");
            afterUnequip.SetParameter("Actor", (object)actor);
            afterUnequip.SetParameter("Item", (object)_item);
            actor.FireEventAndRelease(afterUnequip);

            // Item-enhancement on-unequip hook (E.2.1). Symmetric pair of
            // EquipCommand's DispatchOnEquip — fires AFTER AfterUnequip so
            // enhancements removing on-equip bonuses see settled state.
            //
            // E.4.2 cold-eye Finding #1 fix: wrap in transaction.Do so a
            // rollback re-fires OnEquipped (concrete enhancements'
            // AppliedBonus eager-flag makes the redo idempotent).
            enhancementRemoved = true;
            ItemEnhancementDispatch.DispatchOnUnequip(actor, _item);

            return InventoryCommandResult.Ok();
        }

        /// <summary>
        /// Unequips an equipped item and removes it from inventory in one step,
        /// without emitting an unequip log message. Used internally by throw logic
        /// so that "X unequips Y." does not appear before the throw message.
        /// Fires BeforeUnequip (callers can still veto) and AfterUnequip.
        /// The caller is responsible for registering an undo that restores the item
        /// to inventory if the overall operation needs to be rolled back.
        /// Returns false if the item is not equipped or the unequip is vetoed.
        /// </summary>
        internal static bool UnequipAndRemove(
            InventoryContext context,
            Entity item,
            InventoryTransaction transaction)
        {
            if (context?.Actor == null || context.Inventory == null || item == null)
                return false;
            if (!transaction.TryClaim(item, context.Actor, "Unequip")) return false;

            var actor = context.Actor;
            var originalCell = context.Zone?.GetEntityCell(actor) ?? actor.SpatialZone?.GetEntityCell(actor);
            var rollbackState = CaptureEquippedState(context, item);
            if (!rollbackState.HasLocation)
                return false;

            var equippable = item.GetPart<EquippablePart>();
            bool restoreBonuses = true, bonusesRemoved = false, enhancementRemoved = false, detachAttempted = false;

            var beforeUnequip = GameEvent.New("BeforeUnequip");
            beforeUnequip.SetParameter("Actor", (object)actor);
            beforeUnequip.SetParameter("Item", (object)item);
            if (!actor.FireEventAndRelease(beforeUnequip)) return false;
            string originalBonuses = equippable?.AppliedEquipBonuses ?? equippable?.EquipBonuses ?? "";
            string originalSkillBonuses = equippable?.AppliedEquipSkillBonuses ?? "";

            // Register the full recovery before any mutation can invoke a listener.
            // Reverse order restores the location first, then gated enhancement/stat
            // contributions. A refused detach retains its original contributions.
            transaction.Do(null, () =>
            {
                if (restoreBonuses && bonusesRemoved && equippable != null)
                    EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: true, restoredBonuses: originalBonuses, restoredSkillBonuses: originalSkillBonuses);
            });
            transaction.Do(null, () =>
            {
                if (restoreBonuses && enhancementRemoved) ItemEnhancementDispatch.DispatchOnEquip(actor, item);
            });
            transaction.Do(null, () =>
            {
                if (!detachAttempted) return;
                bool liveBody = actor.GetPart<Body>() == context.Body;
                bool original = liveBody && rollbackState.BodyParts.Count > 0;
                if (original)
                    foreach (var part in rollbackState.BodyParts)
                        original &= part != null && part._Equipped == item && context.Body?.GetParts().Contains(part) == true;
                else if (rollbackState.BodyParts.Count == 0)
                    original = context.Inventory.GetEquipped(rollbackState.LegacySlot) == item;
                restoreBonuses = original || TryForceRestore(context, item, rollbackState);
                if (!restoreBonuses) RetainRefusedUnit(context, item, originalCell);
            });
            if (equippable != null)
            {
                bonusesRemoved = true;
                EquipBonusUtility.ApplyEquipBonuses(actor, equippable, apply: false);
            }
            detachAttempted = true;
            if (!TryForceUnequip(context, item, rollbackState)) return false;

            var afterUnequip = GameEvent.New("AfterUnequip");
            afterUnequip.SetParameter("Actor", (object)actor);
            afterUnequip.SetParameter("Item", (object)item);
            actor.FireEventAndRelease(afterUnequip);

            enhancementRemoved = true;
            ItemEnhancementDispatch.DispatchOnUnequip(actor, item);
            // Exact retrieval keeps identity; a rollback must never merge this
            // unit into an unrelated carried stack before restoring its slot.
            if (!context.Inventory.RemoveObject(item)) return false;

            return true;
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
            if (context?.Inventory == null || item == null || snapshot == null || !snapshot.HasLocation
                || context.Actor?.GetPart<InventoryPart>() != context.Inventory)
                return false;

            var inventory = context.Inventory;

            if (snapshot.BodyParts.Count > 0)
            {
                // Snapshot references survive dismemberment and body replacement.
                // They are valid destinations only while still in the live Body.
                // Validate every part before writing any equipment/cache entry.
                var liveParts = context.Actor.GetPart<Body>() == context.Body ? context.Body?.GetParts() : null;
                if (liveParts == null) return false;
                for (int i = 0; i < snapshot.BodyParts.Count; i++)
                {
                    var part = snapshot.BodyParts[i];
                    if (part == null || !liveParts.Contains(part))
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

        // Removed parts are not rollback destinations. Prefer the live carried
        // list, then the actor's current/original cell if no inventory survives.
        // Retain the exact unit without emitting a launch or acquisition event.
        private static void RetainRefusedUnit(InventoryContext context, Entity item, Cell originalCell)
        {
            var inventory = context.Actor?.GetPart<InventoryPart>();
            var physics = item?.GetPart<PhysicsPart>();
            if (physics?.ParentEntity != item || item.SpatialZone != null
                || physics.Equipped != null || physics.InInventory != null && physics.InInventory != context.Actor) return;
            if (inventory?.ParentEntity == context.Actor)
            {
                if (context.Inventory != inventory) context.Inventory?.Objects.Remove(item);
                if (!inventory.Objects.Contains(item)) inventory.Objects.Add(item);
                physics.InInventory = context.Actor;
                inventory.RefreshHandlingCarryPenalty();
                return;
            }
            // A callback may remove InventoryPart after detachment. Writing the
            // old orphan list would hide the refused item from both save and UI.
            context.Inventory?.Objects.Remove(item);
            physics.InInventory = null;
            var currentCell = context.Actor?.SpatialZone?.GetEntityCell(context.Actor);
            if (currentCell != null && currentCell.ParentZone.AddEntity(item, currentCell.X, currentCell.Y)) return;
            if (item.SpatialZone == null && originalCell != null && originalCell != currentCell)
                originalCell.ParentZone.AddEntity(item, originalCell.X, originalCell.Y);
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
