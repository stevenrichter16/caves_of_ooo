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
    // The first block pins an existing visible ground bug independently of the
    // future optional atlas/API. Native art must not gate its behavioral RED.
    public sealed class FlatPixelOpeningGroundCatalogTests
    {
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Color DrySoil = new Color32(69, 52, 31, 255);
        static readonly Color PlainGround = new Color32(3, 47, 35, 255);

        [TestCase("CropRow", "Player")]
        [TestCase("CropRow", "HealingTonic")]
        [TestCase("RipeCropRow", "Player")]
        [TestCase("RipeCropRow", "HealingTonic")]
        public void ActualSubmittedFieldSoilSurvivesARealForegroundCoverUntilTheRowIsRemoved(string rowBlueprint, string coverBlueprint)
        {
            using (var f = new PaintFixture())
            {
                var factory = DensityReferenceGladeTests.Factory();
                var grass = factory.CreateEntity("Grass");
                var row = factory.CreateEntity(rowBlueprint);
                Assert.True(f.Zone.AddEntity(grass, X, Y));
                Assert.True(f.Zone.AddEntity(row, X, Y));
                Assert.IsNull(row.GetPart<CropPart>(), "The authored finite field row is not a growing crop.");
                Assert.IsNull(row.GetPart<CultivatedSoilPart>(), "No preparation marker may accidentally make this precondition pass.");
                f.Renderer.MarkDirty("field fixture initial placement"); f.Frame();
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var bare));
                Assert.AreSame(row, bare.Owner); Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, bare.GroundKind);
                Assert.AreEqual(DrySoil, f.GroundTint(X, Y));
                var originalGround = f.GroundAt(X, Y); Assert.NotNull(originalGround);

                var cover = factory.CreateEntity(coverBlueprint);
                Assert.NotNull(cover); cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Zone.AddEntity(cover, X, Y));
                // This explicitly publishes placement dirtiness. The regression
                // is ground selection under cover, not the placement producer.
                f.Renderer.MarkCellDirty(X, Y, "field fixture cover committed"); f.Frame();
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var covered));
                Assert.AreSame(cover, covered.Owner);
                Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, covered.GroundKind,
                    "The current lower field row must retain its dry substrate under an actor or loose item.");
                Assert.AreSame(originalGround, f.GroundAt(X, Y));
                Assert.AreEqual(DrySoil, f.GroundTint(X, Y));
                Assert.NotNull(f.ForegroundAt(X, Y)); f.Quiet();

                Assert.True(f.Zone.RemoveEntity(row));
                f.Renderer.MarkCellDirty(X, Y, "field fixture row removed"); f.Frame();
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var removed));
                Assert.AreSame(cover, removed.Owner); Assert.AreEqual(FlatPixelGreyboxGround.Ground, removed.GroundKind);
                Assert.AreEqual(PlainGround, f.GroundTint(X, Y)); f.Quiet();
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved;
                f.Frame(); Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        [TestCase("CropRow", "foreign-render")]
        [TestCase("CropRow", "hidden-render")]
        [TestCase("CropRow", "foreign-physics")]
        [TestCase("RipeCropRow", "carried")]
        [TestCase("RipeCropRow", "equipped")]
        [TestCase("RipeCropRow", "stale-list")]
        public void ExistingColorLookupCannotUseAnInvalidLowerRow(string blueprint, string fault)
        {
            using (var f = new PaintFixture())
            {
                var factory = DensityReferenceGladeTests.Factory();
                var row = factory.CreateEntity(blueprint); var cover = factory.CreateEntity("Player");
                cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Zone.AddEntity(row, X, Y)); Assert.True(f.Zone.AddEntity(cover, X, Y));
                f.Renderer.MarkDirty("lower-row guard precondition"); f.Frame();
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var positive));
                Assert.AreSame(cover, positive.Owner); Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, positive.GroundKind);
                Assert.AreEqual(DrySoil, f.GroundTint(X, Y));
                FaultGround(f.Zone, row, fault);
                f.Renderer.MarkCellDirty(X, Y, "fixture invalid lower row"); f.Frame();
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var negative));
                Assert.AreSame(cover, negative.Owner); Assert.AreEqual(FlatPixelGreyboxGround.Ground, negative.GroundKind);
                Assert.AreEqual(PlainGround, f.GroundTint(X, Y)); f.Quiet();
            }
        }

        [Test] public void PreparedHarvestedFieldTakesPriorityOverItsOldFieldTexture()
        {
            using (var f = new CatalogFixture())
            {
                var field = f.Place("RipeCropRow"); field.GetPart<FieldHarvestPart>().Harvested = true;
                Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 18);
                field.SetTag("Plantable"); field.AddPart(new CultivatedSoilPart());
                Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 19);
                var crop = f.Place("DrawgourdCrop").GetPart<CropPart>(); crop.GrowthStage = 1; crop.MoistureTicks = 2;
                Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 20);
                Assert.True(field.GetPart<FieldHarvestPart>().Harvested, "Ground art must not reset the finite field yield.");
            }
        }

        [Test] public void UnpreparedGenericCropRetainsItsColorCueInsteadOfBorrowingUnderlyingRoadArt()
        {
            using (var f = new CatalogFixture())
            {
                f.Place("RoadStone"); Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 16);
                var crop = f.Place("EmberwheatCrop").GetPart<CropPart>(); crop.GrowthStage = 1; crop.MoistureTicks = 3;
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [Test] public void ActualCoatingDirtyHookRepaintsNativeGroundAndItsBankBelowAnUnchangedActor()
        {
            var atlas = new Texture2D(336, 24);
            try
            {
                using (var f = new PaintFixture())
                {
                    f.UseGroundAtlas(atlas);
                    Assert.True(f.Zone.AddEntity(DensityReferenceGladeTests.Factory().CreateEntity("Player"), X, Y));
                    f.Zone.TileState.WriteCoating(X, Y, "water", 7);
                    f.Zone.TileState.WriteCoating(X + 1, Y, "water", 7);
                    f.Renderer.MarkDirty("native-ground initial fixture"); f.Frame(); f.Quiet();
                    var foreground = f.ForegroundAt(X, Y); Assert.NotNull(foreground);
                    Assert.AreSame(atlas, f.GroundAt(X, Y).sprite.texture);
                    Assert.AreEqual(new Rect(13 * 16, 0, 16, 24), f.GroundAt(X, Y).sprite.rect);
                    Assert.AreEqual(Color.white, f.GroundTint(X, Y));
                    long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                    Assert.True(f.Zone.TileState.RemoveCoating(X + 1, Y, "water"));
                    Assert.True(f.Dirty.Contains(Y * Zone.Width + X + 1), "Actual coating hook, not a manually supplied dirty signal.");
                    f.Frame(); f.Quiet();
                    Assert.AreEqual(new Rect(15 * 16, 0, 16, 24), f.GroundAt(X, Y).sprite.rect);
                    Assert.AreSame(foreground, f.ForegroundAt(X, Y)); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                    Assert.True(f.Zone.TileState.RemoveCoating(X, Y, "water")); f.Frame();
                    Assert.AreNotSame(atlas, f.GroundAt(X, Y).sprite.texture); Assert.AreEqual(PlainGround, f.GroundTint(X, Y));
                    long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; f.Frame();
                    Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved); f.Quiet();
                }
            }
            finally { Object.DestroyImmediate(atlas); }
        }

        const string Resource = "Sprites/Live16x24/opening-ground";
        delegate bool GroundQuery(Zone zone, int x, int y, out Tile tile, out Color tint);

        [Test] public void WaterUsesAllSixteenDisclosedDryEdgeMasksWithoutChangingWorldState()
        {
            using (var f = new CatalogFixture())
            {
                var seen = new HashSet<Tile>();
                for (int mask = 0; mask < 16; mask++)
                {
                    var zone = World(); zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
                    WetNeighbors(zone, mask);
                    string state = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
                    Assert.True(f.Resolve(zone, X, Y, out var tile, out var tint)); f.Expect(tile, mask);
                    Assert.True(seen.Add(tile)); Assert.AreEqual(Color.white, tint);
                    Assert.True(f.Resolve(zone, X, Y, out var again, out _)); Assert.AreSame(tile, again);
                    Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
                }
            }
        }

        [TestCase("remembered")]
        [TestCase("unexplored")]
        [TestCase("water")]
        [TestCase("ice")]
        [TestCase("oil")]
        [TestCase("mixed")]
        public void ADisclosedDryNeighborAddsOneBankButUnknownOrLiquidDoesNot(string condition)
        {
            using (var f = new CatalogFixture())
            {
                f.Zone.TileState.WriteCoating(X, Y, "water", 7); WetNeighbors(f.Zone, 2);
                Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 2);
                var east = f.Zone.GetCell(X + 1, Y);
                if (condition == "remembered") east.IsVisible = false;
                else if (condition == "unexplored") east.Explored = false;
                else
                {
                    f.Zone.TileState.WriteCoating(X + 1, Y, condition == "mixed" ? "water" : condition, 7);
                    if (condition == "mixed") f.Zone.TileState.WriteCoating(X + 1, Y, "oil", 7);
                }
                Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 0);
            }
        }

        [TestCase("oil")]
        [TestCase("acid")]
        [TestCase("ice")]
        public void MixedCenterNeverUsesCleanWaterArtOrFallsThroughToRoad(string added)
        {
            using (var f = new CatalogFixture())
            {
                f.Place("RoadStone"); f.Zone.TileState.WriteCoating(X, Y, "water", 7);
                Assert.True(f.Resolve(f.Zone, X, Y, out _, out _));
                f.Zone.TileState.WriteCoating(X, Y, added, 7);
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [Test] public void WaterIsActualCellStateUnderTheForegroundAndNeverReconstructedFromPondCoordinates()
        {
            using (var f = new CatalogFixture())
            {
                var player = f.Place("Player");
                Assert.False(ReferenceGladePlan.IsPond(X, Y));
                f.Zone.TileState.WriteCoating(X, Y, "water", 3);
                Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 15);
                Assert.AreSame(player, f.Zone.GetCell(X, Y).GetTopVisibleObject());
                Assert.True(f.Zone.TileState.RemoveCoating(X, Y, "water"));
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
                var originalPond = f.Zone.GetCell(34, 5); originalPond.Explored = originalPond.IsVisible = true;
                Assert.True(ReferenceGladePlan.IsPond(34, 5));
                Assert.False(f.Resolve(f.Zone, 34, 5, out _, out _), "Original layout is not current water.");
            }
        }

        [TestCase("RoadStone", 16)]
        [TestCase("CropRow", 18)]
        [TestCase("RipeCropRow", 18)]
        public void ExactLowerPassableGroundSurvivesAVisiblePlayerCover(string blueprint, int frame)
        {
            using (var f = new CatalogFixture())
            {
                var ground = f.Place(blueprint); Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, frame);
                var cover = f.Place("Player"); cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.AreSame(cover, f.Zone.GetCell(X, Y).GetTopVisibleObject());
                Assert.True(f.Resolve(f.Zone, X, Y, out var covered, out _)); Assert.AreSame(tile, covered);
                Assert.True(f.Zone.RemoveEntity(ground)); Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [Test] public void RoadVariationUsesCoordinateParityWithoutChangingItsGeometry()
        {
            using (var f = new CatalogFixture())
            {
                var road = f.Place("RoadStone"); Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 16);
                Assert.True(f.Zone.MoveEntity(road, X + 1, Y));
                Assert.True(f.Resolve(f.Zone, X + 1, Y, out tile, out _)); f.Expect(tile, 17);
                Assert.False(f.Zone.GetCell(X + 1, Y).BlocksMovement()); Assert.False(f.Zone.GetCell(X + 1, Y).IsWall());
            }
        }

        [TestCase("foreign-render")]
        [TestCase("hidden-render")]
        [TestCase("foreign-physics")]
        [TestCase("missing-physics")]
        [TestCase("takeable")]
        [TestCase("solid")]
        [TestCase("carried")]
        [TestCase("equipped")]
        [TestCase("visual-id")]
        [TestCase("visual-variant")]
        [TestCase("glyph-variants")]
        [TestCase("foreign-cell")]
        [TestCase("stale-list")]
        [TestCase("footprint")]
        public void InvalidLowerRoadCannotSupplyNativeGroundEvenUnderAValidCover(string fault)
        {
            using (var f = new CatalogFixture())
            {
                var road = f.Place("RoadStone");
                // A nonblocking drop leaves the complete footprint placeable.
                // The ordinary Player remains the cover in all other counters.
                var cover = f.Place(fault == "footprint" ? "HealingTonic" : "Player"); cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Resolve(f.Zone, X, Y, out _, out _));
                FaultGround(f.Zone, road, fault);
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [TestCase("foreign-field")]
        [TestCase("foreign-soil")]
        public void OwnedFieldAndCultivationMarkersCannotBeBorrowedFromAnotherEntity(string fault)
        {
            using (var f = new CatalogFixture())
            {
                var ground = f.Place(fault == "foreign-field" ? "RipeCropRow" : "Grass");
                if (fault == "foreign-soil") ground.AddPart(new CultivatedSoilPart());
                Assert.True(f.Resolve(f.Zone, X, Y, out _, out _));
                if (fault == "foreign-field") ground.GetPart<FieldHarvestPart>().ParentEntity = new Entity();
                else ground.GetPart<CultivatedSoilPart>().ParentEntity = new Entity();
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [Test] public void PreparedGrassKeepsItsAuthoredVariantsAndMoistureComesOnlyFromItsCurrentCrop()
        {
            using (var f = new CatalogFixture())
            {
                var ground = f.Place("Grass"); var render = ground.GetPart<RenderPart>();
                render.VisualID = "reference-glade-ground";
                Assert.IsNotEmpty(render.GlyphVariants, "Actual Grass has authored glyph variants; it is not a generic explicit reskin.");
                ground.AddPart(new CultivatedSoilPart());
                Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 19);
                var plant = f.Place("DrawgourdCrop"); var crop = plant.GetPart<CropPart>(); crop.GrowthStage = 1; crop.MoistureTicks = 4;
                var cover = f.Place("Player"); cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 20);
                crop.MoistureTicks = 0; Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 19);
                crop.MoistureTicks = 4; Assert.True(f.Zone.RemoveEntity(plant));
                Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 19);
                Assert.True(ground.RemovePart(ground.GetPart<CultivatedSoilPart>()));
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [TestCase("part")]
        [TestCase("render")]
        [TestCase("hidden")]
        [TestCase("physics")]
        [TestCase("stage")]
        public void InvalidCropDoesNotKeepPreparedSoilWet(string fault)
        {
            using (var f = new CatalogFixture())
            {
                f.Place("Grass").AddPart(new CultivatedSoilPart()); var plant = f.Place("DrawgourdCrop");
                var crop = plant.GetPart<CropPart>(); crop.GrowthStage = 1; crop.MoistureTicks = 4;
                Assert.True(f.Resolve(f.Zone, X, Y, out var tile, out _)); f.Expect(tile, 20);
                if (fault == "part") crop.ParentEntity = new Entity();
                if (fault == "render") plant.GetPart<RenderPart>().ParentEntity = new Entity();
                if (fault == "hidden") plant.GetPart<RenderPart>().Visible = false;
                if (fault == "physics") plant.GetPart<PhysicsPart>().ParentEntity = new Entity();
                if (fault == "stage") crop.GrowthStage = 3;
                Assert.True(f.Resolve(f.Zone, X, Y, out tile, out _)); f.Expect(tile, 19);
            }
        }

        [Test] public void OrdinaryPlantableGrassAndUnpreparedGrowingCropDoNotBecomePreparedBeds()
        {
            using (var f = new CatalogFixture())
            {
                f.Place("Grass"); var crop = f.Place("EmberwheatCrop").GetPart<CropPart>(); crop.MoistureTicks = 3;
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [TestCase("water")]
        [TestCase("road")]
        [TestCase("field")]
        [TestCase("prepared")]
        public void HiddenOrUnexploredGroundNeverReadsNewNativeState(string kind)
        {
            using (var f = new CatalogFixture())
            {
                if (kind == "water") f.Zone.TileState.WriteCoating(X, Y, "water", 3);
                if (kind == "road") f.Place("RoadStone");
                if (kind == "field") f.Place("RipeCropRow");
                if (kind == "prepared") f.Place("Grass").AddPart(new CultivatedSoilPart());
                Assert.True(f.Resolve(f.Zone, X, Y, out _, out _));
                f.Zone.GetCell(X, Y).IsVisible = false; Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
                f.Zone.GetCell(X, Y).IsVisible = true; f.Zone.GetCell(X, Y).Explored = false;
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [Test] public void ManagedGraphReplacementRejectsTheOldGroundAndRecoversByExactReference()
        {
            using (var f = new CatalogFixture())
            {
                f.Place("RoadStone"); var manager = OverworldZoneManager.CreateDetached(f.Factory, 64, true); manager.SetActiveZone(f.Zone);
                Assert.True(f.Resolve(f.Zone, X, Y, out _, out _));
                manager.CachedZones[f.Zone.ZoneID] = World(); Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
                manager.CachedZones[f.Zone.ZoneID] = f.Zone; Assert.True(f.Resolve(f.Zone, X, Y, out _, out _));
                manager.WorldMap.Tiles[11, 10] = BiomeType.Sodden; Assert.False(f.Resolve(f.Zone, X, Y, out _, out _));
            }
        }

        [TestCase("Overworld.11.9.0")]
        [TestCase("Overworld.11.10.1")]
        [TestCase("Overworld.12.10.0")]
        public void OtherGraphsDoNotBorrowTheOpeningGroundFamily(string id)
        {
            using (var f = new CatalogFixture())
            {
                var elsewhere = World(id); elsewhere.TileState.WriteCoating(X, Y, "water", 7);
                Assert.False(f.Resolve(elsewhere, X, Y, out _, out _));
            }
        }

        [TestCase(0, 0)] [TestCase(335, 24)] [TestCase(336, 23)]
        public void MissingOrMalformedAtlasLeavesBorrowedSelectorAndTextureIntact(int width, int height)
        {
            var texture = width == 0 ? null : new Texture2D(width, height);
            using (var selector = new FlatPixelFallbackCatalog())
            {
                IDisposable adapter = null;
                try
                {
                    adapter = Construct(texture, selector); var query = Query(adapter); var zone = World(); zone.TileState.WriteCoating(X, Y, "water", 7);
                    Assert.False(query(zone, X, Y, out _, out _)); adapter.Dispose();
                    Assert.True(selector.TryResolve(zone, X, Y, out _));
                    if (texture != null) Assert.False(texture == null);
                }
                finally { adapter?.Dispose(); if (texture != null) Object.DestroyImmediate(texture); }
            }
        }

        [Test] public void CachedFramesHaveIndependentOwnersAndBorrowedAssetsAreNeverMutatedOrDisposed()
        {
            using (var f = new CatalogFixture())
            {
                f.Place("RoadStone"); Assert.True(f.Resolve(f.Zone, X, Y, out var first, out _));
                IDisposable other = null;
                try
                {
                    other = Construct(f.Atlas, f.Selector); Assert.True(Query(other)(f.Zone, X, Y, out var second, out _));
                    Assert.AreNotSame(first, second); Assert.AreSame(first.sprite.texture, second.sprite.texture);
                    var sprite = second.sprite; other.Dispose(); other.Dispose();
                    Assert.True(second == null); Assert.True(sprite == null); Assert.False(f.Atlas == null);
                    Assert.AreEqual(FilterMode.Bilinear, f.Atlas.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, f.Atlas.wrapMode);
                    Assert.True(f.Selector.TryResolve(f.Zone, X, Y, out _));
                    Assert.True(f.Resolve(f.Zone, X, Y, out var again, out _)); Assert.AreSame(first, again);
                }
                finally { other?.Dispose(); }
            }
        }

        [Test] public void WarmGroundResolutionAllocatesNothingAndDoesNotAdvanceState()
        {
            using (var f = new CatalogFixture())
            {
                f.Zone.TileState.WriteCoating(X, Y, "water", 7); var query = f.Resolve;
                for (int i = 0; i < 256; i++) query(f.Zone, X, Y, out _, out _);
                string before = f.Zone.TileState.ToSaveString(); int version = f.Zone.EntityVersion;
                long start = GC.GetAllocatedBytesForCurrentThread(); bool success = true;
                for (int i = 0; i < 1024; i++) success &= query(f.Zone, X, Y, out _, out _);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                Assert.True(success); Assert.Zero(allocated); Assert.AreEqual(before, f.Zone.TileState.ToSaveString()); Assert.AreEqual(version, f.Zone.EntityVersion);
            }
        }

        [Test] public void ExistingInjectedWorldRemainsResourceFreeAndKeepsItsFlatColorBackground()
        {
            var texture = new Texture2D(160, 24);
            try
            {
                using (var world = new FlatPixelGreyboxCatalog(texture))
                {
                    var zone = World(); zone.TileState.WriteCoating(X, Y, "water", 7);
                    Assert.True(world.TryResolvePresentation(zone, X, Y, out var result));
                    Assert.AreNotSame(texture, result.GroundTile.sprite.texture);
                    Assert.AreEqual((Color)new Color32(12, 67, 68, 255), result.GroundColor);
                    Assert.AreEqual(FlatPixelGreyboxGround.Water, result.GroundKind);
                }
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test] public void ActualResourceAndDefaultWorldShareAllTwentyOneNativeCells()
        {
            var resource = Resources.Load<Texture2D>(Resource);
            Assert.NotNull(resource, "Genuine missing-resource gate; never substitute an offline or synthetic image.");
            Assert.AreEqual(336, resource.width); Assert.AreEqual(24, resource.height);
            Assert.AreEqual(FilterMode.Point, resource.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, resource.wrapMode); Assert.AreEqual(1, resource.mipmapCount);
            using (var selector = new FlatPixelFallbackCatalog())
            using (var adapter = Construct(resource, selector))
            {
                var query = Query(adapter);
                for (int mask = 0; mask < 16; mask++)
                {
                    var zone = World(); zone.TileState.WriteCoating(X, Y, "water", 7); WetNeighbors(zone, mask);
                    Assert.True(query(zone, X, Y, out var tile, out var tint));
                    Assert.AreSame(resource, tile.sprite.texture); Assert.AreEqual(new Rect(mask * 16, 0, 16, 24), tile.sprite.rect); Assert.AreEqual(Color.white, tint);
                }
            }
            using (var selector = new FlatPixelFallbackCatalog())
            using (var adapter = Construct(resource, selector))
            {
                var factory = DensityReferenceGladeTests.Factory(); var query = Query(adapter);
                for (int frame = 16; frame < 21; frame++)
                {
                    var zone = World(); int x = frame == 17 ? X + 1 : X;
                    var ground = factory.CreateEntity(frame < 18 ? "RoadStone" : frame == 18 ? "CropRow" : "Grass");
                    if (frame >= 19) { ground.GetPart<RenderPart>().VisualID = "reference-glade-ground"; ground.AddPart(new CultivatedSoilPart()); }
                    Assert.True(zone.AddEntity(ground, x, Y));
                    if (frame == 20) { var crop = factory.CreateEntity("DrawgourdCrop"); crop.GetPart<CropPart>().MoistureTicks = 2; Assert.True(zone.AddEntity(crop, x, Y)); }
                    Assert.True(query(zone, x, Y, out var tile, out var tint)); Assert.AreSame(resource, tile.sprite.texture);
                    Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect); Assert.AreEqual(Color.white, tint);
                }
            }
            using (var world = new FlatPixelGreyboxCatalog())
            {
                var zone = World(); zone.TileState.WriteCoating(X, Y, "water", 7);
                Assert.True(world.TryResolvePresentation(zone, X, Y, out var result));
                Assert.AreSame(resource, result.GroundTile.sprite.texture); Assert.AreEqual(new Rect(15 * 16, 0, 16, 24), result.GroundTile.sprite.rect);
                Assert.AreEqual(Color.white, result.GroundColor, "Do not multiply native precolored art by the old teal swatch.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void RealFiniteHarvestAndPrepareCommandsRepaintRetainedSoilWithoutManualDirty(bool fullPack)
        {
            var atlas = new Texture2D(336, 24);
            var previousFactory = HarvestablePart.Factory;
            try
            {
                using (var f = new PaintFixture())
                {
                    var factory = DensityReferenceGladeTests.Factory(); HarvestablePart.Factory = factory;
                    f.UseGroundAtlas(atlas);
                    var row = factory.CreateEntity("RipeCropRow");
                    var actor = factory.CreateEntity("Player"); var pack = actor.GetPart<InventoryPart>();
                    pack.MaxWeight = fullPack ? 0 : 200;
                    Assert.True(f.Zone.AddEntity(row, X, Y)); Assert.True(f.Zone.AddEntity(actor, X - 1, Y));
                    f.Renderer.MarkDirty("real finite field initial placement"); f.Frame(); f.Quiet();
                    Assert.AreSame(atlas, f.GroundAt(X, Y).sprite.texture);
                    Assert.AreEqual(new Rect(18 * 16, 0, 16, 24), f.GroundAt(X, Y).sprite.rect);
                    Assert.False(InventorySystem.PerformAction(actor, row, "PrepareFieldBed", f.Zone));
                    Assert.False(row.HasPart<CultivatedSoilPart>());

                    Assert.True(InventorySystem.PerformAction(actor, row, "Harvest", f.Zone));
                    Assert.True(row.GetPart<FieldHarvestPart>().Harvested);
                    Assert.AreSame(f.Zone.GetCell(X, Y), f.Zone.GetEntityCell(row));
                    Assert.True(f.Dirty.Contains(Y * Zone.Width + X), "The real harvest commit must invalidate its retained source cell.");
                    f.Frame(); f.Quiet();
                    Assert.AreSame(atlas, f.GroundAt(X, Y).sprite.texture);
                    Assert.AreEqual(new Rect(18 * 16, 0, 16, 24), f.GroundAt(X, Y).sprite.rect);
                    int packed = 0, dropped = 0;
                    foreach (var item in pack.Objects) if (item.BlueprintName == "Emberwheat") packed += item.GetPart<StackerPart>()?.StackCount ?? 1;
                    foreach (var item in f.Zone.GetCell(X, Y).Objects) if (item.BlueprintName == "Emberwheat") dropped += item.GetPart<StackerPart>()?.StackCount ?? 1;
                    Assert.AreEqual(fullPack ? 0 : 1, packed); Assert.AreEqual(fullPack ? 1 : 0, dropped);
                    Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var afterHarvest));
                    if (fullPack) Assert.AreEqual("Emberwheat", afterHarvest.Owner.BlueprintName);
                    else Assert.AreSame(row, afterHarvest.Owner);
                    Assert.False(InventorySystem.PerformAction(actor, row, "Harvest", f.Zone));

                    Assert.True(InventorySystem.PerformAction(actor, row, "PrepareFieldBed", f.Zone));
                    Assert.True(row.HasPart<CultivatedSoilPart>()); Assert.True(row.HasTag("Plantable"));
                    Assert.True(row.GetPart<FieldHarvestPart>().Harvested);
                    Assert.True(f.Dirty.Contains(Y * Zone.Width + X), "The actual prepare commit must repaint the existing bed, including beneath dropped produce.");
                    f.Frame(); f.Quiet();
                    Assert.AreSame(atlas, f.GroundAt(X, Y).sprite.texture);
                    Assert.AreEqual(new Rect(19 * 16, 0, 16, 24), f.GroundAt(X, Y).sprite.rect);
                    Assert.AreEqual(Color.white, f.GroundTint(X, Y));
                    Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var prepared));
                    Assert.AreSame(afterHarvest.Owner, prepared.Owner);
                    Assert.False(InventorySystem.PerformAction(actor, row, "PrepareFieldBed", f.Zone));
                    Assert.False(InventorySystem.PerformAction(actor, row, "Harvest", f.Zone));
                    int resolutions = (int)EnvironmentSpriteRenderer.Perf.CellsResolved; f.Frame();
                    Assert.AreEqual(resolutions, EnvironmentSpriteRenderer.Perf.CellsResolved, "Rejected repeat commands must not manufacture a new harvest or repaint the unchanged bed.");
                }
            }
            finally { HarvestablePart.Factory = previousFactory; Object.DestroyImmediate(atlas); }
        }

        [Test] public void GenericFiniteFieldRetainsSoilColorInsteadOfBorrowingUnderlyingRoadArt()
        {
            using (var f = new CatalogFixture())
            using (var world = new FlatPixelGreyboxCatalog(null))
            {
                f.Place("RoadStone");
                Assert.True(f.Resolve(f.Zone, X, Y, out var road, out _)); f.Expect(road, 16);
                var field = f.Place("RipeCropRow"); field.BlueprintName = "DifferentFiniteField";
                Assert.NotNull(field.GetPart<FieldHarvestPart>());
                Assert.False(f.Resolve(f.Zone, X, Y, out _, out _), "A different finite field is not road or the native authored field.");
                Assert.True(world.TryResolvePresentation(f.Zone, X, Y, out var retained));
                Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, retained.GroundKind);
                Assert.AreEqual(DrySoil, retained.GroundColor);
            }
        }

        [TestCase("CropRow")] [TestCase("RipeCropRow")]
        public void ExistingFieldSoilCannotRemainAtAnUnoccupiedFootprintAnchor(string blueprint)
        {
            using (var f = new PaintFixture())
            {
                var factory = DensityReferenceGladeTests.Factory();
                var grass = factory.CreateEntity("Grass"); var row = factory.CreateEntity(blueprint);
                row.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" });
                Assert.True(f.Zone.AddEntity(grass, X, Y)); Assert.True(f.Zone.AddEntity(row, X, Y));
                f.Renderer.MarkDirty("field footprint initial placement"); f.Frame(); f.Quiet();
                Assert.True(f.Zone.GetCell(X, Y).Occupants.Contains(row));
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var initial));
                Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, initial.GroundKind); Assert.AreEqual(DrySoil, f.GroundTint(X, Y));

                Assert.True(f.Zone.TryChangeFootprint(row, "1,0"));
                Assert.AreSame(f.Zone.GetCell(X, Y), f.Zone.GetEntityCell(row));
                Assert.True(f.Zone.GetCell(X, Y).Objects.Contains(row), "The canonical save anchor still contains the row.");
                Assert.False(f.Zone.GetCell(X, Y).Occupants.Contains(row), "The current physical body no longer touches its anchor.");
                Assert.True(f.Zone.GetCell(X + 1, Y).Occupants.Contains(row));
                Assert.True(f.Dirty.Contains(Y * Zone.Width + X), "Real footprint relocation invalidates the former body/anchor.");
                f.Frame(); f.Quiet();
                Assert.True(f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var displaced));
                Assert.AreEqual(FlatPixelGreyboxGround.Ground, displaced.GroundKind);
                Assert.AreEqual(PlainGround, f.GroundTint(X, Y));
            }
        }

        static void WetNeighbors(Zone zone, int dryMask)
        {
            if ((dryMask & 1) == 0) zone.TileState.WriteCoating(X, Y - 1, "water", 7);
            if ((dryMask & 2) == 0) zone.TileState.WriteCoating(X + 1, Y, "water", 7);
            if ((dryMask & 4) == 0) zone.TileState.WriteCoating(X, Y + 1, "water", 7);
            if ((dryMask & 8) == 0) zone.TileState.WriteCoating(X - 1, Y, "water", 7);
        }
        static void FaultGround(Zone zone, Entity owner, string fault)
        {
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>(); var foreign = new Entity();
            if (fault == "foreign-render") render.ParentEntity = foreign;
            if (fault == "hidden-render") render.Visible = false;
            if (fault == "foreign-physics") physics.ParentEntity = foreign;
            if (fault == "missing-physics") owner.RemovePart(physics);
            if (fault == "takeable") physics.Takeable = true;
            if (fault == "solid") physics.Solid = true;
            if (fault == "carried") physics.InInventory = foreign;
            if (fault == "equipped") physics.Equipped = foreign;
            if (fault == "visual-id") render.VisualID = "different-road";
            if (fault == "visual-variant") render.VisualVariant = "different-road";
            if (fault == "glyph-variants") render.GlyphVariants = "=#";
            if (fault == "foreign-cell")
            {
                var old = zone.GetEntityCell(owner); var replaced = new Cell(old.X, old.Y, zone) { IsVisible = true, Explored = true };
                replaced.Objects.AddRange(old.Objects); zone.Cells[old.X, old.Y] = replaced;
            }
            if (fault == "stale-list") { var old = zone.GetEntityCell(owner); Assert.True(zone.MoveEntity(owner, X + 1, Y)); old.Objects.Add(owner); }
            if (fault == "footprint") owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
        }
        static IDisposable Construct(Texture2D atlas, FlatPixelFallbackCatalog selector)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelOpeningGroundCatalog");
            Assert.NotNull(type, "Opening ground companion is not implemented.");
            var ctor = type.GetConstructor(new[] { typeof(Texture2D), typeof(FlatPixelFallbackCatalog) }); Assert.NotNull(ctor);
            return (IDisposable)ctor.Invoke(new object[] { atlas, selector });
        }
        static GroundQuery Query(IDisposable adapter)
        {
            var method = adapter.GetType().GetMethod("TryResolve", I); Assert.NotNull(method);
            return (GroundQuery)Delegate.CreateDelegate(typeof(GroundQuery), adapter, method);
        }
        sealed class CatalogFixture : IDisposable
        {
            public readonly CavesOfOoo.Data.EntityFactory Factory;
            public readonly Zone Zone;
            public readonly FlatPixelFallbackCatalog Selector;
            public readonly Texture2D Atlas;
            public readonly IDisposable Adapter;
            public readonly GroundQuery Resolve;
            public CatalogFixture()
            {
                try
                {
                    Factory = DensityReferenceGladeTests.Factory(); Zone = World(); Selector = new FlatPixelFallbackCatalog();
                    Atlas = new Texture2D(336, 24) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
                    Adapter = Construct(Atlas, Selector); Resolve = Query(Adapter);
                }
                catch { Dispose(); throw; }
            }
            public Entity Place(string blueprint, int x = X, int y = Y)
            {
                var owner = Factory.CreateEntity(blueprint); Assert.NotNull(owner);
                if (blueprint == "Grass") owner.GetPart<RenderPart>().VisualID = "reference-glade-ground";
                Assert.True(Zone.AddEntity(owner, x, y)); return owner;
            }
            public void Expect(Tile tile, int index)
            {
                Assert.NotNull(tile); Assert.AreSame(Atlas, tile.sprite.texture); Assert.AreEqual(new Rect(index * 16, 0, 16, 24), tile.sprite.rect);
                Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
                Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
            }
            public void Dispose() { Adapter?.Dispose(); Selector?.Dispose(); if (Atlas != null) Object.DestroyImmediate(Atlas); }
        }

        static Zone World(string id = ReferenceGladePlan.ZoneID)
        {
            var zone = new Zone(id);
            for (int y = Y - 2; y <= Y + 2; y++) for (int x = X - 2; x <= X + 3; x++)
                zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true;
            return zone;
        }

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
            readonly Tilemap overlay, background;
            public readonly FlatPixelGreyboxCatalog Catalog;
            public readonly Zone Zone; public readonly ZoneRenderer Renderer;
            public readonly EnvironmentSpriteRenderer Environment;
            public HashSet<int> Dirty => (HashSet<int>)Read(Renderer, "_dirtyCells");
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");

            public PaintFixture()
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static))
                        perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null;
                    saved = new HotbarSaveFixture();
                    gridRoot = new GameObject("Ground cadence active Grid", typeof(Grid));
                    saved.Root.transform.SetParent(gridRoot.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>();
                    Assert.NotNull(main, "Borrow ZoneRenderer's existing required Tilemap; never add a duplicate.");
                    var backgroundHost = new GameObject("Ground background", typeof(Tilemap));
                    backgroundHost.transform.SetParent(saved.Root.transform, false); background = backgroundHost.GetComponent<Tilemap>();
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", background);
                    var camera = new GameObject("Owned ground camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false);
                    camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    Environment = saved.Root.AddComponent<EnvironmentSpriteRenderer>();
                    Environment.Init(saved.Root.transform, main, background); Environment.RenderingEnabled = true;
                    Catalog = new FlatPixelGreyboxCatalog(null); Write(Environment, "_flatPixelGreybox", Catalog);
                    overlay = (Tilemap)Read(Environment, "_overlayTilemap");
                    Write(Renderer, "_envSpriteRenderer", Environment); Write(Renderer, "_flatPixelModeEnabled", true);
                    Zone = World(); Renderer.SetZone(Zone);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    Frame(); Quiet();
                }
                catch { Dispose(); throw; }
            }
            public void UseGroundAtlas(Texture2D atlas)
            {
                var fallbackField = typeof(FlatPixelGreyboxCatalog).GetField("_glyphFallback", I);
                Assert.NotNull(fallbackField); ((IDisposable)fallbackField.GetValue(Catalog))?.Dispose();
                var fallback = new FlatPixelFallbackCatalog(); fallbackField.SetValue(Catalog, fallback);
                Write(Catalog, "_groundArt", Construct(atlas, fallback));
            }
            public void Frame()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            static Vector3Int At(int x, int y) => new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0);
            public Tile GroundAt(int x, int y) => background.GetTile<Tile>(At(x, y));
            public Color GroundTint(int x, int y) => background.GetColor(At(x, y));
            public Tile ForegroundAt(int x, int y) => overlay.GetTile<Tile>(At(x, y));
            public void Quiet() { Assert.False(FullDirty); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (gridRoot != null) Object.DestroyImmediate(gridRoot); }
                finally
                {
                    Catalog?.Dispose();
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
            static object Read(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
            static void Write(object owner, string name, object value)
            { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, "Expected existing integration seam: " + name); field.SetValue(owner, value); }
        }
    }
}
