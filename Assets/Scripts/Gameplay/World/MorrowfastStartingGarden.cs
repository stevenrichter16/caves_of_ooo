using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Initializes the six authored starter-soil cells when a fresh game is
    /// created. It never replaces terrain, and is not a loaded-zone migration.
    /// </summary>
    internal static class MorrowfastStartingGarden
    {
        private static readonly (int x, int y)[] Cells = {
            (41, 21), (41, 22), (41, 23), (42, 21), (42, 22), (42, 23) };

        /// <summary>
        /// Returns six when every designated cell is valid and plantable, or
        /// zero without changing any tag when validation refuses the patch.
        /// Existing actor occupancy does not redefine the authored soil.
        /// </summary>
        internal static int Ensure(Zone zone)
        {
            if (!MorrowfastSceneRuntime.IsActive(zone)) return Reject(zone, "not_authored_morrowfast");
            var definition = MorrowfastSceneDefinition.Load();
            var terrain = new Entity[Cells.Length];
            for (int i = 0; i < Cells.Length; ++i)
            {
                var p = Cells[i];
                MorrowfastSceneDefinition.CellSpec authored = null;
                foreach (var spec in definition.cells)
                    if (spec.x == p.x && spec.y == p.y) { authored = spec; break; }
                if (authored == null || authored.solid || authored.opaque || authored.water || authored.interior
                    || definition.RoomAt(p.x, p.y) != null)
                    return Reject(zone, "unsafe_authored_cell");
                foreach (var owner in definition.owners)
                {
                    if (owner.anchorX == p.x && owner.anchorY == p.y) return Reject(zone, "authored_owner");
                    foreach (var point in owner.footprint)
                        if (point.x == p.x && point.y == p.y) return Reject(zone, "authored_owner");
                }
                foreach (var resident in MorrowfastContent.AdditionalResidents)
                    if (resident.X == p.x && resident.Y == p.y) return Reject(zone, "authored_resident");

                var cell = zone.GetCell(p.x, p.y);
                if (cell == null || cell.IsInterior || BarrenGroundRules.IsBarren(cell)
                    || MorrowfastSceneRuntime.BlockingOwner(cell) != null)
                    return Reject(zone, "unsafe_current_cell");
                foreach (var entity in cell.Objects)
                {
                    if (entity == null) return Reject(zone, "invalid_membership");
                    if (entity.HasPart<LiquidPoolPart>()) return Reject(zone, "water");
                    // Actors can stand on soil. A relocated static owner or
                    // solid prop is a different geometry and must be refused.
                    if (!entity.HasTag("Creature") && (entity.HasPart<MorrowfastPropPart>()
                        || entity.HasTag("Solid") || entity.GetPart<PhysicsPart>()?.Solid == true))
                        return Reject(zone, "occupied_static_cell");
                    if (!entity.HasTag("Terrain")) continue;
                    var physics = entity.GetPart<PhysicsPart>();
                    if (terrain[i] != null || !entity.HasTag(MorrowfastSceneRuntime.TerrainTag)
                        || entity.HasTag("Solid") || entity.HasTag("Wall") || physics == null || physics.Solid
                        || entity.BlueprintName != "TepuiStone" || entity.ID != "morrowfast-terrain:" + p.x + ":" + p.y
                        || zone.GetEntityCell(entity) != cell)
                        return Reject(zone, "invalid_authored_terrain");
                    terrain[i] = entity;
                }
                if (terrain[i] == null) return Reject(zone, "missing_authored_terrain");
            }

            // All six references are valid before the first write. SetTag is a
            // dictionary write with no event callbacks; resident tag indexes
            // need the explicit notification without remove/re-add side effects.
            int changed = 0;
            foreach (var entity in terrain)
            {
                if (!entity.HasTag("Plantable")) { entity.SetTag("Plantable"); ++changed; }
                zone.NotifyEntityTagAdded(entity, "Plantable");
            }
            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "MorrowfastGardenPrepared", payload: new { zoneId = zone.ZoneID, cells = Cells.Length, changed });
            return Cells.Length;
        }

        private static int Reject(Zone zone, string reason)
        {
            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "MorrowfastGardenRejected", payload: new { zoneId = zone?.ZoneID, reason });
            return 0;
        }
    }
}
