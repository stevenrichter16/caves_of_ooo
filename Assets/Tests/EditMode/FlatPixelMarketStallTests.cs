// Tests precede MarketStall integration. New optional-input constructor is reflected for native RED.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelMarketStallTests
    {
        const int X = 10, Y = 10;
        const string StallPath = "Sprites/Environment/market_stall";
        const string EastPath = "Sprites/Live16x24/east-kitchen";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed = new List<Texture2D>();
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach("Overworld.10.10.0"); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); foreach (var texture in borrowed) if (texture != null) Object.DestroyImmediate(texture); borrowed.Clear(); }
            finally { content?.Dispose(); }
        }
        Zone Attach(string id)
        { var next = new Zone(id); manager.SetActiveZone(next); foreach (var cell in next.Cells) cell.IsVisible = cell.Explored = true; return next; }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e, x, y)); return e; }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        static object Read(object target, string name) => target.GetType().GetField(name, I).GetValue(target);
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static Texture2D Resource(string path = StallPath)
        { var texture = Resources.Load<Texture2D>(path); Assert.NotNull(texture, path); return texture; }
        Texture2D Texture(int width, int height)
        { var texture = new Texture2D(width, height); borrowed.Add(texture); return texture; }
        static Type Adapter => typeof(FlatPixelSharedFurnitureCatalog);
        delegate bool Query(Entity owner, out Tile tile);
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            Adapter.GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        static IDisposable Art(Texture2D east, Texture2D stall)
        {
            var ctor = Adapter.GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D) });
            Assert.NotNull(ctor, "Independent optional MarketStall texture overload is required.");
            return (IDisposable)ctor.Invoke(new object[] { east, stall });
        }
        static ExaminePortraitRenderer Portraits() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I, null,
            new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        AnnouncementDocument Document(Entity owner, Entity viewer = null)
        { var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetEntityCell(owner), viewer); Assert.NotNull(doc); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc)); return doc; }
        static void Native(Tile tile, Texture2D texture = null)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture ?? Resource(), tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 16), tile.sprite.rect); Assert.AreEqual(new Vector2(8, 8), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(Vector2.one, (Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        Tile NativeOwner(Entity owner, Texture2D texture = null)
        {
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell); var view = View(cell.X, cell.Y);
            Assert.AreSame(owner, view.Owner); Assert.True(view.NativeForeground); Assert.True(view.BlocksMovement);
            Native(view.ForegroundTile, texture); return view.ForegroundTile;
        }
        void Refused(Entity owner)
        {
            using (var art = Art(Resource(EastPath), Resource())) { Assert.False(Resolver(art)(owner, out var tile)); Assert.IsNull(tile); }
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell);
            if (world.TryResolvePresentation(zone, cell.X, cell.Y, out var modern) && modern.ForegroundTile != null)
                Assert.AreNotSame(Resource(), modern.ForegroundTile.sprite.texture);
            if (world.TryResolve(zone, cell.X, cell.Y, out var old) && old.ForegroundTile != null)
                Assert.AreNotSame(Resource(), old.ForegroundTile.sprite.texture);
            if (world.TryGetSubjectSprite(owner, cell.X, cell.Y, out var subject, out _)) Assert.AreNotSame(Resource(), subject.texture);
            if (world.TryGetPresentationSubjectSprite(zone, owner, cell.X, cell.Y, out subject, out _)) Assert.AreNotSame(Resource(), subject.texture);
        }

        [TestCase("Overworld.10.10.0")] [TestCase("Overworld.12.11.0")]
        public void DefaultCounterIsSolidSquareInWorldAndAuthorizedReaderWithoutReplacingFloor(string id)
        {
            zone = Attach(id); var floor = Add("StoneFloor"); var baseView = View();
            Assert.NotNull(baseView.GroundTile); var ground = baseView.GroundTile;
            var owner = Add("MarketStall"); var player = Add("Player", X - 1, Y);
            Assert.False(owner.HasTag("Furniture")); Assert.True(owner.GetPart<PhysicsPart>().Solid);
            Assert.IsNull(owner.GetPart<ContainerPart>()); Assert.IsNull(owner.GetPart<TraderPart>());
            Assert.IsNull(owner.GetPart<MaterialPart>()); Assert.IsNull(owner.GetPart<DestructiblePart>()); Assert.IsNull(owner.GetStat("Hitpoints"));
            var body = NativeOwner(owner); Assert.AreSame(ground, View().GroundTile); Assert.True(zone.GetCell(X, Y).Objects.Contains(floor));
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var subject, out _)); Assert.AreSame(body.sprite, subject);
            if (FlatPixelGreyboxCatalog.IsAdmitted(zone))
            {
                Assert.True(world.TryResolve(zone, X, Y, out var old)); Native(old.ForegroundTile);
                Assert.True(world.TryGetSubjectSprite(owner, X, Y, out subject, out _)); Assert.AreSame(body.sprite, subject);
            }
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); Native(paint.At(X, Y)); Assert.AreSame(ground, paint.GroundAt(X, Y));
                Assert.AreEqual(Matrix4x4.identity, paint.Transform(X, Y)); Assert.AreEqual(Color.white, paint.Tint(X, Y));
                var image = portraits.GetPortrait(Document(owner, player), paint.Renderer);
                Assert.AreSame(Resource(), image.Texture); Assert.AreEqual(new Rect(0, 0, 1, 1), image.Uv); Assert.False(image.IsModel);
                Assert.True(zone.RemoveEntity(owner)); paint.Dirty(X, Y); paint.Step();
                Assert.AreSame(ground, View().GroundTile); Assert.AreSame(ground, paint.GroundAt(X, Y));
            }
        }

        [Test] public void OldOneInputRetainsBedChairAndDoesNotReloadNewCounterGroup()
        {
            var atlas = Texture(288, 24); using (var old = new FlatPixelSharedFurnitureCatalog(atlas))
            {
                foreach (var name in new[] { "Bed", "Chair" })
                {
                    Assert.True(old.TryResolve(content.Factory.CreateEntity(name), out var tile)); Assert.AreSame(atlas, tile.sprite.texture);
                    Assert.AreEqual(new Rect(name == "Bed" ? 32 : 48, 0, 16, 24), tile.sprite.rect);
                }
                Assert.False(old.TryResolve(content.Factory.CreateEntity("MarketStall"), out _));
            }
            // Existing explicitly borrowed East path still supplies Bed/Chair but cannot load a new group.
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            Write(world, "_eastKitchenArt", new FlatPixelEastKitchenCatalog(atlas, null));
            zone = Attach("Overworld.12.11.0"); var bed = Add("Bed"); var chair = Add("Chair", X + 1, Y); var stall = Add("MarketStall", X + 2, Y);
            Assert.AreSame(atlas, View().ForegroundTile.sprite.texture); Assert.AreSame(atlas, View(X + 1, Y).ForegroundTile.sprite.texture);
            Assert.False(View(X + 2, Y).NativeForeground); Assert.AreSame(stall, View(X + 2, Y).Owner);
        }

        [Test] public void DefaultTravelAfterWarmingEastStillLoadsCounterWhileExplicitEastRemainsIndependent()
        {
            zone = Attach("Overworld.12.11.0"); var forge = Add("TinkersForge");
            Assert.AreSame(Resource(EastPath), View().ForegroundTile.sprite.texture);
            Assert.NotNull(Read(world, "_eastKitchenArt")); Assert.True((bool)Read(world, "_loadOptionalResources"));
            zone = Attach("Overworld.10.10.0"); var stall = Add("MarketStall"); NativeOwner(stall);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, stall, X, Y, out var sprite, out _)); Assert.AreSame(Resource(), sprite.texture);
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            Write(world, "_eastKitchenArt", new FlatPixelEastKitchenCatalog(Resource(EastPath), null));
            Assert.False((bool)Read(world, "_loadOptionalResources"));
            Assert.False(View().NativeForeground); Assert.AreSame(stall, View().Owner);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, stall, X, Y, out sprite, out _)); Assert.AreNotSame(Resource(), sprite.texture);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ExplicitMissingOrWrongCounterTextureStaysFallbackWithoutBreakingFurniture(int fault)
        {
            var east = Texture(288, 24); var bad = fault == 0 ? null : fault == 1 ? Texture(15, 16) : Texture(16, 24);
            using (var positive = Art(east, Resource())) Assert.True(Resolver(positive)(content.Factory.CreateEntity("MarketStall"), out _));
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); Write(world, "_sharedFurnitureArt", Art(east, bad));
            zone = Attach("Overworld.12.11.0"); var stall = Add("MarketStall"); Add("Bed", X + 1, Y); Add("Chair", X + 2, Y);
            Assert.AreSame(stall, View().Owner); Assert.False(View().NativeForeground);
            Assert.True(world.TryResolve(zone, X, Y, out var old)); Assert.AreNotSame(Resource(), old.ForegroundTile.sprite.texture);
            Assert.True(world.TryGetSubjectSprite(stall, X, Y, out var oldSubject, out _)); Assert.AreNotSame(Resource(), oldSubject.texture);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, stall, X, Y, out var subject, out _)); Assert.AreNotSame(Resource(), subject.texture);
            Assert.AreSame(east, View(X + 1, Y).ForegroundTile.sprite.texture); Assert.AreSame(east, View(X + 2, Y).ForegroundTile.sprite.texture);
        }

        [Test] public void CounterInputWorksIndependentlyOfAbsentFurnitureAtlas()
        {
            var texture = Texture(16, 16); using (var art = Art(null, texture))
            {
                Assert.True(Resolver(art)(content.Factory.CreateEntity("MarketStall"), out var tile)); Native(tile, texture);
                Assert.False(Resolver(art)(content.Factory.CreateEntity("Bed"), out _)); Assert.False(Resolver(art)(content.Factory.CreateEntity("Chair"), out _));
            }
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); Write(world, "_sharedFurnitureArt", Art(null, texture));
            var stall = Add("MarketStall"); NativeOwner(stall, texture);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, stall, X, Y, out var sprite, out _)); Assert.AreSame(texture, sprite.texture);
        }

        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void RequiredPartsAreUniqueAndOwned(string fault)
        {
            foreach (var type in new[] { typeof(RenderPart), typeof(PhysicsPart), typeof(ExaminablePart) })
            {
                zone = Attach("Overworld.12.11.0"); var owner = Add("MarketStall"); NativeOwner(owner);
                var part = owner.Parts.Single(p => p.GetType() == type);
                if (fault == "missing") Assert.True(owner.RemovePart(part));
                else if (fault == "foreign") part.ParentEntity = new Entity();
                else owner.AddPart((Part)Activator.CreateInstance(type));
                Refused(owner);
            }
        }

        [Test] public void CustodyReskinAndChangedPhysicalMeaningCannotAcquireCounterArt()
        {
            foreach (string fault in new[] { "carried", "equipped", "takeable", "nonsolid", "hidden", "glyph", "layer", "visual", "variant", "variants", "background" })
            {
                zone = Attach("Overworld.12.11.0"); var owner = Add("MarketStall"); NativeOwner(owner);
                var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
                switch (fault)
                {
                    case "carried": physics.InInventory = new Entity(); break;
                    case "equipped": physics.Equipped = new Entity(); break;
                    case "takeable": physics.Takeable = true; break;
                    case "nonsolid": physics.Solid = false; break;
                    case "hidden": render.Visible = false; break;
                    case "glyph": render.RenderString = "!"; break;
                    case "layer": render.RenderLayer = 5; break;
                    case "visual": render.VisualID = "reference-glade-low-wall"; break;
                    case "variant": render.VisualVariant = "custom"; break;
                    case "variants": render.GlyphVariants = "=="; break;
                    case "background": render.BackgroundColor = "^r"; break;
                }
                Refused(owner);
            }
        }

        [Test] public void AddedServicesOrTerrainMeaningAndSameGlyphAliasesRemainDistinct()
        {
            foreach (var type in new[] { typeof(ContainerPart), typeof(TraderPart), typeof(LockPart), typeof(MaterialPart), typeof(ThermalPart), typeof(DestructiblePart), typeof(BedPart), typeof(ChairPart), typeof(DoorPart), typeof(InventoryPart) })
            {
                zone = Attach("Overworld.12.11.0"); var owner = Add("MarketStall"); NativeOwner(owner);
                owner.AddPart((Part)Activator.CreateInstance(type)); Refused(owner);
            }
            foreach (var tag in new[] { "Creature", "Item", "Terrain", "Crop" })
            { zone = Attach("Overworld.12.11.0"); var owner = Add("MarketStall"); NativeOwner(owner); owner.SetTag(tag); Refused(owner); }
            using (var art = Art(Resource(EastPath), Resource()))
            {
                var alias = content.Factory.CreateEntity("Chest"); alias.GetPart<RenderPart>().RenderString = "="; alias.Properties["SourceBlueprint"] = "MarketStall";
                Assert.False(Resolver(art)(alias, out _)); Assert.False(Resolver(art)(null, out _));
            }
        }

        [Test] public void CurrentSingleCellAndOptionalOwnedStackRemainRequired()
        {
            var owner = Add("MarketStall"); NativeOwner(owner);
            owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" }); NativeOwner(owner);
            Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0")); Refused(owner);
            Assert.True(zone.TryChangeFootprint(owner, "0,0")); NativeOwner(owner);
            owner.AddPart(new StackerPart()); var stack = owner.GetPart<StackerPart>(); stack.StackCount = 2; NativeOwner(owner);
            stack.StackCount = 0; Refused(owner); stack.StackCount = 1; NativeOwner(owner);
            stack.ParentEntity = new Entity(); Refused(owner);
        }

        [Test] public void DisclosureCoverRemovalAndReplacedGraphRetireWorldAndPortraitAuthority()
        {
            var owner = Add("MarketStall"); var body = NativeOwner(owner); var doc = Document(owner);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); Assert.AreSame(Resource(), portraits.GetPortrait(doc, paint.Renderer).Texture);
                var cell = zone.GetCell(X, Y); cell.IsVisible = false; paint.Dirty(X, Y); paint.Step();
                Assert.False(View().NativeForeground); Assert.IsNull(portraits.GetPortrait(doc, paint.Renderer).Texture);
                cell.Explored = false; Assert.False(world.TryResolvePresentation(zone, X, Y, out _)); cell.Explored = cell.IsVisible = true;
                var cover = Add("Player"); paint.Dirty(X, Y); paint.Step(); Assert.AreSame(cover, View().Owner); Assert.AreNotSame(body, paint.At(X, Y));
                Assert.True(zone.RemoveEntity(cover)); paint.Dirty(X, Y); paint.Step(); Assert.AreSame(body, paint.At(X, Y));
                manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID);
                Assert.False(world.TryResolvePresentation(zone, X, Y, out _)); Assert.IsNull(portraits.GetPortrait(doc, paint.Renderer).Texture);
                manager.CachedZones[zone.ZoneID] = zone; Assert.True(zone.RemoveEntity(owner));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc)); Assert.IsNull(portraits.GetPortrait(doc, paint.Renderer).Texture);
            }
        }

        [Test] public void ExistingSingleRenderCueIsRetainedWithoutQueriesInventingCounterServices()
        {
            var owner = Add("MarketStall"); NativeOwner(owner); var probe = new EventProbe(); owner.AddPart(probe);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); var body = paint.At(X, Y); Assert.AreEqual(Color.white, paint.Tint(X, Y)); int before = probe.Renders;
                probe.OverrideColor = "&R"; paint.Dirty(X, Y); paint.Step(); Assert.AreEqual(before + 1, probe.Renders);
                Assert.AreSame(body, paint.At(X, Y)); Assert.AreEqual(QudColorParser.Parse("&R"), paint.Tint(X, Y));
                NativeOwner(owner); portraits.GetPortrait(Document(owner), paint.Renderer); paint.Dirty(X + 1, Y); paint.Step();
                Assert.AreEqual(before + 1, probe.Renders); Assert.AreEqual(QudColorParser.Parse("&R"), paint.Tint(X, Y));
                Assert.AreEqual(0, probe.IdleQueries); Assert.AreEqual(0, probe.InventoryActions);
            }
        }

        [Test] public void CachedCounterViewsDisposeWithoutDestroyingOrEditingBorrowedInputs()
        {
            var east = Texture(288, 24); var stall = Texture(16, 16); var owner = content.Factory.CreateEntity("MarketStall");
            var art = Art(east, stall); using (var peer = Art(east, stall))
            {
                try
                {
                    Assert.True(Resolver(art)(owner, out var tile)); Assert.True(Resolver(art)(owner, out var again)); Assert.AreSame(tile, again);
                    art.Dispose(); art.Dispose(); Assert.True(tile == null); Assert.False(Resolver(art)(owner, out _));
                    Assert.True(Resolver(peer)(owner, out var other)); Native(other, stall); Assert.True(east != null && stall != null);
                    Assert.AreEqual(FilterMode.Bilinear, stall.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, stall.wrapMode);
                }
                finally { art.Dispose(); }
            }
        }
        sealed class EventProbe : Part
        {
            public int Renders, IdleQueries, InventoryActions;
            public string OverrideColor;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Render") { Renders++; if (OverrideColor != null) e.SetParameter("ColorString", OverrideColor); }
                if (e.ID == IdleQueryEvent.ID) IdleQueries++;
                if (e.ID == "InventoryAction" || e.ID == "GetInventoryActions") InventoryActions++;
                return true;
            }
        }
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, main, ground;
            public ZoneRenderer Renderer { get; }
            public Transform RootTransform => root.transform;
            public Tilemap Main => main;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Market stall submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var marks = new GameObject("Hazards", typeof(Tilemap)); marks.transform.SetParent(root.transform, false);
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", ground); Write(Renderer, "_tileStateTilemap", marks.GetComponent<Tilemap>());
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, ground); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap"); Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            static Vector3Int Position(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
            public Tile At(int x, int y) { var at = Position(x, y); return overlay.GetTile<Tile>(at) ?? main.GetTile<Tile>(at); }
            public Matrix4x4 Transform(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetTransformMatrix(at) : main.GetTransformMatrix(at); }
            public Color Tint(int x, int y) { var at = Position(x, y); return overlay.HasTile(at) ? overlay.GetColor(at) : main.GetColor(at); }
            public Color Lit(Color color, int x, int y) { var light = (LightMap)Read(Renderer, "_lightMap"); return light == null ? color : light.ApplyToColor(color, x, y); }
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(Position(x, y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Market stall fixture");
            public void Step() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try
                {
                    // The fixture owns this catalog across regions/save checks.
                    // EnvironmentSpriteRenderer owns its field in production,
                    // so detach this borrowed test injection before teardown.
                    if (saved?.Root != null)
                    {
                        var environment = saved.Root.GetComponent<EnvironmentSpriteRenderer>();
                        if (environment != null) Write(environment, "_flatPixelGreybox", null);
                    }
                    saved?.Dispose(); if (root != null) Object.DestroyImmediate(root);
                }
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
