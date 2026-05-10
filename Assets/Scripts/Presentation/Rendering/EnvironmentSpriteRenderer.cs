using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pass 7 §7B.1 — hybrid sprite environment. After ZoneRenderer
    /// paints CP437 glyphs as usual, this scans cells and replaces
    /// environmental glyphs (`#` walls, `.` floors, `~`/`=`/`-` water,
    /// `+`/`'` doors) with actual 16×16 pixel-art sprites on a new
    /// overlay tilemap, clearing the corresponding cell on the main
    /// tilemap so the original glyph doesn't show through.
    ///
    /// <para><b>Auto-tiling for walls:</b> 4-bit neighbor mask
    /// (N=1, E=2, S=4, W=8) into a 4-variant atlas. With only 4
    /// variants instead of 16, walls don't perfectly merge at every
    /// junction — the bitmask reduces to a "connectedness" classifier
    /// (variant 0 = isolated, 1-3 = lighter/different shading per
    /// general topology). Pass 8 can extend to a full 16-variant
    /// auto-tile when the art ships.</para>
    ///
    /// <para><b>Floor variant assignment:</b> hashed from (x, y) cell
    /// coords for visual variety + reproducibility — same cell shows
    /// the same floor variant across reloads.</para>
    ///
    /// <para><b>Toggleable:</b> see
    /// <see cref="CavesOfOoo.Presentation.Effects.SpriteEnvToggleController"/>
    /// — backslash hotkey toggles. When disabled, this renderer
    /// no-ops and the original CP437 glyphs render normally.</para>
    ///
    /// <para>Plan: <c>Docs/GRAPHICS-PASS7.md</c></para>
    /// </summary>
    public class EnvironmentSpriteRenderer : MonoBehaviour
    {
        // Render between AnimatedEnvironment (2) and FX (4). Reuses
        // GlyphGhostRenderer's slot at 3 — they share sortingOrder
        // since they cover different cells (ghosts on movers' previous
        // positions; sprites on environment cells). No conflict in
        // practice. If conflict arises later, bump FX to 5 + sprite
        // env to 4 + ghost to 3.
        private const int OverlaySortingOrder = 3;

        // Wall + floor + water + door glyphs claimed by this renderer.
        // ZoneRenderer.DensityGlyph emits =, -, ~ for water (high to
        // low density). Walls are typically `#`. Floors `.`. Doors
        // `+` (closed) and `'` (open).
        private static readonly char[] WallGlyphs  = { '#' };
        private static readonly char[] FloorGlyphs = { '.' };
        private static readonly char[] WaterGlyphs = { '~', '=', '-' };
        // Note: AnimatedEnvironmentRenderer (Pass 5) already claims
        // water glyphs to scroll them. Pass 7 takes priority — if
        // sprite mode is on, water cells get sprite + Pass 5 shader
        // skipped. AnimatedEnvironmentRenderer doesn't run on cells
        // already cleared by us.

        public bool RenderingEnabled = true;

        private Sprite[] _wallSprites;   // 4 variants from atlas
        private Sprite[] _floorSprites;  // 4 variants from atlas
        private Sprite _waterSprite;
        private Sprite _doorClosedSprite;
        private Sprite _doorOpenSprite;

        // Per-glyph cached Tile assets (TileBase wrapping each Sprite).
        // Reused across paints to avoid allocating Tile objects per cell.
        private Tile[] _wallTiles;
        private Tile[] _floorTiles;
        private Tile _waterTile;
        private Tile _doorClosedTile;
        private Tile _doorOpenTile;

        private Tilemap _overlayTilemap;
        private Tilemap _mainTilemap;
        private readonly List<Vector3Int> _claimedThisFrame = new List<Vector3Int>(256);

        public bool IsInitialized { get; private set; }

        public void Init(Transform gridParent, Tilemap mainTilemap)
        {
            _mainTilemap = mainTilemap;

            // Make overlay tilemap
            var go = new GameObject("EnvironmentSpriteTilemap");
            go.transform.SetParent(gridParent, false);
            _overlayTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = OverlaySortingOrder;

            LoadSprites();
            BuildTiles();

            IsInitialized = true;
        }

        private void LoadSprites()
        {
#if UNITY_EDITOR
            // Wall atlas: 4 variants horizontal slice
            _wallSprites = LoadAtlasSprites("Assets/Sprites/Environment/wall_atlas.png");
            _floorSprites = LoadAtlasSprites("Assets/Sprites/Environment/floor_atlas.png");
            _waterSprite = LoadSingle("Assets/Sprites/Environment/water_tile.png");
            _doorClosedSprite = LoadSingle("Assets/Sprites/Environment/door_closed.png");
            _doorOpenSprite = LoadSingle("Assets/Sprites/Environment/door_open.png");
#endif
        }

#if UNITY_EDITOR
        private static Sprite[] LoadAtlasSprites(string path)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new List<Sprite>();
            foreach (var a in assets) if (a is Sprite s) list.Add(s);
            // Sort by name suffix to ensure stable ordering (..._00, _01, ...)
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return list.ToArray();
        }

        private static Sprite LoadSingle(string path)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
