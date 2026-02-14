using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// An 80x25 grid of Cells — one screen of the game world.
    /// Mirrors Qud's Zone: entities live in cells, and zones are the
    /// fundamental spatial unit. Qud uses 80x25 (classic terminal size).
    /// </summary>
    public class Zone
    {
        public const int Width = 80;
        public const int Height = 25;

        public string ZoneID;

        /// <summary>
        /// The grid of cells, stored as [x, y].
        /// </summary>
        public Cell[,] Cells = new Cell[Width, Height];

        /// <summary>
        /// Quick lookup: entity -> which cell it's in.
        /// </summary>
        private Dictionary<Entity, Cell> _entityCells = new Dictionary<Entity, Cell>();

        public Zone(string zoneID = null)
        {
            ZoneID = zoneID ?? "Zone";
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cells[x, y] = new Cell(x, y, this);
                }
            }
        }

        /// <summary>
        /// Get a cell by coordinates. Returns null if out of bounds.
        /// </summary>
        public Cell GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return Cells[x, y];
        }

        /// <summary>
        /// Place an entity at a specific cell position.
        /// If the entity is already in this zone, it is moved.
        /// </summary>
        public bool AddEntity(Entity entity, int x, int y)
        {
            Cell cell = GetCell(x, y);
            if (cell == null) return false;

            // Remove from old cell if already placed
            if (_entityCells.TryGetValue(entity, out Cell oldCell))
            {
                oldCell.RemoveObject(entity);
            }

            cell.AddObject(entity);
            _entityCells[entity] = cell;
            return true;
        }

        /// <summary>
        /// Remove an entity from the zone entirely.
        /// </summary>
        public bool RemoveEntity(Entity entity)
        {
            if (_entityCells.TryGetValue(entity, out Cell cell))
            {
                cell.RemoveObject(entity);
                _entityCells.Remove(entity);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Move an entity to a new position. Returns false if blocked or out of bounds.
        /// </summary>
        public bool MoveEntity(Entity entity, int newX, int newY)
        {
            Cell target = GetCell(newX, newY);
            if (target == null) return false;

            return AddEntity(entity, newX, newY);
        }

        /// <summary>
        /// Get the cell an entity is currently in.
        /// </summary>
        public Cell GetEntityCell(Entity entity)
        {
            _entityCells.TryGetValue(entity, out Cell cell);
            return cell;
        }

        /// <summary>
        /// Get the position of an entity as (x, y). Returns (-1, -1) if not found.
        /// </summary>
        public (int x, int y) GetEntityPosition(Entity entity)
        {
            if (_entityCells.TryGetValue(entity, out Cell cell))
                return (cell.X, cell.Y);
            return (-1, -1);
        }

        /// <summary>
        /// Check if coordinates are within zone bounds.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Get all entities in the zone.
        /// </summary>
        public List<Entity> GetAllEntities()
        {
            var result = new List<Entity>(_entityCells.Count);
            result.AddRange(_entityCells.Keys);
            return result;
        }

        /// <summary>
        /// Get all entities with a specific tag.
        /// </summary>
        public List<Entity> GetEntitiesWithTag(string tag)
        {
            var result = new List<Entity>();
            foreach (var entity in _entityCells.Keys)
            {
                if (entity.HasTag(tag))
                    result.Add(entity);
            }
            return result;
        }

        /// <summary>
        /// Iterate over all cells. Callback receives (cell, x, y).
        /// </summary>
        public void ForEachCell(System.Action<Cell, int, int> action)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    action(Cells[x, y], x, y);
                }
            }
        }

        /// <summary>
        /// Get the cell adjacent to (x,y) in a given direction.
        /// Directions: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        /// </summary>
        public Cell GetCellInDirection(int x, int y, int direction)
        {
            int dx = 0, dy = 0;
            switch (direction)
            {
                case 0: dy = -1; break;           // N
                case 1: dx = 1; dy = -1; break;   // NE
                case 2: dx = 1; break;             // E
                case 3: dx = 1; dy = 1; break;     // SE
                case 4: dy = 1; break;             // S
                case 5: dx = -1; dy = 1; break;    // SW
                case 6: dx = -1; break;            // W
                case 7: dx = -1; dy = -1; break;   // NW
            }
            return GetCell(x + dx, y + dy);
        }

        /// <summary>
        /// Count of all entities currently in the zone.
        /// </summary>
        public int EntityCount => _entityCells.Count;

        public override string ToString()
        {
            return $"Zone({ZoneID}) [{EntityCount} entities]";
        }
    }
}
