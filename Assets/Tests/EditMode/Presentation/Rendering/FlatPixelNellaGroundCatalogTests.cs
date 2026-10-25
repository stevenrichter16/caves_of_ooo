using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Prepared RED fixture. It deliberately exercises both the companion
    // and the existing world route, whose admission gates are separate.
    public sealed class FlatPixelNellaGroundCatalogTests
    {
        const int X = 10, Y = 10;
        const string Plot = "Overworld.11.8.0";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Color Dry = new Color32(69, 52, 31, 255);
        static readonly Color Wet = new Color32(37, 35, 25, 255);
        static readonly Color Water = new Color32(12, 67, 68, 255);
        static readonly Color Road = new Color32(49, 65, 45, 255);

        [Test]
        public void DefaultWorldRouteUsesExistingResourceButOldInjectedConstructorStaysResourceFree()
        {
            using (var f = new Fixture())
            using (var defaults = new FlatPixelGreyboxCatalog())
            using (var noOptionalResources = new FlatPixelGreyboxCatalog(null))
            {
                f.Soil();
                var resource = Resources.Load<Texture2D>(FlatPixelOpeningGroundCatalog.ResourcePath);
                Assert.NotNull(resource, "Reuse the already accepted 21-frame ground atlas.");
                Assert.True(defaults.TryResolvePresentation(f.Zone, X, Y, out var native));
                Expect(native.GroundTile, resource, 19); Assert.AreEqual(Color.white, native.GroundColor);
                Assert.True(noOptionalResources.TryResolvePresentation(f.Zone, X, Y, out var fallback));
                Assert.AreNotSame(resource, fallback.GroundTile.sprite.texture);
                Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, fallback.GroundKind); Assert.AreEqual(Dry, fallback.GroundColor);
            }
        }

        [TestCase("CropRow")]
        [TestCase("RipeCropRow")]
        public void ActualLowerFieldUsesOldFieldFrameThenPreparationOutranksIt(string blueprint)
        {
            using (var f = new Fixture())
            {
                var grass = f.Place("Grass"); var row = f.Place(blueprint);
                var field = row.GetPart<FieldHarvestPart>();
                if (field != null) field.Harvested = true;
                f.ExpectBoth(18);
                grass.AddPart(new CultivatedSoilPart()); f.ExpectBoth(19);
                Assert.AreSame(row, f.Resolve().Owner);
                if (field != null) Assert.True(field.Harvested, "Art must not refill the finite field.");
            }
        }

        [TestCase(0)]
        [TestCase(3)]
        public void UnpreparedPublicCropRetainsGenericMoistureColorEvenOverAnOldRow(int moisture)
        {
            using (var f = new Fixture())
            {
                var grass = f.Place("Grass"); f.Place("CropRow");
                var crop = f.Place("CandyCarrotCrop").GetPart<CropPart>();
                crop.GrowthStage = 1; crop.MoistureTicks = moisture;
                Assert.False(crop.HarvestAtMaturity);
                Assert.False(f.Ground.TryResolve(f.Zone, X, Y, out _, out _));
                var generic = f.Resolve(); Assert.AreNotSame(f.Atlas, generic.GroundTile.sprite.texture);
                Assert.AreEqual(moisture > 0 ? FlatPixelGreyboxGround.WetSoil : FlatPixelGreyboxGround.DrySoil, generic.GroundKind);
                Assert.AreEqual(moisture > 0 ? Wet : Dry, generic.GroundColor);
                grass.AddPart(new CultivatedSoilPart()); f.ExpectBoth(moisture > 0 ? 20 : 19);
                Assert.AreEqual(moisture, crop.MoistureTicks); Assert.AreEqual(1, crop.GrowthStage);
            }
        }

        [TestCase("Player")]
        [TestCase("PitchpodResin")]
        public void CoveredPreparedGroundKeepsItsOwnerIndependentArtAndMarkerRemovalRecovers(string coverName)
        {
            using (var f = new Fixture())
            {
                var grass = f.Soil(); f.Place("CropRow"); var marker = grass.GetPart<CultivatedSoilPart>();
                var cover = f.Place(coverName); cover.GetPart<RenderPart>().RenderLayer = 100;
                f.ExpectBoth(19); Assert.AreSame(cover, f.Resolve().Owner);
                grass.RemovePart(marker); f.ExpectBoth(18); Assert.AreSame(cover, f.Resolve().Owner);
                grass.AddPart(new CultivatedSoilPart()); f.ExpectBoth(19);
            }
        }

        [TestCase("foreign-marker")]
        [TestCase("foreign-render")]
        [TestCase("foreign-physics")]
        [TestCase("carried")]
        [TestCase("reskin")]
        public void InvalidPreparedOwnerCannotBorrowNativeSoilAndExactRecoveryRestoresIt(string fault)
        {
            using (var f = new Fixture())
            {
                var soil = f.Soil(); var cover = f.Place("PitchpodResin"); cover.GetPart<RenderPart>().RenderLayer = 100;
                var marker = soil.GetPart<CultivatedSoilPart>(); var render = soil.GetPart<RenderPart>(); var physics = soil.GetPart<PhysicsPart>();
                f.ExpectBoth(19);
                var foreign = new Entity();
                if (fault == "foreign-marker") marker.ParentEntity = foreign;
                if (fault == "foreign-render") render.ParentEntity = foreign;
                if (fault == "foreign-physics") physics.ParentEntity = foreign;
                if (fault == "carried") physics.InInventory = foreign;
                if (fault == "reskin") render.VisualVariant = "different authored soil";
                Assert.False(f.Ground.TryResolve(f.Zone, X, Y, out _, out _));
                Assert.AreNotSame(f.Atlas, f.Resolve().GroundTile.sprite.texture);
                marker.ParentEntity = soil; render.ParentEntity = soil; physics.ParentEntity = soil;
                physics.InInventory = null; render.VisualVariant = ""; f.ExpectBoth(19);
            }
        }

        [TestCase("Overworld.11.9.0")]
        [TestCase("Overworld.12.11.0")]
        public void OtherNativeAdmittedSitesDoNotAcquireNellasSoil(string id)
        {
            using (var f = new Fixture(id))
            {
                f.Soil(); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(f.Zone));
                Assert.False(f.Ground.TryResolve(f.Zone, X, Y, out _, out _));
                Assert.AreNotSame(f.Atlas, f.Resolve().GroundTile.sprite.texture);
            }
        }

        [TestCase("hidden")]
        [TestCase("unexplored")]
        [TestCase("replaced-cell")]
        [TestCase("replaced-graph")]
        [TestCase("wrong-biome")]
        [TestCase("poi")]
        public void NativePreparedGroundRequiresCurrentDisclosedExactPlotAuthority(string fault)
        {
            using (var f = new Fixture())
            {
                f.Soil(); f.ExpectBoth(19); var cell = f.Zone.GetCell(X, Y);
                if (fault == "hidden") cell.IsVisible = false;
                if (fault == "unexplored") cell.Explored = false;
                if (fault == "replaced-cell")
                {
                    var replacement = new Cell(X, Y, f.Zone) { IsVisible = true, Explored = true };
                    replacement.Objects.AddRange(cell.Objects); f.Zone.Cells[X, Y] = replacement;
                }
                if (fault == "replaced-graph") f.Manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID);
                if (fault == "wrong-biome") f.Manager.WorldMap.Tiles[11, 8] = BiomeType.Sodden;
                if (fault == "poi") f.Manager.WorldMap.SetPOI(11, 8, new PointOfInterest(POIType.Village, "counter"));
                Assert.False(f.Ground.TryResolve(f.Zone, X, Y, out _, out _));
                if (f.Catalog.TryResolvePresentation(f.Zone, X, Y, out var fallback))
                    Assert.AreNotSame(f.Atlas, fallback.GroundTile.sprite.texture);
                f.Zone.Cells[X, Y] = cell; cell.IsVisible = cell.Explored = true;
                f.Manager.CachedZones[f.Zone.ZoneID] = f.Zone; f.Manager.WorldMap.Tiles[11, 8] = BiomeType.Spread; f.Manager.WorldMap.SetPOI(11, 8, null);
                f.ExpectBoth(19);
            }
        }

        [TestCase("foreign-part")]
        [TestCase("foreign-render")]
        [TestCase("hidden-render")]
        [TestCase("stale-anchor")]
        public void InvalidOrHiddenCropCannotSupplyWetnessToOtherwiseValidPreparedGround(string fault)
        {
            using (var f = new Fixture())
            {
                f.Soil(); var owner = f.Place("PitchpodCrop"); var crop = owner.GetPart<CropPart>(); crop.MoistureTicks = 3;
                f.ExpectBoth(20); var oldCell = f.Zone.GetCell(X, Y);
                if (fault == "foreign-part") crop.ParentEntity = new Entity();
                if (fault == "foreign-render") owner.GetPart<RenderPart>().ParentEntity = new Entity();
                if (fault == "hidden-render") owner.GetPart<RenderPart>().Visible = false;
                if (fault == "stale-anchor") { Assert.True(f.Zone.AddEntity(owner, X + 1, Y)); oldCell.Objects.Add(owner); }
                f.ExpectBoth(19);
                crop.ParentEntity = owner; owner.GetPart<RenderPart>().ParentEntity = owner; owner.GetPart<RenderPart>().Visible = true;
                if (fault == "stale-anchor") { oldCell.Objects.Remove(owner); Assert.True(f.Zone.AddEntity(owner, X, Y)); }
                f.ExpectBoth(20); Assert.AreEqual(3, crop.MoistureTicks);
            }
        }

        [Test]
        public void ExistingOpeningBedRejectsWetnessBorrowedFromAnotherCurrentCropAndRecoversAfterRepair()
        {
            // Use the already-supported glade so a missing Nella admission gate
            // cannot hide the exact current-foreign-owner defect behind it.
            using (var f = new Fixture(ReferenceGladePlan.ZoneID))
            {
                f.Soil().GetPart<RenderPart>().VisualID = "reference-glade-ground";
                var owner = f.Place("PitchpodCrop"); var crop = owner.GetPart<CropPart>(); crop.MoistureTicks = 3;
                var otherSoil = f.Place("Grass", X + 2, Y);
                otherSoil.GetPart<RenderPart>().VisualID = "reference-glade-ground";
                otherSoil.AddPart(new CultivatedSoilPart());
                var other = f.Place("PitchpodCrop", X + 2, Y); var otherCrop = other.GetPart<CropPart>();
                f.ExpectBoth(20);

                // AddPart assigns ParentEntity but does not detach a previous
                // owner's reference. Both entities stay in their real cells.
                Assert.True(other.RemovePart(otherCrop)); other.AddPart(crop);
                Assert.AreSame(crop, owner.GetPart<CropPart>()); Assert.AreSame(crop, other.GetPart<CropPart>());
                Assert.AreSame(other, crop.ParentEntity);
                Assert.AreSame(f.Zone.GetCell(X, Y), f.Zone.GetEntityCell(owner));
                Assert.AreSame(f.Zone.GetCell(X + 2, Y), f.Zone.GetEntityCell(other));
                Assert.True(f.Ground.TryResolve(f.Zone, X + 2, Y, out var actualOther, out _));
                Expect(actualOther, f.Atlas, 20);
                f.ExpectBoth(19);
                Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, f.Resolve().GroundKind);

                Assert.True(owner.RemovePart(crop)); Assert.True(other.RemovePart(crop));
                other.AddPart(otherCrop); owner.AddPart(crop);
                Assert.AreSame(owner, crop.ParentEntity); Assert.AreSame(other, otherCrop.ParentEntity);
                f.ExpectBoth(20);
                Assert.True(f.Ground.TryResolve(f.Zone, X + 2, Y, out var restoredOther, out _));
                Expect(restoredOther, f.Atlas, 19);
                Assert.AreEqual(3, crop.MoistureTicks); Assert.AreEqual(0, otherCrop.MoistureTicks);
            }
        }

        [TestCase("water")]
        [TestCase("oil")]
        [TestCase("water-and-ice")]
        public void LiveCoatingsRefuseNativeNellaSoilAndKeepExistingHazardPresentation(string coating)
        {
            using (var f = new Fixture())
            {
                f.Soil(); f.StartPainting(); f.ExpectSubmitted(19);
                f.Zone.TileState.WriteCoating(X, Y, coating == "water-and-ice" ? "water" : coating, 7);
                if (coating == "water-and-ice") f.Zone.TileState.WriteCoating(X, Y, "ice", 7);
                f.Zone.TileState.AddCharge(X, Y, 4);
                Assert.True(f.Dirty.Contains(Y * Zone.Width + X), "Actual coating/charge hooks must publish repaint.");
                Assert.False(f.Ground.TryResolve(f.Zone, X, Y, out _, out _)); f.Frame(); f.Quiet();
                var fallback = f.Resolve(); Assert.AreNotSame(f.Atlas, fallback.GroundTile.sprite.texture);
                Assert.AreSame(fallback.GroundTile, f.Background.GetTile(f.At(X, Y)));
                Assert.NotNull(f.Hazards.GetTile(f.At(X, Y)), "The hazard layer survives optional native ground refusal.");
                if (coating != "oil") { Assert.AreEqual(FlatPixelGreyboxGround.Water, fallback.GroundKind); Assert.AreEqual(Water, fallback.GroundColor); }
                else { Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, fallback.GroundKind); Assert.AreEqual(Dry, fallback.GroundColor); }
            }
        }

        [Test]
        public void NellaRoadDoesNotBorrowOpeningPathArtAndQueriesDoNotMutateState()
        {
            using (var f = new Fixture())
            {
                f.Place("RoadStone"); var state = f.Zone.TileState.ToSaveString(); int version = f.Zone.EntityVersion;
                for (int i = 0; i < 3; i++)
                {
                    Assert.False(f.Ground.TryResolve(f.Zone, X, Y, out _, out _));
                    var result = f.Resolve(); Assert.AreNotSame(f.Atlas, result.GroundTile.sprite.texture);
                    Assert.AreEqual(FlatPixelGreyboxGround.Road, result.GroundKind); Assert.AreEqual(Road, result.GroundColor);
                }
                Assert.AreEqual(state, f.Zone.TileState.ToSaveString()); Assert.AreEqual(version, f.Zone.EntityVersion);
            }
        }

        [Test]
        public void RealHarvestPlantWaterAndDryRepaintPreparedGroundWithoutManualDirty()
        {
            using (var f = new Fixture())
            {
                var soil = f.Soil(); f.Place("CropRow"); var ripe = f.Place("PitchpodCrop"); ripe.GetPart<CropPart>().GrowthStage = 2;
                var actor = f.Place("Player", X - 1, Y); actor.GetPart<InventoryPart>().MaxWeight = 1000;
                f.StartPainting(); f.ExpectSubmitted(19); f.Quiet(); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                var harvest = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ripe, "HarvestCultivatedCrop"), actor, f.Zone);
                Assert.True(harvest.Success, harvest.ErrorMessage); Assert.IsNull(f.Zone.GetEntityCell(ripe));
                Assert.True(f.Dirty.Contains(Y * Zone.Width + X), "Harvest must publish its own repaint.");
                f.Frame(); f.ExpectSubmitted(19); f.Quiet();
                var products = f.Zone.GetCell(X, Y).Objects.Where(e => e.BlueprintName == "PitchpodResin" || e.BlueprintName == "PitchpodSeed").ToArray();
                Assert.AreEqual(2, products.Where(e => e.BlueprintName == "PitchpodResin").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                Assert.AreEqual(1, products.Where(e => e.BlueprintName == "PitchpodSeed").Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                Assert.AreNotSame(soil, f.Resolve().Owner);
                foreach (var product in products) Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(product), actor, f.Zone).Success);
                var seed = actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "PitchpodSeed");
                var plant = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(seed, "PlantSeedAt|" + Plot + "|10|10"), actor, f.Zone);
                Assert.True(plant.Success, plant.ErrorMessage); Assert.True(f.Dirty.Contains(Y * Zone.Width + X));
                var planted = f.Zone.GetCell(X, Y).Objects.Single(e => e.GetPart<CropPart>() != null).GetPart<CropPart>();
                Assert.AreEqual(0, planted.GrowthStage); Assert.AreEqual(0, planted.MoistureTicks);
                f.Frame(); f.ExpectSubmitted(19); f.Quiet();
                planted.Water(1); Assert.True(f.Dirty.Contains(Y * Zone.Width + X)); f.Frame(); f.ExpectSubmitted(20); f.Quiet();
                CropSystem.OnTickEnd(f.Zone); Assert.AreEqual(0, planted.MoistureTicks);
                Assert.True(f.Dirty.Contains(Y * Zone.Width + X)); f.Frame(); f.ExpectSubmitted(19); f.Quiet();
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; f.Frame(); f.Quiet();
                Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved, "A quiet frame must not repaint these static beds.");
            }
        }

        [Test]
        public void ExistingOpeningGrassRoadAndWaterKeepTheirOriginalFrames()
        {
            using (var f = new Fixture(ReferenceGladePlan.ZoneID))
            {
                var grass = f.Soil(); grass.GetPart<RenderPart>().VisualID = "reference-glade-ground"; f.ExpectBoth(19);
                var crop = f.Place("DrawgourdCrop").GetPart<CropPart>(); crop.MoistureTicks = 1; f.ExpectBoth(20);
                f.Place("RoadStone", X + 1, Y);
                Assert.True(f.Ground.TryResolve(f.Zone, X + 1, Y, out var road, out _)); Expect(road, f.Atlas, 17);
                f.Zone.TileState.WriteCoating(X, Y, "water", 7); f.ExpectBoth(15);
            }
        }

        static void Expect(Tile tile, Texture2D atlas, int frame)
        {
            Assert.NotNull(tile); Assert.AreSame(atlas, tile.sprite.texture); Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        static object Read(object target, string name) => target.GetType().GetField(name, I).GetValue(target);
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, "Existing seam: " + name); field.SetValue(target, value); }

        sealed class Fixture : IDisposable
        {
            readonly HotbarSaveFixture globals;
            readonly TurnManager oldTurns = TurnManager.Active;
            readonly Zone oldActive = SettlementRuntime.ActiveZone;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone priorBound; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<PropertyInfo, object> hooks = typeof(EntityVisualHooks).GetProperties(S).Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
            readonly Dictionary<FieldInfo, object> perf = typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(p => p, p => p.GetValue(null));
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly long revision = MessageLog.Revision, generation = MessageLog.HistoryGeneration;
            readonly object origins = typeof(MessageLog).GetField("AnnouncementOrigins", S).GetValue(null);
            readonly Array oldOrigins;
            readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            GameObject root;
            public readonly EntityFactory Factory;
            public readonly OverworldZoneManager Manager;
            public readonly Zone Zone;
            public readonly Texture2D Atlas;
            public readonly FlatPixelFallbackCatalog Selector;
            public readonly FlatPixelOpeningGroundCatalog Ground;
            public readonly FlatPixelGreyboxCatalog Catalog;
            public ZoneRenderer Renderer;
            public Tilemap Background, Hazards;
            public HashSet<int> Dirty => (HashSet<int>)Read(Renderer, "_dirtyCells");
            public Fixture(string id = Plot)
            {
                oldOrigins = (Array)origins.GetType().GetMethod("ToArray").Invoke(origins, null);
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out var old))
                { priorBound = old; oldChanged = old.TileState.OnCellChanged; oldSight = old.TileState.OnSightChanged; }
                try
                {
                    globals = new HotbarSaveFixture(true, false); EntityVisualHooks.Reset();
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null;
                    typeof(TurnManager).GetProperty("Active", S).SetValue(null, null);
                    Factory = DensityReferenceGladeTests.Factory(); SeedPart.Factory = CropSystem.Factory = Factory;
                    Manager = OverworldZoneManager.CreateDetached(Factory, 64, true); Zone = new Zone(id);
                    Manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
                    for (int x = 0; x < CavesOfOoo.Core.Zone.Width; x++) for (int y = 0; y < CavesOfOoo.Core.Zone.Height; y++)
                        Zone.GetCell(x, y).IsVisible = Zone.GetCell(x, y).Explored = true;
                    Atlas = new Texture2D(336, 24); Selector = new FlatPixelFallbackCatalog();
                    Ground = new FlatPixelOpeningGroundCatalog(Atlas, Selector);
                    Catalog = new FlatPixelGreyboxCatalog(null); Write(Catalog, "_glyphFallback", Selector); Write(Catalog, "_groundArt", Ground);
                }
                catch { Dispose(); throw; }
            }
            public Entity Place(string blueprint, int x = X, int y = Y)
            { var owner = Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(Zone.AddEntity(owner, x, y)); return owner; }
            public Entity Soil()
            {
                var grass = Place("Grass");
                Assert.True(string.IsNullOrEmpty(grass.GetPart<RenderPart>().VisualID));
                Assert.AreEqual("...,.'`", grass.GetPart<RenderPart>().GlyphVariants);
                grass.AddPart(new CultivatedSoilPart()); return grass;
            }
            public FlatPixelGreyboxCell Resolve()
            { Assert.True(Catalog.TryResolvePresentation(Zone, X, Y, out var result)); return result; }
            public void ExpectBoth(int frame)
            {
                Assert.True(Ground.TryResolve(Zone, X, Y, out var tile, out var tint), "Direct ground companion.");
                Expect(tile, Atlas, frame); Assert.AreEqual(Color.white, tint);
                var world = Resolve(); Assert.AreSame(tile, world.GroundTile, "Actual world routing must reuse the cached ground."); Assert.AreEqual(Color.white, world.GroundColor);
            }
            public void StartPainting()
            {
                root = new GameObject("Nella ground native fixture", typeof(Grid)); root.SetActive(false);
                var main = Map("Main", 0); Background = Map("Background", -1); Hazards = Map("Hazards", 4);
                var camera = new GameObject("Owned camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true;
                Renderer = main.gameObject.AddComponent<ZoneRenderer>(); Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", Background);
                Write(Renderer, "_tileStateTilemap", Hazards); Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                var env = root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, Background); Write(env, "_flatPixelGreybox", Catalog);
                Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true);
                ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                Renderer.SetZone(Zone); Frame(); Assert.True(Renderer.FlatPixelModeActive); Quiet();
            }
            Tilemap Map(string name, int order)
            { var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(root.transform, false); go.GetComponent<TilemapRenderer>().sortingOrder = order; return go.GetComponent<Tilemap>(); }
            public Vector3Int At(int x, int y) => new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0);
            public void ExpectSubmitted(int frame) { Expect(Background.GetTile<Tile>(At(X, Y)), Atlas, frame); Assert.AreEqual(Color.white, Background.GetColor(At(X, Y))); }
            public void Frame()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Quiet() { Assert.False((bool)Read(Renderer, "_fullDirty")); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { if (root != null) Object.DestroyImmediate(root); Catalog?.Dispose(); Ground?.Dispose(); Selector?.Dispose(); if (Atlas != null) Object.DestroyImmediate(Atlas); }
                finally
                {
                    globals?.Dispose(); typeof(TurnManager).GetProperty("Active", S).SetValue(null, oldTurns); SettlementRuntime.ActiveZone = oldActive;
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull;
                    foreach (var hook in hooks) hook.Key.SetValue(null, hook.Value); bound.SetValue(null, oldBound);
                    if (priorBound != null) { priorBound.TileState.OnCellChanged = oldChanged; priorBound.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial);
                    // Restore transient reader/audio provenance and change stamps
                    // after the nested save fixture has finished its own cleanup.
                    origins.GetType().GetMethod("Clear").Invoke(origins, null);
                    foreach (object origin in oldOrigins) origins.GetType().GetMethod("Enqueue").Invoke(origins, new[] { origin });
                    typeof(MessageLog).GetProperty("Revision", S).SetValue(null, revision);
                    typeof(MessageLog).GetProperty("HistoryGeneration", S).SetValue(null, generation);
                    MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
        }
    }
}
