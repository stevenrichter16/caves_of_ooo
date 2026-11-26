using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Moves loose goods between two directly carried parts of the same pack.</summary>
    public static class CarriedStorage
    {
        public static bool IsEligible(Entity vessel)
        {
            if (!UsesCarriedContents(vessel) || vessel.GetPart<ContainerPart>() is not ContainerPart container
                || container.GetType() != typeof(ContainerPart) || container.ParentEntity != vessel
                || container.Contents == null || container.IsLocked || !container.AllowsCleanAccess
                || vessel.Parts.Count(part => part is ContainerPart) != 1
                || vessel.Parts.Count(part => part is ContainerLoadPart) != 1
                || vessel.GetPart<PhysicsPart>() is not { Takeable: true, Solid: false } physics
                || physics.ParentEntity != vessel || vessel.HasPart<StackerPart>() || vessel.HasPart<InventoryPart>()
                || vessel.HasPart<SpatialFootprintPart>() || vessel.HasPart<DeliveryStoragePart>()
                || vessel.HasPart<ContainedLiquidSourcePart>() || vessel.HasTag("Creature")
                || vessel.GetPart<DestructiblePart>() is { Gone: true })
                return false;

            return vessel.BlueprintName == "FieldSatchel"
                || vessel.GetPart<PackableFixturePart>() is PackableFixturePart kit
                && kit.ParentEntity == vessel && kit.Kind == CampKinds.Box
                && vessel.GetPart<InstallablePart>()?.State != InstallablePart.Installed;
        }

        internal static bool UsesCarriedContents(Entity vessel)
            => (vessel?.BlueprintName == "FieldSatchel" || vessel?.BlueprintName == "CampBox")
                && vessel.GetPart<ContainerLoadPart>()?.ParentEntity == vessel;

        /// <summary>New acquisition fits the same pounds allowance as the loose goods.</summary>
        internal static bool CanAcceptNewLoad(InventoryPart pack, Entity vessel)
            => UsesCarriedContents(vessel) && FitsCombinedLoad(pack, InventoryPart.GetItemWeight(vessel));

        internal static bool FitsCombinedLoad(InventoryPart pack, long incomingWeight)
        {
            if (pack == null || incomingWeight < 0 || incomingWeight >= int.MaxValue) return false;

            int limit = pack.MaxWeight;
            int strengthLimit = pack.ParentEntity?.HasTag("Player") == true ? pack.GetMaxCarryWeight() : -1;
            if (strengthLimit >= 0)
                limit = limit < 0 ? strengthLimit : Math.Min(limit, strengthLimit);
            long total = pack.GetCarriedWeight() + incomingWeight;
            return total < int.MaxValue && (limit < 0 || total <= limit);
        }

        public static bool IsCarried(Entity actor, Entity vessel)
            => IsEligible(vessel) && WorldResourceActions.Carried(actor, vessel)
                && IntactContents(actor.GetPart<InventoryPart>(), vessel, vessel.GetPart<ContainerPart>());

        public static bool CanDeposit(Entity actor, Entity vessel, Entity item, Zone zone, out string failure)
            => CanMove(actor, vessel, item, zone, true, out failure);

        public static bool CanWithdraw(Entity actor, Entity vessel, Entity item, Zone zone, out string failure)
            => CanMove(actor, vessel, item, zone, false, out failure);

        public static bool TryDeposit(Entity actor, Entity vessel, Entity item, Zone zone, out string failure)
            => Move(actor, vessel, item, zone, true, out failure);

        public static bool TryWithdraw(Entity actor, Entity vessel, Entity item, Zone zone, out string failure)
            => Move(actor, vessel, item, zone, false, out failure);

        static bool ActorCurrent(Entity actor, Zone zone)
        {
            var manager = WorldLocationContext.For(zone);
            return PreparationActions.CurrentActor(actor, zone)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && (manager == null || manager.ActiveZone == zone);
        }

        static bool IsLooseGood(Entity item)
        {
            if (item?.GetPart<PhysicsPart>() is not { Takeable: true } physics || physics.ParentEntity != item
                || physics.Equipped != null || item.SpatialZone != null || string.IsNullOrEmpty(item.ID)
                || item.HasTag("Creature") || item.HasPart<SpatialFootprintPart>() || item.HasPart<InventoryPart>()
                || item.HasPart<ContainerPart>() || item.HasPart<ContainerLoadPart>()
                || item.HasPart<CargoSledPart>() || item.HasPart<SledLoadPart>() || !HandlingService.IsCarryable(item))
                return false;

            var stack = item.GetPart<StackerPart>();
            return (stack == null || stack.ParentEntity == item && stack.StackCount > 0 && stack.StackCount <= stack.MaxStack)
                && InventoryPart.GetItemWeight(item) < int.MaxValue;
        }

        static bool IntactContents(InventoryPart pack, Entity vessel, ContainerPart container)
        {
            // The existing cargo query bounds the entire physical graph to 64 owners.
            if (container.Contents.Count >= 64 || InventoryPart.GetItemWeight(vessel) == int.MaxValue) return false;

            var ids = new HashSet<string> { vessel.ID };
            foreach (var item in container.Contents)
            {
                if (!IsLooseGood(item) || !ids.Add(item.ID) || item.GetPart<PhysicsPart>().InInventory != vessel
                    || pack.Objects.Any(owner => owner?.ID == item.ID)
                    || pack.EquippedItems.Values.Any(owner => owner?.ID == item.ID)
                    || pack.FindEquippedBodyPart(item) != null)
                    return false;
            }
            return true;
        }

        static bool CanMove(Entity actor, Entity vessel, Entity item, Zone zone, bool depositing, out string failure)
        {
            if (!ActorCurrent(actor, zone) || !IsCarried(actor, vessel))
                return Refuse("That storage is no longer available in your pack.", out failure);
            if (!IsLooseGood(item) || LocalPorterReservationPart.Blocks(item) || MaterialProcessService.IsProtected(item)
                || ContainedLiquidSourcePart.IsRecoveryCustodyProtected(item))
                return Refuse("Only loose goods can move between this storage and your pack.", out failure);

            var container = vessel.GetPart<ContainerPart>();
            bool owned = depositing
                ? WorldResourceActions.Carried(actor, item, single: false)
                : container.Contents.Contains(item);
            if (!owned || depositing && container.Contents.Any(other => other.ID == item.ID))
                return Refuse("That item is no longer in the selected place.", out failure);
            if (depositing && !HasRoom(container, item))
                return Refuse("There is no room for that item in this storage.", out failure);

            failure = null;
            return true;
        }

        static bool HasRoom(ContainerPart container, Entity item)
        {
            if ((container.MaxItems < 0 || container.Contents.Count < container.MaxItems)
                && container.Contents.Count < 63) return true;

            var stack = item.GetPart<StackerPart>();
            if (stack == null) return false;
            long room = 0;
            foreach (var resident in container.Contents)
            {
                var existing = resident.GetPart<StackerPart>();
                if (existing?.CanStackWith(item) == true) room += (long)existing.MaxStack - existing.StackCount;
            }
            return room >= stack.StackCount;
        }

        static bool Move(Entity actor, Entity vessel, Entity item, Zone zone, bool depositing, out string failure)
        {
            var result = InventorySystem.ExecuteCommand(new StorageTransfer(vessel, item, depositing), actor, zone);
            failure = result.Success ? null : result.ErrorMessage;
            return result.Success;
        }

        sealed class StorageTransfer : IInventoryCommand
        {
            readonly Entity vessel;
            readonly Entity item;
            readonly bool depositing;

            public StorageTransfer(Entity vessel, Entity item, bool depositing)
            {
                this.vessel = vessel;
                this.item = item;
                this.depositing = depositing;
            }

            public string Name => depositing ? "StowCarriedItem" : "RetrieveCarriedItem";

            public InventoryValidationResult Validate(InventoryContext context)
                => CanMove(context.Actor, vessel, item, context.Zone, depositing, out string failure)
                    ? InventoryValidationResult.Valid()
                    : InventoryValidationResult.Invalid(InventoryValidationErrorCode.BlockedByRule, failure);

            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var actor = context.Actor;
                var zone = context.Zone;
                var pack = context.Inventory;
                if (actor.GetPart<InventoryPart>() != pack) return Failed("The selected storage changed.");
                if (!CanMove(actor, vessel, item, zone, depositing, out string failure)) return Failed(failure);
                if (!transaction.TryClaim(vessel, actor, Name) || !transaction.TryClaim(item, actor, Name))
                    return Failed("The selected storage or item is already in use.");

                var container = vessel.GetPart<ContainerPart>();
                var actorCell = zone.GetEntityCell(actor);
                int weight = pack.GetCarriedWeight();
                int maxItems = container.MaxItems;
                if (weight == int.MaxValue) return Failed("The combined load could not be weighed.");

                var packReceipt = InventoryTransferSnapshot.Capture(pack, item);
                var storageReceipt = InventoryTransferSnapshot.Capture(container, item);
                var source = depositing ? packReceipt : storageReceipt;
                var destination = depositing ? storageReceipt : packReceipt;
                // Rollback runs in reverse. Recalculate after both owners
                // are restored, including a failed withdrawal into the pack.
                transaction.Do(null, pack.RefreshHandlingCarryPenalty);
                transaction.Do(null, source.Restore);
                if (!source.Apply(() => depositing ? pack.RemoveObject(item) : container.RemoveItem(item)))
                    return Failed("The item moved before it could be transferred.");
                transaction.Do(null, destination.Restore);
                if (!destination.Apply(() => depositing ? container.AddItem(item) : pack.AddStorageWithdrawal(item))
                    || !destination.ClaimChanges(transaction, actor, Name))
                    return Failed("The destination could not accept this item.");

                bool Current() => ActorCurrent(actor, zone) && actor.GetPart<InventoryPart>() == pack
                    && zone.GetEntityCell(actor) == actorCell && vessel.GetPart<ContainerPart>() == container
                    && IsCarried(actor, vessel) && container.MaxItems == maxItems
                    && (maxItems < 0 || container.Contents.Count <= maxItems)
                    && pack.GetCarriedWeight() == weight;
                if (!Current()) return Failed("The transfer would change the combined carried weight or storage capacity.");
                transaction.BeforeCommit(Current);
                transaction.AfterCommit(pack.RefreshHandlingCarryPenalty);
                return InventoryCommandResult.Ok();
            }

            static InventoryCommandResult Failed(string reason)
                => InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, reason);
        }

        static bool Refuse(string reason, out string failure)
        {
            failure = reason;
            return false;
        }
    }
}
