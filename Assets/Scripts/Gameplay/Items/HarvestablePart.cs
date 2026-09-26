using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Random = System.Random;

namespace CavesOfOoo.Core
{
    /// <summary>A finite corpse/mineral harvest. The actor must carry the source
    /// or stand within one cell of its physical footprint. Products and source
    /// consumption join the calling inventory transaction; overflow stays on
    /// the source cell. A failed roll still spends a valid source exactly once.</summary>
    public class HarvestablePart : Part
    {
        public override string Name => "Harvestable";
        public static EntityFactory Factory;
        public string YieldBlueprint = "";
        public int YieldMin = 1;
        public int YieldMax = 1;
        public int YieldChance = 100;
        /// <summary>Saved by normal Part serialization; retained references and
        /// restored spent sources cannot be harvested again.</summary>
        public bool Harvested;
        public const int MaximumYieldCount = 64;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                if (!Harvested) e.GetParameter<InventoryActionList>("Actions")?.AddAction("Harvest", "harvest", "Harvest", 'h', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "Harvest") return true;
            if (!DoHarvest(e.GetParameter<Entity>("Actor"), e)) return true;
            e.Handled = true;
            return false;
        }

        private string Validate(Entity actor, Zone zone, out bool carried, out Cell sourceCell, out Cell dropCell)
        {
            carried = false; sourceCell = dropCell = null;
            if (actor == null || ParentEntity == null) return "missing-context";
            if (CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)) return "actor-dead";
            var inventory = actor.GetPart<InventoryPart>();
            carried = inventory?.CanConsumeOne(ParentEntity) == true;
            var physics = ParentEntity.GetPart<PhysicsPart>();
            if (carried)
            {
                if (ParentEntity.SpatialZone != null || physics?.Equipped != null
                    || (physics != null && physics.InInventory != actor)) return "inconsistent-owner";
                if (zone != null && zone.GetEntityCell(actor) == null) return "actor-not-in-zone";
                dropCell = zone?.GetEntityCell(actor);
                return null;
            }
            if (physics?.InInventory != null || physics?.Equipped != null) return "not-owned";
            if (zone == null || zone.GetEntityCell(actor) == null || (sourceCell = zone.GetEntityCell(ParentEntity)) == null) return "detached-source-or-actor";
            if (SpatialQuery.Distance(zone, actor, ParentEntity) > 1) return "out-of-reach";
            dropCell = sourceCell;
            return null;
        }

        private bool DoHarvest(Entity actor, GameEvent e)
        {
            var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (Harvested) return Reject(actor, "spent");
            string reason = Validate(actor, zone, out bool carried, out var sourceCell, out var dropCell);
            if (reason != null) return Reject(actor, reason);
            if (YieldMin < 1 || YieldMax < YieldMin || YieldMax > MaximumYieldCount || YieldChance < 0 || YieldChance > 100)
                return Reject(actor, "invalid-yield-range");
            var factory = Factory;
            if (factory == null || string.IsNullOrEmpty(YieldBlueprint) || !factory.Blueprints.ContainsKey(YieldBlueprint))
                return Reject(actor, "missing-yield");
            var outputBlueprint = factory.Blueprints[YieldBlueprint];
            if (outputBlueprint.Tags.ContainsKey("Creature") || !outputBlueprint.Parts.TryGetValue("Physics", out var outputPhysics)
                || !outputPhysics.TryGetValue("Takeable", out var takeableText) || !bool.TryParse(takeableText, out bool takeable) || !takeable)
                return Reject(actor, "invalid-product");

            var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            bool ownTransaction = transaction == null;
            transaction ??= new InventoryTransaction();
            var inventory = actor.GetPart<InventoryPart>();
            InventoryTransferSnapshot receipt = null;
            var droppedItems = new List<Entity>();
            bool removedFromZone = false, restored = false;
            Action restore = () =>
            {
                if (restored) return;
                restored = true;
                foreach (var item in droppedItems) zone?.RemoveEntity(item);
                receipt?.Restore();
                if (removedFromZone && ParentEntity.SpatialZone == null) zone.AddEntity(ParentEntity, sourceCell.X, sourceCell.Y);
                Harvested = false;
            };
            bool Fail(string failure) { restore(); return Reject(actor, failure); }
            try
            {
                if (!transaction.TryClaim(actor, actor, "Harvest") || !transaction.TryClaim(ParentEntity, actor, "Harvest"))
                    return Reject(actor, "in-progress");
                // Claim before factory/weight callbacks. Rollback restores this flag;
                // success leaves it durable even if an old source reference survives.
                transaction.Do(() => Harvested = true, restore);
                var rng = e.GetParameter<Random>("Random") ?? new Random();
                bool rollPassed = YieldChance >= 100 || rng.Next(100) < YieldChance;
                int count = rollPassed ? (YieldMax > YieldMin ? rng.Next(YieldMin, YieldMax + 1) : YieldMin) : 0;
                string product = YieldBlueprint, sourceName = ParentEntity.GetDisplayName();
                int min = YieldMin, max = YieldMax, chance = YieldChance;
                var products = new Entity[count];
                for (int i = 0; i < count; i++)
                {
                    products[i] = factory.CreateEntity(product);
                    var item = products[i];
                    if (item == null || item.HasTag("Creature") || item.GetPart<PhysicsPart>()?.Takeable != true
                        || item.SpatialZone != null || item.GetPart<PhysicsPart>()?.InInventory != null
                        || item.GetPart<PhysicsPart>()?.Equipped != null || (item.GetPart<StackerPart>()?.StackCount ?? 1) != 1)
                        return Fail("invalid-product");
                }
                reason = Validate(actor, zone, out bool stillCarried, out var stillSource, out var stillDrop);
                if (reason != null || stillCarried != carried || !ReferenceEquals(sourceCell, stillSource) || !ReferenceEquals(dropCell, stillDrop)
                    || actor.GetPart<InventoryPart>() != inventory || YieldBlueprint != product || YieldMin != min || YieldMax != max || YieldChance != chance)
                    return Fail("source-changed");

                receipt = inventory == null ? null : InventoryTransferSnapshot.Capture(inventory, products);
                int packed = 0;
                bool Transfer()
                {
                    if (carried)
                    { if (!inventory.RemoveObject(ParentEntity)) return false; }
                    else
                    { if (!zone.RemoveEntity(ParentEntity)) return false; removedFromZone = true; }
                    foreach (var item in products)
                    {
                        bool accepted = false;
                        if (inventory != null)
                        {
                            var unit = InventoryTransferSnapshot.Capture(inventory, item);
                            accepted = unit.Apply(() => inventory.AddCraftedUnitWithinCapacity(item, out _));
                            if (!accepted) unit.Restore();
                        }
                        if (accepted) { packed++; continue; }
                        if (dropCell == null || !zone.AddEntity(item, dropCell.X, dropCell.Y)) return false;
                        droppedItems.Add(item);
                    }
                    return true;
                }
                bool applied = receipt == null ? Transfer() : receipt.Apply(Transfer);
                if (!applied || (receipt != null && !receipt.ClaimChanges(transaction, actor, "Harvest"))) return Fail("transfer-refused");
                int dropped = droppedItems.Count;
                transaction.AfterCommit(() =>
                {
                    if (dropCell != null) ZoneRenderHooks.MarkCellDirty(dropCell.X, dropCell.Y, "Harvested");
                    GroveLaw.OnDig(actor, zone, ParentEntity);
                    if (Diag.IsChannelEnabled("loot")) Diag.Record("loot", "Harvested", actor, ParentEntity,
                        new { source = ParentEntity.BlueprintName, yield = product, count = packed, dropped, rollPassed });
                    MessageLog.Add(dropped > 0
                        ? $"You harvest {sourceName}: {packed} x {product} packed, {dropped} left on the ground."
                        : packed > 0 ? $"You harvest {sourceName}: {packed} x {product}."
                        : $"You harvest {sourceName}, but find nothing worth keeping.");
                });
                if (ownTransaction) transaction.Commit();
                return true;
            }
            finally { if (ownTransaction) transaction.Rollback(); }
        }

        private bool Reject(Entity actor, string reason)
        {
            if (Diag.IsChannelEnabled("loot")) Diag.Record("loot", "HarvestRejected", actor, ParentEntity, new { reason });
            return false;
        }
    }
}
