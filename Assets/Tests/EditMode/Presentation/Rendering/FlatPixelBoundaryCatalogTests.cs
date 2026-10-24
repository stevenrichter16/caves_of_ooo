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
    // G2 connected masonry contract: authored before the production adapter.
    // Missing APIs are reflected so the future RED remains an assertion failure.
    public sealed class FlatPixelBoundaryCatalogTests
    {
        const int X = 10, Y = 10;
        const string Resource = "Sprites/Live16x24/opening-boundaries";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Resolve(Zone zone, int x, int y, out Tile tile, out Color tint);
        delegate bool Subject(Entity owner, out Sprite sprite, out Color tint);
        readonly List<IDisposable> owned = new List<IDisposable>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        Action<int, int, string> oldCell; Action<string> oldFull;
        FlatPixelFallbackCatalog selector;
        Texture2D atlas;
        Zone zone;

        [SetUp] public void Setup()
        {
            oldCell = ZoneRenderHooks.CellDirtyCallback; oldFull = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            selector = new FlatPixelFallbackCatalog(); owned.Add(selector);
            atlas = Texture(256, 72); zone = World();
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); owned.Clear();
                foreach (var texture in textures) if (texture != null) Object.DestroyImmediate(texture); textures.Clear();
            }
            finally { ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void AllSixteenCardinalMasksUseNativeRectangularFramesAndStableCache(int material)
        {
            var resolve = Query(New(atlas, selector));
            var seen = new HashSet<Tile>();
            for (int mask = 0; mask < 16; mask++)
            {
                var world = World(); var center = Wall(world, X, Y, material);
                Neighbors(world, mask, material);
                Assert.True(resolve(world, X, Y, out var tile, out var tint));
                Frame(tile, material, mask); Assert.AreEqual(Color.white, tint); Assert.True(seen.Add(tile));
                Assert.True(resolve(world, X, Y, out var repeated, out _)); Assert.AreSame(tile, repeated);
                Assert.AreEqual(40, center.GetPart<DestructiblePart>().HP); Assert.True(world.GetCell(X, Y).BlocksMovement());
            }
        }

        [Test] public void MixedMaterialsJoinButDiagonalsAndDistantOwnersDoNotSupplyBits()
        {
            Wall(zone, X, Y, 1); Wall(zone, X, Y - 1, 0); Wall(zone, X + 1, Y, 2);
            Wall(zone, X - 1, Y - 1, 1); Wall(zone, X, Y + 2, 1);
            var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out var tile, out _)); Frame(tile, 1, 3);
        }

        [TestCase("GlowQuartzVein")] [TestCase("GleanersBuckledWicket")] [TestCase("FallenBeam")]
        [TestCase("RoadStone")] [TestCase("Rubble")] [TestCase("BrokenColumn")] [TestCase("StoneWall")]
        public void SimilarVisualIdsOnOtherBlueprintsNeverJoinOrBecomeMasonry(string blueprint)
        {
            Wall(zone, X, Y, 1); var neighbor = Wall(zone, X + 1, Y, 2); neighbor.BlueprintName = blueprint;
            var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out var tile, out _)); Frame(tile, 1, 0);
            Assert.False(resolve(zone, X + 1, Y, out _, out _));
        }

        [TestCase("visual-id")] [TestCase("visual-variant")] [TestCase("glyph-variants")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("foreign-destructible")]
        [TestCase("missing-physics")] [TestCase("missing-destructible")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")]
        [TestCase("gone")] [TestCase("zero-hp")] [TestCase("non-solid")]
        public void PositiveThenInvalidCenterAndNeighborCannotBorrowTheKnownNativeRole(string fault)
        {
            var center = Wall(zone, X, Y, 1); var east = Wall(zone, X + 1, Y, 1);
            var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out var initial, out _)); Frame(initial, 1, 2);
            Fault(east, fault); Assert.True(resolve(zone, X, Y, out var capped, out _)); Frame(capped, 1, 0);
            Fault(center, fault); Assert.False(resolve(zone, X, Y, out _, out _));
        }

        [Test] public void ExactTopOwnerWinsIncludingLastEqualLayerAndInvalidForeignCoverIsIgnored()
        {
            Wall(zone, X, Y, 1); var east = Wall(zone, X + 1, Y, 1); var resolve = Query(New(atlas, selector));
            Assert.True(resolve(zone, X, Y, out var joined, out _)); Frame(joined, 1, 2);
            var cover = Add(zone, "Foreign covering object", X + 1, Y, 1);
            Assert.True(resolve(zone, X, Y, out var capped, out _)); Frame(capped, 1, 0);
            cover.GetPart<RenderPart>().ParentEntity = new Entity();
            Assert.True(resolve(zone, X, Y, out var restored, out _)); Frame(restored, 1, 2);
            cover.GetPart<RenderPart>().ParentEntity = cover; Assert.True(zone.RemoveEntity(cover));
            Assert.True(resolve(zone, X + 1, Y, out var eastTile, out _)); Frame(eastTile, 1, 8);
            Assert.AreSame(east, zone.GetCell(X + 1, Y).GetTopVisibleObject());
        }

        [Test] public void WornLowCrossingKeepsWalkingCollisionWhileItsSightTagRemainsAbsent()
        {
            var center = Wall(zone, X, Y, 0); center.Tags.Remove("Wall"); var resolve = Query(New(atlas, selector));
            Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.False(zone.GetCell(X, Y).IsWall());
            Assert.True(resolve(zone, X, Y, out var tile, out _)); Frame(tile, 0, 0);
            Assert.True(zone.GetCell(X, Y).BlocksMovement()); Assert.False(center.HasTag("Wall"));
            center.GetPart<PhysicsPart>().Solid = false; center.Tags.Remove("Solid");
            Assert.False(resolve(zone, X, Y, out _, out _)); Assert.False(zone.GetCell(X, Y).BlocksMovement());
        }

        [Test] public void UndisclosedNeighborsNeverConnectAndMemoryNeverReadsLiveDamageOrAdjacency()
        {
            var center = Wall(zone, X, Y, 1); Wall(zone, X + 1, Y, 1); var eastCell = zone.GetCell(X + 1, Y);
            var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out var connected, out _)); Frame(connected, 1, 2);
            eastCell.IsVisible = false; Assert.True(resolve(zone, X, Y, out var capped, out _)); Frame(capped, 1, 0);
            eastCell.Explored = false; Assert.True(resolve(zone, X, Y, out capped, out _)); Frame(capped, 1, 0);
            zone.GetCell(X, Y).IsVisible = false; center.GetPart<DestructiblePart>().HP = 0; center.GetPart<DestructiblePart>().Gone = true;
            center.GetPart<PhysicsPart>().Solid = false; center.Tags.Remove("Solid");
            eastCell.IsVisible = eastCell.Explored = true;
            Assert.True(resolve(zone, X, Y, out var memory, out var tint)); Frame(memory, 1, 0);
            Assert.AreEqual(new Color(.35f, .35f, .35f, 1), tint);
            zone.GetCell(X, Y).IsVisible = true; Assert.False(resolve(zone, X, Y, out _, out _));
            zone.GetCell(X, Y).Explored = false; Assert.False(resolve(zone, X, Y, out _, out _));
        }

        [Test] public void RememberedLiveFamilyDisguiseCannotBecomeStaticMasonry()
        {
            var center = Wall(zone, X, Y, 1); center.AddPart(new HarvestablePart()); var resolve = Query(New(atlas, selector));
            Assert.True(resolve(zone, X, Y, out _, out _));
            zone.GetCell(X, Y).IsVisible = false; Assert.False(resolve(zone, X, Y, out _, out _));
        }

        [Test] public void ReplacedCellsStaleListsAndDuplicateIdsCannotSupplyConnectivity()
        {
            Wall(zone, X, Y, 1); var east = Wall(zone, X + 1, Y, 1); var cell = zone.GetCell(X + 1, Y);
            var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out var connected, out _)); Frame(connected, 1, 2);
            Assert.True(zone.MoveEntity(east, X + 2, Y)); cell.Objects.Add(east);
            Assert.True(resolve(zone, X, Y, out var moved, out _)); Frame(moved, 1, 0);
            var phantom = MakeWall(1); phantom.ID = east.ID; cell.Objects.Add(phantom);
            Assert.True(resolve(zone, X, Y, out var duplicate, out _)); Frame(duplicate, 1, 0);
            var replacement = new Cell(X, Y, zone) { Explored = true, IsVisible = true };
            replacement.Objects.AddRange(zone.GetCell(X, Y).Objects); zone.Cells[X, Y] = replacement;
            Assert.False(resolve(zone, X, Y, out _, out _));
        }

        [Test] public void SameAddressManagerReplacementRejectsOldGraphAndRecoveryUsesExactReference()
        {
            Wall(zone, X, Y, 1); var manager = OverworldZoneManager.CreateDetached(DensityReferenceGladeTests.Factory(), 64, true);
            manager.SetActiveZone(zone); var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out _, out _));
            manager.CachedZones[zone.ZoneID] = World(); Assert.False(resolve(zone, X, Y, out _, out _));
            manager.CachedZones[zone.ZoneID] = zone; Assert.True(resolve(zone, X, Y, out _, out _));
        }

        [Test] public void ExactAddressStillRequiresTheManagedOpeningBiomePoiAndContentAuthority()
        {
            Wall(zone, X, Y, 1); var manager = OverworldZoneManager.CreateDetached(DensityReferenceGladeTests.Factory(), 64, true);
            manager.SetActiveZone(zone); var resolve = Query(New(atlas, selector)); Assert.True(resolve(zone, X, Y, out _, out _));
            manager.WorldMap.Tiles[11, 10] = BiomeType.Sodden; Assert.False(resolve(zone, X, Y, out _, out _));
            manager.WorldMap.Tiles[11, 10] = BiomeType.Spread; Assert.True(resolve(zone, X, Y, out _, out _));
            manager.WorldMap.SetPOI(11, 10, new PointOfInterest(POIType.Village, "boundary scope counter", null, 1));
            Assert.False(resolve(zone, X, Y, out _, out _)); manager.WorldMap.SetPOI(11, 10, null); Assert.True(resolve(zone, X, Y, out _, out _));
            typeof(ReferenceGladePlan).GetMethod("Attach", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { zone, manager.WorldMap, false });
            Assert.False(resolve(zone, X, Y, out _, out _));
        }

        [TestCase("Overworld.11.10.1")] [TestCase("Overworld.11.9.0")] [TestCase("Overworld.12.12.0")]
        public void KnownVisualIdOutsideExactOpeningCannotImportItsMasonryFamily(string id)
        {
            var elsewhere = World(id); Wall(elsewhere, X, Y, 1);
            Assert.False(Query(New(atlas, selector))(elsewhere, X, Y, out _, out _));
        }

        [Test] public void NonSingleCellFootprintIsRejectedEvenWhenItsAnchorAndGeometryAreCurrent()
        {
            var center = MakeWall(1); center.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" }); Assert.True(zone.AddEntity(center, X, Y));
            Assert.True(selector.TryResolve(zone, X, Y, out var selected)); Assert.AreSame(center, selected.Owner);
            var resolve = Query(New(atlas, selector)); Assert.False(resolve(zone, X, Y, out _, out _)); Assert.False(resolve(zone, X + 1, Y, out _, out _));
        }

        [TestCase(0, 0)] [TestCase(255, 72)] [TestCase(256, 71)] [TestCase(272, 72)]
        public void MissingOrMalformedOptionalAtlasFallsThroughWithoutDamagingBorrowedSelector(int width, int height)
        {
            Wall(zone, X, Y, 1); var source = width == 0 ? null : Texture(width, height);
            var adapter = New(source, selector); Assert.False(Query(adapter)(zone, X, Y, out _, out _));
            adapter.Dispose(); Assert.True(selector.TryResolve(zone, X, Y, out var glyph)); Assert.NotNull(glyph.ForegroundTile);
            if (source != null) Assert.False(source == null);
        }

        [Test] public void IndependentCatalogsOwnOnlyCachedTilesAndSpritesNotAtlasOrSelector()
        {
            Wall(zone, X, Y, 1); var first = New(atlas, selector); var second = New(atlas, selector);
            Assert.True(Query(first)(zone, X, Y, out var a, out _)); Assert.True(Query(second)(zone, X, Y, out var b, out _));
            Assert.AreNotSame(a, b); Assert.AreSame(a.sprite.texture, b.sprite.texture); var sprite = a.sprite;
            first.Dispose(); first.Dispose(); Assert.True(a == null); Assert.True(sprite == null); Assert.False(atlas == null);
            Assert.False(Query(first)(zone, X, Y, out _, out _)); Assert.True(Query(second)(zone, X, Y, out _, out _));
            Assert.True(selector.TryResolve(zone, X, Y, out var glyph)); Assert.NotNull(glyph.ForegroundTile);
        }

        [Test] public void ExistingInjectedCatalogDoesNotLoadNewResourcesAndMissingBoundaryPreservesItsApprovedWall()
        {
            var baseArt = Texture(160, 24); Wall(zone, X, Y, 1); Wall(zone, X + 1, Y, 1);
            using (var world = new FlatPixelGreyboxCatalog(baseArt))
            {
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var before)); Assert.AreSame(baseArt, before.ForegroundTile.sprite.texture);
                var shared = new FlatPixelFallbackCatalog();
                var fallbackField = typeof(FlatPixelGreyboxCatalog).GetField("_glyphFallback", I);
                ((IDisposable)fallbackField.GetValue(world))?.Dispose(); fallbackField.SetValue(world, shared);
                var field = typeof(FlatPixelGreyboxCatalog).GetField("_boundaryArt", I); Assert.NotNull(field);
                field.SetValue(world, Construct(null, shared));
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var after)); Assert.AreSame(before.ForegroundTile, after.ForegroundTile);
                Assert.AreSame(baseArt, after.ForegroundTile.sprite.texture);
            }
            Assert.False(baseArt == null);
        }

        [Test] public void WarmFiveCellResolutionDoesNotAllocateOrMutateSimulation()
        {
            var center = Wall(zone, X, Y, 1); Neighbors(zone, 15, 1); var resolve = Query(New(atlas, selector));
            for (int i = 0; i < 256; i++) resolve(zone, X, Y, out _, out _);
            int version = zone.EntityVersion, hp = center.GetPart<DestructiblePart>().HP;
            long before = GC.GetAllocatedBytesForCurrentThread(); bool all = true;
            for (int i = 0; i < 1024; i++) all &= resolve(zone, X, Y, out _, out _);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(all); Assert.Zero(allocated); Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(hp, center.GetPart<DestructiblePart>().HP);
        }

        [Test] public void ExactSubjectPortraitUsesIsolatedMaterialFrameWithoutQueryingNeighbors()
        {
            var owner = Wall(zone, X, Y, 2); Neighbors(zone, 15, 1); var adapter = New(atlas, selector);
            var method = adapter.GetType().GetMethod("TryGetSubjectSprite", I); Assert.NotNull(method);
            var portrait = (Subject)Delegate.CreateDelegate(typeof(Subject), adapter, method);
            Assert.True(portrait(owner, out var sprite, out var tint)); Assert.AreEqual(Rect(2, 0), sprite.rect); Assert.AreEqual(Color.white, tint);
            owner.BlueprintName = "GlowQuartzVein"; Assert.False(portrait(owner, out _, out _));
        }

        [Test] public void ActualResourceLoadsAllFortyEightFramesAndDefaultWorldAndPortraitUseTheSameGroup()
        {
            var resource = Resources.Load<Texture2D>(Resource); Assert.NotNull(resource, "Genuine absent-resource RED gate: do not substitute source art or a synthetic texture.");
            Assert.AreEqual(256, resource.width); Assert.AreEqual(72, resource.height); Assert.AreEqual(FilterMode.Point, resource.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, resource.wrapMode); Assert.AreEqual(1, resource.mipmapCount);
            var adapter = NewDefault(selector); var resolve = Query(adapter);
            for (int material = 0; material < 3; material++) for (int mask = 0; mask < 16; mask++)
            {
                var world = World(); Wall(world, X, Y, material); Neighbors(world, mask, material);
                Assert.True(resolve(world, X, Y, out var tile, out _)); Assert.AreSame(resource, tile.sprite.texture); Assert.AreEqual(Rect(material, mask), tile.sprite.rect);
            }
            using (var world = new FlatPixelGreyboxCatalog())
            {
                var owner = Wall(zone, X, Y, 1); Wall(zone, X + 1, Y, 1);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var selected)); Assert.AreSame(resource, selected.ForegroundTile.sprite.texture); Assert.AreEqual(Rect(1, 2), selected.ForegroundTile.sprite.rect);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _)); Assert.AreSame(resource, sprite.texture); Assert.AreEqual(Rect(1, 0), sprite.rect);
            }
        }

        [Test] public void ActualDestructionDirtiesOnlyItsCellAndNeighborPassCapsTheAlreadySubmittedWall()
        {
            using (var f = new PaintFixture(atlas))
            {
                Wall(f.Zone, X, Y, 1); var east = Wall(f.Zone, X + 1, Y, 1);
                f.Renderer.MarkDirty("initial boundaries"); f.Frame(); Frame(f.At(X, Y), 1, 2); f.Quiet();
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(east, null, f.Zone, "boundary test"));
                Assert.False(f.FullDirty); Assert.True(f.Dirty.Contains(Y * Zone.Width + X + 1)); Assert.False(f.Dirty.Contains(Y * Zone.Width + X));
                f.Frame(); Frame(f.At(X, Y), 1, 0); Assert.IsNull(f.At(X + 1, Y)); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; f.Frame(); Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved); f.Quiet();
            }
        }

        [Test] public void DisclosureAndTopOwnerChangesRepaintAdjacentConnectionsWithoutFullRedraw()
        {
            using (var f = new PaintFixture(atlas))
            {
                Wall(f.Zone, X, Y, 1); Wall(f.Zone, X + 1, Y, 1);
                f.Renderer.MarkDirty("initial boundaries"); f.Frame(); Frame(f.At(X, Y), 1, 2); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                f.Zone.GetCell(X + 1, Y).IsVisible = false; f.Renderer.MarkCellDirty(X + 1, Y, "FOV changed"); f.Frame(); Frame(f.At(X, Y), 1, 0);
                f.Zone.GetCell(X + 1, Y).IsVisible = true; f.Renderer.MarkCellDirty(X + 1, Y, "FOV reveal"); f.Frame(); Frame(f.At(X, Y), 1, 2);
                var cover = Add(f.Zone, "Higher owner", X + 1, Y, 7); f.Renderer.MarkCellDirty(X + 1, Y, "placement committed"); f.Frame(); Frame(f.At(X, Y), 1, 0);
                Assert.True(f.Zone.RemoveEntity(cover)); f.Renderer.MarkCellDirty(X + 1, Y, "removal committed"); f.Frame(); Frame(f.At(X, Y), 1, 2);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses); f.Quiet();
            }
        }

        [Test] public void ActualSubmittedMemoryUsesOneDimmingPassAndNeverKeepsLiveConnections()
        {
            using (var f = new PaintFixture(atlas))
            {
                Wall(f.Zone, X, Y, 1); Wall(f.Zone, X + 1, Y, 1);
                f.Renderer.MarkDirty("initial boundaries"); f.Frame(); Frame(f.At(X, Y), 1, 2); Assert.AreEqual(Color.white, f.TintAt(X, Y));
                f.Zone.GetCell(X, Y).IsVisible = false; f.Renderer.MarkCellDirty(X, Y, "FOV memory"); f.Frame();
                Frame(f.At(X, Y), 1, 0); Assert.AreEqual(new Color(.35f, .35f, .35f, 1), f.TintAt(X, Y), "Adapter memory tint must not be dimmed a second time by the native-world route.");
                f.Zone.GetCell(X, Y).IsVisible = true; f.Renderer.MarkCellDirty(X, Y, "FOV reveal"); f.Frame();
                Frame(f.At(X, Y), 1, 2); Assert.AreEqual(Color.white, f.TintAt(X, Y));
            }
        }

        [Test]
        public void RealFactoryWallDamageLeavesRubbleWhileSubmittedNeighborCaps()
        {
            using (var f = new PaintFixture(atlas))
            {
                var factory = DensityReferenceGladeTests.Factory();
                var oldFactory = DestructionSystem.EntityFactoryRef;
                try
                {
                    DestructionSystem.EntityFactoryRef = factory;
                    var center = factory.CreateEntity("Wall");
                    var east = factory.CreateEntity("Wall");
                    Assert.NotNull(center); Assert.NotNull(east);
                    center.GetPart<RenderPart>().VisualID = "reference-glade-dark-ruin";
                    east.GetPart<RenderPart>().VisualID = "reference-glade-dark-ruin";
                    Assert.AreEqual("Rubble", east.GetPart<DestructiblePart>().WreckageBlueprint);
                    Assert.True(f.Zone.AddEntity(center, X, Y));
                    Assert.True(f.Zone.AddEntity(east, X + 1, Y));
                    f.Renderer.MarkDirty("real factory boundaries"); f.Frame();
                    Frame(f.At(X, Y), 1, 2); Frame(f.At(X + 1, Y), 1, 8); f.Quiet();
                    long full = EnvironmentSpriteRenderer.Perf.FullPasses;

                    Assert.AreEqual(DestroyVerdict.Destroyed,
                        DestructionSystem.Damage(east, 999, null, f.Zone));
                    Assert.True(east.GetPart<DestructiblePart>().Gone);
                    Assert.IsNull(f.Zone.GetEntityCell(east));
                    Assert.False(f.FullDirty);
                    Assert.True(f.Dirty.Contains(Y * Zone.Width + X + 1));
                    Assert.False(f.Dirty.Contains(Y * Zone.Width + X));

                    var cell = f.Zone.GetCell(X + 1, Y);
                    Assert.AreEqual(1, cell.Objects.Count);
                    var rubble = cell.Objects[0];
                    Assert.AreEqual("Rubble", rubble.BlueprintName);
                    Assert.AreSame(cell, f.Zone.GetEntityCell(rubble));
                    Assert.False(cell.BlocksMovement());
                    Assert.IsTrue(string.IsNullOrEmpty(rubble.GetPart<RenderPart>().VisualID),
                        "Real wreckage does not inherit the removed wall's native role.");
                    Assert.True(selector.TryResolve(f.Zone, X + 1, Y, out var expected));
                    Assert.AreSame(rubble, expected.Owner);
                    Assert.NotNull(expected.ForegroundTile);

                    // No manually injected dirty signal after destruction. The actual
                    // commit hook repaints the changed cell and the surviving neighbor.
                    f.Frame(); Frame(f.At(X, Y), 1, 0);
                    var painted = f.At(X + 1, Y);
                    Assert.NotNull(painted, "Truthful wreckage must not disappear.");
                    Assert.AreNotSame(atlas, painted.sprite.texture,
                        "Rubble must not retain a connected masonry sprite.");
                    Assert.AreEqual(expected.ForegroundTile.sprite.rect, painted.sprite.rect,
                        "The actual loose rubble glyph remains the fallback identity.");
                    Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                    f.Quiet();
                    long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved;
                    f.Frame();
                    Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
                    f.Quiet();
                }
                finally { DestructionSystem.EntityFactoryRef = oldFactory; }
            }
        }

        [TestCase(0, "visual-variant")]
        [TestCase(1, "visual-variant")]
        [TestCase(2, "glyph-variants")]
        [TestCase(1, "missing-destructible")]
        [TestCase(2, "foreign-destructible")]
        [TestCase(0, "gone")]
        [TestCase(2, "zero-hp")]
        [TestCase(0, "non-solid")]
        public void RejectedKnownBoundaryKeepsExactGlyphInsteadOfBorrowingOlderWallArt(int material, string fault)
        {
            var baseArt = Texture(160, 24); var anchors = Texture(192, 24);
            var owner = Wall(zone, X, Y, material); Wall(zone, X + 1, Y, material);
            using (var world = new FlatPixelGreyboxCatalog(baseArt, null, anchors))
            {
                InjectBoundary(world, atlas);
                // Portrait first is intentional: selecting a subject must not
                // require a prior world-paint call to initialize its policy.
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out var tint));
                Assert.AreSame(atlas, portrait.texture); Assert.AreEqual(Rect(material, 0), portrait.rect);
                Assert.AreEqual(Color.white, tint);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var positive));
                Frame(positive.ForegroundTile, material, 2); Assert.True(positive.NativeForeground);
                Assert.AreEqual(Color.white, positive.ForegroundColor);

                Fault(owner, fault);
                Assert.True(world.TryResolveFallback(zone, X, Y, out var expected));
                Assert.AreSame(owner, expected.Owner, "This fault must leave the real visible glyph owner selectable.");
                Assert.NotNull(expected.ForegroundTile);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var rejected));
                Assert.AreSame(expected.ForegroundTile, rejected.ForegroundTile,
                    "A rejected structural/native role must not fall through into older wall art.");
                Assert.AreEqual(expected.ForegroundColor, rejected.ForegroundColor);
                Assert.False(rejected.NativeForeground);

                Assert.True(world.TryGetFallbackSubjectSprite(owner, X, Y, out var glyph, out var glyphTint));
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out portrait, out tint));
                Assert.AreSame(glyph, portrait); Assert.AreEqual(glyphTint, tint);
            }
        }

        [TestCase(0)] [TestCase(2)]
        public void MissingOptionalBoundaryKeepsValidPreviousWallInWorldAndFirstSubjectQuery(int material)
        {
            var baseArt = Texture(160, 24); var anchors = Texture(192, 24);
            var expectedTexture = material == 2 ? anchors : baseArt;
            var expectedRect = new UnityEngine.Rect((material == 2 ? 11 : 2) * 16, 0, 16, 24);
            var owner = Wall(zone, X, Y, material);
            using (var world = new FlatPixelGreyboxCatalog(baseArt, null, anchors))
            {
                InjectBoundary(world, null);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out var tint));
                Assert.AreSame(expectedTexture, portrait.texture); Assert.AreEqual(expectedRect, portrait.rect);
                Assert.AreEqual(Color.white, tint);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var selected));
                Assert.AreSame(expectedTexture, selected.ForegroundTile.sprite.texture);
                Assert.AreEqual(expectedRect, selected.ForegroundTile.sprite.rect);
                Assert.True(selected.NativeForeground); Assert.AreEqual(Color.white, selected.ForegroundColor);
            }
        }

        [Test]
        public void ExplicitOldScopedApisKeepTheirOriginalWallPolicyEvenWithBoundaryInjected()
        {
            var baseArt = Texture(160, 24); var anchors = Texture(192, 24);
            var owner = Wall(zone, X, Y, 1); Fault(owner, "visual-variant");
            using (var world = new FlatPixelGreyboxCatalog(baseArt, null, anchors))
            {
                InjectBoundary(world, atlas);
                Assert.True(world.TryResolve(zone, X, Y, out var oldWorld));
                Assert.AreSame(baseArt, oldWorld.ForegroundTile.sprite.texture);
                Assert.AreEqual(new UnityEngine.Rect(16, 0, 16, 24), oldWorld.ForegroundTile.sprite.rect);
                Assert.True(world.TryGetSubjectSprite(owner, X, Y, out var oldPortrait, out var tint));
                Assert.AreSame(baseArt, oldPortrait.texture); Assert.AreEqual(oldWorld.ForegroundTile.sprite.rect, oldPortrait.rect);
                Assert.AreEqual(Color.white, tint);
            }
            // A pre-G2 injected catalog does not opt into the new policy merely
            // because the build eventually contains an optional resource.
            using (var oldInjection = new FlatPixelGreyboxCatalog(baseArt, null, anchors))
            {
                Assert.True(oldInjection.TryResolvePresentation(zone, X, Y, out var oldWorld));
                Assert.AreSame(baseArt, oldWorld.ForegroundTile.sprite.texture);
                Assert.True(oldInjection.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var oldPortrait, out _));
                Assert.AreSame(baseArt, oldPortrait.texture);
            }
        }

        void InjectBoundary(FlatPixelGreyboxCatalog world, Texture2D source)
        {
            // The world owns both objects; its Dispose releases the adapter
            // before the shared selector. Source textures remain fixture-owned.
            var fallback = new FlatPixelFallbackCatalog();
            var fallbackField = typeof(FlatPixelGreyboxCatalog).GetField("_glyphFallback", I);
            Assert.NotNull(fallbackField);
            ((IDisposable)fallbackField.GetValue(world))?.Dispose(); fallbackField.SetValue(world, fallback);
            var boundaryField = typeof(FlatPixelGreyboxCatalog).GetField("_boundaryArt", I);
            Assert.NotNull(boundaryField); boundaryField.SetValue(world, Construct(source, fallback));
        }

        Texture2D Texture(int width, int height)
        { var t = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp }; textures.Add(t); return t; }
        static Zone World(string id = ReferenceGladePlan.ZoneID)
        { var world = new Zone(id); for (int y = Y - 2; y <= Y + 2; y++) for (int x = X - 2; x <= X + 3; x++) world.GetCell(x, y).Explored = world.GetCell(x, y).IsVisible = true; return world; }
        static Entity MakeWall(int material)
        {
            var owner = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "Wall" };
            owner.SetTag("Wall"); owner.SetTag("Solid"); owner.SetTag("Terrain");
            owner.AddPart(new RenderPart { RenderString = "#", ColorString = "&G", RenderLayer = 1, VisualID = "reference-glade-" + new[] { "low-wall", "dark-ruin", "lit-wall" }[material] });
            owner.AddPart(new PhysicsPart { Solid = true, Takeable = false }); owner.AddPart(new DestructiblePart { HP = 40, MaxHP = 40, Hardness = 3 });
            return owner;
        }
        static Entity Wall(Zone world, int x, int y, int material)
        { var owner = MakeWall(material); Assert.True(world.AddEntity(owner, x, y)); return owner; }
        static Entity Add(Zone world, string name, int x, int y, int layer)
        { var owner = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = name }; owner.AddPart(new RenderPart { RenderString = "?", ColorString = "&R", RenderLayer = layer }); owner.AddPart(new PhysicsPart()); Assert.True(world.AddEntity(owner, x, y)); return owner; }
        static void Neighbors(Zone world, int mask, int material)
        { if ((mask & 1) != 0) Wall(world, X, Y - 1, material); if ((mask & 2) != 0) Wall(world, X + 1, Y, material); if ((mask & 4) != 0) Wall(world, X, Y + 1, material); if ((mask & 8) != 0) Wall(world, X - 1, Y, material); }
        static void Fault(Entity owner, string fault)
        {
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>(); var structural = owner.GetPart<DestructiblePart>(); var foreign = new Entity();
            switch (fault)
            {
                case "visual-id": render.VisualID = "foreign-wall"; break;
                case "visual-variant": render.VisualVariant = "other"; break;
                case "glyph-variants": render.GlyphVariants = "#%"; break;
                case "foreign-render": render.ParentEntity = foreign; break;
                case "foreign-physics": physics.ParentEntity = foreign; break;
                case "foreign-destructible": structural.ParentEntity = foreign; break;
                case "missing-physics": owner.RemovePart(physics); break;
                case "missing-destructible": owner.RemovePart(structural); break;
                case "carried": physics.InInventory = foreign; break;
                case "equipped": physics.Equipped = foreign; break;
                case "takeable": physics.Takeable = true; break;
                case "gone": structural.Gone = true; break;
                case "zero-hp": structural.HP = 0; break;
                case "non-solid": physics.Solid = false; owner.Tags.Remove("Solid"); break;
            }
        }
        static Rect Rect(int material, int mask) => new Rect(mask * 16, (2 - material) * 24, 16, 24);
        void Frame(Tile tile, int material, int mask)
        { Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(atlas, tile.sprite.texture); Assert.AreEqual(Rect(material, mask), tile.sprite.rect); Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelBoundaryCatalog"); Assert.NotNull(type, "Optional one-family boundary adapter is not implemented."); return type; }
        IDisposable New(Texture2D source, FlatPixelFallbackCatalog borrowed)
        { var instance = Construct(source, borrowed); owned.Add(instance); return instance; }
        static IDisposable Construct(Texture2D source, FlatPixelFallbackCatalog borrowed)
        { var ctor = AdapterType().GetConstructor(new[] { typeof(Texture2D), typeof(FlatPixelFallbackCatalog) }); Assert.NotNull(ctor, "Explicit injected atlas and shared borrowed selector are required."); return (IDisposable)ctor.Invoke(new object[] { source, borrowed }); }
        IDisposable NewDefault(FlatPixelFallbackCatalog borrowed)
        { var ctor = AdapterType().GetConstructor(new[] { typeof(FlatPixelFallbackCatalog) }); Assert.NotNull(ctor); var instance = (IDisposable)ctor.Invoke(new object[] { borrowed }); owned.Add(instance); return instance; }
        static Resolve Query(object adapter)
        { var method = adapter.GetType().GetMethod("TryResolve", I); Assert.NotNull(method); return (Resolve)Delegate.CreateDelegate(typeof(Resolve), adapter, method); }

        sealed class PaintFixture : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject gridRoot;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback; readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries(); readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            readonly Tilemap overlay;
            readonly FlatPixelGreyboxCatalog worldCatalog;
            public readonly Zone Zone; public readonly ZoneRenderer Renderer; public readonly EnvironmentSpriteRenderer Environment;
            public HashSet<int> Dirty => (HashSet<int>)Read(Renderer, "_dirtyCells");
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");
            public PaintFixture(Texture2D atlas)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null;
                    saved = new HotbarSaveFixture(); gridRoot = new GameObject("Boundary cadence active Grid", typeof(Grid)); saved.Root.transform.SetParent(gridRoot.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); Assert.NotNull(main, "ZoneRenderer requires the owned main Tilemap."); var backgroundHost = new GameObject("Background", typeof(Tilemap)); backgroundHost.transform.SetParent(saved.Root.transform, false);
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", backgroundHost.GetComponent<Tilemap>());
                    var camera = new GameObject("Owned boundary camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    Environment = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); Environment.Init(saved.Root.transform, main, backgroundHost.GetComponent<Tilemap>()); Environment.RenderingEnabled = true;
                    worldCatalog = new FlatPixelGreyboxCatalog(null); var fallback = new FlatPixelFallbackCatalog();
                    Write(Environment, "_flatPixelGreybox", worldCatalog); Write(worldCatalog, "_glyphFallback", fallback); Write(worldCatalog, "_boundaryArt", Construct(atlas, fallback));
                    overlay = (Tilemap)Read(Environment, "_overlayTilemap"); Write(Renderer, "_envSpriteRenderer", Environment); Write(Renderer, "_flatPixelModeEnabled", true);
                    Zone = World(); Renderer.SetZone(Zone); ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty; Frame(); Quiet();
                }
                catch { Dispose(); throw; }
            }
            public void Frame()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0));
            public Color TintAt(int x, int y) => overlay.GetColor(new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0));
            public void Quiet() { Assert.False(FullDirty); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (gridRoot != null) Object.DestroyImmediate(gridRoot); }
                finally
                {
                    // Explicit ownership cleanup also covers missing-API RED on
                    // a fixture host that has never been active in the editor.
                    worldCatalog?.Dispose();
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
            static object Read(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
            static void Write(object owner, string name, object value)
            { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, "Expected bounded integration seam: " + name); field.SetValue(owner, value); }
        }
    }
}
