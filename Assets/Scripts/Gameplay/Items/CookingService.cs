using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One carried stack becomes the same quantity of cooked food atomically.</summary>
    public static class CookingService
    {
        // New opt-in stored-heat cooking contract; legacy authored stations ignore it.
        public const float MinimumFiniteCookingTemperature = 150f;

        public static bool TryCook(Entity actor, Entity food, Zone zone, EntityFactory factory, InventoryTransaction transaction = null)
        {
            if (CombatSystem.IsDeathHandled(actor) || (actor?.GetStat("Hitpoints") is Stat hp && hp.Value <= 0))
                return Reject(actor, food, "actor_dead", "You cannot cook while dead.");
            var inventory = actor?.GetPart<InventoryPart>();
            var recipe = food?.GetPart<CookablePart>();
            var stack = food?.GetPart<StackerPart>();
            var foodPhysics = food?.GetPart<PhysicsPart>();
            int count = stack?.StackCount ?? 1;
            if (!CurrentFood(actor, food, inventory, recipe, stack, foodPhysics) || string.IsNullOrEmpty(recipe.Into)
                || count <= 0 || count > (stack?.MaxStack ?? 1))
                return Reject(actor, food, "unavailable_food", "You must carry a usable stack of raw food.");
            if (factory == null) return Reject(actor, food, "no_factory", "That food cannot be prepared right now.");
            StationProof source = FindStation(actor, zone);
            if (source == null) return Reject(actor, food, "no_fire", "Stand beside a campfire, hearth, or stove to cook.");
            Entity station = source.Owner;
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
                if (!CurrentFood(actor, food, inventory, recipe, stack, foodPhysics)
                    || (stack?.StackCount ?? 1) != count || count > (stack?.MaxStack ?? 1) || recipe.Into != into
                    || CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat currentHp && currentHp.Value <= 0)
                    || !source.Current(actor, zone))
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
        private static bool CurrentFood(Entity actor, Entity food, InventoryPart inventory,
            CookablePart recipe, StackerPart stack, PhysicsPart physics)
        {
            return actor != null && food != null && inventory != null && inventory.ParentEntity == actor
                && ReferenceEquals(inventory, actor.GetPart<InventoryPart>()) && inventory.CanConsumeOne(food)
                && recipe != null && recipe.ParentEntity == food && ReferenceEquals(recipe, food.GetPart<CookablePart>())
                && ReferenceEquals(stack, food.GetPart<StackerPart>()) && (stack == null || stack.ParentEntity == food)
                && physics != null && physics.ParentEntity == food && ReferenceEquals(physics, food.GetPart<PhysicsPart>())
                && physics.InInventory == actor && physics.Equipped == null && food.SpatialZone == null;
        }

        private static StationProof FindStation(Entity actor, Zone zone)
        {
            if (zone?.GetEntityCell(actor) == null) return null;
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (!entity.HasPart<CampfirePart>()) continue;
                var source = new StationProof(entity, zone);
                if (source.Current(actor, zone)) return source;
            }
            return null;
        }

        // Capture the chosen source before output factories run. A different
        // adjacent station, replacement part, or moved source cannot inherit its permission.
        private sealed class StationProof
        {
            internal readonly Entity Owner;
            readonly Cell anchor;
            readonly CampfirePart campfire;
            readonly PhysicsPart physics;
            readonly FuelPart fuel;
            readonly ThermalPart thermal;
            readonly bool finite;

            internal StationProof(Entity owner, Zone zone)
            {
                Owner = owner; anchor = zone.GetEntityCell(owner);
                campfire = owner.GetPart<CampfirePart>(); physics = owner.GetPart<PhysicsPart>();
                fuel = owner.GetPart<FuelPart>(); thermal = owner.GetPart<ThermalPart>();
                finite = campfire != null && campfire.FiniteCooking;
            }

            internal bool Current(Entity actor, Zone zone)
            {
                if (zone == null || actor == null || actor.SpatialZone != zone || zone.GetEntityCell(actor) == null
                    || Owner.SpatialZone != zone || anchor == null || anchor.ParentZone != zone
                    || !ReferenceEquals(anchor, zone.GetEntityCell(Owner)) || !anchor.Objects.Contains(Owner)
                    || campfire == null || campfire.ParentEntity != Owner || !ReferenceEquals(campfire, Owner.GetPart<CampfirePart>())
                    || campfire.FiniteCooking != finite || physics == null || physics.ParentEntity != Owner
                    || !ReferenceEquals(physics, Owner.GetPart<PhysicsPart>()) || physics.InInventory != null || physics.Equipped != null
                    || !ReferenceEquals(fuel, Owner.GetPart<FuelPart>()) || (fuel != null && fuel.ParentEntity != Owner)
                    || !ReferenceEquals(thermal, Owner.GetPart<ThermalPart>()) || (thermal != null && thermal.ParentEntity != Owner)
                    || SpatialQuery.Distance(zone, actor, Owner) > 1) return false;
                // This is a new opt-in rule. Legacy authored stations retain
                // their established usability, including cold or thermalless hearths.
                return !finite || (fuel != null && thermal != null && Finite(fuel.FuelMass) && fuel.FuelMass > 0
                    && Finite(thermal.Temperature) && thermal.Temperature >= MinimumFiniteCookingTemperature);
            }
            static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        }
        private static bool Reject(Entity actor, Entity food, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "CookingRejected", actor, food, new { reason }); return false; }
    }
}
