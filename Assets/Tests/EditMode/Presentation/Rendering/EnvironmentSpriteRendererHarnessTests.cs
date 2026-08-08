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
        private Tilemap _bgTilemap;
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
            var bgGo = new GameObject("BgTilemap");
            bgGo.transform.SetParent(_gridGo.transform, false);
            _bgTilemap = bgGo.AddComponent<Tilemap>();
            bgGo.AddComponent<TilemapRenderer>();

            _renderer = _gridGo.AddComponent<EnvironmentSpriteRenderer>();
            _renderer.Init(_gridGo.transform, _mainTilemap, _bgTilemap);

            // A '.' glyph tile named the way ZoneRenderer names CP437
            // tiles (hex code of '.' = 2E).
            _floorGlyphTile = ScriptableObject.CreateInstance<Tile>();
            _floorGlyphTile.name = "CP437_2E";
        }

        /// <summary>Round 2 — cells default to unexplored, and the fog
        /// gate (correctly) claims nothing there. Tests of the VISIBLE
        /// path reveal the whole zone first, the way FOV does.</summary>
        private static void Reveal(Zone zone)
        {
            for (int x = 0; x < Zone.Width; x++)
                for (int y = 0; y < Zone.Height; y++)
                {
                    var c = zone.GetCell(x, y);
                    if (c != null) { c.Explored = true; c.IsVisible = true; }
                }
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
            Reveal(zone);
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
            Reveal(zone);
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
            Reveal(zone);
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
            Reveal(zone);
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
            Reveal(zone);

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

            Reveal(zone);
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
            Reveal(zone);
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

        // ── ROUND 2: fog of war reaches the sprite pass ──────────

        [Test]
        public void Fog_UnexploredCell_IsNeverClaimed()
        {
            // Round 1's claims ignored Explored entirely — a chest or
            // river visibly rendered through unexplored blackness.
            var zone = ZoneWithGrassAt(5, 3);   // NO Reveal — fog stays
            var tilePos = PaintFloorGlyph(5, 3, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "an unexplored cell shows NOTHING — no sprite claim through the fog");
            Assert.IsNotNull(_mainTilemap.GetTile(tilePos),
                "and the main glyph is not displaced either");
        }

        [Test]
        public void Fog_RememberedCell_ClaimsTerrainWithTheDimTint()
        {
            // Explored-but-not-visible: terrain stays, dimmed — the
            // sprite equivalent of ZoneRenderer's dark-gray remembered
            // glyphs. Round 1 tinted everything Color.white, so sprite
            // terrain glowed at full brightness inside the fog.
            var zone = ZoneWithGrassAt(5, 3);
            Reveal(zone);
            zone.GetCell(5, 3).IsVisible = false;
            var tilePos = PaintFloorGlyph(5, 3, Color.gray);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var overlay = FindOverlay();
            StringAssert.StartsWith("grass", overlay.GetTile(tilePos)?.name,
                "remembered terrain still gets its sprite");
            Assert.AreEqual(EnvironmentSpriteRenderer.RememberedTint, overlay.GetColor(tilePos),
                "but dimmed with the remembered tint, not full-bright white");
        }

        [Test]
        public void Fog_VisibleCell_ClaimsFullBright()
        {
            // Counter-check for the pair above: same setup, cell VISIBLE
            // → white tint. Without this, a bug dimming everything would
            // pass the remembered test vacuously.
            var zone = ZoneWithGrassAt(5, 3);
            Reveal(zone);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual(Color.white, FindOverlay().GetColor(tilePos),
                "visible terrain is full-bright (Light2D handles the rest)");
        }

        [Test]
        public void Fog_RememberedCell_HidesActorSprites()
        {
            // RenderRememberedCell hides creatures (layer > 1) in the
            // fog. The sprite pass must mirror that: a snapjaw standing
            // in a remembered cell may NOT render its actor sprite —
            // that would leak live enemy positions through the fog.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 5, 3);
            var snapjaw = new Entity { ID = "sj", BlueprintName = "Snapjaw" };
            snapjaw.AddPart(new RenderPart { DisplayName = "snapjaw", RenderString = "s", RenderLayer = 5 });
            zone.AddEntity(snapjaw, 5, 3);
            Reveal(zone);
            zone.GetCell(5, 3).IsVisible = false;
            var tilePos = PaintFloorGlyph(5, 3, Color.gray);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var name = FindOverlay().GetTile(tilePos)?.name;
            StringAssert.StartsWith("grass", name,
                "the remembered cell resolves its TERRAIN, never the actor standing in it");
        }

        // ── ROUND 2: false-identity guards (S2) ──────────────────

        [Test]
        public void GlyphGuards_TrapsAndImpostorsKeepHonestGlyphs()
        {
            // Census-built allow-lists: each pair is (sprite-worthy
            // blueprint → allowed) + (the collision that made the guard
            // necessary → denied).
            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('^', "Stalagmite"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('^', "SpikeTrap"),
                "a trap disguised as scenery is a lethal lie");
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('^', "PressurePlate"));

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('T', "Tree"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('T', "SleepingTroll"),
                "the troll is not a tree");
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('T', "Warhammer"));

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('*', "Campfire"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('*', "GlowQuartzVein"),
                "ore veins are not campfires (and must not spawn fire lights)");
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('*', "RuneOfFlame"));

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('h', "Chair"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('h', "DesertBandit"),
                "the bandit is not furniture");

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('+', "LockedDoor"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('+', "ConflagrationGrimoire"),
                "a grimoire is not a door");

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed(',', "Bone"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed(',', "CandyCarrotSeed"),
                "seeds are not bones");

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('%', "Mushroom"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('%', "DriedMeat"),
                "rations keep their food glyphs");

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('o', "CompassStoneNorth"),
                "compass stones ARE stones — tinted by their own glyph color");
            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('/', "LongSword"));
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('/', "Torch"),
                "a torch is not a dropped blade");

            Assert.IsTrue(EnvironmentSpriteRenderer.GlyphClaimAllowed('>', "StairsDown"),
                "unambiguous glyphs pass the guard untouched");
            Assert.IsFalse(EnvironmentSpriteRenderer.GlyphClaimAllowed('^', null),
                "no blueprint, no claim — honest ASCII when identity is unknown");
        }

        [Test]
        public void Viper_DoesNotRenderAsWater()
        {
            // The '~' glyph tier used to hand ANY tilde the water
            // family. A snake rendered as a pond is the worst lie the
            // old table told.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Sand", "."), 12, 8);
            var viper = new Entity { ID = "v", BlueprintName = "Viper" };
            viper.AddPart(new RenderPart { DisplayName = "viper", RenderString = "~", RenderLayer = 5 });
            zone.AddEntity(viper, 12, 8);
            Reveal(zone);
            var tildeGlyph = ScriptableObject.CreateInstance<Tile>();
            tildeGlyph.name = "CP437_7E"; // '~'
            var tilePos = new Vector3Int(12, Zone.Height - 1 - 8, 0);
            _mainTilemap.SetTile(tilePos, tildeGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "the viper keeps its honest '~' — no water claim");
            Assert.IsNotNull(_mainTilemap.GetTile(tilePos), "glyph not displaced");
            StringAssert.StartsWith("sand", _bgTilemap.GetTile(tilePos)?.name,
                "but the ground still paints beneath the honest glyph");
            Object.DestroyImmediate(tildeGlyph);
        }

        [Test]
        public void SpikeTrap_DoesNotRenderAsStalagmite()
        {
            var zone = new Zone("T");
            var trap = new Entity { ID = "tr", BlueprintName = "SpikeTrap" };
            trap.AddPart(new RenderPart { DisplayName = "spike trap", RenderString = "^", RenderLayer = 2 });
            zone.AddEntity(trap, 9, 9);
            Reveal(zone);
            var caretGlyph = ScriptableObject.CreateInstance<Tile>();
            caretGlyph.name = "CP437_5E"; // '^'
            var tilePos = new Vector3Int(9, Zone.Height - 1 - 9, 0);
            _mainTilemap.SetTile(tilePos, caretGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "the trap keeps its honest '^' — no scenery disguise");
            Object.DestroyImmediate(caretGlyph);
        }

        // ── ROUND 2: named role NPCs (S3) ────────────────────────

        [Test]
        public void Weaponsmith_ClaimsItsRoleSprite()
        {
            // The 9 role NPCs (5 shopkeepers + 4 hermits) resolve by
            // BLUEPRINT name — no ActorSpriteKind enum extension.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 4, 4);
            var smith = new Entity { ID = "ws", BlueprintName = "Weaponsmith" };
            smith.AddPart(new RenderPart { DisplayName = "weaponsmith", RenderString = "v", RenderLayer = 5 });
            zone.AddEntity(smith, 4, 4);
            Reveal(zone);
            var vGlyph = ScriptableObject.CreateInstance<Tile>();
            vGlyph.name = "CP437_76"; // 'v'
            var tilePos = new Vector3Int(4, Zone.Height - 1 - 4, 0);
            _mainTilemap.SetTile(tilePos, vGlyph);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("Weaponsmith", FindOverlay().GetTile(tilePos)?.name,
                "the shopkeeper renders as its role sprite, not a letter");
            Assert.AreEqual(9, EnvironmentSpriteRenderer.NamedActorSprites.Length,
                "roster pin: 5 shopkeepers + 4 hermits");
        }

        // ── ROUND 2: player ground highlight (S4) ────────────────

        [Test]
        public void Player_GetsTheWarmGroundHighlight()
        {
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 6, 6);
            var player = new Entity { ID = "p", BlueprintName = "Player" };
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@", RenderLayer = 9 });
            zone.AddEntity(player, 6, 6);
            Reveal(zone);
            var atGlyph = ScriptableObject.CreateInstance<Tile>();
            atGlyph.name = "CP437_40"; // '@'
            var tilePos = new Vector3Int(6, Zone.Height - 1 - 6, 0);
            _mainTilemap.SetTile(tilePos, atGlyph);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("Player", FindOverlay().GetTile(tilePos)?.name,
                "the player sprite claims the cell");
            Assert.AreEqual(EnvironmentSpriteRenderer.PlayerHighlightTint,
                _bgTilemap.GetColor(tilePos),
                "and the ground beneath warms to the highlight tint (S4 findability)");
        }
    }
}

