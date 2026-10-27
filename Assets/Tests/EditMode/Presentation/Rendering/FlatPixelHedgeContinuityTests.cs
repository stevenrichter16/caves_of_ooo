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
    /// <summary>Connected art must describe real hedges without changing their
    /// gaps, collision or disclosure. Actual kitchen capture identified the
    /// repeated scalloped silhouette; production routing is tested separately
    /// from the optional adapter and the already submitted Tilemap.</summary>
    public sealed class FlatPixelHedgeContinuityTests
    {
        const string Kitchen = "Overworld.12.11.0", Resource = "Sprites/Live16x24/hedge-continuity";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Zone zone, int x, int y, out Tile tile);
        delegate bool Portrait(Entity owner, out Sprite sprite);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelFallbackCatalog selector;
        FlatPixelGreyboxCatalog world;
        Texture2D atlas;
        IDisposable adapter;
        Query query;
        Portrait portrait;
        Zone zone;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            selector = new FlatPixelFallbackCatalog(); atlas = new Texture2D(256, 24); zone = Attach(Kitchen);
        }
        [TearDown] public void Cleanup()
        { world?.Dispose(); adapter?.Dispose(); selector?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); content?.Dispose(); }
        Zone Attach(string id)
        {
            var result = new Zone(id); manager.SetActiveZone(result);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                result.GetCell(x, y).Explored = result.GetCell(x, y).IsVisible = true;
            return result;
        }
        Entity Add(string blueprint = "Hedge", int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        void Create(Texture2D source, bool injectWorld = false)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelHedgeCatalog");
            Assert.NotNull(type, "No disclosed-owner native hedge continuity adapter exists.");
            var ctor = type.GetConstructor(new[] { typeof(Texture2D), typeof(FlatPixelFallbackCatalog) }); Assert.NotNull(ctor);
            adapter = (IDisposable)ctor.Invoke(new object[] { source, selector });
            query = (Query)Delegate.CreateDelegate(typeof(Query), adapter, type.GetMethod("TryResolve", I));
            portrait = (Portrait)Delegate.CreateDelegate(typeof(Portrait), adapter, type.GetMethod("TryGetSubjectSprite", I));
            if (!injectWorld) return;
            world = new FlatPixelGreyboxCatalog(); Write(world, "_hedgeArt", adapter);
        }
        static void Write(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(owner, value); }
        static object Read(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
        static Rect Rect(int mask) => new Rect(mask * 16, 0, 16, 24);
        void Frame(Tile tile, int mask)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(atlas, tile.sprite.texture);
            Assert.AreEqual(Rect(mask), tile.sprite.rect); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(TileFlags.None, tile.flags);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        Tile Expect(int mask)
        { Assert.True(query(zone, X, Y, out var tile)); Frame(tile, mask); return tile; }
        FlatPixelGreyboxCell Visible()
        { Assert.True(world.TryResolvePresentation(zone, X, Y, out var value)); return value; }
        void Neighbors(int mask)
        {
            if ((mask & 1) != 0) Add(x: X, y: Y - 1);
            if ((mask & 2) != 0) Add(x: X + 1, y: Y);
            if ((mask & 4) != 0) Add(x: X, y: Y + 1);
            if ((mask & 8) != 0) Add(x: X - 1, y: Y);
        }

        [TestCase("Overworld.11.10.0")] [TestCase(GleanersCellarBuilder.ZoneID)]
        [TestCase("Overworld.11.9.0")] [TestCase("Overworld.11.8.0")]
        [TestCase("Overworld.12.10.0")] [TestCase(Kitchen)]
        public void DefaultWorldActuallyUsesConnectedHedgeArtInsideExistingAdmission(string id)
        {
            zone = Attach(id); var center = Add(); Neighbors(10); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone)); var value = Visible();
            Assert.AreSame(center, value.Owner); Assert.True(value.NativeForeground);
            Assert.AreEqual(Rect(10), value.ForegroundTile.sprite.rect, "A horizontal run still repeats the isolated hedge frame.");
            var actual = Resources.Load<Texture2D>(Resource); Assert.NotNull(actual);
            Assert.AreSame(actual, value.ForegroundTile.sprite.texture); Assert.AreEqual(256, actual.width); Assert.AreEqual(24, actual.height);
            Assert.AreEqual(FilterMode.Point, actual.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, actual.wrapMode); Assert.AreEqual(1, actual.mipmapCount);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, center, X, Y, out var sprite, out var tint));
            Assert.AreSame(actual, sprite.texture); Assert.AreEqual(Rect(0), sprite.rect); Assert.AreEqual(Color.white, tint);
            Assert.True(value.BlocksMovement); Assert.False(center.HasTag("Wall")); Assert.False(center.HasTag("Terrain"));
        }

        [Test] public void AllSixteenCardinalShapesStayCachedAndIgnoreDiagonalsAndDistantHedges()
        {
            Create(atlas); var seen = new HashSet<Tile>();
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(Kitchen); var center = Add(); Neighbors(mask);
                Add(x: X - 1, y: Y - 1); Add(x: X + 1, y: Y + 1); Add(x: X + 2, y: Y);
                var tile = Expect(mask); Assert.True(seen.Add(tile)); Assert.AreSame(tile, Expect(mask));
                Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y).IsWall());
                Assert.AreEqual(10, center.GetPart<DestructiblePart>().HP);
                foreach (var at in new[] { (X, Y - 1), (X + 1, Y), (X, Y + 1), (X - 1, Y) })
                    foreach (var owner in zone.GetCell(at.Item1, at.Item2).Objects.ToArray()) zone.RemoveEntity(owner);
                Expect(0);
            }
        }

        [TestCase("Tree")] [TestCase("Bush")] [TestCase("BerryBush")] [TestCase("StoneWall")]
        public void SimilarObjectsNeverSupplyHedgeConnections(string blueprint)
        {
            Create(atlas); Add(); var east = Add(x: X + 1); Expect(2); Assert.True(zone.RemoveEntity(east));
            Add(blueprint, X + 1); Expect(0); Assert.False(query(zone, X + 1, Y, out _));
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("covered")]
        public void OnlyDisclosedSelectedNeighborsContributeAndRevealRestoresThem(string state)
        {
            Create(atlas); Add(); Add(x: X + 1); Expect(2); var east = zone.GetCell(X + 1, Y);
            Entity cover = null;
            if (state == "hidden") east.IsVisible = false;
            if (state == "unexplored") east.Explored = false;
            if (state == "covered") { cover = Add("Dagger", X + 1); cover.GetPart<RenderPart>().RenderLayer = 1; }
            Expect(0);
            east.Explored = east.IsVisible = true; if (cover != null) Assert.True(zone.RemoveEntity(cover)); Expect(2);
            zone.GetCell(X, Y).IsVisible = false; Assert.False(query(zone, X, Y, out _), "Hedge does not acquire masonry memory.");
            zone.GetCell(X, Y).IsVisible = true; Expect(2);
        }

        static readonly string[] ArtFaults = { "visual-id", "visual-variant", "glyph-variants", "glyph", "foreign-render", "identity", "vegetation" };
        static readonly string[] StructuralFaults = { "foreign-destructible", "missing-destructible", "gone", "zero-hp", "non-solid", "foreign-physics", "missing-physics" };
        static readonly string[] CustodyFaults = { "carried", "equipped", "takeable", "creature", "item", "player", "footprint", "zero-stack" };
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void InvalidCenterAndNeighborCannotBorrowTheLiveHedgeRole(int group)
        {
            Create(atlas);
            foreach (string fault in group == 0 ? ArtFaults : group == 1 ? StructuralFaults : CustodyFaults)
            {
                zone = Attach(Kitchen); var center = Add(); var east = Add(x: X + 1); Expect(2);
                Fault(east, fault); Expect(0); Fault(center, fault);
                Assert.False(query(zone, X, Y, out _), fault); Assert.False(portrait(center, out _), fault);
            }
        }

        [TestCase("visual-variant")] [TestCase("glyph-variants")] [TestCase("missing-destructible")]
        [TestCase("foreign-destructible")] [TestCase("gone")] [TestCase("zero-hp")]
        public void InvalidKnownHedgeCannotEscapeThroughOlderNativeVegetation(string fault)
        {
            Create(atlas, true); var center = Add(); Add(x: X + 1); Frame(Visible().ForegroundTile, 2);
            Fault(center, fault); Assert.True(world.TryResolveFallback(zone, X, Y, out var expected));
            Assert.AreSame(center, expected.Owner); Assert.NotNull(expected.ForegroundTile);
            var value = Visible(); Assert.AreSame(expected.ForegroundTile, value.ForegroundTile); Assert.False(value.NativeForeground);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, center, X, Y, out var sprite, out _));
            Assert.AreSame(expected.ForegroundTile.sprite, sprite, "Reader must not bypass the same semantic rejection.");
        }

        [Test] public void NonlethalStructuralDamageKeepsLiveConnectionsAndTheIsolatedPortrait()
        {
            Create(atlas, true); var center = Add(); var east = Add(x: X + 1);
            var tile = Expect(2); Assert.True(portrait(center, out var sprite));
            Assert.AreEqual(DestroyVerdict.Damaged, DestructionSystem.Damage(center, 3, null, zone));
            Assert.AreEqual(DestroyVerdict.Damaged, DestructionSystem.Damage(east, 9, null, zone));
            Assert.AreEqual(7, center.GetPart<DestructiblePart>().HP); Assert.AreEqual(1, east.GetPart<DestructiblePart>().HP);
            Assert.AreSame(tile, Expect(2)); Assert.AreSame(tile, Visible().ForegroundTile);
            Assert.True(portrait(center, out var unchanged)); Assert.AreSame(sprite, unchanged);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, center, X, Y, out var reader, out _)); Assert.AreSame(sprite, reader);
            Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.True(zone.GetCell(X + 1, Y).BlocksMovement());
        }

        [Test] public void PortraitStaysIsolatedAndWarmQueriesDoNotAllocateOrChangeSimulation()
        {
            Create(atlas); var center = Add(); Neighbors(15); Expect(15);
            Assert.True(portrait(center, out var sprite)); Assert.AreEqual(Rect(0), sprite.rect);
            int version = zone.EntityVersion, hp = center.GetPart<DestructiblePart>().HP;
            for (int i = 0; i < 256; i++) query(zone, X, Y, out _);
            long before = GC.GetAllocatedBytesForCurrentThread(); bool all = true;
            for (int i = 0; i < 1024; i++) all &= query(zone, X, Y, out _);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(all); Assert.Zero(allocated); Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(hp, center.GetPart<DestructiblePart>().HP); Assert.True(center.GetPart<PhysicsPart>().Solid);
            foreach (var at in new[] { (X, Y - 1), (X + 1, Y), (X, Y + 1), (X - 1, Y) }) zone.GetCell(at.Item1, at.Item2).IsVisible = false;
            Expect(0); Assert.True(portrait(center, out var unchanged)); Assert.AreSame(sprite, unchanged);
        }

        [Test] public void StaleMembersDuplicateIdsCurrentGraphAndRegionalAuthorityStayStrict()
        {
            Create(atlas); Add(); var east = Add(x: X + 1); Expect(2); var old = zone.GetCell(X + 1, Y);
            Assert.True(zone.MoveEntity(east, X + 2, Y)); old.Objects.Add(east); Expect(0);
            var phantom = content.Factory.CreateEntity("Hedge"); phantom.ID = east.ID; old.Objects.Add(phantom); Expect(0);
            manager.CachedZones[Kitchen] = new Zone(Kitchen); Assert.False(query(zone, X, Y, out _));
            manager.CachedZones[Kitchen] = zone; Expect(0);
            manager.WorldMap.SetPOI(12, 11, new PointOfInterest(POIType.Village, "hedge counter", null, 1));
            Assert.False(query(zone, X, Y, out _)); manager.WorldMap.SetPOI(12, 11, null); Expect(0);
            manager.WorldMap.Tiles[12, 11] = BiomeType.Sodden; Assert.False(query(zone, X, Y, out _));
            manager.WorldMap.Tiles[12, 11] = BiomeType.Spread; Expect(0);
            zone = Attach("Overworld.12.12.0"); Add(); Neighbors(10); Assert.False(query(zone, X, Y, out _));
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedOptionalGroupRetainsOldArtAndBorrowedResources(bool malformed)
        {
            var bad = malformed ? new Texture2D(255, 24) : null;
            try
            {
                Create(bad, true); var center = Add(); Add(x: X + 1);
                Assert.False(query(zone, X, Y, out _)); var value = Visible(); Assert.True(value.NativeForeground);
                var old = Resources.Load<Texture2D>("Sprites/Live16x24/north-vegetation"); Assert.NotNull(old);
                Assert.AreSame(old, value.ForegroundTile.sprite.texture); Assert.AreEqual(Rect(1), value.ForegroundTile.sprite.rect);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, center, X, Y, out var sprite, out _)); Assert.AreSame(old, sprite.texture);
                center.GetPart<DestructiblePart>().Gone = true;
                Assert.True(world.TryResolveFallback(zone, X, Y, out var expected));
                value = Visible(); Assert.False(value.NativeForeground); Assert.AreSame(expected.ForegroundTile, value.ForegroundTile);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, center, X, Y, out sprite, out _)); Assert.AreSame(expected.ForegroundTile.sprite, sprite);
                adapter.Dispose(); Assert.True(selector.TryResolve(zone, X, Y, out _)); if (bad != null) Assert.True(bad != null);
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test] public void DisposalAndOldInjectedConstructorDoNotOwnOrLoadOptionalPixels()
        {
            Create(atlas); Add(); var tile = Expect(0); var sprite = tile.sprite;
            adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.True(atlas != null);
            Assert.True(selector.TryResolve(zone, X, Y, out _)); Assert.False(query(zone, X, Y, out _));
            world = new FlatPixelGreyboxCatalog((Texture2D)null); Assert.False(Visible().NativeForeground);
        }

        [Test] public void ActualDestructionClearsTheGapAndCapsBothSubmittedNeighborsIncrementally()
        {
            Create(atlas, true); Add(x: X - 1); var center = Add(); Add(x: X + 1);
            using (var paint = new Paint(world, zone))
            {
                Frame(paint.At(X - 1, Y), 2); Frame(paint.At(X, Y), 10); Frame(paint.At(X + 1, Y), 8);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(center, 999, null, zone));
                Assert.True(center.GetPart<DestructiblePart>().Gone); Assert.IsNull(zone.GetEntityCell(center));
                Assert.False(paint.FullDirty); paint.Step();
                Frame(paint.At(X - 1, Y), 0); Assert.IsNull(paint.At(X, Y)); Frame(paint.At(X + 1, Y), 0);
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step(); Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        [Test] public void DisclosureAndForegroundChangesRepaintExistingConnectionsWithoutFullRedraw()
        {
            Create(atlas, true); Add(); Add(x: X + 1);
            using (var paint = new Paint(world, zone))
            {
                Frame(paint.At(X, Y), 2); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                zone.GetCell(X + 1, Y).IsVisible = false; paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), 0);
                zone.GetCell(X + 1, Y).IsVisible = true; paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), 2);
                var cover = Add("Dagger", X + 1); paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), 0);
                Assert.True(zone.RemoveEntity(cover)); paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), 2);
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step(); Assert.IsNull(paint.At(X, Y));
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }

        [Test] public void SillVineRemovalCapsSubmittedNeighborsThroughTheGlobalDirtyRadius()
        {
            zone = Attach("Overworld.10.10.0");
            for (int x = X - 1; x <= X + 1; x++) Add("Grass", x, Y);
            Add("VineWall", X - 1); var center = Add("VineWall"); Add("VineWall", X + 1);
            world = new FlatPixelGreyboxCatalog();
            var source = Resources.Load<Texture2D>(FlatPixelSillEnvelopeCatalog.ResourcePath); Assert.NotNull(source);
            void SillFrame(Tile tile, int mask)
            {
                Assert.NotNull(tile); Assert.AreSame(source, tile.sprite.texture);
                Assert.AreEqual(new Rect(mask * 16, 0, 16, 24), tile.sprite.rect);
            }
            using (var paint = new Paint(world, zone))
            {
                SillFrame(paint.At(X - 1, Y), 2); SillFrame(paint.At(X, Y), 10); SillFrame(paint.At(X + 1, Y), 8);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(center, 999, null, zone));
                Assert.IsNull(zone.GetEntityCell(center)); Assert.False(paint.FullDirty); paint.Step();
                SillFrame(paint.At(X - 1, Y), 0); SillFrame(paint.At(X + 1, Y), 0);
                var gap = paint.At(X, Y);
                // VineWall may leave real Rubble; require no stale intact vine, not an empty world cell.
                Assert.True(gap == null || gap.sprite.texture != source);
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step();
                Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        static void Fault(Entity owner, string fault)
        {
            var r = owner.GetPart<RenderPart>(); var p = owner.GetPart<PhysicsPart>(); var d = owner.GetPart<DestructiblePart>(); var foreign = new Entity();
            switch (fault)
            {
                case "visual-id": r.VisualID = "custom-hedge"; break;
                case "visual-variant": r.VisualVariant = "custom-hedge"; break;
                case "glyph-variants": r.GlyphVariants = "#%"; break;
                case "glyph": r.RenderString = "?"; break;
                case "foreign-render": r.ParentEntity = foreign; break;
                case "identity": owner.BlueprintName = "UnrelatedBoundary"; break;
                case "vegetation": owner.Tags.Remove("Vegetation"); break;
                case "foreign-destructible": d.ParentEntity = foreign; break;
                case "missing-destructible": owner.RemovePart(d); break;
                case "gone": d.Gone = true; break;
                case "zero-hp": d.HP = 0; break;
                case "non-solid": p.Solid = false; break;
                case "foreign-physics": p.ParentEntity = foreign; break;
                case "missing-physics": owner.RemovePart(p); break;
                case "carried": p.InInventory = foreign; break;
                case "equipped": p.Equipped = foreign; break;
                case "takeable": p.Takeable = true; break;
                case "creature": owner.SetTag("Creature"); break;
                case "item": owner.SetTag("Item"); break;
                case "player": owner.SetTag("Player"); break;
                // The east neighbor is occupied: extend south into a free cell
                // so the real footprint transaction succeeds before rejection.
                case "footprint": owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1" }); break;
                case "zero-stack": owner.AddPart(new StackerPart { StackCount = 0 }); break;
            }
        }

        // Bounded submitted renderer, following the existing boundary cadence
        // fixture. Restore its global observers/perf values, not only GameObjects.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject grid;
            readonly ZoneRenderer renderer;
            readonly Tilemap overlay;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public bool FullDirty => (bool)Read(renderer, "_fullDirty");
            public Paint(FlatPixelGreyboxCatalog catalog, Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); grid = new GameObject("Hedge cadence grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var background = new GameObject("Hedge background", typeof(Tilemap)); background.transform.SetParent(saved.Root.transform, false);
                    renderer = saved.Renderer; Write(renderer, "_tilemap", main); Write(renderer, "_bgTilemap", background.GetComponent<Tilemap>());
                    var camera = new GameObject("Hedge camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, background.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(renderer, "_envSpriteRenderer", env); Write(renderer, "_flatPixelModeEnabled", true); renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = renderer.MarkDirty; Step();
                }
                catch { Dispose(); throw; }
            }
            public void Dirty(int x, int y) => renderer.MarkCellDirty(x, y, "hedge disclosure");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
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
