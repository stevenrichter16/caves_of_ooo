using System;
using System.Collections.Generic;
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
        internal const string NoReadyFire = "No usable fire within reach.";
        private const string CookAtPrefix = "CookAt|";

        public static bool TryCook(Entity actor, Entity food, Zone zone, EntityFactory factory, InventoryTransaction transaction = null)
            => TryCook(actor, food, zone, factory, transaction, null);

        internal static bool TryCook(Entity actor, Entity food, Zone zone, EntityFactory factory,
            InventoryTransaction transaction, string command)
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
            bool selected = IsCookAt(command);
            StationProof source = selected ? FindSelectedFire(actor, zone, command) : FindStation(actor, zone, command == null);
            if (source == null)
                return Reject(actor, food, "no_fire", selected ? "That fire is no longer usable." : NoReadyFire);
            Entity station = source.Owner;
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, "Cook") || !transaction.TryClaim(food, actor, "Cook")
                    || source.Improvised && !transaction.TryClaim(station, actor, "Cook"))
                    return Reject(actor, food, "in_progress", "That food is already being prepared.");
                var audio=WorldAudioUtility.TakeStation(actor,station,zone,WorldAudioCue.CookPrepare,
                    ()=>source.Current(actor,zone)&&actor.GetPart<InventoryPart>()==inventory
                        &&food.GetPart<CookablePart>()==recipe&&recipe.ParentEntity==food
                        &&food.GetPart<PhysicsPart>()==foodPhysics&&foodPhysics.ParentEntity==food
                        &&!inventory.Objects.Contains(food)&&foodPhysics.InInventory==null&&foodPhysics.Equipped==null
                        &&food.SpatialZone==null&&food.GetPart<StackerPart>()==stack&&(stack==null||stack.StackCount==0));
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
                if (source.Improvised)
                {
                    if (!source.Current(actor, zone))
                    {
                        restore();
                        return Reject(actor, food, "source_changed", "That fire is no longer usable.");
                    }
                    receipt.WatchChangedOwners(transaction);
                    transaction.BeforeCommit(() => source.Current(actor, zone)
                        && inventory.ParentEntity == actor && actor.GetPart<InventoryPart>() == inventory);
                }
                transaction.AfterCommit(()=>audio?.Publish());
                transaction.AfterCommit(() => Diag.Record("event", "FoodCooked", actor, food,
                    new { from = food.BlueprintName, into, count, station = station.BlueprintName }));
                transaction.AfterCommit(() => MessageLog.Add("You cook " + count + " " + InventoryPart.GetUnitDisplayName(food)
                    + (source.Improvised ? " over " + station.GetDisplayName() : "") + "."));
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

        /// <summary>Read the same local heat sources used when cooking commits.</summary>
        public static List<Entity> StationsBeside(Entity actor, Zone zone)
        {
            var found = new List<Entity>();
            if (actor == null || zone?.GetEntityCell(actor) == null) return found;
            foreach (var entity in zone.GetReadOnlyEntitiesWithPart<CampfirePart>())
                if (new StationProof(entity, zone).Current(actor, zone)) found.Add(entity);
            foreach (var source in ImprovisedBeside(actor, zone)) found.Add(source.Owner);
            return found;
        }

        private static StationProof FindStation(Entity actor, Zone zone, bool allowImprovised)
        {
            if (actor == null || zone?.GetEntityCell(actor) == null) return null;
            foreach (var entity in zone.GetReadOnlyEntitiesWithPart<CampfirePart>())
            {
                var source = new StationProof(entity, zone);
                if (source.Current(actor, zone)) return source;
            }
            if (allowImprovised)
                foreach (var source in ImprovisedBeside(actor, zone)) return source;
            return null;
        }

        internal static bool IsCookAt(string command) => command?.StartsWith(CookAtPrefix, StringComparison.Ordinal) == true;

        internal static string CookAt(Zone zone, Entity source)
        {
            var cell = zone.GetEntityCell(source);
            return CookAtPrefix + Uri.EscapeDataString(zone.ZoneID) + "|" + cell.X + "|" + cell.Y + "|" + Uri.EscapeDataString(source.ID);
        }

        private static StationProof FindSelectedFire(Entity actor, Zone zone, string command)
        {
            var fields = command.Split('|');
            if (fields.Length != 5 || WorldResourceActions.Decode(fields[1]) != zone?.ZoneID
                || !int.TryParse(fields[2], out int x) || !int.TryParse(fields[3], out int y)
                || zone?.GetCell(x, y) is not Cell cell) return null;
            string id = WorldResourceActions.Decode(fields[4]);
            if (string.IsNullOrEmpty(id)) return null;
            Entity target = null;
            foreach (var entity in cell.Objects)
            {
                if (entity.ID != id) continue;
                if (target != null) return null;
                target = entity;
            }
            if (target == null) return null;
            var source = new StationProof(target, zone);
            return source.Improvised && source.Current(actor, zone) ? source : null;
        }

        private static IEnumerable<StationProof> ImprovisedBeside(Entity actor, Zone zone)
        {
            if (!WorldResourceActions.ActorCurrent(actor, zone)) yield break;
            var seen = new HashSet<Entity>();
            foreach (var origin in zone.GetOccupiedCells(actor))
            {
                if (origin == null) continue;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var cell = zone.GetCell(origin.X + dx, origin.Y + dy);
                    if (cell?.IsVisible != true || !cell.Explored) continue;
                    foreach (var entity in cell.Occupants)
                    {
                        if (entity.HasPart<CampfirePart>() || !seen.Add(entity) || !entity.HasEffect<BurningEffect>()) continue;
                        var source = new StationProof(entity, zone);
                        if (source.Current(actor, zone)) yield return source;
                    }
                }
            }
        }

        internal static bool VisibleContact(Entity actor, Cell cell)
            => cell?.IsVisible == true && cell.Explored
                && SpatialQuery.DistanceToCell(cell.ParentZone, actor, cell.X, cell.Y) <= 1;

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
            readonly BurningEffect burn;
            readonly MaterialPart material;
            readonly DestructiblePart structure;
            readonly RenderPart render;

            internal bool Improvised => campfire == null;

            internal StationProof(Entity owner, Zone zone)
            {
                Owner = owner; anchor = zone.GetEntityCell(owner);
                campfire = owner.GetPart<CampfirePart>(); physics = owner.GetPart<PhysicsPart>();
                fuel = owner.GetPart<FuelPart>(); thermal = owner.GetPart<ThermalPart>();
                finite = campfire != null && campfire.FiniteCooking;
                if (Improvised)
                {
                    burn = owner.GetEffect<BurningEffect>();
                    material = owner.GetPart<MaterialPart>();
                    structure = owner.GetPart<DestructiblePart>();
                    render = owner.GetPart<RenderPart>();
                }
            }

            internal bool Current(Entity actor, Zone zone)
            {
                if (zone == null || actor == null || actor.SpatialZone != zone || zone.GetEntityCell(actor) == null
                    || Owner.SpatialZone != zone || anchor == null || anchor.ParentZone != zone
                    || !ReferenceEquals(anchor, zone.GetEntityCell(Owner)) || !anchor.Objects.Contains(Owner)
                    || !ReferenceEquals(campfire, Owner.GetPart<CampfirePart>()) || physics == null || physics.ParentEntity != Owner
                    || !ReferenceEquals(physics, Owner.GetPart<PhysicsPart>()) || physics.InInventory != null || physics.Equipped != null
                    || !ReferenceEquals(fuel, Owner.GetPart<FuelPart>()) || (fuel != null && fuel.ParentEntity != Owner)
                    || !ReferenceEquals(thermal, Owner.GetPart<ThermalPart>()) || (thermal != null && thermal.ParentEntity != Owner)
                    || SpatialQuery.Distance(zone, actor, Owner) > 1) return false;
                if (Improvised) return BurningSceneryCurrent(actor, zone);
                return campfire.ParentEntity == Owner && campfire.FiniteCooking == finite
                    && (!finite || (fuel != null && thermal != null && Finite(fuel.FuelMass) && fuel.FuelMass > 0
                    && Finite(thermal.Temperature) && thermal.Temperature >= MinimumFiniteCookingTemperature));
            }

            private bool BurningSceneryCurrent(Entity actor, Zone zone)
            {
                float flame = thermal?.GetEffectiveFlameTemperature() ?? float.NaN;
                if (!WorldResourceActions.Nearby(actor, Owner, zone) || Owner == actor || physics.Takeable
                    || Owner.HasTag("Creature") || Owner.HasPart<BrainPart>() || Owner.HasPart<CorpsePart>()
                    || render?.ParentEntity != Owner || !ReferenceEquals(render, Owner.GetPart<RenderPart>()) || !render.Visible
                    || burn == null || burn.Owner != Owner || !ReferenceEquals(burn, Owner.GetEffect<BurningEffect>())
                    || burn.Duration < Effect.DURATION_INDEFINITE || burn.Duration == 0
                    || !Finite(burn.Intensity) || burn.Intensity <= 0 || Owner.HasEffect<SmolderingEffect>()
                    || material?.ParentEntity != Owner || !ReferenceEquals(material, Owner.GetPart<MaterialPart>())
                    || !Finite(material.Combustibility) || material.Combustibility <= 0
                    || !ReferenceEquals(structure, Owner.GetPart<DestructiblePart>())
                    || structure != null && (structure.ParentEntity != Owner || structure.Gone || structure.IsDestroyed)
                    || !Finite(flame) || !Finite(thermal.Temperature)
                    || thermal.Temperature < Math.Max(MinimumFiniteCookingTemperature, flame)
                    || fuel != null && (!Finite(fuel.FuelMass) || fuel.FuelMass <= 0)) return false;
                foreach (var cell in zone.GetOccupiedCells(Owner))
                    if (VisibleContact(actor, cell)) return true;
                return false;
            }
            static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        }
        private static bool Reject(Entity actor, Entity food, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "CookingRejected", actor, food, new { reason }); return false; }
    }
}
