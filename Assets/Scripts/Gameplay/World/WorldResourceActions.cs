using System;
using System.Linq;
namespace CavesOfOoo.Core
{
    /// <summary>Pure ownership and command predicates for nearby resource work.</summary>
    public static class WorldResourceActions
    {
        public static bool IsCommand(string command) => command == "ClearCrop" || command == "GatherCrop"
            || command == "FeedCookingFire" || command == "RelightCookingFire" || command == "PrepareFieldBed"
            || command != null && (command.StartsWith("PlantSeedAt|", StringComparison.Ordinal)
                || command.StartsWith("TransferWater|", StringComparison.Ordinal) || command.StartsWith("PourOneLiquidVessel|", StringComparison.Ordinal)
                || command.StartsWith("DouseWorld|", StringComparison.Ordinal) || command.StartsWith("DryGear|", StringComparison.Ordinal));
        internal static bool ActorCurrent(Entity actor, Zone zone)
        {
            if (actor == null || zone == null) return false;
            var physics = actor.GetPart<PhysicsPart>(); var cell = zone.GetEntityCell(actor);
            return actor != null && actor.SpatialZone == zone && cell?.ParentZone == zone && cell.Objects.Contains(actor)
                && (!actor.HasPart<SpatialFootprintPart>() || zone.IsFootprintCurrent(actor)) && physics?.ParentEntity == actor && physics.InInventory == null && physics.Equipped == null
                && !CombatSystem.IsDeathHandled(actor) && (!(actor.GetStat("Hitpoints") is Stat hp) || hp.Value > 0)
                && !(actor.GetEffect<FrozenEffect>()?.Cold > 0);
        }
        internal static bool Ground(Entity owner, Zone zone)
        {
            if (owner == null || zone == null) return false;
            var physics = owner.GetPart<PhysicsPart>(); var cell = zone.GetEntityCell(owner);
            return owner != null && !string.IsNullOrEmpty(owner.ID) && owner.SpatialZone == zone && cell?.ParentZone == zone
                && cell.Objects.Contains(owner) && (!owner.HasPart<SpatialFootprintPart>() || zone.IsFootprintCurrent(owner)) && physics?.ParentEntity == owner
                && physics.InInventory == null && physics.Equipped == null;
        }
        internal static bool Nearby(Entity actor, Entity owner, Zone zone) => ActorCurrent(actor, zone) && Ground(owner, zone)
            && SpatialQuery.Distance(zone, actor, owner) <= 1;
        internal static bool Carried(Entity actor, Entity item, bool single = true)
        {
            if (actor == null || item == null) return false;
            var pack = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>(); var stack = item?.GetPart<StackerPart>();
            return item != null && !string.IsNullOrEmpty(item.ID) && pack?.ParentEntity == actor && item.SpatialZone == null
                && pack.Objects.Count(e => e == item || e?.ID == item.ID) == 1 && physics?.ParentEntity == item && physics.Takeable
                && physics.InInventory == actor && physics.Equipped == null && !pack.EquippedItems.ContainsValue(item)
                && pack.FindEquippedBodyPart(item) == null && (stack == null || stack.ParentEntity == item && (single ? stack.StackCount == 1 : stack.StackCount > 0));
        }
        internal static Entity ExactGround(Zone zone, string escapedId)
        {
            string id = Decode(escapedId); if (string.IsNullOrEmpty(id) || zone == null) return null;
            var matches = zone.GetReadOnlyEntities().Where(e => e.ID == id).ToArray(); return matches.Length == 1 ? matches[0] : null;
        }
        internal static Entity ExactCarried(Entity actor, string escapedId)
        {
            string id = Decode(escapedId); var pack = actor?.GetPart<InventoryPart>(); if (string.IsNullOrEmpty(id) || pack == null) return null;
            var matches = pack.Objects.Where(e => e?.ID == id).ToArray(); return matches.Length == 1 ? matches[0] : null;
        }
        internal static string Decode(string text) { try { return Uri.UnescapeDataString(text ?? ""); } catch (UriFormatException) { return null; } }
        internal static bool Reject(Entity actor, Entity target, string command, string reason)
        {
            CavesOfOoo.Diagnostics.Diag.Record("event", "WorldResourceRejected", actor: actor, target: target,
                payload: new { command, reason });
            return false;
        }
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
