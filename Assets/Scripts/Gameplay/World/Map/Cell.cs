using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A single tile position in a Zone. Holds a stack of entities.
    /// Mirrors Qud's Cell: entities are layered by render order,
    /// and the cell tracks properties like solidity from its contents.
    /// </summary>
    public class Cell
    {
        public int X;
        public int Y;
        public Zone ParentZone;

        /// <summary>
        /// All entities at this position, ordered by render layer (lowest first).
        /// </summary>
        public List<Entity> Objects = new List<Entity>(4);

        public Cell(int x, int y, Zone zone = null)
        {
            X = x;
            Y = y;
            ParentZone = zone;
        }

        /// <summary>
        /// Add an entity to this cell. Maintains render layer sort order.
        /// </summary>
        public void AddObject(Entity entity)
        {
            int layer = GetRenderLayer(entity);
            int insertIndex = Objects.Count;
            for (int i = 0; i < Objects.Count; i++)
            {
                if (GetRenderLayer(Objects[i]) > layer)
                {
                    insertIndex = i;
                    break;
                }
            }
            Objects.Insert(insertIndex, entity);
        }

        /// <summary>
        /// Remove an entity from this cell.
        /// </summary>
        public bool RemoveObject(Entity entity)
        {
            return Objects.Remove(entity);
        }

        /// <summary>
        /// Returns true if any entity in this cell has the "Solid" tag.
        /// </summary>
        public bool IsSolid()
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasTag("Solid"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if any entity in this cell has the "Wall" tag.
        /// </summary>
        public bool IsWall()
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasTag("Wall"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the topmost visible entity (highest render layer).
        /// </summary>
        public Entity GetTopVisibleObject()
        {
            for (int i = Objects.Count - 1; i >= 0; i--)
            {
                var render = Objects[i].GetPart<RenderPart>();
                if (render != null && render.Visible)
                    return Objects[i];
            }
            return null;
        }

        /// <summary>
        /// Get all entities with a specific tag.
        /// </summary>
        public List<Entity> GetObjectsWithTag(string tag)
        {
            var result = new List<Entity>();
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasTag(tag))
                    result.Add(Objects[i]);
            }
            return result;
        }

        /// <summary>
        /// Check if any entity with the given tag exists in this cell.
        /// </summary>
        public bool HasObjectWithTag(string tag)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasTag(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a specific part type exists on any entity here.
        /// </summary>
        public bool HasObjectWithPart<T>() where T : Part
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasPart<T>())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Is this cell empty (no entities)?
        /// </summary>
        public bool IsEmpty()
        {
            return Objects.Count == 0;
        }

        /// <summary>
        /// Is this cell passable (not solid)?
        /// </summary>
        public bool IsPassable()
        {
            return !IsSolid();
        }

        private static int GetRenderLayer(Entity entity)
        {
            var render = entity.GetPart<RenderPart>();
            return render?.RenderLayer ?? 0;
        }

        public override string ToString()
        {
            return $"Cell({X},{Y}) [{Objects.Count} objects]";
        }
    }
}
