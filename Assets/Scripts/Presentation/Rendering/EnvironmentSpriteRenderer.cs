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
        // Pass 11 — round-out per-blueprint disambiguation
        private Sprite _bedSprite;        // = when blueprint contains "Bed"
        private Sprite _corpseSprite;     // % when blueprint contains "Corpse"/"Body"
        // Pass 12 — blueprint-keyed terrain identity, farming, fixtures
        private Sprite _grassSprite;           // Grass ('.', was generic floor)
        private Sprite _sandSprite;            // Sand ('.', was generic floor)
        private Sprite _bankSprite;            // Bank ('.', riverbank, was generic floor)
        private Sprite _cropSeedSprite;        // any *Crop at stage 0
        private Sprite _candyCarrotCropSprite; // CandyCarrotCrop stage 1 ('t' collided with cactus)
        private Sprite _emberwheatCropSprite;  // EmberwheatCrop stage 1 ('i' uncovered)
        private Sprite _wellSprite;            // Well ('O' uncovered)
        private Sprite _marketStallSprite;     // MarketStall ('=' collided with bed)
        private Sprite _forgeSprite;           // TinkersForge ('n' uncovered)
        private Sprite _alchemyStillSprite;    // AlchemyStill ('&' uncovered)
        // Pass 13 — Muted Overgrowth actor pilot + ruin fixtures
        private Sprite _playerSprite;          // Player ('@'), authored-color
        private Sprite _snapjawSprite;         // Snapjaw family ('s'/'S'), authored-color
        private Sprite _pillarSprite;          // Pillar 'I' / BrokenColumn ','
        // Pass 14 — 15-sprite expansion (STYLE-GUIDE.md; GRAPHICS-PASS14.md)
        private Sprite _villagerSprite;        // Villager/Innkeeper/WellKeeper/Scribe
        private Sprite _merchantSprite;        // Merchant
        private Sprite _elderSprite;           // Elder
        private Sprite _wardenSprite;          // Warden
        private Sprite _childSprite;           // VillageChild
        private Sprite _sporeShamblerSprite;   // SporeShambler
        private Sprite _iceWightSprite;        // IceWight
        private Sprite _rubbleSprite;          // Rubble (',' rendered as BONES before)
        private Sprite _ovenSprite;            // Oven ('#' rendered as WALL before)
        private Sprite _iceStalactiteSprite;   // IceStalactite ('|' was generic stone)
        private Sprite _weaponGroundSprite;    // glyph '/' dropped blades (color-copy tinted)
        private Sprite _oilSeepSprite;         // OilSeep ('~' rendered as WATER before)
        private Sprite _acidPondSprite;        // AcidPond ('~' rendered as WATER before)
        private Sprite _vineWallSprite;        // VineWall themed wall
        private Sprite _sandstoneWallSprite;   // SandstoneWall themed wall

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
        private Tile _bedTile;
        private Tile _corpseTile;
        // Pass 12 tiles
        private Tile _grassTile;
        private Tile _sandTile;
        private Tile _bankTile;
        private Tile _cropSeedTile;
        private Tile _candyCarrotCropTile;
        private Tile _emberwheatCropTile;
        private Tile _wellTile;
        private Tile _marketStallTile;
        private Tile _forgeTile;
        private Tile _alchemyStillTile;
        // Pass 13 tiles
        private Tile _playerTile;
        private Tile _snapjawTile;
        private Tile _pillarTile;
        // Pass 14 tiles
        private Tile _villagerTile;
        private Tile _merchantTile;
        private Tile _elderTile;
        private Tile _wardenTile;
        private Tile _childTile;
        private Tile _sporeShamblerTile;
        private Tile _iceWightTile;
        private Tile _rubbleTile;
        private Tile _ovenTile;
        private Tile _iceStalactiteTile;
        private Tile _weaponGroundTile;
        private Tile _oilSeepTile;
        private Tile _acidPondTile;
        private Tile _vineWallTile;
        private Tile _sandstoneWallTile;

        private Tilemap _overlayTilemap;
        private Tilemap _mainTilemap;

        /// <summary>
        /// PASS 15 R3 — a claim remembers the main-tilemap tile + color
        /// it displaced so releasing RESTORES the glyph instead of
        /// leaving a hole. The Pass 13 dirty-path blanking bug: release
        /// nulled the overlay while the main tile had already been
        /// nulled at claim time, so every non-dirty claimed cell
        /// rendered as nothing.
        /// </summary>
        private struct Claim
        {
            public Vector3Int Pos;
            public TileBase MainTile;
            public Color MainColor;
        }
        private readonly List<Claim> _claimedThisFrame = new List<Claim>(2048);

        // PASS 15 R5 — this scan was invisible to the perf budget table.
        private static readonly Unity.Profiling.ProfilerMarker s_PostRenderMarker =
            new Unity.Profiling.ProfilerMarker("COO.EnvSprites.PostRender");

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

        // PASS 15 R1 — sprites load via Resources (the assets live at
        // Assets/Resources/Sprites/Environment/). The old loader was
        // wrapped in #if UNITY_EDITOR and used AssetDatabase: in ANY
        // player build every sprite was null, PostRender no-opped on
        // every cell, and with no scene reference the PNGs were
        // build-stripping candidates. Resources.Load works identically
        // in editor, EditMode tests, and builds.
        private const string SpriteRoot = "Sprites/Environment/";

        private void LoadSprites()
        {
            _wallSprites = LoadAtlasSprites(SpriteRoot + "wall_atlas");
            _floorSprites = LoadAtlasSprites(SpriteRoot + "floor_atlas");
            _waterSprite = LoadSingle(SpriteRoot + "water_tile");
            _doorClosedSprite = LoadSingle(SpriteRoot + "door_closed");
            _doorOpenSprite = LoadSingle(SpriteRoot + "door_open");

            // Pass 8 sprites
            _stalagmiteSprite = LoadSingle(SpriteRoot + "stalagmite");
            _boulderSprite    = LoadSingle(SpriteRoot + "boulder");
            _stalactiteSprite = LoadSingle(SpriteRoot + "stalactite");
            _bushSprite       = LoadSingle(SpriteRoot + "bush");
            _cactusSprite     = LoadSingle(SpriteRoot + "cactus");
            _treeSprite       = LoadSingle(SpriteRoot + "tree");
            _campfireSprite   = LoadSingle(SpriteRoot + "campfire");
            _shrineSprite     = LoadSingle(SpriteRoot + "shrine");
            _stairsDownSprite = LoadSingle(SpriteRoot + "stairs_down");
            _stairsUpSprite   = LoadSingle(SpriteRoot + "stairs_up");
            _bonesSprite      = LoadSingle(SpriteRoot + "bones");
            _barrelSprite     = LoadSingle(SpriteRoot + "barrel");
            _mushroomSprite   = LoadSingle(SpriteRoot + "mushroom");
            _goldPileSprite   = LoadSingle(SpriteRoot + "gold_pile");
            _chairSprite      = LoadSingle(SpriteRoot + "chair");
            // Pass 10
            _chestSprite      = LoadSingle(SpriteRoot + "chest");
            _lanternSprite    = LoadSingle(SpriteRoot + "lantern");
            // Pass 11
            _bedSprite        = LoadSingle(SpriteRoot + "bed");
            _corpseSprite     = LoadSingle(SpriteRoot + "corpse");
            // Pass 12
            _grassSprite           = LoadSingle(SpriteRoot + "grass");
            _sandSprite            = LoadSingle(SpriteRoot + "sand");
            _bankSprite            = LoadSingle(SpriteRoot + "bank");
            _cropSeedSprite        = LoadSingle(SpriteRoot + "crop_seed");
            _candyCarrotCropSprite = LoadSingle(SpriteRoot + "candycarrot_crop");
            _emberwheatCropSprite  = LoadSingle(SpriteRoot + "emberwheat_crop");
            _wellSprite            = LoadSingle(SpriteRoot + "well");
            _marketStallSprite     = LoadSingle(SpriteRoot + "market_stall");
            _forgeSprite           = LoadSingle(SpriteRoot + "forge");
            _alchemyStillSprite    = LoadSingle(SpriteRoot + "alchemy_still");
            // Pass 13
            _playerSprite          = LoadSingle(SpriteRoot + "player");
            _snapjawSprite         = LoadSingle(SpriteRoot + "snapjaw");
            _pillarSprite          = LoadSingle(SpriteRoot + "pillar");
            // Pass 14
            _villagerSprite        = LoadSingle(SpriteRoot + "villager");
            _merchantSprite        = LoadSingle(SpriteRoot + "merchant");
            _elderSprite           = LoadSingle(SpriteRoot + "elder");
            _wardenSprite          = LoadSingle(SpriteRoot + "warden");
            _childSprite           = LoadSingle(SpriteRoot + "village_child");
            _sporeShamblerSprite   = LoadSingle(SpriteRoot + "spore_shambler");
            _iceWightSprite        = LoadSingle(SpriteRoot + "ice_wight");
            _rubbleSprite          = LoadSingle(SpriteRoot + "rubble");
            _ovenSprite            = LoadSingle(SpriteRoot + "oven");
            _iceStalactiteSprite   = LoadSingle(SpriteRoot + "ice_stalactite");
            _weaponGroundSprite    = LoadSingle(SpriteRoot + "weapon_ground");
            _oilSeepSprite         = LoadSingle(SpriteRoot + "oil_seep");
            _acidPondSprite        = LoadSingle(SpriteRoot + "acid_pond");
            _vineWallSprite        = LoadSingle(SpriteRoot + "vine_wall");
            _sandstoneWallSprite   = LoadSingle(SpriteRoot + "sandstone_wall");
        }

        private static Tile MakeTile(Sprite s, string name)
        {
            if (s == null) return null;
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.name = name;
            return t;
        }

        private static Sprite[] LoadAtlasSprites(string resourcePath)
        {
            var assets = Resources.LoadAll<Sprite>(resourcePath);
            var list = new List<Sprite>(assets);
            // Sort by name suffix to ensure stable ordering (..._00, _01, ...)
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return list.ToArray();
        }

        private static Sprite LoadSingle(string resourcePath)
        {
            return Resources.Load<Sprite>(resourcePath);
        }

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
            _bedTile        = MakeTile(_bedSprite,        "Bed");
            _corpseTile     = MakeTile(_corpseSprite,     "Corpse");
            // Pass 12
            _grassTile           = MakeTile(_grassSprite,           "Grass");
            _sandTile            = MakeTile(_sandSprite,            "Sand");
            _bankTile            = MakeTile(_bankSprite,            "Bank");
            _cropSeedTile        = MakeTile(_cropSeedSprite,        "CropSeed");
            _candyCarrotCropTile = MakeTile(_candyCarrotCropSprite, "CandyCarrotCrop");
            _emberwheatCropTile  = MakeTile(_emberwheatCropSprite,  "EmberwheatCrop");
            _wellTile            = MakeTile(_wellSprite,            "Well");
            _marketStallTile     = MakeTile(_marketStallSprite,     "MarketStall");
            _forgeTile           = MakeTile(_forgeSprite,           "TinkersForge");
            _alchemyStillTile    = MakeTile(_alchemyStillSprite,    "AlchemyStill");
            // Pass 13
            _playerTile          = MakeTile(_playerSprite,          "Player");
            _snapjawTile         = MakeTile(_snapjawSprite,         "Snapjaw");
            _pillarTile          = MakeTile(_pillarSprite,          "Pillar");
            // Pass 14
            _villagerTile        = MakeTile(_villagerSprite,        "Villager");
            _merchantTile        = MakeTile(_merchantSprite,        "Merchant");
            _elderTile           = MakeTile(_elderSprite,           "Elder");
            _wardenTile          = MakeTile(_wardenSprite,          "Warden");
            _childTile           = MakeTile(_childSprite,           "VillageChild");
            _sporeShamblerTile   = MakeTile(_sporeShamblerSprite,   "SporeShambler");
            _iceWightTile        = MakeTile(_iceWightSprite,        "IceWight");
            _rubbleTile          = MakeTile(_rubbleSprite,          "Rubble");
            _ovenTile            = MakeTile(_ovenSprite,            "Oven");
            _iceStalactiteTile   = MakeTile(_iceStalactiteSprite,   "IceStalactite");
            _weaponGroundTile    = MakeTile(_weaponGroundSprite,    "WeaponGround");
            _oilSeepTile         = MakeTile(_oilSeepSprite,         "OilSeep");
            _acidPondTile        = MakeTile(_acidPondSprite,        "AcidPond");
            _vineWallTile        = MakeTile(_vineWallSprite,        "VineWall");
            _sandstoneWallTile   = MakeTile(_sandstoneWallSprite,   "SandstoneWall");
        }

        /// <summary>
        /// PASS 15 R3 — ZoneRenderer calls this right after
        /// <c>ClearAllTiles</c> on the full-repaint path. The main
        /// tilemap is already blank, so restoring remembered glyphs
        /// would resurrect STALE tiles over the fresh repaint; instead
        /// we just drop the overlay claims and forget them.
        /// </summary>
        public void NotifyMainTilemapCleared()
        {
            if (!IsInitialized) return;
            for (int i = 0; i < _claimedThisFrame.Count; i++)
                _overlayTilemap.SetTile(_claimedThisFrame[i].Pos, null);
            _claimedThisFrame.Clear();
        }

        public void PostRender(Zone zone, int width, int height)
        {
            if (!IsInitialized || _mainTilemap == null) return;
            using var _ = s_PostRenderMarker.Auto();

            // PASS 15 R3 — release last frame's claims by RESTORING the
            // displaced glyphs. On the dirty path only a handful of
            // cells were repainted; every other claimed cell needs its
            // glyph back so the rescan below can re-resolve it (the old
            // code left them null → the environment blanked out around
            // the dirty cells). This also fixes the toggle-off hole:
            // disabling sprite mode now restores the full ASCII view
            // immediately instead of waiting for a full redraw.
            for (int i = 0; i < _claimedThisFrame.Count; i++)
            {
                var claim = _claimedThisFrame[i];
                _overlayTilemap.SetTile(claim.Pos, null);
                _mainTilemap.SetTile(claim.Pos, claim.MainTile);
                _mainTilemap.SetColor(claim.Pos, claim.MainColor);
            }
            _claimedThisFrame.Clear();

            if (!RenderingEnabled || zone == null) return;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector3Int(x, y, 0);

                    // PASS 15 R2 — the tilemap is painted VERTICALLY
                    // FLIPPED relative to zone space (ZoneRenderer
                    // paints zone cell (x, zy) at tile row
                    // Height-1-zy). Every zone lookup below must use
                    // the flipped row or the blueprint tier resolves
                    // sprites from the MIRRORED cell — the audit's
                    // "player renders reflected across the midline"
                    // defect.
                    int zoneY = height - 1 - y;

                    // PASS 15 R5 — ONE top-entity fetch per cell,
                    // shared by the pre-pass and every resolver tier
                    // (previously fetched twice, plus a third
                    // GetPart<CropPart> scan on every cell).
                    Entity topEntity = TopEntityAt(zone, x, zoneY);

                    // Pass 10 — entity-based pre-pass. Chest + lantern
                    // entities don't always paint their RenderString
                    // glyph to the main tilemap (they share cells with
                    // a Floor entity that wins the paint race), so the
                    // glyph-only scan misses them. Look directly at
                    // the cell's top entity and force-paint when its
                    // blueprint matches a sprite-emitting kind.
                    Tile entityTile = TryEntityBasedTile(topEntity);
                    if (entityTile != null)
                    {
                        ClaimCell(pos, entityTile, _mainTilemap.GetColor(pos));
                        continue;
                    }

                    var existingTile = _mainTilemap.GetTile(pos);
                    if (existingTile == null) continue;

                    char glyph = ExtractGlyph(existingTile);
                    if (glyph == '\0') continue;

                    Tile target = ChooseTile(zone, x, zoneY, glyph, topEntity, out bool authoredColor);
                    if (target == null) continue;

                    var color = _mainTilemap.GetColor(pos);
                    if (authoredColor)
                    {
                        // Pass 13: actor sprites carry their own palette —
                        // apply only the cell's LIGHTING (max channel of
                        // the glyph color, which already includes the
                        // lightmap) as a gray tint, never the glyph hue.
                        float v = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                        color = new Color(v, v, v, color.a);
                    }
                    ClaimCell(pos, target, color);
                }
            }
        }

        private void ClaimCell(Vector3Int pos, Tile target, Color color)
        {
            _claimedThisFrame.Add(new Claim
            {
                Pos = pos,
                MainTile = _mainTilemap.GetTile(pos),
                MainColor = _mainTilemap.GetColor(pos),
            });
            _overlayTilemap.SetTile(pos, target);
            _overlayTilemap.SetColor(pos, color);
            _mainTilemap.SetTile(pos, null);
        }

        /// <summary>
        /// Pass 10 — entity-based override. Returns a Tile when the
        /// cell hosts a chest / lantern blueprint, regardless of which
        /// glyph the cell currently paints. Returns null otherwise.
        /// (Pass 15 R5: takes the already-fetched top entity.)
        /// </summary>
        private Tile TryEntityBasedTile(Entity topEntity)
        {
            string bp = topEntity?.BlueprintName;
            if (string.IsNullOrEmpty(bp)) return null;
            if (BlueprintIsChest(bp))   return _chestTile;
            if (BlueprintIsLantern(bp)) return _lanternTile;
            if (BlueprintIsBed(bp))     return _bedTile;
            if (BlueprintIsCorpse(bp))  return _corpseTile;
            return null;
        }

        private static bool BlueprintIsBed(string bp)
        {
            // Pass 11 — bed blueprints in Objects.json. Includes "Bed"
            // exact match plus variants (StrawBed, RoyalBed, etc.).
            // Distinct from "Bedroll" (carryable) — we EndsWith here
            // so "Bedroll" doesn't spuriously claim a bed tile.
            if (string.IsNullOrEmpty(bp)) return false;
            return bp.EndsWith("Bed", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool BlueprintIsCorpse(string bp)
        {
            // Pass 11 — corpse blueprints. Most named entities use the
            // suffix "Corpse" (e.g. SnapjawCorpse). We match suffix to
            // avoid colliding with names that have "corpse" in the
            // middle (none currently, but safer if content grows).
            if (string.IsNullOrEmpty(bp)) return false;
            return bp.EndsWith("Corpse", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Pass 12 — exact-blueprint-name terrain/fixture kinds.
        /// Exact match ONLY: near-miss names (SandstoneFloor, SilverSand,
        /// WellGroundMarker, WellKeeper) must keep their own rendering.
        /// Contract pinned by EnvironmentSpriteRendererBlueprintTests.</summary>
        public enum EnvFixtureKind
        {
            None, Grass, Sand, Bank, Well, MarketStall, TinkersForge, AlchemyStill, Pillar,
            // Pass 14
            OilSeep, AcidPond, Rubble, Oven, IceStalactite, VineWall, SandstoneWall
        }

        /// <summary>Pass 13 — actor sprites (STYLE-GUIDE.md §6). The
        /// player plus the living Snapjaw family; corpses are excluded
        /// (they keep Pass 11 corpse handling). Actor sprites are
        /// AUTHORED-COLOR: the renderer applies only lighting value,
        /// never the glyph hue — a &amp;Y player must not render yellow.</summary>
        public enum ActorSpriteKind
        {
            None, Player, Snapjaw,
            // Pass 14
            Villager, Merchant, Elder, Warden, Child, SporeShambler, IceWight
        }

        public static ActorSpriteKind ResolveActorKind(string blueprintName)
        {
            if (string.IsNullOrEmpty(blueprintName)) return ActorSpriteKind.None;
            if (blueprintName == "Player") return ActorSpriteKind.Player;
            if (blueprintName.StartsWith("Snapjaw", System.StringComparison.Ordinal)
                && !blueprintName.EndsWith("Corpse", System.StringComparison.OrdinalIgnoreCase))
                return ActorSpriteKind.Snapjaw;
            switch (blueprintName)
            {
                // Robed townsfolk share one sprite; role NPCs with a
                // distinct read get their own.
                case "Villager":
                case "Innkeeper":
                case "WellKeeper":
                case "Scribe":        return ActorSpriteKind.Villager;
                case "Merchant":      return ActorSpriteKind.Merchant;
                case "Elder":         return ActorSpriteKind.Elder;
                case "Warden":        return ActorSpriteKind.Warden;
                case "VillageChild":  return ActorSpriteKind.Child;
                case "SporeShambler": return ActorSpriteKind.SporeShambler;
                case "IceWight":      return ActorSpriteKind.IceWight;
                default:              return ActorSpriteKind.None;
            }
        }

        /// <summary>Pass 12 — stage-aware crop sprite kinds. Stage 0 is a
        /// generic tilled mound (safe for ANY *Crop blueprint, including
        /// future ones); stage 1+ maps only KNOWN crops so an unknown
        /// sprout keeps its CP437 glyph instead of borrowing a look.
        /// Stage -1 = entity had no CropPart → None.</summary>
        public enum CropSpriteKind { None, Seed, CandyCarrot, Emberwheat }

        public static EnvFixtureKind ResolveFixtureKind(string blueprintName)
        {
            switch (blueprintName)
            {
                case "Grass":        return EnvFixtureKind.Grass;
                case "Sand":         return EnvFixtureKind.Sand;
                case "Bank":         return EnvFixtureKind.Bank;
                case "Well":         return EnvFixtureKind.Well;
                case "MarketStall":  return EnvFixtureKind.MarketStall;
                case "TinkersForge": return EnvFixtureKind.TinkersForge;
                case "AlchemyStill": return EnvFixtureKind.AlchemyStill;
                // Pass 13: both ruin pieces share the pillar sprite.
                // BrokenColumn paints ',' and previously fell through
                // to the BONES glyph mapping.
                case "Pillar":       return EnvFixtureKind.Pillar;
                case "BrokenColumn": return EnvFixtureKind.Pillar;
                // Pass 14 — wrong-visual fixes: OilSeep/AcidPond
                // rendered as WATER, Rubble as BONES, Oven as a bare
                // WALL before the blueprint tier claimed them.
                case "OilSeep":       return EnvFixtureKind.OilSeep;
                case "AcidPond":      return EnvFixtureKind.AcidPond;
                case "Rubble":        return EnvFixtureKind.Rubble;
                case "Oven":          return EnvFixtureKind.Oven;
                case "IceStalactite": return EnvFixtureKind.IceStalactite;
                // Themed walls trade the 4-variant auto-tile for
                // identity; Wall/StoneWall stay on the generic atlas.
                case "VineWall":      return EnvFixtureKind.VineWall;
                case "SandstoneWall": return EnvFixtureKind.SandstoneWall;
                default:             return EnvFixtureKind.None;
            }
        }

        public static CropSpriteKind ResolveCropKind(string blueprintName, int growthStage)
        {
            if (string.IsNullOrEmpty(blueprintName)) return CropSpriteKind.None;
            if (!blueprintName.EndsWith("Crop", System.StringComparison.Ordinal))
                return CropSpriteKind.None;
            if (growthStage < 0) return CropSpriteKind.None; // no CropPart
            if (growthStage == 0) return CropSpriteKind.Seed;
            switch (blueprintName)
            {
                case "CandyCarrotCrop": return CropSpriteKind.CandyCarrot;
                case "EmberwheatCrop":  return CropSpriteKind.Emberwheat;
                default:                return CropSpriteKind.None;
            }
        }

        private Tile ChooseTile(Zone zone, int x, int y, char glyph, Entity topEntity, out bool authoredColor)
        {
            authoredColor = false;
            // Pass 12 — blueprint-keyed resolution FIRST. Three families
            // whose glyphs are ambiguous or collide with earlier claims:
            //  - terrain identity: Grass/Sand/Bank all paint '.' and were
            //    swallowed by the generic stone floor atlas;
            //  - farming crops: stage-0 '.' vanished into the floor, and
            //    the CandyCarrot sprout 't' rendered as a CACTUS;
            //  - village fixtures: Well 'O' / TinkersForge 'n' /
            //    AlchemyStill '&' were uncovered, MarketStall '=' rendered
            //    as a bed. Null tiles (missing PNG) fall through to the
            //    original glyph handling below.
            // (Pass 15 R5: topEntity arrives pre-fetched; x/y are ZONE
            // coordinates — the caller has already un-flipped them.)
            {
                string bpName = topEntity?.BlueprintName;

                // Pass 13 — actor tier runs before everything: the
                // player and snapjaw family render as authored-color
                // Muted Overgrowth sprites (STYLE-GUIDE.md §6/§8).
                switch (ResolveActorKind(bpName))
                {
                    case ActorSpriteKind.Player:
                        if (_playerTile != null) { authoredColor = true; return _playerTile; }
                        break;
                    case ActorSpriteKind.Snapjaw:
                        if (_snapjawTile != null) { authoredColor = true; return _snapjawTile; }
                        break;
                    // Pass 14 actors
                    case ActorSpriteKind.Villager:
                        if (_villagerTile != null) { authoredColor = true; return _villagerTile; }
                        break;
                    case ActorSpriteKind.Merchant:
                        if (_merchantTile != null) { authoredColor = true; return _merchantTile; }
                        break;
                    case ActorSpriteKind.Elder:
                        if (_elderTile != null) { authoredColor = true; return _elderTile; }
                        break;
                    case ActorSpriteKind.Warden:
                        if (_wardenTile != null) { authoredColor = true; return _wardenTile; }
                        break;
                    case ActorSpriteKind.Child:
                        if (_childTile != null) { authoredColor = true; return _childTile; }
                        break;
                    case ActorSpriteKind.SporeShambler:
                        if (_sporeShamblerTile != null) { authoredColor = true; return _sporeShamblerTile; }
                        break;
                    case ActorSpriteKind.IceWight:
                        if (_iceWightTile != null) { authoredColor = true; return _iceWightTile; }
                        break;
                }

                // Pass 15 R5: only crops pay for the CropPart scan —
                // previously every cell ran a full Parts walk for a
                // near-always-null result.
                int stage = -1;
                if (bpName != null && bpName.EndsWith("Crop", System.StringComparison.Ordinal))
                    stage = topEntity?.GetPart<CropPart>()?.GrowthStage ?? -1;
                switch (ResolveCropKind(bpName, stage))
                {
                    case CropSpriteKind.Seed:        if (_cropSeedTile != null) return _cropSeedTile; break;
                    case CropSpriteKind.CandyCarrot: if (_candyCarrotCropTile != null) return _candyCarrotCropTile; break;
                    case CropSpriteKind.Emberwheat:  if (_emberwheatCropTile != null) return _emberwheatCropTile; break;
                }

                switch (ResolveFixtureKind(bpName))
                {
                    case EnvFixtureKind.Grass:        if (_grassTile != null) return _grassTile; break;
                    case EnvFixtureKind.Sand:         if (_sandTile != null) return _sandTile; break;
                    case EnvFixtureKind.Bank:         if (_bankTile != null) return _bankTile; break;
                    case EnvFixtureKind.Well:         if (_wellTile != null) return _wellTile; break;
                    case EnvFixtureKind.MarketStall:  if (_marketStallTile != null) return _marketStallTile; break;
                    case EnvFixtureKind.TinkersForge: if (_forgeTile != null) return _forgeTile; break;
                    case EnvFixtureKind.AlchemyStill: if (_alchemyStillTile != null) return _alchemyStillTile; break;
                    case EnvFixtureKind.Pillar:       if (_pillarTile != null) return _pillarTile; break;
                    // Pass 14 fixtures/terrain
                    case EnvFixtureKind.OilSeep:       if (_oilSeepTile != null) return _oilSeepTile; break;
                    case EnvFixtureKind.AcidPond:      if (_acidPondTile != null) return _acidPondTile; break;
                    case EnvFixtureKind.Rubble:        if (_rubbleTile != null) return _rubbleTile; break;
                    case EnvFixtureKind.Oven:          if (_ovenTile != null) return _ovenTile; break;
                    case EnvFixtureKind.IceStalactite: if (_iceStalactiteTile != null) return _iceStalactiteTile; break;
                    case EnvFixtureKind.VineWall:      if (_vineWallTile != null) return _vineWallTile; break;
                    case EnvFixtureKind.SandstoneWall: if (_sandstoneWallTile != null) return _sandstoneWallTile; break;
                }
            }

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
                // Pass 14: dropped blades (Dagger/ShortSword/LongSword/
                // Spear/Cudgel all paint '/'). Near-gray art — the
                // color-copy tint keeps each weapon's glyph color as
                // its identity.
                case '/':  return _weaponGroundTile;
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
                string bp = topEntity?.BlueprintName;
                if (bp == null) return null;
                if (glyph == '[' && BlueprintIsChest(bp)) return _chestTile;
                if (glyph == '!' && BlueprintIsLantern(bp)) return _lanternTile;
            }
            return null;
        }

        /// <summary>Pass 12 — top visible entity itself (the crop resolver
        /// needs its CropPart, not just the blueprint name).</summary>
        private static Entity TopEntityAt(Zone zone, int x, int y)
        {
            if (zone == null) return null;
            var c = zone.GetCell(x, y);
            return c?.GetTopVisibleObject();
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
