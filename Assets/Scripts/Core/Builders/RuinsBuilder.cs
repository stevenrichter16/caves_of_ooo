using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates ruins terrain: rectangular rooms connected by corridors.
    /// Fills with stone walls, carves rooms and connects them with L-shaped passages.
    /// Priority: NORMAL (2000) — after borders, before connectivity.
    /// </summary>
    public class RuinsBuilder : IZoneBuilder
    {
        public string Name => "RuinsBuilder";
        public int Priority => 2000;

        public string StoneFloorBlueprint = "StoneFloor";
        public string StoneWallBlueprint = "StoneWall";
        public string RubbleBlueprint = "Rubble";
        public int MinRooms = 4;
        public int MaxRooms = 8;
        public int MinRoomSize = 4;
        public int MaxRoomSize = 12;

        private struct Room
        {
            public int X, Y, W, H;
            public int CenterX => X + W / 2;
            public int CenterY => Y + H / 2;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // 1. Fill interior with stone walls
            FillWithWalls(zone, factory);

            // 2. Place rooms
            int roomCount = rng.Next(MinRooms, MaxRooms + 1);
            var rooms = new List<Room>();
            int maxAttempts = roomCount * 20;

            for (int attempt = 0; attempt < maxAttempts && rooms.Count < roomCount; attempt++)
            {
                int w = rng.Next(MinRoomSize, MaxRoomSize + 1);
                int h = rng.Next(MinRoomSize, Math.Min(MaxRoomSize + 1, Zone.Height - 3));
                int rx = rng.Next(2, Zone.Width - w - 2);
                int ry = rng.Next(2, Zone.Height - h - 2);

                var room = new Room { X = rx, Y = ry, W = w, H = h };

                if (!OverlapsAny(room, rooms))
                {
                    rooms.Add(room);
                    CarveRoom(zone, factory, room);
                }
            }

            // 3. Connect rooms with L-shaped corridors
            for (int i = 1; i < rooms.Count; i++)
            {
                CarveCorridor(zone, factory, rng, rooms[i - 1], rooms[i]);
            }

            // If we have rooms, also connect last to first for a loop
            if (rooms.Count > 2)
            {
                CarveCorridor(zone, factory, rng, rooms[rooms.Count - 1], rooms[0]);
            }

            return rooms.Count >= 2;
        }

        private void FillWithWalls(Zone zone, EntityFactory factory)
        {
            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell.IsWall()) continue;

                    var wall = factory.CreateEntity(StoneWallBlueprint);
                    if (wall != null)
                        zone.AddEntity(wall, x, y);
                }
            }
        }

        private bool OverlapsAny(Room candidate, List<Room> rooms)
        {
            foreach (var r in rooms)
            {
                // Check overlap with 1-cell padding
                if (candidate.X - 1 < r.X + r.W && candidate.X + candidate.W + 1 > r.X &&
                    candidate.Y - 1 < r.Y + r.H && candidate.Y + candidate.H + 1 > r.Y)
                    return true;
            }
            return false;
        }

        private void CarveRoom(Zone zone, EntityFactory factory, Room room)
        {
            for (int x = room.X; x < room.X + room.W; x++)
            {
                for (int y = room.Y; y < room.Y + room.H; y++)
                {
                    if (!zone.InBounds(x, y)) continue;
                    ClearAndPlaceFloor(zone, factory, x, y);
                }
            }
        }

        private void CarveCorridor(Zone zone, EntityFactory factory, System.Random rng, Room from, Room to)
        {
            int x = from.CenterX;
            int y = from.CenterY;
            int tx = to.CenterX;
            int ty = to.CenterY;

            // Horizontal first, then vertical (or vice versa randomly)
            bool horizontalFirst = rng.Next(2) == 0;

            if (horizontalFirst)
            {
                CarveHorizontal(zone, factory, rng, x, tx, y);
                CarveVertical(zone, factory, rng, tx, y, ty);
            }
            else
            {
                CarveVertical(zone, factory, rng, x, y, ty);
                CarveHorizontal(zone, factory, rng, x, tx, ty);
            }
        }

        private void CarveHorizontal(Zone zone, EntityFactory factory, System.Random rng, int x1, int x2, int y)
        {
            int start = Math.Min(x1, x2);
            int end = Math.Max(x1, x2);
            for (int x = start; x <= end; x++)
            {
                if (!zone.InBounds(x, y)) continue;
                ClearAndPlaceFloor(zone, factory, x, y);
                if (rng.NextDouble() < 0.15)
                    PlaceRubble(zone, factory, x, y);
            }
        }

        private void CarveVertical(Zone zone, EntityFactory factory, System.Random rng, int x, int y1, int y2)
        {
            int start = Math.Min(y1, y2);
            int end = Math.Max(y1, y2);
            for (int y = start; y <= end; y++)
            {
                if (!zone.InBounds(x, y)) continue;
                ClearAndPlaceFloor(zone, factory, x, y);
                if (rng.NextDouble() < 0.15)
                    PlaceRubble(zone, factory, x, y);
            }
        }

        private void ClearAndPlaceFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            // Remove walls and terrain
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Terrain"))
                    zone.RemoveEntity(cell.Objects[i]);
            }

            var floor = factory.CreateEntity(StoneFloorBlueprint);
            if (floor != null)
                zone.AddEntity(floor, x, y);
        }

        private void PlaceRubble(Zone zone, EntityFactory factory, int x, int y)
        {
            var rubble = factory.CreateEntity(RubbleBlueprint);
            if (rubble != null)
                zone.AddEntity(rubble, x, y);
        }
    }
}
