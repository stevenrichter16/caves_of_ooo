using System.Collections.Generic;
using Unity.Profiling;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A single tile position in a Zone. Holds a stack of entities.
    /// Mirrors Qud's Cell: entities are layered by render order,
    /// and the cell tracks properties like solidity from its contents.
    /// </summary>
    public class Cell
    {
        private static readonly ProfilerMarker ArchiveBarrierMarker = new ProfilerMarker("COO.World.ArchiveBarrier");
        // Readonly: the tile-state readout keys the sparse store by these
        // and trusts they match the zone's array index (Zone.cs fills
        // Cells[x,y] = new Cell(x,y)). Nothing ever mutated them; making
        // that a compile-time fact keeps it true (adversarial review S1).
        public readonly int X;
        public readonly int Y;
        public Zone ParentZone;

        /// <summary>
        /// True if this cell has ever been in the player's field of view.
        /// </summary>
        public bool Explored;

        /// <summary>
        /// True if this cell is currently visible to the player.
        /// Reset each turn by the FOV system.
        /// </summary>
        public bool IsVisible;

        /// <summary>
        /// True when this cell is under a roof — inside a village building,
        /// or anywhere in a dungeon (wz &gt; 0) zone. Set at zone-generation
        /// time by VillageBuilder (interior floor cells) and
        /// OverworldZoneManager.OnZoneGenerated (every cell in an underground
        /// zone). Persists for the cell's lifetime; if the zone unloads and
        /// re-runs generation, the flag gets re-set.
        ///
        /// <para>Consumed by MoveToInteriorGoal / MoveToExteriorGoal as a
        /// per-cell predicate target. Future weather / curfew systems can
        /// gate "safe from rain" and "daylight pressure" rules on it.</para>
        ///
        /// <para>Note: this is a CoO adaptation. Qud uses a zone-level
        /// IsInside flag plus InteriorZone pocket dimensions; since our
        /// buildings are walls+floor in the same 80×25 zone, we encode
        /// the same concept per-cell.</para>
        /// </summary>
        public bool IsInterior;

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
        /// True for a Solid tag or a closed, opted-in archive barrier.
        /// Ordinary Physics-only furniture keeps its existing semantics.
        /// </summary>
        public bool IsSolid()
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasTag("Solid") || Objects[i].GetPart<SealedLibraryBarrierPart>()?.IsClosed == true)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True when something here would stop a move into this cell.
        ///
        /// <para><b>Not the same as <see cref="IsSolid"/>,</b> which tests
        /// the <c>Solid</c> tag plus opted-in closed archive barriers.
        /// The movement gate additionally honors <c>PhysicsPart.Solid</c>,
        /// and blueprints use both ordinary spellings:
        /// walls and trees carry the tag, while the haulable furniture sets
        /// only the Part field. A caller that asks <c>IsSolid</c> when it
        /// means "can I move here" silently walks through the second group.
        /// </para>
        ///
        /// <para>Four private copies of this rule already exist
        /// (<c>DisposeOfCorpseGoal</c>, <c>AILayRunePart</c>,
        /// <c>LandmarkBuilder</c>, <c>SkillCombatHelpers</c>). This is the
        /// first shared one; consolidating those is deliberately left out of
        /// the slice that introduced it.</para>
        /// </summary>
        public bool BlocksMovement(Entity ignoring = null)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                var o = Objects[i];
                if (o == null || o == ignoring) continue;
                if (o.HasTag("Solid")) return true;
                var physics = o.GetPart<PhysicsPart>();
                if (physics != null && physics.Solid) return true;
                if (o.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true) return true;
            }
            return false;
        }

        /// <summary>
        /// True for a Wall tag or a closed archive barrier (including its door).
        /// </summary>
        public bool IsWall()
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].HasTag("Wall") || Objects[i].GetPart<SealedLibraryBarrierPart>()?.IsClosed == true)
                    return true;
            }
            return false;
        }

        /// <summary>Specific closed-archive collision, used by movement paths
        /// that intentionally bypass ordinary obstacles. No allocations.</summary>
        public bool HasClosedArchiveBarrier()
        {
            using (ArchiveBarrierMarker.Auto())
            {
                for (int i = 0; i < Objects.Count; i++)
                    if (Objects[i]?.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true) return true;
                return false;
            }
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
