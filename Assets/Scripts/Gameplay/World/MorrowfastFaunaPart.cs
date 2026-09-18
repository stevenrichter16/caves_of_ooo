using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A local preference for the two authored animals' voluntary movement.
    /// Their ordinary brain, health, combat, turn costs and forced movement remain native.
    /// Only ComponentId is saved; zone and geometry are always read from the real owner.
    /// </summary>
    public sealed class MorrowfastFaunaPart : Part
    {
        public override string Name => "MorrowfastFauna";
        public string ComponentId = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "BeforeMove" || ParentEntity == null) return true;
            bool frog = ComponentId == "western-bank-frog", tortoise = ComponentId == "southern-tortoise";
            if (!frog && !tortoise) return true;

            var source = e.GetParameter<Cell>("SourceCell");
            var zone = ParentEntity.GetPart<BrainPart>()?.CurrentZone ?? source?.ParentZone;
            // Copying this part onto an unrelated creature must not impose a settlement rule on wildlife.
            if (!MorrowfastSceneRuntime.IsActive(zone) || MorrowfastSceneRuntime.FindOwner(zone, ComponentId) != ParentEntity) return true;
            var actual = zone.GetEntityCell(ParentEntity);
            var target = e.GetParameter<Cell>("TargetCell");
            if (e.GetParameter<Entity>("Actor") != ParentEntity || actual == null || source != actual
                || source.ParentZone != zone || !source.Objects.Contains(ParentEntity)
                || target == null || target.ParentZone != zone || zone.GetCell(target.X, target.Y) != target)
                return Refuse(e);

            return AllowedCell(zone, target, frog) || Refuse(e);
        }

        /// <summary>
        /// Add the missing local rule to pre-habitat saves. Existing guarded animals, including
        /// animals displaced by combat, are untouched. A blocked repair is retryable and never
        /// replaces an actor, changes its inventory/health, or forces an occupied destination.
        /// </summary>
        public static bool EnsureHabitat(Zone zone)
        {
            if (!MorrowfastSceneRuntime.IsActive(zone)) return false;
            bool complete = true;
            foreach (string id in new[] { "western-bank-frog", "southern-tortoise" })
            {
                var owner = MorrowfastSceneRuntime.FindOwner(zone, id);
                if (owner == null || owner.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(owner)) continue;
                var part = owner.GetPart<MorrowfastFaunaPart>();
                if (part != null && part.ComponentId == id) continue;
                var current = zone.GetEntityCell(owner); bool frog = id == "western-bank-frog";
                if (!FreeHome(current, owner, zone, frog))
                {
                    var anchor = MorrowfastSceneRuntime.GetAuthoredAnchor(id);
                    Cell destination = null; int best = int.MaxValue;
                    int minX = frog ? 21 : 35, maxX = frog ? 27 : 41, minY = frog ? 15 : 21, maxY = frog ? 23 : 24;
                    for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                    {
                        int distance = Math.Abs(x - anchor.x) + Math.Abs(y - anchor.y);
                        if (distance >= best) continue;
                        var candidate = zone.GetCell(x, y);
                        if (FreeHome(candidate, owner, zone, frog)) { destination = candidate; best = distance; }
                    }
                    if (destination == null || !zone.MoveEntity(owner, destination.X, destination.Y)) { complete = false; continue; }
                    ZoneRenderHooks.MarkCellDirty(current.X, current.Y, "MorrowfastHabitatUpgrade");
                    ZoneRenderHooks.MarkCellDirty(destination.X, destination.Y, "MorrowfastHabitatUpgrade");
                }
                if (part == null) owner.AddPart(new MorrowfastFaunaPart { ComponentId = id });
                else part.ComponentId = id;
            }
            return complete;
        }
        private static bool FreeHome(Cell cell, Entity owner, Zone zone, bool frog)
        {
            if (!AllowedCell(zone, cell, frog) || cell.BlocksMovement(owner)) return false;
            foreach (var other in cell.Objects) if (other != owner && !other.HasTag(MorrowfastSceneRuntime.TerrainTag)) return false;
            return true;
        }
        private static bool AllowedCell(Zone zone, Cell target, bool frog)
        {
            if (target == null || target.ParentZone != zone) return false;
            int minX = frog ? 21 : 35, maxX = frog ? 27 : 41, minY = frog ? 15 : 21, maxY = frog ? 23 : 24;
            if (target.X < minX || target.X > maxX || target.Y < minY || target.Y > maxY) return false;
            var definition = MorrowfastSceneDefinition.Load();
            if (target.IsInterior || definition.RoomAt(target.X, target.Y) != null) return false;
            // Keep the ordinary south-arrival spine open even when Mossfoot pauses between steps.
            if (target.X == 40 && target.Y >= 21) return false;
            foreach (var building in definition.buildings)
            {
                if (Near(target, building.entryX, building.entryY)) return false;
                var door = definition.FindOwner(building.doorId);
                foreach (var point in door.footprint) if (Near(target, point.x, point.y)) return false;
            }
            // The frog may sit beside the creek, but its wandering should not seal the only footbridge approach.
            foreach (var owner in definition.owners)
                if (owner.kind == "bridge") foreach (var point in owner.bridgeSupport)
                    if (Near(target, point.x, point.y)) return false;
            return true;
        }
        private static bool Near(Cell cell, int x, int y) => Math.Abs(cell.X - x) <= 1 && Math.Abs(cell.Y - y) <= 1;
        private static bool Refuse(GameEvent e) { e.SetParameter("Blocked", true); e.SetParameter("BlockedReason", "MorrowfastHabitat"); return false; }
    }
}
