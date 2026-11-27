using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Authorizes use of an adjacent, movable public stock crate.</summary>
    public sealed class PublicCargoStoragePart : Part, IDescribeState, IPostCommitInventoryActionNotification
    {
        public override string Name => "PublicCargoStorage";
        public const int CargoLimit = 24;
        const string Timber = "SalvagedTimber";
        const string TakeUnit = "TakePublicCargoUnit|";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 24;

        public string DescribeState(DescribeContext context)
        {
            var container = ParentEntity?.GetPart<ContainerPart>();
            int packed = PackedWeight();
            if (container == null || packed < 0) return null;
            return $"Public cargo crate. {packed}/{CargoLimit} lb of goods, {container.Contents.Count}/{container.MaxItems} entries. "
                + "Open from beside it to take goods, or take one board from an eligible timber stack. "
                + "Put loose goods in from your pack. Set the crate down before transferring anything.";
        }

        public bool OwnsInventoryReceipt(string command) => ParentEntity?.GetPart<PublicCargoStoragePart>() == this
            && command?.StartsWith(TakeUnit, StringComparison.Ordinal) == true;

        public bool NotifyAfterCommit(string command) => OwnsInventoryReceipt(command);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions" && ParentEntity != null)
            {
                var actor = e.GetParameter<Entity>("Actor");
                var zone = PreparationActions.ZoneFor(e, actor);
                RouteWorkMenu.Rows(ParentEntity, actor, zone, e.GetParameter<InventoryActionList>("Actions"), UnitChoices(actor, zone), true);
            }
            if (e.ID != "InventoryAction" || !OwnsInventoryReceipt(e.GetStringParameter("Command"))
                || !RouteWorkMenu.Act(ParentEntity, e)) return true;
            e.Handled = true;
            return false;
        }

        IEnumerable<RouteWorkMenu.Entry> UnitChoices(Entity actor, Zone zone)
        {
            if (!CanReach(actor, zone)) yield break;
            var owner = ParentEntity;
            var container = owner.GetPart<ContainerPart>();
            var contents = container.Contents;
            var entries = contents.ToArray();
            var pack = actor.GetPart<InventoryPart>();
            var from = zone.GetEntityCell(actor);
            var at = zone.GetEntityCell(owner);
            var ownerIdentity = RouteWorkMenu.Identity(owner);
            int packed = PackedWeight();
            if (packed < 0) yield break;
            var factory = WorldLocationContext.For(zone)?.Factory ?? HarvestablePart.Factory;
            var template = MaterialProcessTemplate.Capture(factory, Timber);
            if (template == null) yield break;

            foreach (var stock in entries)
            {
                if (stock?.BlueprintName != Timber || !CanWithdraw(actor, stock, zone)
                    || stock.GetPart<StackerPart>() is not { StackCount: > 1 } stack || stack.StackCount > stack.MaxStack
                    || !stack.CanSplit() || !HandlingService.CanLift(actor, stock, out _)
                    || !StorageLabels.TryRead(stock, out var label) || template.Snapshot(stock) is not { } payload) continue;
                int count = stack.StackCount;
                int weight = HandlingService.GetWeight(stock);
                if (weight <= 0 || !CarriedStorage.FitsCombinedLoad(pack, weight)) continue;
                var stockIdentity = RouteWorkMenu.Identity(stock);
                Entity unit = null;
                InventoryUnitOutputReceipt destination = null;

                bool Context() => ParentEntity == owner && ownerIdentity() && container.Contents == contents
                    && zone.GetEntityCell(owner) == at && zone.GetEntityCell(actor) == from && actor.GetPart<InventoryPart>() == pack
                    && contents.SequenceEqual(entries) && CanWithdraw(actor, stock, zone) && payload.Matches()
                    && StorageLabels.TryRead(stock, out var currentLabel) && currentLabel.Equals(label);
                bool Before() => Context() && stack.StackCount == count && stack.CanSplit() && PackedWeight() == packed
                    && HandlingService.CanLift(actor, stock, out _) && CarriedStorage.FitsCombinedLoad(pack, weight);
                bool After() => Context() && stack.StackCount == count - 1 && PackedWeight() == packed - weight
                    && destination?.Current() == true && template.Matches(destination.Recipient)
                    && StorageLabels.SameStackLabel(stock, destination.Recipient) && CarriedStorage.FitsCombinedLoad(pack, 0);

                yield return new RouteWorkMenu.Entry
                {
                    Actor = actor, Zone = zone, Cell = at, Key = TakeUnit + stock.ID,
                    Claims = new[] { stock }, Label = $"take one {InventoryPart.GetUnitDisplayName(stock)} ({weight} lb)",
                    Hint = $"Take one board and leave {count - 1} in this stack. One action.",
                    Before = Before, After = After,
                    Apply = tx =>
                    {
                        unit = stock.CloneForStack();
                        unit.GetPart<StackerPart>().StackCount = 1;
                        if (!Before() || !template.Matches(unit) || !StorageLabels.SameStackLabel(stock, unit)
                            || !PreparationActions.Fresh(unit, Timber) || unit.ID == actor.ID || unit.ID == owner.ID
                            || contents.Concat(pack.Objects).Concat(pack.EquippedItems.Values).Any(item => item?.ID == unit.ID)
                            || zone.GetReadOnlyEntities().Any(item => item.ID == unit.ID)) return false;

                        tx.Do(() => stack.StackCount--, () =>
                        {
                            // Only refund a board the destination receipt could retract. A later move or lock stays in effect.
                            if (!stockIdentity() || destination is { RetractedUnit: false }) return;
                            stack.StackCount = WaterQuantityUndo.Restore(stack.StackCount, -1, stack.MaxStack);
                            stock.GetPart<PhysicsPart>()?.InInventory?.GetPart<InventoryPart>()?.RefreshHandlingCarryPenalty();
                        });
                        return InventoryUnitOutputReceipt.TryPlace(pack, actor, unit, tx, TakeUnit, out destination);
                    },
                    Publish = () =>
                    {
                        ThrowingRules.OnStackChanged(stock);
                        var taken = GameEvent.New("Taken");
                        taken.SetParameter("Actor", actor);
                        taken.SetParameter("Item", unit);
                        unit.FireEventAndRelease(taken);
                        MessageLog.Add($"You take one board from the {owner.GetDisplayName()}.");
                    }
                };
            }
        }

        internal bool CanReach(Entity actor, Zone zone)
        {
            var owner = ParentEntity;
            if (!PreparationActions.CurrentActor(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || !WorldResourceActions.Ground(owner, zone) || owner.GetPart<PublicCargoStoragePart>() != this
                || owner.Parts.Count(part => part is PublicCargoStoragePart) != 1 || SecondExplorationActions.Special(owner)
                || owner.HasPart<DeliveryStoragePart>() || owner.HasPart<ContainedLiquidSourcePart>() || owner.HasPart<InstallationMarkerPart>()
                || LocalPorterReservationPart.Blocks(owner) || DragSystem.IsBeingDragged(owner)
                || owner.GetPart<PhysicsPart>() is not { Solid: true, Takeable: false }
                || owner.GetPart<ContainerPart>() is not { IsLocked: false, MaxItems: > 0 and <= 8 } container
                || container.ParentEntity != owner || container.Contents == null
                || owner.GetPart<ContainerLoadPart>()?.ParentEntity != owner
                || owner.GetPart<HandlingPart>() is not { Weight: > 0 } handling || handling.ParentEntity != owner
                || owner.GetPart<RenderPart>() is not { Visible: true } render || render.ParentEntity != owner
                || zone.GetReadOnlyEntities().Count(entity => entity.ID == owner.ID) != 1) return false;

            var manager = WorldLocationContext.For(zone);
            if (manager != null && manager.ActiveZone != zone) return false;
            var from = zone.GetEntityCell(actor);
            var at = zone.GetEntityCell(owner);
            if (actor.HasTag("Player") && (!at.IsVisible || !at.Explored)) return false;
            int dx = Math.Abs(from.X - at.X), dy = Math.Abs(from.Y - at.Y);
            return dx <= 1 && dy <= 1 && (dx == 0 || dy == 0
                || !zone.GetCell(from.X, at.Y).BlocksMovement(actor) || !zone.GetCell(at.X, from.Y).BlocksMovement(actor));
        }

        internal string RefuseDeposit(Entity actor, Entity item, Zone zone)
        {
            if (!CanReach(actor, zone)) return "Stand beside the unlocked cargo crate after letting go of it.";
            if (!PreparationActions.Carried(actor, item) || !LooseGoods(item) || item.ID == actor.ID || item.ID == ParentEntity.ID)
                return "The cargo crate takes loose goods you are carrying.";
            var contents = ParentEntity.GetPart<ContainerPart>().Contents;
            if (contents.Any(entry => entry?.ID == item.ID) || zone.GetReadOnlyEntities().Any(entry => entry.ID == item.ID))
                return "That item's custody has changed.";
            int packed = PackedWeight();
            return packed >= 0 && packed + (long)WeightOfStack(item) <= CargoLimit
                ? null : $"The cargo crate holds up to {CargoLimit} lb of goods. Separate a smaller stack first.";
        }

        internal bool CanWithdraw(Entity actor, Entity item, Zone zone)
        {
            if (!CanReach(actor, zone) || !LooseGoods(item) || item.ID == ParentEntity.ID || item.ID == actor.ID
                || item.SpatialZone != null || item.GetPart<PhysicsPart>() is not { } physics
                || physics.ParentEntity != item || physics.InInventory != ParentEntity || physics.Equipped != null) return false;
            var pack = actor.GetPart<InventoryPart>();
            return ParentEntity.GetPart<ContainerPart>().Contents.Count(entry => entry == item || entry?.ID == item.ID) == 1
                && ParentEntity.GetPart<ContainerPart>().Contents.Contains(item)
                && !pack.Objects.Concat(pack.EquippedItems.Values).Any(entry => entry?.ID == item.ID)
                && !zone.GetReadOnlyEntities().Any(entry => entry.ID == item.ID);
        }

        internal bool WatchTransfer(Entity actor, Entity item, Zone zone, InventoryTransaction tx, bool deposit)
        {
            int packed = PackedWeight();
            int weight = WeightOfStack(item);
            long expected = packed + (deposit ? (long)weight : -(long)weight);
            if (packed < 0 || weight < 0 || expected < 0 || deposit && expected > CargoLimit) return false;
            var owner = ParentEntity;
            var container = owner.GetPart<ContainerPart>();
            var load = owner.GetPart<ContainerLoadPart>();
            var contents = container.Contents;
            var at = zone.GetEntityCell(owner);
            var from = zone.GetEntityCell(actor);
            var pack = actor.GetPart<InventoryPart>();
            var packItems = pack.Objects;
            tx.BeforeCommit(() => ParentEntity == owner && CanReach(actor, zone) && zone.GetEntityCell(actor) == from
                && zone.GetEntityCell(owner) == at && owner.GetPart<ContainerPart>() == container && container.Contents == contents
                && owner.GetPart<ContainerLoadPart>() == load && actor.GetPart<InventoryPart>() == pack && pack.Objects == packItems
                && PackedWeight() == expected && (deposit
                    ? !pack.Objects.Contains(item) && !pack.EquippedItems.ContainsValue(item) && item.SpatialZone == null
                    : !contents.Contains(item) && CarriedStorage.FitsCombinedLoad(pack, 0)));
            return true;
        }

        private int PackedWeight()
        {
            var container = ParentEntity?.GetPart<ContainerPart>();
            if (container?.Contents == null || container.Contents.Count > container.MaxItems || !container.Contents.All(LooseGoods)) return -1;
            int weight = ParentEntity.GetPart<ContainerLoadPart>()?.Weight(0) ?? int.MaxValue;
            return weight == int.MaxValue ? -1 : weight;
        }

        private static bool LooseGoods(Entity item) => !SecondExplorationActions.Special(item) && item.HasTag("Item")
            && !item.HasTag("Currency") && !item.HasPart<ContainerPart>() && !item.HasPart<RentalPart>() && !item.HasPart<SpatialFootprintPart>()
            && !item.HasPart<InstallationMarkerPart>() && !LocalPorterReservationPart.Blocks(item) && WeightOfStack(item) >= 0;

        private static int WeightOfStack(Entity item)
        {
            int units = item?.GetPart<StackerPart>()?.StackCount ?? 1;
            long weight = (long)HandlingService.GetWeight(item) * units;
            return units > 0 && weight >= 0 && weight < int.MaxValue ? (int)weight : -1;
        }
    }
}
