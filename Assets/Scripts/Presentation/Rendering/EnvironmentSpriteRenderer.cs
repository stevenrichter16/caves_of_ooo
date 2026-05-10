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

        // Pass 8 sprite expansion — 15 additional glyph→sprite mappings
        // for stalagmites, boulders, vegetation, fixtures, decorations,
        // stairs, and the gold/bones decorations.
        private Sprite _stalagmiteSprite; // ^
        private Sprite _boulderSprite;    // o
        private Sprite _stalactiteSprite; // |
        private Sprite _bushSprite;       // ;
        private Sprite _cactusSprite;     // t
        private Sprite _treeSprite;       // T
        private Sprite _campfireSprite;   // *
        private Sprite _shrineSprite;     // _
        private Sprite _stairsDownSprite; // >
        private Sprite _stairsUpSprite;   // <
        private Sprite _bonesSprite;      // ,
        private Sprite _barrelSprite;     // 0
        private Sprite _mushroomSprite;   // %
        private Sprite _goldPileSprite;   // $
        private Sprite _chairSprite;      // h
        // Pass 10 — per-blueprint disambiguation
        private Sprite _chestSprite;      // [ when blueprint contains "chest"
        private Sprite _lanternSprite;    // ! when blueprint contains "lantern"

        // Per-glyph cached Tile assets (TileBase wrapping each Sprite).
        // Reused across paints to avoid allocating Tile objects per cell.
        private Tile[] _wallTiles;
        private Tile[] _floorTiles;
        private Tile _waterTile;
        private Tile _doorClosedTile;
        private Tile _doorOpenTile;

        // Pass 8 tile cache
        private Tile _stalagmiteTile;
        private Tile _boulderTile;
        private Tile _stalactiteTile;
        private Tile _bushTile;
        private Tile _cactusTile;
        private Tile _treeTile;
        private Tile _campfireTile;
        private Tile _shrineTile;
        private Tile _stairsDownTile;
        private Tile _stairsUpTile;
        private Tile _bonesTile;
        private Tile _barrelTile;
        private Tile _mushroomTile;
        private Tile _goldPileTile;
        private Tile _chairTile;
        // Pass 10 tiles
        private Tile _chestTile;
        private Tile _lanternTile;

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

            // Pass 8 sprites
            _stalagmiteSprite = LoadSingle("Assets/Sprites/Environment/stalagmite.png");
            _boulderSprite    = LoadSingle("Assets/Sprites/Environment/boulder.png");
            _stalactiteSprite = LoadSingle("Assets/Sprites/Environment/stalactite.png");
            _bushSprite       = LoadSingle("Assets/Sprites/Environment/bush.png");
            _cactusSprite     = LoadSingle("Assets/Sprites/Environment/cactus.png");
            _treeSprite       = LoadSingle("Assets/Sprites/Environment/tree.png");
            _campfireSprite   = LoadSingle("Assets/Sprites/Environment/campfire.png");
            _shrineSprite     = LoadSingle("Assets/Sprites/Environment/shrine.png");
            _stairsDownSprite = LoadSingle("Assets/Sprites/Environment/stairs_down.png");
            _stairsUpSprite   = LoadSingle("Assets/Sprites/Environment/stairs_up.png");
            _bonesSprite      = LoadSingle("Assets/Sprites/Environment/bones.png");
            _barrelSprite     = LoadSingle("Assets/Sprites/Environment/barrel.png");
            _mushroomSprite   = LoadSingle("Assets/Sprites/Environment/mushroom.png");
            _goldPileSprite   = LoadSingle("Assets/Sprites/Environment/gold_pile.png");
            _chairSprite      = LoadSingle("Assets/Sprites/Environment/chair.png");
            // Pass 10
            _chestSprite      = LoadSingle("Assets/Sprites/Environment/chest.png");
            _lanternSprite    = LoadSingle("Assets/Sprites/Environment/lantern.png");
