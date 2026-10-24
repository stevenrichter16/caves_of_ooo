using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Identity-preserving world glyphs in native 16x24 cells. This catalog owns
    /// copies of the borrowed world bank, never changes native-art admission,
    /// and never dispatches Render events or authorizes an examine document.
    /// </summary>
    public sealed class FlatPixelFallbackCatalog : IDisposable
    {
        private const int Width = 16, Height = 24, GlyphHeight = 16, Padding = 4;
        private const int Columns = 16, GlyphCount = 256, BackgroundIndex = GlyphCount;
        private const int AtlasWidth = Columns * Width, AtlasHeight = 17 * Height;
        private const float MemoryDim = .35f;
        private readonly Tile[] _tiles = new Tile[GlyphCount + 1];
        private readonly Sprite[] _sprites = new Sprite[GlyphCount + 1];
        private readonly bool[] _hasInk = new bool[GlyphCount];
        private Texture2D _atlas;
        private bool _disposed;

        public FlatPixelFallbackCatalog() : this(CP437TilesetGenerator.GetAtlasTexture()) { }

        /// <summary>Copies a borrowed readable 256x256 world bank once. Invalid
        /// injection stays unavailable; it never substitutes a global resource.</summary>
        public FlatPixelFallbackCatalog(Texture2D sourceAtlas)
        {
            if (sourceAtlas == null || sourceAtlas.width != AtlasWidth
                || sourceAtlas.height != GlyphHeight * Columns || !sourceAtlas.isReadable) return;
            Color32[] source;
            try { source = sourceAtlas.GetPixels32(); }
            catch (UnityException) { return; }

            for (int glyph = 0; glyph < GlyphCount; glyph++)
            {
                int left = glyph % Columns * Width;
                int bottom = (Columns - 1 - glyph / Columns) * GlyphHeight;
                for (int y = 0; y < GlyphHeight && !_hasInk[glyph]; y++)
                    for (int x = 0; x < Width; x++)
                        if (source[(bottom + y) * AtlasWidth + left + x].a != 0)
                        { _hasInk[glyph] = true; break; }
            }
            if (!_hasInk[AsciiWorldRenderPolicy.FallbackGlyph]) return;
            BuildAtlas(source);
        }

        /// <summary>Resolves only the current explored cell and its legitimate
        /// occupants. Supports unskinned zones without admitting regional art.
        /// Colors are semantic base colors; the live renderer owns its one
        /// Render event and lighting pass.</summary>
        public bool TryResolve(Zone zone, int x, int y, out FlatPixelGreyboxCell result)
        {
            result = default;
            if (_disposed || _atlas == null || !CurrentGraph(zone)) return false;
            Cell cell = zone.GetCell(x, y);
            if (!CurrentCell(zone, cell) || cell.X != x || cell.Y != y || !cell.Explored) return false;

            bool remembered = !cell.IsVisible;
            Entity owner = null;
            RenderPart selectedRender = null;
            int topLayer = int.MinValue, backgroundLayer = int.MinValue;
            Color background = Color.black;
            foreach (Entity candidate in cell.Occupants)
            {
                if (!CurrentOwner(zone, cell, candidate, out RenderPart render)
                    || remembered && !CanRemember(candidate, render)) continue;
                if (render.RenderLayer >= topLayer)
                {
                    owner = candidate;
                    selectedRender = render;
                    topLayer = render.RenderLayer;
                }
                if (render.RenderLayer < backgroundLayer) continue;
                Color authoredBackground = QudColorParser.ParseBackground(render.BackgroundColor);
                if (authoredBackground.a <= 0) continue;
                background = QudColorParser.DarkenForBackground(authoredBackground);
                backgroundLayer = render.RenderLayer;
            }

            Tile foreground = selectedRender == null ? null : _tiles[Glyph(selectedRender, x, y)];
            Color foregroundColor = selectedRender == null ? Color.white : ForegroundColor(selectedRender);
            if (remembered)
            {
                background *= MemoryDim; background.a = 1;
                foregroundColor *= MemoryDim; foregroundColor.a = 1;
            }
            result = new FlatPixelGreyboxCell(_tiles[BackgroundIndex], foreground, background,
                foregroundColor, owner, owner == null ? FlatPixelGreyboxKind.Ground : FlatPixelGreyboxKind.Unknown,
                FlatPixelGreyboxGround.Ground, remembered, !remembered && cell.BlocksMovement());
            return true;
        }

        /// <summary>Semantic image only, including carried subjects. The caller
        /// must separately prove current document/custody/disclosure authority.</summary>
        public bool TryGetSubjectSprite(Entity subject, int x, int y, out Sprite sprite, out Color tint)
        {
            sprite = null; tint = Color.white;
            if (_disposed || _atlas == null) return false;
            RenderPart render = subject?.GetPart<RenderPart>();
            if (render == null || !ReferenceEquals(render.ParentEntity, subject) || !render.Visible) return false;
            sprite = _sprites[Glyph(render, x, y)];
            tint = ForegroundColor(render);
            return true;
        }

        private static bool CurrentGraph(Zone zone)
        {
            if (zone == null) return false;
            var manager = WorldLocationContext.For(zone);
            return manager == null || zone.ZoneID != null
                && manager.CachedZones.TryGetValue(zone.ZoneID, out Zone current) && ReferenceEquals(current, zone);
        }

        private static bool CurrentCell(Zone zone, Cell cell)
            => cell != null && ReferenceEquals(cell.ParentZone, zone)
                && ReferenceEquals(zone.GetCell(cell.X, cell.Y), cell);

        private static bool CurrentOwner(Zone zone, Cell cell, Entity owner, out RenderPart render)
        {
            render = owner?.GetPart<RenderPart>();
            if (render == null || !render.Visible || !ReferenceEquals(render.ParentEntity, owner)
                || !ReferenceEquals(owner.SpatialZone, zone)) return false;
            Cell anchor = zone.GetEntityCell(owner);
            if (!CurrentCell(zone, anchor) || !anchor.Objects.Contains(owner)) return false;
            var physics = owner.GetPart<PhysicsPart>();
            if (physics != null && (!ReferenceEquals(physics.ParentEntity, owner)
                || physics.InInventory != null || physics.Equipped != null)) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (!ReferenceEquals(stack.ParentEntity, owner) || stack.StackCount <= 0)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            return footprint == null ? ReferenceEquals(anchor, cell)
                : ReferenceEquals(footprint.ParentEntity, owner) && zone.IsFootprintCurrent(owner)
                    && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }

        private static bool CanRemember(Entity owner, RenderPart render)
        {
            if (render.RenderLayer > 1 || !(owner.HasTag("Terrain") || owner.HasTag("Wall"))
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.GetPart<PhysicsPart>()?.Takeable == true) return false;
            // Reject whole live-state families before examining their values.
            // This is conservative static terrain, not a historical snapshot.
            return !owner.HasPart<CropPart>() && !owner.HasPart<FieldHarvestPart>()
                && !owner.HasPart<HarvestablePart>() && !owner.HasPart<RepairablePart>()
                && !owner.HasPart<LiquidPoolPart>() && !owner.HasPart<CampfirePart>()
                && !owner.HasPart<TorchLightPart>() && !owner.HasPart<DoorPart>()
                && !owner.HasPart<SealedLibraryBarrierPart>();
        }

        private int Glyph(RenderPart render, int x, int y)
        {
            char glyph = AsciiWorldRenderPolicy.FallbackGlyph;
            if (!string.IsNullOrEmpty(render.GlyphVariants)) glyph = render.ResolveGlyph(x, y);
            // Avoid the validator's allocated diagnostic strings on malformed
            // input while retaining its actual printable/single-glyph contract.
            else if (render.RenderString != null && render.RenderString.Length == 1
                && AsciiWorldRenderPolicy.IsPrintableGlyph(render.RenderString[0]))
                glyph = AsciiWorldRenderPolicy.GetGlyphOrFallback(render, out _);
            if (glyph >= GlyphCount || !AsciiWorldRenderPolicy.IsPrintableGlyph(glyph)
                || glyph != AsciiWorldRenderPolicy.EmptyGlyph && !_hasInk[glyph])
                return AsciiWorldRenderPolicy.FallbackGlyph;
            return glyph;
        }

        private static Color ForegroundColor(RenderPart render)
            => QudColorParser.Parse(AsciiWorldRenderPolicy.GetColorOrFallback(render, out _));

        private void BuildAtlas(Color32[] source)
        {
            var pixels = new Color32[AtlasWidth * AtlasHeight];
            for (int glyph = 0; glyph < GlyphCount; glyph++)
            {
                int left = glyph % Columns * Width;
                int sourceBottom = (Columns - 1 - glyph / Columns) * GlyphHeight;
                int bottom = glyph / Columns * Height + Padding;
                for (int y = 0; y < GlyphHeight; y++)
                    Array.Copy(source, (sourceBottom + y) * AtlasWidth + left,
                        pixels, (bottom + y) * AtlasWidth + left, Width);
            }
            int backgroundBottom = BackgroundIndex / Columns * Height;
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                pixels[(backgroundBottom + y) * AtlasWidth + x] = new Color32(255, 255, 255, 255);
            _atlas = new Texture2D(AtlasWidth, AtlasHeight, TextureFormat.RGBA32, false)
            {
                name = "Live16x24 World Glyph Fallback", filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            _atlas.SetPixels32(pixels);
            _atlas.Apply(false, false);
            for (int i = 0; i < _tiles.Length; i++)
            {
                var rect = new Rect(i % Columns * Width, i / Columns * Height, Width, Height);
                Sprite sprite = Sprite.Create(_atlas, rect, new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Flat world glyph " + i;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
                _sprites[i] = sprite; _tiles[i] = tile;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (Tile tile in _tiles) DestroyOwned(tile);
            foreach (Sprite sprite in _sprites) DestroyOwned(sprite);
            DestroyOwned(_atlas); _atlas = null;
        }

        private static void DestroyOwned(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }
}
