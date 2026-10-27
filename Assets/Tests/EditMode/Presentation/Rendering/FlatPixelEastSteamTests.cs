using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // One optional tile cue, not a vent/actor identity or a new simulation.
    // Existing painter assertions supply behavioral RED before new API checks.
    public sealed class FlatPixelEastSteamTests
    {
        const string East = "Overworld.12.10.0";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Vector3Int At = new Vector3Int(X, Zone.Height - 1 - Y, 0);
        static readonly Color CloudColor = new Color(.8f, .8f, .85f);
        delegate bool Query(Zone zone, Cell cell, out Tile tile);
        readonly List<Action> restore = new List<Action>();
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;
        Texture2D atlas;

        [SetUp] public void Setup()
        {
            SnapshotStatics(typeof(LiquidRegistry)); SnapshotStatics(typeof(TileReactionSystem));
            SnapshotStatics(typeof(MessageLog)); SnapshotStatics(typeof(WorldAudioHooks));
            SnapshotStatics(typeof(WorldAudioUtility)); SnapshotStatics(typeof(EntityVisualHooks));
            foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" })
                Snapshot(typeof(Diag).GetField(name, S));
            content = new DensityLootTestScope();
            WorldAudioHooks.Callback = null; WorldAudioUtility.Callback = null; EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(a => a.text));
            var reactions = Resources.Load<TextAsset>("Content/Data/TileReactions/Reactions"); Assert.NotNull(reactions);
            TileReactionSystem.Initialize(reactions.text); Assert.True(TileReactionSystem.IsInitialized);
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = Attach(East); world = new FlatPixelGreyboxCatalog(); atlas = new Texture2D(16, 24);
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); content?.Dispose(); }
            finally { for (int n = restore.Count - 1; n >= 0; n--) restore[n](); restore.Clear(); }
        }
        Zone Attach(string id)
        {
            var z = new Zone(id); manager.SetActiveZone(z);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                z.GetCell(x, y).Explored = z.GetCell(x, y).IsVisible = true;
            return z;
        }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e); Assert.True(zone.AddEntity(e, x, y)); return e; }
        void Steam(int turns = 3) => zone.TileState.WriteCloud(X, Y, "steam", turns);
        static Type ArtType()
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelEastSteamCatalog");
            Assert.NotNull(type, "No optional native East steam tile cue exists yet."); return type;
        }
        static IDisposable Create(Texture2D texture)
        { return (IDisposable)Activator.CreateInstance(ArtType(), new object[] { texture }); }
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            ArtType().GetMethod("TryResolve", new[] { typeof(Zone), typeof(Cell), typeof(Tile).MakeByRefType() }));
        static void Put(object target, string field, object value)
        { var f = target.GetType().GetField(field, I); Assert.NotNull(f, field); f.SetValue(target, value); }
        static object Call(object target, string method, params object[] args)
        {
            try { return target.GetType().GetMethod(method, I).Invoke(target, args); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        static void Frame(Tile tile, Texture2D texture)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(TileFlags.None, tile.flags);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        // Old production can still paint its observable glyph before a missing-API assertion.
        // Once present, the optional seam gets a borrowed fixture texture, independent of asset import.
        IDisposable OptionalArt() => typeof(ZoneRenderer).Assembly.GetType(
            "CavesOfOoo.Rendering.FlatPixelEastSteamCatalog") == null ? null : Create(atlas);
        static void Native(Paint paint, Texture2D texture)
        {
            Frame(paint.Mark as Tile, texture); Assert.AreEqual(Color.white, paint.MarkTint);
            Assert.AreEqual(Matrix4x4.identity, paint.MarkTransform);
        }

        [Test]
        public void ActualVentReactionKeepsTheApprovedGrateAndUsesNativeLiveSteam()
        {
            var vent = Add("SteamVent"); var actor = Add("Player", X - 1, Y);
            int hp = actor.GetStatValue("Hitpoints"); float temperature = vent.GetPart<ThermalPart>().Temperature;
            ZoneTileStateSystem.OnPlayerTurnEnd(zone);
            var state = zone.TileState.Get(X, Y); Assert.NotNull(state);
            Assert.AreEqual("steam", state.Cloud); Assert.AreEqual(1, state.CloudTurns);
            Assert.Zero(state.Coatings.Count); Assert.Zero(state.Heat); Assert.False(zone.TileState.ObscuresSight(X, Y));
            using (var art = OptionalArt()) using (var paint = new Paint(world, zone))
            {
                if (art != null) Put(paint.Renderer, "_eastSteamArt", art);
                paint.Step(); Assert.NotNull(paint.Mark);
                Assert.AreNotSame(CP437TilesetGenerator.GetTile('\''), paint.Mark,
                    "The actual live steam over the native vent still uses a legacy apostrophe.");
                Native(paint, atlas);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var v)); Assert.AreSame(vent, v.Owner);
                Assert.AreEqual(new Rect(16 * 16, 0, 16, 24), v.ForegroundTile.sprite.rect);
                Assert.AreSame(v.ForegroundTile, paint.Submitted);
                string before = zone.TileState.ToSaveString(); var mark = paint.Mark;
                for (int i = 0; i < 4; i++) { paint.Direct(); Assert.AreSame(mark, paint.Mark); }
                Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
                Assert.AreEqual(temperature, vent.GetPart<ThermalPart>().Temperature);
            }
        }

        [Test]
        public void IndependentSteamOverNativeWaterUsesTheSameCueWithoutAnUnderlyingVent()
        {
            Assert.Zero(zone.EntityCount); zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent); Steam(1);
            using (var art = OptionalArt()) using (var paint = new Paint(world, zone))
            {
                if (art != null) Put(paint.Renderer, "_eastSteamArt", art);
                paint.Step(); Assert.NotNull(paint.Mark);
                Assert.AreNotSame(CP437TilesetGenerator.GetTile('\''), paint.Mark,
                    "Represented water must still expose the current steam cue.");
                Native(paint, atlas); Assert.Zero(zone.EntityCount);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var view)); Assert.True(view.LocalGround);
                Assert.AreEqual(FlatPixelGreyboxGround.Water, view.GroundKind);
            }
        }

        [Test]
        public void ColdVentBodyWithoutCloudDoesNotInventSteamOrChangeItsAcceptedIdentity()
        {
            var vent = Add("SteamVent"); vent.GetPart<ThermalPart>().Temperature = -50;
            zone.TileState.Clear(X, Y); // Explicit staged absence, not a claim that cooling disables the source.
            using (var paint = new Paint(world, zone))
            {
                paint.Direct(); Assert.IsNull(paint.Mark);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var view)); Assert.AreSame(vent, view.Owner);
                Assert.True(view.NativeForeground); Assert.AreEqual(new Rect(256, 0, 16, 24), view.ForegroundTile.sprite.rect);
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("foreign-cell")]
        [TestCase("stale-graph")] [TestCase("unmanaged")] [TestCase("foreign-poi")]
        [TestCase("foreign-biome")] [TestCase("null-cell")]
        public void ExactCanonicalCurrentVisibleEastStateIsRequired(string fault)
        {
            Steam(); using (var art = Create(atlas))
            {
                var q = Resolver(art); Assert.True(q(zone, zone.GetCell(X, Y), out var tile)); Frame(tile, atlas);
                var target = zone; var cell = zone.GetCell(X, Y);
                if (fault == "hidden") cell.IsVisible = false;
                else if (fault == "unexplored") cell.Explored = false;
                else if (fault == "foreign-cell") cell = new Zone(East).GetCell(X, Y);
                else if (fault == "stale-graph") manager.SetActiveZone(new Zone(East));
                else if (fault == "unmanaged") { target = new Zone(East); target.TileState.WriteCloud(X, Y, "steam", 3); cell = target.GetCell(X, Y); cell.IsVisible = cell.Explored = true; }
                else if (fault == "foreign-poi") manager.WorldMap.SetPOI(12, 10, new PointOfInterest(POIType.Village, "foreign", null, 1));
                else if (fault == "foreign-biome") manager.WorldMap.Tiles[12, 10] = BiomeType.Sodden;
                else cell = null;
                Assert.False(q(target, cell, out var rejected)); Assert.IsNull(rejected);
            }
        }

        [TestCase("Overworld.11.10.0")] [TestCase("Overworld.11.9.0")]
        [TestCase("Overworld.12.11.0")] [TestCase("Overworld.12.10.1")]
        public void OtherGraphsKeepTheirExistingCloudGlyph(string address)
        {
            zone = Attach(address); Steam(); using (var art = Create(atlas))
            using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art);
                Assert.False(Resolver(art)(zone, zone.GetCell(X, Y), out _));
                paint.Direct(); Assert.AreSame(CP437TilesetGenerator.GetTile('\''), paint.Mark); Assert.AreEqual(CloudColor, paint.MarkTint);
            }
        }

        [TestCase("smoke", 3)] [TestCase("unknown", 3)] [TestCase("Steam", 3)]
        [TestCase("steam", 0)] [TestCase("steam", -1)]
        public void OnlyExactLiveSteamReceivesNewArtWhileOtherCloudFallbackStaysTruthful(string cloud, int turns)
        {
            Steam(); using (var art = Create(atlas)) using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art); paint.Direct(); Native(paint, atlas);
                var state = zone.TileState.Get(X, Y); state.Cloud = cloud; state.CloudTurns = turns;
                Assert.False(Resolver(art)(zone, zone.GetCell(X, Y), out _)); paint.Direct();
                Assert.AreSame(CP437TilesetGenerator.GetTile('\''), paint.Mark, "Retain the existing malformed/unknown fallback contract.");
                Assert.AreEqual(CloudColor, paint.MarkTint); Assert.AreEqual(Matrix4x4.identity, paint.MarkTransform);
                zone.TileState.Clear(X, Y); paint.Direct(); Assert.IsNull(paint.Mark);
            }
        }

        [TestCase("veil-mist", '\'')] [TestCase("residue", '"')] [TestCase("oil", '~')]
        [TestCase("ice", '~')] [TestCase("charge", '*')] [TestCase("heat", '^')] [TestCase("cold", '^')]
        public void ExistingHigherPriorityMarksWinAndSurvivingSteamReturns(string hazard, char glyph)
        {
            Steam(5); using (var art = Create(atlas)) using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art); paint.Direct(); Native(paint, atlas);
                if (hazard == "veil-mist") zone.TileState.WriteCloud(X, Y, hazard, 3);
                else if (hazard == "residue") zone.TileState.WriteResidue(X, Y, "embers", 3);
                else if (hazard == "oil" || hazard == "ice") zone.TileState.WriteCoating(X, Y, hazard, 3);
                else if (hazard == "charge") zone.TileState.AddCharge(X, Y, 2);
                else if (hazard == "heat") zone.TileState.AddHeat(X, Y, 2);
                else zone.TileState.AddCold(X, Y, 2);
                paint.Direct(); Assert.AreSame(CP437TilesetGenerator.GetTile(glyph), paint.Mark);
                if (hazard == "veil-mist") Steam(5); // Cloud replacement, not simultaneous clouds.
                else { for (int n = 0; n < 3; n++) zone.TileState.Tick(); }
                paint.Direct(); Native(paint, atlas);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void NativeCueDoesNotLeakThroughFogAndModeLossKeepsLegacyMark(bool spriteLoss)
        {
            Steam(); using (var art = Create(atlas)) using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art); paint.Direct(); Native(paint, atlas);
                zone.GetCell(X, Y).IsVisible = false; paint.Direct(); Assert.IsNull(paint.Mark);
                zone.GetCell(X, Y).IsVisible = true; paint.Direct(); Native(paint, atlas);
                if (spriteLoss) paint.Environment.RenderingEnabled = false; else Put(paint.Renderer, "_flatPixelModeEnabled", false);
                paint.Direct(); Assert.AreSame(CP437TilesetGenerator.GetTile('\''), paint.Mark);
                Assert.AreEqual(CloudColor, paint.MarkTint); Assert.AreEqual(Matrix4x4.identity, paint.MarkTransform);
                if (spriteLoss) paint.Environment.RenderingEnabled = true; else Put(paint.Renderer, "_flatPixelModeEnabled", true);
                paint.Direct(); Native(paint, atlas);
            }
        }

        [TestCase(0, 0)] [TestCase(16, 23)] [TestCase(32, 24)]
        public void MissingOrMalformedOptionalTexturePreservesGlyphAndBorrowedResources(int width, int height)
        {
            var bad = width == 0 ? null : new Texture2D(width, height);
            try
            {
                Steam(); using (var art = Create(bad)) using (var paint = new Paint(world, zone))
                {
                    Put(paint.Renderer, "_eastSteamArt", art); Assert.False(Resolver(art)(zone, zone.GetCell(X, Y), out _));
                    paint.Direct(); Assert.AreSame(CP437TilesetGenerator.GetTile('\''), paint.Mark);
                    Assert.AreEqual(CloudColor, paint.MarkTint);
                }
                if (width != 0) Assert.True(bad != null, "The borrowed texture must survive disposal.");
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test]
        public void CachedNativeObjectsAreIndependentAndRendererCleanupNeverDestroysTheTexture()
        {
            Steam(); using (var peer = Create(atlas))
            {
                var owned = Create(atlas); var q = Resolver(owned); Assert.True(q(zone, zone.GetCell(X, Y), out var tile));
                var sprite = tile.sprite; Assert.True(Resolver(peer)(zone, zone.GetCell(X, Y), out var other)); Assert.AreNotSame(tile, other);
                using (var paint = new Paint(world, zone))
                {
                    Put(paint.Renderer, "_eastSteamArt", owned);
                    string before = zone.TileState.ToSaveString();
                    for (int n = 0; n < 10; n++) { paint.Direct(); Assert.AreSame(tile, paint.Mark); }
                    Assert.AreEqual(before, zone.TileState.ToSaveString());
                    Assert.AreEqual(FilterMode.Bilinear, atlas.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, atlas.wrapMode);
                }
                Assert.True(tile == null); Assert.True(sprite == null); Assert.True(atlas != null);
                owned.Dispose(); Assert.False(q(zone, zone.GetCell(X, Y), out _));
                Assert.True(Resolver(peer)(zone, zone.GetCell(X, Y), out var alive)); Assert.AreSame(other, alive);
            }
        }

        [Test]
        public void ActorCoverKeepsExactVentReaderAndTileCueWithoutReplacingSelectedOwner()
        {
            var vent = Add("SteamVent"); var actor = Add("Player", X - 1, Y); Steam();
            using (var art = Create(atlas)) using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art); paint.Step(); Native(paint, atlas);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var empty)); Assert.AreSame(vent, empty.Owner);
                var doc = ExamineDocumentBuilder.BuildWorld(vent, zone, zone.GetCell(X, Y), actor); Assert.NotNull(doc);
                Assert.True(zone.MoveEntity(actor, X, Y)); ZoneRenderHooks.MarkCellDirty(X, Y, "Test.ActorCover"); paint.Step();
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var covered)); Assert.AreSame(actor, covered.Owner);
                Assert.AreSame(covered.ForegroundTile, paint.Submitted); Native(paint, atlas);
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(doc, out var portrait));
                Assert.AreSame(empty.ForegroundTile.sprite.texture, portrait.Texture);
                Assert.AreEqual(new Rect(16f / 18, 0, 1f / 18, 1), portrait.Uv);
                Assert.True(zone.MoveEntity(actor, X - 1, Y)); ZoneRenderHooks.MarkCellDirty(X, Y, "Test.ActorUncover"); paint.Step();
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var back)); Assert.AreSame(vent, back.Owner);
                Assert.AreSame(empty.ForegroundTile, paint.Submitted); Native(paint, atlas);
            }
        }

        [Test]
        public void ReplacedGraphClearsPreviouslySubmittedNativeMarkThroughActualRenderPath()
        {
            Steam(); using (var art = Create(atlas)) using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art); paint.Step(); Native(paint, atlas);
                manager.SetActiveZone(new Zone(East));
                paint.Renderer.MarkDirty("Test.ReplacedGraph"); paint.Step();
                Assert.IsNull(paint.Mark, "The obsolete graph cannot retain its prior native disclosure.");
                Assert.False(Resolver(art)(zone, zone.GetCell(X, Y), out _));
            }
        }

        [Test]
        public void ActualWriteAndExpiryHooksRepaintTheMarkWithoutAnotherFullPassOrInactiveLeak()
        {
            using (var art = Create(atlas)) using (var paint = new Paint(world, zone))
            {
                Put(paint.Renderer, "_eastSteamArt", art); paint.Step(); Assert.IsNull(paint.Mark);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Steam(2); paint.Step(); Native(paint, atlas); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                zone.TileState.Tick(); paint.Step(); Native(paint, atlas);
                zone.TileState.Tick(); paint.Step(); Assert.IsNull(paint.Mark); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                var old = zone; var current = Attach("Overworld.12.11.0"); paint.Renderer.SetZone(current); paint.Step();
                var dirty = (HashSet<int>)typeof(ZoneRenderer).GetField("_dirtyCells", I).GetValue(paint.Renderer); Assert.Zero(dirty.Count);
                old.TileState.WriteCloud(X, Y, "steam", 2); Assert.Zero(dirty.Count, "Inactive cached state must not dirty the active graph.");
                current.TileState.WriteCloud(X, Y, "steam", 2); Assert.Greater(dirty.Count, 0); paint.Step();
                Assert.AreSame(CP437TilesetGenerator.GetTile('\''), paint.Mark, "Kitchen keeps its existing mark.");
                Assert.True(Resolver(art)(old, old.GetCell(X, Y), out _), "Cached current graph authority is separate from active render hooks.");
                manager.SetActiveZone(old); paint.Renderer.SetZone(old); paint.Step(); Native(paint, atlas);
            }
        }

        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved; readonly GameObject root; readonly Tilemap overlay, marks;
            public ZoneRenderer Renderer { get; }
            public EnvironmentSpriteRenderer Environment { get; }
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone target)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var f in typeof(EnvironmentSpriteRenderer.Perf).GetFields(S)) if (!f.IsLiteral) perf[f] = f.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Owned steam submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false);
                    var mark = new GameObject("State marks", typeof(Tilemap)); mark.transform.SetParent(root.transform, false); marks = mark.GetComponent<Tilemap>();
                    Renderer = saved.Renderer; Put(Renderer, "_tilemap", main); Put(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>()); Put(Renderer, "_tileStateTilemap", marks);
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Put(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    Environment = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); Environment.Init(root.transform, main, bg.GetComponent<Tilemap>());
                    Environment.RenderingEnabled = true; Environment.FlatPixelGreyboxEnabled = true; Put(Environment, "_flatPixelGreybox", catalog);
                    overlay = (Tilemap)typeof(EnvironmentSpriteRenderer).GetField("_overlayTilemap", I).GetValue(Environment);
                    Put(Renderer, "_envSpriteRenderer", Environment); Put(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(target);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public Tile Submitted => overlay.GetTile<Tile>(At);
            public TileBase Mark => marks.GetTile(At);
            public Color MarkTint => marks.GetColor(At);
            public Matrix4x4 MarkTransform => marks.GetTransformMatrix(At);
            public void Direct() => Call(Renderer, "PaintTileStateMark", X, Y, At, Renderer.CurrentZone.GetCell(X, Y));
            public void Step() => Call(Renderer, "LateUpdate");
            public void Dispose()
            {
                try
                {
                    // This never-active EditMode fixture invokes the saved
                    // lifecycle method explicitly. It proves cleanup behavior,
                    // not Unity's automatic OnDestroy callback dispatch.
                    if (Renderer != null) Call(Renderer, "OnDestroy");
                    saved?.Dispose(); if (root != null) Object.DestroyImmediate(root);
                }
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var pair in perf) pair.Key.SetValue(null, pair.Value);
                }
            }
        }
        void SnapshotStatics(Type type) { foreach (var f in type.GetFields(S)) if (!f.IsLiteral) Snapshot(f); }
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array) { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary)
            {
                var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry item in dictionary) copy.Add(item);
                restore.Add(() => { dictionary.Clear(); foreach (var item in copy) dictionary.Add(item.Key, item.Value); });
            }
            else if (value is IList list)
            { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