#endif

        private void BuildTiles()
        {
            _wallTiles = new Tile[_wallSprites?.Length ?? 0];
            for (int i = 0; i < _wallTiles.Length; i++)
            {
                var t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = _wallSprites[i];
                t.name = $"WallTile_{i:D2}";
                _wallTiles[i] = t;
            }
            _floorTiles = new Tile[_floorSprites?.Length ?? 0];
            for (int i = 0; i < _floorTiles.Length; i++)
            {
                var t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = _floorSprites[i];
                t.name = $"FloorTile_{i:D2}";
                _floorTiles[i] = t;
            }
            if (_waterSprite != null)
            {
                _waterTile = ScriptableObject.CreateInstance<Tile>();
                _waterTile.sprite = _waterSprite;
                _waterTile.name = "WaterTile";
            }
            if (_doorClosedSprite != null)
            {
                _doorClosedTile = ScriptableObject.CreateInstance<Tile>();
                _doorClosedTile.sprite = _doorClosedSprite;
                _doorClosedTile.name = "DoorClosed";
            }
            if (_doorOpenSprite != null)
            {
                _doorOpenTile = ScriptableObject.CreateInstance<Tile>();
                _doorOpenTile.sprite = _doorOpenSprite;
                _doorOpenTile.name = "DoorOpen";
            }
        }

        public void PostRender(Zone zone, int width, int height)
        {
            if (!IsInitialized || _mainTilemap == null) return;

            // Clear last frame's claims first.
            for (int i = 0; i < _claimedThisFrame.Count; i++)
                _overlayTilemap.SetTile(_claimedThisFrame[i], null);
            _claimedThisFrame.Clear();

            if (!RenderingEnabled || zone == null) return;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var existingTile = _mainTilemap.GetTile(pos);
                    if (existingTile == null) continue;

                    char glyph = ExtractGlyph(existingTile);
                    if (glyph == '\0') continue;

                    Tile target = ChooseTile(zone, x, y, glyph);
                    if (target == null) continue;

                    var color = _mainTilemap.GetColor(pos);
                    _overlayTilemap.SetTile(pos, target);
                    _overlayTilemap.SetColor(pos, color);
                    _mainTilemap.SetTile(pos, null);
                    _claimedThisFrame.Add(pos);
                }
            }
        }

        private Tile ChooseTile(Zone zone, int x, int y, char glyph)
        {
            // Walls
            for (int i = 0; i < WallGlyphs.Length; i++)
            {
                if (WallGlyphs[i] == glyph)
                {
                    if (_wallTiles == null || _wallTiles.Length == 0) return null;
                    int variant = WallVariantIndex(zone, x, y);
                    return _wallTiles[variant % _wallTiles.Length];
                }
            }
            // Floors
            for (int i = 0; i < FloorGlyphs.Length; i++)
            {
                if (FloorGlyphs[i] == glyph)
                {
                    if (_floorTiles == null || _floorTiles.Length == 0) return null;
                    int variant = FloorVariantIndex(x, y);
                    return _floorTiles[variant % _floorTiles.Length];
                }
            }
            // Water
            for (int i = 0; i < WaterGlyphs.Length; i++)
                if (WaterGlyphs[i] == glyph) return _waterTile;
            // Doors
            if (glyph == '+') return _doorClosedTile;
            if (glyph == '\'') return _doorOpenTile;
            return null;
        }

        /// <summary>
        /// Compute the 0-3 wall variant index from a 4-bit neighbor mask.
        /// With only 4 atlas slots (vs the canonical 16), we collapse:
        ///   mask 0  → variant 0 (isolated)
        ///   1, 2, 4, 8 (1-side connect) → variant 2 (single-edge)
        ///   3, 6, 12, 9 (corner) → variant 3 (corner)
        ///   else → variant 1 (multi-connect / interior)
        /// Future: replace with full 16-variant atlas → just `return mask`.
        /// </summary>
        public static int WallVariantIndex(Zone zone, int x, int y)
        {
            int mask = NeighborMask(zone, x, y);
            if (mask == 0) return 0;
            // Single-side: 1=N, 2=E, 4=S, 8=W
            if (mask == 1 || mask == 2 || mask == 4 || mask == 8) return 2;
            // L-corner: NE=3, ES=6, SW=12, WN=9
            if (mask == 3 || mask == 6 || mask == 12 || mask == 9) return 3;
            // Else (T-junctions, opposite-pairs, full): interior look
            return 1;
        }

        private static int NeighborMask(Zone zone, int x, int y)
        {
            int mask = 0;
            if (IsWallAt(zone, x, y - 1)) mask |= 1; // N
            if (IsWallAt(zone, x + 1, y)) mask |= 2; // E
            if (IsWallAt(zone, x, y + 1)) mask |= 4; // S
            if (IsWallAt(zone, x - 1, y)) mask |= 8; // W
            return mask;
        }

        private static bool IsWallAt(Zone zone, int x, int y)
        {
            if (zone == null) return false;
            var c = zone.GetCell(x, y);
            return c != null && c.IsWall();
        }

        /// <summary>
        /// Deterministic-hash floor variant for visual variety.
        /// Same (x, y) → same variant across reloads.
        /// </summary>
        public static int FloorVariantIndex(int x, int y)
        {
            // Bit-mix hash. Cheap; produces good distribution for small ints.
            int h = x * 73856093 ^ y * 19349663;
            return (h & 0x7fffffff) % 4;
        }

        /// <summary>
        /// Extract the CP437 glyph from a Tile asset. Same convention
        /// as AnimatedEnvironmentRenderer.
        /// </summary>
        private static char ExtractGlyph(TileBase tile)
        {
            if (tile == null) return '\0';
            string n = tile.name;
            if (string.IsNullOrEmpty(n)) return '\0';
            const string PREFIX = "CP437_";
            if (n.Length == PREFIX.Length + 2 && n.StartsWith(PREFIX))
            {
                if (int.TryParse(n.Substring(PREFIX.Length),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out int code)
                    && code >= 0 && code < 256)
                {
                    return (char)code;
                }
            }
            return '\0';
        }
    }
}
