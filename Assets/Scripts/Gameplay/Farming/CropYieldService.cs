using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Stages a complete physical crop yield, then removes its exact
    /// parent in the same transaction. No output is silently packed or lost to
    /// inventory capacity. The next ordinary pickup owns that separate action.</summary>
    internal static class CropYieldService
    {
        internal const int MaximumYieldUnits = 32;

        internal static bool TryRelease(CropPart crop, Zone zone, Entity actor = null, InventoryTransaction transaction = null, bool gather = false, bool seedsOnly = false)
        {
            if (gather && actor?.GetPart<InventoryPart>() == null) return false;
            var source = new Source(crop, zone, actor, seedsOnly);
            if (!source.Current() || !source.ValidRecipe()) return Reject(crop, actor, "invalid-source-or-yield");
            var gathering = LocalGatheringClaims.CaptureHarvest(actor, source.Owner, zone);
            var factory = CropSystem.Factory;
            if (factory == null) return Reject(crop, actor, "no_factory");
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            var products = new List<Entity>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            bool removed = false, restored = false;
            Action restore = () =>
            {
                if (restored) return;
                restored = true;
                foreach (var product in products)
                    if (product.SpatialZone == zone) zone.RemoveEntity(product);
                if (removed && source.Owner.SpatialZone == null && zone.AddEntity(source.Owner, source.Cell.X, source.Cell.Y))
                {
                    // Restore the source's cell ordering as well as its identity.
                    source.Cell.Objects.Remove(source.Owner);
                    source.Cell.Objects.Insert(Math.Min(source.Index, source.Cell.Objects.Count), source.Owner);
                }
                ZoneRenderHooks.MarkCellDirty(source.Cell.X, source.Cell.Y, "CropYieldRollback");
            };
            try
            {
                if (!transaction.TryClaim(source.Owner, actor, "HarvestCrop")
                    || (actor != null && !transaction.TryClaim(actor, actor, "HarvestCrop")))
                    return Reject(crop, actor, "in-progress");
                int total = source.YieldCount + source.SeedCount;
                for (int i = 0; i < total; i++)
                {
                    bool seed = i >= source.YieldCount;
                    string blueprint = seed ? source.SeedBlueprint : source.YieldBlueprint;
                    if (!factory.Blueprints.ContainsKey(blueprint)) return Reject(crop, actor, "unknown_yield_blueprint");
                    var product = factory.CreateEntity(blueprint);
                    if (!source.Current() || !Fresh(product, blueprint) || !identities.Add(product.ID)
                        || HasIdentity(zone, product.ID) || !transaction.TryClaim(product, actor, "HarvestCrop"))
                        return Reject(crop, actor, "invalid-output-or-source-changed");
                    if (seed && (product.GetPart<SeedPart>() is not SeedPart seedPart
                        || seedPart.ParentEntity != product || seedPart.CropBlueprint != source.Blueprint))
                        return Reject(crop, actor, "invalid-returned-seed");
                    products.Add(product);
                }
                // A later product initializer can mutate an earlier product.
                // Revalidate the entire staged batch before writing any owner.
                identities.Clear();
                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    bool seed = i >= source.YieldCount;
                    if (!Fresh(product, seed ? source.SeedBlueprint : source.YieldBlueprint)
                        || !identities.Add(product.ID) || HasIdentity(zone, product.ID)
                        || (seed && (product.GetPart<SeedPart>() is not SeedPart returnedSeed
                            || returnedSeed.ParentEntity != product || returnedSeed.CropBlueprint != source.Blueprint)))
                        return Reject(crop, actor, "output-changed");
                }
                // Register before the first world write. No creation callback runs
                // between placement and source removal; all outputs are preflighted.
                transaction.Do(null, restore);
                foreach (var product in products)
                {
                    if (!source.Current() || !zone.AddEntity(product, source.Cell.X, source.Cell.Y))
                    { restore(); return Reject(crop, actor, "yield-placement"); }
                }
                if (!source.Current() || !zone.RemoveEntity(source.Owner))
                { restore(); return Reject(crop, actor, "source-changed"); }
                removed = true;
                LocalGatheringClaims.RecordHarvest(gathering, products, transaction);
                int packed = 0;
                if (gather)
                {
                    var pack = actor.GetPart<InventoryPart>();
                    foreach (var product in products)
                    {
                        var taking = LocalGatheringClaims.CaptureTake(actor, product, zone);
                        var receipt = InventoryTransferSnapshot.Capture(pack, product);
                        transaction.Do(null, receipt.Restore);
                        if (!receipt.Apply(() => pack.AddCraftedUnitWithinCapacity(product, out _))) { receipt.Restore(); continue; }
                        if (!receipt.ClaimChanges(transaction, actor, "GatherCrop")) return false;
                        zone.RemoveEntity(product); packed++;
                        LocalGatheringClaims.RecordTake(taking, transaction);
                    }
                }
                ConnectedSpreadProgress.RecordHarvest(actor, source.Owner, zone, transaction);
                ZoneRenderHooks.MarkCellDirty(source.Cell.X, source.Cell.Y, actor == null ? "CropMatured" : "CropHarvested");
                transaction.AfterCommit(() =>
                {
                    MessageLog.Add(actor == null
                        ? "The " + source.Owner.GetDisplayName() + " is ready — its harvest lies on the ground."
                        : gather ? "You gather " + source.Owner.GetDisplayName() + ": " + packed + " packed, " + (products.Count - packed) + " left here."
                        : seedsOnly ? "You harvest " + source.Owner.GetDisplayName() + " for seed. No produce is gathered."
                        : "You harvest " + source.Owner.GetDisplayName() + ". The produce and saved seed lie here to pick up.");
                    if (Diag.IsChannelEnabled("crop")) Diag.Record("crop", actor == null ? "CropMatured" : "CropHarvested",
                        actor: actor, target: source.Owner, payload: new { yieldBlueprint = source.YieldBlueprint,
                            yieldCount = source.YieldCount, seedBlueprint = source.SeedBlueprint, seedCount = source.SeedCount,
                            x = source.Cell.X, y = source.Cell.Y });
                });
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        internal static bool Fresh(Entity entity, string blueprint)
        {
            var physics = entity?.GetPart<PhysicsPart>();
            var stack = entity?.GetPart<StackerPart>();
            return entity != null && entity.BlueprintName == blueprint && !string.IsNullOrEmpty(entity.ID)
                && entity.SpatialZone == null && physics != null && physics.ParentEntity == entity
                && physics.Takeable && !physics.Solid && physics.InInventory == null && physics.Equipped == null
                && !entity.HasTag("Creature") && !entity.HasTag("Crop") && !entity.HasTag("Solid")
                && !entity.HasPart<CropPart>() && !entity.HasPart<SpatialFootprintPart>()
                && (stack == null || (stack.ParentEntity == entity && stack.StackCount == 1 && stack.MaxStack >= 1));
        }
        private static bool HasIdentity(Zone zone, string id)
        { foreach (var entity in zone.GetReadOnlyEntities()) if (entity.ID == id) return true; return false; }
        private static bool Reject(CropPart crop, Entity actor, string reason)
        {
            if (actor != null && actor.HasTag("Player")) MessageLog.Add("That crop cannot be harvested here yet; nothing is gathered.");
            if (Diag.IsChannelEnabled("crop")) Diag.Record("crop", actor == null ? "MatureBlocked" : "CropHarvestRejected",
                actor: actor, target: crop?.ParentEntity, payload: new { reason });
            return false;
        }
        private sealed class Source
        {
            internal readonly Entity Owner;
            internal readonly Cell Cell;
            internal readonly int Index, YieldCount, SeedCount;
            internal readonly string YieldBlueprint, SeedBlueprint, Blueprint;
            readonly CropPart crop;
            readonly PhysicsPart physics;
            readonly Zone zone;
            readonly Entity actor;
            readonly string id;
            readonly int stage, progress, moisture;
            readonly bool harvest, seedsOnly;
            readonly int originalYieldCount, originalSeedCount;
            internal Source(CropPart crop, Zone zone, Entity actor, bool seedsOnly)
            {
                this.crop = crop; this.zone = zone; this.actor = actor; this.seedsOnly = seedsOnly;
                Owner = crop?.ParentEntity; Cell = zone?.GetEntityCell(Owner); Index = Cell?.Objects.IndexOf(Owner) ?? -1;
                physics = Owner?.GetPart<PhysicsPart>(); id = Owner?.ID; Blueprint = Owner?.BlueprintName;
                YieldBlueprint = crop?.YieldBlueprint; originalYieldCount = crop?.YieldCount ?? 0; YieldCount = seedsOnly ? 0 : originalYieldCount;
                harvest = crop?.HarvestAtMaturity == true;
                SeedBlueprint = harvest ? crop.SeedYieldBlueprint : ""; originalSeedCount = harvest ? crop.SeedYieldCount : 0;
                SeedCount = seedsOnly ? (int)Math.Min(int.MaxValue, (long)originalSeedCount * 3) : originalSeedCount;
                stage = crop?.GrowthStage ?? -1; progress = crop?.TicksInStage ?? -1; moisture = crop?.MoistureTicks ?? -1;
            }
            internal bool ValidRecipe() => (seedsOnly ? harvest && actor != null && SeedCount > 0 : YieldCount > 0) && YieldCount <= MaximumYieldUnits
                && SeedCount >= 0 && SeedCount <= MaximumYieldUnits - YieldCount
                && !string.IsNullOrEmpty(YieldBlueprint) && (SeedCount == 0 || !string.IsNullOrEmpty(SeedBlueprint));
            internal bool Current()
            {
                if (Owner == null || zone == null || Cell == null || Cell.ParentZone != zone || Index < 0
                    || Owner.SpatialZone != zone || zone.GetEntityCell(Owner) != Cell || !Cell.Objects.Contains(Owner)
                    || Owner.ID != id || string.IsNullOrEmpty(id) || Owner.BlueprintName != Blueprint
                    || crop == null || crop.ParentEntity != Owner || Owner.GetPart<CropPart>() != crop || !Owner.HasTag("Crop")
                    || crop.GrowthStage != stage || crop.TicksInStage != progress || crop.MoistureTicks != moisture
                    || crop.HarvestAtMaturity != harvest || crop.YieldBlueprint != YieldBlueprint || crop.YieldCount != originalYieldCount
                    || (harvest && (crop.SeedYieldBlueprint != SeedBlueprint || crop.SeedYieldCount != originalSeedCount))
                    || physics == null || Owner.GetPart<PhysicsPart>() != physics || physics.ParentEntity != Owner
                    || physics.Takeable || physics.Solid || physics.InInventory != null || physics.Equipped != null
                    || BarrenGroundRules.IsBarren(Cell)) return false;
                if (actor == null) return !harvest && stage >= 1;
                var actorCell = zone.GetEntityCell(actor);
                return harvest && stage == 2 && ReferenceEquals(SettlementRuntime.ActiveZone, zone)
                    && actor.SpatialZone == zone && actorCell != null && actorCell.Objects.Contains(actor)
                    && !CombatSystem.IsDeathHandled(actor) && (!(actor.GetStat("Hitpoints") is Stat hp) || hp.Value > 0)
                    && Math.Abs(actorCell.X - Cell.X) <= 1 && Math.Abs(actorCell.Y - Cell.Y) <= 1;
            }
        }
    }
}
