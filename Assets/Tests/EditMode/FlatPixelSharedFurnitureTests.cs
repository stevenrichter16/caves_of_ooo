// Exact shared furniture bodies. Public-path RED does not require the new type to exist.
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
    public sealed class FlatPixelSharedFurnitureTests
    {
        const int X = 10, Y = 10;
        const string ResourcePath = "Sprites/Live16x24/east-kitchen";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        readonly List<Texture2D> borrowed = new List<Texture2D>();

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach("Overworld.15.7.0"); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); foreach (var texture in borrowed) if (texture != null) Object.DestroyImmediate(texture); borrowed.Clear(); }
            finally { content?.Dispose(); }
        }
        Zone Attach(string id)
        { var graph = new Zone(id); manager.SetActiveZone(graph); foreach (var cell in graph.Cells) cell.IsVisible = cell.Explored = true; return graph; }
        Entity Add(string name, int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(name); Assert.NotNull(owner, name); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var view)); return view; }
        static Texture2D Resource()
        { var texture = Resources.Load<Texture2D>(ResourcePath); Assert.NotNull(texture); return texture; }
        static Rect BodyRect(string name) => new Rect(name == "Bed" ? 32 : 48, 0, 16, 24);
        static void Native(Tile tile, string name, Texture2D texture = null)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture ?? Resource(), tile.sprite.texture);
            Assert.AreEqual(BodyRect(name), tile.sprite.rect); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(1, 1.5f), (Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        void NativeOwner(Entity owner, Texture2D texture = null)
        {
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell); var view = View(cell.X, cell.Y);
            Assert.AreSame(owner, view.Owner); Assert.True(view.NativeForeground); Assert.False(view.BlocksMovement);
            Native(view.ForegroundTile, owner.BlueprintName, texture);
        }
        static object Read(object target, string name) => target.GetType().GetField(name, I).GetValue(target);
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static ExaminePortraitRenderer Portraits() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I, null,
            new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        AnnouncementDocument Document(Entity owner, Entity viewer = null)
        { var doc = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetEntityCell(owner), viewer); Assert.NotNull(doc); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc)); return doc; }

        [TestCase("Bed")] [TestCase("Chair")]
        public void DefaultPublicWorldAndAuthorizedReaderReuseFurnitureAcrossRegions(string name)
        {
            foreach (string id in new[] { "Overworld.15.7.0", "Overworld.12.11.0", "StandaloneFurnitureTest" })
            {
                zone = Attach(id); var owner = Add(name); var player = Add("Player", X - 1, Y);
                NativeOwner(owner);
                using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
                {
                    paint.Step(); Native(paint.At(X, Y), name);
                    Assert.AreEqual(Color.white, paint.Tint(X, Y)); Assert.AreEqual(Matrix4x4.identity, paint.Transform(X, Y));
                    var image = portraits.GetPortrait(Document(owner, player), paint.Renderer);
                    var texture = Resource(); var rect = BodyRect(name);
                    Assert.AreSame(texture, image.Texture); Assert.False(image.IsModel);
                    Assert.AreEqual(new Rect(rect.x / texture.width, rect.y / texture.height, rect.width / texture.width, rect.height / texture.height), image.Uv);
                }
            }
        }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSharedFurnitureCatalog"); Assert.NotNull(type); return type; }
        delegate bool Query(Entity owner, out Tile tile);
        static IDisposable Construct(Texture2D texture)
        { var ctor = AdapterType().GetConstructor(new[] { typeof(Texture2D) }); Assert.NotNull(ctor); return (IDisposable)ctor.Invoke(new object[] { texture }); }
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            AdapterType().GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        Texture2D Texture(int width = 288, int height = 24)
        { var texture = new Texture2D(width, height); borrowed.Add(texture); return texture; }
        static IDisposable East(Texture2D atlas)
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelEastKitchenCatalog"); Assert.NotNull(type); return (IDisposable)Activator.CreateInstance(type, new object[] { atlas, null }); }
        static Part Service(Entity owner) => owner.BlueprintName == "Bed" ? (Part)owner.GetPart<BedPart>() : owner.GetPart<ChairPart>();
        static bool Occupied(Entity owner) => owner.BlueprintName == "Bed" ? owner.GetPart<BedPart>().Occupied : owner.GetPart<ChairPart>().Occupied;
        static void Reservation(Entity owner, string claimant, bool occupied, Entity occupant)
        {
            if (owner.BlueprintName == "Bed") { var bed = owner.GetPart<BedPart>(); bed.Owner = claimant; bed.Occupied = occupied; }
            else { var chair = owner.GetPart<ChairPart>(); chair.Owner = claimant; chair.Occupied = occupied; chair.Occupant = occupant; }
        }

        [TestCase("Bed")] [TestCase("Chair")]
        public void OwnerReservationAndOccupantDoNotInventDifferentBodyOrServiceQueries(string name)
        {
            var owner = Add(name); NativeOwner(owner); var body = View().ForegroundTile;
            var player = Add("Player", X - 1, Y); var probe = new EventProbe(); owner.AddPart(probe);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); int hp = player.GetStatValue("Hitpoints"); var position = zone.GetEntityPosition(player);
                foreach (bool occupied in new[] { false, true })
                {
                    Reservation(owner, player.ID, occupied, occupied ? player : null);
                    for (int i = 0; i < 3; i++)
                    {
                        NativeOwner(owner); Assert.AreSame(body, View().ForegroundTile);
                        Assert.AreSame(Resource(), portraits.GetPortrait(Document(owner, player), paint.Renderer).Texture);
                    }
                    Assert.AreEqual(occupied, Occupied(owner));
                    Assert.AreEqual(player.ID, name == "Bed" ? owner.GetPart<BedPart>().Owner : owner.GetPart<ChairPart>().Owner);
                    if (name == "Chair") Assert.AreSame(occupied ? player : null, owner.GetPart<ChairPart>().Occupant);
                }
                Assert.AreEqual(0, probe.IdleQueries); Assert.AreEqual(0, probe.InventoryActions);
                Assert.AreEqual(hp, player.GetStatValue("Hitpoints")); Assert.AreEqual(position, zone.GetEntityPosition(player));
                Assert.IsNull(player.GetPart<StatusEffectsPart>(), "Pure presentation cannot install a first effect container.");
            }
        }

        [TestCase("Bed")] [TestCase("Chair")]
        public void RealNpcIdleOfferAndCleanupRemainSeparateFromPresentation(string name)
        {
            var owner = Add(name); var npc = Add("Villager", X - 1, Y); npc.SetTag("AllowIdleBehavior");
            var brain = npc.GetPart<BrainPart>(); Assert.NotNull(brain); brain.CurrentZone = zone;
            var probe = new EventProbe(); owner.AddPart(probe); owner.Parts.Remove(probe); owner.Parts.Insert(0, probe);
            NativeOwner(owner); Assert.False(Occupied(owner)); var body = View().ForegroundTile;
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); int goals = brain.GoalCount; var offer = IdleQueryEvent.QueryOffer(owner, npc);
                Assert.NotNull(offer); Assert.AreSame(owner, offer.Source); Assert.AreEqual(X, offer.TargetX); Assert.AreEqual(Y, offer.TargetY);
                Assert.True(Occupied(owner)); Assert.AreEqual(1, probe.IdleQueries);
                for (int i = 0; i < 3; i++) { NativeOwner(owner); portraits.GetPortrait(Document(owner, npc), paint.Renderer); }
                Assert.AreEqual(1, probe.IdleQueries); Assert.AreEqual(0, probe.InventoryActions); Assert.AreEqual(goals, brain.GoalCount);
                Assert.True(Occupied(owner)); Assert.AreSame(body, View().ForegroundTile);
                Assert.NotNull(offer.Cleanup); offer.Cleanup(null); Assert.False(Occupied(owner)); NativeOwner(owner);
                Assert.AreSame(body, View().ForegroundTile); Assert.IsNull(npc.GetPart<StatusEffectsPart>()?.GetEffect<SittingEffect>());
            }
        }

        [TestCase("missing-service")] [TestCase("foreign-service")] [TestCase("duplicate-service")]
        [TestCase("cross-role")] [TestCase("foreign-render")] [TestCase("duplicate-render")]
        [TestCase("foreign-physics")] [TestCase("solid")] [TestCase("takeable")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("glyph")]
        [TestCase("variant")] [TestCase("visual")] [TestCase("layer")]
        [TestCase("actor")]
        public void UnsupportedOwnerCannotBypassThroughOldEastArt(string fault)
        {
            foreach (string name in new[] { "Bed", "Chair" })
            {
                zone = Attach("Overworld.12.11.0"); var owner = Add(name); NativeOwner(owner);
                switch (fault)
                {
                    case "missing-service": Assert.True(owner.RemovePart(Service(owner))); break;
                    case "foreign-service": Service(owner).ParentEntity = new Entity(); break;
                    case "duplicate-service": owner.AddPart(name == "Bed" ? (Part)new BedPart() : new ChairPart()); break;
                    case "cross-role": owner.AddPart(name == "Bed" ? (Part)new ChairPart() : new BedPart()); break;
                    case "foreign-render": owner.GetPart<RenderPart>().ParentEntity = new Entity(); break;
                    case "duplicate-render": owner.AddPart(new RenderPart { RenderString = owner.GetPart<RenderPart>().RenderString }); break;
                    case "foreign-physics": owner.GetPart<PhysicsPart>().ParentEntity = new Entity(); break;
                    case "solid": owner.GetPart<PhysicsPart>().Solid = true; break;
                    case "takeable": owner.GetPart<PhysicsPart>().Takeable = true; break;
                    case "carried": owner.GetPart<PhysicsPart>().InInventory = new Entity(); break;
                    case "equipped": owner.GetPart<PhysicsPart>().Equipped = new Entity(); break;
                    case "glyph": owner.GetPart<RenderPart>().RenderString = "!"; break;
                    case "variant": owner.GetPart<RenderPart>().VisualVariant = "custom"; break;
                    case "visual": owner.GetPart<RenderPart>().VisualID = "other"; break;
                    case "layer": owner.GetPart<RenderPart>().RenderLayer = 4; break;
                    case "actor": owner.AddPart(new BrainPart()); break;
                }
                using (var art = Construct(Resource())) { Assert.False(Resolver(art)(owner, out var rejected)); Assert.IsNull(rejected); }
                Assert.False(View().NativeForeground, name + " / " + fault);
                if (fault != "foreign-render")
                {
                    if (world.TryGetSubjectSprite(owner, X, Y, out var subject, out _)) Assert.AreNotSame(Resource(), subject.texture);
                    Assert.True(world.TryResolve(zone, X, Y, out var legacy));
                    if (legacy.ForegroundTile != null) Assert.AreNotSame(Resource(), legacy.ForegroundTile.sprite.texture);
                }
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ExplicitMissingOrMalformedSharedInputCannotUseValidOldEastFallback(int resourceFault)
        {
            var valid = Texture(); var invalid = resourceFault == 0 ? null : resourceFault == 1 ? Texture(287, 24) : Texture(288, 23);
            using (var positive = Construct(valid)) foreach (string name in new[] { "Bed", "Chair" })
            { Assert.True(Resolver(positive)(content.Factory.CreateEntity(name), out var tile)); Native(tile, name, valid); }
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            Write(world, "_eastKitchenArt", East(valid)); Write(world, "_sharedFurnitureArt", Construct(invalid));
            zone = Attach("Overworld.12.11.0"); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var bed = Add("Bed"); var chair = Add("Chair", X + 1, Y); var forge = Add("TinkersForge", X + 2, Y);
            foreach (var owner in new[] { bed, chair })
            {
                var cell = zone.GetEntityCell(owner); var view = View(cell.X, cell.Y); Assert.AreSame(owner, view.Owner); Assert.False(view.NativeForeground);
                Assert.True(world.TryResolve(zone, cell.X, cell.Y, out var oldWorld)); Assert.AreNotSame(valid, oldWorld.ForegroundTile.sprite.texture);
                Assert.True(world.TryGetSubjectSprite(owner, cell.X, cell.Y, out var oldSubject, out _)); Assert.AreNotSame(valid, oldSubject.texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, cell.X, cell.Y, out var subject, out _)); Assert.AreNotSame(valid, subject.texture);
            }
            Assert.AreSame(forge, View(X + 2, Y).Owner); Assert.True(View(X + 2, Y).NativeForeground);
            Assert.AreSame(valid, View(X + 2, Y).ForegroundTile.sprite.texture, "Other existing East roles retain their independent resource.");
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); foreach (var owner in new[] { bed, chair })
                { var cell = zone.GetEntityCell(owner); Assert.AreNotSame(valid, paint.At(cell.X, cell.Y).sprite.texture); Assert.AreNotSame(valid, portraits.GetPortrait(Document(owner), paint.Renderer).Texture); }
            }
        }

        [TestCase("Bed")] [TestCase("Chair")]
        public void ExistingInjectedEastAtlasSuppliesFurnitureWithoutImplicitResources(string name)
        {
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); var atlas = Texture(); Write(world, "_eastKitchenArt", East(atlas));
            Assert.False((bool)Read(world, "_loadOptionalResources"));
            foreach (string id in new[] { "Overworld.12.11.0", "Overworld.15.7.0" })
            { zone = Attach(id); var owner = Add(name); NativeOwner(owner, atlas); Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _)); Assert.AreSame(atlas, sprite.texture); }
        }

        [Test] public void OtherFurnitureAndSpoofedNamesDoNotAcquireTheseExactBodies()
        {
            using (var art = Construct(Resource()))
            {
                Assert.True(Resolver(art)(content.Factory.CreateEntity("Bed"), out _));
                Assert.False((bool)AdapterType().GetMethod("HasKnownRole").Invoke(null, new object[] { null }));
                foreach (string name in new[] { "CreatureCorpse", "Chest", "Campfire" })
                {
                    var owner = content.Factory.CreateEntity(name); Assert.NotNull(owner); owner.SetTag("Furniture"); owner.AddPart(new BedPart());
                    owner.GetPart<RenderPart>().RenderString = "="; owner.Properties["SourceBlueprint"] = "Bed";
                    Assert.False((bool)AdapterType().GetMethod("HasKnownRole").Invoke(null, new object[] { owner })); Assert.False(Resolver(art)(owner, out _));
                }
            }
        }

        [Test] public void MulticellRefusesOneCellBodyAndRestoringFootprintRecovers()
        {
            var owner = Add("Bed"); NativeOwner(owner); owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" });
            Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0;0,1;1,1")); Assert.AreEqual(4, zone.GetOccupiedCells(owner).Count);
            Assert.False(View().NativeForeground); Assert.False(View(X + 1, Y + 1).NativeForeground);
            Assert.True(zone.TryChangeFootprint(owner, "0,0")); NativeOwner(owner);
        }

        [Test] public void DisclosureCoverMoveAndRemovalRetireTheAuthorizedReader()
        {
            var owner = Add("Chair"); NativeOwner(owner); var document = Document(owner);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); var body = paint.At(X, Y); var cell = zone.GetCell(X, Y);
                cell.IsVisible = false; paint.Dirty(X, Y); paint.Step(); Assert.False(View().NativeForeground); Assert.IsNull(portraits.GetPortrait(document, paint.Renderer).Texture);
                cell.Explored = false; Assert.False(world.TryResolvePresentation(zone, X, Y, out _)); cell.Explored = cell.IsVisible = true;
                var cover = Add("Player"); paint.Dirty(X, Y); paint.Step(); Assert.AreSame(cover, View().Owner); Assert.AreNotSame(body, paint.At(X, Y));
                Assert.True(zone.RemoveEntity(cover)); paint.Dirty(X, Y); paint.Step(); Assert.AreSame(body, paint.At(X, Y));
                Assert.True(zone.MoveEntity(owner, X + 1, Y)); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); document = Document(owner);
                Assert.True(zone.RemoveEntity(owner)); paint.Dirty(X, Y); paint.Dirty(X + 1, Y); paint.Step(); Assert.AreNotSame(body, paint.At(X + 1, Y));
                Assert.IsNull(portraits.GetPortrait(document, paint.Renderer).Texture);
            }
        }

        [Test] public void StaleMembershipAndReplacementGraphCannotGrantFurnitureAuthority()
        {
            var owner = Add("Bed"); NativeOwner(owner); Assert.True(zone.MoveEntity(owner, X + 1, Y)); zone.GetCell(X, Y).Objects.Add(owner);
            Assert.IsNull(View().Owner); NativeOwner(owner);
            manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID);
            Assert.False(world.TryResolvePresentation(zone, X + 1, Y, out _)); Assert.False(world.TryGetPresentationSubjectSprite(zone, owner, X + 1, Y, out _, out _));
        }

        [Test] public void ActualChangedRenderCueIsDispatchedOnceAndReaderNeighborQueriesDoNotConsumeIt()
        {
            var owner = Add("Chair"); NativeOwner(owner); var probe = new EventProbe(); owner.AddPart(probe);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); var body = paint.At(X, Y); Assert.AreEqual(Color.white, paint.Tint(X, Y));
                int before = probe.Renders; probe.OverrideColor = "&R"; paint.Dirty(X, Y); paint.Step();
                Assert.AreEqual(before + 1, probe.Renders); Assert.AreSame(body, paint.At(X, Y)); Assert.AreEqual(QudColorParser.Parse("&R"), paint.Tint(X, Y));
                portraits.GetPortrait(Document(owner), paint.Renderer); NativeOwner(owner); paint.Dirty(X + 1, Y); paint.Step();
                Assert.AreEqual(before + 1, probe.Renders); Assert.AreEqual(QudColorParser.Parse("&R"), paint.Tint(X, Y));
                probe.OverrideColor = null; paint.Dirty(X, Y); paint.Step(); Assert.AreEqual(before + 2, probe.Renders); Assert.AreEqual(Color.white, paint.Tint(X, Y));
            }
        }

        [Test] public void CachedViewsDisposeIndependentlyWithoutChangingBorrowedAtlas()
        {
            var atlas = Texture(); var owner = content.Factory.CreateEntity("Bed"); var art = Construct(atlas);
            using (var peer = Construct(atlas))
            {
                try
                {
                    Assert.True(Resolver(art)(owner, out var first)); Assert.True(Resolver(art)(owner, out var again)); Assert.AreSame(first, again);
                    art.Dispose(); art.Dispose(); Assert.True(first == null); Assert.False(Resolver(art)(owner, out _));
                    Assert.True(Resolver(peer)(owner, out var other)); Native(other, "Bed", atlas);
                    Assert.True(atlas != null); Assert.AreEqual(FilterMode.Bilinear, atlas.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, atlas.wrapMode);
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Shared furniture submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Shared furniture fixture");
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
