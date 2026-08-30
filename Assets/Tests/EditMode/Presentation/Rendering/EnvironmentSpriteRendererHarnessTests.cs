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

            // Round 5: the viper now has its OWN sprite — the invariant
            // is still "never the water family".
            Assert.AreEqual("Viper", FindOverlay().GetTile(tilePos)?.name,
                "the viper renders as a SNAKE, never as water");
            StringAssert.StartsWith("sand", _bgTilemap.GetTile(tilePos)?.name,
                "and the ground still paints beneath it");
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
            // '@' — the canonical townsfolk glyph (the reskin guard
            // denies the sprite for any OTHER glyph, by design).
            smith.AddPart(new RenderPart { DisplayName = "weaponsmith", RenderString = "@", RenderLayer = 5 });
            zone.AddEntity(smith, 4, 4);
            Reveal(zone);
            var vGlyph = ScriptableObject.CreateInstance<Tile>();
            vGlyph.name = "CP437_40"; // '@'
            var tilePos = new Vector3Int(4, Zone.Height - 1 - 4, 0);
            _mainTilemap.SetTile(tilePos, vGlyph);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("Weaponsmith", FindOverlay().GetTile(tilePos)?.name,
                "the shopkeeper renders as its role sprite, not a letter");
            Assert.AreEqual(14, EnvironmentSpriteRenderer.NamedActorSprites.Length,
                "roster pin: 5 shopkeepers + 4 hermits + Farmer/Undertaker/"
                + "Marceline + the W5.6 catacomb pair (Warden, Plaque-Tender)");
        }

        // ── ROUND 3: town live-sweep fixes ───────────────────────

        [Test]
        public void Campfire_ClaimsItsSprite_OnAnyFlickerFrame()
        {
            // The campfire uses GlyphVariants — the painted glyph is
            // often NOT '*' (the live town showed a red 'z' frame).
            // The blueprint-keyed pre-pass must claim it regardless.
            var zone = new Zone("T");
            var fire = new Entity { ID = "cf", BlueprintName = "Campfire" };
            fire.AddPart(new RenderPart { DisplayName = "campfire", RenderString = "*", RenderLayer = 2 });
            zone.AddEntity(fire, 8, 8);
            Reveal(zone);
            var zGlyph = ScriptableObject.CreateInstance<Tile>();
            zGlyph.name = "CP437_7A"; // 'z' — a flicker frame, not '*'
            var tilePos = new Vector3Int(8, Zone.Height - 1 - 8, 0);
            _mainTilemap.SetTile(tilePos, zGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("Campfire", FindOverlay().GetTile(tilePos)?.name,
                "campfire resolves by BLUEPRINT — flicker frames must not break the sprite " +
                "(or the tile-name-keyed fire light)");
            Object.DestroyImmediate(zGlyph);
        }

        [Test]
        public void FloorMacro_IgnoresTheGlyphHue()
        {
            // The town's CampfireGroundMarkers paint a warm '.' — the
            // color-copy tinted the plaza's stone macro RED (the
            // red-cross finding). Ground families carry their own
            // palette; only the lighting VALUE may come through.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Floor", "."), 9, 4);
            Reveal(zone);
            var tilePos = PaintFloorGlyph(9, 4, new Color(1f, 0.25f, 0.25f, 1f)); // red '.'

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var overlay = FindOverlay();
            StringAssert.StartsWith("floor_m", overlay.GetTile(tilePos)?.name);
            var c = overlay.GetColor(tilePos);
            Assert.AreEqual(c.r, c.g, 0.001f, "no hue leaks into ground tiles");
            Assert.AreEqual(c.g, c.b, 0.001f, "gray = lighting only");
        }

        [Test]
        public void MarketStall_ClaimsEvenWhenAnimatedEnvStrippedTheGlyph()
        {
            // The animated-environment renderer claims '=' glyphs off
            // the main tilemap before this pass runs — the stall
            // blanked back to nothing. Blueprint tiers must run even
            // with no readable glyph.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("MarketStall", "="), 11, 6);
            Reveal(zone);
            // NO main tile painted at the stall's position — stripped.

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var tilePos = new Vector3Int(11, Zone.Height - 1 - 6, 0);
            Assert.AreEqual("MarketStall", FindOverlay().GetTile(tilePos)?.name,
                "fixtures resolve by blueprint even when the glyph is gone");
        }

        [Test]
        public void Tinker_IsVillagerKin()
        {
            Assert.AreEqual(EnvironmentSpriteRenderer.ActorSpriteKind.Villager,
                EnvironmentSpriteRenderer.ResolveActorKind("Tinker"),
                "the town tinker renders as villager-kin, not a bare letter");
        }

        // ── ROUND 3: audit-confirmed fixes ───────────────────────

        [Test]
        public void Release_NeverClobbersAFreshRepaint()
        {
            // AUDIT 🔴 — RenderDirtyCells runs BEFORE PostRender on the
            // incremental path. Pre-fix, the release loop restored last
            // frame's '.' snapshot over the freshly painted monster
            // glyph, then re-claimed the cell as floor: a spriteless
            // monster walking toward a stationary player was INVISIBLE.
            var zone = ZoneWithGrassAt(5, 3);
            Reveal(zone);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);
            _renderer.PostRender(zone, Zone.Width, Zone.Height); // grass claimed, main nulled

            // A spriteless '~' entity steps in (SteamCloud — the round-5
            // bestiary gave the Viper a sprite): the dirty-path repaint
            // puts ITS glyph on the main tilemap (fresher than our
            // snapshot).
            var viper = new Entity { ID = "v", BlueprintName = "SteamCloud" };
            viper.AddPart(new RenderPart { DisplayName = "steam", RenderString = "~", RenderLayer = 5 });
            zone.AddEntity(viper, 5, 3);
            var viperGlyph = ScriptableObject.CreateInstance<Tile>();
            viperGlyph.name = "CP437_7E";
            _mainTilemap.SetTile(tilePos, viperGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("CP437_7E", _mainTilemap.GetTile(tilePos)?.name,
                "the fresh repaint SURVIVES the claim release — the monster is visible");
            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "and no terrain sprite is claimed over the honest viper glyph");
            Object.DestroyImmediate(viperGlyph);
        }

        [Test]
        public void ReleaseAllClaims_RestoresTheWorldForFullscreenUIs()
        {
            // AUDIT 🟡 — the overlay (order 3) kept last frame's sprites
            // above the main tilemap fullscreen UIs paint on (order 0).
            // ZoneRenderer calls this on its Paused transition.
            var zone = ZoneWithGrassAt(5, 3);
            Reveal(zone);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);
            _renderer.PostRender(zone, Zone.Width, Zone.Height);
            Assert.IsNotNull(FindOverlay().GetTile(tilePos), "precondition: claimed");

            _renderer.ReleaseAllClaims();

            Assert.IsNull(FindOverlay().GetTile(tilePos), "overlay cleared for the UI");
            Assert.IsNotNull(_mainTilemap.GetTile(tilePos), "glyph handed back");
        }

        [Test]
        public void Shoreline_IgnoresActorsStandingInWater()
        {
            // AUDIT 🟡 — IsLandAt keyed off the TOP entity: a viper
            // swimming the river flipped its cell to 'land' and dragged
            // shoreline scallops along with it (through fog, that
            // tracked an unseen enemy). Terrain decides, not actors.
            var zone = new Zone("T");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    zone.AddEntity(TerrainEntity("WaterPuddle", "~"), 10 + dx, 10 + dy);
            var swimmer = new Entity { ID = "v", BlueprintName = "Viper" };
            swimmer.AddPart(new RenderPart { DisplayName = "viper", RenderString = "~", RenderLayer = 5 });
            zone.AddEntity(swimmer, 10, 9); // in the NORTH water cell
            Reveal(zone);
            var waterGlyph = ScriptableObject.CreateInstance<Tile>();
            waterGlyph.name = "CP437_7E";
            var tilePos = new Vector3Int(10, Zone.Height - 1 - 10, 0);
            _mainTilemap.SetTile(tilePos, waterGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            StringAssert.StartsWith("water_m", FindOverlay().GetTile(tilePos)?.name,
                "open water stays open water — the swimming viper is not a shore");
            Object.DestroyImmediate(waterGlyph);
        }

        [Test]
        public void ReskinnedQuestNPC_KeepsItsHonestGlyph()
        {
            // AUDIT 🟡 — quest builders reskin base blueprints by
            // mutating RenderString (BMO is a Villager reskinned to
            // 'b'). The sprite tier must notice the glyph is no longer
            // canonical and stand down.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 6, 6);
            var bmo = new Entity { ID = "bmo", BlueprintName = "Villager" };
            bmo.AddPart(new RenderPart { DisplayName = "BMO", RenderString = "b", RenderLayer = 5 });
            zone.AddEntity(bmo, 6, 6);
            Reveal(zone);
            var bGlyph = ScriptableObject.CreateInstance<Tile>();
            bGlyph.name = "CP437_62"; // 'b'
            var tilePos = new Vector3Int(6, Zone.Height - 1 - 6, 0);
            _mainTilemap.SetTile(tilePos, bGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "BMO renders as its cyan 'b', not as a lookalike villager");
            Object.DestroyImmediate(bGlyph);
        }

        [Test]
        public void CanonicalVillager_StillClaimsItsSprite()
        {
            // Counter-check for the reskin guard: an UN-reskinned '@'
            // villager keeps its sprite — the guard must not lock
            // everyone out.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 7, 6);
            var v = new Entity { ID = "vg", BlueprintName = "Villager" };
            v.AddPart(new RenderPart { DisplayName = "villager", RenderString = "@", RenderLayer = 5 });
            zone.AddEntity(v, 7, 6);
            Reveal(zone);
            var atGlyph = ScriptableObject.CreateInstance<Tile>();
            atGlyph.name = "CP437_40";
            var tilePos = new Vector3Int(7, Zone.Height - 1 - 6, 0);
            _mainTilemap.SetTile(tilePos, atGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("Villager", FindOverlay().GetTile(tilePos)?.name);
            Object.DestroyImmediate(atGlyph);
        }

        [Test]
        public void StageMarkers_KeepTheirColorSignal()
        {
            // AUDIT 🟡 — Well/Oven/Lantern GroundMarkers shift color by
            // repair stage (quest feedback). They keep the honest
            // colored dot; the campfire's static marker still claims.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("WellGroundMarker", "."), 3, 3);
            zone.AddEntity(TerrainEntity("CampfireGroundMarker", "."), 4, 3);
            Reveal(zone);
            var wellPos = PaintFloorGlyph(3, 3, Color.yellow);
            var campPos = PaintFloorGlyph(4, 3, Color.red);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var overlay = FindOverlay();
            Assert.IsNull(overlay.GetTile(wellPos),
                "the stage-colored dot survives — repair progress stays readable");
            StringAssert.StartsWith("floor_m", overlay.GetTile(campPos)?.name,
                "counter-check: the campfire's static marker claims stone as before");
        }

        // ── ROUND 4: incremental claims (perf) ───────────────────

        private static int Key(int x, int zoneY) => zoneY * Zone.Width + x;

        [Test]
        public void IncrementalPath_TouchesOnlyTheDirtyNeighborhood()
        {
            // The audit measured the full release/rescan at ~13-15k
            // tilemap writes per NPC step. The incremental path must
            // resolve ONLY the dirty cell + its 8-neighborhood and
            // leave every other claim untouched.
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 5, 3);
            zone.AddEntity(TerrainEntity("Grass", "."), 20, 10);
            Reveal(zone);
            var posA = PaintFloorGlyph(5, 3, Color.white);
            var posB = PaintFloorGlyph(20, 10, Color.white);
            _renderer.PostRender(zone, Zone.Width, Zone.Height); // full

            long cellsBefore = EnvironmentSpriteRenderer.Perf.CellsResolved;
            var dirty = new System.Collections.Generic.HashSet<int> { Key(20, 10) };
            _renderer.PostRender(zone, Zone.Width, Zone.Height, dirty);
            long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved - cellsBefore;

            Assert.LessOrEqual(resolved, 9,
                "one dirty cell resolves at most its 9-cell neighborhood — not 2000");
            StringAssert.StartsWith("grass", FindOverlay().GetTile(posA)?.name,
                "the untouched claim across the map was never released");
            StringAssert.StartsWith("grass", FindOverlay().GetTile(posB)?.name,
                "the dirty cell re-resolved to the same correct claim");
        }

        [Test]
        public void IncrementalPath_ReresolvesADirtyCellHonestly()
        {
            // A viper steps onto a claimed grass cell; the dirty-path
            // repaint painted its '~'. The incremental release must
            // honor the fresh glyph (round-3 clobber guard) and the
            // re-resolve must go honest-ASCII.
            var zone = ZoneWithGrassAt(5, 3);
            Reveal(zone);
            var tilePos = PaintFloorGlyph(5, 3, Color.white);
            _renderer.PostRender(zone, Zone.Width, Zone.Height); // grass claimed

            var viper = new Entity { ID = "v", BlueprintName = "SteamCloud" };
            viper.AddPart(new RenderPart { DisplayName = "steam", RenderString = "~", RenderLayer = 5 });
            zone.AddEntity(viper, 5, 3);
            var viperGlyph = ScriptableObject.CreateInstance<Tile>();
            viperGlyph.name = "CP437_7E";
            _mainTilemap.SetTile(tilePos, viperGlyph);

            var dirty = new System.Collections.Generic.HashSet<int> { Key(5, 3) };
            _renderer.PostRender(zone, Zone.Width, Zone.Height, dirty);

            Assert.AreEqual("CP437_7E", _mainTilemap.GetTile(tilePos)?.name,
                "the viper's fresh glyph survives the targeted release");
            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "and the re-resolve keeps it honest ASCII");
            Object.DestroyImmediate(viperGlyph);
        }

        [Test]
        public void IncrementalPath_NeighborShorelineUpdates()
        {
            // Wall variants and shoreline edges are functions of their
            // NEIGHBORS — a dirty cell must re-resolve its neighborhood
            // or a terrain change leaves stale edges beside it.
            var zone = new Zone("T");
            var north = TerrainEntity("WaterPuddle", "~");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == -1) { zone.AddEntity(north, 10, 9); continue; }
                    zone.AddEntity(TerrainEntity("WaterPuddle", "~"), 10 + dx, 10 + dy);
                }
            Reveal(zone);
            _renderer.PostRender(zone, Zone.Width, Zone.Height); // full: center = open water
            var centerPos = new Vector3Int(10, Zone.Height - 1 - 10, 0);
            StringAssert.StartsWith("water_m", FindOverlay().GetTile(centerPos)?.name,
                "precondition: fully surrounded by water = open-water macro");

            // The NORTH cell's water drains (terrain change) → land.
            zone.RemoveEntity(north);
            var dirty = new System.Collections.Generic.HashSet<int> { Key(10, 9) };
            _renderer.PostRender(zone, Zone.Width, Zone.Height, dirty);

            Assert.AreEqual("water_e_n", FindOverlay().GetTile(centerPos)?.name,
                "the CENTER (a neighbor of the dirty cell) re-resolves to the north shore lip");
        }

        // ── ROUND 5: bestiary, item bodies, interactables ────────

        [Test]
        public void JungleApe_ClaimsItsBestiarySprite()
        {
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 6, 6);
            var ape = new Entity { ID = "ja", BlueprintName = "JungleApe" };
            ape.AddPart(new RenderPart { DisplayName = "jungle ape", RenderString = "A", RenderLayer = 5 });
            zone.AddEntity(ape, 6, 6);
            Reveal(zone);
            var aGlyph = ScriptableObject.CreateInstance<Tile>();
            aGlyph.name = "CP437_41"; // 'A'
            var tilePos = new Vector3Int(6, Zone.Height - 1 - 6, 0);
            _mainTilemap.SetTile(tilePos, aGlyph);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("JungleApe", FindOverlay().GetTile(tilePos)?.name,
                "the bestiary resolves by blueprint through the actor tier");
            Assert.AreEqual(50, EnvironmentSpriteRenderer.CreatureSprites.Length,
                "roster pin: the 44-creature bestiary + the gin frog (W5.6) "
                + "+ the tepui's lowland band (W6.3a)");
            Object.DestroyImmediate(aGlyph);
        }

        [Test]
        public void EveryDeclaredActorSpriteFile_ActuallyLoads()
        {
            // W5.6 sweep — no loads-audit existed for the actor tables,
            // so a typo'd filename degrades silently to the glyph
            // fallback and is only noticed by a human looking at the
            // screen. (The fixture tier already has this guard in
            // TerrainRenderCoverageTests.)
            foreach (var (bp, file, _) in EnvironmentSpriteRenderer.NamedActorSprites)
                Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/" + file),
                    bp + " declares " + file + ", which must load");
            foreach (var (bp, file, _) in EnvironmentSpriteRenderer.CreatureSprites)
                Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/" + file),
                    bp + " declares " + file + ", which must load");
        }

        [Test]
        public void ReskinnedCreature_KeepsItsHonestGlyph()
        {
            // Dirt gnomes are Snapjaws reskinned to 'g' — same guard,
            // bestiary flavor: a JungleApe reskinned to 'x' stands down.
            var zone = new Zone("T");
            var ape = new Entity { ID = "rx", BlueprintName = "JungleApe" };
            ape.AddPart(new RenderPart { DisplayName = "strange ape", RenderString = "x", RenderLayer = 5 });
            zone.AddEntity(ape, 7, 7);
            Reveal(zone);
            var xGlyph = ScriptableObject.CreateInstance<Tile>();
            xGlyph.name = "CP437_78";
            var tilePos = new Vector3Int(7, Zone.Height - 1 - 7, 0);
            _mainTilemap.SetTile(tilePos, xGlyph);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.IsNull(FindOverlay().GetTile(tilePos),
                "non-canonical glyph = not that creature anymore = honest ASCII");
            Object.DestroyImmediate(xGlyph);
        }

        [Test]
        public void FireTonic_BecomesARedTintedVial()
        {
            // One near-gray vial serves every tonic: the claim copies
            // the glyph's COLOR, so identity rides the tint.
            var zone = new Zone("T");
            var tonic = new Entity { ID = "ft", BlueprintName = "FireTonic" };
            tonic.AddPart(new RenderPart { DisplayName = "fire tonic", RenderString = "!", RenderLayer = 5 });
            zone.AddEntity(tonic, 8, 8);
            Reveal(zone);
            var bangGlyph = ScriptableObject.CreateInstance<Tile>();
            bangGlyph.name = "CP437_21"; // '!'
            var tilePos = new Vector3Int(8, Zone.Height - 1 - 8, 0);
            _mainTilemap.SetTile(tilePos, bangGlyph);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, new Color(1f, 0.3f, 0.2f, 1f)); // red '!'

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            var overlay = FindOverlay();
            Assert.AreEqual("item_vial", overlay.GetTile(tilePos)?.name);
            var c = overlay.GetColor(tilePos);
            Assert.Greater(c.r, c.g + 0.2f,
                "the vial is TINTED RED by the glyph color — the tint IS the identity");
            Object.DestroyImmediate(bangGlyph);
        }

        [Test]
        public void OreVein_ClaimsTheTintedRockFace()
        {
            // Round 2 forced veins to honest ASCII ('*' guard); round 5
            // gives the mineable node a real rock-face body, tinted.
            var zone = new Zone("T");
            var v = TerrainEntity("GlowQuartzVein", "*");
            zone.AddEntity(v, 9, 9);
            Reveal(zone);
            var starGlyph = ScriptableObject.CreateInstance<Tile>();
            starGlyph.name = "CP437_2A"; // '*'
            var tilePos = new Vector3Int(9, Zone.Height - 1 - 9, 0);
            _mainTilemap.SetTile(tilePos, starGlyph);
            _mainTilemap.SetTileFlags(tilePos, TileFlags.None);
            _mainTilemap.SetColor(tilePos, new Color(0.4f, 0.9f, 1f, 1f)); // cyan

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("item_vein", FindOverlay().GetTile(tilePos)?.name,
                "the vein renders as a crystal-flecked rock face (cyan-tinted), not a letter");
            Object.DestroyImmediate(starGlyph);
        }

        [Test]
        public void BerryBush_ClaimsViaTheFixturePrePass()
        {
            var zone = new Zone("T");
            zone.AddEntity(TerrainEntity("Grass", "."), 10, 5);
            var bush = new Entity { ID = "bb", BlueprintName = "BerryBush" };
            bush.AddPart(new RenderPart { DisplayName = "berry bush", RenderString = ";", RenderLayer = 4 });
            zone.AddEntity(bush, 10, 5);
            Reveal(zone);
            var tilePos = PaintFloorGlyph(10, 5, Color.white);

            _renderer.PostRender(zone, Zone.Width, Zone.Height);

            Assert.AreEqual("BerryBush", FindOverlay().GetTile(tilePos)?.name,
                "the new interactable resolves blueprint-keyed, glyph-independent");
        }

        [Test]
        public void ResolveItemBody_FamilyPins()
        {
            Assert.AreEqual("item_vial", EnvironmentSpriteRenderer.ResolveItemBody("HealingTonic"));
            Assert.AreEqual("item_book", EnvironmentSpriteRenderer.ResolveItemBody("ConflagrationGrimoire"));
            Assert.AreEqual("item_vein", EnvironmentSpriteRenderer.ResolveItemBody("PaleSaltVein"));
            Assert.AreEqual("item_gem", EnvironmentSpriteRenderer.ResolveItemBody("GlowQuartz"));
            Assert.AreEqual("item_seed", EnvironmentSpriteRenderer.ResolveItemBody("CandyCarrotSeed"));
            Assert.AreEqual("item_meat", EnvironmentSpriteRenderer.ResolveItemBody("DriedMeat"));
            Assert.AreEqual("item_torch", EnvironmentSpriteRenderer.ResolveItemBody("Torch"));
            Assert.IsNull(EnvironmentSpriteRenderer.ResolveItemBody("Wall"),
                "counter-check: non-items resolve nothing");
            Assert.IsNull(EnvironmentSpriteRenderer.ResolveItemBody(null));
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

