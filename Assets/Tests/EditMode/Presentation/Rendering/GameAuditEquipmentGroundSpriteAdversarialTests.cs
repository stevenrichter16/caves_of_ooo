using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Seven ground-item families: real resource, fallback and claim-lifecycle gates.
    // 32 cases: seven PNG/import contracts, seven removed-registration controls,
    // and eighteen cross-family, visibility, lifecycle, fallback and tint probes.
    public class GameAuditEquipmentGroundSpriteAdversarialTests
    {
        static readonly string[] Bodies = { "item_dagger", "item_sword", "item_spear",
            "item_boots", "item_gloves", "item_helmet", "item_mace" };
        static readonly string[,] Mappings = {
            { "Dagger", "item_dagger" }, { "ShortSword", "item_sword" },
            { "LongSword", "item_sword" }, { "Spear", "item_spear" },
            { "LeatherBoots", "item_boots" }, { "IronshodBoots", "item_boots" },
            { "LeatherGloves", "item_gloves" }, { "LeatherCap", "item_helmet" },
            { "IronHelmet", "item_helmet" }, { "Mace", "item_mace" }
        };
        const string AssetRoot = "Assets/Resources/Sprites/Environment/";
        GroundSpriteAuditFixture _f;
        [SetUp] public void Setup() { _f = new GroundSpriteAuditFixture(); }
        [TearDown] public void Teardown() { _f?.Dispose(); }

        // Source PNG is decoded separately; imported textures deliberately stay unreadable.
        // Testing the importer does not prove silhouette recognition or visual quality.
        [TestCaseSource(nameof(Bodies))]
        public void Adversarial_AssetHasFullPixelFrameBinaryAlphaSharedInkAndSafeImport(string body)
        {
            string path = AssetRoot + body + ".png";
            Assert.IsTrue(File.Exists(path), body + " production PNG is missing");
            var sprite = Resources.Load<Sprite>("Sprites/Environment/" + body);
            Assert.NotNull(sprite, body + " must load through the production Resources API");
            Assert.AreEqual(path, AssetDatabase.GetAssetPath(sprite));
            Assert.AreEqual(new Rect(0, 0, 16, 16), sprite.rect, "no cropped armor slice");
            Assert.AreEqual(16f, sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(8, 8), sprite.pivot);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(importer);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.IsTrue(importer.alphaIsTransparency);
            var png = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(png.LoadImage(File.ReadAllBytes(path), false));
                Assert.AreEqual(16, png.width); Assert.AreEqual(16, png.height);
                int transparent = 0, opaque = 0, ink = 0, bodyPixels = 0;
                foreach (var p in png.GetPixels32())
                {
                    Assert.IsTrue(p.a == 0 || p.a == 255, "semi-transparent source pixel");
                    if (p.a == 0) transparent++;
                    else
                    {
                        opaque++;
                        if (p.r == 30 && p.g == 32 && p.b == 28) ink++;
                        else bodyPixels++;
                    }
                }
                Assert.Greater(transparent, 0, "opaque square is not an item cutout");
                Assert.Greater(opaque, 0, "blank art must fail");
                Assert.Greater(ink, 0, "shared outline must be authored in the PNG");
                Assert.Greater(bodyPixels, 0, "an ink-only placeholder must fail");
            }
            finally { Object.DestroyImmediate(png); }
        }

        // Removal models an unavailable resource without changing production files.
        // A positive registration assertion prevents a vacuous missing-asset test.
        [TestCase("Dagger", "item_dagger")]
        [TestCase("ShortSword", "item_sword")]
        [TestCase("Spear", "item_spear")]
        [TestCase("LeatherBoots", "item_boots")]
        [TestCase("LeatherGloves", "item_gloves")]
        [TestCase("LeatherCap", "item_helmet")]
        [TestCase("Mace", "item_mace")]
        public void Adversarial_MissingNewBodyRetainsOwnGlyphInsteadOfWrongGenericFamily(string bp, string body)
        {
            Assert.IsTrue(_f.ItemTiles.ContainsKey(body), "new body must be registered first");
            Assert.NotNull(_f.ItemTiles[body]);
            _f.ItemTiles.Remove(body);
            var item = _f.Place(bp, 4, 3);
            var glyph = _f.Paint(item, 4, 3, Color.cyan);
            _f.Render();
            Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 3)), "missing art must not claim a different family");
            Assert.AreSame(glyph, _f.Main.GetTile(_f.Pos(4, 3)));
            Assert.AreEqual(Color.cyan, _f.Main.GetColor(_f.Pos(4, 3)));
            // Other authored bodies must still work after the local miss.
            var tonic = _f.Place("FireTonic", 8, 3);
            _f.Paint(tonic, 8, 3, Color.red); _f.Render();
            _f.AssertBody(8, 3, "item_vial");
        }

        [Test]
        public void Adversarial_AllTenExactBlueprintsReachTheirRealResourcesSpriteAtUnflippedRow()
        {
            for (int i = 0; i < Mappings.GetLength(0); i++)
            {
                string bp = Mappings[i, 0], body = Mappings[i, 1];
                Assert.AreEqual(body, EnvironmentSpriteRenderer.ResolveItemBody(bp), bp);
                var item = _f.Place(bp, 2 + i * 3, 3);
                Assert.AreEqual(5, item.GetPart<RenderPart>().RenderLayer, bp + " real ground-item layer");
                _f.Paint(item, 2 + i * 3, 3, Color.white);
            }
            _f.Render();
            for (int i = 0; i < Mappings.GetLength(0); i++)
            {
                var tile = _f.AssertBody(2 + i * 3, 3, Mappings[i, 1]);
                Assert.AreSame(Resources.Load<Sprite>("Sprites/Environment/" + Mappings[i, 1]), tile.sprite);
                Assert.IsNull(_f.Overlay.GetTile(new Vector3Int(2 + i * 3, 3, 0)), "must not mirror zone y");
            }
        }

        [Test]
        public void Adversarial_SevenFamiliesHaveDifferentSilhouettesAndDistinctGuids()
        {
            var masks = new HashSet<string>(); var guids = new HashSet<string>();
            string templateGuid = AssetDatabase.AssetPathToGUID(AssetRoot + "weapon_ground.png");
            foreach (var body in Bodies)
            {
                string path = AssetRoot + body + ".png";
                Assert.IsTrue(File.Exists(path), body);
                string guid = AssetDatabase.AssetPathToGUID(path);
                Assert.AreEqual(32, guid.Length, body + " imported GUID");
                Assert.AreNotEqual(templateGuid, guid, "metadata copy must get a fresh GUID");
                Assert.IsTrue(guids.Add(guid), "two families alias the same GUID");
                var png = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.IsTrue(png.LoadImage(File.ReadAllBytes(path), false));
                    var p = png.GetPixels32(); var mask = new char[p.Length];
                    for (int i = 0; i < p.Length; i++) mask[i] = p[i].a == 0 ? '.' : '#';
                    Assert.IsTrue(masks.Add(new string(mask)), body + " duplicates another family's silhouette");
                }
                finally { Object.DestroyImmediate(png); }
            }
            Assert.AreEqual(7, masks.Count);
        }

        [Test]
        public void Adversarial_ExactMappingDoesNotCaptureUnreviewedAliasesOrMalformedNames()
        {
            foreach (string bp in new[] { null, "", " ", "dagger", "DAGGER", " Dagger", "Dagger ",
                "RentalDagger", "UnknownSword", "Greatsword", "ForgedWeapon", "Hatchet", "Battleaxe",
                "Warhammer", "Buckler", "IronBuckler", "Cloak", "WardedCloak", "UnknownBoots", "LeatherArmor" })
                Assert.IsNull(EnvironmentSpriteRenderer.ResolveItemBody(bp), bp ?? "<null>");
            Assert.AreEqual("item_vial", EnvironmentSpriteRenderer.ResolveItemBody("FireTonic"));
            foreach (string bp in new[] { "Greatsword", "ForgedWeapon" })
            {
                var item = _f.Place(bp, bp == "Greatsword" ? 4 : 8, 4);
                _f.Paint(item, bp == "Greatsword" ? 4 : 8, 4, Color.white);
            }
            _f.Render(); _f.AssertBody(4, 4, "WeaponGround"); _f.AssertBody(8, 4, "WeaponGround");
        }

        [Test]
        public void Adversarial_NullNewTileRetainsMaceGlyphAndDoesNotBorrowVial()
        {
            Assert.IsTrue(_f.ItemTiles.ContainsKey("item_mace"));
            Assert.NotNull(_f.ItemTiles["item_mace"]);
            _f.ItemTiles["item_mace"] = null;
            var item = _f.Place("Mace", 4, 4); var glyph = _f.Paint(item, 4, 4, Color.white);
            _f.Render(); Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 4)));
            Assert.AreSame(glyph, _f.Main.GetTile(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_UnrelatedVialAndTorsoArmorKeepExistingFamilyFallbacks()
        {
            var tonic = _f.Place("FireTonic", 4, 4); _f.Paint(tonic, 4, 4, Color.red);
            var armor = _f.Place("LeatherArmor", 8, 4); _f.Paint(armor, 8, 4, Color.gray);
            _f.Render(); _f.AssertBody(4, 4, "item_vial"); _f.AssertBody(8, 4, "item_armor");
            Assert.AreEqual(Color.red, _f.Overlay.GetColor(_f.Pos(4, 4)));
            // Existing recognized item family can still use its old glyph fallback.
            var book = _f.Place("ConflagrationGrimoire", 12, 4);
            Assert.IsTrue(_f.ItemTiles.Remove("item_book"));
            var glyph = _f.Paint(book, 12, 4, Color.yellow); _f.Render();
            Assert.IsNull(_f.Overlay.GetTile(_f.Pos(12, 4)));
            Assert.AreSame(glyph, _f.Main.GetTile(_f.Pos(12, 4)));
        }

        [Test]
        public void Adversarial_RealChestAndWatchLanternKeepTheirFixturePrecedence()
        {
            var chest = _f.Place("Chest", 4, 4); _f.Paint(chest, 4, 4, Color.white);
            var lantern = _f.Place("WatchLantern", 8, 4); _f.Paint(lantern, 8, 4, Color.yellow);
            _f.Render(); _f.AssertBody(4, 4, "Chest"); _f.AssertBody(8, 4, "Lantern");
        }

        [Test]
        public void Adversarial_HigherLayerActorHidesGroundEquipmentUntilItLeaves()
        {
            var item = _f.Place("Dagger", 4, 4);
            var actor = new Entity { ID = "sprite-audit-actor", BlueprintName = "Villager" };
            actor.AddPart(new RenderPart { RenderString = "@", RenderLayer = 20 });
            _f.Zone.AddEntity(actor, 4, 4); _f.Paint(actor, 4, 4, Color.white);
            _f.Render(); _f.AssertBody(4, 4, "Villager");
            _f.Zone.RemoveEntity(actor); _f.Paint(item, 4, 4, Color.white);
            _f.Render(); _f.AssertBody(4, 4, "item_dagger");
        }

        [Test]
        public void Adversarial_RememberedFogDoesNotRevealGroundEquipment()
        {
            var item = _f.Place("Mace", 4, 4); _f.Paint(item, 4, 4, Color.white);
            _f.Render(); _f.AssertBody(4, 4, "item_mace");
            _f.Zone.GetCell(4, 4).IsVisible = false;
            _f.Main.ClearAllTiles(); _f.Renderer.NotifyMainTilemapCleared();
            _f.Render(); Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 4)));
            Assert.IsNull(_f.Main.GetTile(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_UnexploredCellCannotClaimNewItemArt()
        {
            var item = _f.Place("LeatherBoots", 4, 4); _f.Paint(item, 4, 4, Color.white);
            _f.Zone.GetCell(4, 4).Explored = false; _f.Render();
            Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 4)));
            _f.Zone.GetCell(4, 4).Explored = true; _f.Render(); _f.AssertBody(4, 4, "item_boots");
        }

        [Test]
        public void Adversarial_InvisibleHigherItemDoesNotHideVisibleLowerItem()
        {
            var sword = _f.Place("ShortSword", 4, 4);
            var mace = _f.Place("Mace", 4, 4); mace.GetPart<RenderPart>().Visible = false;
            Assert.AreSame(sword, _f.Zone.GetCell(4, 4).GetTopVisibleObject());
            _f.Paint(sword, 4, 4, Color.white); _f.Render(); _f.AssertBody(4, 4, "item_sword");
            mace.GetPart<RenderPart>().Visible = true;
            _f.Paint(mace, 4, 4, Color.white); _f.Render(); _f.AssertBody(4, 4, "item_mace");
        }

        [Test]
        public void Adversarial_StationaryFullRepaintReplacesOldFamilyWithoutStaleClaim()
        {
            var item = _f.Place("Dagger", 4, 4); _f.Paint(item, 4, 4, Color.cyan);
            _f.Render(); _f.AssertBody(4, 4, "item_dagger"); _f.Zone.RemoveEntity(item);
            var boots = _f.Place("LeatherBoots", 4, 4);
            _f.Main.ClearAllTiles(); _f.Renderer.NotifyMainTilemapCleared();
            _f.Paint(boots, 4, 4, Color.gray); _f.Render(); _f.AssertBody(4, 4, "item_boots");
            Assert.AreEqual(Color.gray, _f.Overlay.GetColor(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_IncrementalReplacementUsesNewGlyphColorAndBody()
        {
            var item = _f.Place("Mace", 4, 4); _f.Paint(item, 4, 4, Color.white);
            _f.Render(); _f.AssertBody(4, 4, "item_mace"); _f.Zone.RemoveEntity(item);
            var glove = _f.Place("LeatherGloves", 4, 4); _f.Paint(glove, 4, 4, Color.green);
            _f.RenderDirty(4, 4); _f.AssertBody(4, 4, "item_gloves");
            Assert.AreEqual(Color.green, _f.Overlay.GetColor(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_ItemRemovalExposesFreshUnmappedGlyphWithoutRestoringOldOne()
        {
            var item = _f.Place("Mace", 4, 4); _f.Paint(item, 4, 4, Color.white);
            _f.Render(); _f.AssertBody(4, 4, "item_mace"); _f.Zone.RemoveEntity(item);
            var other = new Entity { ID = "sprite-audit-other", BlueprintName = "SpriteAuditUnknown" };
            other.AddPart(new RenderPart { RenderString = "?", RenderLayer = 5 });
            _f.Zone.AddEntity(other, 4, 4); var glyph = _f.Paint(other, 4, 4, Color.yellow);
            _f.RenderDirty(4, 4); Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 4)));
            Assert.AreSame(glyph, _f.Main.GetTile(_f.Pos(4, 4)));
            Assert.AreEqual(Color.yellow, _f.Main.GetColor(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_TwoSameBlueprintInstancesShareBodyWithoutSharingCellTint()
        {
            var a = _f.Place("Dagger", 4, 4); var b = _f.Place("Dagger", 8, 4);
            Assert.AreNotSame(a, b); Assert.AreNotEqual(a.ID, b.ID);
            _f.Paint(a, 4, 4, Color.cyan); _f.Paint(b, 8, 4, Color.magenta); _f.Render();
            Assert.AreSame(_f.AssertBody(4, 4, "item_dagger"), _f.AssertBody(8, 4, "item_dagger"));
            Assert.AreEqual(Color.cyan, _f.Overlay.GetColor(_f.Pos(4, 4)));
            Assert.AreEqual(Color.magenta, _f.Overlay.GetColor(_f.Pos(8, 4)));
        }

        [Test]
        public void Adversarial_BrightAndDimForegroundCarryHueAndAlphaWithoutAuthoredColorOverride()
        {
            var bright = new Color(0.8f, 0.2f, 0.1f, 0.9f);
            var dim = new Color(0.2f, 0.05f, 0.025f, 0.9f);
            var a = _f.Place("LeatherCap", 4, 4); var b = _f.Place("IronHelmet", 8, 4);
            _f.Paint(a, 4, 4, bright); _f.Paint(b, 8, 4, dim); _f.Render();
            _f.AssertBody(4, 4, "item_helmet"); _f.AssertBody(8, 4, "item_helmet");
            Assert.AreEqual(bright, _f.Overlay.GetColor(_f.Pos(4, 4)));
            Assert.AreEqual(dim, _f.Overlay.GetColor(_f.Pos(8, 4)));
        }

        [Test]
        public void Adversarial_ReleaseClaimsRestoresOriginalGlyphAndTint()
        {
            var item = _f.Place("Spear", 4, 4); var glyph = _f.Paint(item, 4, 4, Color.yellow);
            _f.Render(); _f.AssertBody(4, 4, "item_spear");
            _f.Renderer.ReleaseAllClaims(); Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 4)));
            Assert.AreSame(glyph, _f.Main.GetTile(_f.Pos(4, 4)));
            Assert.AreEqual(Color.yellow, _f.Main.GetColor(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_DirtyNeighborhoodPreservesDistantEquipmentClaimAndTint()
        {
            var a = _f.Place("LeatherGloves", 4, 4); var b = _f.Place("LongSword", 30, 15);
            _f.Paint(a, 4, 4, Color.green); _f.Paint(b, 30, 15, Color.yellow); _f.Render();
            var distant = _f.AssertBody(30, 15, "item_sword");
            long before = EnvironmentSpriteRenderer.Perf.CellsResolved;
            _f.Paint(a, 4, 4, Color.blue); _f.RenderDirty(4, 4);
            Assert.Greater(EnvironmentSpriteRenderer.Perf.CellsResolved - before, 0);
            Assert.LessOrEqual(EnvironmentSpriteRenderer.Perf.CellsResolved - before, 9);
            Assert.AreSame(distant, _f.AssertBody(30, 15, "item_sword"));
            Assert.AreEqual(Color.yellow, _f.Overlay.GetColor(_f.Pos(30, 15)));
            Assert.AreEqual(Color.blue, _f.Overlay.GetColor(_f.Pos(4, 4)));
        }

        [Test]
        public void Adversarial_DisablingSpriteModeRestoresEquipmentGlyphThenReclaimsOnEnable()
        {
            var item = _f.Place("IronshodBoots", 4, 4); var glyph = _f.Paint(item, 4, 4, Color.white);
            _f.Render(); _f.AssertBody(4, 4, "item_boots");
            _f.Renderer.RenderingEnabled = false; _f.Render();
            Assert.IsNull(_f.Overlay.GetTile(_f.Pos(4, 4)));
            Assert.AreSame(glyph, _f.Main.GetTile(_f.Pos(4, 4)));
            _f.Renderer.RenderingEnabled = true; _f.Render(); _f.AssertBody(4, 4, "item_boots");
        }
    }

    // Local harness follows shipped EnvironmentSpriteRendererHarnessTests APIs.
    // It additionally restores shared perf/cache state and destroys the runtime
    // Tile/Sprite objects Init creates (without destroying imported Resources).
    internal sealed class GroundSpriteAuditFixture : IDisposable
    {
        readonly EntityEquipmentContentFixture _content;
        readonly GameObject _grid;
        readonly List<Tile> _glyphs = new List<Tile>();
        readonly List<Tile> _ownedTiles = new List<Tile>();
        readonly List<Sprite> _ownedSprites = new List<Sprite>();
        readonly Dictionary<FieldInfo, long> _perf = new Dictionary<FieldInfo, long>();
        readonly Dictionary<TileBase, char> _glyphCache;
        readonly Dictionary<TileBase, char> _oldGlyphCache;
        public readonly Zone Zone = new Zone("EquipmentGroundSpriteAudit");
        public readonly Tilemap Main, Background, Overlay;
        public readonly EnvironmentSpriteRenderer Renderer;
        public readonly Dictionary<string, Tile> ItemTiles;

        public GroundSpriteAuditFixture()
        {
            _glyphCache = (Dictionary<TileBase, char>)typeof(EnvironmentSpriteRenderer)
                .GetField("s_glyphCache", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            _oldGlyphCache = new Dictionary<TileBase, char>(_glyphCache);
            foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.FieldType == typeof(long)) _perf.Add(field, (long)field.GetValue(null));
            _content = new EntityEquipmentContentFixture();
            var oldTiles = new HashSet<Tile>(Resources.FindObjectsOfTypeAll<Tile>());
            var oldSprites = new HashSet<Sprite>(Resources.FindObjectsOfTypeAll<Sprite>());
            _grid = new GameObject("EquipmentGroundSpriteAuditGrid"); _grid.AddComponent<Grid>();
            Main = MakeMap("Main"); Background = MakeMap("Background");
            Renderer = _grid.AddComponent<EnvironmentSpriteRenderer>();
            Renderer.Init(_grid.transform, Main, Background);
            Overlay = _grid.transform.Find("EnvironmentSpriteTilemap").GetComponent<Tilemap>();
            ItemTiles = (Dictionary<string, Tile>)typeof(EnvironmentSpriteRenderer)
                .GetField("_itemBodyTiles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Renderer);
            foreach (var tile in Resources.FindObjectsOfTypeAll<Tile>())
                if (!oldTiles.Contains(tile) && !AssetDatabase.Contains(tile)) _ownedTiles.Add(tile);
            foreach (var sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                if (!oldSprites.Contains(sprite) && !AssetDatabase.Contains(sprite)) _ownedSprites.Add(sprite);
        }

        Tilemap MakeMap(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(_grid.transform, false);
            var map = go.AddComponent<Tilemap>(); go.AddComponent<TilemapRenderer>(); return map;
        }
        public Vector3Int Pos(int x, int y) => new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0);
        public Entity Place(string bp, int x, int y)
        {
            var e = _content.Create(bp); Assert.NotNull(e, "missing actual blueprint " + bp);
            Assert.NotNull(e.GetPart<RenderPart>(), "missing real RenderPart " + bp);
            Zone.AddEntity(e, x, y); var cell = Zone.GetCell(x, y);
            cell.Explored = cell.IsVisible = true; return e;
        }
        public Tile Paint(Entity entity, int x, int y, Color color)
        {
            var glyph = entity.GetPart<RenderPart>().RenderString;
            Assert.IsFalse(string.IsNullOrEmpty(glyph));
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = "CP437_" + ((int)glyph[0]).ToString("X2"); _glyphs.Add(tile);
            Main.SetTile(Pos(x, y), tile); Main.SetTileFlags(Pos(x, y), TileFlags.None);
            Main.SetColor(Pos(x, y), color); return tile;
        }
        public void Render() => Renderer.PostRender(Zone, CavesOfOoo.Core.Zone.Width, CavesOfOoo.Core.Zone.Height);
        public void RenderDirty(int x, int y) => Renderer.PostRender(Zone, CavesOfOoo.Core.Zone.Width,
            CavesOfOoo.Core.Zone.Height, new HashSet<int> { y * CavesOfOoo.Core.Zone.Width + x });
        public Tile AssertBody(int x, int y, string name)
        {
            var tile = Overlay.GetTile(Pos(x, y)) as Tile;
            Assert.NotNull(tile, name + " not claimed"); Assert.AreEqual(name, tile.name);
            Assert.NotNull(tile.sprite, name + " has no real Sprite");
            Assert.IsNull(Main.GetTile(Pos(x, y)), "claimed glyph must be displaced"); return tile;
        }
        public void Dispose()
        {
            if (_grid != null) Object.DestroyImmediate(_grid);
            foreach (var tile in _glyphs) if (tile != null) Object.DestroyImmediate(tile);
            foreach (var tile in _ownedTiles) if (tile != null) Object.DestroyImmediate(tile);
            foreach (var sprite in _ownedSprites) if (sprite != null) Object.DestroyImmediate(sprite);
            _glyphCache.Clear(); foreach (var pair in _oldGlyphCache) _glyphCache.Add(pair.Key, pair.Value);
            foreach (var pair in _perf) pair.Key.SetValue(null, pair.Value);
            _content?.Dispose();
        }
    }
}
