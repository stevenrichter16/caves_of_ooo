using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One exact simple healing tonic spent in its carried container.</summary>
    internal sealed class CarriedTonicUse
    {
        readonly Entity actor, vessel;
        readonly Zone zone;
        readonly Cell origin;
        readonly InventoryPart pack;
        readonly ContainerPart holder;
        readonly List<Entity> contents;
        readonly PhysicsPart physics;
        readonly StackerPart stack;
        readonly TonicPart tonic;
        readonly (string healing, string message, bool drink) payload;
        readonly string itemId, vesselId;
        readonly int units;

        internal Entity Item { get; }
        internal string Label => (payload.drink ? "Drink " : "Apply ") + Item.GetDisplayName() + " (one action)";

        CarriedTonicUse(Entity actor, Entity vessel, Entity item, Zone zone, TonicPart tonic)
        {
            this.actor = actor;
            this.vessel = vessel;
            this.zone = zone;
            this.tonic = tonic;
            Item = item;
            origin = zone.GetEntityCell(actor);
            pack = actor.GetPart<InventoryPart>();
            holder = vessel.GetPart<ContainerPart>();
            contents = holder.Contents;
            physics = item.GetPart<PhysicsPart>();
            stack = item.GetPart<StackerPart>();
            units = stack?.StackCount ?? 1;
            itemId = item.ID;
            vesselId = vessel.ID;
            payload = (tonic.Healing, tonic.Message, tonic.Drink);
        }

        static bool SimpleHealing(Entity item, TonicPart tonic)
            => tonic != null && tonic.GetType() == typeof(TonicPart) && tonic.ParentEntity == item
                && item.Parts.Count(part => part is TonicPart) == 1
                && tonic.Effect == "Healing" && tonic.Duration == 0
                && SafeHealing(tonic.Healing) && string.IsNullOrEmpty(tonic.StatBoost)
                && !item.HasPart<StatusTonicPart>() && !item.HasPart<CureTonicPart>()
                && !item.HasPart<BrewItemPart>();

        static bool SafeHealing(string expression)
        {
            try
            {
                var (count, sides, modifier) = DiceRoller.Parse(expression);
                return count is > 0 and <= 100 && sides > 0 && sides < int.MaxValue
                    && (long)count + modifier > 0 && (long)count * sides + modifier <= int.MaxValue / 2;
            }
            catch (Exception error) when (error is OverflowException || error is FormatException)
            {
                return false;
            }
        }

        internal static CarriedTonicUse Offer(Entity actor, Entity vessel, Entity item, Zone zone)
        {
            var tonic = item?.GetPart<TonicPart>();
            if (!SimpleHealing(item, tonic) || !CarriedStorage.CanWithdraw(actor, vessel, item, zone, out _))
                return null;
            var offer = new CarriedTonicUse(actor, vessel, item, zone, tonic);
            return offer.NoOtherCopy() ? offer : null;
        }

        internal InventoryCommandResult Execute()
            => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(this), actor, zone);

        bool ExactContainer() => vessel.ID == vesselId && vessel.GetPart<ContainerPart>() == holder
            && holder.ParentEntity == vessel && holder.Contents == contents;

        bool ExactItem() => Item.ID == itemId && Item.GetPart<PhysicsPart>() == physics
            && physics.ParentEntity == Item && physics.Equipped == null && Item.SpatialZone == null
            && Item.GetPart<StackerPart>() == stack && (stack == null || stack.ParentEntity == Item);

        bool NoOtherCopy()
        {
            bool Matches(Entity item) => item == Item || item?.ID == itemId;
            return !pack.Objects.Any(Matches) && !pack.EquippedItems.Values.Any(Matches)
                && pack.FindEquippedBodyPart(Item) == null && !zone.GetReadOnlyEntities().Any(Matches)
                && !pack.Objects.Where(owner => owner != vessel)
                    .Any(owner => owner?.GetPart<ContainerPart>()?.Contents?.Any(Matches) == true)
                && !zone.GetReadOnlyEntitiesWithPart<ContainerPart>()
                    .Any(owner => owner != vessel && owner.GetPart<ContainerPart>().Contents?.Any(Matches) == true);
        }

        bool InHolder() => physics.InInventory == vessel
            && contents.Count(item => item == Item || item?.ID == itemId) == 1 && contents.Contains(Item);

        bool Ready(InventoryContext context)
            => context.Actor == actor && context.Zone == zone && context.Inventory == pack
                && actor.GetPart<InventoryPart>() == pack && CarriedStorage.ActorCurrent(actor, zone)
                && zone.GetEntityCell(actor) == origin && ExactContainer() && ExactItem()
                && CarriedStorage.TryBrowse(actor, vessel, out var current) && current == holder
                && Item.GetPart<TonicPart>() == tonic && SimpleHealing(Item, tonic)
                && (tonic.Healing, tonic.Message, tonic.Drink) == payload && NoOtherCopy();

        internal bool CanPay(InventoryContext context)
            => Ready(context) && InHolder() && (stack?.StackCount ?? 1) == units
                && CarriedStorage.CanWithdraw(actor, vessel, Item, zone, out _);

        internal bool TryClaim(InventoryContext context, InventoryTransaction transaction)
            => CanPay(context) && transaction.TryClaimAction(actor)
                && transaction.TryClaim(actor, actor, TonicPart.ApplyCommand)
                && transaction.TryClaim(vessel, actor, TonicPart.ApplyCommand)
                && transaction.TryClaim(Item, actor, TonicPart.ApplyCommand);

        internal bool TryStage(InventoryContext context, InventoryTransaction transaction)
        {
            if (!CanPay(context)) return false;
            int index = contents.IndexOf(Item);
            bool final = units == 1;
            if (final)
            {
                if (!holder.RemoveItem(Item)) return false;
            }
            else stack.StackCount--;

            transaction.Do(null, () =>
            {
                if (actor.GetPart<InventoryPart>() == pack) pack.RefreshHandlingCarryPenalty();
            });
            transaction.Do(null, () =>
            {
                if (!ExactContainer() || !ExactItem() || actor.GetPart<InventoryPart>() != pack
                    || !NoOtherCopy()) return;
                if (!final && InHolder())
                {
                    if (stack.StackCount >= 0 && stack.StackCount < stack.MaxStack) stack.StackCount++;
                }
                else if (final && Detached() && (holder.MaxItems < 0 || contents.Count < holder.MaxItems))
                {
                    contents.Insert(Math.Min(index, contents.Count), Item);
                    physics.InInventory = vessel;
                }
            });

            bool Paid() => Ready(context) && (final ? Detached() : InHolder() && stack.StackCount == units - 1)
                && !LocalPorterReservationPart.Blocks(Item) && !MaterialProcessService.IsProtected(Item)
                && !ContainedLiquidSourcePart.IsRecoveryCustodyProtected(Item);
            transaction.BeforeCommit(Paid);
            transaction.AfterCommit(() =>
            {
                pack.RefreshHandlingCarryPenalty();
                Diag.Record("event", "ItemUnitConsumed", actor: actor, target: Item,
                    payload: new { blueprint = Item.BlueprintName, quantityBefore = units, quantityAfter = units - 1 });
                tonic.ApplyPaidStoredUse(actor, zone);
            });
            return Paid();
        }

        bool Detached() => physics.InInventory == null && (stack?.StackCount ?? 1) == units
            && !contents.Any(item => item == Item || item?.ID == itemId);
    }
}
