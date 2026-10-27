using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>One native neutral brine frame for the disclosed Kitchen court.
    /// Borrows the atlas and selector; owns only its cached tile and sprite.
    /// Other liquid states retain their existing glyph and hazard treatment.</summary>
    public sealed class FlatPixelKitchenBrineCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/kitchen-brine";
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile tile;
        bool disposed;

        public FlatPixelKitchenBrineCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }

        /// <summary>Missing or malformed injection never loads a substitute.</summary>
        public FlatPixelKitchenBrineCatalog(Texture2D atlas, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (selector == null || atlas == null || atlas.width != 16 || atlas.height != 24) return;
            var sprite = Sprite.Create(atlas, new Rect(0, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 Kitchen neutral brine"; sprite.hideFlags = HideFlags.HideAndDontSave;
            tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
        }

        bool Available(Zone zone, int x, int y)
        {
            if (disposed || tile == null || selector == null || zone?.ZoneID != "Overworld.12.11.0"
                || !FlatPixelGreyboxCatalog.IsAdmitted(zone) || !FlatPixelFallbackCatalog.IsCurrentGraph(zone)) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && cell.X == x && cell.Y == y && ReferenceEquals(cell.ParentZone, zone)
                && cell.Explored && cell.IsVisible && IsSoleLiveBrine(zone.TileState.Get(x, y));
        }

        /// <summary>True only for one live brine layer; mixtures and expired layers
        /// must keep the fallback cue. This observation never creates tile state.</summary>
        internal static bool IsSoleLiveBrine(ZoneTileState.TileState state)
            => state != null && state.Coatings.Count == 1
                && state.Coatings[0].Id == "brine" && state.Coatings[0].Turns > 0;

        public bool TryResolve(Zone zone, int x, int y, out Tile result)
        {
            result = null;
            if (!Available(zone, x, y) || !selector.TryResolve(zone, x, y, out var selected)
                || selected.Remembered || !Eligible(selected.Owner)) return false;
            result = tile; return true;
        }

        /// <summary>The reader may show the exact current lower subject beneath
        /// an actor; world priority remains owned by the borrowed selector.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity subject, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!Available(zone, x, y) || !Eligible(subject) || !ReferenceEquals(subject.SpatialZone, zone)) return false;
            var cell = zone.GetCell(x, y);
            if (!ReferenceEquals(zone.GetEntityCell(subject), cell) || !cell.Objects.Contains(subject)
                || subject.HasPart<SpatialFootprintPart>() && !zone.IsFootprintCurrent(subject)) return false;
            sprite = tile.sprite; return true;
        }

        static bool Eligible(Entity owner)
        {
            if (owner?.BlueprintName != "BrinePool" || !owner.HasTag("Terrain")
                || owner.HasTag("Creature") || owner.HasTag("Item") || owner.HasTag("Player")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var pool = Owned<LiquidPoolPart>(owner); var thermal = Owned<ThermalPart>(owner);
            if (render == null || !render.Visible || render.RenderString != "~"
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || physics == null || physics.Solid || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || pool == null || pool.LiquidId != "brine" || pool.Volume <= 0
                || Owned<MaterialPart>(owner)?.MaterialID != "Brine"
                || Owned<TileStateSourcePart>(owner)?.EffectiveCoating != "brine"
                || thermal == null || !float.IsFinite(thermal.Temperature) || !float.IsFinite(thermal.FreezeTemperature)
                || thermal.Temperature <= thermal.FreezeTemperature) return false;
            // Preserve owner-driven Render tints rather than replacing an active
            // cold/electrical effect with neutral white native pixels.
            if (owner.HasEffect<FrozenEffect>() || owner.HasEffect<ElectrifiedEffect>()) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            return footprint == null || footprint.ParentEntity == owner && footprint.Offsets.Length == 1
                && footprint.Offsets[0] == Vector2Int.zero;
        }

        static T Owned<T>(Entity owner) where T : Part
        { var part = owner.GetPart<T>(); return part?.ParentEntity == owner ? part : null; }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (tile != null) { DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
