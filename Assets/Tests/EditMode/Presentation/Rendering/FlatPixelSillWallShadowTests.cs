using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Public painter RED: no new production API is needed to compile this fixture.
    public sealed class FlatPixelSillWallShadowTests
    {
        const int X = 10, Y = 10;
        const string Sill = "Overworld.10.10.0";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity wall, receiver;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = Attach(Sill); world = new FlatPixelGreyboxCatalog();
            DestructionSystem.EntityFactoryRef = content.Factory;
            Assert.True(FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone));
        }
        [TearDown] public void Cleanup()
        { try { world?.Dispose(); } finally { content?.Dispose(); } }
        Zone Attach(string id)
        {
            var z = new Zone(id); manager.SetActiveZone(z);
            foreach (var cell in z.Cells) cell.Explored = cell.IsVisible = true;
            return z;
        }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, x, y)); return e; }
        void Scene(string floor = "Grass")
        { Add("Grass"); wall = Add("StoneWall"); receiver = Add(floor, X, Y + 1); }
        static object Read(object o, string name)
        { var f = o.GetType().GetField(name, I); Assert.NotNull(f, name); return f.GetValue(o); }
        static void Write(object o, string name, object value)
        { var f = o.GetType().GetField(name, I); Assert.NotNull(f, name); f.SetValue(o, value); }
        static bool Option(EnvironmentSpriteRenderer env, bool? value = null)
        {
            var option = typeof(EnvironmentSpriteRenderer).GetProperty("FlatWallShadowEnabled", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(option, "A reversible public wall-shadow option is required.");
            Assert.True(option.CanRead && option.CanWrite); Assert.AreEqual(typeof(bool), option.PropertyType);
            if (value.HasValue) option.SetValue(env, value.Value);
            return (bool)option.GetValue(env);
        }
        void Submitted(Paint p)
        {
            p.Step(); var shade = p.Shade;
            Assert.NotNull(shade.GetTile<Tile>(Paint.Position(X, Y)), "The approved StoneWall must submit a source-anchored shade.");
            Assert.AreSame(p.At(X, Y), shade.GetTile<Tile>(Paint.Position(X, Y)));
            Assert.IsNull(shade.GetTile(Paint.Position(X, Y + 1)), "The receiver is not a second shadow owner.");
            Assert.AreEqual(new Color(0, 0, 0, .16f), shade.GetColor(Paint.Position(X, Y)));
        }
        void Absent(Paint p)
        { Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y))); Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1))); }
        void FullParity(Paint p)
        {
            var tile = p.Shade.GetTile(Paint.Position(X, Y));
            var color = p.Shade.GetColor(Paint.Position(X, Y));
            var bounds = tile == null ? default : p.ShadeBounds(X, Y);
            p.Renderer.MarkDirty("wall shade parity control"); p.Step();
            Assert.AreSame(tile, p.Shade.GetTile(Paint.Position(X, Y)));
            if (tile != null) { Assert.AreEqual(color, p.Shade.GetColor(Paint.Position(X, Y))); Assert.AreEqual(bounds, p.ShadeBounds(X, Y)); }
            Assert.IsNull(p.Shade.GetTile(Paint.Position(X, Y + 1)));
        }

        [TestCase("Grass")] [TestCase("StoneFloor")]
        public void ActualPainterUsesSeparateUnlitLayerAndTwoPixelSouthOffsetWithoutChangingGeometry(string floor)
        {
            Scene(floor);
            using (var p = new Paint(world, zone))
            {
                int version = zone.EntityVersion, hp = wall.GetPart<DestructiblePart>().HP;
                string state = zone.TileState.ToSaveString(); Submitted(p); Assert.True(Option(p.Environment));
                var tile = p.Shade.GetTile<Tile>(Paint.Position(X, Y));
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.EnvironmentResourcePath), tile.sprite.texture);
                Assert.AreEqual(new Rect(128, 0, 16, 24), tile.sprite.rect);
                Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
                Assert.IsNull(p.Shade.GetComponent<TilemapCollider2D>());
                var renderer = p.Shade.GetComponent<TilemapRenderer>(); Assert.NotNull(renderer);
                Assert.AreEqual(0, renderer.sortingOrder); Assert.NotNull(renderer.sharedMaterial);
                Assert.That(renderer.sharedMaterial.shader.name, Does.Not.Contain("Lit-Default"));
                Assert.AreNotSame(p.Decoration, p.Shade, "Reserve-bed twine is a separate physical channel.");
                Assert.AreEqual(1, p.Decoration.GetComponent<TilemapRenderer>().sortingOrder);
                Bounds body = p.BodyBounds(X, Y), shade = p.ShadeBounds(X, Y);
                Assert.AreEqual(body.size, shade.size);
                Assert.That(Vector3.Distance(new Vector3(0, -2f / 16, 0), shade.center - body.center), Is.LessThan(.0001f));
                Assert.That(shade.min.y, Is.EqualTo(p.CellBounds(X, Y).min.y - 2f / 16).Within(.0001f));
                Assert.GreaterOrEqual(shade.min.y, p.CellBounds(X, Y + 1).min.y);
                Assert.That(shade.min.x, Is.EqualTo(body.min.x).Within(.0001f));
                Assert.That(shade.max.x, Is.EqualTo(body.max.x).Within(.0001f));
                Assert.AreSame(wall, zone.GetCell(X, Y).GetTopVisibleObject());
                Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y + 1).BlocksMovement());
                Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(hp, wall.GetPart<DestructiblePart>().HP);
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.IsNull(p.Decoration.GetTile(Paint.Position(X, Y)));
            }
        }

        [TestCase("memory")] [TestCase("unexplored")] [TestCase("render-hidden")] [TestCase("custom")]
        [TestCase("zero-hp")] [TestCase("removed")] [TestCase("covered")] [TestCase("foreign-render")]
        public void SourceMustRemainCurrentVisibleSelectedNativeStoneWall(string fault)
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p);
                if (fault == "memory") zone.GetCell(X, Y).IsVisible = false;
                if (fault == "unexplored") zone.GetCell(X, Y).Explored = false;
                if (fault == "render-hidden") wall.GetPart<RenderPart>().Visible = false;
                if (fault == "custom") wall.GetPart<RenderPart>().VisualVariant = "other-wall";
                if (fault == "zero-hp") wall.GetPart<DestructiblePart>().HP = 0;
                if (fault == "removed") Assert.True(zone.RemoveEntity(wall));
                if (fault == "covered") Add("Player");
                if (fault == "foreign-render") wall.GetPart<RenderPart>().ParentEntity = new Entity();
                p.Dirty(X, Y); p.Step(); Absent(p); FullParity(p);
            }
        }

        [TestCase("memory")] [TestCase("unexplored")] [TestCase("water")]
        [TestCase("oil")] [TestCase("prop")] [TestCase("item")] [TestCase("player")]
        public void SouthReceiverMustBeDisclosedDryAndClear(string fault)
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var sourceTile = p.At(X, Y); Entity obstruction = null;
                if (fault == "memory") zone.GetCell(X, Y + 1).IsVisible = false;
                if (fault == "unexplored") zone.GetCell(X, Y + 1).Explored = false;
                if (fault == "water") obstruction = Add("WaterPuddle", X, Y + 1);
                if (fault == "oil") zone.TileState.WriteCoating(X, Y + 1, "oil", 5);
                if (fault == "prop") obstruction = Add("MarketStall", X, Y + 1);
                if (fault == "item") obstruction = Add("Dagger", X, Y + 1);
                if (fault == "player") obstruction = Add("Player", X, Y + 1);
                p.Dirty(X, Y + 1); p.Step(); Absent(p); Assert.AreSame(sourceTile, p.At(X, Y)); FullParity(p);
                if (fault == "prop" || fault == "item")
                {
                    obstruction.GetPart<RenderPart>().Visible = false;
                    p.Dirty(X, Y + 1); Submitted(p);
                }
            }
        }

        [Test] public void SouthOnlyDirtyAddRemoveRepaintsNorthShadeAndMatchesFullPass()
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

        [Test] public void ActualWallDestructionClearsShadeWithoutManualNorthDirtyAndMatchesFullPass()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p);
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 999, null, zone));
                Assert.True(wall.GetPart<DestructiblePart>().Gone); Assert.IsNull(zone.GetEntityCell(wall));
                p.Step(); Absent(p); FullParity(p);
                Assert.AreSame(receiver, zone.GetCell(X, Y + 1).GetTopVisibleObject());
            }
        }

        [Test] public void ToggleClearsOnlyWallShadeImmediatelyAndSchedulesItsReturn()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); var body = p.At(X, Y); var floor = p.GroundAt(X, Y + 1);
                var decoration = p.Decoration; int version = zone.EntityVersion;
                Assert.False(Option(p.Environment, false)); Absent(p);
                Assert.AreSame(body, p.At(X, Y)); Assert.AreSame(floor, p.GroundAt(X, Y + 1));
                Assert.AreSame(decoration, p.Decoration); Assert.AreEqual(version, zone.EntityVersion);
                p.Step(); Absent(p); Assert.True(Option(p.Environment, true)); Submitted(p);
                Assert.AreSame(body, p.At(X, Y)); Assert.AreEqual(version, zone.EntityVersion);
            }
        }

        [TestCase("pause")] [TestCase("travel")] [TestCase("legacy")]
        public void PresentationExitClearsShadeAndReturnRestoresCurrentOwners(string exit)
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p);
                if (exit == "pause") p.Renderer.Paused = true;
                if (exit == "travel") p.Renderer.SetZone(Attach("Overworld.7.8.0"));
                if (exit == "legacy") p.Renderer.FlatPixelModeEnabled = false;
                p.Step(); Absent(p);
                if (exit == "pause") p.Renderer.Paused = false;
                if (exit == "travel") { manager.SetActiveZone(zone); p.Renderer.SetZone(zone); }
                if (exit == "legacy") p.Renderer.FlatPixelModeEnabled = true;
                Submitted(p); Assert.True(Option(p.Environment));
            }
        }

        [Test] public void CurrentSillAuthorityCannotBeBorrowedByRenamedOrReplacedGraphs()
        {
            Scene(); using (var p = new Paint(world, zone))
            {
                Submitted(p); manager.WorldMap.GetPOI(10, 10).Name = "Other town";
                p.Dirty(X, Y); p.Step(); Absent(p);
                manager.WorldMap.GetPOI(10, 10).Name = "Sill"; p.Dirty(X, Y); Submitted(p);
                manager.CachedZones[Sill] = new Zone(Sill); p.Dirty(X, Y); p.Step(); Absent(p);
            }
        }

        [Test] public void MissingNativeWallResourceRetainsFallbackBodyWithoutShade()
        {
            Scene(); using (var p = new Paint(world, zone))
            using (var unavailable = new FlatPixelGreyboxCatalog((Texture2D)null))
            {
                Submitted(p); p.Catalog(unavailable); p.Renderer.MarkDirty("missing native wall control"); p.Step();
                Absent(p); Assert.NotNull(p.At(X, Y));
                Assert.True(unavailable.TryResolveFallback(zone, X, Y, out var fallback)); Assert.AreSame(wall, fallback.Owner);
                Assert.AreSame(fallback.ForegroundTile, p.At(X, Y));
                p.Catalog(world); p.Renderer.MarkDirty("native wall restored"); Submitted(p);
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Sill wall shade submitted grid", typeof(Grid));
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
