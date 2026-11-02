using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Presentation of the installed Morrowfast river and its real footbridge support.
    /// Borrows approved atlases; never changes terrain, bridge authority or wetness.</summary>
    public sealed class FlatPixelMorrowfastRiverCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/felling-blackwater";
        const string BridgeId = "western-footbridge";
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] water, boards;
        readonly MorrowfastSceneDefinition.CellSpec[,] cells = new MorrowfastSceneDefinition.CellSpec[Zone.Width, Zone.Height];
        readonly MorrowfastSceneDefinition.OwnerSpec bridge;
        readonly string[,] ownerIds = new string[Zone.Width, Zone.Height];
        bool disposed;

        public FlatPixelMorrowfastRiverCatalog(Texture2D river17, Texture2D travel28, FlatPixelFallbackCatalog borrowedSelector)
        {
            selector = borrowedSelector;
            water = FlatPixelSettlementGroundCatalog.Frames(river17, 17, 0, 17, "Morrowfast river");
            boards = FlatPixelSettlementGroundCatalog.Frames(travel28, 28, 0, 16, "Morrowfast bridge");
            var definition = MorrowfastSceneDefinition.Load();
            if (definition == null) return;
            foreach (var c in definition.cells)
            { cells[c.x, c.y] = c; if (c.water) ownerIds[c.x, c.y] = "morrowfast-terrain:" + c.x + ":" + c.y; }
            bridge = definition.FindOwner(BridgeId);
        }

        bool Ready(Zone zone, int x, int y, out Cell cell)
        {
            cell = zone?.GetCell(x, y);
            if (disposed || selector == null || cell == null || !cell.Explored || !cell.IsVisible
                || zone.ZoneID != MorrowfastSceneRuntime.ZoneID || !FlatPixelSettlementGroundCatalog.IsSupportedZone(zone)) return false;
            var state = MorrowfastSceneRuntime.GetState(zone);
            var anchor = state?.ParentEntity;
            return state?.Revision == 1 && anchor?.ID == "morrowfast-scene-state"
                && Owned<MorrowfastSceneStatePart>(anchor) == state && Placed(zone, zone.GetCell(0, 0), anchor)
                && selector.TryResolve(zone, x, y, out var picked) && !picked.Remembered;
        }

        bool WaterOwner(Zone zone, int x, int y, out Entity source)
        {
            source = null;
            if (!Ready(zone, x, y, out var cell)) return false;
            var spec = cells[x, y];
            if (spec == null || !spec.water || spec.opaque) return false;
            var state = zone.TileState.Get(x, y);
            if (!FlatPixelStumpMaterialsCatalog.PermanentWater(state) || state.Heat != 0 || state.Cold != 0 || state.Charge != 0
                || !string.IsNullOrEmpty(state.Cloud) || state.Residues == null || state.Residues.Count != 0) return false;
            foreach (var owner in cell.Occupants)
            {
                if (!FlatPixelStumpMaterialsCatalog.VisiblePhysicalOwner(owner)
                    || !FlatPixelStumpMaterialsCatalog.Placed(zone, cell, owner)) continue;
                if (owner.ID == ownerIds[x, y] && Placed(zone, cell, owner)
                    && FlatPixelStumpMaterialsCatalog.CanonicalAuthoredWater(owner, MorrowfastSceneRuntime.TerrainTag, spec.solid))
                { if (source != null) return false; source = owner; }
                else if (owner.HasTag("Terrain") || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>()
                    || owner.HasPart<TileStateSourcePart>() || owner.HasPart<FiniteLiquidFilmPart>()) return false;
            }
            return source != null;
        }

        bool JoinsWater(Zone zone, int x, int y)
            => WaterOwner(zone, x, y, out var source) && selector.TryResolve(zone, x, y, out var picked) && picked.Owner == source;

        public bool TryResolveWater(Zone zone, int x, int y, out Tile tile, out Entity source)
        {
            tile = null; source = null;
            if (water == null || !WaterOwner(zone, x, y, out source)) return false;
            int mask = 0;
            if (!JoinsWater(zone, x, y - 1)) mask |= 1;
            if (!JoinsWater(zone, x + 1, y)) mask |= 2;
            if (!JoinsWater(zone, x, y + 1)) mask |= 4;
            if (!JoinsWater(zone, x - 1, y)) mask |= 8;
            tile = water[mask == 0 ? (((x * 17 + y * 31) & 3) == 0 ? 1 : 0) : mask + 1];
            return true;
        }

        bool Support(int x, int y)
        {
            if (bridge?.bridgeSupport == null) return false;
            foreach (var p in bridge.bridgeSupport) if (p.x == x && p.y == y) return true;
            return false;
        }
        bool DisclosedSupport(Zone zone, int x, int y) => Support(x, y) && WaterOwner(zone, x, y, out _);

        public Tile Bridge(Zone zone, int x, int y)
        {
            if (boards == null || !DisclosedSupport(zone, x, y) || !Ready(zone, bridge.anchorX, bridge.anchorY, out var anchor)) return null;
            var owner = MorrowfastSceneRuntime.FindOwner(zone, BridgeId);
            if (owner?.BlueprintName != "MorrowfastSceneProp" || owner.ID != "morrowfast-owner:" + BridgeId
                || !Placed(zone, anchor, owner) || owner.Parts.Count != 4 || owner.HasTag("Solid") || owner.HasTag("Wall")) return null;
            var part = Owned<MorrowfastPropPart>(owner); var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (part?.ComponentId != BridgeId || Owned<ExaminablePart>(owner) == null || render == null || !render.Visible
                || render.RenderString != "*" || render.ColorString != "&y" || render.RenderLayer != 3
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.BackgroundColor) || !string.IsNullOrEmpty(render.GlyphVariants)
                || physics == null || physics.Solid || physics.Takeable || physics.InInventory != null || physics.Equipped != null) return null;
            int mask = 0;
            if (DisclosedSupport(zone, x, y - 1)) mask |= 1;
            if (DisclosedSupport(zone, x + 1, y)) mask |= 2;
            if (DisclosedSupport(zone, x, y + 1)) mask |= 4;
            if (DisclosedSupport(zone, x - 1, y)) mask |= 8;
            return boards[mask];
        }
        static bool Placed(Zone zone, Cell cell, Entity owner)
            => cell != null && owner != null && owner.SpatialZone == zone && zone.GetEntityCell(owner) == cell && cell.Objects.Contains(owner);
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            foreach (var p in owner.Parts) if (p is T part) { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var group in new[] { water, boards }) if (group != null) foreach (var tile in group)
            { Destroy(tile.sprite); Destroy(tile); }
        }
        static void Destroy(UnityEngine.Object value)
        { if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
    }
}
