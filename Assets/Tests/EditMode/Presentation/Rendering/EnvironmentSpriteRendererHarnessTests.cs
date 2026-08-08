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
            StringAssert.StartsWith("grass", overlayTile.name,
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

            StringAssert.StartsWith("grass", FindOverlay().GetTile(tilePos)?.name,
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

        // ── PASS 15 G: flowing ground ────────────────────────────

        private static Entity TerrainEntity(string blueprint, string glyph)
        {
            var e = new Entity { ID = blueprint + "-t", BlueprintName = blueprint };
            e.AddPart(new RenderPart { DisplayName = blueprint, RenderString = glyph, RenderLayer = 0 });
            return e;
        }

        [Test]
        public void GrassRegion_AssemblesTheMacroField_NotOneRepeatingTile()
        {
            // A 2×2 grass patch must show FOUR DIFFERENT macro slices in
            // field-adjacent order — that is the whole trick: variation
            // larger than the tile, so the grid disappears.
            var zone = new Zone("T");
            foreach (var (x, y) in new[] { (5, 3), (6, 3), (5, 4), (6, 4) })
            {
                zone.AddEntity(TerrainEntity("Grass", "."), x, y);
                PaintFloorGlyph(x, y, Color.white);
            }

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            string NameAt(int x, int zy) =>
                FindOverlay().GetTile(new Vector3Int(x, Zone.Height - 1 - zy, 0))?.name;

            Assert.AreEqual($"grass_m{EnvironmentSpriteRenderer.MacroIndex(5, 3):D2}".Replace("_m", "_m"),
                NameAt(5, 3));
            var names = new[] { NameAt(5, 3), NameAt(6, 3), NameAt(5, 4), NameAt(6, 4) };
            foreach (var n in names)
                StringAssert.StartsWith("grass_m", n, "every cell resolves the grass macro");
            CollectionAssert.AllItemsAreUnique(names,
                "adjacent cells show DIFFERENT slices of the source field");
        }

        [Test]
        public void WaterWithLandToTheNorth_GetsTheNorthShorelineLip()
        {
            // A realistic riverbank neighborhood: the target water cell
            // surrounded by water on every side EXCEPT grass to the
            // zone-north. (Bare cells deliberately count as land, so an
            // unpopulated test zone would read as a puddle in a field.)
            var zone = new Zone("T");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int cx = 10 + dx, cy = 10 + dy;
                    if (dx == 0 && dy == -1)
                        zone.AddEntity(TerrainEntity("Grass", "."), cx, cy);
                    else
                        zone.AddEntity(TerrainEntity("WaterPuddle", "~"), cx, cy);
                }

            var waterGlyph = ScriptableObject.CreateInstance<Tile>();
            waterGlyph.name = "CP437_7E"; // '~'
            var tilePos = new Vector3Int(10, Zone.Height - 1 - 10, 0);
            _mainTilemap.SetTile(tilePos, waterGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("water_e_n", FindOverlay().GetTile(tilePos)?.name,
                "the shoreline family resolves by neighbor mask — the river gets edges");
            Object.DestroyImmediate(waterGlyph);
        }

        [Test]
        public void SlateFloor_FinallyRendersAsSlate_NotBones()
        {
            // The audit's absurdity champion: SlateFloor paints ',' and
            // the glyph tier claimed it with the BONES sprite.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("SlateFloor", ","), 7, 7);
            var commaGlyph = ScriptableObject.CreateInstance<Tile>();
            commaGlyph.name = "CP437_2C"; // ','
            var tilePos = new Vector3Int(7, Zone.Height - 1 - 7, 0);
            _mainTilemap.SetTile(tilePos, commaGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var name = FindOverlay().GetTile(tilePos)?.name;
            StringAssert.StartsWith("slate_m", name,
                "a floor material renders as FLOOR — the ground tier outranks the glyph map");
            Object.DestroyImmediate(commaGlyph);
        }

        [Test]
        public void GroundMaterialResolver_StrataFinallyDiffer()
        {
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Sandstone,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("SandstoneFloor"));
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Obsidian,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("ObsidianFloor"));
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Slate,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("SlateFloor"));
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Sand,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("SilverSand"),
                "silver sand belongs to the sand family, not the generic floor");
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.None,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("Wall"),
                "counter-check: walls are not ground");
        }
    }
}
