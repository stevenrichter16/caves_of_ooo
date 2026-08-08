using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// GRAPHICS PASS 15 — the harness the sprite system never had. The
    /// 44 existing resolver tests are pure-static pins; none exercised
    /// Init/LoadSprites/PostRender or the coordinate mapping, which is
    /// exactly where all three audit defects lived (editor-only
    /// loading, the vertical mirror, dirty-path blanking). These tests
    /// drive the REAL component end-to-end: real Resources sprites,
    /// real tilemaps, a real zone.
    /// </summary>
    [TestFixture]
    public class EnvironmentSpriteRendererHarnessTests
    {
        private GameObject _gridGo;
        private Tilemap _mainTilemap;
        private EnvironmentSpriteRenderer _renderer;
        private Tile _floorGlyphTile;

        [SetUp]
        public void Setup()
        {
            _gridGo = new GameObject("TestGrid");
            _gridGo.AddComponent<Grid>();
            var mainGo = new GameObject("MainTilemap");
            mainGo.transform.SetParent(_gridGo.transform, false);
            _mainTilemap = mainGo.AddComponent<Tilemap>();
            mainGo.AddComponent<TilemapRenderer>();

            _renderer = _gridGo.AddComponent<EnvironmentSpriteRenderer>();
            _renderer.Init(_gridGo.transform, _mainTilemap);

            // A '.' glyph tile named the way ZoneRenderer names CP437
            // tiles (hex code of '.' = 2E).
            _floorGlyphTile = ScriptableObject.CreateInstance<Tile>();
            _floorGlyphTile.name = "CP437_2E";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gridGo);
            Object.DestroyImmediate(_floorGlyphTile);
        }

        /// <summary>Paint a zone cell's glyph the way ZoneRenderer does:
        /// zone (x, zy) lands at tile row Height-1-zy.</summary>
        private Vector3Int PaintFloorGlyph(int x, int zoneY, Color color)
        {
            var tilePos = new Vector3Int(x, Zone.Height - 1 - zoneY, 0);
            _mainTilemap.SetTile(tilePos, _floorGlyphTile);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, color);
            return tilePos;
        }

        private static Zone ZoneWithGrassAt(int x, int y)
        {
            var zone = new Zone("T");
            var grass = new Entity { ID = "g", BlueprintName = "Grass" };
            grass.AddPart(new RenderPart { DisplayName = "grass", RenderString = ".", RenderLayer = 0 });
            zone.AddEntity(grass, x, y);
            return zone;
        }

        // ── R1: sprites really load from Resources ───────────────

        [Test]
        public void Init_LoadsSpritesFromResources()
        {
            // The old loader was #if UNITY_EDITOR + AssetDatabase; this
            // test would have passed there too — but it now runs the
            // same Resources path a player build runs, and the painting
            // tests below prove the loaded sprites actually resolve.
            Assert.IsTrue(_renderer.IsInitialized);
        }

        // ── R2: the vertical mirror ──────────────────────────────

        [Test]
        public void PostRender_ResolvesBlueprintFromTheUNFLIPPEDZoneRow()
        {
            // Grass entity at ZONE row 3. ZoneRenderer paints its glyph
            // at TILE row 21 (25-1-3). The sprite must appear at tile
            // row 21 — resolved from zone row 3, not zone row 21.
            var zone = ZoneWithGrassAt(5, 3);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var overlayTile = FindOverlay().GetTile(tilePos);
            Assert.IsNotNull(overlayTile,
                "the grass cell's tile row must be claimed by the sprite pass");
            Assert.AreEqual("Grass", overlayTile.name,
                "and resolved as GRASS from the un-flipped zone row — " +
                "pre-fix this resolved the MIRRORED row and painted a generic floor");
        }

        // ── R3: dirty-path release restores, never blanks ────────

        [Test]
        public void PostRender_SecondPassWithoutRepaint_KeepsTheEnvironment()
        {
            // The Pass 13 blanking bug: PostRender #1 nulls the claimed
            // main-tilemap cells; PostRender #2 (dirty path — main NOT
            // repainted) released the overlay and found nothing beneath
            // → the world blanked out. Release must restore the glyph
            // so the rescan can re-claim it.
            var zone = ZoneWithGrassAt(5, 3);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);
            _renderer.PostRender(zone, Zone.Width, Zone.Height); // dirty-path shape

            Assert.AreEqual("Grass", FindOverlay().GetTile(tilePos)?.name,
                "the environment survives a rescan without a main repaint");
        }

        [Test]
        public void PostRender_ToggleOff_RestoresTheGlyphImmediately()
        {
            var zone = ZoneWithGrassAt(5, 3);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);
            Assert.IsNull(_mainTilemap.GetTile(tilePos), "claimed: glyph displaced");

            _renderer.RenderingEnabled = false;
            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.IsNotNull(_mainTilemap.GetTile(tilePos),
                "toggle-off restores the ASCII view at once (old code left holes until a full redraw)");
            Assert.IsNull(FindOverlay().GetTile(tilePos), "overlay released");
        }

        [Test]
        public void NotifyMainTilemapCleared_DropsClaimsWithoutRestoring()
        {
            // Full-repaint path: ClearAllTiles has blanked the main map;
            // restoring remembered glyphs would resurrect STALE tiles
            // over the fresh repaint.
            var zone = ZoneWithGrassAt(5, 3);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);
            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            _mainTilemap.ClearAllTiles();
            _renderer.NotifyMainTilemapCleared();

            Assert.IsNull(_mainTilemap.GetTile(tilePos),
                "no stale resurrection onto a cleared map");
            Assert.IsNull(FindOverlay().GetTile(tilePos), "overlay dropped");
        }

        private Tilemap FindOverlay()
        {
            foreach (Transform child in _gridGo.transform)
                if (child.name == "EnvironmentSpriteTilemap")
                    return child.GetComponent<Tilemap>();
            Assert.Fail("overlay tilemap not created");
            return null;
        }
    }
}
