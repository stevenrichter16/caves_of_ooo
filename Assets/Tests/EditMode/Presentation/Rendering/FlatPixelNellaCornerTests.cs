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
    // The corner is existing conductive metal and peat, not a new irrigation
    // system. These tests require the real selected owner and native world route.
    public sealed class FlatPixelNellaCornerTests
    {
        const string Plot = "Overworld.11.8.0", Resource = "Sprites/Live16x24/nella-corner";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Zone zone, int x, int y, out Tile tile);
        delegate bool Portrait(Zone zone, Entity subject, int x, int y, out Sprite sprite);
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
            selector = new FlatPixelFallbackCatalog(); atlas = new Texture2D(272, 24); zone = Attach(Plot);
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
        Entity Add(string blueprint = "CopperPipe", int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        void Create(Texture2D source, bool inject = false)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelNellaCornerCatalog");
            Assert.NotNull(type, "Nella's disclosed pipe and peat corner has no native owner adapter.");
            var ctor = type.GetConstructor(new[] { typeof(Texture2D), typeof(FlatPixelFallbackCatalog) }); Assert.NotNull(ctor);
            adapter = (IDisposable)ctor.Invoke(new object[] { source, selector });
            query = (Query)Delegate.CreateDelegate(typeof(Query), adapter, type.GetMethod("TryResolve", I));
            portrait = (Portrait)Delegate.CreateDelegate(typeof(Portrait), adapter, type.GetMethod("TryGetSubjectSprite", I));
            if (!inject) return;
            world = new FlatPixelGreyboxCatalog((Texture2D)null); Write(world, "_nellaCornerArt", adapter);
        }
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static object Read(object target, string name) => target.GetType().GetField(name, I).GetValue(target);
        static Rect Rect(int frame) => new Rect(frame * 16, 0, 16, 24);
        static void Frame(Tile tile, Texture2D texture, int frame)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(Rect(frame), tile.sprite.rect); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(TileFlags.None, tile.flags);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        Tile Expect(int frame)
        { Assert.True(query(zone, X, Y, out var tile)); Frame(tile, atlas, frame); return tile; }
        FlatPixelGreyboxCell Visible()
        { Assert.True(world.TryResolvePresentation(zone, X, Y, out var value)); return value; }
        void Neighbors(int mask)
        {
            if ((mask & 1) != 0) Add(x: X, y: Y - 1);
            if ((mask & 2) != 0) Add(x: X + 1, y: Y);
            if ((mask & 4) != 0) Add(x: X, y: Y + 1);
            if ((mask & 8) != 0) Add(x: X - 1, y: Y);
        }

        [TestCase("CopperPipe", 5)] [TestCase("PeatBog", 16)]
        public void DefaultWorldAndReaderUseActualCornerResourceForCurrentNellaOwners(string blueprint, int frame)
        {
            var center = Add(blueprint); Neighbors(5); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone)); var value = Visible();
            Assert.AreSame(center, value.Owner); Assert.True(value.NativeForeground, "The ordinary world still uses a literal terrain glyph.");
            var resource = Resources.Load<Texture2D>(Resource); Assert.NotNull(resource);
            Assert.AreEqual(272, resource.width); Assert.AreEqual(24, resource.height);
            Assert.AreEqual(FilterMode.Point, resource.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, resource.wrapMode);
            Assert.AreEqual(1, resource.mipmapCount); Frame(value.ForegroundTile, resource, frame);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, center, X, Y, out var sprite, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.False(value.BlocksMovement); Assert.False(zone.GetCell(X, Y).BlocksMovement());
        }

        [Test] public void AllSixteenPipeMasksCacheExactCardinalOwnersAndIgnoreDiagonals()
        {
            Create(atlas); var seen = new HashSet<Tile>();
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(Plot); var center = Add(); Neighbors(mask); Add(x: X + 1, y: Y + 1); Add(x: X + 2);
                var tile = Expect(mask); Assert.True(seen.Add(tile)); Assert.AreSame(tile, Expect(mask));
                Assert.True(portrait(zone, center, X, Y, out var sprite)); Assert.AreSame(tile.sprite, sprite);
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.IsNull(center.GetPart<DestructiblePart>(), "CopperPipe has no invented HP prerequisite.");
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("covered")]
        public void AConnectionRequiresTheDisclosedSelectedNeighborAndRecovers(string fault)
        {
            Create(atlas); var center = Add(); Add(x: X + 1); Expect(2); var east = zone.GetCell(X + 1, Y); Entity cover = null;
            if (fault == "hidden") east.IsVisible = false;
            if (fault == "unexplored") east.Explored = false;
            if (fault == "covered") { cover = Add("Player", X + 1); cover.GetPart<RenderPart>().RenderLayer = 100; }
            var cap = Expect(0); Assert.True(portrait(zone, center, X, Y, out var sprite)); Assert.AreSame(cap.sprite, sprite);
            east.IsVisible = east.Explored = true; if (cover != null) Assert.True(zone.RemoveEntity(cover)); Expect(2);
        }

        [TestCase("PeatBog")] [TestCase("StoneWall")]
        public void OtherConductiveOrAdjacentTerrainDoesNotBecomeAPipeConnection(string blueprint)
        {
            Create(atlas); Add(); var east = Add(x: X + 1); Expect(2); Assert.True(zone.RemoveEntity(east));
            Add(blueprint, X + 1); Expect(0);
        }

        static readonly string[] ArtFaults = { "visual-id", "visual-variant", "glyph-variants", "foreign-render", "invisible", "identity", "terrain" };
        static readonly string[] StructureFaults = { "foreign-material", "wrong-material", "missing-material", "foreign-physics", "missing-physics", "solid" };
        static readonly string[] CustodyFaults = { "carried", "equipped", "takeable", "creature", "item", "player", "footprint", "zero-stack", "foreign-stack" };
        [TestCase("CopperPipe", 0)] [TestCase("CopperPipe", 1)] [TestCase("CopperPipe", 2)]
        [TestCase("PeatBog", 0)] [TestCase("PeatBog", 1)] [TestCase("PeatBog", 2)]
        public void ArtAndOwnershipFaultsCannotBorrowTheNativeRole(string blueprint, int group)
        {
            Create(atlas);
            foreach (string fault in group == 0 ? ArtFaults : group == 1 ? StructureFaults : CustodyFaults)
            {
                zone = Attach(Plot); var owner = Add(blueprint); Expect(blueprint == "CopperPipe" ? 0 : 16);
                Fault(owner, fault); Assert.False(query(zone, X, Y, out _), fault);
                Assert.False(portrait(zone, owner, X, Y, out _), fault);
            }
        }

        [Test] public void IneligiblePipeNeighborsDoNotContributeAHiddenConnection()
        {
            Create(atlas);
            foreach (string fault in new[] { "foreign-material", "visual-variant", "foreign-physics", "footprint", "carried" })
            {
                zone = Attach(Plot); Add(); var east = Add(x: X + 1); Expect(2); Fault(east, fault); Expect(0);
            }
        }

        [Test] public void PeatRequiresItsOwnPoolSourceAndLiveStructureWithoutRequiringFullStock()
        {
            Create(atlas);
            foreach (string fault in new[] { "missing-pool", "foreign-pool", "other-liquid", "missing-source", "foreign-source", "other-coating", "missing-structure", "foreign-structure", "gone", "zero-hp" })
            {
                zone = Attach(Plot); var peat = Add("PeatBog"); Expect(16); Fault(peat, fault);
                Assert.False(query(zone, X, Y, out _), fault); Assert.False(portrait(zone, peat, X, Y, out _), fault);
            }
            zone = Attach(Plot); var remaining = Add("PeatBog"); var tile = Expect(16);
            remaining.GetPart<LiquidPoolPart>().Volume = 1; remaining.GetPart<DestructiblePart>().HP = 1;
            Assert.AreSame(tile, Expect(16), "The neutral peat identity does not advertise full stock or undamaged condition.");
            remaining.GetPart<LiquidPoolPart>().Volume = 0;
            Assert.AreSame(tile, Expect(16), "Physical peat remains when the pool has no drawable stock.");
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("replaced-graph")]
        [TestCase("wrong-biome")] [TestCase("poi")]
        public void CurrentVisibleExactNellaAuthorityControlsBothWorldAndSubject(string fault)
        {
            Create(atlas, true); var owner = Add(); Expect(0); var cell = zone.GetCell(X, Y);
            if (fault == "hidden") cell.IsVisible = false;
            if (fault == "unexplored") cell.Explored = false;
            if (fault == "replaced-graph") manager.CachedZones[Plot] = new Zone(Plot);
            if (fault == "wrong-biome") manager.WorldMap.Tiles[11, 8] = BiomeType.Sodden;
            if (fault == "poi") manager.WorldMap.SetPOI(11, 8, new PointOfInterest(POIType.Village, "corner counter", null, 1));
            Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, owner, X, Y, out _));
            if (world.TryResolvePresentation(zone, X, Y, out var fallback))
                Assert.True(fallback.ForegroundTile == null || fallback.ForegroundTile.sprite.texture != atlas);
            cell.IsVisible = cell.Explored = true; manager.CachedZones[Plot] = zone;
            manager.WorldMap.Tiles[11, 8] = BiomeType.Spread; manager.WorldMap.SetPOI(11, 8, null); Expect(0);
        }

        [Test] public void CoveredCurrentSubjectKeepsItsOwnConnectedPortraitButNeverReplacesTheActor()
        {
            Create(atlas, true); var pipe = Add(); Add(x: X + 1); var tile = Expect(2);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.False(query(zone, X, Y, out _)); Assert.AreSame(actor, Visible().Owner);
            Assert.True(portrait(zone, pipe, X, Y, out var sprite)); Assert.AreSame(tile.sprite, sprite);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, pipe, X, Y, out var reader, out _)); Assert.AreSame(sprite, reader);
            Assert.True(zone.RemoveEntity(actor)); Assert.AreSame(tile, Expect(2)); Assert.AreSame(tile, Visible().ForegroundTile);
        }

        [Test] public void MovedStaleAndSameIdSubjectsCannotReuseTheOldCellAuthority()
        {
            Create(atlas); var pipe = Add(); var east = Add(x: X + 1); Expect(2); var oldEast = zone.GetCell(X + 1, Y);
            Assert.True(zone.MoveEntity(east, X + 2, Y)); oldEast.Objects.Add(east); Expect(0);
            var phantom = content.Factory.CreateEntity("CopperPipe"); phantom.ID = east.ID; oldEast.Objects.Add(phantom); Expect(0);
            Assert.False(portrait(zone, phantom, X + 1, Y, out _)); Assert.False(portrait(zone, east, X + 1, Y, out _));
            Assert.True(portrait(zone, east, X + 2, Y, out _)); Assert.False(portrait(zone, pipe, X + 1, Y, out _));
            Assert.True(zone.RemoveEntity(pipe)); zone.GetCell(X, Y).Objects.Add(pipe);
            Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, pipe, X, Y, out _));
        }

        [TestCase("Overworld.11.9.0")] [TestCase("Overworld.12.10.0")] [TestCase("Overworld.12.12.0")]
        public void OtherSitesKeepTheirExistingPresentationInsteadOfBorrowingTheCorner(string id)
        {
            Create(atlas, true); zone = Attach(id); var owner = Add(); Neighbors(5);
            Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, owner, X, Y, out _));
            Assert.AreNotSame(atlas, Visible().ForegroundTile.sprite.texture);
        }

        [Test] public void QueriesDoNotCreateWaterChargeOrChangePeatStockAndSource()
        {
            Create(atlas, true); var peat = Add("PeatBog"); var pool = peat.GetPart<LiquidPoolPart>();
            var source = peat.GetPart<TileStateSourcePart>(); var structure = peat.GetPart<DestructiblePart>();
            var tile = Expect(16); int version = zone.EntityVersion; string state = zone.TileState.ToSaveString();
            Assert.AreEqual("water", pool.LiquidId); Assert.AreEqual(70, pool.Volume); Assert.AreEqual(4, source.CoatingTurns);
            for (int i = 0; i < 32; i++)
            {
                Assert.AreSame(tile, Expect(16)); Assert.AreSame(tile, Visible().ForegroundTile);
                Assert.True(portrait(zone, peat, X, Y, out var sprite)); Assert.AreSame(tile.sprite, sprite);
            }
            Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(70, pool.Volume); Assert.AreEqual("water", source.Coating); Assert.AreEqual(4, source.CoatingTurns);
            Assert.AreEqual(40, structure.HP); Assert.False(structure.Gone); Assert.AreSame(zone.GetCell(X, Y), zone.GetEntityCell(peat));
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedPixelsKeepGlyphFallbackWithoutLoadingAnInjectedResource(bool malformed)
        {
            var bad = malformed ? new Texture2D(271, 24) : null;
            try
            {
                Create(bad, true); var owner = Add();
                Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, owner, X, Y, out _));
                Assert.AreSame(owner, Visible().Owner); Assert.False(Visible().NativeForeground);
                Assert.AreEqual("=", owner.GetPart<RenderPart>().RenderString);
                adapter.Dispose(); Assert.True(selector.TryResolve(zone, X, Y, out _)); if (bad != null) Assert.True(bad != null);
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test] public void DisposalReleasesOnlyCachedFramesAndKeepsBorrowedPixelsAndSelectorAlive()
        {
            Create(atlas); var pipe = Add(); var tile = Expect(0); var sprite = tile.sprite;
            adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.True(atlas != null);
            Assert.True(selector.TryResolve(zone, X, Y, out _)); Assert.False(query(zone, X, Y, out _));
            Assert.False(portrait(zone, pipe, X, Y, out _));
            world = new FlatPixelGreyboxCatalog((Texture2D)null); Assert.False(Visible().NativeForeground);
        }

        [Test] public void NeighborRemovalCoverAndDisclosureRepaintSubmittedConnectionsIncrementally()
        {
            Create(atlas, true); Add(); var east = Add(x: X + 1);
            using (var paint = new Paint(world, zone))
            {
                Frame(paint.At(X, Y), atlas, 2); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(zone.RemoveEntity(east)); paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), atlas, 0);
                Assert.True(zone.AddEntity(east, X + 1, Y)); paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), atlas, 2);
                var actor = Add("Player", X + 1); actor.GetPart<RenderPart>().RenderLayer = 100;
                paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), atlas, 0);
                Assert.True(zone.RemoveEntity(actor)); paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), atlas, 2);
                zone.GetCell(X + 1, Y).IsVisible = false; paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), atlas, 0);
                zone.GetCell(X + 1, Y).IsVisible = true; paint.Dirty(X + 1, Y); paint.Step(); Frame(paint.At(X, Y), atlas, 2);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step(); Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        static void Fault(Entity owner, string fault)
        {
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>(); var foreign = new Entity();
            switch (fault)
            {
                case "visual-id": render.VisualID = "custom-corner"; break;
                case "visual-variant": render.VisualVariant = "custom-corner"; break;
                case "glyph-variants": render.GlyphVariants = "=~"; break;
                case "foreign-render": render.ParentEntity = foreign; break;
                case "invisible": render.Visible = false; break;
                case "identity": owner.BlueprintName = "OtherTerrain"; break;
                case "terrain": owner.Tags.Remove("Terrain"); break;
                case "foreign-material": owner.GetPart<MaterialPart>().ParentEntity = foreign; break;
                case "wrong-material": owner.GetPart<MaterialPart>().MaterialID = "Stone"; break;
                case "missing-material": owner.RemovePart(owner.GetPart<MaterialPart>()); break;
                case "foreign-physics": physics.ParentEntity = foreign; break;
                case "missing-physics": owner.RemovePart(physics); break;
                case "solid": physics.Solid = true; break;
                case "carried": physics.InInventory = foreign; break;
                case "equipped": physics.Equipped = foreign; break;
                case "takeable": physics.Takeable = true; break;
                case "creature": owner.SetTag("Creature"); break;
                case "item": owner.SetTag("Item"); break;
                case "player": owner.SetTag("Player"); break;
                case "footprint": owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1" }); break;
                case "zero-stack": owner.AddPart(new StackerPart { StackCount = 0 }); break;
                case "foreign-stack": var stack = new StackerPart { StackCount = 1 }; owner.AddPart(stack); stack.ParentEntity = foreign; break;
                case "missing-pool": owner.RemovePart(owner.GetPart<LiquidPoolPart>()); break;
                case "foreign-pool": owner.GetPart<LiquidPoolPart>().ParentEntity = foreign; break;
                case "other-liquid": owner.GetPart<LiquidPoolPart>().LiquidId = "oil"; break;
                case "missing-source": owner.RemovePart(owner.GetPart<TileStateSourcePart>()); break;
                case "foreign-source": owner.GetPart<TileStateSourcePart>().ParentEntity = foreign; break;
                case "other-coating": owner.GetPart<TileStateSourcePart>().Coating = "oil"; break;
                case "missing-structure": owner.RemovePart(owner.GetPart<DestructiblePart>()); break;
                case "foreign-structure": owner.GetPart<DestructiblePart>().ParentEntity = foreign; break;
                case "gone": owner.GetPart<DestructiblePart>().Gone = true; break;
                case "zero-hp": owner.GetPart<DestructiblePart>().HP = 0; break;
            }
        }

        // Submitted Tilemap cadence follows the accepted hedge fixture. FOV and
        // entity edits explicitly request a cell repaint; this is not an input test.
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
            public Paint(FlatPixelGreyboxCatalog catalog, Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); grid = new GameObject("Nella corner cadence grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var background = new GameObject("Corner background", typeof(Tilemap)); background.transform.SetParent(saved.Root.transform, false);
                    renderer = saved.Renderer; Write(renderer, "_tilemap", main); Write(renderer, "_bgTilemap", background.GetComponent<Tilemap>());
                    var camera = new GameObject("Corner camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, background.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(renderer, "_envSpriteRenderer", env); Write(renderer, "_flatPixelModeEnabled", true); renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = renderer.MarkDirty; Step();
                }
                catch { Dispose(); throw; }
            }
            public void Dirty(int x, int y) => renderer.MarkCellDirty(x, y, "corner disclosure");
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
