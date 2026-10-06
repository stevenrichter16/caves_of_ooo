using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class FieldHarvestService
    {
        internal static bool TryHarvest(Entity actor, FieldHarvestPart harvest, Zone zone, InventoryTransaction tx)
        {
            if (actor == null || harvest?.ParentEntity == null || zone == null)
                return WorldResourceActions.Reject(actor, harvest?.ParentEntity, "Harvest", "invalid_harvest_context");
            var owner = harvest.ParentEntity; var cell = zone?.GetEntityCell(owner); var physical = owner?.GetPart<PhysicsPart>();
            string blueprint = harvest?.YieldBlueprint, id = owner?.ID; int count = harvest?.YieldCount ?? 0;
            bool Current() => harvest != null && !harvest.Harvested && owner.GetPart<FieldHarvestPart>() == harvest && harvest.ParentEntity == owner
                && harvest.YieldBlueprint == blueprint && harvest.YieldCount == count && owner.ID == id
                && WorldResourceActions.Nearby(actor, owner, zone) && zone.GetEntityCell(owner) == cell
                && owner.GetPart<PhysicsPart>() == physical && !physical.Takeable && !owner.HasTag("Creature");
            var factory = HarvestablePart.Factory;
            if (!Current() || factory == null || count < 1 || count > 32 || string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint)) return WorldResourceActions.Reject(actor, owner, "Harvest", "invalid_source_or_yield");
            bool own = tx == null; tx ??= new InventoryTransaction();
            var products = new List<Entity>(); var ids = new HashSet<string>(StringComparer.Ordinal); var receipts = new List<InventoryTransferSnapshot>();
            var render = owner.GetPart<RenderPart>(); var examine = owner.GetPart<ExaminablePart>();
            string name = render?.DisplayName, glyph = render?.RenderString, color = render?.ColorString, description = examine?.Text;
            bool changed = false;
            bool Fresh(Entity product) => CropYieldService.Fresh(product, blueprint) && !zone.GetReadOnlyEntities().Any(e => e.ID == product.ID)
                && actor.GetPart<InventoryPart>()?.Objects.Any(e => e.ID == product.ID) != true;
            try
            {
                if (!tx.TryClaim(actor, actor, "Harvest") || !tx.TryClaim(owner, actor, "Harvest")) return false;
                for (int i = 0; i < count; i++)
                {
                    var product = factory.CreateEntity(blueprint);
                    if (!Current() || !Fresh(product) || !ids.Add(product.ID)) return WorldResourceActions.Reject(actor, owner, "Harvest", "changed_source_or_invalid_output");
                    products.Add(product);
                }
                ids.Clear(); foreach (var product in products) if (!Fresh(product) || !ids.Add(product.ID)) return WorldResourceActions.Reject(actor, owner, "Harvest", "changed_source_or_invalid_output");
                tx.Do(null, () =>
                {
                    for (int i = receipts.Count - 1; i >= 0; i--) receipts[i].Restore();
                    foreach (var product in products) if (product.SpatialZone == zone) zone.RemoveEntity(product);
                    if (changed) { harvest.Harvested = false; if (render != null) { render.DisplayName = name; render.RenderString = glyph; render.ColorString = color; } if (examine != null) examine.Text = description; }
                    ZoneRenderHooks.MarkCellDirty(cell, "FieldHarvestRollback");
                });
                foreach (var product in products) if (!Current() || !zone.AddEntity(product, cell.X, cell.Y)) return WorldResourceActions.Reject(actor, owner, "Harvest", "output_placement_failed");
                harvest.SetStubble(); changed = true; int packed = 0; var pack = actor.GetPart<InventoryPart>();
                foreach (var product in products)
                {
                    if (pack == null) break;
                    var receipt = InventoryTransferSnapshot.Capture(pack, product); receipts.Add(receipt);
                    if (!receipt.Apply(() => pack.AddCraftedUnitWithinCapacity(product, out _))) { receipt.Restore(); continue; }
                    if (!receipt.ClaimChanges(tx, actor, "Harvest")) return false;
                    zone.RemoveEntity(product); packed++;
                }
                tx.AfterCommit(() => { ZoneRenderHooks.MarkCellDirty(cell, "FieldHarvested"); MessageLog.Add("You gather the grain: " + packed + " packed, " + (count - packed) + " left on the cut row."); });
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
