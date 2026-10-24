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
    public sealed class FlatPixelCindercallerTests
    {
        const string Resource = "Sprites/Live16x24/cindercaller";
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Entity owner, viewer;
        Action<int, int, string> oldCell;
        Action<string> oldFull;

        [SetUp] public void Setup()
        {
            oldCell = ZoneRenderHooks.CellDirtyCallback;
            oldFull = ZoneRenderHooks.FullDirtyCallback;
            content = new DensityLootTestScope();
            ZoneRenderHooks.CellDirtyCallback = null;
            ZoneRenderHooks.FullDirtyCallback = null;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = new Zone("Overworld.12.10.0");
            manager.SetActiveZone(zone);
            for (int y = Y - 1; y <= Y + 1; y++)
                for (int x = X - 1; x <= X + 1; x++)
                    zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true;
            owner = content.Factory.CreateEntity("MarlbackCindercaller");
            viewer = content.Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(owner, X, Y));
            Assert.True(zone.AddEntity(viewer, X - 1, Y));
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone), "The fixture must exercise the existing current east admission.");
            Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(owner), "Inherited Scrabbler mechanics must not alias its body motion.");
        }

        [TearDown] public void Cleanup()
        {
            try { content?.Dispose(); }
            finally
            {
                ZoneRenderHooks.CellDirtyCallback = oldCell;
                ZoneRenderHooks.FullDirtyCallback = oldFull;
            }
        }

        [Test] public void FactoryCindercallerHasItsOwnNativeWorldBodyInsteadOfInheritedOrGlyphArt()
        {
            using (var catalog = new FlatPixelGreyboxCatalog())
            {
                Assert.True(catalog.TryResolvePresentation(zone, X, Y, out var body));
                Assert.AreSame(owner, body.Owner);
                Assert.True(body.NativeForeground, "The actual Cindercaller still resolves the glyph fallback.");
                AssertFrame(body.ForegroundTile.sprite);
                Assert.AreEqual(Color.white, body.ForegroundColor);
            }
        }

        [Test] public void CurrentCindercallerSubjectUsesItsNativeFrameInsteadOfTheCachedGlyph()
        {
            var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(X, Y), viewer);
            Assert.NotNull(document, "A real factory Examinable and current disclosed world subject are required.");
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            using (var catalog = new FlatPixelGreyboxCatalog())
            {
                Assert.True(catalog.TryGetFallbackSubjectSprite(owner, X, Y, out var glyph, out _));
                Assert.True(catalog.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out var tint));
                Assert.AreNotSame(glyph, portrait, "The exact-subject art path still returns the Cindercaller glyph.");
                AssertFrame(portrait);
                Assert.AreEqual(Color.white, tint);
            }
        }

        static void AssertFrame(Sprite sprite)
        {
            var expected = Resources.Load<Texture2D>(Resource);
            Assert.NotNull(expected, "The actual optional resource must be imported; a synthetic positive is insufficient.");
            Assert.NotNull(sprite);
            Assert.AreSame(expected, sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), sprite.pivot);
            Assert.AreEqual(16, sprite.pixelsPerUnit);
        }

        [Test] public void IndependentThirdFrameIsCachedAndDoesNotCreateMotionOrMutateTheOwner()
        {
            using (var art = new Art())
            {
                int parts = owner.Parts.Count;
                var inventory = owner.GetPart<InventoryPart>();
                var stock = inventory.Objects.ToArray();
                var render = owner.GetPart<RenderPart>();
                var tile = art.Positive(owner, art.Caster);
                for (int i = 0; i < 100; i++) Assert.AreSame(tile, art.Positive(owner, art.Caster));
                var farmer = art.Positive(content.Factory.CreateEntity("Farmer"), art.Farmer);
                var grazer = art.Positive(content.Factory.CreateEntity("ReedbackGrazer"), art.Grazer);
                Assert.AreNotSame(tile, farmer); Assert.AreNotSame(tile, grazer);
                Assert.AreEqual(parts, owner.Parts.Count); CollectionAssert.AreEqual(stock, inventory.Objects);
                Assert.AreEqual("g", render.RenderString); Assert.AreEqual("&R", render.ColorString);
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(owner));
                Assert.AreEqual(FilterMode.Bilinear, art.Caster.filterMode);
                Assert.AreEqual(TextureWrapMode.Repeat, art.Caster.wrapMode);
            }
        }

        [Test] public void OlderTwoTextureAndGreyboxInjectionsRemainResourceFree()
        {
            using (var art = new Art())
            using (var old = new FlatPixelIncidentalActorCatalog(art.Farmer, art.Grazer))
            using (var world = new FlatPixelGreyboxCatalog((Texture2D)null))
            {
                Assert.False(old.TryResolve(owner, out _));
                Assert.True(old.TryResolve(content.Factory.CreateEntity("Farmer"), out var farmer));
                Assert.AreSame(art.Farmer, farmer.sprite.texture);
                Assert.True(old.TryResolve(content.Factory.CreateEntity("ReedbackGrazer"), out var grazer));
                Assert.AreSame(art.Grazer, grazer.sprite.texture);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var body)); Assert.False(body.NativeForeground);
            }
        }

        [TestCase("caster", false)] [TestCase("caster", true)]
        [TestCase("farmer", false)] [TestCase("farmer", true)]
        [TestCase("grazer", false)] [TestCase("grazer", true)]
        public void MissingOrMalformedGroupNeverDisablesItsPeersOrLoadsAResource(string group, bool malformed)
        {
            using (var art = new Art())
            {
                var bad = malformed ? new Texture2D(16, 16) : null;
                try
                {
                    using (var catalog = new FlatPixelIncidentalActorCatalog(group == "farmer" ? bad : art.Farmer,
                        group == "grazer" ? bad : art.Grazer, group == "caster" ? bad : art.Caster))
                    {
                        Assert.AreEqual(group != "caster", catalog.TryResolve(owner, out var caster));
                        if (caster != null) Assert.AreSame(art.Caster, caster.sprite.texture);
                        Assert.AreEqual(group != "farmer", catalog.TryResolve(content.Factory.CreateEntity("Farmer"), out var farmer));
                        if (farmer != null) Assert.AreSame(art.Farmer, farmer.sprite.texture);
                        Assert.AreEqual(group != "grazer", catalog.TryResolve(content.Factory.CreateEntity("ReedbackGrazer"), out var grazer));
                        if (grazer != null) Assert.AreSame(art.Grazer, grazer.sprite.texture);
                    }
                }
                finally { if (bad != null) Object.DestroyImmediate(bad); }
            }
        }

        [Test] public void DisposalRetiresOwnedFramesOnlyAndIsIndependentOfPeers()
        {
            using (var art = new Art())
            using (var peer = new FlatPixelIncidentalActorCatalog(art.Farmer, art.Grazer, art.Caster))
            {
                var tile = art.Positive(owner, art.Caster); var sprite = tile.sprite;
                Assert.True(peer.TryResolve(owner, out var independent)); Assert.AreNotSame(tile, independent);
                art.Catalog.Dispose(); art.Catalog.Dispose();
                Assert.True(tile == null); Assert.True(sprite == null); Assert.False(art.Catalog.TryResolve(owner, out _));
                Assert.True(art.Caster != null); Assert.True(art.Farmer != null); Assert.True(art.Grazer != null);
                Assert.True(peer.TryResolve(owner, out var after)); Assert.AreSame(independent, after);
            }
        }

        [TestCase("visual-id")] [TestCase("variant")] [TestCase("glyph-variants")] [TestCase("glyph")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("missing-physics")]
        [TestCase("foreign-brain")] [TestCase("foreign-body")] [TestCase("missing-brain")]
        [TestCase("hidden")] [TestCase("creature-tag")] [TestCase("item-tag")] [TestCase("player-tag")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")] [TestCase("footprint")]
        [TestCase("empty-stack")] [TestCase("foreign-stack")]
        public void KnownOwnerRechecksItsExactSemanticAuthorityAfterCaching(string fault)
        {
            using (var art = new Art())
            {
                art.Positive(owner, art.Caster);
                var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
                if (fault == "visual-id") render.VisualID = "custom";
                if (fault == "variant") render.VisualVariant = "costume";
                if (fault == "glyph-variants") render.GlyphVariants = "gQ";
                if (fault == "glyph") render.RenderString = "Q";
                if (fault == "foreign-render") render.ParentEntity = new Entity();
                if (fault == "foreign-physics") physics.ParentEntity = new Entity();
                if (fault == "missing-physics") owner.RemovePart(physics);
                if (fault == "foreign-brain") owner.GetPart<BrainPart>().ParentEntity = new Entity();
                if (fault == "foreign-body") owner.GetPart<Body>().ParentEntity = new Entity();
                if (fault == "missing-brain") owner.RemovePart(owner.GetPart<BrainPart>());
                if (fault == "hidden") render.Visible = false;
                if (fault == "creature-tag") owner.Tags.Remove("Creature");
                if (fault == "item-tag") owner.SetTag("Item");
                if (fault == "player-tag") owner.SetTag("Player");
                if (fault == "carried") physics.InInventory = viewer;
                if (fault == "equipped") physics.Equipped = viewer;
                if (fault == "takeable") physics.Takeable = true;
                if (fault == "footprint") owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
                if (fault == "empty-stack" || fault == "foreign-stack")
                {
                    var stack = new StackerPart { StackCount = fault == "empty-stack" ? 0 : 1 };
                    owner.AddPart(stack); if (fault == "foreign-stack") stack.ParentEntity = viewer;
                }
                Assert.False(art.Catalog.TryResolve(owner, out _), fault);
                art.Positive(content.Factory.CreateEntity("MarlbackCindercaller"), art.Caster);
            }
        }

        [Test] public void InheritanceAndLookalikesDoNotBorrowTheCindercallerFrame()
        {
            using (var art = new Art())
            {
                art.Positive(owner, art.Caster);
                foreach (var bp in new[] { "MarlbackScrabbler", "MarlbackGleaner", "MarlbackSoursprayer", "Player", "Villager" })
                {
                    var other = content.Factory.CreateEntity(bp); Assert.NotNull(other);
                    Assert.False(art.Catalog.TryResolve(other, out _), bp);
                }
                owner.BlueprintName = "UnknownCindercaller";
                Assert.False(art.Catalog.TryResolve(owner, out _));
            }
        }

        [Test] public void NeutralBodySurvivesRealReagentRemovalUnequipAndMutableCombatStateWithoutTickingIt()
        {
            using (var art = new Art())
            {
                var tile = art.Positive(owner, art.Caster);
                var inventory = owner.GetPart<InventoryPart>();
                var moss = inventory.Objects.Find(item => item.BlueprintName == "FireMoss");
                Assert.NotNull(moss); Assert.True(inventory.RemoveObject(moss));
                Entity cudgel = null;
                foreach (var item in inventory.EquippedItems.Values) if (item.BlueprintName == "Cudgel") { cudgel = item; break; }
                Assert.NotNull(cudgel); Assert.True(InventorySystem.UnequipItem(owner, cudgel));
                owner.GetStat("Hitpoints").BaseValue = 1;
                owner.GetPart<BrainPart>().Passive = true;
                owner.GetPart<RenderPart>().DisplayName = "named cindercaller";
                var ability = owner.GetPart<ActivatedAbilitiesPart>().AbilityList.Find(a => a.Command == "CommandEmberSpit");
                Assert.NotNull(ability); ability.CooldownRemaining = 7;
                for (int i = 0; i < 100; i++) Assert.AreSame(tile, art.Positive(owner, art.Caster));
                Assert.AreEqual(7, ability.CooldownRemaining); Assert.AreEqual(1, owner.GetStatValue("Hitpoints"));
                Assert.False(inventory.Objects.Contains(moss)); Assert.IsNull(cudgel.GetPart<PhysicsPart>().Equipped);
            }
        }

        [TestCase("fog")] [TestCase("replaced-graph")] [TestCase("replaced-cell")]
        [TestCase("carried")] [TestCase("foreign-render")]
        public void SubmittedWorldAndAuthorizedReaderBorrowTheSameFrameAndRetireInvalidSubjects(string fault)
        {
            using (var paint = new PaintFixture(zone))
            using (var portraits = PortraitProbe())
            {
                paint.Frame(); paint.Quiet(); AssertFrame(paint.At(X, Y).sprite);
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(X, Y), viewer);
                Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                var image = portraits.GetPortrait(document, paint.Renderer);
                Assert.AreSame(paint.At(X, Y).sprite.texture, image.Texture);
                Assert.False(image.IsModel); Assert.AreEqual(new Rect(0, 0, 1, 1), image.Uv);
                if (fault == "fog") zone.GetCell(X, Y).IsVisible = false;
                if (fault == "replaced-graph") manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID);
                if (fault == "replaced-cell") zone.Cells[X, Y] = new Cell(X, Y) { ParentZone = zone };
                if (fault == "carried") owner.GetPart<PhysicsPart>().InInventory = viewer;
                if (fault == "foreign-render") owner.GetPart<RenderPart>().ParentEntity = viewer;
                Assert.IsNull(portraits.GetPortrait(document, paint.Renderer).Texture, fault);
                paint.Renderer.MarkCellDirty(X, Y, "Actual disclosure/owner transition"); paint.Frame();
                Assert.IsNull(paint.At(X, Y), fault);
            }
        }

        [Test] public void RealMovementRestoresCoveredBedAndNewCellWithoutFullRepaintOrMotionAlias()
        {
            var bed = content.Factory.CreateEntity("Bed"); Assert.True(zone.AddEntity(bed, X, Y));
            using (var paint = new PaintFixture(zone))
            {
                paint.Frame(); paint.Quiet(); AssertFrame(paint.At(X, Y).sprite);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(MovementSystem.TryMove(owner, zone, 1, 0));
                Assert.True(paint.Dirty.Contains(Y * Zone.Width + X)); Assert.True(paint.Dirty.Contains(Y * Zone.Width + X + 1));
                paint.Frame(); paint.Quiet(); AssertFrame(paint.At(X + 1, Y).sprite);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                var restored = paint.At(X, Y); Assert.NotNull(restored);
                Assert.AreSame(Resources.Load<Texture2D>("Sprites/Live16x24/east-kitchen"), restored.sprite.texture);
                Assert.AreEqual(new Rect(32, 0, 16, 24), restored.sprite.rect);
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.True(zone.GetCell(X + 1, Y).BlocksMovement());
                Assert.False(FlatPixelActorMotionCatalog.IsPortableActor(owner));
            }
        }

        [Test] public void OutsideAdmissionHiddenAndStaleOldMembershipDoNotAcquireLocalBodyArt()
        {
            using (var catalog = new FlatPixelGreyboxCatalog())
            {
                Assert.True(catalog.TryResolvePresentation(zone, X, Y, out var visible)); Assert.True(visible.NativeForeground);
                zone.GetCell(X, Y).IsVisible = false;
                Assert.True(catalog.TryResolvePresentation(zone, X, Y, out var memory)); Assert.IsNull(memory.Owner); Assert.IsNull(memory.ForegroundTile);
                zone.GetCell(X, Y).Explored = false; Assert.False(catalog.TryResolvePresentation(zone, X, Y, out _));
                zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true;
                Assert.True(zone.MoveEntity(owner, X + 1, Y)); zone.GetCell(X, Y).Objects.Add(owner);
                Assert.True(catalog.TryResolvePresentation(zone, X, Y, out var stale)); Assert.IsNull(stale.Owner);
                Assert.True(catalog.TryResolvePresentation(zone, X + 1, Y, out var moved)); AssertFrame(moved.ForegroundTile.sprite);
                var outside = new Zone("Overworld.13.10.0"); manager.SetActiveZone(outside);
                outside.GetCell(X, Y).Explored = outside.GetCell(X, Y).IsVisible = true;
                Assert.True(zone.RemoveEntity(owner)); Assert.True(outside.AddEntity(owner, X, Y));
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(outside));
                Assert.True(catalog.TryResolvePresentation(outside, X, Y, out var other)); Assert.False(other.NativeForeground);
                Assert.True(catalog.TryGetPresentationSubjectSprite(outside, owner, X, Y, out var portrait, out _));
                Assert.AreNotSame(Resources.Load<Texture2D>(Resource), portrait.texture);
            }
        }

        [TestCase("visual-id")] [TestCase("variant")] [TestCase("glyph-variants")] [TestCase("glyph")]
        public void WorldAndSubjectDoNotBypassExplicitAppearanceThroughAnInheritedOldAlias(string fault)
        {
            using (var catalog = new FlatPixelGreyboxCatalog())
            {
                Assert.True(catalog.TryResolvePresentation(zone, X, Y, out var first)); AssertFrame(first.ForegroundTile.sprite);
                var render = owner.GetPart<RenderPart>();
                if (fault == "visual-id") render.VisualID = "custom";
                if (fault == "variant") render.VisualVariant = "costume";
                if (fault == "glyph-variants") render.GlyphVariants = "gQ";
                if (fault == "glyph") render.RenderString = "Q";
                Assert.True(catalog.TryResolvePresentation(zone, X, Y, out var rejected)); Assert.False(rejected.NativeForeground);
                Assert.True(catalog.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out _));
                Assert.AreNotSame(Resources.Load<Texture2D>(Resource), portrait.texture);
            }
        }

        [Test] public void ActualDefaultResourcePreservesNativeFrameAndItsTwoEarlierPeers()
        {
            using (var catalog = new FlatPixelIncidentalActorCatalog())
            {
                Assert.True(catalog.TryResolve(owner, out var caster)); AssertFrame(caster.sprite);
                var texture = caster.sprite.texture;
                Assert.AreEqual(16, texture.width); Assert.AreEqual(24, texture.height);
                Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode); Assert.AreEqual(1, texture.mipmapCount);
                Assert.True(catalog.TryResolve(content.Factory.CreateEntity("Farmer"), out var farmer));
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelIncidentalActorCatalog.FarmerResourcePath), farmer.sprite.texture);
                Assert.True(catalog.TryResolve(content.Factory.CreateEntity("ReedbackGrazer"), out var grazer));
                Assert.AreSame(Resources.Load<Texture2D>(FlatPixelIncidentalActorCatalog.GrazerResourcePath), grazer.sprite.texture);
            }
        }

        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static object Get(object instance, string name) => instance.GetType().GetField(name, I).GetValue(instance);
        static void Set(object instance, string name, object value) => instance.GetType().GetField(name, I).SetValue(instance, value);
        static ExaminePortraitRenderer PortraitProbe() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I,
            null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => {}) }, null);

        sealed class Art : IDisposable
        {
            public readonly Texture2D Farmer = new Texture2D(16, 24), Grazer = new Texture2D(16, 24), Caster = new Texture2D(16, 24);
            public readonly FlatPixelIncidentalActorCatalog Catalog;
            public Art() { Catalog = new FlatPixelIncidentalActorCatalog(Farmer, Grazer, Caster); }
            public Tile Positive(Entity actor, Texture2D texture)
            {
                Assert.True(Catalog.TryResolve(actor, out var tile)); Assert.AreSame(texture, tile.sprite.texture);
                Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
                Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); return tile;
            }
            public void Dispose()
            { Catalog.Dispose(); Object.DestroyImmediate(Farmer); Object.DestroyImmediate(Grazer); Object.DestroyImmediate(Caster); }
        }

        sealed class PaintFixture : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject grid;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly EntityMovedVisualHandler oldMoved = EntityVisualHooks.MovedCallback;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            readonly Tilemap overlay;
            readonly FlatPixelGreyboxCatalog catalog;
            public readonly ZoneRenderer Renderer;
            public HashSet<int> Dirty => (HashSet<int>)Get(Renderer, "_dirtyCells");
            public PaintFixture(Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null; saved = new HotbarSaveFixture(); EntityVisualHooks.MovedCallback = null;
                    grid = new GameObject("Cindercaller Grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); Assert.NotNull(main);
                    var bg = new GameObject("Cindercaller background", typeof(Tilemap)); bg.transform.SetParent(saved.Root.transform, false);
                    Renderer = saved.Renderer; Set(Renderer, "_tilemap", main); Set(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>());
                    var camera = new GameObject("Cindercaller camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false);
                    camera.GetComponent<Camera>().orthographic = true; Set(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    catalog = new FlatPixelGreyboxCatalog(); Set(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Get(env, "_overlayTilemap");
                    Set(Renderer, "_envSpriteRenderer", env); Set(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    Frame(); Quiet();
                }
                catch { Dispose(); throw; }
            }
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public void Frame()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; } }
            public void Quiet() { Assert.False((bool)Get(Renderer, "_fullDirty")); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
                finally
                {
                    catalog?.Dispose(); EntityVisualHooks.MovedCallback = oldMoved; ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull;
                    bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
        }
    }
}
