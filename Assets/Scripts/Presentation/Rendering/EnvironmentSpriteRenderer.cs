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

        /// <summary>
        /// PASS 15 round 2 — remembered-but-not-visible cells claim
        /// TERRAIN sprites tinted with this dim gray-blue, mirroring
        /// ZoneRenderer's dark remembered glyphs. Public so tests pin
        /// the fog contract against the exact value.
        /// </summary>
        public static readonly Color RememberedTint = new Color(0.40f, 0.42f, 0.50f, 1f);

        /// <summary>
        /// PASS 15 round 2 (S4) — the ground patch painted under the
        /// player's cell warms toward candle-light so the player pops
        /// against any terrain. Values stay ≤1 (vertex-color multiply
        /// can't brighten past the sprite's own palette).
        /// </summary>
        public static readonly Color PlayerHighlightTint = new Color(1f, 0.97f, 0.76f, 1f);

        /// <summary>
        /// PASS 15 round 2 (S3) — blueprint-named role NPC sprites
        /// (5 shopkeepers + 4 hermits), palette-kin of the villager
        /// base. Public so tests pin the roster.
        /// </summary>
        /// <summary>Glyph = the blueprint's canonical RenderString —
        /// the reskin guard denies the sprite when a quest builder has
        /// mutated the entity's glyph (it isn't that NPC anymore).</summary>
        public static readonly (string Blueprint, string File, char Glyph)[] NamedActorSprites =
        {
            ("Weaponsmith",  "weaponsmith",   '@'),
            ("Armorer",      "armorer",       '@'),
            ("Apothecary",   "apothecary",    '@'),
            ("Arcanist",     "arcanist",      '@'),
            ("Provisioner",  "provisioner",   '@'),
            ("CaveHermit",   "cave_hermit",   '@'),
            ("DesertHermit", "desert_hermit", '@'),
            ("JungleHermit", "jungle_hermit", '@'),
            ("RuinsHermit",  "ruins_hermit",  '@'),
            // Round 3 — the remaining townsfolk
            ("Farmer",       "farmer",        '@'),
            ("Undertaker",   "undertaker",    'U'),
            ("Marceline",    "marceline",     'M'),
            // W5.6 sweep — the W5.4 villagers were shipped as bare
            // '@'s; the people the catacomb design leads with get
            // faces (olm-pale, per the canon morphology).
            ("CatacombWarden", "catacomb_warden", '@'),
            ("PlaqueTender",   "plaque_tender",   '@'),
        };

        /// <summary>Round 3 — current render glyph of an entity, '\0'
        /// when unreadable. The reskin guard's input.</summary>
        private static char CurrentGlyphOf(Entity e)
        {
            var rs = e?.GetPart<RenderPart>()?.RenderString;
            return string.IsNullOrEmpty(rs) ? '\0' : rs[0];
        }

        /// <summary>Canonical glyph across BOTH blueprint tables (role
        /// NPCs + bestiary). Instance code uses the _namedActorGlyphs
        /// dict; this static scan exists for tests.</summary>
        public static char NamedActorCanonicalGlyph(string blueprint)
        {
            for (int i = 0; i < NamedActorSprites.Length; i++)
                if (NamedActorSprites[i].Blueprint == blueprint)
                    return NamedActorSprites[i].Glyph;
            for (int i = 0; i < CreatureSprites.Length; i++)
                if (CreatureSprites[i].Blueprint == blueprint)
                    return CreatureSprites[i].Glyph;
            return '\0';
        }

        /// <summary>Canonical glyphs per actor kind (from Objects.json):
        /// humanoid townsfolk are '@'; the rest are species letters.</summary>
        private static char KindCanonicalGlyph(ActorSpriteKind kind)
        {
            switch (kind)
            {
                case ActorSpriteKind.Player:        return '@';
                case ActorSpriteKind.Snapjaw:       return 's';
                case ActorSpriteKind.Villager:      return '@';
                case ActorSpriteKind.Merchant:      return '@';
                case ActorSpriteKind.Elder:         return '@';
                case ActorSpriteKind.Warden:        return '@';
                case ActorSpriteKind.Child:         return 'c';
                case ActorSpriteKind.SporeShambler: return 'f';
                case ActorSpriteKind.IceWight:      return 'W';
                default:                            return '\0';
            }
        }

        /// <summary>
        /// ROUND 5 — the BESTIARY: 44 hostile/wild creatures, blueprint-
        /// keyed like the role NPCs, canonical glyph for the reskin
        /// guard (dirt-gnome-style reskins keep honest letters).
        /// </summary>
        public static readonly (string Blueprint, string File, char Glyph)[] CreatureSprites =
        {
            ("CaveBear", "cave_bear", 'B'), ("BrittleHound", "brittle_hound", 'b'),
            ("DesertProwler", "desert_prowler", 'D'), ("PetDog", "pet_dog", 'd'),
            ("JungleStalker", "jungle_stalker", 'J'), ("PaleStalker", "pale_stalker", 'p'),
            ("JungleApe", "jungle_ape", 'A'), ("StoneGolem", "stone_golem", 'G'),
            ("ObsidianBrute", "obsidian_brute", 'O'), ("Mosshulk", "mosshulk", 'M'),
            ("SleepingTroll", "sleeping_troll", 'T'), ("AncientGuardian", "ancient_guardian", 'H'),
            ("CharredHusk", "charred_husk", 'H'), ("BrassHusk", "brass_husk", 'H'),
            ("DesertBandit", "desert_bandit", 'h'), ("AmbushBandit", "ambush_bandit", 'b'),
            ("RuneCultist", "rune_cultist", 'c'), ("RuinScavenger", "ruin_scavenger", 'r'),
            ("PaleCurator", "pale_curator", '@'), ("SaccharineEnvoy", "saccharine_envoy", '@'),
            ("GlassblownDrifter", "glassblown_drifter", '@'), ("PalimpsestEcho", "palimpsest_echo", '@'),
            ("Viper", "viper", '~'), ("SandWurm", "sand_wurm", 'W'),
            ("GiantSpider", "giant_spider", 'S'), ("Scorpion", "scorpion", 'x'),
            ("GlassScorpion", "glass_scorpion", 's'), ("CaveBat", "cave_bat", 'b'),
            ("Magpie", "magpie", 'b'), ("CaveSlime", "cave_slime", 'j'),
            ("Rotling", "rotling", 'r'), ("Glowmaw", "glowmaw", 'O'),
            ("SkeletalSentry", "skeletal_sentry", 'Z'), ("VaultSentinel", "vault_sentinel", 'V'),
            ("ChoirTendril", "choir_tendril", 'F'), ("CanopyStrangler", "canopy_strangler", 'C'),
            ("DuneLurker", "dune_lurker", 'd'), ("Mogu", "mogu", 'M'),
            ("Grib", "grib", 'G'), ("Nam", "nam", 'N'),
            ("Sien", "sien", 'S'), ("Sopp", "sopp", 's'),
            ("SnapjawChieftain", "snapjaw_chieftain", 'S'), ("SnapjawWarlord", "snapjaw_warlord", 'S'),
            // W5.6 — Ginmere makes the drowned sima reachable, and its
            // headline fauna stops being a bare letter.
            ("GinFrog", "gin_frog", 'f'),
            // W6.3a — the tepui's lowland band. The Sari-Snake and the
            // Wardline share a glyph ('s') and are told apart by their
            // art, which is the point of having art.
            ("SariSnake", "sari_snake", 's'),
            ("Wardline", "wardline", 's'),
            ("CascadeFather", "cascade_father", 'f'),
            ("GlasspaneFrog", "glasspane_frog", 'f'),
            ("YellowfootWayfarer", "yellowfoot_wayfarer", 't'),
        };

        /// <summary>ROUND 5 — interactable fixtures resolved in the
        /// entity pre-pass (blueprint-keyed, glyph-independent).</summary>
        public static readonly (string Blueprint, string File)[] FixtureSprites =
        {
            ("BerryBush", "berry_bush"), ("Beehive", "beehive"),
            ("HollowStump", "hollow_stump"), ("MushroomRing", "mushroom_ring"),
            ("Signpost", "signpost"),
            // LOOT OVERHAUL SM4 — the container family. Blueprint-keyed
            // like every other fixture, so they resolve glyph-independently
            // through the entity pre-pass.
            ("Crate", "cont_crate"), ("Sack", "cont_sack"),
            ("Urn", "cont_urn"), ("StrongBox", "cont_strongbox"),
            ("OreCache", "cont_ore_cache"), ("BoneCache", "cont_bone_cache"),
            ("WovenBasket", "cont_woven_basket"), ("HollowLog", "cont_hollow_log"),
            ("Reliquary", "cont_reliquary"), ("Bookshelf", "cont_bookshelf"),
            ("WeaponRack", "cont_weapon_rack"), ("AlchemyShelf", "cont_alchemy_shelf"),

            // W5 — the vertical world. Blueprint-keyed on purpose:
            // these share glyphs with unrelated content ('=' is the
            // plaque-wall AND a ledge, '*' is the hearth-patch AND a
            // campfire), and the glyph tier's false-identity guard
            // exists precisely because a shared glyph is not an
            // identity. Keyed here, each resolves to its own art and
            // nothing else can claim it.
            ("SinkholeLip", "sinkhole_lip"), ("DescentLedge", "descent_ledge"),
            ("RopeAnchor", "rope_anchor"),
            ("HearthPatch", "hearth_patch"), ("NicheHome", "niche_home"),
            ("DroseraRing", "drosera_ring"), ("BeetleJar", "beetle_jar"),
            ("PebbleSundewThreshold", "pebble_sundew"),
            // The wall and its plaques share one slab; the smoothed
            // niche gets its OWN art, because the whole point of it is
            // that there is nothing cut into it.
            ("PlaqueWall", "plaque_wall"),
            ("PlaqueRiane", "plaque_wall"), ("PlaqueOssu", "plaque_wall"),
            ("PlaqueMerrin", "plaque_wall"), ("PlaqueVashti", "plaque_wall"),
            ("PlaqueOldest", "plaque_wall"),
            ("PlaqueSmoothed", "plaque_smoothed"),
            // W5.5 — the Cathedral. NOTE the tint regime: this tier
            // never sets authoredColor, so a fixture sprite is
            // MULTIPLIED by its blueprint's glyph colour. These three
            // paint &y/&Y deliberately; a &K blueprint would render its
            // art near-black (which is why SinkholeLip's colour moved).
            ("SubstrateVault", "substrate_vault"),
            // W6.2a — the Grainfield's ridge (bulk-stamped: variants).
            ("GrainRidge", "grain_ridge"),
            ("StoneDome", "stone_dome"), ("TankBrocchinia", "tank_brocchinia"),
            ("ChoirNode", "choir_node"),
            ("EncasedElder", "encased_elder"),
        };

        /// <summary>W5 cold-eye — fixtures stamped in bulk (the vault
        /// covers up to 272 cells of a Cathedral floor) get position-
        /// hashed variants so the field doesn't read as wallpaper.
        /// A blueprint listed here loads <c>file</c> (variant 0) plus
        /// <c>file_v1..file_vN-1</c>; cells pick one via
        /// <see cref="FixtureVariantIndex"/>. Fixtures NOT listed keep
        /// the single-tile path untouched.</summary>
        public static readonly Dictionary<string, int> FixtureVariantCounts =
            new Dictionary<string, int>
        {
            { "SubstrateVault", 4 },
            { "GrainRidge", 4 },
            { "StoneDome", 3 },
        };

        private readonly Dictionary<string, Tile> _namedActorTiles =
            new Dictionary<string, Tile>(80);
        private readonly Dictionary<string, char> _namedActorGlyphs =
            new Dictionary<string, char>(80);
        private readonly Dictionary<string, Tile> _fixtureBlueprintTiles =
            new Dictionary<string, Tile>(8);
        private readonly Dictionary<string, Tile[]> _fixtureVariantTiles =
            new Dictionary<string, Tile[]>(2);
        private readonly Dictionary<string, Tile> _itemBodyTiles =
            new Dictionary<string, Tile>(16);
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

        // ── PASS 15 G — flowing ground ───────────────────────────
        // Macro-field ground: each material is a seamless toroidal
        // 64×64 field sliced 4×4; the game reassembles it by tilemap
        // position, so variation lives LARGER than the tile and the
        // grid disappears. Shorelines are a complete 12-piece scallop
        // family; walls carry the top-face treatment.

        /// <summary>Blueprint-keyed ground material. This is ALSO the
        /// fix for five strata floors collapsing into one ochre atlas
        /// and SlateFloor (glyph ',') rendering as the BONES sprite.</summary>
        public enum GroundMaterial
        {
            None, Grass, Sand, Floor, Bank,
            Sandstone, Limestone, Shale, Slate, Quartzite, Obsidian,
            Water,
            /// <summary>W6 — the tepui's pink-grey sandstone. Its own
            /// material rather than Sandstone's, because ground macro
            /// tiles paint at AUTHORED colour (unlike the fixture tier,
            /// which multiplies by glyph colour), so sharing the
            /// desert's tileset would have painted the god-tree's
            /// stump desert-yellow.</summary>
            Tepui
        }

        public static GroundMaterial ResolveGroundMaterial(string blueprintName)
        {
            switch (blueprintName)
            {
                case "Grass":          return GroundMaterial.Grass;
                case "Sand":           return GroundMaterial.Sand;
                case "SilverSand":     return GroundMaterial.Sand;
                case "Bank":           return GroundMaterial.Bank;
                case "Floor":          return GroundMaterial.Floor;
                case "StoneFloor":     return GroundMaterial.Floor;
                case "SandstoneFloor": return GroundMaterial.Sandstone;
                case "LimestoneFloor": return GroundMaterial.Limestone;
                case "ShaleFloor":     return GroundMaterial.Shale;
                case "SlateFloor":     return GroundMaterial.Slate;
                case "QuartziteFloor": return GroundMaterial.Quartzite;
                case "ObsidianFloor":  return GroundMaterial.Obsidian;
                case "WaterPuddle":    return GroundMaterial.Water;
                // W6.2a — the gorge's spray rides the water tileset +
                // the '~' animation family.
                case "SprayPool":      return GroundMaterial.Water;
                case "TepuiStone":     return GroundMaterial.Tepui;

                // W1 formations. A packed road IS a ground surface — the
                // old stone under the earth — so it paints as floor rather
                // than leaving a hole in the flowing field for a '=' glyph
                // to sit in. Rubble and ash are likewise surfaces, not
                // objects standing on one.
                case "RoadStone":      return GroundMaterial.Floor;
                case "Rubble":         return GroundMaterial.Floor;
                case "AshPile":        return GroundMaterial.Floor;
                case "AshBed":         return GroundMaterial.Floor;
                default:               return GroundMaterial.None;
            }
        }

        private readonly Dictionary<GroundMaterial, Tile[]> _groundMacroTiles =
            new Dictionary<GroundMaterial, Tile[]>();
        private Tile[] _waterMacroTiles;
        private Tile[] _shoreEdgeTiles;   // n, e, s, w
        private Tile[] _shoreOuterTiles;  // ne, se, sw, nw
        private Tile[] _shoreInnerTiles;  // ne, se, sw, nw
        private Tile[] _wallTopTiles;          // generic wall, 4 variants
        private Tile[] _vineWallTopTiles;
        private Tile[] _sandstoneWallTopTiles;
        private Tile[] _tepuiWallTopTiles;      // W6 — the tepui's own outcrops

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
            // Round 3 — the tile WE wrote (bg claims): restore-guard
            // token so a ZoneRenderer bg repaint is never clobbered.
            public TileBase Written;
        }
        // ROUND 4 — claims persist across frames (keyed by tile pos);
        // the incremental path releases/re-resolves only dirty cells.
        private readonly Dictionary<Vector3Int, Claim> _claims =
            new Dictionary<Vector3Int, Claim>(2048);

        // PASS 15 R5 — this scan was invisible to the perf budget table.
        private static readonly Unity.Profiling.ProfilerMarker s_PostRenderMarker =
            new Unity.Profiling.ProfilerMarker("COO.EnvSprites.PostRender");

        public bool IsInitialized { get; private set; }

        // PASS 15 V-loop fix #1: cells still rendered as ASCII (actors,
        // items) kept their opaque dark background boxes — white
        // letters on floating black squares punching holes across the
        // flowing terrain (screenshot evidence, first live run). When
        // the cell's terrain resolves a ground material, we now paint
        // that ground sprite INTO the bg tilemap so the letter sits
        // directly on grass/stone.
        private Tilemap _bgTilemap;
        private readonly Dictionary<Vector3Int, Claim> _bgClaims =
            new Dictionary<Vector3Int, Claim>(512);

        public void Init(Transform gridParent, Tilemap mainTilemap, Tilemap bgTilemap = null)
        {
            _mainTilemap = mainTilemap;
            _bgTilemap = bgTilemap;

            // Make overlay tilemap
            var go = new GameObject("EnvironmentSpriteTilemap");
            go.transform.SetParent(gridParent, false);
            _overlayTilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = OverlaySortingOrder;

            LoadSprites();
            BuildTiles();
            LoadGroundSets();

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

        // PASS 15 G — macro/shoreline/wall-set loaders. Missing files
        // return empty arrays and the resolver falls back to the old
        // singles/atlas, so a partial art drop never blanks the world.
        private Tile[] LoadTileSet(string prefix, string[] suffixes)
        {
            var tiles = new Tile[suffixes.Length];
            bool any = false;
            for (int i = 0; i < suffixes.Length; i++)
            {
                var s = LoadSingle(SpriteRoot + prefix + suffixes[i]);
                if (s != null) { tiles[i] = MakeTile(s, prefix + suffixes[i]); any = true; }
            }
            return any ? tiles : System.Array.Empty<Tile>();
        }

        private static readonly string[] MacroSuffixes =
        {
            "_m00","_m01","_m02","_m03","_m04","_m05","_m06","_m07",
            "_m08","_m09","_m10","_m11","_m12","_m13","_m14","_m15",
        };
        private static readonly string[] SideSuffixes   = { "_e_n", "_e_e", "_e_s", "_e_w" };
        private static readonly string[] OuterSuffixes  = { "_oc_ne", "_oc_se", "_oc_sw", "_oc_nw" };
        private static readonly string[] InnerSuffixes  = { "_ic_ne", "_ic_se", "_ic_sw", "_ic_nw" };
        private static readonly string[] WallVarSuffixes = { "_v0", "_v1", "_v2", "_v3" };

        private void LoadGroundSets()
        {
            foreach (GroundMaterial mat in System.Enum.GetValues(typeof(GroundMaterial)))
            {
                if (mat == GroundMaterial.None || mat == GroundMaterial.Water) continue;
                var set = LoadTileSet(mat.ToString().ToLowerInvariant(), MacroSuffixes);
                if (set.Length > 0) _groundMacroTiles[mat] = set;
            }
            _waterMacroTiles = LoadTileSet("water", MacroSuffixes);
            _shoreEdgeTiles = LoadTileSet("water", SideSuffixes);
            _shoreOuterTiles = LoadTileSet("water", OuterSuffixes);
            _shoreInnerTiles = LoadTileSet("water", InnerSuffixes);
            _wallTopTiles = LoadTileSet("wall", WallVarSuffixes);
            _vineWallTopTiles = LoadTileSet("vine_wall", WallVarSuffixes);
            _sandstoneWallTopTiles = LoadTileSet("sandstone_wall", WallVarSuffixes);
            _tepuiWallTopTiles     = LoadTileSet("tepui_wall", WallVarSuffixes);
        }

        private static Tile MakeTile(Sprite s, string name)
        {
            if (s == null) return null;
            var t = ScriptableObject.CreateInstance<Tile>();
            t.sprite = s;
            t.name = name;
            // ROUND 2 root-cause fix: a fresh Tile defaults to
            // TileFlags.LockColor, which turns every later SetColor on
            // a claimed cell into a SILENT NO-OP. This is why FOV
            // dimming never reached sprite terrain — the claims were
            // already copying the dim glyph color, and LockColor threw
            // it away.
            t.flags = TileFlags.None;
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
            // PASS 15 round 2 (S3) — role NPCs keyed by BLUEPRINT
            // name. Adding a future named-NPC sprite = drop a PNG +
            // one row here; no enum/field/switch triple to extend.
            _namedActorTiles.Clear();
            _namedActorGlyphs.Clear();
            foreach (var (blueprint, file, glyph) in NamedActorSprites)
            {
                var s = LoadSingle(SpriteRoot + file);
                if (s != null) _namedActorTiles[blueprint] = MakeTile(s, blueprint);
                _namedActorGlyphs[blueprint] = glyph;
            }
            // Round 5 — the bestiary rides the same blueprint-keyed
            // actor mechanism (and the same reskin guard).
            foreach (var (blueprint, file, glyph) in CreatureSprites)
            {
                var s = LoadSingle(SpriteRoot + file);
                if (s != null) _namedActorTiles[blueprint] = MakeTile(s, blueprint);
                _namedActorGlyphs[blueprint] = glyph;
            }
            // Round 5 — interactable fixtures (entity pre-pass).
            _fixtureBlueprintTiles.Clear();
            _fixtureVariantTiles.Clear();
            foreach (var (blueprint, file) in FixtureSprites)
            {
                var s = LoadSingle(SpriteRoot + file);
                if (s != null) _fixtureBlueprintTiles[blueprint] = MakeTile(s, blueprint);

                // W5 cold-eye — bulk-stamped fixtures carry _v1..vN-1
                // siblings; the base file doubles as variant 0. A
                // missing sibling falls back to the base so a bad
                // import degrades to the old look, never to a hole.
                if (s != null && FixtureVariantCounts.TryGetValue(blueprint, out int n))
                {
                    var tiles = new Tile[n];
                    tiles[0] = _fixtureBlueprintTiles[blueprint];
                    for (int v = 1; v < n; v++)
                    {
                        var sv = LoadSingle(SpriteRoot + file + "_v" + v);
                        tiles[v] = sv != null ? MakeTile(sv, blueprint + "_v" + v) : tiles[0];
                    }
                    _fixtureVariantTiles[blueprint] = tiles;
                }
            }
            // Round 5 — item bodies (near-gray, tinted by glyph color
            // at claim time — one vial serves every tonic).
            _itemBodyTiles.Clear();
            foreach (var body in new[] { "item_vial", "item_book", "item_gem",
                "item_key", "item_torch", "item_meat", "item_fruit", "item_seed",
                "item_armor", "item_bone", "item_vein", "item_scroll" })
            {
                var s = LoadSingle(SpriteRoot + body);
                if (s != null) _itemBodyTiles[body] = MakeTile(s, body);
            }
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
            foreach (var kvp in _claims)
                _overlayTilemap.SetTile(kvp.Key, null);
            _claims.Clear();
            _bgClaims.Clear(); // bg was cleared with the rest
        }

        public void PostRender(Zone zone, int width, int height)
            => PostRender(zone, width, height, null);

        /// <summary>
        /// ROUND 4 (perf) — two paths:
        /// <para>FULL (dirtyKeys == null): release every claim, rescan
        /// every cell. Runs on RenderZone (player moved, zone changed).</para>
        /// <para>INCREMENTAL (dirtyKeys != null): release + re-resolve
        /// ONLY the dirty cells and their 8-neighborhoods (wall
        /// variants and shoreline edges read neighbors). The audit
        /// measured the old always-full design at ~13-15k tilemap
        /// writes per NPC-step repaint; claims now persist across
        /// frames and untouched cells cost nothing.</para>
        /// </summary>
        public void PostRender(Zone zone, int width, int height, HashSet<int> dirtyKeys)
        {
            if (!IsInitialized || _mainTilemap == null) return;
            using var _ = s_PostRenderMarker.Auto();
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            Perf.Frames++;

            if (!RenderingEnabled || zone == null || dirtyKeys == null)
            {
                Perf.FullPasses++;
                // PASS 15 R3 — release by RESTORING displaced glyphs
                // (toggle-off shows ASCII at once; the rescan below
                // re-resolves everything).
                ReleaseClaims();
                if (RenderingEnabled && zone != null)
                {
                    for (int x = 0; x < width; x++)
                        for (int y = 0; y < height; y++)
                            ResolveCell(zone, x, height - 1 - y, new Vector3Int(x, y, 0));
                }
                Perf.Accumulate(t0);
                return;
            }

            // INCREMENTAL — expand each dirty cell to its 8-neighborhood
            // (wall top-face variants + shoreline masks are functions of
            // neighbors), then release + re-resolve only those.
            Perf.IncrementalPasses++;
            _incrScratch.Clear();
            foreach (int key in dirtyKeys)
            {
                int cx = key % Zone.Width;
                int cy = key / Zone.Width;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        _incrScratch.Add(ny * Zone.Width + nx);
                    }
                }
            }
            foreach (int key in _incrScratch)
            {
                int zx = key % Zone.Width;
                int zy = key / Zone.Width;
                var pos = new Vector3Int(zx, height - 1 - zy, 0);
                ReleaseClaimAt(pos);
                ReleaseBgClaimAt(pos);
                ResolveCell(zone, zx, zy, pos);
            }
            Perf.Accumulate(t0);
        }

        /// <summary>One cell through every tier — the body of the old
        /// full-scan loop, extracted so the incremental path can run it
        /// per dirty cell. (x, zoneY) are ZONE coordinates; pos is the
        /// flipped TILE position (R2 mirror contract).</summary>
        private void ResolveCell(Zone zone, int x, int zoneY, Vector3Int pos)
        {
            Perf.CellsResolved++;
            // PASS 15 round 2 — FOG OF WAR. Unexplored cells are never
            // claimed. Remembered-not-visible cells claim TERRAIN ONLY,
            // dimmed, mirroring RenderRememberedCell's contract.
            var cell = zone.GetCell(x, zoneY);
            if (cell == null || !cell.Explored) return;
            bool visible = cell.IsVisible;
            Color tint = visible ? Color.white : RememberedTint;

            // PASS 15 R5 — ONE top-entity fetch per cell. In remembered
            // fog the "top entity" is the cell's TERRAIN, so actors
            // never resolve there.
            Entity topEntity = visible
                ? cell.GetTopVisibleObject()
                : TerrainEntityOf(cell);

            // Pass 10 — entity-based pre-pass (chest/lantern/bed/corpse
            // + campfire): blueprint-keyed, glyph-independent.
            Tile entityTile = visible ? TryEntityBasedTile(topEntity, x, zoneY) : null;
            if (entityTile != null)
            {
                ClaimCell(pos, entityTile, _mainTilemap.GetColor(pos));
                PaintGroundUnderAscii(zone, x, zoneY, pos, tint);
                return;
            }

            // PASS 15 V-loop fix #2 (round 2): ALL ground claims are
            // blueprint-driven, glyph-independent — the animated env
            // renderer strips water/grass/floor glyphs before this pass.
            var topGround = ResolveGroundMaterial(topEntity?.BlueprintName);
            if (topGround == GroundMaterial.Water)
            {
                var shoreline = PickShorelineTile(zone, x, zoneY);
                if (shoreline != null)
                {
                    ClaimCell(pos, shoreline, tint);
                    return;
                }
            }
            else if (topGround != GroundMaterial.None
                && _groundMacroTiles.TryGetValue(topGround, out var topMacro)
                && topMacro.Length == 16)
            {
                var mt = topMacro[MacroIndex(x, zoneY)];
                if (mt != null)
                {
                    ClaimCell(pos, mt, tint);
                    return;
                }
            }

            var existingTile = _mainTilemap.GetTile(pos);
            char glyph = existingTile != null ? ExtractGlyph(existingTile) : '\0';
            if (glyph == '\0')
            {
                // Animated-env-claimed cell (glyph stripped) — round 3:
                // still run the BLUEPRINT tiers (MarketStall etc.).
                Tile blindTarget = ChooseTile(zone, x, zoneY, '\0', topEntity, out bool _blindAuthored);
                if (blindTarget != null)
                    ClaimCell(pos, blindTarget, tint);
                PaintGroundUnderAscii(zone, x, zoneY, pos, tint);
                return;
            }

            Tile target = ChooseTile(zone, x, zoneY, glyph, topEntity, out bool authoredColor);
            if (target == null)
            {
                // Honest ASCII — put the GROUND under the letter.
                PaintGroundUnderAscii(zone, x, zoneY, pos, tint);
                return;
            }

            var color = _mainTilemap.GetColor(pos);
            if (!visible)
            {
                // Round 3 audit 🔵 — one dim, every tier.
                color = tint;
            }
            else if (authoredColor)
            {
                // Pass 13: authored palettes take only the LIGHTING
                // value (max channel), never the glyph hue.
                float v = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
                color = new Color(v, v, v, color.a);
            }
            ClaimCell(pos, target, color);
            // The PLAYER's cell gets the round-2 findability highlight.
            PaintGroundUnderAscii(zone, x, zoneY, pos,
                target == _playerTile ? PlayerHighlightTint : tint);
        }

        /// <summary>
        /// ROUND 4 — live perf counters for the sprite pass, readable
        /// via execute_code and reset per measurement window. The A/B
        /// evidence for the incremental-claims change lives here.
        /// </summary>
        public static class Perf
        {
            public static long Frames, FullPasses, IncrementalPasses;
            public static long CellsResolved, ClaimsMade, TilemapWrites;
            public static long TotalTicks, MaxTicks;

            public static void Reset()
            {
                Frames = FullPasses = IncrementalPasses = 0;
                CellsResolved = ClaimsMade = TilemapWrites = 0;
                TotalTicks = MaxTicks = 0;
            }

            internal static void Accumulate(long t0)
            {
                long dt = System.Diagnostics.Stopwatch.GetTimestamp() - t0;
                TotalTicks += dt;
                if (dt > MaxTicks) MaxTicks = dt;
            }

            public static string Snapshot()
            {
                double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                long f = Frames > 0 ? Frames : 1;
                return "frames=" + Frames
                    + " (full=" + FullPasses + " incr=" + IncrementalPasses + ")"
                    + " cells/frame=" + (CellsResolved / f)
                    + " claims/frame=" + (ClaimsMade / f)
                    + " writes/frame=" + (TilemapWrites / f)
                    + " avgMs=" + (TotalTicks * toMs / f).ToString("F2")
                    + " maxMs=" + (MaxTicks * toMs).ToString("F2");
            }
        }

        // Incremental-path scratch (expanded dirty neighborhood).
        private readonly HashSet<int> _incrScratch = new HashSet<int>();

        /// <summary>
        /// PASS 15 V-loop fix #1 — for a cell that keeps its ASCII
        /// glyph: find the cell's ground-material TERRAIN entity (not
        /// the top entity — that's the actor/item) and paint its macro
        /// slice into the BG tilemap, replacing the dark contrast box.
        /// The white letter then reads as standing ON the ground.
        /// </summary>
        private void PaintGroundUnderAscii(Zone zone, int x, int zoneY, Vector3Int pos, Color tint)
        {
            if (_bgTilemap == null) return;
            var cell = zone.GetCell(x, zoneY);
            if (cell == null) return;

            GroundMaterial ground = GroundMaterial.None;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                ground = ResolveGroundMaterial(cell.Objects[i].BlueprintName);
                if (ground != GroundMaterial.None) break;
            }
            if (ground == GroundMaterial.None || ground == GroundMaterial.Water) return;
            if (!_groundMacroTiles.TryGetValue(ground, out var macro) || macro.Length != 16) return;

            var mt = macro[MacroIndex(x, zoneY)];
            if (mt == null) return;

            if (_bgClaims.TryGetValue(pos, out var existingBg))
            {
                // Re-claim without release (defensive): KEEP the
                // original displaced snapshot — capturing our own
                // macro as "original" would leak it on release.
                existingBg.Written = mt;
                _bgClaims[pos] = existingBg;
            }
            else
            {
                _bgClaims[pos] = new Claim
                {
                    Pos = pos,
                    MainTile = _bgTilemap.GetTile(pos),
                    MainColor = _bgTilemap.GetColor(pos),
                    Written = mt,
                };
            }
            Perf.TilemapWrites += 3;
            _bgTilemap.SetTile(pos, mt);
            _bgTilemap.SetTileFlags(pos, TileFlags.None);
            // Round 2: the tint carries fog dimming (RememberedTint) and
            // the player's ground highlight — no more always-white.
            _bgTilemap.SetColor(pos, tint);
        }

        /// <summary>
        /// Release every live claim: overlay cells clear; displaced
        /// main/bg tiles restore UNLESS the ZoneRenderer repainted them
        /// after we claimed (Round 3 audit 🔴 — RenderDirtyCells runs
        /// before PostRender on the incremental path, and restoring a
        /// stale snapshot over its fresh paint made spriteless movers
        /// INVISIBLE to a stationary player: our ClaimCell leaves the
        /// main cell null, so non-null means someone painted since).
        /// </summary>
        private void ReleaseClaims()
        {
            foreach (var kvp in _claims)
                RestoreMainClaim(kvp.Value);
            _claims.Clear();
            if (_bgTilemap != null)
            {
                foreach (var kvp in _bgClaims)
                    RestoreBgClaim(kvp.Value);
            }
            _bgClaims.Clear();
        }

        /// <summary>ROUND 4 — targeted release for the incremental path.</summary>
        private void ReleaseClaimAt(Vector3Int pos)
        {
            if (!_claims.TryGetValue(pos, out var claim)) return;
            RestoreMainClaim(claim);
            _claims.Remove(pos);
        }

        private void ReleaseBgClaimAt(Vector3Int pos)
        {
            if (_bgTilemap == null || !_bgClaims.TryGetValue(pos, out var claim)) return;
            RestoreBgClaim(claim);
            _bgClaims.Remove(pos);
        }

        private void RestoreMainClaim(in Claim claim)
        {
            _overlayTilemap.SetTile(claim.Pos, null);
            Perf.TilemapWrites++;
            if (_mainTilemap.GetTile(claim.Pos) != null) return;
            _mainTilemap.SetTile(claim.Pos, claim.MainTile);
            // SetTile resets the cell's flags to the TILE asset's —
            // clear them or the color restore below can silently
            // no-op on a LockColor'd tile (round 2's root-cause
            // lesson applied to the restore path too).
            _mainTilemap.SetTileFlags(claim.Pos, TileFlags.None);
            _mainTilemap.SetColor(claim.Pos, claim.MainColor);
            Perf.TilemapWrites += 3;
        }

        private void RestoreBgClaim(in Claim claim)
        {
            // bg flavor of the round-3 guard: only restore if the bg
            // still shows the tile WE wrote.
            if (_bgTilemap.GetTile(claim.Pos) != claim.Written) return;
            _bgTilemap.SetTile(claim.Pos, claim.MainTile);
            _bgTilemap.SetTileFlags(claim.Pos, TileFlags.None);
            _bgTilemap.SetColor(claim.Pos, claim.MainColor);
            Perf.TilemapWrites += 3;
        }

        /// <summary>
        /// ROUND 3 audit 🟡 fix — fullscreen UIs (inventory, quest log)
        /// paint the MAIN tilemap (order 0) while this overlay (order 3)
        /// kept the last gameplay frame's sprites on top of their lower
        /// rows. ZoneRenderer calls this on its Paused transition so the
        /// UI opens over a clean slate.
        /// </summary>
        public void ReleaseAllClaims()
        {
            if (!IsInitialized) return;
            ReleaseClaims();
        }

        private void ClaimCell(Vector3Int pos, Tile target, Color color)
        {
            Perf.ClaimsMade++;
            if (!_claims.ContainsKey(pos))
            {
                _claims[pos] = new Claim
                {
                    Pos = pos,
                    MainTile = _mainTilemap.GetTile(pos),
                    MainColor = _mainTilemap.GetColor(pos),
                };
            }
            // else: re-claim without release (defensive) — keep the
            // ORIGINAL displaced snapshot; the main cell is null.
            Perf.TilemapWrites += 4;
            _overlayTilemap.SetTile(pos, target);
            // Guard against LockColor'd tiles from any future source —
            // without None flags the SetColor below silently no-ops.
            _overlayTilemap.SetTileFlags(pos, TileFlags.None);
            _overlayTilemap.SetColor(pos, color);
            _mainTilemap.SetTile(pos, null);
        }

        /// <summary>
        /// Pass 10 — entity-based override. Returns a Tile when the
        /// cell hosts a chest / lantern blueprint, regardless of which
        /// glyph the cell currently paints. Returns null otherwise.
        /// (Pass 15 R5: takes the already-fetched top entity.)
        /// </summary>
        /// <summary>
        /// Whether this entity will be drawn as a SPRITE rather than as a
        /// CP437 glyph.
        ///
        /// <para>Exposed for <c>GlyphGhostRenderer</c>, which must not smear
        /// an ASCII glyph behind an actor the player sees as pixel art. It
        /// cannot infer this from the main tilemap: the ghost pass runs
        /// BEFORE the sprite pass claims the cell, so the tilemap still
        /// holds the glyph at that moment.</para>
        /// </summary>
        public bool WillRenderAsSprite(Entity entity)
            => entity != null && TryEntityBasedTile(entity, 0, 0) != null;

        private Tile TryEntityBasedTile(Entity topEntity, int x, int y)
        {
            string bp = topEntity?.BlueprintName;
            if (string.IsNullOrEmpty(bp)) return null;
            if (BlueprintIsChest(bp))   return _chestTile;
            if (BlueprintIsLantern(bp)) return _lanternTile;
            if (BlueprintIsBed(bp))     return _bedTile;
            if (BlueprintIsCorpse(bp))  return _corpseTile;
            // Round 3 — the campfire uses GlyphVariants (flicker
            // frames), so the '*'-keyed glyph tier misses whenever the
            // current frame isn't '*' (the town's red-cross-with-'z'
            // finding). Blueprint-keyed = every frame; this also keeps
            // the tile-name-keyed fire light alive.
            if (bp == "Campfire")       return _campfireTile;
            // Round 5 — interactable fixtures (berry bush, beehive,
            // hollow stump, mushroom ring, signpost).
            if (_fixtureBlueprintTiles.TryGetValue(bp, out var fixture))
            {
                // W5 cold-eye — bulk-stamped fixtures pick a position-
                // hashed face so a 272-cell vault floor isn't wallpaper.
                if (_fixtureVariantTiles.TryGetValue(bp, out var variants))
                    return variants[FixtureVariantIndex(x, y, variants.Length)];
                return fixture;
            }
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
            OilSeep, AcidPond, Rubble, Oven, IceStalactite, VineWall, SandstoneWall,
            TepuiWall
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
                case "Tinker": // Round 3 — the town tinker is villager-kin
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
                case "TepuiWall":     return EnvFixtureKind.TepuiWall;
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
                // Round 2 (S3): blueprint-named role NPCs (shopkeepers
                // + hermits) resolve first — they are not in the
                // ActorSpriteKind enum.
                // ROUND 3 audit 🟡 — the RESKIN GUARD: quest builders
                // repurpose base blueprints by mutating RenderString
                // (BMO is a Villager reskinned to 'b'; dirt gnomes are
                // Snapjaws reskinned to 'g'). If the entity's CURRENT
                // glyph is not the kind's canonical one, it is not
                // that kind anymore — honest ASCII beats a lookalike
                // villager.
                char curGlyph = CurrentGlyphOf(topEntity);
                if (bpName != null
                    && _namedActorTiles.TryGetValue(bpName, out var namedActor)
                    && namedActor != null
                    && _namedActorGlyphs.TryGetValue(bpName, out var canonGlyph)
                    && curGlyph == canonGlyph)
                {
                    authoredColor = true;
                    return namedActor;
                }
                var actorKind = ResolveActorKind(bpName);
                if (actorKind != ActorSpriteKind.None
                    && curGlyph != KindCanonicalGlyph(actorKind))
                    actorKind = ActorSpriteKind.None;
                switch (actorKind)
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

                // PASS 15 G — ground-material tier: macro-field ground
                // and shoreline water resolve by blueprint BEFORE the
                // legacy fixture singles. This is also where the five
                // strata floors stop collapsing into one atlas and
                // SlateFloor stops rendering as bones.
                var ground = ResolveGroundMaterial(bpName);
                if (ground == GroundMaterial.Water)
                {
                    var shoreline = PickShorelineTile(zone, x, y);
                    if (shoreline != null) return shoreline;
                }
                else if (ground != GroundMaterial.None
                    && _groundMacroTiles.TryGetValue(ground, out var macro)
                    && macro.Length == 16)
                {
                    var mt = macro[MacroIndex(x, y)];
                    if (mt != null) return mt;
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
                    case EnvFixtureKind.VineWall:
                        // Pass 15 G: themed walls use their top-face
                        // variant sets when the art exists.
                        if (_vineWallTopTiles.Length == 4)
                            return _vineWallTopTiles[WallVariantIndex(zone, x, y)];
                        if (_vineWallTile != null) return _vineWallTile;
                        break;
                    case EnvFixtureKind.SandstoneWall:
                        if (_sandstoneWallTopTiles.Length == 4)
                            return _sandstoneWallTopTiles[WallVariantIndex(zone, x, y)];
                        break;
                    case EnvFixtureKind.TepuiWall:
                        if (_tepuiWallTopTiles.Length == 4)
                            return _tepuiWallTopTiles[WallVariantIndex(zone, x, y)];
                        if (_sandstoneWallTile != null) return _sandstoneWallTile;
                        break;
                }

                // ROUND 5 — item-body tier: family bodies tinted by the
                // glyph's color at claim time (authoredColor stays
                // FALSE — the color copy IS the identity carrier).
                // Also covers ore veins ('*'), whose guard previously
                // forced honest ASCII — a tinted rock-face beats a
                // letter for a mineable node.
                string itemBody = ResolveItemBody(bpName);
                if (itemBody != null && _itemBodyTiles.TryGetValue(itemBody, out var bodyTile))
                    return bodyTile;
            }

            // Walls — Pass 15 G: the top-face set (lighter top band +
            // ink seam over a flat front) makes walls read as solid
            // blocks; the old 4-slot atlas remains the fallback.
            for (int i = 0; i < WallGlyphs.Length; i++)
            {
                if (WallGlyphs[i] == glyph)
                {
                    int variant = WallVariantIndex(zone, x, y);
                    if (_wallTopTiles.Length == 4) return _wallTopTiles[variant];
                    if (_wallTiles == null || _wallTiles.Length == 0) return null;
                    return _wallTiles[variant % _wallTiles.Length];
                }
            }
            // Floors — Pass 15 G: generic '.' with no ground-material
            // blueprint uses the generic stone macro field; the old
            // hash-picked atlas is the fallback. Round 3: authoredColor
            // — the macro carries the material's palette; copying the
            // glyph hue tinted the plaza red around the campfire (the
            // markers paint a warm '.') while lighting/fog still
            // arrive via the gray max-channel.
            for (int i = 0; i < FloorGlyphs.Length; i++)
            {
                if (FloorGlyphs[i] == glyph)
                {
                    // ROUND 3 audit 🟡 — Well/Oven/Lantern GroundMarkers
                    // carry a STAGE-COLORED '.' (ash-gray fouled → warm
                    // gold repaired: quest feedback). Claiming them as
                    // gray macro stone erased the signal — they keep
                    // their honest colored dot. The campfire's marker
                    // is static decor and may claim.
                    string floorBp = topEntity?.BlueprintName;
                    if (floorBp != null
                        && floorBp.EndsWith("GroundMarker", System.StringComparison.Ordinal)
                        && floorBp != "CampfireGroundMarker")
                        return null;
                    if (_groundMacroTiles.TryGetValue(GroundMaterial.Floor, out var stoneMacro)
                        && stoneMacro.Length == 16)
                    {
                        var mt = stoneMacro[MacroIndex(x, y)];
                        if (mt != null) { authoredColor = true; return mt; }
                    }
                    if (_floorTiles == null || _floorTiles.Length == 0) return null;
                    int variant = FloorVariantIndex(x, y);
                    authoredColor = true;
                    return _floorTiles[variant % _floorTiles.Length];
                }
            }
            // Water — Pass 15 G: shoreline-aware; plain water sprite as
            // fallback. Round 2 (S2): '~' is also the Viper, sludges
            // and gas clouds — only a real water blueprint may claim
            // the water family. A snake rendered as a pond is the
            // worst possible lie. Round 3: authoredColor for the same
            // reason as floors.
            for (int i = 0; i < WaterGlyphs.Length; i++)
            {
                if (WaterGlyphs[i] == glyph)
                {
                    if (ResolveGroundMaterial(topEntity?.BlueprintName) != GroundMaterial.Water)
                        return null;
                    authoredColor = true;
                    var shoreline = PickShorelineTile(zone, x, y);
                    if (shoreline != null) return shoreline;
                    return _waterTile;
                }
            }
            // Doors — Round 2 (S2): '+' is also 21 grimoires and the
            // graveyard marker; only actual doors claim the door art.
            if (glyph == '+') return GlyphClaimAllowed('+', topEntity?.BlueprintName) ? _doorClosedTile : null;
            if (glyph == '\'') return GlyphClaimAllowed('\'', topEntity?.BlueprintName) ? _doorOpenTile : null;

            // Pass 8 — direct glyph→sprite map. Round 2 (S2): every
            // ambiguous glyph now verifies the top entity actually IS
            // the thing the sprite depicts (GlyphClaimAllowed). A
            // mismatch keeps the honest CP437 glyph — an honest letter
            // beats a false sprite: SpikeTrap≠stalagmite,
            // SleepingTroll≠tree, ore veins≠campfire (which even
            // spawned fire lights via the tile-name-keyed light hook),
            // DesertBandit≠chair, seeds≠bones, rations≠mushroom.
            if (!GlyphClaimAllowed(glyph, topEntity?.BlueprintName)) return null;
            switch (glyph)
            {
                case '^':  return _stalagmiteTile;
                case 'o':  return _boulderTile;    // rock, compass stones
                case '|':  return _stalactiteTile;
                case ';':  return _bushTile;
                case 't':  return _cactusTile;
                case 'T':  return _treeTile;
                case '*':  return _campfireTile;
                case '_':  return _shrineTile;
                case '>':  return _stairsDownTile;
                case '<':  return _stairsUpTile;
                case ',':  return _bonesTile;
                case '0':  return _barrelTile;
                case '%':  return _mushroomTile;
                case '$':  return _goldPileTile;
                case 'h':  return _chairTile;
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
                // Round 5 — glyph-family fallbacks: any other ground
                // armor becomes the tinted cuirass; any other '!' the
                // tinted vial. Color copy keeps each item's identity.
                if (glyph == '[' && _itemBodyTiles.TryGetValue("item_armor", out var armorTile))
                    return armorTile;
                if (glyph == '!' && _itemBodyTiles.TryGetValue("item_vial", out var vialTile))
                    return vialTile;
            }
            return null;
        }

        /// <summary>
        /// PASS 15 round 2 (S2) — the false-identity guard table. For
        /// each glyph the Pass 8 map claims, answers: is this blueprint
        /// actually the thing the sprite depicts? Built from a census
        /// of every blueprint in Objects.json that paints the glyph
        /// (inheritance-resolved). Unlisted glyphs return true — the
        /// guard only exists where a collision exists. Public: pinned
        /// by resolver tests.
        /// </summary>
        public static bool GlyphClaimAllowed(char glyph, string blueprintName)
        {
            if (string.IsNullOrEmpty(blueprintName)) return false;
            switch (glyph)
            {
                // '^' — SpikeTrap/FireTrap/BearTrap/PressurePlate all
                // paint it; a trap disguised as scenery is a lethal lie.
                case '^': return blueprintName == "Stalagmite";
                // 'T' — Warhammer, DissolutionMaul, SleepingTroll.
                case 'T': return blueprintName == "Tree";
                // 'h' — DesertBandit.
                case 'h': return blueprintName == "Chair";
                // '*' — runes, FireClay, capacitor, three ORE VEINS and
                // their gems. Campfire-only also stops the tile-name-
                // keyed LightSourceSpriteHook spawning fire lights on
                // quartz veins.
                case '*': return blueprintName == "Campfire";
                // '+' / open-door — 21 grimoires + the graveyard marker.
                case '+':
                case '\'': return blueprintName.Contains("Door");
                // ',' — seeds, ash piles (Rubble/SlateFloor/BrokenColumn
                // resolve in earlier tiers).
                case ',': return blueprintName == "Bone" || blueprintName == "Bones";
                // '%' — corpses (entity pre-pass) + ~20 foods/reagents.
                case '%': return blueprintName == "Mushroom";
                // '|' — LoanerSpear + Reeds; no plain Stalactite
                // blueprint exists today, so this is future-proofing.
                case '|': return blueprintName == "Stalactite";
                // 'o' — compass stones ARE stones; the color-copy tint
                // keeps each one's directional glyph color.
                case 'o': return blueprintName == "Rock"
                    || blueprintName.StartsWith("CompassStone", System.StringComparison.Ordinal);
                case ';': return blueprintName == "Bush";
                case 't': return blueprintName == "Cactus";
                case '_': return blueprintName == "Shrine";
                case '0': return blueprintName == "WoodenBarrel";
                case '$': return blueprintName == "GoldCoin";
                // '/' — 26 painters, all but four genuine blades; deny
                // the known non-weapons. Deny-list polarity is a
                // deliberate tradeoff: a future non-weapon '/' painter
                // needs a row here, but a future WEAPON works with no
                // edit (weapons outnumber exceptions ~6:1).
                case '/': return blueprintName != "Torch" && blueprintName != "IronKey"
                    && blueprintName != "OldWorldPipe" && blueprintName != "TemporalShard";
                default: return true;
            }
        }

        /// <summary>
        /// ROUND 5 — item BODY families: a few near-gray sprites cover
        /// dozens of blueprints because the claim copies the glyph's
        /// COLOR (FireTonic's red '!' → red vial; GlowQuartz's cyan
        /// '*' → cyan gem). Public: pinned by tests.
        /// </summary>
        public static string ResolveItemBody(string bp)
        {
            if (string.IsNullOrEmpty(bp)) return null;
            if (bp.EndsWith("Tonic", System.StringComparison.Ordinal)) return "item_vial";
            if (bp.EndsWith("GasGrenade", System.StringComparison.Ordinal)) return "item_grenade";
            if (bp.Contains("Grimoire")) return "item_book";
            if (bp.EndsWith("Vein", System.StringComparison.Ordinal)) return "item_vein";
            if (bp.EndsWith("Seed", System.StringComparison.Ordinal)) return "item_seed";
            switch (bp)
            {
                case "PaleSalt":
                case "ChoirIron":
                case "GlowQuartz":
                case "FireClay":        return "item_gem";
                case "Torch":           return "item_torch";
                case "IronKey":         return "item_key";
                case "Bone":            return "item_bone";
                case "DriedMeat":
                case "RawMeat":
                case "CookedMeat":      return "item_meat";
                case "Starapple":
                case "RoastedStarapple":
                case "EmberFruit":
                case "CandyCarrot":
                case "CandyHeartRoot":  return "item_fruit";
                default:                return null;
            }
        }

        /// <summary>Pass 12 — top visible entity itself (the crop resolver
        /// needs its CropPart, not just the blueprint name).</summary>
        private static Entity TopEntityAt(Zone zone, int x, int y)
        {
            if (zone == null) return null;
            var c = zone.GetCell(x, y);
            return c?.GetTopVisibleObject();
        }

        /// <summary>
        /// PASS 15 round 2 — the remembered-fog counterpart of
        /// GetTopVisibleObject: the first visible layer≤1 entity,
        /// mirroring ZoneRenderer.RenderRememberedCell's predicate
        /// exactly, so the sprite pass never shows an actor or item
        /// that the glyph pass would hide in the fog.
        /// </summary>
        private static Entity TerrainEntityOf(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var rp = cell.Objects[i].GetPart<RenderPart>();
                if (rp != null && rp.Visible && rp.RenderLayer <= 1)
                    return cell.Objects[i];
            }
            return null;
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
        /// PASS 15 G — macro-field slice index. The field was authored
        /// with image row 0 at the TOP; zone row 0 also renders at the
        /// top of the screen, so zone coordinates index the field
        /// directly: adjacent zone cells always show adjacent slices
        /// and the 64×64 source reassembles seamlessly across the map.
        /// </summary>
        public static int MacroIndex(int x, int zoneY)
        {
            int cx = ((x % 4) + 4) % 4;
            int cy = ((zoneY % 4) + 4) % 4;
            return cy * 4 + cx;
        }

        /// <summary>
        /// PASS 15 G — shoreline resolution for a water cell (zone
        /// coords). Land on one side → that edge tile; two adjacent
        /// land sides → outer corner; land only at a diagonal → inner
        /// corner nub; open water → macro slice. Out-of-bounds counts
        /// as water so rivers run cleanly off the map edge.
        /// </summary>
        private Tile PickShorelineTile(Zone zone, int x, int zoneY)
        {
            if (_shoreEdgeTiles.Length != 4 || _shoreOuterTiles.Length != 4
                || _shoreInnerTiles.Length != 4)
                return null;

            bool n = IsLandAt(zone, x, zoneY - 1);
            bool e = IsLandAt(zone, x + 1, zoneY);
            bool s = IsLandAt(zone, x, zoneY + 1);
            bool w = IsLandAt(zone, x - 1, zoneY);

            // Outer corners (two adjacent land sides). Suffix order:
            // ne, se, sw, nw.
            if (n && e) return _shoreOuterTiles[0];
            if (s && e) return _shoreOuterTiles[1];
            if (s && w) return _shoreOuterTiles[2];
            if (n && w) return _shoreOuterTiles[3];
            // Single edges. Suffix order: n, e, s, w.
            if (n) return _shoreEdgeTiles[0];
            if (e) return _shoreEdgeTiles[1];
            if (s) return _shoreEdgeTiles[2];
            if (w) return _shoreEdgeTiles[3];
            // Diagonal-only land → inner corner nub.
            if (IsLandAt(zone, x + 1, zoneY - 1)) return _shoreInnerTiles[0];
            if (IsLandAt(zone, x + 1, zoneY + 1)) return _shoreInnerTiles[1];
            if (IsLandAt(zone, x - 1, zoneY + 1)) return _shoreInnerTiles[2];
            if (IsLandAt(zone, x - 1, zoneY - 1)) return _shoreInnerTiles[3];
            // Open water.
            if (_waterMacroTiles.Length == 16)
            {
                var mt = _waterMacroTiles[MacroIndex(x, zoneY)];
                if (mt != null) return mt;
            }
            return null;
        }

        private static bool IsLandAt(Zone zone, int x, int zoneY)
        {
            if (zone == null) return false;
            var c = zone.GetCell(x, zoneY);
            if (c == null) return false; // off-map continues as water
            // ROUND 3 audit 🟡 fix — classify by the cell's TERRAIN,
            // not its top entity. Keyed off GetTopVisibleObject, a
            // viper swimming the river flipped its cell to "land" and
            // dragged a moving cluster of shoreline scallops along —
            // through fog, that tracked an unseen enemy's position.
            for (int i = 0; i < c.Objects.Count; i++)
            {
                var o = c.Objects[i];
                if (ResolveGroundMaterial(o.BlueprintName) == GroundMaterial.Water
                    || o.GetPart<LiquidPoolPart>() != null)
                    return false;
            }
            return true; // no water object (incl. bare cell) = land
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
        /// Deterministic-hash fixture variant (W5 cold-eye). Same
        /// (x, y) → same face across frames and reloads; different
        /// mixing constants from <see cref="FloorVariantIndex"/> so
        /// the two patterns don't spatially correlate.
        /// </summary>
        public static int FixtureVariantIndex(int x, int y, int count)
        {
            if (count <= 1) return 0;
            int h = x * 83492791 ^ y * 50331653;
            return (h & 0x7fffffff) % count;
        }

        /// <summary>
        /// Extract the CP437 glyph from a Tile asset. Same convention
        /// as AnimatedEnvironmentRenderer.
        /// </summary>
        // ROUND 4 perf — tile.name + Substring allocated per ASCII cell
        // per rescan (audit: ~0.3-0.6MB/s of garbage in wall-heavy
        // zones). Tile instances are a small fixed set (CP437 generator
        // caches them): parse each ONCE.
        private static readonly Dictionary<TileBase, char> s_glyphCache =
            new Dictionary<TileBase, char>(512);

        private static char ExtractGlyph(TileBase tile)
        {
            if (tile == null) return '\0';
            if (s_glyphCache.TryGetValue(tile, out char cached)) return cached;

            char result = '\0';
            string n = tile.name;
            const string PREFIX = "CP437_";
            if (!string.IsNullOrEmpty(n)
                && n.Length == PREFIX.Length + 2 && n.StartsWith(PREFIX))
            {
                if (int.TryParse(n.Substring(PREFIX.Length),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out int code)
                    && code >= 0 && code < 256)
                {
                    result = (char)code;
                }
            }
            s_glyphCache[tile] = result;
            return result;
        }
    }
}

