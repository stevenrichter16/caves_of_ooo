using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original founding chamber, after stair repair. Both
    /// chamber outlines and the east-facing root wall are authored, never
    /// inferred from random cave walls. The open sacred gap is cultural
    /// space: villagers stay outside it, but it is not invisible collision.</summary>
    public sealed class FoundingVillageBuilder : IZoneBuilder
    {
        public string Name => "FoundingVillage";
        public int Priority => 3650;
        public const int BodyX = 58;
        public const int RootWallX = 65;
        private const int MainX = 29;
        private const int MainY = 12;
        private static readonly (int x, int y)[] Neighbors = { (0, -1), (0, 1), (-1, 0), (1, 0) };
        private static readonly string[] Required = { "TheRooted", "FoundingPlume", "FoundingListener",
            "FoundingPlaqueTender", "StoneFloor", "SandstoneWall", "NicheHome", "PlaqueWall", "PlaqueOldest", "BeetleJar" };

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null) return false;
            foreach (string bp in Required) if (!factory.Blueprints.ContainsKey(bp)) return Refuse(zone, "missing:" + bp);
            foreach (var e in zone.GetReadOnlyEntities()) if (e.BlueprintName == "TheRooted") return true;
            var stairs = new List<(int x, int y)>();
            foreach (var e in zone.GetReadOnlyEntities())
                if (e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>()) stairs.Add(zone.GetEntityPosition(e));
            int bodyY = -1;
            foreach (int candidate in new[] { 12, 8, 16, 11, 13, 10, 14, 9, 15, 7, 17 })
            {
                bool conflict = false;
                foreach (var stair in stairs)
                    if ((stair.x >= BodyX - 3 && stair.x <= RootWallX && Math.Abs(stair.y - candidate) <= 1)
                        || (stair.x == RootWallX + 1 && Math.Abs(stair.y - candidate) >= 2 && Math.Abs(stair.y - candidate) <= 3))
                    { conflict = true; break; }
                if (!conflict) { bodyY = candidate; break; }
            }
            if (bodyY < 0) return Refuse(zone, "no_sacred_anchor");

            // The compact annex's rounded ellipse foci are x58 and x65.
            // Its east wall turns inward at the second focus and bows out
            // above/below the three-cell-high empty embrace-space.
            var floor = new HashSet<(int x, int y)>();
            for (int x = 1; x < Zone.Width - 1; x++) for (int y = 1; y < Zone.Height - 1; y++)
            {
                bool main = Ellipse(x, y, MainX, MainY, 27, 10);
                int wallX = RootWallX + (Math.Abs(y - bodyY) >= 2 ? 1 : 0);
                bool annex = Ellipse(x, y, 61, bodyY, 7, 6) && x < wallX;
                bool passage = x >= 48 && x <= BodyX && Math.Abs(y - bodyY) <= 1;
                if (main || annex || passage) floor.Add((x, y));
            }
            foreach (var p in floor) ClearFloor(zone, factory, p.x, p.y);
            // An actual shell makes the chamber read the same even on an
            // entirely open base. Stairs are kept, then their approaches
            // are carved through this shell before sacred decoration.
            var shell = new HashSet<(int x, int y)>();
            foreach (var p in floor) foreach (var d in Neighbors)
            {
                var n = (p.x + d.x, p.y + d.y);
                if (!floor.Contains(n) && zone.GetCell(n.Item1, n.Item2) != null) shell.Add(n);
            }
            foreach (var p in shell)
            {
                if (stairs.Contains(p)) continue;
                ClearObjects(zone, p.x, p.y);
                BuilderSpawn.TryPlace(zone, factory, "SandstoneWall", p.x, p.y);
                zone.GenReservedCells.Add(p);
            }
            foreach (var stair in stairs)
            {
                int x = stair.x, y = stair.y;
                // East-side arrivals must go around the empty embrace;
                // no stair approach becomes a road through his fingertips.
                if (x > BodyX && Math.Abs(y - bodyY) <= 1)
                    while (y != bodyY - 3) { ClearFloor(zone, factory, x, y); y--; }
                while (x != MainX) { ClearFloor(zone, factory, x, y); x += Math.Sign(MainX - x); }
                while (y != MainY) { ClearFloor(zone, factory, x, y); y += Math.Sign(MainY - y); }
                ClearFloor(zone, factory, x, y);
            }
            var body = BuilderSpawn.TryPlace(zone, factory, "TheRooted", BodyX, bodyY);
            if (body == null) return Refuse(zone, "body_placement");
            int plume = 0;
            for (int x = BodyX - 3; x <= BodyX; x++) for (int y = bodyY - 1; y <= bodyY + 1; y++)
            {
                if (x == BodyX && y == bodyY) continue;
                if (BuilderSpawn.TryPlace(zone, factory, "FoundingPlume", x, y) != null) plume++;
            }
            for (int x = 8; x <= 45; x += 5)
            {
                int top = MainTop(x);
                PlaceIfClear(zone, factory, "NicheHome", x, top, requireWall: true);
                PlaceIfClear(zone, factory, "NicheHome", x, 2 * MainY - top, requireWall: true);
            }
            PlaceIfClear(zone, factory, "PlaqueWall", 28, 4);
            PlaceIfClear(zone, factory, "PlaqueOldest", 29, 4);
            PlaceIfClear(zone, factory, "BeetleJar", 34, 8);
            PlaceIfClear(zone, factory, "BeetleJar", 34, 16);
            bool listener = PlaceIfClear(zone, factory, "FoundingListener", 36, MainTop(36), requireWall: true);
            bool tender = PlaceIfClear(zone, factory, "FoundingPlaqueTender", 28, 5);
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "FoundingVillageBuilt", body, null,
                new { zone = zone.ZoneID, bodyX = BodyX, bodyY, plume, listener, tender, stairs = stairs.Count });
            return plume == 11 && listener && tender;
        }
        private static int MainTop(int x)
        {
            for (int y = 1; y <= MainY; y++) if (Ellipse(x, y, MainX, MainY, 27, 10)) return y;
            return MainY;
        }
        private static bool Ellipse(int x, int y, int cx, int cy, int rx, int ry)
            => (double)(x - cx) * (x - cx) / (rx * rx) + (double)(y - cy) * (y - cy) / (ry * ry) <= 1;
        private static void ClearObjects(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y); if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                if (!e.HasPart<StairsUpPart>() && !e.HasPart<StairsDownPart>()) zone.RemoveEntity(e);
            }
        }
        private static void ClearFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            if (zone.GetCell(x, y) == null) return;
            ClearObjects(zone, x, y); BuilderSpawn.TryPlace(zone, factory, "StoneFloor", x, y);
            zone.GenReservedCells.Add((x, y));
        }
        private static bool PlaceIfClear(Zone zone, EntityFactory factory, string bp, int x, int y, bool requireWall = false)
        {
            for (int r = 0; r <= 3; r++) for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
            {
                var cell = zone.GetCell(x + dx, y + dy);
                if (cell == null || cell.BlocksMovement() || !zone.GenReservedCells.Contains((cell.X, cell.Y))) continue;
                bool stair = false, wall = false;
                foreach (var e in cell.Objects) if (e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>()) stair = true;
                if (stair) continue;
                foreach (var d in Neighbors)
                {
                    var neighbor = zone.GetCell(cell.X + d.x, cell.Y + d.y);
                    if (neighbor == null) continue;
                    foreach (var e in neighbor.Objects) if (e.HasTag("Wall")) wall = true;
                }
                if (requireWall && !wall) continue;
                return BuilderSpawn.TryPlace(zone, factory, bp, cell.X, cell.Y) != null;
            }
            return false;
        }
        private static bool Refuse(Zone zone, string reason)
        {
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "FoundingVillageRefused", payload: new { zone = zone.ZoneID, reason });
            return false;
        }
    }
}
