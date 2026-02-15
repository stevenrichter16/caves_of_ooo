using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates village terrain: open area with rectangular buildings,
    /// a central village square, and biome-appropriate palette.
    /// Priority: NORMAL (2000) — after borders, before connectivity.
    /// </summary>
    public class VillageBuilder : IZoneBuilder
    {
        public string Name => "VillageBuilder";
        public int Priority => 2000;

        private BiomeType _biome;
        private PointOfInterest _poi;

        // Biome palette
        private string _floorBlueprint;
        private string _wallBlueprint;
        private string _pathBlueprint;

        public VillageBuilder(BiomeType biome, PointOfInterest poi)
        {
            _biome = biome;
            _poi = poi;
            SetBiomePalette(biome);
        }

        private void SetBiomePalette(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    _floorBlueprint = "Sand";
                    _wallBlueprint = "SandstoneWall";
                    _pathBlueprint = "Sand";
                    break;
                case BiomeType.Jungle:
                    _floorBlueprint = "Grass";
                    _wallBlueprint = "VineWall";
                    _pathBlueprint = "Grass";
                    break;
                case BiomeType.Ruins:
                    _floorBlueprint = "StoneFloor";
                    _wallBlueprint = "StoneWall";
                    _pathBlueprint = "StoneFloor";
                    break;
                case BiomeType.Cave:
                default:
                    _floorBlueprint = "Floor";
                    _wallBlueprint = "Wall";
                    _pathBlueprint = "Floor";
                    break;
            }
        }

        private struct Room
        {
            public int X, Y, W, H;
            public int CenterX => X + W / 2;
            public int CenterY => Y + H / 2;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // 1. Fill the zone with biome-appropriate floor
            FillWithFloor(zone, factory);

            // 2. Define the village square in the center
            int sqW = 12;
            int sqH = 6;
            int sqX = Zone.Width / 2 - sqW / 2;
            int sqY = Zone.Height / 2 - sqH / 2;

            // 3. Place 3-5 buildings around the square
            int buildingCount = rng.Next(3, 6);
            var buildings = new List<Room>();
            int maxAttempts = buildingCount * 30;

            for (int attempt = 0; attempt < maxAttempts && buildings.Count < buildingCount; attempt++)
            {
                int w = rng.Next(5, 10);
                int h = rng.Next(4, 7);
                int rx = rng.Next(2, Zone.Width - w - 2);
                int ry = rng.Next(2, Zone.Height - h - 2);

                var room = new Room { X = rx, Y = ry, W = w, H = h };

                // Don't overlap square
                if (Overlaps(room, sqX - 1, sqY - 1, sqW + 2, sqH + 2)) continue;
                // Don't overlap other buildings
                if (OverlapsAny(room, buildings)) continue;

                buildings.Add(room);
                BuildRoom(zone, factory, rng, room);
            }

            // 4. Connect buildings to the village square with paths
            foreach (var building in buildings)
            {
                CarvePath(zone, factory, building.CenterX, building.CenterY,
                    sqX + sqW / 2, sqY + sqH / 2);
            }

            return true;
        }

        private void FillWithFloor(Zone zone, EntityFactory factory)
        {
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    // Clear any existing wall/terrain
                    for (int i = cell.Objects.Count - 1; i >= 0; i--)
                    {
                        if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Terrain"))
                            zone.RemoveEntity(cell.Objects[i]);
                    }

                    var floor = factory.CreateEntity(_floorBlueprint);
                    if (floor != null)
                        zone.AddEntity(floor, x, y);
                }
            }
        }

        private void BuildRoom(Zone zone, EntityFactory factory, System.Random rng, Room room)
        {
            // Place walls around the perimeter
            for (int x = room.X; x < room.X + room.W; x++)
            {
                for (int y = room.Y; y < room.Y + room.H; y++)
                {
                    if (!zone.InBounds(x, y)) continue;

                    bool isEdge = (x == room.X || x == room.X + room.W - 1 ||
                                   y == room.Y || y == room.Y + room.H - 1);

                    if (isEdge)
                    {
                        var wall = factory.CreateEntity(_wallBlueprint);
                        if (wall != null)
                            zone.AddEntity(wall, x, y);
                    }
                    else
                    {
                        // Interior gets stone floor
                        ClearAndFloor(zone, factory, x, y, "StoneFloor");
                    }
                }
            }

            // Place a door gap on one side
            int side = rng.Next(4);
            int doorX, doorY;
            switch (side)
            {
                case 0: // North
                    doorX = room.X + rng.Next(1, room.W - 1);
                    doorY = room.Y;
                    break;
                case 1: // South
                    doorX = room.X + rng.Next(1, room.W - 1);
                    doorY = room.Y + room.H - 1;
                    break;
                case 2: // West
                    doorX = room.X;
                    doorY = room.Y + rng.Next(1, room.H - 1);
                    break;
                default: // East
                    doorX = room.X + room.W - 1;
                    doorY = room.Y + rng.Next(1, room.H - 1);
                    break;
            }

            if (zone.InBounds(doorX, doorY))
            {
                // Remove wall at door position
                var cell = zone.GetCell(doorX, doorY);
                for (int i = cell.Objects.Count - 1; i >= 0; i--)
                {
                    if (cell.Objects[i].HasTag("Wall"))
                        zone.RemoveEntity(cell.Objects[i]);
                }
            }
        }

        private void CarvePath(Zone zone, EntityFactory factory, int x1, int y1, int x2, int y2)
        {
            int cx = x1, cy = y1;
            int maxSteps = Zone.Width + Zone.Height;

            for (int step = 0; step < maxSteps; step++)
            {
                if (cx == x2 && cy == y2) break;
                if (!zone.InBounds(cx, cy)) break;

                // Don't carve through building walls -- just place path floor
                var cell = zone.GetCell(cx, cy);
                if (cell != null && !cell.IsWall())
                {
                    // Ensure path floor exists
                    bool hasFloor = false;
                    foreach (var obj in cell.Objects)
                    {
                        if (obj.HasTag("Terrain")) { hasFloor = true; break; }
                    }
                    if (!hasFloor)
                    {
                        var floor = factory.CreateEntity(_pathBlueprint);
                        if (floor != null)
                            zone.AddEntity(floor, cx, cy);
                    }
                }

                // Step toward target
                int dx = x2 - cx;
                int dy = y2 - cy;
                if (Math.Abs(dx) >= Math.Abs(dy))
                    cx += dx > 0 ? 1 : -1;
                else
                    cy += dy > 0 ? 1 : -1;
            }
        }

        private void ClearAndFloor(Zone zone, EntityFactory factory, int x, int y, string floorBP)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Terrain"))
                    zone.RemoveEntity(cell.Objects[i]);
            }
            var floor = factory.CreateEntity(floorBP);
            if (floor != null)
                zone.AddEntity(floor, x, y);
        }

        private bool Overlaps(Room r, int bx, int by, int bw, int bh)
        {
            return r.X < bx + bw && r.X + r.W > bx &&
                   r.Y < by + bh && r.Y + r.H > by;
        }

        private bool OverlapsAny(Room candidate, List<Room> rooms)
        {
            foreach (var r in rooms)
            {
                if (candidate.X - 1 < r.X + r.W && candidate.X + candidate.W + 1 > r.X &&
                    candidate.Y - 1 < r.Y + r.H && candidate.Y + candidate.H + 1 > r.Y)
                    return true;
            }
            return false;
        }
    }
}
