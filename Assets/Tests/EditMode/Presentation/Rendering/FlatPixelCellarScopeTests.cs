using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    // Scope/floor foundation only. The separate generated census and actual
    // cellar journey establish content/art acceptance, not these staged cells.
    public sealed class FlatPixelCellarScopeTests
    {
        const string Cellar = GleanersCellarBuilder.ZoneID;
        const int X = 8, Y = 9;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog catalog;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            manager.WorldMap.Tiles[11, 10] = BiomeType.Spread;
            manager.WorldMap.SetPOI(11, 10, null);
            catalog = new FlatPixelGreyboxCatalog(null);
        }
        [TearDown] public void Cleanup() { catalog?.Dispose(); content?.Dispose(); }
        Zone Attach(string id = Cellar)
        {
            var zone = new Zone(id); manager.SetActiveZone(zone);
            zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true;
            return zone;
        }
        Entity Add(Zone zone, string blueprint)
        { var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, X, Y)); return owner; }
        FlatPixelGreyboxCell Resolve(Zone zone)
        { Assert.True(catalog.TryResolve(zone, X, Y, out var cell), "Current cellar must participate in the same flat cell pipeline."); return cell; }

        [Test] public void CurrentCellarAdmitsWithoutGeneratingOrRequiringSurvivingServices()
        {
            var zone = Attach(); var stairs = Add(zone, "StairsUp"); var crate = Add(zone, "Crate");
            int zones = manager.CachedZoneCount, routes = manager.GetConnectionSnapshot().Count;
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            zone.RemoveEntity(stairs); zone.RemoveEntity(crate);
            for (int i = 0; i < 4; i++) Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.Zero(zone.EntityCount); Assert.AreEqual(zones, manager.CachedZoneCount);
            Assert.AreEqual(routes, manager.GetConnectionSnapshot().Count);
        }
        [Test] public void StandaloneOpeningKeepsItsExistingSeparateContract()
        { Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(new Zone(ReferenceGladePlan.ZoneID))); }
        [Test] public void DetachedAndUnattachedCacheCellarsCannotBorrowAuthority()
        {
            var zone = new Zone(Cellar); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.CachedZones[Cellar] = zone; Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.AreSame(zone, manager.GetZone(Cellar)); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }
        [TestCase("Overworld.13.10.0")][TestCase("Overworld.13.11.0")]
        [TestCase("Overworld.11.11.0")][TestCase("Overworld.11.10.2")]
        [TestCase("Overworld.12.10.1")][TestCase("Overworld.011.10.1")]
        public void UnselectedSitesAndOtherDepthsRetainPreviousPresentation(string id)
        { Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(Attach(id))); }
        [Test] public void CurrentGraphIdentityIsRecheckedRatherThanCachedByAddress()
        {
            var zone = Attach(); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var replacement = new Zone(Cellar); manager.CachedZones[Cellar] = replacement;
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(replacement));
            manager.CachedZones[Cellar] = zone; Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            zone.ZoneID = "Overworld.11.10.2"; Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }
        [TestCase(POIType.Village)][TestCase(POIType.Lair)][TestCase(POIType.RiverChunk)]
        public void CellarCannotBorrowChangedSurfacePoiAuthority(POIType type)
        {
            var zone = Attach(); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.SetPOI(11, 10, new PointOfInterest(type, "scope counter", null, 1));
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.SetPOI(11, 10, null); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }
        [Test] public void ChangedBiomeCannotBorrowAnEarlierCellarAdmission()
        {
            var zone = Attach(); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.Tiles[11, 10] = BiomeType.Sodden; Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.Tiles[11, 10] = BiomeType.Spread; Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }
        [TestCase("null-tiles")][TestCase("small-tiles")][TestCase("null-pois")][TestCase("small-pois")]
        public void MalformedMapRefusesWithoutRepairOrException(string fault)
        {
            var zone = Attach(); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            if (fault == "null-tiles") manager.WorldMap.Tiles = null;
            if (fault == "small-tiles") manager.WorldMap.Tiles = new BiomeType[1, 1];
            if (fault == "null-pois") manager.WorldMap.POIs = null;
            if (fault == "small-pois") manager.WorldMap.POIs = new PointOfInterest[1, 1];
            Assert.DoesNotThrow(() => Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)));
            Assert.AreEqual(1, manager.CachedZoneCount);
        }
        [Test] public void ActualFloorIsGroundOnlyAndPersistsBeneathAnotherOwner()
        {
            var zone = Attach(); var floor = Add(zone, "Floor"); var plain = Resolve(zone);
            Assert.AreSame(floor, plain.Owner); Assert.AreEqual(FlatPixelGreyboxKind.Ground, plain.Kind);
            Assert.IsNull(plain.ForegroundTile); Assert.NotNull(plain.GroundTile);
            Assert.AreEqual("StoneFloor", plain.GroundKind.ToString()); Assert.False(plain.BlocksMovement);
            var crate = Add(zone, "Crate"); var occupied = Resolve(zone);
            Assert.AreSame(crate, occupied.Owner); Assert.NotNull(occupied.ForegroundTile);
            Assert.AreEqual(plain.GroundKind, occupied.GroundKind); Assert.AreEqual(plain.GroundColor, occupied.GroundColor);
            Assert.True(occupied.BlocksMovement); zone.RemoveEntity(crate); Assert.IsNull(Resolve(zone).ForegroundTile);
            Assert.True(catalog.TryGetSubjectSprite(floor, X, Y, out var sprite, out var tint));
            Assert.AreSame(plain.GroundTile.sprite, sprite); Assert.AreEqual(plain.GroundColor, tint);
        }
        [Test] public void SurfaceGrassDoesNotAcquireUndergroundGroundTone()
        {
            var cellar = Attach(); Add(cellar, "Floor"); var floor = Resolve(cellar);
            var glade = new Zone(ReferenceGladePlan.ZoneID); Add(glade, "Grass");
            glade.GetCell(X, Y).Explored = glade.GetCell(X, Y).IsVisible = true;
            var grass = Resolve(glade); Assert.AreEqual(FlatPixelGreyboxGround.Ground, grass.GroundKind);
            Assert.AreNotEqual(floor.GroundColor, grass.GroundColor); Assert.IsNull(grass.ForegroundTile);
        }
        [TestCase("foreign-physics")][TestCase("foreign-render")][TestCase("solid")]
        [TestCase("portable")][TestCase("no-terrain")][TestCase("reskin")]
        public void MalformedOrExplicitlyReskinnedFloorDoesNotClaimStoneGround(string fault)
        {
            var zone = Attach(); var floor = Add(zone, "Floor");
            if (fault == "foreign-physics") floor.GetPart<PhysicsPart>().ParentEntity = new Entity();
            if (fault == "foreign-render") floor.GetPart<RenderPart>().ParentEntity = new Entity();
            if (fault == "solid") floor.GetPart<PhysicsPart>().Solid = true;
            if (fault == "portable") floor.GetPart<PhysicsPart>().Takeable = true;
            if (fault == "no-terrain") floor.Tags.Remove("Terrain");
            if (fault == "reskin") floor.GetPart<RenderPart>().VisualID = "different-authored-floor";
            Assert.AreNotEqual("StoneFloor", Resolve(zone).GroundKind.ToString());
        }
        [Test] public void ActualWaterAndCultivatedMoistureOverrideStoneOnlyWhileVisible()
        {
            var zone = Attach(); var floor = Add(zone, "Floor"); var dryFloor = Resolve(zone);
            floor.SetTag("Plantable"); floor.AddPart(new CultivatedSoilPart());
            var crop = Add(zone, "SootrootCrop"); var growth = crop.GetPart<CropPart>();
            growth.GrowthStage = 1; growth.MoistureTicks = 4;
            Assert.AreEqual(FlatPixelGreyboxGround.WetSoil, Resolve(zone).GroundKind);
            growth.MoistureTicks = 0; Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, Resolve(zone).GroundKind);
            zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            Assert.AreEqual(FlatPixelGreyboxGround.Water, Resolve(zone).GroundKind);
            zone.GetCell(X, Y).IsVisible = false; var remembered = Resolve(zone);
            Assert.True(remembered.Remembered); Assert.AreSame(floor, remembered.Owner);
            Assert.IsNull(remembered.ForegroundTile); Assert.AreEqual(dryFloor.GroundKind, remembered.GroundKind);
            Assert.That(remembered.GroundColor.r, Is.EqualTo(dryFloor.GroundColor.r * .35f).Within(.00001f));
            Assert.False(remembered.BlocksMovement);
            zone.GetCell(X, Y).IsVisible = true; zone.TileState.RemoveCoating(X, Y, "water"); zone.RemoveEntity(crop);
            Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, Resolve(zone).GroundKind, "The real bed remains after its crop leaves.");
            zone.GetCell(X, Y).Explored = false; Assert.False(catalog.TryResolve(zone, X, Y, out _));
        }
        [Test] public void RememberedWallsStayDistinctFromEmptyFloorWithoutDisclosingHiddenActors()
        {
            var zone = Attach(); var wall = Add(zone, "StoneWall"); var actor = Add(zone, "MarlbackScrabbler");
            Assert.AreSame(actor, Resolve(zone).Owner); zone.GetCell(X, Y).IsVisible = false;
            var memory = Resolve(zone); Assert.AreSame(wall, memory.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.Wall, memory.Kind); Assert.NotNull(memory.ForegroundTile);
            Assert.False(memory.BlocksMovement);
        }
        [Test] public void RealZoneBindingKeepsGlobalPixelsAndRestoresFullscreenAndExplicitOff()
        {
            var cellar = Attach(); Add(cellar, "Floor");
            var glade = new Zone(ReferenceGladePlan.ZoneID); var outside = Attach("Overworld.13.10.0");
            using (var graph = new PresentationGraph())
            {
                for (int i = 0; i < 2; i++)
                {
                    graph.Show(glade); graph.Pixel(true);
                    graph.Show(cellar); graph.Pixel(true); graph.AssertFloor(X, Y);
                    graph.Presentation.Paused = true; graph.Pixel(false); Assert.True(graph.Presentation.FlatPixelModeActive);
                    graph.Presentation.Paused = false; graph.Pixel(true);
                    graph.Show(outside); graph.Pixel(true); Assert.True(graph.Presentation.FlatPixelModeActive);
                    Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(outside), "Regional native art stays scoped while projection continues.");
                }
                graph.Show(cellar); graph.Presentation.FlatPixelModeEnabled = false; graph.Pixel(false);
                graph.Presentation.FlatPixelModeEnabled = true; graph.Pixel(true);
            }
        }
        sealed class PresentationGraph : CameraCleanupFixture, IDisposable
        {
            readonly HotbarSaveFixture globals;
            readonly object oldBound;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly Zone previous;
            readonly Action<int, int> oldChanged;
            readonly Action oldSight;
            public ZoneRenderer Presentation => Renderer;
            Tilemap Main => Renderer.GetComponent<Tilemap>();
            public PresentationGraph()
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out var old))
                { previous = old; oldChanged = old.TileState.OnCellChanged; oldSight = old.TileState.OnSightChanged; }
                globals = new HotbarSaveFixture(true, false);
                try { SetupCameraCleanup(); Renderer.BindFlatPixelCamera(Follow); Renderer.FlatPixelModeEnabled = true; }
                catch { Dispose(); throw; }
            }
            public void Show(Zone zone) { Renderer.SetZone(zone); Follow.CurrentZone = zone; }
            public void Pixel(bool active)
            {
                Assert.AreEqual(active ? 1.5f : 1f, Main.layoutGrid.cellSize.y);
                Assert.AreEqual(active ? 16 : 0, Follow.WorldPixelsPerUnit);
                if (active) Assert.AreSame(Main, Follow.WorldTilemap); else Assert.IsNull(Follow.WorldTilemap);
            }
            public void AssertFloor(int x, int y)
            {
                Renderer.RenderZone();
                var env = Read<EnvironmentSpriteRenderer>(Renderer, "_envSpriteRenderer");
                var overlay = Read<Tilemap>(env, "_overlayTilemap");
                var pos = new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0);
                Assert.IsNull(overlay.GetTile(pos)); Assert.IsNull(Main.GetTile(pos));
                Assert.NotNull(Renderer.BgTilemap.GetTile(pos));
                Assert.False(Renderer.SpawnRing3D.PresentationVisible);
                Assert.False(Renderer.Village3D.PresentationVisible);
            }
            public void Dispose()
            {
                try { CleanupCameraFixture(); }
                finally
                {
                    globals?.Dispose(); bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                }
            }
        }
    }
}
