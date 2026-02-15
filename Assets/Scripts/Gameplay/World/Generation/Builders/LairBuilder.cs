using System;
using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Generates lair terrain: biome-appropriate walls with a central boss chamber
    /// and 2-3 side rooms connected by L-shaped corridors.
    /// Follows RuinsBuilder's room/corridor pattern.
    /// Priority: NORMAL (2000) — after borders, before connectivity.
    /// </summary>
    public class LairBuilder : IZoneBuilder
    {
        public string Name => "LairBuilder";
        public int Priority => 2000;

        private BiomeType _biome;
        private PointOfInterest _poi;
        private string _wallBlueprint;
        private string _floorBlueprint;

        public LairBuilder(BiomeType biome, PointOfInterest poi)
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
                    _wallBlueprint = "SandstoneWall";
                    _floorBlueprint = "Sand";
                    break;
                case BiomeType.Jungle:
                    _wallBlueprint = "VineWall";
                    _floorBlueprint = "Grass";
                    break;
                case BiomeType.Ruins:
                    _wallBlueprint = "StoneWall";
                    _floorBlueprint = "StoneFloor";
                    break;
                case BiomeType.Cave:
                default:
                    _wallBlueprint = "Wall";
                    _floorBlueprint = "Floor";
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
            // 1. Fill with biome-appropriate walls
            FillWithWalls(zone, factory);

            var rooms = new List<Room>();

            // 2. Carve central boss chamber (12x8)
            int bossW = 12;
            int bossH = 8;
            int bossX = Zone.Width / 2 - bossW / 2;
            int bossY = Zone.Height / 2 - bossH / 2;
            var bossRoom = new Room { X = bossX, Y = bossY, W = bossW, H = bossH };
            rooms.Add(bossRoom);
            CarveRoom(zone, factory, bossRoom);

            // 3. Place boss in center of boss chamber
            if (_poi?.BossBlueprint != null)
            {
                var boss = factory.CreateEntity(_poi.BossBlueprint);
                if (boss != null)
                    zone.AddEntity(boss, bossRoom.CenterX, bossRoom.CenterY);
            }

            // 4. Carve 2-3 side rooms
            int sideCount = rng.Next(2, 4);
            int maxAttempts = sideCount * 20;
            for (int attempt = 0; attempt < maxAttempts && rooms.Count - 1 < sideCount; attempt++)
            {
                int w = rng.Next(5, 9);
                int h = rng.Next(4, 7);
                int rx = rng.Next(2, Zone.Width - w - 2);
                int ry = rng.Next(2, Zone.Height - h - 2);

                var room = new Room { X = rx, Y = ry, W = w, H = h };
                if (!OverlapsAny(room, rooms))
                {
                    rooms.Add(room);
                    CarveRoom(zone, factory, room);
                }
            }

            // 5. Connect side rooms to boss chamber with L-shaped corridors
            for (int i = 1; i < rooms.Count; i++)
            {
                CarveCorridor(zone, factory, rng, rooms[i], bossRoom);
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

                    var wall = factory.CreateEntity(_wallBlueprint);
                    if (wall != null)
                        zone.AddEntity(wall, x, y);
                }
            }
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

            bool horizontalFirst = rng.Next(2) == 0;

            if (horizontalFirst)
            {
                CarveHorizontal(zone, factory, x, tx, y);
                CarveVertical(zone, factory, tx, y, ty);
            }
            else
            {
                CarveVertical(zone, factory, x, y, ty);
                CarveHorizontal(zone, factory, x, tx, ty);
            }
        }

        private void CarveHorizontal(Zone zone, EntityFactory factory, int x1, int x2, int y)
        {
            int start = Math.Min(x1, x2);
            int end = Math.Max(x1, x2);
            for (int x = start; x <= end; x++)
            {
                if (!zone.InBounds(x, y)) continue;
                ClearAndPlaceFloor(zone, factory, x, y);
            }
        }

        private void CarveVertical(Zone zone, EntityFactory factory, int x, int y1, int y2)
        {
            int start = Math.Min(y1, y2);
            int end = Math.Max(y1, y2);
            for (int y = start; y <= end; y++)
            {
                if (!zone.InBounds(x, y)) continue;
                ClearAndPlaceFloor(zone, factory, x, y);
            }
        }

        private void ClearAndPlaceFloor(Zone zone, EntityFactory factory, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;

            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                if (cell.Objects[i].HasTag("Wall") || cell.Objects[i].HasTag("Terrain"))
                    zone.RemoveEntity(cell.Objects[i]);
            }

            var floor = factory.CreateEntity(_floorBlueprint);
            if (floor != null)
                zone.AddEntity(floor, x, y);
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
