using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Ensures StairsUp and StairsDown within the same zone are connected
    /// by carving a path between them (BFS through walls).
    /// Priority: 3600 (after stair placement).
    /// </summary>
    public class StairConnectorBuilder : IZoneBuilder
    {
        public string Name => "StairConnectorBuilder";
        public int Priority => 3600;

        private string _floorBlueprint;

        public StairConnectorBuilder(string floorBlueprint)
        {
            _floorBlueprint = floorBlueprint;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // Find StairsUp and StairsDown positions
            int upX = -1, upY = -1, downX = -1, downY = -1;

            for (int x = 0; x < Zone.Width && (upX < 0 || downX < 0); x++)
            {
                for (int y = 0; y < Zone.Height && (upX < 0 || downX < 0); y++)
                {
                    var cell = zone.GetCell(x, y);
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        if (upX < 0 && cell.Objects[i].HasTag("StairsUp"))
                        { upX = x; upY = y; }
                        if (downX < 0 && cell.Objects[i].HasTag("StairsDown"))
                        { downX = x; downY = y; }
                    }
                }
            }

            // If both exist, check if they're connected
            if (upX < 0 || downX < 0) return true;

            // Flood fill from StairsUp to see if StairsDown is reachable
            var reachable = ConnectivityBuilder.FloodFill(zone, upX, upY);
            if (reachable[downX, downY]) return true; // Already connected

            // Carve a path from StairsDown toward StairsUp
            CarvePath(zone, factory, downX, downY, upX, upY);

            return true;
        }

        private void CarvePath(Zone zone, EntityFactory factory, int fromX, int fromY, int toX, int toY)
        {
            int cx = fromX;
            int cy = fromY;
            int maxSteps = Zone.Width + Zone.Height;

            for (int step = 0; step < maxSteps; step++)
            {
                if (cx == toX && cy == toY) break;

                ClearAndFloor(zone, factory, cx, cy);

                // Step toward target
                int dx = toX - cx;
                int dy = toY - cy;

                if (System.Math.Abs(dx) >= System.Math.Abs(dy))
                    cx += dx > 0 ? 1 : -1;
                else
                    cy += dy > 0 ? 1 : -1;

                cx = System.Math.Max(0, System.Math.Min(cx, Zone.Width - 1));
                cy = System.Math.Max(0, System.Math.Min(cy, Zone.Height - 1));
            }
        }

        private void ClearAndFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            // Remove walls
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Solid"))
                    zone.RemoveEntity(cell.Objects[i]);
            }

            // Place floor if empty
            if (cell.IsEmpty())
            {
                Entity floor = factory.CreateEntity(_floorBlueprint);
                if (floor != null)
                    zone.AddEntity(floor, x, y);
            }
        }
    }
}
