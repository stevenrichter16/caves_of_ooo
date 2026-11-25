using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Admits suitable clay to a parcel, a handover shelf, or public loose storage.</summary>
    public sealed class DeliveryStoragePart : Part
    {
        public override string Name => "DeliveryStorage";
        public bool ReceivesParcels;
        public bool AcceptsLooseStock;
        public bool ContentsWithdrawn;

        private const int Measures = 2;
        private bool IsFixed => ReceivesParcels || AcceptsLooseStock;

        internal bool CanReach(Entity actor, Zone zone)
        {
            var owner = ParentEntity;
            if (!PreparationActions.CurrentActor(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || !Ground(owner, zone)) return false;
            var from = zone.GetEntityCell(actor);
            var at = zone.GetEntityCell(owner);
            return Math.Max(Math.Abs(from.X - at.X), Math.Abs(from.Y - at.Y)) <= 1
                && zone.GetReadOnlyEntities().Count(e => e.ID == owner.ID) == 1;
        }

        internal string RefuseDeposit(Entity actor, Entity item, Zone zone)
        {
            if (!CanReach(actor, zone)) return "Stand beside the storage first.";
            var container = ParentEntity.GetPart<ContainerPart>();
            var pack = actor.GetPart<InventoryPart>();
            if (!PreparationActions.Carried(actor, item) || !Unique(item, pack.Objects)
                || item.ID == ParentEntity.ID || item.ID == actor.ID
                || zone.GetReadOnlyEntities().Any(e => e.ID == item.ID)
                || container.Contents.Any(e => e == item || e?.ID == item.ID))
                return "That item is no longer in your pack.";

            if (ReceivesParcels)
            {
                if (container.Contents.Count != 0) return "The receiving shelf already holds a box.";
                return ParcelWeightOrZero(item, zone) > 0
                    ? null : "The shelf takes one box packed with two measures of lining clay.";
            }

            int stored = ReadMeasures(zone);
            int incoming = ReadMeasure(item, zone);
            if (incoming <= 0) return "This storage takes fire clay or prepared lining clay.";
            if (container.Contents.Any(e => !ConnectedClayProvenance.CanMerge(e, item)))
                return "These measures overlap the marks on the clay already stored here.";
            return stored >= 0 && incoming <= Measures - stored ? null : "This storage holds two measures. Separate a smaller stack first.";
        }

        internal bool CanWithdraw(Entity actor, Entity item, Zone zone)
        {
            if (!CanReach(actor, zone) || !HeldBy(item, ParentEntity)) return false;
            var pack = actor.GetPart<InventoryPart>();
            var container = ParentEntity.GetPart<ContainerPart>();
            if (AcceptsLooseStock)
            {
                if (ReadMeasures(zone) < 0) return false;
                string world = LocalGatheringClaims.WorldKeyFor(zone);
                int origin = ConnectedClayProvenance.Mask(item, world);
                if (pack.EquippedItems.Values.Any(owner => owner?.ID == item.ID)
                    || origin != 0 && pack.Objects.Concat(pack.EquippedItems.Values)
                        .Any(owner => (origin & ConnectedClayProvenance.Mask(owner, world)) != 0)) return false;
            }
            return item.ID != actor.ID && item.ID != ParentEntity.ID && Unique(item, container.Contents)
                && pack.Objects.All(e => e != item && e?.ID != item.ID)
                && zone.GetReadOnlyEntities().All(e => e.ID != item.ID)
                && pack.FindEquippedBodyPart(item) == null && !pack.EquippedItems.ContainsValue(item);
        }

        internal bool WatchLooseTransfer(Entity actor, Zone zone, InventoryTransaction transaction, int change)
        {
            if (!AcceptsLooseStock) return true;
            if (!CanReach(actor, zone)) return false;
            int previous = ReadMeasures(zone);
            int total = previous + change;
            if (previous < 0 || total is < 0 or > Measures) return false;
            var tray = ParentEntity;
            var trayCell = zone.GetEntityCell(tray);
            var actorCell = zone.GetEntityCell(actor);
            transaction.BeforeCommit(() => AcceptsLooseStock && ParentEntity == tray && CanReach(actor, zone)
                && zone.GetEntityCell(tray) == trayCell && zone.GetEntityCell(actor) == actorCell
                && ReadMeasures(zone) == total);
            return true;
        }

        internal void RecordWithdrawal(InventoryTransaction transaction)
        {
            // A returned shipment can prove continuous packing only until its first committed withdrawal.
            if (!IsFixed && !ContentsWithdrawn)
                transaction.AfterCommit(() => ContentsWithdrawn = true);
        }

        internal static int ParcelWeightOrZero(Entity item, Zone zone)
        {
            var storage = item?.GetPart<DeliveryStoragePart>();
            if (storage == null || storage.IsFixed || storage.ReadMeasures(zone) != Measures) return 0;
            int weight = HandlingService.GetWeight(item);
            return weight is > 0 and <= 8 ? weight : 0;
        }

        internal int ReadMeasures(Zone zone)
        {
            var box = ParentEntity;
            var container = box?.GetPart<ContainerPart>();
            if (ReceivesParcels || !Intact(box, container) || !AcceptsLooseStock && box.GetPart<ContainerLoadPart>()?.ParentEntity != box
                || box.HasPart<StackerPart>() || container.Contents.Count > Measures) return -1;
            int count = 0;
            int originMask = 0;
            foreach (var clay in container.Contents)
            {
                if (clay?.ID == box.ID || !HeldBy(clay, box) || !Unique(clay, container.Contents)
                    || zone.GetReadOnlyEntities().Any(e => e.ID == clay.ID)) return -1;
                int units = ReadMeasure(clay, zone);
                if (units <= 0 || units > Measures - count) return -1;
                var origin = clay.GetPart<ConnectedClayOriginPart>();
                if (origin != null && (originMask & origin.Units) != 0) return -1;
                originMask |= origin?.Units ?? 0;
                count += units;
            }
            return count;
        }

        private bool Ground(Entity owner, Zone zone)
        {
            var container = owner?.GetPart<ContainerPart>();
            if (!Intact(owner, container) || container.IsLocked || owner.SpatialZone != zone
                || owner.GetPart<RenderPart>()?.Visible != true) return false;
            var at = zone.GetEntityCell(owner);
            return at?.ParentZone == zone && Unique(owner, at.Objects)
                && owner.GetPart<PhysicsPart>() is PhysicsPart physics && physics.ParentEntity == owner
                && physics.Takeable != IsFixed && physics.InInventory == null && physics.Equipped == null;
        }

        internal bool Intact(Entity owner, ContainerPart container) => owner != null && container?.ParentEntity == owner
            && owner.GetPart<DeliveryStoragePart>() == this && owner.Parts.Count(p => p is DeliveryStoragePart) == 1
            && owner.Parts.All(p => p?.ParentEntity == owner) && owner.Parts.Count(p => p is ContainerPart) == 1 && container.Contents != null
            && (!AcceptsLooseStock || !ReceivesParcels && !owner.HasPart<ContainerLoadPart>() && !owner.HasPart<StackerPart>())
            && container.MaxItems == (ReceivesParcels ? 1 : Measures) && !owner.HasPart<SpatialFootprintPart>();

        private static bool HeldBy(Entity item, Entity owner) => item != null && !string.IsNullOrEmpty(item.ID)
            && item.SpatialZone == null && item.GetPart<PhysicsPart>() is PhysicsPart p
            && p.ParentEntity == item && p.Takeable && p.InInventory == owner && p.Equipped == null;

        private static bool Unique(Entity item, System.Collections.Generic.IEnumerable<Entity> entries) => item != null
            && !string.IsNullOrEmpty(item.ID) && entries.Contains(item) && entries.Count(e => e?.ID == item.ID) == 1;

        private static int ReadMeasure(Entity item, Zone zone)
        {
            if (item?.GetPart<StackerPart>() is not StackerPart stack || stack.ParentEntity != item
                || stack.StackCount < 1 || stack.StackCount > Measures || stack.StackCount > stack.MaxStack
                || item.HasPart<ContainerPart>() || item.HasTag("Creature") || item.HasTag("Terrain")
                || item.HasPart<SpatialFootprintPart>() || MaterialProcessService.IsProtected(item)) return 0;
            var recipe = RepairRecipeRegistry.Get("clay-well-lining");
            var factory = WorldLocationContext.For(zone)?.Factory ?? HarvestablePart.Factory;
            if (!RepairMaterialPart.ReadPayment(factory, item, recipe, out _)) return 0;
            var origin = item.GetPart<ConnectedClayOriginPart>();
            if (origin != null && origin.Units != 0 && ConnectedClayProvenance.Mask(item, LocalGatheringClaims.WorldKeyFor(zone)) == 0)
                return 0;
            return stack.StackCount;
        }
    }
}
