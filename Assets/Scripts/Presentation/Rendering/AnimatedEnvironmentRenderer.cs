using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pass 5: animated environment overlay. Creates three sibling
    /// tilemaps (Water, Grass, Fire) each using the
    /// <c>CavesOfOoo/AnimatedEnvironment</c> shader with a
    /// per-effect-tuned material. After the main
    /// <see cref="ZoneRenderer"/> paints each frame, the
    /// <see cref="PostRender"/> pass scans cells looking for
    /// animated-eligible glyphs (water density chars, grass `,`,
    /// fire `*`), paints those onto the appropriate overlay
    /// tilemap, and CLEARS the corresponding cell on the main
    /// tilemap so there's no z-fighting double-render.
    ///
    /// <para>The animated tiles use the same CP437 glyph sprites
    /// as the main render — no new sprite art required for v1.
    /// The motion comes entirely from the shader: vertex sway
    /// for grass, UV scroll for water, brightness flicker for
    /// fire. See <c>Docs/GRAPHICS-PASS5.md</c> §5A.</para>
    ///
    /// <para><b>Architecture:</b> sits alongside ZoneRenderer
    /// rather than modifying it. ZoneRenderer paints normally;
    /// this component runs a post-render cell scan that re-routes
    /// specific glyphs to the overlay layers. ZoneRenderer hooks
    /// into <see cref="PostRender"/> at the end of its redraw
    /// cycle — single line addition, no hot-path change.</para>
    /// </summary>
    public class AnimatedEnvironmentRenderer : MonoBehaviour
    {
        // Sorting order for the three overlay tilemaps. Sits BETWEEN
        // the main tilemap (0) and the FX tilemap. ZoneRenderer's
        // FX renderer is at order 2; we bump it to 3 in init so
        // these can fit at order 2 without conflict.
        private const int OverlaySortingOrder = 2;

        // Glyphs that map to each material. The main tilemap's
        // foreground glyph determines which overlay (if any)
        // claims the cell.
        private static readonly char[] WaterGlyphs = { '~', '=', '-' };
        private static readonly char[] GrassGlyphs = { ',', ';' };
        private static readonly char[] FireGlyphs  = { '*' };

        private Tilemap _waterTilemap;
        private Tilemap _grassTilemap;
        private Tilemap _fireTilemap;

        private Material _waterMaterial;
        private Material _grassMaterial;
        private Material _fireMaterial;

        // Reusable scratch list to avoid per-frame allocation.
        private readonly List<Vector3Int> _claimedThisFrame = new List<Vector3Int>(128);

        // Source-of-truth tilemap (the main ZoneTilemap). Provided
        // on Init. We CLEAR claimed cells on this so the overlay
        // is the only thing that renders for those cells.
        private Tilemap _mainTilemap;

        // Cache of the main tilemap's glyph tiles per cell so we
        // can paint the same tile onto the overlay. ZoneRenderer's
        // _tilemap stores the actual tile asset; we read it back
        // via Tilemap.GetTile().

        public bool IsInitialized { get; private set; }

        public void Init(Transform gridParent, Tilemap mainTilemap, TilemapRenderer fxRenderer)
        {
            _mainTilemap = mainTilemap;

            // Ensure FX renders ABOVE our overlays.
            if (fxRenderer != null)
                fxRenderer.sortingOrder = OverlaySortingOrder + 1;

            _waterMaterial = LoadMaterial("Assets/Materials/AnimatedEnvironment_Water.mat");
            _grassMaterial = LoadMaterial("Assets/Materials/AnimatedEnvironment_Grass.mat");
            _fireMaterial  = LoadMaterial("Assets/Materials/AnimatedEnvironment_Fire.mat");

            _waterTilemap = MakeOverlayTilemap(gridParent, "AnimatedWaterTilemap", _waterMaterial);
            _grassTilemap = MakeOverlayTilemap(gridParent, "AnimatedGrassTilemap", _grassMaterial);
            _fireTilemap  = MakeOverlayTilemap(gridParent, "AnimatedFireTilemap",  _fireMaterial);

            IsInitialized = true;
        }

        private static Material LoadMaterial(string assetPath)
        {
#if UNITY_EDITOR
            var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
                Debug.LogWarning($"[AnimatedEnv] Material not found at {assetPath}");
            return mat;
#else
            // Build path: load via Resources or pre-bundled reference.
            // For now, the materials are editor-only (build path is
            // a Pass 6 concern).
            return null;
#endif
        }

        private static Tilemap MakeOverlayTilemap(Transform gridParent, string name, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(gridParent, false);
            var tm = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = OverlaySortingOrder;
            if (material != null)
                renderer.sharedMaterial = material;
            return tm;
        }

        /// <summary>
        /// Hook called by ZoneRenderer at the end of its redraw cycle.
        /// Scans cells, claims animated glyphs, repaints them to the
        /// appropriate overlay tilemap, clears the main tilemap cell
        /// so the overlay is the sole render for that cell.
        /// </summary>
        /// <summary>
        /// Wipe the three animated overlays.
        ///
        /// <para>ZoneRenderer calls this on its <c>Paused</c> transition,
        /// alongside the bg/fx clears and the sprite-claim release, so a
        /// fullscreen UI opens over a clean slate.</para>
        ///
        /// <para><b>Why this was needed.</b> These overlays sort at
        /// order 2; the fullscreen UIs paint the main tilemap at order 0.
        /// Without this clear, 264 grass tiles and a fire tile kept
        /// rendering ON TOP of the inventory screen — world terrain
        /// glyphs scattered across the panels. Measured live via a
        /// tilemap census while the inventory was open. The sibling
        /// overlays (EnvironmentSprite, GlyphGhost) already had their
        /// own release for exactly this reason; these three were simply
        /// missed.</para>
        /// </summary>
        public void ClearAll()
        {
            _waterTilemap?.ClearAllTiles();
            _grassTilemap?.ClearAllTiles();
            _fireTilemap?.ClearAllTiles();
        }

        public void PostRender(Zone zone, int width, int height)
        {
            if (!IsInitialized || zone == null || _mainTilemap == null) return;

            // Clear last frame's claims (cheap).
            for (int i = 0; i < _claimedThisFrame.Count; i++)
            {
                var p = _claimedThisFrame[i];
                _waterTilemap.SetTile(p, null);
                _grassTilemap.SetTile(p, null);
                _fireTilemap.SetTile(p, null);
            }
            _claimedThisFrame.Clear();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector3Int(x, y, 0);
                    var existingTile = _mainTilemap.GetTile(pos);
                    if (existingTile == null) continue;

                    // The CP437 glyph is encoded in the tile asset's
                    // name (e.g., "Glyph_126" for '~'). Extract.
                    char glyph = ExtractGlyph(existingTile);
                    if (glyph == '\0') continue;

                    Tilemap target = ChooseOverlay(glyph);
                    if (target == null) continue;

                    // The glyph is a fast pre-filter, NOT the decision.
                    // Ask what the thing actually IS before animating it.
                    if (!IsAnimatableEnvironment(zone, x, y, target)) continue;

                    // Re-route: paint the same tile + color onto the
                    // overlay, clear from the main.
                    target.SetTile(pos, existingTile);
                    target.SetColor(pos, _mainTilemap.GetColor(pos));
                    target.SetTransformMatrix(pos, _mainTilemap.GetTransformMatrix(pos));
                    _mainTilemap.SetTile(pos, null);
                    _claimedThisFrame.Add(pos);
                }
            }
        }


        /// <summary>
        /// Whether the entity on this cell is genuinely animated
        /// environment, rather than merely something that happens to draw
        /// the same letter.
        ///
        /// <para><b>This gate is the fix for a real, shipped bug.</b> The
        /// overlay used to claim cells by CP437 glyph ALONE: <c>~ = -</c>
        /// meant water, <c>,</c> and <c>;</c> meant grass, <c>*</c> meant
        /// fire. Roughly fifty blueprints collide with those characters —
        /// a packed road and a copper pipe draw <c>=</c>, a peat bog draws
        /// <c>~</c>, charm-flowers draw <c>*</c>, and chests, beds, ore
        /// veins and even a Viper draw one of them too. All were being
        /// re-parented onto a scrolling overlay.</para>
        ///
        /// <para>The water material scrolls its UVs with no per-cell phase
        /// offset (<c>AnimatedEnvironment.shader</c>, the <c>_ENABLE_SCROLL</c>
        /// path — unlike sway and flicker, which do offset per cell), so
        /// every claimed cell scrolled in lockstep. A run of roads therefore
        /// produced a coherent band of glyph fragments marching across the
        /// terrain: the "wave of ASCII characters" a player reported.</para>
        ///
        /// <para>Fixing the shader phase would only have made the wave
        /// incoherent. The road should not be animating at all.</para>
        /// </summary>
        private bool IsAnimatableEnvironment(Zone zone, int x, int y, Tilemap target)
        {
            if (zone == null) return false;
            var cell = zone.GetCell(x, y);
            var top = cell?.GetTopVisibleObject();
            if (top == null) return false;

            // Never animate anything alive. A Viper renders '~'.
            if (top.HasTag("Creature")) return false;

            // Water animation requires the cell to actually render as a
            // WATER SPRITE, not merely to be a liquid.
            //
            // LiquidPoolPart is not sufficient: BrinePool, PeatBog,
            // AcidPool and TarSeep all carry it while rendering as a bare
            // '~' glyph, and scrolling a LETTER is the bug this whole gate
            // exists to stop. Only WaterPuddle resolves a water ground
            // material and has a tile to scroll. The rest stay static until
            // they have sprites of their own.
            if (target == _waterTilemap)
                return EnvironmentSpriteRenderer.ResolveGroundMaterial(top.BlueprintName)
                       == EnvironmentSpriteRenderer.GroundMaterial.Water;

            if (target == _grassTilemap)
                return EnvironmentSpriteRenderer.ResolveGroundMaterial(top.BlueprintName)
                       == EnvironmentSpriteRenderer.GroundMaterial.Grass;

            // Fire is an explicit short list. Fourteen blueprints draw
            // '*' and only these are actually alight — the rest are ore
            // veins, salt, quartz and charm-flowers, all of which were
            // flickering like flame.
            if (target == _fireTilemap)
                return top.BlueprintName == "Campfire"
                    || top.BlueprintName == "RuneOfFlame"
                    || top.HasTag("Fire");

            return false;
        }

        /// <summary>
        /// Extract the CP437 glyph from a Tile asset whose name
        /// follows the project's
        /// <see cref="CavesOfOoo.Rendering.CP437TilesetGenerator"/>
        /// convention: <c>CP437_<i>HH</i></c> where HH is the
        /// hex byte (0-FF) of the CP437 glyph. e.g. <c>CP437_7E</c>
        /// for '~'. Returns '\0' for tiles that don't match
        /// (e.g., text glyphs at "Text_<HH>" don't trigger
        /// environment animation).
        /// </summary>
        private static char ExtractGlyph(TileBase tile)
        {
            if (tile == null) return '\0';
            string n = tile.name;
            if (string.IsNullOrEmpty(n)) return '\0';

            // Match "CP437_HH" where HH is a 2-char hex code.
            const string PREFIX = "CP437_";
            if (n.Length == PREFIX.Length + 2 && n.StartsWith(PREFIX))
            {
                string hex = n.Substring(PREFIX.Length);
                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out int code)
                    && code >= 0 && code < 256)
                {
                    return (char)code;
                }
            }
            return '\0';
        }

        private Tilemap ChooseOverlay(char glyph)
        {
            for (int i = 0; i < WaterGlyphs.Length; i++)
                if (WaterGlyphs[i] == glyph) return _waterTilemap;
            for (int i = 0; i < GrassGlyphs.Length; i++)
                if (GrassGlyphs[i] == glyph) return _grassTilemap;
            for (int i = 0; i < FireGlyphs.Length; i++)
                if (FireGlyphs[i] == glyph) return _fireTilemap;
            return null;
        }

        // ── Test seams ───────────────────────────────────────────────────

        public bool TestOnly_IsWaterGlyph(char g)
        {
            for (int i = 0; i < WaterGlyphs.Length; i++) if (WaterGlyphs[i] == g) return true;
            return false;
        }
        public bool TestOnly_IsGrassGlyph(char g)
        {
            for (int i = 0; i < GrassGlyphs.Length; i++) if (GrassGlyphs[i] == g) return true;
            return false;
        }
        public bool TestOnly_IsFireGlyph(char g)
        {
            for (int i = 0; i < FireGlyphs.Length; i++) if (FireGlyphs[i] == g) return true;
            return false;
        }
    }
}
