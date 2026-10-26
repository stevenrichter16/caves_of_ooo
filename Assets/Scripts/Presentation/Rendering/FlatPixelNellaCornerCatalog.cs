using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Disclosed copper topology and neutral peat in Nella's existing
    /// plot. Borrows atlas/selector; owns only cached tiles and sprites. Neither
    /// pipe flow nor water stock is inferred from these material identities.</summary>
    public sealed class FlatPixelNellaCornerCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/nella-corner";
        const string Plot = "Overworld.11.8.0";
        const int Frames = 17;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] tiles;
        bool disposed;

        public FlatPixelNellaCornerCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Null/malformed injection stays unavailable without loading
        /// other pixels. Neither borrowed dependency is modified or disposed.</summary>
        public FlatPixelNellaCornerCatalog(Texture2D borrowedAtlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || borrowedAtlas == null || borrowedAtlas.width != 16 * Frames
                || borrowedAtlas.height != 24) return;
            tiles = new Tile[Frames];
            for (int i = 0; i < Frames; i++)
            {
                var sprite = Sprite.Create(borrowedAtlas, new Rect(i * 16, 0, 16, 24),
                    new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Nella corner " + i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
                tiles[i] = tile;
            }
        }

        internal static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "CopperPipe" || owner?.BlueprintName == "PeatBog";

        bool Available(Zone zone) => !disposed && tiles != null && zone?.ZoneID == Plot
            && FlatPixelGreyboxCatalog.IsAdmitted(zone) && FlatPixelFallbackCatalog.IsCurrentGraph(zone);

        /// <summary>Only the selected live foreground owner can occupy this
        /// cell. Neighbor joins describe visible selected copper, never a
        /// covered/remembered owner or an unseen continuation.</summary>
        public bool TryResolve(Zone zone, int x, int y, out Tile tile)
        {
            tile = null;
            if (!Available(zone) || !Selected(zone, x, y, out var owner)) return false;
            tile = tiles[Frame(zone, owner, x, y)]; return true;
        }

        /// <summary>Exact current subject image, including an authorized lower
        /// occupant. The reader still owns document authorization; this checks
        /// physical placement/disclosure rather than selecting another owner.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity subject, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!Available(zone) || !Disclosed(zone, x, y) || !Eligible(subject)
                || !ReferenceEquals(subject.SpatialZone, zone)) return false;
            var cell = zone.GetCell(x, y);
            if (!ReferenceEquals(zone.GetEntityCell(subject), cell) || !cell.Objects.Contains(subject)) return false;
            if (subject.HasPart<SpatialFootprintPart>() && !zone.IsFootprintCurrent(subject)) return false;
            sprite = tiles[Frame(zone, subject, x, y)].sprite; return true;
        }

        int Frame(Zone zone, Entity owner, int x, int y)
        {
            if (owner.BlueprintName == "PeatBog") return 16;
            int mask = 0;
            if (Pipe(zone, x, y - 1)) mask |= 1;
            if (Pipe(zone, x + 1, y)) mask |= 2;
            if (Pipe(zone, x, y + 1)) mask |= 4;
            if (Pipe(zone, x - 1, y)) mask |= 8;
            return mask;
        }

        bool Pipe(Zone zone, int x, int y)
            => Selected(zone, x, y, out var owner) && owner.BlueprintName == "CopperPipe";

        bool Selected(Zone zone, int x, int y, out Entity owner)
        {
            owner = null;
            if (!Disclosed(zone, x, y) || !selector.TryResolve(zone, x, y, out var selected)
                || selected.Remembered || !Eligible(selected.Owner)) return false;
            owner = selected.Owner; return true;
        }

        static bool Disclosed(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && cell.X == x && cell.Y == y && ReferenceEquals(cell.ParentZone, zone)
                && cell.Explored && cell.IsVisible;
        }

        static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }

        // Shared neutral peat identity; region admission and exact world/reader
        // authority remain the responsibility of each companion adapter.
        internal static bool IsEligiblePeat(Entity owner)
            => owner?.BlueprintName == "PeatBog" && Eligible(owner);

        static bool Eligible(Entity owner)
        {
            if (!HasKnownRole(owner) || !owner.HasTag("Terrain") || owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var material = Owned<MaterialPart>(owner); bool pipe = owner.BlueprintName == "CopperPipe";
            if (render == null || !render.Visible || render.RenderString != (pipe ? "=" : "~")
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || physics == null || physics.Solid || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null || material == null
                || material.MaterialID != (pipe ? "Copper" : "Peat")) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint != null && (footprint.ParentEntity != owner || footprint.Offsets.Length != 1
                || footprint.Offsets[0] != Vector2Int.zero)) return false;
            if (pipe) return true;
            var pool = Owned<LiquidPoolPart>(owner); var source = Owned<TileStateSourcePart>(owner);
            var structure = Owned<DestructiblePart>(owner);
            // Neutral peat remains after drawing the pool empty. Wetness,
            // freezing, charge and fire are separately rendered live layers.
            return pool != null && pool.LiquidId == "water" && source != null && source.Coating == "water"
                && structure != null && !structure.Gone && structure.HP > 0;
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (tiles == null) return;
            foreach (var tile in tiles)
                if (tile != null) { DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
