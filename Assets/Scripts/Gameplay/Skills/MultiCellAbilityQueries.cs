using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>Cast-local physical contacts. Material and damage passes each
    /// take their own owner snapshot; no owner is hit twice by one pulse.</summary>
    internal static class MultiCellAbilityQueries
    {
        internal static List<Cell> RadiusCells(Zone zone, int x, int y, int radius)
        {
            var cells = new List<Cell>();
            if (zone == null || radius < 0) return cells;
            for (int cy = Math.Max(0, y - radius); cy <= Math.Min(Zone.Height - 1, y + radius); cy++)
                for (int cx = Math.Max(0, x - radius); cx <= Math.Min(Zone.Width - 1, x + radius); cx++)
                    cells.Add(zone.GetCell(cx, cy));
            return cells;
        }

        internal static List<Entity> SnapshotOccupants(IEnumerable<Cell> cells,
            Entity exclude = null, bool reverse = false)
        {
            var result = new List<Entity>();
            var seen = new HashSet<Entity>();
            foreach (var cell in cells)
            {
                if (cell == null) continue;
                var occupants = cell.Occupants;
                // Keep the legacy forward/reverse ordering within ordinary cells.
                for (int n = 0; n < occupants.Count; n++)
                {
                    var entity = occupants[reverse ? occupants.Count - 1 - n : n];
                    if (entity != null && entity != exclude && seen.Add(entity)) result.Add(entity);
                }
            }
            return result;
        }

        internal static List<Cell> AdjacentCells(Zone zone, Entity actor)
        {
            var result = new List<Cell>();
            var ownCells = new HashSet<Cell>(zone.GetOccupiedCells(actor));
            var seen = new HashSet<Cell>();
            foreach (var origin in zone.GetOccupiedCells(actor))
            {
                if (origin == null) continue;
                for (int dir = 0; dir < 8; dir++)
                {
                    var cell = zone.GetCellInDirection(origin.X, origin.Y, dir);
                    if (cell != null && !ownCells.Contains(cell) && seen.Add(cell)) result.Add(cell);
                }
            }
            return result;
        }

        internal static List<Entity> AdjacentCreatures(Zone zone, Entity actor)
        {
            var result = new List<Entity>();
            var seen = new HashSet<Entity>();
            foreach (var cell in AdjacentCells(zone, actor))
                foreach (var entity in cell.Occupants)
                {
                    if (!AbilityTargeting.IsCreatureTarget(entity, actor)) continue;
                    if (seen.Add(entity)) result.Add(entity);
                    break; // Preserve the original first-creature-per-cell rule.
                }
            return result;
        }
        internal static Entity FirstAdjacentCreature(Zone zone, Entity actor, out Cell contact)
        {
            foreach (var cell in AdjacentCells(zone, actor))
                foreach (var entity in cell.Occupants)
                    if (AbilityTargeting.IsCreatureTarget(entity, actor))
                    { contact = cell; return entity; }
            contact = null;
            return null;
        }

        internal static int ContactDirection(Zone zone, Entity actor, Cell contact)
        {
            if (contact == null) return -1;
            var origin = SpatialQuery.ClosestCell(zone, actor, contact.X, contact.Y);
            if (origin == null) return -1;
            for (int dir = 0; dir < 8; dir++)
                if (zone.GetCellInDirection(origin.X, origin.Y, dir) == contact) return dir;
            return -1;
        }

        internal static Entity CreatureAtPlacement(Zone zone, Entity actor, int x, int y)
        {
            foreach (var cell in zone.GetOccupiedCells(actor, x, y))
            {
                if (cell == null) continue;
                foreach (var entity in cell.Occupants)
                    if (AbilityTargeting.IsCreatureTarget(entity, actor)) return entity;
            }
            return null;
        }
    }
}
