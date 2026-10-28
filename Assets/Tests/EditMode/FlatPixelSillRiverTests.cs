// Contract-first Sill river substrate. Reflection permits native RED before the companion exists.
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
    public sealed class FlatPixelSillRiverTests
    {
        const string Home = "Overworld.10.10.0", Atlas = "Sprites/Live16x24/opening-ground";
        const int X = 39, Y = 20;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Vector2Int[] Cardinal = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelFallbackCatalog borrowedSelector;
        readonly List<Texture2D> textures = new List<Texture2D>();
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach(Home); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone));
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); borrowedSelector?.Dispose(); foreach (var t in textures) if (t != null) Object.DestroyImmediate(t); textures.Clear(); }
            finally { content?.Dispose(); }
        }
        Zone Attach(string id)
        { var z = new Zone(id); manager.SetActiveZone(z); foreach (var c in z.Cells) c.Explored = c.IsVisible = true; return z; }
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e, x, y)); return e; }
        Entity Pool(int x = X, int y = Y)
        { var e = Add("WaterPuddle", x, y); Assert.AreEqual(120, e.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(ZoneTileState.Permanent, zone.TileState.CoatingTurns(x, y, "water")); return e; }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var v)); return v; }
        static object Read(object o, string name)
        { var f = o.GetType().GetField(name, I); Assert.NotNull(f, name); return f.GetValue(o); }
        static void Write(object o, string name, object v)
        { var f = o.GetType().GetField(name, I); Assert.NotNull(f, name); f.SetValue(o, v); }
        static Type Adapter()
        { var t = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSillRiverCatalog"); Assert.NotNull(t, "Exact Sill river companion is not implemented."); return t; }
        object Art()
        { Adapter(); View(); var a = Read(world, "_sillRiverArt"); Assert.NotNull(a); return a; }
        static bool Call(object o, string method, object[] args)
        { var m = o.GetType().GetMethod(method, I); Assert.NotNull(m, method); return (bool)m.Invoke(o, args); }
        bool Water(out Tile tile, out Entity source, int x = X, int y = Y)
        { object[] args = { zone, x, y, null, null }; bool ok = Call(Art(), "TryResolveWaterGround", args); tile = (Tile)args[3]; source = (Entity)args[4]; return ok; }
        bool Subject(Entity source)
        { var cell = zone.GetEntityCell(source); object[] args = { zone, source, cell?.X ?? X, cell?.Y ?? Y, null }; return Call(Art(), "TryGetSubjectSprite", args); }
        bool Supported(Zone z) => (bool)Adapter().GetMethod("IsSupportedZone", S).Invoke(null, new object[] { z });
        static Texture2D Resource()
        { var t = Resources.Load<Texture2D>(Atlas); Assert.NotNull(t); Assert.AreEqual(336, t.width); Assert.AreEqual(24, t.height); return t; }
        static void Native(Tile t, int mask, Texture2D texture = null)
        {
            Assert.NotNull(t); Assert.NotNull(t.sprite); Assert.AreSame(texture ?? Resource(), t.sprite.texture);
            Assert.AreEqual(new Rect(mask * 16, 0, 16, 24), t.sprite.rect); Assert.AreEqual(new Vector2(8, 12), t.sprite.pivot);
            Assert.AreEqual(16, t.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(1, 1.5f), (Vector2)t.sprite.bounds.size);
            Assert.AreEqual(Tile.ColliderType.None, t.colliderType);
        }
        Tile NativeWater(Entity expected, int mask = 15)
        {
            var v = View(); Native(v.GroundTile, mask); Assert.True(v.LocalGround); Assert.AreEqual(FlatPixelGreyboxGround.Water, v.GroundKind);
            Assert.True(Water(out var tile, out var source)); Assert.AreSame(expected, source); Assert.AreSame(v.GroundTile, tile); return tile;
        }
        void Refused()
        { Assert.False(Water(out var tile, out var source)); Assert.IsNull(tile); Assert.IsNull(source); Assert.AreNotSame(Resource(), View().GroundTile.sprite.texture); }
        static ExaminePortraitRenderer Portraits() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I, null,
            new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        AnnouncementDocument Document(Entity e)
        { var d = ExamineDocumentBuilder.BuildWorld(e, zone, zone.GetEntityCell(e)); Assert.NotNull(d); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(d)); return d; }
        IDisposable Construct(Texture2D atlas, FlatPixelFallbackCatalog selector)
        {
            var ctor = Adapter().GetConstructor(new[] { typeof(Texture2D), typeof(FlatPixelFallbackCatalog) }); Assert.NotNull(ctor);
            return (IDisposable)ctor.Invoke(new object[] { atlas, selector });
        }

        [Test] public void VisibleActualWaterKeepsGrassActorReaderAndSimulationIdentity()
        {
            var grass = Add("Grass"); var source = Pool(); string state = zone.TileState.ToSaveString(); var body = NativeWater(source);
            Assert.AreSame(source, View().Owner); Assert.IsNull(View().ForegroundTile, "Suppress only the represented selected pool glyph."); Assert.True(Subject(source));
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); Native(paint.GroundAt(X, Y), 15); Assert.IsNull(paint.At(X, Y)); Assert.IsNull(paint.Mark);
                var doc = Document(source); var portrait = portraits.GetPortrait(doc, paint.Renderer);
                Assert.AreSame(Resource(), portrait.Texture); Assert.AreEqual(new Rect(15f / 21f, 0, 1f / 21f, 1), portrait.Uv);
                var actor = Add("Player"); paint.Dirty(X, Y); paint.Step();
                Assert.AreSame(actor, View().Owner); Assert.NotNull(paint.At(X, Y)); Assert.AreSame(body, paint.GroundAt(X, Y)); Assert.False(Subject(source));
                Assert.True(zone.RemoveEntity(actor)); paint.Dirty(X, Y); paint.Step(); Assert.IsNull(paint.At(X, Y)); Assert.True(Subject(source));
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(120, source.GetPart<LiquidPoolPart>().Volume);
                Assert.True(zone.GetCell(X, Y).Objects.Contains(grass)); Assert.False(source.GetPart<PhysicsPart>().Solid);
            }
        }

        [Test] public void InitializedOrdinaryWaterDefinitionColorRetainsNativeWorldAndReader()
        {
            var definition = Resources.Load<TextAsset>("Content/Data/LiquidDefinitions/water");
            Assert.NotNull(definition);
            var liquid = JsonUtility.FromJson<LiquidDefinitionCollection>(definition.text).Liquids[0];
            Assert.AreEqual("water", liquid.Id);
            Assert.AreEqual("~", liquid.Glyph);
            Assert.AreEqual("&c", liquid.Color);
            var source = Pool(); NativeWater(source); // Blueprint-authored before registry initialization.
            var render = source.GetPart<RenderPart>();
            render.RenderString = liquid.Glyph; render.ColorString = liquid.Color;
            NativeWater(source); Assert.True(Subject(source));
            render.ColorString = "&R"; Refused(); Assert.False(Subject(source));
        }

        [Test] public void AllSixteenBanksUseDisclosedDryBitsNotWaterConnections()
        {
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(Home); var source = Pool();
                for (int i = 0; i < 4; i++)
                { int x = X + Cardinal[i].x, y = Y + Cardinal[i].y; Add("Grass", x, y); if ((mask & (1 << i)) == 0) Pool(x, y); }
                NativeWater(source, mask);
            }
        }

        [Test] public void HiddenUnknownOrMixedNeighborCannotRevealABankAndCoveringActorDoesNotChangeIt()
        {
            var source = Pool(); NativeWater(source); var north = zone.GetCell(X, Y - 1);
            north.IsVisible = false; NativeWater(source, 14); north.Explored = false; NativeWater(source, 14);
            var northPool = Pool(X, Y - 1); NativeWater(source, 14); north.Explored = north.IsVisible = true; NativeWater(source, 14);
            Assert.True(zone.RemoveEntity(northPool)); zone.TileState.Clear(X, Y - 1); zone.TileState.WriteCoating(X, Y - 1, "oil", 2); NativeWater(source, 14);
            zone.TileState.Clear(X, Y - 1); var actor = Add("Player", X, Y - 1); NativeWater(source, 15);
            Assert.True(zone.RemoveEntity(actor)); NativeWater(source, 15);
        }

        [Test] public void EmptyAlteredThermalMaterialRenderOrForeignRoleCannotUseWaterBody()
        {
            foreach (string fault in new[] { "empty", "negative", "liquid", "frozen", "vapor", "nan", "material", "flammable", "glyph", "color", "background", "variant", "visual", "layer", "crop", "source", "solid", "takeable", "carried", "equipped", "hidden", "footprint" })
            {
                zone = Attach(Home); var source = Pool(); NativeWater(source);
                var pool = source.GetPart<LiquidPoolPart>(); var thermal = source.GetPart<ThermalPart>(); var render = source.GetPart<RenderPart>(); var physics = source.GetPart<PhysicsPart>();
                switch (fault)
                {
                    case "empty": pool.Volume = 0; break;
                    case "negative": pool.Volume = -1; break;
                    case "liquid": pool.LiquidId = "brine"; break;
                    case "frozen": thermal.Temperature = thermal.FreezeTemperature; break;
                    case "vapor": thermal.Temperature = thermal.VaporTemperature; break;
                    case "nan": thermal.Temperature = float.NaN; break;
                    case "material": source.GetPart<MaterialPart>().MaterialID = "Stone"; break;
                    case "flammable": source.GetPart<MaterialPart>().Combustibility = .5f; break;
                    case "glyph": render.RenderString = "."; break;
                    case "color": render.ColorString = "&R"; break;
                    case "background": render.BackgroundColor = "^r"; break;
                    case "variant": render.VisualVariant = "custom"; break;
                    case "visual": render.VisualID = "reference-glade-low-wall"; break;
                    case "layer": render.RenderLayer = 5; break;
                    case "crop": source.AddPart(new CropPart()); break;
                    case "source": source.AddPart(new TileStateSourcePart()); break;
                    case "solid": physics.Solid = true; break;
                    case "takeable": physics.Takeable = true; break;
                    case "carried": physics.InInventory = new Entity(); break;
                    case "equipped": physics.Equipped = new Entity(); break;
                    case "hidden": render.Visible = false; break;
                    case "footprint": source.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" }); Assert.True(zone.TryChangeFootprint(source, "0,0;1,0")); break;
                }
                Refused(); Assert.False(Subject(source), fault);
            }
        }

        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void SourcePartsMustBeUniqueAndOwned(string fault)
        {
            foreach (var type in new[] { typeof(RenderPart), typeof(PhysicsPart), typeof(ExaminablePart), typeof(LiquidPoolPart), typeof(MaterialPart), typeof(ThermalPart) })
            {
                zone = Attach(Home); var source = Pool(); NativeWater(source); var part = source.Parts.Single(p => p.GetType() == type);
                if (fault == "missing") Assert.True(source.RemovePart(part)); else if (fault == "foreign") part.ParentEntity = new Entity();
                else source.AddPart((Part)Activator.CreateInstance(type)); Refused();
            }
        }

        [Test] public void WaterLookingCoatingAloneMixedTemporaryProjectedOrDuplicateSourceRefuses()
        {
            foreach (string fault in new[] { "absent", "temporary", "projected", "mixed", "retained-zero", "duplicate", "removed", "stale" })
            {
                zone = Attach(Home); var source = Pool(); NativeWater(source);
                switch (fault)
                {
                    case "absent": zone.TileState.Clear(X, Y); break;
                    case "temporary": zone.TileState.Clear(X, Y); zone.TileState.WriteCoating(X, Y, "water", 2); break;
                    case "projected": zone.TileState.Get(X, Y).Coatings[0].ProjectionOwnerId = source.ID; break;
                    case "mixed": zone.TileState.WriteCoating(X, Y, "oil", 3); break;
                    case "retained-zero": zone.TileState.Get(X, Y).Coatings.Add(new ZoneTileState.Layer { Id = "oil", Turns = 0 }); break;
                    case "duplicate": Pool(); break;
                    case "removed": Assert.True(zone.RemoveEntity(source)); zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent); break;
                    case "stale": Assert.True(zone.MoveEntity(source, X + 2, Y)); zone.GetCell(X, Y).Objects.Add(source); zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent); break;
                }
                Refused();
            }
        }

        [Test] public void VisibleCompetingSubstrateRefusesWhileHiddenCustodiedOrForeignContentDoesNotLeak()
        {
            foreach (var name in new[] { "StoneFloor", "SandstoneFloor", "Grass" })
            {
                zone = Attach(Home); var source = Pool(); NativeWater(source); var other = Add(name);
                if (name == "Grass") other.AddPart(new CultivatedSoilPart());
                Refused(); other.GetPart<RenderPart>().Visible = false; NativeWater(source);
                other.GetPart<RenderPart>().Visible = true; Refused(); other.GetPart<PhysicsPart>().InInventory = new Entity(); NativeWater(source);
                other.GetPart<PhysicsPart>().InInventory = null; other.GetPart<RenderPart>().ParentEntity = new Entity(); NativeWater(source);
            }
        }

        [Test] public void OnlyCurrentCanonicalSillWithExactLivePoiMapAndBiomeCanUseTheAdapter()
        {
            var source = Pool(); NativeWater(source); Assert.True(Supported(zone));
            var poi = manager.WorldMap.GetPOI(10, 10); string name = poi.Name; poi.Name = "Other"; Assert.False(Supported(zone)); Refused(); poi.Name = name;
            var biome = manager.WorldMap.Tiles[10, 10]; manager.WorldMap.Tiles[10, 10] = BiomeType.Stump; Assert.False(Supported(zone)); manager.WorldMap.Tiles[10, 10] = biome; NativeWater(source);
            foreach (string id in new[] { "Overworld.10.10.1", "Overworld.12.10.0", "Overworld.010.10.0", "StandaloneRiver" })
            { zone = Attach(id); Pool(); Assert.False(Supported(zone)); }
            zone = Attach(Home); source = Pool(); NativeWater(source); manager.CachedZones[Home] = new Zone(Home);
            Assert.False(Supported(zone)); Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
        }

        [Test] public void FogAndRemovedOwnerRetireAuthorizedReaderWithoutLivePoolDisclosure()
        {
            var source = Pool(); NativeWater(source); var doc = Document(source);
            using (var paint = new Paint(world, zone)) using (var portraits = Portraits())
            {
                paint.Step(); Assert.AreSame(Resource(), portraits.GetPortrait(doc, paint.Renderer).Texture);
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step(); Refused(); Assert.IsNull(portraits.GetPortrait(doc, paint.Renderer).Texture);
                zone.GetCell(X, Y).Explored = false; Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
                zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true; NativeWater(source);
                Assert.True(zone.RemoveEntity(source)); paint.Step(); Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc)); Assert.IsNull(portraits.GetPortrait(doc, paint.Renderer).Texture);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedAtlasKeepsActualWaterMarkAndNeverReloads(bool malformed)
        {
            var source = Pool(); NativeWater(source); Texture2D bad = null;
            if (malformed) { bad = new Texture2D(256, 24); textures.Add(bad); }
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); borrowedSelector = new FlatPixelFallbackCatalog();
            Write(world, "_sillRiverArt", Construct(bad, borrowedSelector)); Refused();
            using (var paint = new Paint(world, zone))
            { paint.Step(); Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark); Assert.AreSame(source, View().Owner); }
        }

        [TestCase("heat", '^')] [TestCase("charge", '*')] [TestCase("cloud", '\'')] [TestCase("residue", '"')]
        public void SubmittedWaterSuppressesOnlySoleWaterAndKeepsIndependentHazard(string hazard, char glyph)
        {
            Add("Grass"); var source = Pool(); NativeWater(source);
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); Native(paint.GroundAt(X, Y), 15); Assert.IsNull(paint.Mark);
                if (hazard == "heat") zone.TileState.AddHeat(X, Y, 1);
                if (hazard == "charge") zone.TileState.AddCharge(X, Y, 1);
                if (hazard == "cloud") zone.TileState.WriteCloud(X, Y, "veil-mist", 3);
                if (hazard == "residue") zone.TileState.WriteResidue(X, Y, "grit", 3);
                string saved = zone.TileState.ToSaveString(); paint.Step(); NativeWater(source); Native(paint.GroundAt(X, Y), 15);
                Assert.AreSame(CP437TilesetGenerator.GetTile(glyph), paint.Mark); Assert.AreEqual(saved, zone.TileState.ToSaveString());
                zone.TileState.WriteCoating(X, Y, "oil", 3); paint.Step(); Refused(); Assert.NotNull(paint.Mark);
            }
        }

        [Test] public void CachedFramesAreOwnedButAtlasAndSelectorRemainBorrowed()
        {
            var source = Pool(); source.GetPart<ThermalPart>().Temperature = 27f; var atlas = Resource(); using (var selector = new FlatPixelFallbackCatalog())
            {
                var art = Construct(atlas, selector); object[] args = { zone, X, Y, null, null };
                try
                {
                    Assert.True(Call(art, "TryResolveWaterGround", args)); var tile = (Tile)args[3]; Native(tile, 15);
                    Assert.True(Call(art, "TryResolveWaterGround", args)); Assert.AreSame(tile, args[3]); art.Dispose(); art.Dispose(); Assert.True(tile == null);
                    Assert.False(Call(art, "TryResolveWaterGround", args)); Assert.True(atlas != null); Assert.True(selector.TryResolve(zone, X, Y, out var selected)); Assert.AreSame(source, selected.Owner);
                    using (var noSelector = Construct(atlas, null)) Assert.False(Call(noSelector, "TryResolveWaterGround", args));
                }
                finally { art.Dispose(); }
            }
        }
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, main, ground;
            Tilemap marks;
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Sill river submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var hazardObject = new GameObject("Hazards", typeof(Tilemap)); hazardObject.transform.SetParent(root.transform, false); marks = hazardObject.GetComponent<Tilemap>();
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", ground); Write(Renderer, "_tileStateTilemap", marks);
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
            public TileBase Mark => marks.GetTile(Position(X, Y));
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(Position(x, y));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Sill river fixture");
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
