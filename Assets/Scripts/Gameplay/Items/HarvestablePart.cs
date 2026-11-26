using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <summary>Saved world/inventory menu label; the action still uses the
        /// existing Harvest command and defaults to the ordinary harvest verb.</summary>
        public string ActionText = "harvest";
        public int YieldMin = 1;
        public int YieldMax = 1;
        public int YieldChance = 100;
        /// <summary>Saved by normal Part serialization; retained references and
        /// restored spent sources cannot be harvested again.</summary>
        public bool Harvested;
        public const int MaximumYieldCount = 64;

        internal static bool CanCreateYield(EntityFactory factory, string blueprint)
            => factory != null && !string.IsNullOrEmpty(blueprint) && factory.Blueprints.TryGetValue(blueprint, out var product)
                && !product.Tags.ContainsKey("Creature") && product.Parts.TryGetValue("Physics", out var physics)
                && physics.TryGetValue("Takeable", out var text) && bool.TryParse(text, out bool takeable) && takeable
                && (!product.Parts.TryGetValue("Stacker", out var stacker) || !stacker.TryGetValue("StackCount", out var count)
                    || int.TryParse(count, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture,
                        out int units) && units == 1);

        internal bool SeasonalAccess(Zone zone) => this is not SeasonalHarvestPart seasonal || seasonal.Ready(zone);

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? ParentEntity?.SpatialZone;
                bool canGather = this is RainSeamHarvestPart seam ? seam.CanGather(actor, zone)
                    : SeasonalAccess(zone) && (this is not SeasonalHarvestPart || SeasonalHarvestPart.ActorCanGather(actor, zone));
                if (!Harvested && canGather)
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction("Harvest", ActionText, "Harvest", 'h', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "Harvest") return true;
            var harvester = e.GetParameter<Entity>("Actor");
            if (!DoHarvest(harvester, e.GetParameter<Zone>("Zone") ?? harvester?.SpatialZone,
                e.GetParameter<InventoryTransaction>("InventoryTransaction"), e.GetParameter<Random>("Random"))) return true;
            e.Handled = true;
            return false;
        }

        private string Validate(Entity actor, Zone zone, int? selectedSeason, out bool carried, out Cell sourceCell, out Cell dropCell)
        {
            carried = false; sourceCell = dropCell = null;
            if (FieldWorkRigRules.IsRig(ParentEntity) && !FieldWorkRigRules.Available(actor, ParentEntity, zone, true)) return "rig-unavailable";
            if (actor == null || ParentEntity == null) return "missing-context";
            if (this is SeasonalHarvestPart seasonal)
            {
                if (!seasonal.Ready(zone, selectedSeason)) return "seasonal-source-unavailable";
                if (!SeasonalHarvestPart.ActorCanGather(actor, zone)) return "seasonal-actor-unavailable";
            }
            if (this is RainSeamHarvestPart seam && !seam.CanGather(actor, zone)) return "rain-seam-unavailable";
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

        internal bool DoHarvest(Entity actor, Zone zone, InventoryTransaction transaction, Random suppliedRandom = null,
            (string Product, int Season)? seasonalYield = null)
        {
            // Retain this physical source even if a later callback detaches its Part.
            var source = ParentEntity;
            if (Harvested) return Reject(actor, "spent");
            if (seasonalYield.HasValue && this is not SeasonalHarvestPart) return Reject(actor, "invalid-seasonal-yield");
            string reason = Validate(actor, zone, seasonalYield?.Season, out bool carried, out var sourceCell, out var dropCell);
            if (reason != null) return Reject(actor, reason);
            var seasonalProof = (this as SeasonalHarvestPart)?.CaptureSeason(zone, sourceCell, seasonalYield?.Season);
            var rainSeam = this as RainSeamHarvestPart;
            var rainProof = rainSeam?.CaptureGathering(actor, zone, sourceCell);
            string declaredProduct = YieldBlueprint;
            int declaredMin = YieldMin, declaredMax = YieldMax, declaredChance = YieldChance;
            string product = seasonalYield?.Product ?? declaredProduct;
            int min = seasonalYield.HasValue ? 1 : declaredMin;
            int max = seasonalYield.HasValue ? 1 : declaredMax;
            int chance = seasonalYield.HasValue ? 100 : declaredChance;
            if (min < 1 || max < min || max > MaximumYieldCount || chance < 0 || chance > 100)
                return Reject(actor, "invalid-yield-range");
            var factory = Factory;
            if (factory == null || string.IsNullOrEmpty(product) || !factory.Blueprints.ContainsKey(product))
                return Reject(actor, "missing-yield");
            var outputTemplate = seasonalYield.HasValue ? MaterialProcessTemplate.Capture(factory, product, true) : null;
            if (!CanCreateYield(factory, product) || seasonalYield.HasValue && outputTemplate == null)
                return Reject(actor, "invalid-product");

            bool ownTransaction = transaction == null;
            transaction ??= new InventoryTransaction();
            var inventory = actor.GetPart<InventoryPart>();
            InventoryTransferSnapshot receipt = null;
            var droppedItems = new List<Entity>();
            bool removedFromZone = false, restored = false;
            Entity hauler = null;
            DragPart grip = null;
            DraggedPart held = null;
            int appliedHaulPenalty = 0;
            Action restore = () =>
            {
                if (restored) return;
                restored = true;
                foreach (var item in droppedItems) zone?.RemoveEntity(item);
                receipt?.Restore();
                if (removedFromZone && source.SpatialZone == null && source.GetPart<DestructiblePart>()?.Gone != true) zone.AddEntity(source, sourceCell.X, sourceCell.Y);
                // Zone removal releases hauling immediately. If this transfer
                // rolls back, restore its exact grip and only the penalty that
                // removal refunded; never take over a later replacement grip.
                if (removedFromZone && grip != null && held != null
                    && zone.GetEntityCell(source) == sourceCell && zone.GetEntityCell(hauler) != null
                    && SpatialQuery.Distance(zone, hauler, source) <= DragSystem.GrabReach
                    && !CombatSystem.IsDeathHandled(hauler)
                    && !(hauler.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)
                    && source.GetPart<DestructiblePart>()?.Gone != true
                    && hauler.GetPart<DragPart>() == null && source.GetPart<DraggedPart>() == null
                    && grip.ParentEntity == null && held.ParentEntity == null)
                {
                    hauler.AddPart(grip); source.AddPart(held);
                    grip.AppliedPenalty = appliedHaulPenalty;
                    if (hauler.GetStat("Speed") is Stat speed) speed.Penalty += appliedHaulPenalty;
                }
                Harvested = false;
            };
            bool Fail(string failure) { restore(); return Reject(actor, failure); }
            try
            {
                if (!transaction.TryClaim(actor, actor, "Harvest") || !transaction.TryClaim(source, actor, "Harvest"))
                    return Reject(actor, "in-progress");
                if (FieldWorkRigRules.IsRig(source) && !FieldWorkRigRules.BeginChange(actor, source, zone, transaction, true)) return Reject(actor, "rig-unavailable");
                var audio=WorldAudioWork.Harvest(actor,this,zone);
                // Claim before factory/weight callbacks. Rollback restores this flag;
                // success leaves it durable even if an old source reference survives.
                transaction.Do(() => Harvested = true, restore);
                var rng = suppliedRandom ?? new Random();
                bool rollPassed = chance >= 100 || rng.Next(100) < chance;
                int count = rollPassed ? (max > min ? rng.Next(min, max + 1) : min) : 0;
                string sourceName = source.GetDisplayName();
                var products = new Entity[count];
                Func<bool> moistYield = null;
                for (int i = 0; i < count; i++)
                {
                    products[i] = factory.CreateEntity(product);
                    var item = products[i];
                    if (item == null || item.HasTag("Creature") || item.GetPart<PhysicsPart>()?.Takeable != true
                        || item.SpatialZone != null || item.GetPart<PhysicsPart>()?.InInventory != null
                        || item.GetPart<PhysicsPart>()?.Equipped != null || (item.GetPart<StackerPart>()?.StackCount ?? 1) != 1)
                        return Fail("invalid-product");
                    if (outputTemplate != null && (!outputTemplate.Matches(item) || !PreparationActions.Fresh(item, product)
                        || item.ID == actor.ID || item.ID == source.ID
                        || inventory.Objects.Any(owner => owner?.ID == item.ID)
                        || inventory.EquippedItems.Values.Any(owner => owner?.ID == item.ID)
                        || zone.GetReadOnlyEntities().Any(owner => owner.ID == item.ID)
                        || !transaction.TryClaim(item, actor, "Harvest")))
                        return Fail("invalid-selected-product");
                    if (rainSeam != null)
                    {
                        moistYield = rainSeam.PrepareYield(item, actor, sourceCell, transaction, factory);
                        if (moistYield == null) return Fail("invalid-rain-seam-product");
                    }
                }
                reason = Validate(actor, zone, seasonalYield?.Season, out bool stillCarried, out var stillSource, out var stillDrop);
                if (reason != null || stillCarried != carried || !ReferenceEquals(sourceCell, stillSource) || !ReferenceEquals(dropCell, stillDrop)
                    || actor.GetPart<InventoryPart>() != inventory || YieldBlueprint != declaredProduct || YieldMin != declaredMin
                    || YieldMax != declaredMax || YieldChance != declaredChance)
                    return Fail("source-changed");

                receipt = inventory == null ? null : InventoryTransferSnapshot.Capture(inventory, products);
                int packed = 0;
                bool Transfer()
                {
                    if (carried)
                    { if (!inventory.RemoveObject(source)) return false; }
                    else if (rainSeam == null)
                    {
                        var currentHeld = source.GetPart<DraggedPart>();
                        var currentHauler = currentHeld?.Dragger;
                        var currentGrip = currentHauler?.GetPart<DragPart>();
                        if (currentHeld?.ParentEntity == source && currentGrip?.Dragged == source
                            && currentGrip.ParentEntity == currentHauler)
                        {
                            held = currentHeld; hauler = currentHauler; grip = currentGrip;
                            appliedHaulPenalty = grip.AppliedPenalty;
                        }
                        if (!zone.RemoveEntity(source)) return false;
                        removedFromZone = true;
                    }
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
                if (seasonalProof != null) transaction.BeforeCommit(seasonalProof);
                if (rainProof != null)
                    transaction.BeforeCommit(() => rainProof() && moistYield?.Invoke() == true);
                int dropped = droppedItems.Count;
                transaction.AfterCommit(()=>audio?.Publish());
                transaction.AfterCommit(() =>
                {
                    if (dropCell != null) ZoneRenderHooks.MarkCellDirty(dropCell.X, dropCell.Y, "Harvested");
                    GroveLaw.OnDig(actor, zone, source);
                    if (Diag.IsChannelEnabled("loot")) Diag.Record("loot", "Harvested", actor, source,
                        new { source = source.BlueprintName, yield = product, count = packed, dropped, rollPassed });
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
