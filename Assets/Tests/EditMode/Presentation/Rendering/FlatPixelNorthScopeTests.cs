using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    // Admission/projection only. This deliberately uses the existing rough
    // catalog; actual north-site art and the ordinary journey are separate gates.
    public sealed class FlatPixelNorthScopeTests
    {
        const string Connector = "Overworld.11.9.0", Plot = "Overworld.11.8.0";
        const int X = 8, Y = 9;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog catalog;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            catalog = new FlatPixelGreyboxCatalog(null);
        }
        [TearDown] public void Cleanup() { catalog?.Dispose(); content?.Dispose(); }
        Zone Attach(string id, OverworldZoneManager owner = null)
        {
            owner = owner ?? manager;
            var zone = new Zone(id); owner.SetActiveZone(zone);
            zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true;
            return zone;
        }
        Entity Add(Zone zone, string blueprint)
        {
            var entity = content.Factory.CreateEntity(blueprint);
            Assert.NotNull(entity); Assert.True(zone.AddEntity(entity, X, Y)); return entity;
        }

        [TestCase(Connector)][TestCase(Plot)]
        public void CurrentNorthGraphAdmitsBeforeAndAfterItsActualOwnersAreRemoved(string id)
        {
            var zone = Attach(id);
            var owner = Add(zone, id == Plot ? "SpreadSeedKeeper" : "AlchemyStill");
            var crop = Add(zone, "EmberwheatCrop");
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            zone.RemoveEntity(owner); zone.RemoveEntity(crop);
            int zones = manager.CachedZoneCount, routes = manager.GetConnectionSnapshot().Count;
            int disposition = manager.Exploration.DispositionFor(id);
            for (int i = 0; i < 4; i++)
            {
                Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
                Assert.True(catalog.TryResolve(zone, X, Y, out _));
            }
            Assert.Zero(zone.EntityCount); Assert.AreEqual(zones, manager.CachedZoneCount);
            Assert.AreEqual(routes, manager.GetConnectionSnapshot().Count);
            Assert.AreEqual(disposition, manager.Exploration.DispositionFor(id));
        }

        [TestCase(Connector)][TestCase(Plot)]
        public void LegacyExplorationGraphKeepsLiteralOwnersWithoutRequiringNewContent(string id)
        {
            var legacy = OverworldZoneManager.CreateDetached(content.Factory, 64, false);
            var zone = Attach(id, legacy); var owner = Add(zone, "Bush");
            Assert.False(legacy.Exploration.Enabled);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.AreSame(owner, zone.GetReadOnlyEntities().Single());
            Assert.AreEqual(1, legacy.CachedZoneCount); Assert.Zero(legacy.Exploration.RetainedGraphCount);
        }

        [TestCase(Connector)][TestCase(Plot)]
        public void AddressOrUnattachedCacheEntryAloneDoesNotGrantAdmission(string id)
        {
            var zone = new Zone(id); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.CachedZones[id] = zone; Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.AreSame(zone, manager.GetZone(id)); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }

        [TestCase(Connector)][TestCase(Plot)]
        public void ReplacedGraphAndChangedAddressCannotBorrowEarlierAdmission(string id)
        {
            var zone = Attach(id); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var replacement = new Zone(id); manager.CachedZones[id] = replacement;
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(replacement));
            manager.CachedZones[id] = zone; Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            zone.ZoneID = "Overworld.12.10.0"; Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }

        [TestCase(Connector)][TestCase(Plot)]
        public void CurrentBiomeIsRecheckedAtTheNorthAddress(string id)
        {
            var zone = Attach(id); var at = WorldMap.FromZoneID(id);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Sodden;
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Spread;
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }

        [TestCase(Connector, POIType.Village)][TestCase(Connector, POIType.Lair)][TestCase(Connector, POIType.RiverChunk)]
        [TestCase(Plot, POIType.Village)][TestCase(Plot, POIType.Lair)][TestCase(Plot, POIType.RiverChunk)]
        public void NorthAddressCannotBorrowBroadSpreadPoiAdmission(string id, POIType type)
        {
            var zone = Attach(id); var at = WorldMap.FromZoneID(id);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.SetPOI(at.x, at.y, new PointOfInterest(type, "north scope counter", null, 1));
            Assert.True(SpreadPresentationScope.IsActive(zone), "The broad scope alone is insufficient.");
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.SetPOI(at.x, at.y, null); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }

        [TestCase("null-tiles")][TestCase("small-tiles")][TestCase("null-pois")][TestCase("small-pois")]
        public void MalformedMapRefusesBothSitesWithoutRepairOrException(string fault)
        {
            var north = Attach(Connector); var plot = Attach(Plot);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(north)); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(plot));
            if (fault == "null-tiles") manager.WorldMap.Tiles = null;
            if (fault == "small-tiles") manager.WorldMap.Tiles = new BiomeType[1, 1];
            if (fault == "null-pois") manager.WorldMap.POIs = null;
            if (fault == "small-pois") manager.WorldMap.POIs = new PointOfInterest[1, 1];
            Assert.DoesNotThrow(() => Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(north)));
            Assert.DoesNotThrow(() => Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(plot)));
            Assert.AreEqual(2, manager.CachedZoneCount);
        }

        [TestCase("Overworld.13.10.0")][TestCase("Overworld.13.11.0")][TestCase("Overworld.11.11.0")]
        [TestCase("Overworld.11.7.0")][TestCase("Overworld.11.9.1")][TestCase("Overworld.11.8.1")]
        [TestCase("Overworld.011.9.0")][TestCase("Overworld.11.08.0")]
        public void OtherSitesDepthsAndNoncanonicalAliasesRetainPreviousPresentation(string id)
        { Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(Attach(id))); }

        [TestCase(64)][TestCase(65)]
        public void ActuallyGeneratedNorthGraphsUseTheSameReadOnlyAdmission(int seed)
        {
            var native = OverworldZoneManager.CreateDetached(content.Factory, seed, true);
            foreach (string id in new[] { Connector, Plot })
            {
                var zone = native.GetZone(id); Assert.NotNull(zone);
                var owners = zone.GetReadOnlyEntities().ToArray(); int count = native.CachedZoneCount;
                Assert.That(owners.Length, Is.GreaterThan(0));
                Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
                Assert.AreEqual(count, native.CachedZoneCount);
                CollectionAssert.AreEquivalent(owners, zone.GetReadOnlyEntities());
            }
        }

        [TestCase(Connector)][TestCase(Plot)]
        public void NewAdmissionDoesNotRevealUnexploredCellsOrHiddenResidents(string id)
        {
            var zone = Attach(id); Add(zone, "Grass"); var resident = Add(zone, "SpreadSeedKeeper");
            Assert.True(catalog.TryResolve(zone, X, Y, out var visible)); Assert.AreSame(resident, visible.Owner);
            zone.GetCell(X, Y).IsVisible = false;
            Assert.True(catalog.TryResolve(zone, X, Y, out var memory)); Assert.True(memory.Remembered);
            Assert.AreNotSame(resident, memory.Owner); Assert.IsNull(memory.ForegroundTile);
            zone.GetCell(X, Y).Explored = false; Assert.False(catalog.TryResolve(zone, X, Y, out _));
        }

        [Test] public void ActualRendererKeepsWorldCellsRectangularAndRestoresFullscreenTwice()
        {
            var glade = Attach(ReferenceGladePlan.ZoneID); var north = Attach(Connector);
            var plot = Attach(Plot); var outside = Attach("Overworld.13.10.0");
            using (var graph = new PresentationGraph())
            {
                for (int i = 0; i < 2; i++)
                {
                    graph.Show(glade); graph.Pixel(true); graph.Show(north); graph.Pixel(true);
                    graph.Show(plot); graph.Pixel(true);
                    graph.Presentation.Paused = true; graph.Pixel(false); Assert.True(graph.Presentation.FlatPixelModeActive);
                    graph.Presentation.Paused = false; graph.Pixel(true);
                    graph.Show(outside); graph.Pixel(true); Assert.True(graph.Presentation.FlatPixelModeActive);
                    Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(outside), "Regional native art stays scoped while projection continues.");
                }
                graph.Show(north); graph.Presentation.FlatPixelModeEnabled = false; graph.Pixel(false);
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
