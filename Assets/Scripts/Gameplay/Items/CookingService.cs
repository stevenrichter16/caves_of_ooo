using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One carried stack becomes the same quantity of cooked food atomically.</summary>
    public static class CookingService
    {
        public static bool TryCook(Entity actor, Entity food, Zone zone, EntityFactory factory, InventoryTransaction transaction = null)
        {
            if (CombatSystem.IsDeathHandled(actor) || (actor?.GetStat("Hitpoints") is Stat hp && hp.Value <= 0))
                return Reject(actor, food, "actor_dead", "You cannot cook while dead.");
            var inventory = actor?.GetPart<InventoryPart>();
            var recipe = food?.GetPart<CookablePart>();
            var stack = food?.GetPart<StackerPart>();
            int count = stack?.StackCount ?? 1;
            if (inventory == null || !inventory.CanConsumeOne(food) || recipe == null || string.IsNullOrEmpty(recipe.Into)
                || count <= 0 || count > (stack?.MaxStack ?? 1))
                return Reject(actor, food, "unavailable_food", "You must carry a usable stack of raw food.");
            if (factory == null) return Reject(actor, food, "no_factory", "That food cannot be prepared right now.");
            Entity station = FindStation(actor, zone);
            if (station == null) return Reject(actor, food, "no_fire", "Stand beside a campfire, hearth, or stove to cook.");
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, "Cook") || !transaction.TryClaim(food, actor, "Cook"))
                    return Reject(actor, food, "in_progress", "That food is already being prepared.");
                string into = recipe.Into;
                var products = new Entity[count];
                for (int i = 0; i < count; i++)
                {
                    products[i] = factory.CreateEntity(into);
                    var output = products[i];
                    if (output?.GetPart<FoodPart>() == null || (output.GetPart<StackerPart>()?.StackCount ?? 1) != 1
                        || output.GetPart<PhysicsPart>()?.InInventory != null || output.GetPart<PhysicsPart>()?.Equipped != null)
                        return Reject(actor, food, "invalid_output", "That recipe has no usable cooked food.");
                }
                // Part initialization can call gameplay. Recheck before consuming anything.
                if (!ReferenceEquals(inventory, actor.GetPart<InventoryPart>()) || !inventory.CanConsumeOne(food)
                    || (stack?.StackCount ?? 1) != count || recipe.Into != into || SpatialQuery.Distance(zone, actor, station) > 1)
                    return Reject(actor, food, "source_changed", "The food or cooking place is no longer available.");
                var receipt = InventoryTransferSnapshot.Capture(inventory, products);
                bool restored = false;
                Action restore = () =>
                {
                    if (restored) return;
                    restored = true;
                    receipt.Restore();
                };
                transaction.Do(null, restore);
                bool applied = receipt.Apply(() =>
                {
                    if (!inventory.RemoveObject(food)) return false;
                    if (stack != null) stack.StackCount = 0;
                    foreach (var output in products) if (!inventory.AddCraftedUnit(output, out _)) return false;
                    return inventory.MaxWeight < 0 || inventory.GetCarriedWeight() <= inventory.MaxWeight;
                });
                if (!applied || !receipt.ClaimChanges(transaction, actor, "Cook"))
                { restore(); return Reject(actor, food, "capacity_or_transfer", "You cannot carry the cooked result; the raw food is unchanged."); }
                transaction.AfterCommit(() => Diag.Record("event", "FoodCooked", actor, food,
                    new { from = food.BlueprintName, into, count, station = station.BlueprintName }));
                transaction.AfterCommit(() => MessageLog.Add("You cook " + count + " " + InventoryPart.GetUnitDisplayName(food) + "."));
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }
        private static Entity FindStation(Entity actor, Zone zone)
        {
            if (zone?.GetEntityCell(actor) == null) return null;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.HasPart<CampfirePart>() && SpatialQuery.Distance(zone, actor, entity) <= 1) return entity;
            return null;
        }
        private static bool Reject(Entity actor, Entity food, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "CookingRejected", actor, food, new { reason }); return false; }
    }
}
