using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Bounded Ginmere frost source and independent cold-floor marks.
    /// Current tile cold is observed directly, never inferred from a hidden neighbor.</summary>
    public sealed class FlatPixelFrostVentCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/frost-vent";
        readonly Tile[] body;
        readonly Tile cold;
        readonly FlatPixelFallbackCatalog selector;
        bool disposed;
        public FlatPixelFrostVentCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath), Resources.Load<Texture2D>(ResourcePath), borrowedSelector) { }
        public FlatPixelFrostVentCatalog(Texture2D body3, Texture2D cold3, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            if (body3 != null && body3.width == 48 && body3.height == 24)
                body = new[] { Frame(body3, 0), Frame(body3, 1) };
            cold = Frame(cold3, 2);
        }
        public bool TryResolve(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || body == null || !ExactSource(zone, x, y, owner)) return false;
            tile = body[(x + y) & 1]; return true;
        }
        public Tile CoveredBody(Zone zone, int x, int y)
        {
            if (disposed || body == null || selector == null || !HasFloor(zone, x, y)) return null;
            foreach (var e in zone.GetCell(x, y).Occupants)
                if (e.BlueprintName == "FrostVent" && ExactSource(zone, x, y, e)
                    && selector.TryResolve(zone, x, y, out var selected) && !ReferenceEquals(selected.Owner, e))
                    return body[(x + y) & 1];
            return null;
        }
        public bool TryCold(Zone zone, int x, int y, out Tile tile)
        {
            tile = null;
            if (disposed || cold == null || !HasFloor(zone, x, y) || (zone.TileState.Get(x, y)?.Cold ?? 0) <= 0) return false;
            tile = cold; return true;
        }
        bool ExactSource(Zone zone, int x, int y, Entity owner)
        {
            if (selector == null || !HasFloor(zone, x, y) || !Eligible(owner) || !Placed(zone, x, y, owner)
                || !selector.TryResolve(zone, x, y, out var selected) || selected.Remembered) return false;
            return ReferenceEquals(selected.Owner, owner) || Cover(selected.Owner) && Visible(selected.Owner) && Placed(zone, x, y, selected.Owner);
        }
        internal static bool HasFloor(Zone zone, int x, int y)
        {
            if (!FlatPixelSiteAdmission.IsGinmereDescent(zone)) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || !cell.Explored || !cell.IsVisible) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && (state.Heat != 0 || state.Charge != 0 || state.Cold < 0 || state.Coatings.Count != 0
                || state.Residues.Count != 0 || !string.IsNullOrEmpty(state.Cloud))) return false;
            Entity floor = null, vent = null;
            foreach (var e in cell.Occupants)
            {
                if (!Visible(e) || !Placed(zone, x, y, e)) continue;
                if (e.BlueprintName == "SandstoneFloor")
                { if (floor != null || !FlatPixelShallowCavesCatalog.OrdinaryFloor(e)) return false; floor = e; }
                else if (e.BlueprintName == "FrostVent")
                { if (vent != null || !Eligible(e)) return false; vent = e; }
                else if (!Cover(e)) return false;
            }
            return floor != null;
        }
        internal static bool Eligible(Entity e)
        {
            if (e?.BlueprintName != "FrostVent" || !e.HasTag("Terrain") || e.HasTag("Solid") || e.HasTag("Wall")
                || e.HasTag("Vegetation") || e.HasTag("Player") || e.HasTag("Creature") || e.HasTag("Item")) return false;
            foreach (var part in e.Parts)
                if (!(part is RenderPart || part is PhysicsPart || part is ExaminablePart || part is MaterialPart
                    || part is ThermalPart || part is TileStateSourcePart || part is StackerPart
                    || part is StatusEffectsPart || part is SpatialFootprintPart)) return false;
            var r = Owned<RenderPart>(e); var p = Owned<PhysicsPart>(e); var m = Owned<MaterialPart>(e);
            var t = Owned<ThermalPart>(e); var source = Owned<TileStateSourcePart>(e);
            if (!Visible(e) || p.Takeable || p.Solid || r.RenderString != "^" || r.ColorString != "&C" || r.RenderLayer != 1
                || !string.IsNullOrEmpty(r.GlyphVariants) || !string.IsNullOrEmpty(r.BackgroundColor)
                || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant) || Owned<ExaminablePart>(e) == null) return false;
            if (e.HasPart<StatusEffectsPart>() && (Owned<StatusEffectsPart>(e) == null || Owned<StatusEffectsPart>(e).GetRenderColorOverride() != null)) return false;
            if (e.HasPart<StackerPart>() && (Owned<StackerPart>(e) == null || Owned<StackerPart>(e).StackCount <= 0)) return false;
            return m != null && m.MaterialID == "Rime" && m.Conductivity == 40 && m.Combustibility == 0 && m.Brittleness == .7f && m.HasMaterialTag("Ice")
                && t != null && float.IsFinite(t.Temperature) && t.FreezeTemperature == 0 && t.BrittleTemperature == -60 && t.HeatCapacity == 1.2f
                && source != null && source.Cold == 1 && source.Heat == 0 && source.Charge == 0 && source.RadiatesEnergy
                && string.IsNullOrEmpty(source.Coating) && string.IsNullOrEmpty(source.Residue);
        }
        static bool Visible(Entity e)
        {
            var r = Owned<RenderPart>(e); var p = Owned<PhysicsPart>(e);
            return r != null && r.Visible && p != null && p.InInventory == null && p.Equipped == null;
        }
        static bool Cover(Entity e) => e != null && (e.HasTag("Player") || e.HasTag("Creature") || e.HasTag("Item"));
        static bool Placed(Zone zone, int x, int y, Entity e)
        {
            if (e == null || e.SpatialZone != zone) return false;
            var cell = zone.GetEntityCell(e);
            if (cell == null || cell.X != x || cell.Y != y || !cell.Objects.Contains(e)) return false;
            if (!e.HasPart<SpatialFootprintPart>()) return true;
            var f = Owned<SpatialFootprintPart>(e);
            return f != null && f.Offsets.Length == 1 && f.Offsets[0] == Vector2Int.zero && zone.IsFootprintCurrent(e);
        }
        static T Owned<T>(Entity e) where T : Part
        {
            T found = null; if (e == null) return null;
            foreach (var p in e.Parts) if (p is T t) { if (found != null || p.ParentEntity != e) return null; found = t; }
            return found;
        }
        static Tile Frame(Texture2D texture, int index)
        {
            if (texture == null || texture.width != 48 || texture.height != 24) return null;
            var sprite = Sprite.Create(texture, new Rect(index * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 frost " + index; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.sprite = sprite;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        { if (disposed) return; disposed = true; if (body != null) foreach (var t in body) Destroy(t); Destroy(cold); }
        static void Destroy(Tile t)
        { if (t == null) return; if (Application.isPlaying) { Object.Destroy(t.sprite); Object.Destroy(t); } else { Object.DestroyImmediate(t.sprite); Object.DestroyImmediate(t); } }
    }
}