#endif
        }

        private static Tile MakeTile(Sprite s, string name)
        {
            if (s == null) return null;
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.name = name;
            return t;
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

            // Pass 8 tiles
            _stalagmiteTile = MakeTile(_stalagmiteSprite, "Stalagmite");
            _boulderTile    = MakeTile(_boulderSprite,    "Boulder");
            _stalactiteTile = MakeTile(_stalactiteSprite, "Stalactite");
            _bushTile       = MakeTile(_bushSprite,       "Bush");
            _cactusTile     = MakeTile(_cactusSprite,     "Cactus");
            _treeTile       = MakeTile(_treeSprite,       "Tree");
            _campfireTile   = MakeTile(_campfireSprite,   "Campfire");
            _shrineTile     = MakeTile(_shrineSprite,     "Shrine");
            _stairsDownTile = MakeTile(_stairsDownSprite, "StairsDown");
            _stairsUpTile   = MakeTile(_stairsUpSprite,   "StairsUp");
            _bonesTile      = MakeTile(_bonesSprite,      "Bones");
            _barrelTile     = MakeTile(_barrelSprite,     "Barrel");
            _mushroomTile   = MakeTile(_mushroomSprite,   "Mushroom");
            _goldPileTile   = MakeTile(_goldPileSprite,   "GoldPile");
            _chairTile      = MakeTile(_chairSprite,      "Chair");
            // Pass 10 tiles
            _chestTile      = MakeTile(_chestSprite,      "Chest");
            _lanternTile    = MakeTile(_lanternSprite,    "Lantern");
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

                    // Pass 10 — entity-based pre-pass. Chest + lantern
                    // entities don't always paint their RenderString
                    // glyph to the main tilemap (they share cells with
                    // a Floor entity that wins the paint race), so the
                    // glyph-only scan misses them. Look directly at
                    // the cell's top entity and force-paint when its
                    // blueprint matches a sprite-emitting kind.
                    Tile entityTile = TryEntityBasedTile(zone, x, y);
                    if (entityTile != null)
                    {
                        var c2 = _mainTilemap.GetColor(pos);
                        _overlayTilemap.SetTile(pos, entityTile);
                        _overlayTilemap.SetColor(pos, c2);
                        _mainTilemap.SetTile(pos, null);
                        _claimedThisFrame.Add(pos);
                        continue;
                    }

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

        /// <summary>
        /// Pass 10 — entity-based override. Returns a Tile when the
        /// cell hosts a chest / lantern blueprint, regardless of which
        /// glyph the cell currently paints. Returns null otherwise.
        /// </summary>
        private Tile TryEntityBasedTile(Zone zone, int x, int y)
        {
            string bp = TopBlueprintNameAt(zone, x, y);
            if (string.IsNullOrEmpty(bp)) return null;
            if (BlueprintIsChest(bp))   return _chestTile;
            if (BlueprintIsLantern(bp)) return _lanternTile;
            return null;
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

            // Pass 8 — direct glyph→sprite map. Each glyph claimed here
            // is unambiguous in the typical zone. Where multiple
            // entities share a glyph (e.g. `%` is also corpse, `=` is
            // also bed), the sprite chosen here is the most common
            // representation; refining via per-entity blueprint lookup
            // is a Pass 9 follow-up.
            switch (glyph)
            {
                case '^':  return _stalagmiteTile; // stalagmite, spike trap
                case 'o':  return _boulderTile;    // rock, compass stone
                case '|':  return _stalactiteTile; // stalactite, reed
                case ';':  return _bushTile;       // bush
                case 't':  return _cactusTile;     // cactus
                case 'T':  return _treeTile;       // tree
                case '*':  return _campfireTile;   // campfire, brazier, rune
                case '_':  return _shrineTile;     // shrine, altar
                case '>':  return _stairsDownTile; // stairs down
                case '<':  return _stairsUpTile;   // stairs up
                case ',':  return _bonesTile;      // bones, rubble
                case '0':  return _barrelTile;     // barrel
                case '%':  return _mushroomTile;   // mushroom (also corpse — overload)
                case '$':  return _goldPileTile;   // gold pile
                case 'h':  return _chairTile;      // chair, stool
            }

            // Pass 10 — per-blueprint disambiguation for shared glyphs.
            // For `[` (armor + chest) and `!` (potion + lantern), only
            // claim the cell if the topmost entity's BlueprintName
            // matches the sprite-emitting kind. Otherwise return null
            // so the original CP437 glyph stays visible. This avoids
            // making every armor look like a chest, and every potion
            // look like a lantern.
            if (glyph == '[' || glyph == '!')
            {
                string bp = TopBlueprintNameAt(zone, x, y);
                if (bp == null) return null;
                if (glyph == '[' && BlueprintIsChest(bp)) return _chestTile;
                if (glyph == '!' && BlueprintIsLantern(bp)) return _lanternTile;
            }
            return null;
        }

        private static string TopBlueprintNameAt(Zone zone, int x, int y)
        {
            if (zone == null) return null;
            var c = zone.GetCell(x, y);
            var top = c?.GetTopVisibleObject();
            return top?.BlueprintName;
        }

        private static bool BlueprintIsChest(string bp)
        {
            // Chest blueprints in Objects.json:
            //   Chest, LockedChest, MimicChest (per Pass 7 inventory).
            return !string.IsNullOrEmpty(bp)
                && bp.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool BlueprintIsLantern(string bp)
        {
            // Pass 10 — lantern blueprints in Objects.json end with
            // the word "Lantern" (e.g. WatchLantern, BrightLantern).
            // The exact name "Lantern" also matches.
            // We do NOT match prefix-style names like "LanternOil"
            // (the fuel tonic) — those are consumables, not light
            // sources, and rendering them as lit lanterns would be
            // wrong. EndsWith("Lantern") is the correct gate.
            if (string.IsNullOrEmpty(bp)) return false;
            return bp.EndsWith("Lantern", System.StringComparison.OrdinalIgnoreCase)
                || bp.Equals("Lantern", System.StringComparison.OrdinalIgnoreCase);
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
