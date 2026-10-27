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
    // The same disclosed clean-water material as the opening, in one exact
    // already-admitted East graph. No generation, resource or collision change.
    public sealed class EastSurfaceWaterPresentationTests
    {
        const string East = "Overworld.12.10.0";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Color OldWater = new Color32(12, 67, 68, 255);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelFallbackCatalog selector;
        FlatPixelOpeningGroundCatalog ground;
        FlatPixelGreyboxCatalog world;
        Texture2D atlas;
        Zone zone;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            selector = new FlatPixelFallbackCatalog(); atlas = new Texture2D(336, 24);
            ground = new FlatPixelOpeningGroundCatalog(atlas, selector); zone = Attach(East);
        }
        [TearDown] public void Cleanup()
        { world?.Dispose(); ground?.Dispose(); selector?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); content?.Dispose(); }
        Zone Attach(string id)
        {
            var value = new Zone(id); manager.SetActiveZone(value);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                value.GetCell(x, y).Explored = value.GetCell(x, y).IsVisible = true;
            return value;
        }
        Entity Add(string blueprint, int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        void Water(int x = X, int y = Y, int turns = ZoneTileState.Permanent) => zone.TileState.WriteCoating(x, y, "water", turns);
        static Rect Frame(int mask) => new Rect(mask * 16, 0, 16, 24);
        Tile Expect(int mask)
        {
            Assert.True(ground.TryResolve(zone, X, Y, out var tile, out var tint), "Current East clean-water ground is still excluded.");
            Assert.AreSame(atlas, tile.sprite.texture); Assert.AreEqual(Frame(mask), tile.sprite.rect);
            Assert.AreEqual(Color.white, tint); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(TileFlags.None, tile.flags);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); return tile;
        }
        void WetNeighbors(int dryMask)
        {
            if ((dryMask & 1) == 0) Water(X, Y - 1);
            if ((dryMask & 2) == 0) Water(X + 1, Y);
            if ((dryMask & 4) == 0) Water(X, Y + 1);
            if ((dryMask & 8) == 0) Water(X - 1, Y);
        }
        FlatPixelGreyboxCell Visible()
        { Assert.True(world.TryResolvePresentation(zone, X, Y, out var value)); return value; }
        Texture2D NativeResource()
        {
            var value = Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath);
            Assert.NotNull(value); Assert.AreEqual(336, value.width); Assert.AreEqual(24, value.height);
            Assert.AreEqual(FilterMode.Point, value.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, value.wrapMode);
            Assert.AreEqual(1, value.mipmapCount); return value;
        }

        [TestCase(64)] [TestCase(65)] [TestCase(200893250)]
        public void DefaultWorldUsesTheExistingBankAtlasForTheActualAdmittedEast(int seed)
        {
            manager = OverworldZoneManager.CreateDetached(content.Factory, seed, true); zone = Attach(East);
            var actor = Add("Player"); Water(); Water(X + 1, Y);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone)); world = new FlatPixelGreyboxCatalog();
            var value = Visible(); Assert.AreSame(actor, value.Owner); Assert.True(value.LocalGround);
            Assert.AreEqual(FlatPixelGreyboxGround.Water, value.GroundKind);
            Assert.AreSame(NativeResource(), value.GroundTile.sprite.texture, "The default presentation must route here without test injection.");
            Assert.AreEqual(Frame(13), value.GroundTile.sprite.rect); Assert.AreEqual(Color.white, value.GroundColor);
            Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
            var dry = Visible(); Assert.AreSame(actor, dry.Owner);
            Assert.AreNotSame(NativeResource(), dry.GroundTile.sprite.texture);
        }

        [Test] public void SixteenMasksUseOnlyDisclosedCardinalBanksAndNeverChangeState()
        {
            var seen = new HashSet<Tile>();
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(East); Water(); WetNeighbors(mask); Water(X - 1, Y - 1); Water(X + 2, Y);
                string saved = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
                var tile = Expect(mask); Assert.True(seen.Add(tile)); Assert.AreSame(tile, Expect(mask));
                Assert.AreEqual(saved, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y).IsWall());
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("ice")]
        [TestCase("oil")] [TestCase("expired-record")]
        public void OnlyDisclosedCoatingFreeNeighborAddsABank(string fault)
        {
            Water(); WetNeighbors(2); Expect(2); var east = zone.GetCell(X + 1, Y);
            if (fault == "hidden") east.IsVisible = false;
            else if (fault == "unexplored") east.Explored = false;
            else { zone.TileState.WriteCoating(X + 1, Y, fault == "expired-record" ? "water" : fault, 2);
                if (fault == "expired-record") zone.TileState.Get(X + 1, Y).Coatings[0].Turns = 0; }
            Expect(0);
            east.IsVisible = east.Explored = true;
            if (fault != "hidden" && fault != "unexplored")
                Assert.True(zone.TileState.RemoveCoating(X + 1, Y, fault == "expired-record" ? "water" : fault));
            Expect(2);
        }

        [TestCase(false)] [TestCase(true)]
        public void HiddenCenterNeverRevealsCurrentWaterOrItsBanks(bool explored)
        {
            Add("Grass"); Water(); Expect(15); world = new FlatPixelGreyboxCatalog();
            zone.GetCell(X, Y).IsVisible = false; zone.GetCell(X, Y).Explored = explored;
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
            bool resolved = world.TryResolvePresentation(zone, X, Y, out var hidden);
            if (explored) { Assert.True(resolved); Assert.True(hidden.Remembered); Assert.AreNotSame(NativeResource(), hidden.GroundTile.sprite.texture); }
            else Assert.False(resolved);
            zone.GetCell(X, Y).IsVisible = zone.GetCell(X, Y).Explored = true; Expect(15);
        }

        [TestCase("oil")] [TestCase("ice")] [TestCase("brine")]
        [TestCase("duplicate-water")] [TestCase("expired-extra")]
        public void MixedOrOtherCoatingsCannotBePortrayedAsCleanWater(string fault)
        {
            Water(); Expect(15);
            var state = zone.TileState.Get(X, Y);
            if (fault == "duplicate-water") state.Coatings.Add(new ZoneTileState.Layer { Id = "water", Turns = 4 });
            else { zone.TileState.WriteCoating(X, Y, fault == "expired-extra" ? "oil" : fault, 4);
                if (fault == "expired-extra") state.Coatings[1].Turns = 0; }
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
            world = new FlatPixelGreyboxCatalog(); Assert.AreNotSame(NativeResource(), Visible().GroundTile.sprite.texture);
            state.Coatings.RemoveAt(1); Expect(15);
            Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
            zone.TileState.WriteCoating(X, Y, "ice", 4); Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
        }

        [TestCase("RoadStone")] [TestCase("CropRow")] [TestCase("RipeCropRow")]
        [TestCase("prepared-Grass")]
        public void EastWaterAdmissionDoesNotAdmitRoadFieldOrPreparedSoilArt(string blueprint)
        {
            var owner = Add(blueprint == "prepared-Grass" ? "Grass" : blueprint);
            if (blueprint == "prepared-Grass") { owner.SetTag("Plantable"); owner.AddPart(new CultivatedSoilPart()); }
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
            Water(); Expect(15); Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
        }

        [TestCase("Overworld.11.7.0")]
        [TestCase("Overworld.12.11.0")] [TestCase(GleanersCellarBuilder.ZoneID)]
        [TestCase("Overworld.12.12.0")]
        public void OtherSitesDoNotAcquireEastWaterArt(string id)
        {
            Water(); Expect(15); zone = Attach(id); Water();
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _));
            world = new FlatPixelGreyboxCatalog(); Assert.AreNotSame(NativeResource(), Visible().GroundTile.sprite.texture);
            zone = Attach(ReferenceGladePlan.ZoneID); Water(); Expect(15);
        }

        [Test] public void CurrentGraphBiomePoiAndCanonicalAddressRemainAuthority()
        {
            Water(); Expect(15); world = new FlatPixelGreyboxCatalog();
            manager.CachedZones[East] = new Zone(East);
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            manager.CachedZones[East] = zone; Expect(15);
            manager.WorldMap.SetPOI(12, 10, new PointOfInterest(POIType.Village, "water counter", null, 1));
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.AreNotSame(NativeResource(), Visible().GroundTile.sprite.texture);
            manager.WorldMap.SetPOI(12, 10, null); Expect(15);
            manager.WorldMap.Tiles[12, 10] = BiomeType.Sodden;
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _)); Assert.AreNotSame(NativeResource(), Visible().GroundTile.sprite.texture);
            manager.WorldMap.Tiles[12, 10] = BiomeType.Spread; Expect(15);
            var unmanaged = new Zone(East); unmanaged.GetCell(X, Y).Explored = unmanaged.GetCell(X, Y).IsVisible = true;
            unmanaged.TileState.WriteCoating(X, Y, "water", 6);
            Assert.False(ground.TryResolve(unmanaged, X, Y, out _, out _));
        }

        [TestCase("Player")] [TestCase("HealingTonic")]
        public void CoveringForegroundPreservesItsOwnerWhileWaterRemainsBelowIt(string blueprint)
        {
            var cover = Add(blueprint); cover.GetPart<RenderPart>().RenderLayer = 100; Water();
            world = new FlatPixelGreyboxCatalog(); var value = Visible();
            Assert.AreSame(cover, value.Owner); Assert.NotNull(value.ForegroundTile);
            Assert.AreSame(NativeResource(), value.GroundTile.sprite.texture); Assert.AreEqual(Frame(15), value.GroundTile.sprite.rect);
            string before = zone.TileState.ToSaveString(); var cell = zone.GetEntityCell(cover); bool blocked = cell.BlocksMovement();
            for (int i = 0; i < 64; i++) { Expect(15); Visible(); }
            Assert.AreSame(cell, zone.GetEntityCell(cover)); Assert.AreEqual(blocked, cell.BlocksMovement());
            Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.True(zone.RemoveEntity(cover));
            Assert.IsNull(Visible().Owner); Assert.AreSame(NativeResource(), Visible().GroundTile.sprite.texture);
        }

        [Test] public void WaterArtDoesNotInventAReservoirOrSpendAnExistingPool()
        {
            Water(); world = new FlatPixelGreyboxCatalog(); Assert.IsNull(Visible().Owner); Expect(15);
            var pool = Add("WaterPuddle"); var liquid = pool.GetPart<LiquidPoolPart>(); Assert.NotNull(liquid);
            int volume = liquid.Volume; int count = zone.EntityVersion; var current = zone.GetEntityCell(pool);
            for (int i = 0; i < 64; i++) { Assert.AreSame(pool, Visible().Owner); Expect(15); }
            Assert.AreEqual(volume, liquid.Volume); Assert.AreEqual(count, zone.EntityVersion); Assert.AreSame(current, zone.GetEntityCell(pool));
            Assert.True(zone.TileState.RemoveCoating(X, Y, "water"));
            Assert.False(ground.TryResolve(zone, X, Y, out _, out _), "A pool glyph is not permission to fabricate a tile-state coating.");
            Assert.AreSame(pool, Visible().Owner); Assert.AreEqual(volume, liquid.Volume);
        }

        [Test] public void MissingOptionalArtKeepsExistingFlatWaterWithoutMutatingBorrowedResources()
        {
            Water(); Expect(15);
            using (var missing = new FlatPixelOpeningGroundCatalog(null, selector))
                Assert.False(missing.TryResolve(zone, X, Y, out _, out _));
            using (var injected = new FlatPixelGreyboxCatalog(null))
            {
                Assert.True(injected.TryResolvePresentation(zone, X, Y, out var value));
                Assert.AreEqual(FlatPixelGreyboxGround.Water, value.GroundKind); Assert.AreEqual(OldWater, value.GroundColor);
                Assert.AreNotSame(NativeResource(), value.GroundTile.sprite.texture);
            }
            Assert.True(selector.TryResolve(zone, X, Y, out _)); Assert.False(atlas == null);
        }

        [Test] public void WarmWaterSelectionAllocatesNothingAndPreservesTileState()
        {
            Water(); Expect(15); for (int i = 0; i < 256; i++) ground.TryResolve(zone, X, Y, out _, out _);
            string before = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            long start = GC.GetAllocatedBytesForCurrentThread(); bool all = true;
            for (int i = 0; i < 1024; i++) all &= ground.TryResolve(zone, X, Y, out _, out _);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.True(all); Assert.Zero(allocated); Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
        }

        [Test] public void ActualExpiryRepaintsNeighborBanksBelowAnUnchangedActorWithoutAFullPass()
        {
            var actor = Add("Player"); Water(); Water(X + 1, Y, 1);
            using (var paint = new PaintFixture(zone))
            {
                paint.AssertNative(X, Y, 13); var foreground = paint.ForegroundAt(X, Y); Assert.NotNull(foreground);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                zone.TileState.Tick(); Assert.True(paint.Dirty.Contains(Y * Zone.Width + X + 1));
                paint.Frame(); paint.AssertNative(X, Y, 15); paint.Quiet();
                Assert.AreSame(foreground, paint.ForegroundAt(X, Y)); Assert.AreSame(actor, zone.GetCell(X, Y).Objects[0]);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Water(X + 1, Y, 2); Assert.True(paint.Dirty.Contains(Y * Zone.Width + X + 1));
                paint.Frame(); paint.AssertNative(X, Y, 13); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.True(zone.TileState.RemoveCoating(X, Y, "water")); paint.Frame();
                Assert.AreNotSame(NativeResource(), paint.GroundAt(X, Y).sprite.texture);
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Frame();
                Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved); paint.Quiet();
            }
        }

        [Test] public void AuthoredFreezeAndMeltRemoveThenRestoreNativeWaterAndRetainIceCue()
        {
            Water(); Water(X + 1, Y);
            using (var reactions = new ReactionScope())
            using (var paint = new PaintFixture(zone))
            {
                paint.AssertNative(X, Y, 13); paint.AssertNative(X + 1, Y, 7);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                zone.TileState.AddCold(X, Y, 1); Assert.Greater(TileReactionSystem.ResolveZone(zone), 0);
                Assert.True(zone.TileState.HasCoating(X, Y, "ice")); Assert.False(zone.TileState.HasCoating(X, Y, "water"));
                Assert.True(paint.Dirty.Contains(Y * Zone.Width + X)); paint.Frame(); paint.Quiet();
                Assert.AreNotSame(NativeResource(), paint.GroundAt(X, Y).sprite.texture);
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.MarkAt(X, Y));
                Assert.AreEqual(new Color(.85f, .95f, 1), paint.MarkTint(X, Y));
                paint.AssertNative(X + 1, Y, 7); // Frozen is coated, not a disclosed dry bank.
                zone.TileState.AddHeat(X, Y, 1); Assert.Greater(TileReactionSystem.ResolveZone(zone), 0);
                Assert.True(zone.TileState.HasCoating(X, Y, "water")); Assert.False(zone.TileState.HasCoating(X, Y, "ice"));
                paint.Frame(); paint.AssertNative(X, Y, 13); Assert.IsNull(paint.MarkAt(X, Y));
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Frame();
                Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved); paint.Quiet();
            }
        }

        [TestCase("charge", '*')] [TestCase("embers", '"')] [TestCase("steam", '\'')]
        public void NativeBankGroundDoesNotSuppressOtherPrioritizedHazards(string hazard, char glyph)
        {
            Water();
            using (var paint = new PaintFixture(zone))
            {
                paint.AssertNative(X, Y, 15); Assert.IsNull(paint.MarkAt(X, Y));
                if (hazard == "charge") zone.TileState.AddCharge(X, Y, 1);
                else if (hazard == "embers") zone.TileState.WriteResidue(X, Y, "embers", 1);
                else zone.TileState.WriteCloud(X, Y, "steam", 1);
                paint.Frame(); paint.AssertNative(X, Y, 15);
                if (hazard == "steam")
                {
                    // The actual lazy/default route must use the optional East
                    // cue while preserving the already represented water bank.
                    var resource = Resources.Load<Texture2D>(FlatPixelEastSteamCatalog.ResourcePath);
                    Assert.NotNull(resource); Assert.AreEqual(16, resource.width); Assert.AreEqual(24, resource.height);
                    var mark = paint.MarkAt(X, Y) as Tile; Assert.NotNull(mark); Assert.NotNull(mark.sprite);
                    Assert.AreSame(resource, mark.sprite.texture); Assert.AreEqual(Color.white, paint.MarkTint(X, Y));
                }
                else Assert.AreSame(CP437TilesetGenerator.GetTile(glyph), paint.MarkAt(X, Y));
                zone.TileState.Tick(); paint.Frame(); Assert.IsNull(paint.MarkAt(X, Y)); paint.AssertNative(X, Y, 15);
            }
        }

        sealed class ReactionScope : IDisposable
        {
            const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            readonly List<TileReactionSystem.TileReaction> list;
            readonly TileReactionSystem.TileReaction[] previous;
            readonly bool initialized = TileReactionSystem.IsInitialized;
            public ReactionScope()
            {
                list = (List<TileReactionSystem.TileReaction>)typeof(TileReactionSystem).GetField("_reactions", S).GetValue(null);
                previous = list.ToArray();
                var source = Resources.Load<TextAsset>("Content/Data/TileReactions/Reactions"); Assert.NotNull(source);
                TileReactionSystem.Initialize(source.text); Assert.True(TileReactionSystem.IsInitialized);
            }
            public void Dispose()
            {
                list.Clear(); list.AddRange(previous);
                typeof(TileReactionSystem).GetProperty("IsInitialized", S).GetSetMethod(true).Invoke(null, new object[] { initialized });
            }
        }

        // Existing renderer harness, narrowed to the shipped resource path.
        // Never manually mark cells after a tested state mutation.
        sealed class PaintFixture : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject gridRoot;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            readonly Tilemap overlay, background, marks;
            readonly FlatPixelGreyboxCatalog catalog;
            readonly ZoneRenderer renderer;
            public HashSet<int> Dirty => (HashSet<int>)Read(renderer, "_dirtyCells");
            public PaintFixture(Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null; saved = new HotbarSaveFixture();
                    gridRoot = new GameObject("East water active Grid", typeof(Grid)); saved.Root.transform.SetParent(gridRoot.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); Assert.NotNull(main);
                    background = ChildMap("East water ground"); marks = ChildMap("East water marks"); renderer = saved.Renderer;
                    Write(renderer, "_tilemap", main); Write(renderer, "_bgTilemap", background); Write(renderer, "_tileStateTilemap", marks);
                    var camera = new GameObject("Owned water camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false);
                    camera.GetComponent<Camera>().orthographic = true; Write(renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var environment = saved.Root.AddComponent<EnvironmentSpriteRenderer>();
                    environment.Init(saved.Root.transform, main, background); environment.RenderingEnabled = true; environment.FlatPixelGreyboxEnabled = true;
                    catalog = new FlatPixelGreyboxCatalog(); Write(environment, "_flatPixelGreybox", catalog);
                    overlay = (Tilemap)Read(environment, "_overlayTilemap");
                    Write(renderer, "_envSpriteRenderer", environment); Write(renderer, "_flatPixelModeEnabled", true); renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = renderer.MarkDirty;
                    Frame(); Quiet(); Assert.True(renderer.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            Tilemap ChildMap(string name)
            { var host = new GameObject(name, typeof(Tilemap)); host.transform.SetParent(saved.Root.transform, false); return host.GetComponent<Tilemap>(); }
            static Vector3Int At(int x, int y) => new Vector3Int(x, Zone.Height - 1 - y, 0);
            public Tile GroundAt(int x, int y) => background.GetTile<Tile>(At(x, y));
            public Tile ForegroundAt(int x, int y) => overlay.GetTile<Tile>(At(x, y));
            public Tile MarkAt(int x, int y) => marks.GetTile<Tile>(At(x, y));
            public Color MarkTint(int x, int y) => marks.GetColor(At(x, y));
            public void AssertNative(int x, int y, int mask)
            {
                Assert.NotNull(GroundAt(x, y)); Assert.AreSame(Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath), GroundAt(x, y).sprite.texture);
                Assert.AreEqual(EastSurfaceWaterPresentationTests.Frame(mask), GroundAt(x, y).sprite.rect); Assert.AreEqual(Color.white, background.GetColor(At(x, y)));
            }
            public void Frame()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Quiet() { Assert.False((bool)Read(renderer, "_fullDirty")); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (gridRoot != null) Object.DestroyImmediate(gridRoot); }
                finally
                {
                    catalog?.Dispose(); ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
            static object Read(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
            static void Write(object owner, string name, object value)
            { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(owner, value); }
        }
    }
}
