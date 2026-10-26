using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Existing native stone, gate and peat pixels for the exact East
    /// meadow. Borrows resources/selector; owns four cached tiles and sprites.
    /// This never changes topology, door state, liquid stock or world admission.</summary>
    public sealed class FlatPixelEastFixtureCatalog : IDisposable
    {
        public const string CellarResourcePath = "Sprites/Live16x24/opening-cellar";
        public const string StatesResourcePath = "Sprites/Live16x24/opening-states";
        public const string PeatResourcePath = FlatPixelNellaCornerCatalog.ResourcePath;
        const string East = "Overworld.12.10.0";
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile wall, gateClosed, gateOpen, peat;
        bool disposed;

        public FlatPixelEastFixtureCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(CellarResourcePath), Resources.Load<Texture2D>(StatesResourcePath),
                Resources.Load<Texture2D>(PeatResourcePath), borrowedSelector) { }

        /// <summary>Each optional source is independent. Missing or malformed
        /// injection never loads Resources or substitutes another frame group.</summary>
        public FlatPixelEastFixtureCatalog(Texture2D cellar, Texture2D states, Texture2D corner,
            FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            wall = Build(cellar, 9, 8, "East plain stone wall");
            gateClosed = Build(states, 12, 1, "East pen gate closed");
            gateOpen = Build(states, 12, 2, "East pen gate open");
            peat = Build(corner, 17, 16, "East neutral peat");
        }

        internal static bool HasKnownRole(Entity owner) => owner?.BlueprintName == "Wall"
            || owner?.BlueprintName == "FrontierPenGate" || owner?.BlueprintName == "PeatBog";

        bool Available(Zone zone) => !disposed && selector != null && zone?.ZoneID == East
            && FlatPixelGreyboxCatalog.IsAdmitted(zone) && FlatPixelFallbackCatalog.IsCurrentGraph(zone);

        public bool TryResolve(Zone zone, int x, int y, out Tile tile)
        {
            tile = null;
            if (!Available(zone) || !Disclosed(zone, x, y)
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered
                || !Eligible(selected.Owner)) return false;
            tile = For(selected.Owner); return tile != null;
        }

        /// <summary>The reader authorizes its document separately. This seam
        /// retains the exact disclosed current subject even beneath an actor.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity subject, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!Available(zone) || !Disclosed(zone, x, y) || !Eligible(subject)
                || !ReferenceEquals(subject.SpatialZone, zone)) return false;
            var cell = zone.GetCell(x, y);
            if (!ReferenceEquals(zone.GetEntityCell(subject), cell) || !cell.Objects.Contains(subject)
                || subject.HasPart<SpatialFootprintPart>() && !zone.IsFootprintCurrent(subject)) return false;
            var tile = For(subject); if (tile == null) return false;
            sprite = tile.sprite; return true;
        }

        Tile For(Entity owner)
        {
            if (owner.BlueprintName == "Wall") return wall;
            if (owner.BlueprintName == "PeatBog") return peat;
            // IsClosed also respects the real owned lock. Physics.Solid stays
            // false for both ordinary door states and is never the state cue.
            return owner.GetPart<DoorPart>().IsClosed ? gateClosed : gateOpen;
        }

        static bool Eligible(Entity owner)
        {
            if (!HasKnownRole(owner)) return false;
            if (owner.BlueprintName == "PeatBog") return FlatPixelNellaCornerCatalog.IsEligiblePeat(owner);
            if (owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var structure = Owned<DestructiblePart>(owner);
            if (render == null || !render.Visible || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics == null || physics.Takeable || physics.InInventory != null || physics.Equipped != null
                || structure == null || structure.Gone || structure.HP <= 0) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint != null && (footprint.ParentEntity != owner || footprint.Offsets.Length != 1
                || footprint.Offsets[0] != Vector2Int.zero)) return false;
            if (owner.BlueprintName == "Wall")
                return owner.HasTag("Wall") && physics.Solid && render.RenderString == "#"
                    && Owned<MaterialPart>(owner)?.MaterialID == "Stone" && !owner.HasPart<DoorPart>();
            var door = Owned<DoorPart>(owner); var latch = owner.GetPart<LockPart>();
            return owner.HasTag("Furniture") && !physics.Solid && door != null
                && (latch == null || latch.ParentEntity == owner)
                && (render.RenderString == "+" || render.RenderString == "/")
                && !owner.HasPart<RepairablePart>() && !owner.HasPart<MorrowfastDoorPart>()
                && !owner.HasPart<SealedLibraryBarrierPart>();
        }

        static bool Disclosed(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && cell.X == x && cell.Y == y && ReferenceEquals(cell.ParentZone, zone)
                && cell.Explored && cell.IsVisible;
        }
        static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }
        static Tile Build(Texture2D atlas, int frames, int frame, string name)
        {
            if (atlas == null || atlas.width != frames * 16 || atlas.height != 24) return null;
            var sprite = Sprite.Create(atlas, new Rect(frame * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 " + name; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            Release(wall); Release(gateClosed); Release(gateOpen); Release(peat);
        }
        static void Release(Tile tile)
        { if (tile != null) { DestroyOwned(tile.sprite); DestroyOwned(tile); } }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
