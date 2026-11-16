using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>A deliberate workshop operation retains one physical station
    /// through callbacks. It cannot switch to another nearby forge at commit.</summary>
    internal static class ForgeWorkReceipt
    {
        internal static bool Current(Zone zone)
        {
            if (zone == null) return false;
            var manager = WorldLocationContext.For(zone);
            return manager == null || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone;
        }
        internal static bool Ready(Entity owner, Zone zone)
        {
            if (owner == null || zone == null) return false;
            var cell = zone?.GetEntityCell(owner);
            if (owner == null || !Current(zone) || cell == null || owner.SpatialZone != zone || !cell.Objects.Contains(owner)
                || cell.ParentZone != zone || zone.GetCell(cell.X,cell.Y) != cell
                || owner.GetPart<ForgePart>()?.ParentEntity != owner || owner.Parts.Count(p => p is ForgePart) != 1
                || RepairablePart.BlocksFunction(owner) || owner.HasPart<DraggedPart>()
                || owner.GetPart<HarvestablePart>()?.Harvested == true
                || owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0)) return false;
            return !FieldWorkRigRules.IsRig(owner) || FieldWorkRigRules.Payload(owner);
        }
        internal static bool Near(Entity actor, Entity owner, Zone zone)
            => actor != null && zone?.GetEntityCell(actor) is Cell cell && actor.SpatialZone == zone && cell.Objects.Contains(actor)
                && cell.ParentZone == zone && zone.GetCell(cell.X,cell.Y) == cell
                && Ready(owner, zone) && SpatialQuery.Distance(zone, actor, owner) <= 1;
        internal static Entity Resolve(Entity actor, Zone zone)
        {
            if (actor == null || zone == null) return null;
            var at = zone?.GetEntityCell(actor); if (at == null || actor?.SpatialZone != zone || !Current(zone)) return null;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            {
                var cell = zone.GetCell(at.X + dx, at.Y + dy); if (cell == null) continue;
                foreach (var owner in cell.Objects) if (Ready(owner, zone)) return owner;
            }
            return null;
        }
        internal static bool Begin(Entity actor, Zone zone, InventoryTransaction tx, string action, Entity selected = null)
        {
            var station = selected ?? Resolve(actor, zone);
            if (tx == null || !Near(actor, station, zone) || !tx.TryClaim(actor, actor, action) || !tx.TryClaim(station, actor, action)) return false;
            var actorCell = zone.GetEntityCell(actor); var stationCell = zone.GetEntityCell(station);
            string id = station.ID, blueprint = station.BlueprintName; var parts = station.Parts.ToArray();
            tx.BeforeCommit(() => station.ID == id && station.BlueprintName == blueprint && station.Parts.SequenceEqual(parts)
                && zone.GetEntityCell(actor) == actorCell && zone.GetEntityCell(station) == stationCell
                && Near(actor, station, zone) && !CombatSystem.IsDeathHandled(actor)
                && (actor.GetStat("Hitpoints") == null || actor.GetStatValue("Hitpoints") > 0)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true);
            return true;
        }
    }
}
