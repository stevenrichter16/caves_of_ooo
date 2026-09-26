using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Adds one or two visible traps to a lair after guards and containers
    /// occupy their cells. Ruin stamps carry their own authored trap positions.
    /// Reserved cells, the zone edge, stairs, liquids and occupied cells remain
    /// clear, so generation never starts a creature standing on a trap.
    /// </summary>
    public class TrapPlacementBuilder : IZoneBuilder
    {
        public string Name => "TrapPlacementBuilder";
        public int Priority => 4150;
        private const int EdgeMargin = 3;
        private const int MaxTraps = 2;
        private static readonly string[] Blueprints =
            { "SpikeTrap", "FireTrap", "BearTrap", "PressurePlate" };

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null || rng == null)
            {
                RecordSkipped(zone, "InvalidInput");
                return false;
            }

            var available = new List<string>();
            foreach (string blueprint in Blueprints)
                if (factory.Blueprints != null && factory.Blueprints.ContainsKey(blueprint))
                    available.Add(blueprint);
            if (available.Count == 0)
            {
                RecordSkipped(zone, "NoTrapBlueprints");
                return true;
            }

            var cells = new List<Cell>();
            for (int x = EdgeMargin; x < Zone.Width - EdgeMargin; x++)
                for (int y = EdgeMargin; y < Zone.Height - EdgeMargin; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (zone.GenReservedCells.Contains((x, y)) || !IsSafeFloor(cell)) continue;
                    cells.Add(cell);
                }
            if (cells.Count == 0)
            {
                RecordSkipped(zone, "NoSafeCells");
                return true;
            }

            int count = System.Math.Min(1 + rng.Next(MaxTraps), cells.Count);
            for (int i = 0; i < count; i++)
            {
                int index = rng.Next(cells.Count);
                var cell = cells[index];
                cells.RemoveAt(index);
                string blueprint = available[rng.Next(available.Count)];
                var trap = factory.CreateEntity(blueprint);
                if (trap == null)
                {
                    RecordSkipped(zone, "EntityCreationFailed");
                    continue;
                }
                zone.AddEntity(trap, cell.X, cell.Y);
                if (Diag.IsChannelEnabled("worldgen"))
                    Diag.Record("worldgen", "TrapPlaced", target: trap,
                        payload: new { zone = zone.ZoneID, blueprint, x = cell.X, y = cell.Y });
            }
            return true;
        }

        private static bool IsSafeFloor(Cell cell)
        {
            if (cell == null || !cell.IsPassable()) return false;
            foreach (var entity in cell.Objects)
            {
                if (entity == null) continue;
                if (!entity.HasTag("Terrain") || entity.HasTag("Solid")
                    || entity.HasPart<LiquidPoolPart>() || entity.HasPart<TileStateSourcePart>()
                    || entity.HasPart<TriggerOnStepPart>() || entity.HasPart<StairsDownPart>()
                    || entity.HasPart<StairsUpPart>()) return false;
            }
            return true;
        }

        private static void RecordSkipped(Zone zone, string reason)
        {
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "TrapsSkipped", payload: new { zone = zone?.ZoneID, reason });
        }
    }
}
