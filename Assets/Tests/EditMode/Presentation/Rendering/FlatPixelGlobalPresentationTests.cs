using System;
using System.Collections.Generic;
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
    // G1.2 integration RED: uses the real Awake graph and full/dirty rendering.
    // Native test results and ordinary-input Play acceptance are separate gates.
    public sealed class FlatPixelGlobalPresentationTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const string Outside = "Overworld.12.10.0";
        const int X = 10, Y = 10;
        static readonly Vector3Int At = Position(X, Y);
        EntityFactory factory;
        [OneTimeSetUp] public void Load() => factory = DensityReferenceGladeTests.Factory();

        [Test]
        public void RequestedModeKeepsProjectionAcrossLocalOtherBiomeAndUndergroundGraphs()
        {
            using (var f = new Fixture(factory))
            {
                foreach (string id in new[] { ReferenceGladePlan.ZoneID, Outside, "Overworld.12.12.0", "Overworld.12.12.1", ReferenceGladePlan.ZoneID })
                {
                    f.Show(id);
                    if (id == "Overworld.12.12.0") f.Manager.WorldMap.Tiles[12, 12] = BiomeType.Sodden;
                    f.Frame();
                    Assert.True(f.Renderer.FlatPixelModeActive, id);
                    Assert.AreEqual(new Vector3(1, 1.5f, f.OriginalCellSize.z), f.Grid.cellSize, id);
                    Assert.AreSame(f.Main, f.Follow.WorldTilemap, id);
                    Assert.AreEqual(id == ReferenceGladePlan.ZoneID, FlatPixelGreyboxCatalog.IsAdmitted(f.Zone),
                        "Continuous presentation must not broaden the native regional admission contract.");
                }
                f.Renderer.FlatPixelModeEnabled = false;
                Assert.AreEqual(f.OriginalCellSize, f.Grid.cellSize);
                Assert.IsNull(f.Follow.WorldTilemap);
            }
        }

        [Test]
        public void FullscreenAndExplicitOffRoundTripsRestoreWorldWithoutOverwritingUiTwice()
        {
            using (var f = new Fixture(factory))
            {
                f.Add("UnknownMarker", "A"); f.Frame();
                for (int i = 0; i < 2; i++)
                {
                    f.Renderer.Paused = true;
                    Assert.AreEqual(f.OriginalCellSize, f.Grid.cellSize);
                    f.Main.ClearAllTiles(); var ui = CP437TilesetGenerator.GetUiTile('U'); f.Main.SetTile(At, ui);
                    f.Frame(); Assert.AreSame(ui, f.Main.GetTile(At), "Paused world must not repaint the borrowed UI canvas.");
                    f.Renderer.Paused = false; f.Renderer.MarkDirty("fullscreen return"); f.Frame();
                    Assert.AreEqual(new Vector3(1, 1.5f, f.OriginalCellSize.z), f.Grid.cellSize); AssertGlyph(f.Foreground(), 'A');
                    f.Renderer.FlatPixelModeEnabled = false; f.Frame();
                    Assert.AreEqual(f.OriginalCellSize, f.Grid.cellSize); Assert.IsNull(f.Follow.WorldTilemap);
                    f.Renderer.FlatPixelModeEnabled = true; f.Frame(); AssertGlyph(f.Foreground(), 'A');
                }
            }
        }

        [TestCase(Outside)] [TestCase(ReferenceGladePlan.ZoneID)]
        public void UncoveredIdentitiesKeepDifferentRealGlyphsWithinAndOutsideNativeScope(string zone)
        {
            using (var f = new Fixture(factory, zone))
            {
                f.Add("UnfamiliarCreatureA", "A").SetTag("Creature");
                f.Add("UnfamiliarCreatureB", "B", X + 1, Y).SetTag("Creature"); f.Frame();
                AssertGlyph(f.Foreground(), 'A'); AssertGlyph(f.Foreground(X + 1, Y), 'B');
                Assert.AreNotSame(f.Foreground(), f.Foreground(X + 1, Y));
                Assert.Zero(f.Actors.ActiveViewCount, "Unknown creatures must not acquire a borrowed native or legacy body.");
            }
        }

        [Test]
        public void FullDirtyAndNeighborClaimPathsPreserveOneDamageRenderEventAndLighting()
        {
            using (var f = new Fixture(factory, ReferenceGladePlan.ZoneID))
            {
                var owner = f.Add("UnknownCombatant", "A"); var probe = new RenderProbe(); owner.AddPart(probe);
                owner.AddPart(new DamageFlashPart()); var damage = GameEvent.New("TakeDamage");
                try { owner.FireEvent(damage); } finally { damage.Release(); }
                f.Frame(); Assert.AreEqual(1, probe.Calls); AssertGlyph(f.Foreground(), 'A');
                f.AssertLit(QudColorParser.BrightRed);
                f.Renderer.MarkCellDirty(X + 1, Y, "neighbor claim"); f.Frame();
                Assert.AreEqual(1, probe.Calls, "Reclaiming a neighbor must not consume another flash frame."); f.AssertLit(QudColorParser.BrightRed);
                for (int i = 0; i < 2; i++)
                { f.Renderer.MarkCellDirty(X, Y, "flash"); f.Frame(); Assert.AreEqual(i + 2, probe.Calls); f.AssertLit(QudColorParser.BrightRed); }
                f.Renderer.MarkCellDirty(X, Y, "flash ended"); f.Frame();
                Assert.AreEqual(4, probe.Calls); f.AssertLit(QudColorParser.BrightGreen);
                for (int i = 0; i < 3; i++) f.Frame();
                Assert.AreEqual(4, probe.Calls, "Quiet frames do not dispatch gameplay Render events.");
            }
        }

        [Test]
        public void ValidLowerBackgroundFillsAllTwentyFourRowsAndReceivesLightingOnce()
        {
            using (var f = new Fixture(factory))
            {
                var ground = f.Add("UnknownFloor", ".", X, Y, 0); ground.SetTag("Terrain");
                ground.GetPart<RenderPart>().BackgroundColor = "^b";
                f.Add("UnknownUpperOwner", "A", X, Y, 20); f.Frame();
                var tile = f.Background.GetTile<Tile>(At); Assert.NotNull(tile); Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
                var pixels = SpritePixels(tile.sprite); foreach (var pixel in pixels) Assert.AreEqual(255, pixel.a);
                AssertColor(f.Lit(QudColorParser.DarkenForBackground(QudColorParser.DarkBlue)), f.Background.GetColor(At));
                Assert.AreEqual(Matrix4x4.identity, f.Background.GetTransformMatrix(At));
            }
        }

        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("carried")]
        public void InvalidTopOwnerCannotDonateItsGlyphColorOrRenderEvent(string fault)
        {
            using (var f = new Fixture(factory, ReferenceGladePlan.ZoneID))
            {
                var lower = f.Add("UnknownFloor", ".", X, Y, 0); lower.SetTag("Terrain"); var good = new RenderProbe(); lower.AddPart(good);
                var upper = f.Add("GhostOwner", "B", X, Y, 99); var bad = new RenderProbe { Color = "&R" }; upper.AddPart(bad);
                if (fault == "foreign-render") upper.GetPart<RenderPart>().ParentEntity = new Entity();
                if (fault == "foreign-physics") upper.GetPart<PhysicsPart>().ParentEntity = new Entity();
                if (fault == "carried") upper.GetPart<PhysicsPart>().InInventory = new Entity();
                f.Frame(); Assert.Zero(bad.Calls); Assert.AreEqual(1, good.Calls); AssertGlyph(f.Foreground(), '.'); f.AssertLit(QudColorParser.BrightGreen);
            }
        }

        [Test]
        public void CanonicalRenderOnlyOwnerIsNotMistakenForAMalformedCreature()
        {
            using (var f = new Fixture(factory))
            {
                var owner = f.Add("RenderOnlyMarker", "A"); owner.RemovePart(owner.GetPart<PhysicsPart>());
                var probe = new RenderProbe(); owner.AddPart(probe); f.Frame();
                AssertGlyph(f.Foreground(), 'A'); Assert.AreEqual(1, probe.Calls);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void FogNeverDispatchesRenderOrDisclosesALowLayerLiveOwner(bool explored)
        {
            using (var f = new Fixture(factory))
            {
                var terrain = f.Add("UnknownWall", "#", X, Y, 0); terrain.SetTag("Terrain"); var groundProbe = new RenderProbe(); terrain.AddPart(groundProbe);
                var living = f.Add("UnknownCreature", "A", X, Y, 1); living.SetTag("Creature"); var liveProbe = new RenderProbe(); living.AddPart(liveProbe);
                f.Zone.GetCell(X, Y).IsVisible = false; f.Zone.GetCell(X, Y).Explored = explored; f.Frame();
                Assert.Zero(liveProbe.Calls); Assert.Zero(groundProbe.Calls);
                if (explored) AssertGlyph(f.Foreground(), '#'); else Assert.IsNull(f.Foreground());
                f.Zone.GetCell(X, Y).IsVisible = f.Zone.GetCell(X, Y).Explored = true;
                f.Renderer.MarkCellDirty(X, Y, "reveal"); f.Frame(); AssertGlyph(f.Foreground(), 'A'); Assert.AreEqual(1, liveProbe.Calls);
            }
        }

        [Test]
        public void UnsupportedUnicodeThroughActualRendererDoesNotRebuildSharedWorldOrUiBanks()
        {
            using (var f = new Fixture(factory))
            {
                var bank = CP437TilesetGenerator.GetAtlasTexture(); var world = CP437TilesetGenerator.GetTile('A'); var ui = CP437TilesetGenerator.GetUiTile('A');
                f.Add("UnknownSnowflake", "\u2603"); f.Frame(); AssertGlyph(f.Foreground(), '?');
                Assert.AreSame(bank, CP437TilesetGenerator.GetAtlasTexture()); Assert.AreSame(world, CP437TilesetGenerator.GetTile('A'));
                Assert.AreSame(ui, CP437TilesetGenerator.GetUiTile('A'));
            }
        }

        [Test]
        public void CoordinateVariantsAndReplacedRenderPartCannotReuseOldGlyphOrEventTint()
        {
            using (var f = new Fixture(factory))
            {
                var owner = f.Add("VariableMarker", "A"); var render = owner.GetPart<RenderPart>(); render.GlyphVariants = "AB";
                var probe = new RenderProbe { Color = "&R" }; owner.AddPart(probe); f.Frame(); AssertGlyph(f.Foreground(), render.ResolveGlyph(X, Y));
                owner.RemovePart(render); owner.RemovePart(probe);
                owner.AddPart(new RenderPart { RenderString = "B", ColorString = "&C", RenderLayer = 20 });
                f.Renderer.MarkCellDirty(X + 1, Y, "neighbor discovers replacement"); f.Frame();
                AssertGlyph(f.Foreground(), 'B'); f.AssertLit(QudColorParser.BrightCyan); Assert.AreEqual(1, probe.Calls);
                f.Renderer.MarkCellDirty(X, Y, "owner repaint"); f.Frame(); AssertGlyph(f.Foreground(), 'B'); f.AssertLit(QudColorParser.BrightCyan);
            }
        }

        [TestCase("Player", 0)] [TestCase("PetDog", 1)] [TestCase("MarlbackScrabbler", 2)] [TestCase("MarlbackGleaner", 3)]
        public void FourPortableActorsHaveMatchingNativeWorldAndIdlePortraitOutsideOpening(string identity, int idle)
        {
            using (var f = new Fixture(factory)) using (var portraits = Portraits())
            {
                var owner = f.AddActor(identity); f.Frame(); Assert.True(f.Renderer.FlatPixelModeActive);
                Assert.AreEqual(1, f.Actors.ActiveViewCount); var body = f.Body(identity); Assert.NotNull(body); Assert.IsNull(f.Foreground());
                var atlas = Resources.Load<Texture2D>(identity == "Player" ? FlatPixelOpeningArtCatalog.ResourcePath : FlatPixelOpeningArtCatalog.CastResourcePath);
                Assert.AreSame(atlas, body.sprite.texture); Assert.AreEqual(new Rect(idle * 16, 0, 16, 24), body.sprite.rect);
                var image = portraits.GetPortrait(f.Document(owner), f.Renderer); AssertPortrait(image, body.sprite); AssertNoPreview(portraits);
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(f.Zone));
            }
        }

        [Test]
        public void MissingMotionUsesAvailablePortableStaticIdleInsteadOfLosingBody()
        {
            using (var f = new Fixture(factory, motion: false))
            {
                f.AddActor("PetDog"); f.Frame(); Assert.Zero(f.Actors.ActiveViewCount);
                var sprite = f.Foreground()?.sprite; Assert.NotNull(sprite);
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath), sprite.texture);
                Assert.AreEqual(new Rect(16, 0, 16, 24), sprite.rect);
            }
        }

        [Test]
        public void MissingIdleKeepsTruthfulGlyphAndDoesNotLetMotionSuppressIt()
        {
            using (var f = new Fixture(factory, nativeIdle: false)) using (var portraits = Portraits())
            {
                var owner = f.AddActor("Player"); f.Frame(); Assert.Zero(f.Actors.ActiveViewCount); AssertGlyph(f.Foreground(), '@');
                AssertPortrait(portraits.GetPortrait(f.Document(owner), f.Renderer), f.Foreground().sprite); AssertNoPreview(portraits);
            }
        }

        [TestCase("HealingTonic")] [TestCase("MarlbackPatchbearer")]
        public void RegionalPropAndFiniteSupplyActorUseMatchingGlyphOutsideThenNativeInside(string blueprint)
        {
            using (var f = new Fixture(factory)) using (var portraits = Portraits())
            {
                var owner = factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(f.Zone.AddEntity(owner, X, Y)); f.Frame();
                var sprite = f.Foreground()?.sprite; Assert.NotNull(sprite); Assert.AreEqual(new Vector2(16, 24), sprite.rect.size);
                Assert.AreEqual(408, sprite.texture.height, "Outside native scope uses the identity-preserving glyph bank.");
                AssertPortrait(portraits.GetPortrait(f.Document(owner), f.Renderer), sprite); AssertNoPreview(portraits);
                Assert.True(f.Zone.RemoveEntity(owner)); f.Show(ReferenceGladePlan.ZoneID); Assert.True(f.Zone.AddEntity(owner, X, Y)); f.Frame();
                var native = f.Foreground()?.sprite; Assert.NotNull(native); Assert.AreEqual(24, native.texture.height);
                AssertPortrait(portraits.GetPortrait(f.Document(owner), f.Renderer), native); AssertNoPreview(portraits);
            }
        }

        [Test]
        public void CarriedRegionalItemUsesViewerZonePolicyWhileInventoryOwnsSquareCanvas()
        {
            using (var f = new Fixture(factory)) using (var portraits = Portraits())
            {
                var owner = factory.CreateEntity("HealingTonic"); Assert.True(f.Zone.AddEntity(owner, X, Y)); f.Frame();
                var worldSprite = f.Foreground().sprite; var viewer = f.Add("Viewer", "v", X - 1, Y); var inventory = new InventoryPart(); viewer.AddPart(inventory);
                Assert.True(f.Zone.RemoveEntity(owner)); Assert.True(inventory.AddObject(owner)); f.Renderer.Paused = true;
                var document = ExamineDocumentBuilder.BuildInventory(owner, viewer); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                AssertPortrait(portraits.GetPortrait(document, f.Renderer), worldSprite); AssertNoPreview(portraits);
                inventory.Objects.Remove(owner); Assert.IsNull(portraits.GetPortrait(document, f.Renderer).Texture);
            }
        }

        [Test]
        public void QuietManagerReplacementDropsPortableBodyAndRejectsOldPortraitWithoutLegacyFallback()
        {
            using (var f = new Fixture(factory)) using (var portraits = Portraits())
            {
                var owner = f.AddActor("PetDog"); f.Frame(); Assert.AreEqual(1, f.Actors.ActiveViewCount);
                var document = f.Document(owner); Assert.NotNull(portraits.GetPortrait(document, f.Renderer).Texture);
                var old = f.Zone; f.Manager.CachedZones[old.ZoneID] = new Zone(old.ZoneID);
                f.Tick(); f.Frame(); Assert.Zero(f.Actors.ActiveViewCount); Assert.IsNull(f.Foreground());
                Assert.IsNull(portraits.GetPortrait(document, f.Renderer).Texture); AssertNoPreview(portraits);
                Assert.True(f.Renderer.FlatPixelModeActive, "Invalid graph means no disclosure, not permission for an old presenter.");
                f.Manager.CachedZones[old.ZoneID] = old; f.Renderer.MarkDirty("restore exact graph"); f.Frame();
                Assert.AreEqual(1, f.Actors.ActiveViewCount); Assert.NotNull(portraits.GetPortrait(document, f.Renderer).Texture);
            }
        }

        [TestCase(Outside, false, '~')] [TestCase(Outside, true, '~')]
        [TestCase(ReferenceGladePlan.ZoneID, false, '\0')] [TestCase(ReferenceGladePlan.ZoneID, true, '*')]
        public void WaterMarkIsSuppressedOnlyWhenSelectedLocalGroundActuallyRepresentsWater(string zone, bool charge, char expected)
        {
            using (var f = new Fixture(factory, zone))
            {
                f.Zone.TileState.WriteCoating(X, Y, "water", 4); if (charge) f.Zone.TileState.AddCharge(X, Y, 3); f.Frame();
                Assert.True(f.Renderer.FlatPixelModeActive);
                if (expected == '\0') Assert.IsNull(f.Marks.GetTile(At));
                else Assert.AreSame(CP437TilesetGenerator.GetTile(expected), f.Marks.GetTile(At));
                f.Zone.GetCell(X, Y).IsVisible = false; f.Renderer.MarkCellDirty(X, Y, "fog"); f.Frame(); Assert.IsNull(f.Marks.GetTile(At));
            }
        }

        [Test]
        public void ActiveFlatWorldRetiresFineWaterAndDoesNotRebuildUnusedLegacyCache()
        {
            using (var f = new Fixture(factory))
            {
                var water = f.Add("UnskinnedWater", "~"); water.AddPart(new LiquidPoolPart { LiquidId = "water", Volume = 20 });
                f.Renderer.FlatPixelModeEnabled = false; f.Frame();
                Assert.AreEqual(1, f.WaterCache.Count, "Counter requires actual legacy water cache admission.");
                f.FineWater.SetTile(Vector3Int.zero, CP437TilesetGenerator.GetTile('~'));
                f.Renderer.FlatPixelModeEnabled = true; f.Frame();
                Assert.Zero(f.WaterCache.Count); Assert.Zero(f.FineWater.GetUsedTilesCount());
                f.Renderer.MarkDirty("flat full repaint"); f.Frame(); Assert.Zero(f.WaterCache.Count);
                f.Renderer.FlatPixelModeEnabled = false; f.Frame(); Assert.AreEqual(1, f.WaterCache.Count);
            }
        }

        sealed class RenderProbe : Part
        {
            public int Calls; public string Color;
            public override bool HandleEvent(GameEvent e)
            { if (e.ID == "Render") { Calls++; if (Color != null) e.SetParameter("ColorString", Color); } return true; }
        }
        static Vector3Int Position(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
        static object Read(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
        static void Write(object owner, string name, object value) => owner.GetType().GetField(name, I).SetValue(owner, value);
        static void Call(object owner, string name, params object[] args)
        { try { owner.GetType().GetMethod(name, I).Invoke(owner, args); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
        static void AssertColor(Color expected, Color actual)
        { Assert.That(actual.r, Is.EqualTo(expected.r).Within(.0001)); Assert.That(actual.g, Is.EqualTo(expected.g).Within(.0001)); Assert.That(actual.b, Is.EqualTo(expected.b).Within(.0001)); Assert.That(actual.a, Is.EqualTo(expected.a).Within(.0001)); }
        static Color32[] SpritePixels(Sprite sprite)
        {
            var rect = sprite.rect; var atlas = sprite.texture.GetPixels32(); var result = new Color32[(int)(rect.width * rect.height)];
            for (int y = 0; y < rect.height; y++) for (int x = 0; x < rect.width; x++) result[y * (int)rect.width + x] = atlas[((int)rect.y + y) * sprite.texture.width + (int)rect.x + x];
            return result;
        }
        static void AssertGlyph(Tile tile, char glyph)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            var actual = SpritePixels(tile.sprite); var source = CP437TilesetGenerator.GetAtlasTexture(); var expected = source.GetPixels32();
            int sx = glyph % 16 * 16, sy = (15 - glyph / 16) * 16;
            for (int y = 0; y < 24; y++) for (int x = 0; x < 16; x++)
            { byte alpha = y < 4 || y >= 20 ? (byte)0 : expected[(sy + y - 4) * source.width + sx + x].a; Assert.AreEqual(alpha, actual[y * 16 + x].a, "Glyph ink at " + x + "," + y); }
        }
        static ExaminePortraitRenderer Portraits() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I, null,
            new object[] { (Action<Camera, RenderTexture>)((camera, target) => { }) }, null);
        static void AssertPortrait(ExaminePortrait image, Sprite sprite)
        {
            Assert.NotNull(image.Texture); Assert.False(image.IsModel); Assert.AreSame(sprite.texture, image.Texture);
            var r = sprite.rect; Assert.AreEqual(new Rect(r.x / image.Texture.width, r.y / image.Texture.height, r.width / image.Texture.width, r.height / image.Texture.height), image.Uv);
        }
        static void AssertNoPreview(ExaminePortraitRenderer portraits)
        {
            Assert.IsNull(portraits.CurrentModelId);
            foreach (string name in new[] { "PreviewRoot", "PreviewCamera", "OwnedTarget" }) Assert.IsNull(typeof(ExaminePortraitRenderer).GetProperty(name, I).GetValue(portraits));
            Assert.Zero((int)typeof(ExaminePortraitRenderer).GetProperty("CaptureCount", I).GetValue(portraits));
        }
        sealed class NativeGraph : CameraCleanupFixture
        {
            public ZoneRenderer Presentation => Renderer;
            public CameraFollow CameraFollow => Follow;
            public Grid MainGrid => GridObject.GetComponent<Grid>();
        }
        sealed class Fixture : IDisposable
        {
            readonly NativeGraph graph; readonly HotbarSaveFixture globals; readonly Action restore;
            public readonly ZoneRenderer Renderer; public readonly AnimatedEntityRenderer Actors; public readonly EnvironmentSpriteRenderer Environment;
            public readonly Tilemap Main, Overlay, Background, Marks, FineWater; public readonly Grid Grid; public readonly CameraFollow Follow;
            public readonly OverworldZoneManager Manager; public readonly Vector3 OriginalCellSize; public Zone Zone;
            public List<Vector2Int> WaterCache => (List<Vector2Int>)Read(Renderer, "_waterTilePositions");
            public Fixture(EntityFactory factory, string zone = Outside, bool motion = true, bool nativeIdle = true)
            {
                var moved = EntityVisualHooks.MovedCallback; var attack = EntityVisualHooks.AttackCallback; var damage = EntityVisualHooks.DamageCallback;
                var cast = EntityVisualHooks.CastCallback; var death = EntityVisualHooks.DeathCallback;
                var interaction = EntityVisualHooks.InteractionCallback; var selfUse = EntityVisualHooks.SelfUseCallback;
                var cell = ZoneRenderHooks.CellDirtyCallback; var full = ZoneRenderHooks.FullDirtyCallback;
                var bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic); var oldBound = bound.GetValue(null);
                Zone previous = null; Action<int, int> oldChanged = null; Action oldSight = null;
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                restore = () => {
                    EntityVisualHooks.MovedCallback = moved; EntityVisualHooks.AttackCallback = attack; EntityVisualHooks.DamageCallback = damage;
                    EntityVisualHooks.CastCallback = cast; EntityVisualHooks.DeathCallback = death; EntityVisualHooks.InteractionCallback = interaction; EntityVisualHooks.SelfUseCallback = selfUse;
                    ZoneRenderHooks.CellDirtyCallback = cell; ZoneRenderHooks.FullDirtyCallback = full; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                };
                try
                {
                    globals = new HotbarSaveFixture(true, false); EntityVisualHooks.Reset();
                    graph = new NativeGraph(); graph.SetupCameraCleanup(); Renderer = graph.Presentation; Follow = graph.CameraFollow; Grid = graph.MainGrid; OriginalCellSize = Grid.cellSize;
                    Main = (Tilemap)Read(Renderer, "_tilemap"); Background = (Tilemap)Read(Renderer, "_bgTilemap"); Marks = (Tilemap)Read(Renderer, "_tileStateTilemap"); FineWater = (Tilemap)Read(Renderer, "_fineWaterTilemap");
                    Actors = (AnimatedEntityRenderer)Read(Renderer, "_entityVisualRenderer"); Environment = (EnvironmentSpriteRenderer)Read(Renderer, "_envSpriteRenderer");
                    Assert.NotNull(Actors); Assert.NotNull(Environment); Overlay = (Tilemap)Read(Environment, "_overlayTilemap"); Environment.RenderingEnabled = true;
                    if (!motion) Write(Actors, "_flatMotionCatalog", new FlatPixelActorMotionCatalog(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath), Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath), null));
                    if (!nativeIdle) Write(Environment, "_flatPixelGreybox", new FlatPixelGreyboxCatalog(null));
                    Manager = OverworldZoneManager.CreateDetached(factory, 64, true);
                    Renderer.BindFlatPixelCamera(Follow); Renderer.FlatPixelModeEnabled = true;
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty; Show(zone);
                }
                catch { Dispose(); throw; }
            }
            public void Show(string id)
            {
                Zone = new Zone(id); Manager.SetActiveZone(Zone);
                foreach (var cell in Zone.Cells) cell.Explored = cell.IsVisible = true;
                Renderer.SetZone(Zone); Follow.CurrentZone = Zone;
            }
            public Entity Add(string blueprint, string glyph, int x = X, int y = Y, int layer = 20)
            {
                var owner = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = blueprint };
                owner.AddPart(new PhysicsPart()); owner.AddPart(new RenderPart { RenderString = glyph, ColorString = "&G", RenderLayer = layer });
                Assert.True(Zone.AddEntity(owner, x, y)); return owner;
            }
            public Entity AddActor(string blueprint)
            {
                var owner = Add(blueprint, blueprint == "Player" ? "@" : "d"); owner.SetTag("Creature"); if (blueprint == "Player") owner.SetTag("Player");
                owner.AddPart(new ExaminablePart());
                owner.GetPart<PhysicsPart>().Solid = true; owner.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 }; return owner;
            }
            public void Frame() => Call(Renderer, "LateUpdate");
            public void Tick() => Call(Actors, "AdvancePresentation", .01f);
            public Tile Foreground(int x = X, int y = Y) { var at = Position(x, y); return Overlay.GetTile<Tile>(at) ?? Main.GetTile<Tile>(at); }
            public Color ForegroundTint => Overlay.HasTile(At) ? Overlay.GetColor(At) : Main.GetColor(At);
            public Color Lit(Color color) => ((LightMap)Read(Renderer, "_lightMap")).ApplyToColor(color, X, Y);
            public void AssertLit(Color color) => AssertColor(Lit(color), ForegroundTint);
            public SpriteRenderer Body(string identity) => Actors.transform.Find("Visual_" + identity)?.Find("Body")?.GetComponent<SpriteRenderer>();
            public AnnouncementDocument Document(Entity owner)
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, Zone, Zone.GetEntityCell(owner));
                Assert.NotNull(document, "Positive portrait fixture requires a genuinely examinable visible owner.");
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document), "Art selection must receive an authorized current document.");
                return document;
            }
            public void Dispose() { try { graph?.CleanupCameraFixture(); } finally { globals?.Dispose(); restore?.Invoke(); } }
        }
    }
}
