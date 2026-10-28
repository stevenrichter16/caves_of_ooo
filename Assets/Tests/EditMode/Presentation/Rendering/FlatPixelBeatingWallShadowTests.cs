using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Actual submitted painter geometry and state, not a new production API pin.
    // Native/live acceptance belongs to the root runner; this fixture does not
    // establish perceived contrast, whole-scene quality or device performance.
    public sealed class FlatPixelBeatingWallShadowTests
    {
        const int X = 10, Y = 10;
        const string Ruin = "Overworld.19.12.0";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity wall, receiver;
        EntityFactory previousDestructionFactory;

        [SetUp] public void Setup()
        {
            previousDestructionFactory = DestructionSystem.EntityFactoryRef;
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach(Ruin); world = new FlatPixelGreyboxCatalog();
            DestructionSystem.EntityFactoryRef = content.Factory;
            Assert.True(FlatPixelBeatingMaterialsCatalog.IsSupportedZone(zone));
            Assert.AreEqual(Formation.RuinField, FormationSelector.For(BiomeType.Beating, Ruin));
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); }
            finally { try { content?.Dispose(); } finally { DestructionSystem.EntityFactoryRef = previousDestructionFactory; } }
        }
        Zone Attach(string id)
        {
            var z = new Zone(id); manager.SetActiveZone(z);
            foreach (var cell in z.Cells) cell.Explored = cell.IsVisible = true;
            return z;
        }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, x, y)); return e; }
        void Scene(string floor = "Sand")
        { Add("Sand"); wall = Add("SandstoneWall"); receiver = Add(floor, X, Y + 1); }
        static object Read(object o, string name)
        { var f = o.GetType().GetField(name, I); Assert.NotNull(f, name); return f.GetValue(o); }
        static void Write(object o, string name, object value)
        { var f = o.GetType().GetField(name, I); Assert.NotNull(f, name); f.SetValue(o, value); }
        void Submitted(Paint p)
        {
            p.Step(); var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
            Assert.NotNull(tile, "The selected native wall must submit its source-anchored shade.");
            Assert.AreSame(p.At(X, Y), tile);
            Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1)), "The receiver is not a second shadow owner.");
            Assert.AreEqual(new Color(0, 0, 0, .16f), p.Shade.GetColor(Paint.Position(X, Y)));
        }
        void Absent(Paint p)
        { Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y))); Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1))); }
        void FullParity(Paint p)
        {
            var tile = p.Shade.GetTile(Paint.Position(X, Y));
            var color = p.Shade.GetColor(Paint.Position(X, Y));
            var bounds = tile == null ? default : p.ShadeBounds(X, Y);
            p.Renderer.MarkDirty("Beating wall shade full-pass control"); p.Step();
            Assert.AreSame(tile, p.Shade.GetTile(Paint.Position(X, Y)));
            if (tile != null) { Assert.AreEqual(color, p.Shade.GetColor(Paint.Position(X, Y))); Assert.AreEqual(bounds, p.ShadeBounds(X, Y)); }
            Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1)));
        }

        [TestCase("Sand")] [TestCase("SandstoneFloor")]
        public void SubmittedShadeBorrowsOpaqueWallAndExtendsExactlyTwoNativePixelsOntoDryMaterial(string floor)
        {
            Scene(floor);
            using (var p = new Paint(world, zone))
            {
                int version = zone.EntityVersion, hp = wall.GetPart<DestructiblePart>().HP;
                string state = zone.TileState.ToSaveString(); Submitted(p);
                Assert.True(p.Environment.FlatWallShadowEnabled);
                var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
                var atlas = Resources.Load<Texture2D>(FlatPixelBeatingMaterialsCatalog.ResourcePath);
                Assert.AreSame(atlas, tile.sprite.texture);
                Assert.AreEqual(new Rect(16 * (16 + ((X + Y) & 1)), 0, 16, 24), tile.sprite.rect);
                Assert.AreEqual(FilterMode.Point, atlas.filterMode);
                Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
                Assert.IsNull(p.Shade.GetComponent<TilemapCollider2D>());
                var renderer = p.Shade.GetComponent<TilemapRenderer>(); Assert.NotNull(renderer.sharedMaterial);
                Assert.AreEqual(0, renderer.sortingOrder);
                Assert.That(renderer.sharedMaterial.shader.name, Does.Not.Contain("Lit-Default"));
                Assert.AreNotSame(p.Decoration, p.Shade);
                Assert.AreEqual(1, p.Decoration.GetComponent<TilemapRenderer>().sortingOrder);
                Bounds body = p.BodyBounds(X, Y), shade = p.ShadeBounds(X, Y);
                Assert.AreEqual(body.size, shade.size);
                Assert.That(Vector3.Distance(new Vector3(0, -2f / 16, 0), shade.center - body.center), Is.LessThan(.0001f));
                Assert.That(shade.min.y, Is.EqualTo(p.CellBounds(X, Y).min.y - 2f / 16).Within(.0001f));
                Assert.GreaterOrEqual(shade.min.y, p.CellBounds(X, Y + 1).min.y);
                Assert.That(shade.min.x, Is.EqualTo(body.min.x).Within(.0001f));
                Assert.That(shade.max.x, Is.EqualTo(body.max.x).Within(.0001f));
                Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y + 1).BlocksMovement());
                Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(hp, wall.GetPart<DestructiblePart>().HP);
                Assert.AreEqual(state, zone.TileState.ToSaveString());
                // Import is intentionally non-readable. Decode the actual source
                // PNG independently; do not alter its importer or borrowed pixels.
                var pixels = new Texture2D(2, 2);
                try
                {
                    Assert.True(pixels.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,
                        "Resources/Sprites/Live16x24/beating-materials.png"))));
                    Assert.AreEqual(atlas.width, pixels.width); Assert.AreEqual(atlas.height, pixels.height);
                    foreach (int frame in new[] { 16, 17 })
                        foreach (var pixel in pixels.GetPixels(frame * 16, 0, 16, 24))
                            Assert.AreEqual(1f, pixel.a, "Opaque masonry must cover the duplicate above its southern strip.");
                }
                finally { Object.DestroyImmediate(pixels); }
            }
        }

        [Test] public void SourceMustStayDisclosedSelectedAndCanonicallyNative()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var cell = zone.GetCell(X, Y); var render = wall.GetPart<RenderPart>();
                cell.IsVisible = false; p.Dirty(X, Y); p.Step(); Absent(p);
                cell.IsVisible = true; p.Dirty(X, Y); Submitted(p);
                render.Visible = false; p.Dirty(X, Y); p.Step(); Absent(p);
                render.Visible = true; p.Dirty(X, Y); Submitted(p);
                render.VisualVariant = "different masonry"; p.Dirty(X, Y); p.Step(); Absent(p);
                render.VisualVariant = ""; p.Dirty(X, Y); Submitted(p);
                render.ParentEntity = new Entity(); p.Dirty(X, Y); p.Step(); Absent(p);
                render.ParentEntity = wall; p.Dirty(X, Y); Submitted(p);
                var actor = Add("Player"); p.Dirty(X, Y); p.Step(); Absent(p);
                Assert.True(zone.RemoveEntity(actor)); p.Dirty(X, Y); Submitted(p); FullParity(p);
            }
        }

        [Test] public void ReceiverRequiresDisclosedUnambiguousDryGroundWithoutVisibleOccupants()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var body = p.At(X, Y); var cell = zone.GetCell(X, Y + 1);
                cell.IsVisible = false; p.Dirty(X, Y + 1); p.Step(); Absent(p);
                cell.IsVisible = true; p.Dirty(X, Y + 1); Submitted(p);
                cell.Explored = false; p.Dirty(X, Y + 1); p.Step(); Absent(p);
                cell.Explored = true; p.Dirty(X, Y + 1); Submitted(p);
                foreach (var name in new[] { "WaterPuddle", "Grass", "Rock", "Dagger", "Player" })
                {
                    var obstacle = Add(name, X, Y + 1); p.Dirty(X, Y + 1); p.Step(); Absent(p);
                    Assert.AreSame(body, p.At(X, Y));
                    if (name == "Dagger")
                    {
                        obstacle.GetPart<RenderPart>().Visible = false; p.Dirty(X, Y + 1); Submitted(p);
                        obstacle.GetPart<RenderPart>().Visible = true; p.Dirty(X, Y + 1); p.Step(); Absent(p);
                    }
                    Assert.True(zone.RemoveEntity(obstacle)); p.Dirty(X, Y + 1); Submitted(p);
                }
                zone.TileState.WriteCoating(X, Y + 1, "oil", 5); p.Step(); Absent(p);
                Assert.True(zone.TileState.RemoveCoating(X, Y + 1, "oil")); Submitted(p); FullParity(p);
            }
        }

        [Test] public void SouthOnlyDirtyChangesWithdrawAndRestoreShadeWithFullPassParity()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var body = p.At(X, Y);
                var item = Add("Dagger", X, Y + 1); p.Dirty(X, Y + 1);
                Assert.False(p.FullDirty); p.Step(); Absent(p); Assert.AreSame(body, p.At(X, Y)); FullParity(p);
                Assert.True(zone.RemoveEntity(item)); p.Dirty(X, Y + 1); Assert.False(p.FullDirty);
                Submitted(p); FullParity(p);
                zone.TileState.WriteCoating(X, Y + 1, "water", ZoneTileState.Permanent);
                Assert.False(p.FullDirty); p.Step(); Absent(p); FullParity(p);
                Assert.True(zone.TileState.RemoveCoating(X, Y + 1, "water"));
                Assert.False(p.FullDirty); Submitted(p); FullParity(p);
            }
        }

        [Test] public void RealWallDestructionClearsShadeAndKeepsTheReceiverMaterial()
        {
            Scene("SandstoneFloor"); using (var p = new Paint(world, zone))
            {
                Submitted(p); var floor = p.GroundAt(X, Y + 1);
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 999, null, zone));
                Assert.True(wall.GetPart<DestructiblePart>().Gone); Assert.IsNull(zone.GetEntityCell(wall));
                p.Step(); Absent(p); FullParity(p); Assert.AreSame(floor, p.GroundAt(X, Y + 1));
                Assert.AreSame(receiver, zone.GetCell(X, Y + 1).GetTopVisibleObject());
            }
        }

        [Test] public void ReversibleOptionAndLegacyExitClearOnlyShadeAndRestoreItOnReturn()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var body = p.At(X, Y); var floor = p.GroundAt(X, Y + 1); int version = zone.EntityVersion;
                p.Environment.FlatWallShadowEnabled = false; Absent(p);
                Assert.AreSame(body, p.At(X, Y)); Assert.AreSame(floor, p.GroundAt(X, Y + 1));
                p.Step(); Absent(p); p.Environment.FlatWallShadowEnabled = true; Submitted(p);
                p.Renderer.FlatPixelModeEnabled = false; p.Step(); Absent(p);
                p.Renderer.FlatPixelModeEnabled = true; Submitted(p);
                Assert.AreEqual(version, zone.EntityVersion);
                Assert.AreSame(body, p.At(X, Y)); Assert.AreSame(floor, p.GroundAt(X, Y + 1));
            }
        }

        [Test] public void ChangedMapOrReplacedCurrentGraphCannotBorrowBeatingShadowAdmission()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var map = manager.WorldMap; var biome = map.Tiles[19, 12]; var poi = map.POIs[19, 12];
                try
                {
                    map.Tiles[19, 12] = BiomeType.Sodden; p.Dirty(X, Y); p.Step(); Absent(p);
                    map.Tiles[19, 12] = biome; p.Dirty(X, Y); Submitted(p);
                    map.SetPOI(19, 12, new PointOfInterest(POIType.MerchantCamp, "not ordinary ruins"));
                    p.Dirty(X, Y); p.Step(); Absent(p);
                    map.POIs[19, 12] = poi; p.Dirty(X, Y); Submitted(p);
                    manager.CachedZones[Ruin] = new Zone(Ruin); p.Dirty(X, Y); p.Step(); Absent(p);
                }
                finally { map.Tiles[19, 12] = biome; map.POIs[19, 12] = poi; manager.CachedZones[Ruin] = zone; }
                p.Dirty(X, Y); Submitted(p);
            }
        }

        [Test] public void MissingNativeAtlasKeepsFallbackBodyAndCannotCastAnApprovedShade()
        {
            Scene(); using (var p = new Paint(world, zone))
            using (var unavailable = new FlatPixelGreyboxCatalog((Texture2D)null))
            {
                Submitted(p); p.Catalog(unavailable); p.Renderer.MarkDirty("missing Beating atlas"); p.Step();
                Absent(p); Assert.NotNull(p.At(X, Y));
                Assert.True(unavailable.TryResolveFallback(zone, X, Y, out var fallback));
                Assert.AreSame(wall, fallback.Owner); Assert.AreSame(fallback.ForegroundTile, p.At(X, Y));
                p.Catalog(world); p.Renderer.MarkDirty("restored Beating atlas"); Submitted(p);
            }
        }

        [Test] public void ExistingSillStoneWallShadeRetainsItsOwnMaterialAndReceiverPolicy()
        {
            zone = Attach("Overworld.10.10.0"); Add("Grass"); wall = Add("StoneWall"); receiver = Add("Grass", X, Y + 1);
            Assert.True(FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone));
            using (var p = new Paint(world, zone))
            {
                Submitted(p);
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath), p.At(X, Y).sprite.texture);
                var actor = Add("Player", X, Y + 1); p.Dirty(X, Y + 1); p.Step(); Absent(p);
                Assert.True(zone.RemoveEntity(actor)); p.Dirty(X, Y + 1); Submitted(p); FullParity(p);
            }
        }

        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, main, ground;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public ZoneRenderer Renderer { get; }
            public EnvironmentSpriteRenderer Environment { get; }
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");
            public Tilemap Shade
            {
                get
                {
                    var layer = root.transform.Find("FlatGroundShadowTilemap");
                    Assert.NotNull(layer, "A separate FlatGroundShadowTilemap has not been implemented.");
                    return layer.GetComponent<Tilemap>();
                }
            }
            public Tilemap Decoration => (Tilemap)Read(Environment, "_flatGroundDecorationTilemap");
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Beating wall shade submitted grid", typeof(Grid));
                    root.GetComponent<Grid>().cellSize = new Vector3(1, 1.5f, 0); saved.Root.transform.SetParent(root.transform, false);
                    main = saved.Root.GetComponent<Tilemap>();
                    var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var marks = new GameObject("Hazards", typeof(Tilemap)); marks.transform.SetParent(root.transform, false);
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", ground);
                    Write(Renderer, "_tileStateTilemap", marks.GetComponent<Tilemap>());
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false);
                    camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    Environment = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); Environment.Init(root.transform, main, ground);
                    Environment.RenderingEnabled = true; Environment.FlatPixelGreyboxEnabled = true; Catalog(catalog);
                    overlay = (Tilemap)Read(Environment, "_overlayTilemap"); Write(Renderer, "_envSpriteRenderer", Environment);
                    Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public static Vector3Int Position(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
            public void Catalog(FlatPixelGreyboxCatalog value) => Write(Environment, "_flatPixelGreybox", value);
            public Tile At(int x, int y) => overlay.GetTile<Tile>(Position(x, y)) ?? main.GetTile<Tile>(Position(x, y));
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(Position(x, y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "wall shade receiver/source fixture");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            static Bounds TileBounds(Tilemap map, int x, int y)
            {
                var pos = Position(x, y); var tile = map.GetTile<Tile>(pos); Assert.NotNull(tile);
                var matrix = map.transform.localToWorldMatrix
                    * Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)pos + map.tileAnchor)) * map.GetTransformMatrix(pos);
                var low = matrix.MultiplyPoint3x4(tile.sprite.bounds.min); var high = matrix.MultiplyPoint3x4(tile.sprite.bounds.max);
                return new Bounds((low + high) * .5f, high - low);
            }
            public Bounds BodyBounds(int x, int y) => TileBounds(overlay.HasTile(Position(x, y)) ? overlay : main, x, y);
            public Bounds ShadeBounds(int x, int y) => TileBounds(Shade, x, y);
            public Bounds CellBounds(int x, int y)
            {
                var low = ground.CellToWorld(Position(x, y)); var high = ground.CellToWorld(Position(x, y) + new Vector3Int(1, 1, 0));
                return new Bounds((low + high) * .5f, high - low);
            }
            public void Dispose()
            {
                try { if (Environment != null) Catalog(null); saved?.Dispose(); if (root != null) Object.DestroyImmediate(root); }
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                }
            }
        }
    }
}
