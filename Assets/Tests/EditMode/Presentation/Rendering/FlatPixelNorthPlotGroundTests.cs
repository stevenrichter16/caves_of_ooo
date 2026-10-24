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
    /// <summary>Real submitted ground, decoration and foreground layers. Claim
    /// state changes below intentionally do not manually publish dirty cells.</summary>
    public sealed class FlatPixelNorthPlotGroundTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string Marker = "ConnectedSpread.ReserveBed";

        [TestCase("Player")]
        [TestCase("HealingTonic")]
        [TestCase("Chest")]
        public void VisibleLowerCropKeepsItsActualMoistureUnderAnotherForegroundOwner(string blueprint)
        {
            using (var f = new Fixture())
            {
                var crop = f.Crop("CandyCarrotCrop", 14, 10);
                var cover = f.Factory.CreateEntity(blueprint);
                cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Zone.AddEntity(cover, 14, 10));
                crop.GetPart<CropPart>().Water(3);
                var wet = f.Resolve(14, 10);
                Assert.AreSame(cover, wet.Owner);
                Assert.AreEqual(FlatPixelGreyboxGround.WetSoil, wet.GroundKind);
                crop.GetPart<CropPart>().MoistureTicks = 0;
                Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, f.Resolve(14, 10).GroundKind);
                Assert.IsNull(Decoration(wet), "A public crop does not become a reserve bed.");
            }
        }

        [TestCase("foreign-part")]
        [TestCase("foreign-render")]
        [TestCase("hidden-render")]
        [TestCase("foreign-physics")]
        [TestCase("carried")]
        [TestCase("removed")]
        [TestCase("stale-cell")]
        public void InvalidLowerCropCannotSupplyWetGround(string fault)
        {
            using (var f = new Fixture())
            {
                var crop = f.Crop("CandyCarrotCrop", 14, 10);
                crop.GetPart<CropPart>().Water(3);
                var cover = f.Factory.CreateEntity("Chest");
                cover.GetPart<RenderPart>().RenderLayer = 100;
                Assert.True(f.Zone.AddEntity(cover, 14, 10));
                Assert.AreEqual(FlatPixelGreyboxGround.WetSoil, f.Resolve(14, 10).GroundKind);
                if (fault == "foreign-part") crop.GetPart<CropPart>().ParentEntity = f.Keeper;
                if (fault == "foreign-render") crop.GetPart<RenderPart>().ParentEntity = f.Keeper;
                if (fault == "hidden-render") crop.GetPart<RenderPart>().Visible = false;
                if (fault == "foreign-physics") crop.GetPart<PhysicsPart>().ParentEntity = f.Keeper;
                if (fault == "carried") crop.GetPart<PhysicsPart>().InInventory = f.Player;
                if (fault == "removed") Assert.True(f.Zone.RemoveEntity(crop));
                if (fault == "stale-cell")
                {
                    var old = f.Zone.GetEntityCell(crop);
                    Assert.True(f.Zone.AddEntity(crop, 15, 10));
                    old.Objects.Add(crop);
                }
                Assert.AreEqual(FlatPixelGreyboxGround.Ground, f.Resolve(14, 10).GroundKind);
            }
        }

        [Test] public void OrdinaryWaterAndDryHooksUpdateSubmittedSoilBelowAnActor()
        {
            using (var f = new Fixture())
            {
                var crop = f.Crop("MarlrootCrop", 10, 10);
                Assert.True(f.Zone.AddEntity(f.Player, 10, 10));
                f.Renderer.MarkDirty("fixture initial placement"); f.Frame(); f.Quiet();
                var foreground = f.Foreground.GetTile(f.At(10, 10));
                crop.GetPart<CropPart>().Water(1);
                f.Frame(); f.ExpectGround(10, 10, FlatPixelGreyboxGround.WetSoil);
                Assert.AreSame(foreground, f.Foreground.GetTile(f.At(10, 10)));
                f.ExpectCord(10, 10); f.Quiet();
                CropSystem.OnTickEnd(f.Zone);
                f.Frame(); f.ExpectGround(10, 10, FlatPixelGreyboxGround.DrySoil);
                Assert.AreSame(foreground, f.Foreground.GetTile(f.At(10, 10)));
                f.ExpectCord(10, 10); f.Quiet();
            }
        }

        [Test] public void WaterAndAnotherHazardKeepTheirLayersWhileCordStaysIndependent()
        {
            using (var f = new Fixture())
            {
                var crop = f.Crop("MarlrootCrop", 10, 10);
                crop.GetPart<CropPart>().Water(3);
                f.Zone.TileState.WriteCoating(10, 10, "water", 7);
                f.Zone.TileState.AddCharge(10, 10, 4);
                f.Frame(); f.ExpectCord(10, 10);
                f.ExpectGround(10, 10, FlatPixelGreyboxGround.Water);
                Assert.NotNull(f.Hazards.GetTile(f.At(10, 10)));
                Assert.NotNull(f.Foreground.GetTile(f.At(10, 10)));
                int order = f.Decorations.GetComponent<TilemapRenderer>().sortingOrder;
                Assert.AreEqual(1, order);
                Assert.Greater(order, f.Background.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.Less(order, f.Foreground.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.Less(order, f.Hazards.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.AreSame(f.Main.layoutGrid, f.Decorations.layoutGrid);
            }
        }

        [Test] public void BothExactBedsKeepCordAfterRealHarvestAndAProductCoversTheSoil()
        {
            using (var f = new Fixture())
            {
                var crop = f.Crop("MarlrootCrop", 10, 10);
                crop.GetPart<CropPart>().GrowthStage = 2;
                f.Renderer.MarkDirty("fixture initial ripe crop"); f.Frame();
                f.ExpectCord(10, 10); f.ExpectCord(12, 10);
                var result = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(crop, "HarvestCultivatedCrop"), f.Player, f.Zone);
                Assert.True(result.Success);
                Assert.IsNull(f.Zone.GetEntityCell(crop));
                Assert.True(f.Zone.GetCell(10, 10).Objects.Any(e => e.BlueprintName == "MarlrootClod"));
                f.Frame(); f.ExpectCord(10, 10); f.ExpectCord(12, 10); f.Quiet();
                Assert.AreNotSame(f.SoilA, f.Resolve(10, 10).Owner, "The physical product remains the foreground.");
            }
        }

        [TestCase("unmarked")]
        [TestCase("wrong-marker")]
        [TestCase("copied-marker")]
        [TestCase("same-id-replacement")]
        public void FreshDirectResolutionDoesNotTrustCachedOrForgedSoilClaims(string fault)
        {
            using (var f = new Fixture())
            {
                Assert.NotNull(Decoration(f.Resolve(10, 10)));
                int x = 10;
                if (fault == "unmarked") f.SoilA.Properties.Remove(Marker);
                if (fault == "wrong-marker") f.SoilA.Properties[Marker] = f.Player.ID;
                if (fault == "copied-marker")
                {
                    x = 14;
                    var copied = f.Soil(14, 10); copied.Properties[Marker] = f.Keeper.ID;
                }
                if (fault == "same-id-replacement")
                {
                    Assert.True(f.Zone.RemoveEntity(f.SoilA));
                    var replaced = f.Soil(10, 10); replaced.ID = f.SoilA.ID;
                    replaced.Properties[Marker] = f.Keeper.ID;
                }
                Assert.IsNull(Decoration(f.Resolve(x, 10)), "Direct callers must not need an earlier reconciliation tick.");
            }
        }

        [TestCase("tray-move")]
        [TestCase("tray-remove")]
        [TestCase("tray-render")]
        [TestCase("tray-container")]
        [TestCase("keeper-remove")]
        [TestCase("keeper-render")]
        [TestCase("claim-owner")]
        [TestCase("world-key")]
        [TestCase("soil-part")]
        [TestCase("soil-cell")]
        [TestCase("tray-cell")]
        [TestCase("keeper-cell")]
        public void QuietRemoteInvalidationRetiresBothSubmittedCords(string fault)
        {
            using (var f = new Fixture())
            {
                f.ExpectCord(10, 10); f.ExpectCord(12, 10); f.Quiet();
                if (fault == "tray-move") Assert.True(f.Zone.AddEntity(f.Tray, 21, 12));
                if (fault == "tray-remove") Assert.True(f.Zone.RemoveEntity(f.Tray));
                if (fault == "tray-render") f.Tray.GetPart<RenderPart>().Visible = false;
                if (fault == "tray-container") f.Tray.GetPart<ContainerPart>().ParentEntity = f.Keeper;
                if (fault == "keeper-remove") Assert.True(f.Zone.RemoveEntity(f.Keeper));
                if (fault == "keeper-render") f.Keeper.GetPart<RenderPart>().Visible = false;
                if (fault == "claim-owner") f.Claim.ParentEntity = f.Player;
                if (fault == "world-key") f.Claim.WorldKey = "another-world";
                if (fault == "soil-part") Assert.True(f.SoilA.RemovePart(f.SoilA.GetPart<CultivatedSoilPart>()));
                if (fault.EndsWith("-cell", StringComparison.Ordinal))
                {
                    var owner = fault == "soil-cell" ? f.SoilA : fault == "tray-cell" ? f.Tray : f.Keeper;
                    var old = f.Zone.GetEntityCell(owner);
                    f.Zone.Cells[old.X, old.Y] = new Cell(old.X, old.Y, f.Zone) { Explored = true, IsVisible = true };
                }
                f.Quiet(); f.Frame(); f.ExpectNoCord(10, 10); f.ExpectNoCord(12, 10); f.Quiet();
                long passes = EnvironmentSpriteRenderer.Perf.IncrementalPasses;
                f.Frame(); Assert.AreEqual(passes, EnvironmentSpriteRenderer.Perf.IncrementalPasses);
            }
        }

        [Test] public void KeeperMayWalkWithinTheZoneWithoutErasingThePhysicalReserve()
        {
            using (var f = new Fixture())
            {
                f.ExpectCord(10, 10); var tile = f.Decorations.GetTile(f.At(10, 10));
                Assert.True(f.Zone.AddEntity(f.Keeper, 30, 10)); f.Quiet();
                Assert.True(f.Bound()); f.Frame();
                Assert.AreSame(tile, f.Decorations.GetTile(f.At(10, 10))); f.ExpectCord(12, 10);
            }
        }

        [Test] public void ClaimReplacementWithoutMembershipChangeInvalidatesAndRecovers()
        {
            using (var f = new Fixture())
            {
                var members = f.Zone.GetReadOnlyEntitiesWithPart<LocalGatheringClaimPart>();
                int version = PartVersion(f.Zone);
                var replacement = new LocalGatheringClaimPart(); f.Keeper.AddPart(replacement);
                Assert.True(f.Keeper.RemovePart(f.Claim));
                Assert.AreEqual(version, PartVersion(f.Zone)); Assert.AreSame(members, f.Zone.GetReadOnlyEntitiesWithPart<LocalGatheringClaimPart>());
                f.Quiet(); f.Frame(); f.ExpectNoCord(10, 10); f.ExpectNoCord(12, 10);
                Assert.True(replacement.Configure(f.Zone, f.SoilA, f.SoilB, f.Tray, f.Manager.WorldSeed));
                f.Quiet(); f.Frame(); f.ExpectCord(10, 10); f.ExpectCord(12, 10); f.Quiet();
            }
        }

        [Test] public void GrantAndSuspensionNeverRecolorCordOrSpendAnythingWhileRendering()
        {
            using (var f = new Fixture())
            {
                var tile = f.Decorations.GetTile(f.At(10, 10));
                int playerMoney = TradeSystem.GetDrams(f.Player), keeperMoney = TradeSystem.GetDrams(f.Keeper);
                int tick = WorldClock.CurrentTick, quantity = f.Tray.GetPart<ContainerPart>().Contents.Count;
                var record = new ReserveAccessRecord { Player = f.Player }; f.Claim.Permissions.Add(record);
                foreach (ReserveAccessState state in Enum.GetValues(typeof(ReserveAccessState)))
                {
                    record.State = state; f.Quiet(); f.Frame(); f.ExpectCord(10, 10);
                    Assert.AreSame(tile, f.Decorations.GetTile(f.At(10, 10)));
                    Assert.AreEqual(Color.white, f.Decorations.GetColor(f.At(10, 10)));
                    Assert.AreEqual(state, record.State); Assert.Zero(record.BreachOrdinal);
                    Assert.AreEqual(playerMoney, TradeSystem.GetDrams(f.Player)); Assert.AreEqual(keeperMoney, TradeSystem.GetDrams(f.Keeper));
                    Assert.AreEqual(tick, WorldClock.CurrentTick); Assert.AreEqual(quantity, f.Tray.GetPart<ContainerPart>().Contents.Count);
                }
            }
        }

        [Test] public void CordMeansBoundOwnershipRatherThanCurrentPlantingReadiness()
        {
            using (var f = new Fixture())
            {
                f.SoilA.Tags.Remove("Plantable"); f.SoilA.SetTag("Barren");
                Assert.False(CultivatedSoilPart.IsCultivated(f.Zone, f.Zone.GetCell(10, 10)));
                Assert.True(f.Bound()); f.Frame(); f.ExpectCord(10, 10);
                Assert.NotNull(Decoration(f.Resolve(10, 10)));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void HiddenBedLosesCordAndNeverExposesItsCurrentCropMoisture(bool explored)
        {
            using (var f = new Fixture())
            {
                var crop = f.Crop("MarlrootCrop", 10, 10); crop.GetPart<CropPart>().Water(4); f.Frame(); f.ExpectCord(10, 10);
                var cell = f.Zone.GetCell(10, 10); cell.IsVisible = false; cell.Explored = explored;
                f.Frame(); f.ExpectNoCord(10, 10);
                Assert.True(f.Catalog.TryResolve(f.Zone, 10, 10, out var hidden) == explored);
                if (explored) { Assert.IsNull(Decoration(hidden)); Assert.AreNotEqual(FlatPixelGreyboxGround.WetSoil, hidden.GroundKind); }
                crop.GetPart<CropPart>().MoistureTicks = 0;
                long passes = EnvironmentSpriteRenderer.Perf.IncrementalPasses; f.Frame();
                Assert.AreEqual(passes, EnvironmentSpriteRenderer.Perf.IncrementalPasses);
                cell.IsVisible = cell.Explored = true; f.Renderer.MarkCellDirty(10, 10, "normal FOV reveal");
                f.Frame(); f.ExpectCord(10, 10); f.ExpectGround(10, 10, FlatPixelGreyboxGround.DrySoil);
            }
        }

        [TestCase("paused")] [TestCase("mode")] [TestCase("sprites")]
        public void CanvasAndModeChangesClearDecorationAndRestoreOnlyCurrentClaims(string reason)
        {
            using (var f = new Fixture())
            {
                f.ExpectCord(10, 10);
                if (reason == "paused") f.Renderer.Paused = true;
                if (reason == "mode") f.Renderer.FlatPixelModeEnabled = false;
                if (reason == "sprites") f.Environment.RenderingEnabled = false;
                f.Frame(); f.ExpectNoCord(10, 10); f.ExpectNoCord(12, 10);
                Assert.True(f.Zone.AddEntity(f.Tray, 21, 12));
                if (reason == "paused") f.Renderer.Paused = false;
                if (reason == "mode") f.Renderer.FlatPixelModeEnabled = true;
                if (reason == "sprites") f.Environment.RenderingEnabled = true;
                f.Frame(); f.ExpectNoCord(10, 10);
                Assert.True(f.Zone.AddEntity(f.Tray, 20, 12)); f.Quiet(); f.Frame();
                f.ExpectCord(10, 10); f.ExpectCord(12, 10); f.Quiet();
            }
        }

        [Test] public void CurrentGraphReplacementCannotKeepOldCordsAndRestorationReobserves()
        {
            using (var f = new Fixture())
            {
                f.Manager.CachedZones[f.Zone.ZoneID] = new Zone(f.Zone.ZoneID);
                f.Frame(); Assert.True(f.Renderer.FlatPixelModeActive); f.ExpectNoCord(10, 10);
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(f.Zone));
                f.Manager.CachedZones[f.Zone.ZoneID] = f.Zone;
                f.Frame(); Assert.True(f.Renderer.FlatPixelModeActive); f.ExpectCord(10, 10);
            }
        }

        [Test] public void SavedClaimReferencesRebindToLoadedSoilsWithoutReusingOldGraph()
        {
            using (var f = new Fixture())
            {
                f.Claim.Permissions.Add(new ReserveAccessRecord { Player = f.Player, State = ReserveAccessState.Granted });
                var loadedKeeper = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.Keeper);
                var claim = loadedKeeper.GetPart<LocalGatheringClaimPart>();
                Assert.AreNotSame(f.SoilA, claim.FirstSoil);
                var loadedZone = new Zone(f.Zone.ZoneID);
                Assert.True(loadedZone.AddEntity(loadedKeeper, 20, 10));
                Assert.True(loadedZone.AddEntity(claim.FirstSoil, claim.FirstX, claim.FirstY));
                Assert.True(loadedZone.AddEntity(claim.SecondSoil, claim.SecondX, claim.SecondY));
                Assert.True(loadedZone.AddEntity(claim.Tray, claim.TrayX, claim.TrayY));
                Assert.True(loadedZone.AddEntity(claim.Permissions[0].Player, 9, 10));
                f.UseZone(loadedZone); f.Frame(); f.ExpectCord(10, 10); f.ExpectCord(12, 10);
                f.SoilA.Properties[Marker] = "old graph mutation";
                f.Quiet(); f.Frame(); f.ExpectCord(10, 10);
                Assert.AreEqual(ReserveAccessState.Granted, claim.GetState(claim.Permissions[0].Player));
            }
        }

        [Test] public void BoundValidationIsAllocationFreeAfterWarmupWithoutChangingPermissionSemantics()
        {
            using (var f = new Fixture(false))
            {
                for (int i = 0; i < 16; i++) f.Claim.Permissions.Add(new ReserveAccessRecord { Player = new Entity(), State = ReserveAccessState.Granted });
                var validate = BoundDelegate(f.Claim);
                for (int i = 0; i < 10; i++) Assert.True(validate(f.Zone));
                bool valid = true; long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) valid &= validate(f.Zone);
                long after = GC.GetAllocatedBytesForCurrentThread();
                Assert.True(valid); Assert.AreEqual(before, after, "A render reconciliation cannot allocate LINQ permission sets each frame.");
                Assert.AreEqual(16, f.Claim.Permissions.Count);
            }
        }

        [TestCase("null-list")] [TestCase("null-record")] [TestCase("null-player")]
        [TestCase("duplicate-reference")] [TestCase("negative-breach")] [TestCase("invalid-state")] [TestCase("too-many")]
        public void BoundStillRejectsMalformedPermissionGraphs(string fault)
        {
            using (var f = new Fixture(false))
            {
                var a = new ReserveAccessRecord { Player = f.Player, State = ReserveAccessState.Granted };
                f.Claim.Permissions.Add(a); Assert.True(f.Bound());
                if (fault == "null-list") f.Claim.Permissions = null;
                if (fault == "null-record") f.Claim.Permissions.Add(null);
                if (fault == "null-player") a.Player = null;
                if (fault == "duplicate-reference") f.Claim.Permissions.Add(new ReserveAccessRecord { Player = f.Player });
                if (fault == "negative-breach") a.BreachOrdinal = -1;
                if (fault == "invalid-state") a.State = (ReserveAccessState)99;
                if (fault == "too-many") for (int i = 0; i < 64; i++) f.Claim.Permissions.Add(new ReserveAccessRecord { Player = new Entity() });
                Assert.False(f.Bound());
            }
        }

        [Test] public void EqualPlayerIdsAreDistinctPermissionReferencesAndSixtyFourIsValid()
        {
            using (var f = new Fixture(false))
            {
                for (int i = 0; i < 64; i++) f.Claim.Permissions.Add(new ReserveAccessRecord { Player = new Entity { ID = "same-id" }, State = (ReserveAccessState)(i % 3) });
                Assert.True(f.Bound());
                f.Claim.Permissions[63].Player = f.Claim.Permissions[0].Player;
                Assert.False(f.Bound());
            }
        }

        [Test] public void WarmIndexedReconciliationAllocatesNothingAndLeavesAnUnchangedCanvasQuiet()
        {
            using (var f = new Fixture())
            {
                var index = f.Zone.GetReadOnlyEntitiesWithPart<LocalGatheringClaimPart>();
                int membership = PartVersion(f.Zone);
                for (int i = 0; i < 10; i++) f.Environment.ReconcileFlatOwnerStates(f.Zone, f.Renderer);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) f.Environment.ReconcileFlatOwnerStates(f.Zone, f.Renderer);
                long after = GC.GetAllocatedBytesForCurrentThread();
                Assert.AreEqual(before, after); f.Quiet();
                Assert.AreSame(index, f.Zone.GetReadOnlyEntitiesWithPart<LocalGatheringClaimPart>());
                Assert.AreEqual(membership, PartVersion(f.Zone));
            }
        }

        static Tile Decoration(FlatPixelGreyboxCell cell)
        {
            var field = typeof(FlatPixelGreyboxCell).GetField("GroundDecorationTile");
            Assert.NotNull(field, "The physical reserve cord needs a layer independent of foreground owners.");
            return (Tile)field.GetValue(cell);
        }
        static Func<Zone, bool> BoundDelegate(LocalGatheringClaimPart claim)
            => (Func<Zone, bool>)Delegate.CreateDelegate(typeof(Func<Zone, bool>), claim, typeof(LocalGatheringClaimPart).GetMethod("Bound", Instance));
        static int PartVersion(Zone zone) => (int)typeof(Zone).GetMethod("GetPartMembershipVersion", Instance)
            .MakeGenericMethod(typeof(LocalGatheringClaimPart)).Invoke(zone, null);

        sealed class Fixture : IDisposable
        {
            readonly HotbarSaveFixture globals;
            readonly TurnManager oldTurns = TurnManager.Active;
            readonly Zone oldActive = SettlementRuntime.ActiveZone;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo boundZone = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", Static);
            readonly object oldBound;
            readonly Zone priorBound;
            readonly Action<int, int> oldChanged;
            readonly Action oldSight;
            readonly Dictionary<PropertyInfo, object> hooks = typeof(EntityVisualHooks).GetProperties(Static)
                .Where(p => p.CanRead && p.CanWrite).ToDictionary(p => p, p => p.GetValue(null));
            readonly GameObject root;
            readonly Texture2D plot, baseArt;
            public readonly EntityFactory Factory;
            public readonly OverworldZoneManager Manager;
            public Zone Zone;
            public readonly Entity Player, Keeper, SoilA, SoilB, Tray;
            public readonly LocalGatheringClaimPart Claim;
            public readonly ZoneRenderer Renderer;
            public readonly EnvironmentSpriteRenderer Environment;
            public readonly FlatPixelGreyboxCatalog Catalog;
            public readonly Tilemap Main, Background, Foreground, Hazards;
            public Tilemap Decorations => (Tilemap)typeof(EnvironmentSpriteRenderer).GetField("_flatGroundDecorationTilemap", Instance)?.GetValue(Environment);

            public Fixture(bool renderer = true)
            {
                oldBound = boundZone.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out var old))
                { priorBound = old; oldChanged = old.TileState.OnCellChanged; oldSight = old.TileState.OnSightChanged; }
                try
                {
                    globals = new HotbarSaveFixture(true, false); EntityVisualHooks.Reset();
                    typeof(TurnManager).GetProperty("Active", Static).SetValue(null, null);
                    Factory = DensityReferenceGladeTests.Factory(); CropSystem.Factory = Factory;
                    Manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
                    Zone = new Zone(LocalGatheringClaimPart.ReserveZoneID); Manager.SetActiveZone(Zone); SettlementRuntime.ActiveZone = Zone;
                    Player = Factory.CreateEntity("Player"); Keeper = Factory.CreateEntity("SpreadSeedKeeper"); Tray = Factory.CreateEntity("ConnectedReserveTray");
                    Assert.True(Zone.AddEntity(Player, 9, 10)); Assert.True(Zone.AddEntity(Keeper, 20, 10)); Assert.True(Zone.AddEntity(Tray, 20, 12));
                    SoilA = Soil(10, 10); SoilB = Soil(12, 10);
                    Claim = new LocalGatheringClaimPart(); Keeper.AddPart(Claim);
                    Assert.True(Claim.Configure(Zone, SoilA, SoilB, Tray, Manager.WorldSeed));
                    SoilA.Properties[Marker] = SoilB.Properties[Marker] = Keeper.ID;
                    Reveal(Zone); Assert.True(Bound());
                    if (!renderer) return;
                    root = new GameObject("North plot ground fixture", typeof(Grid)); root.SetActive(false);
                    Main = Map("Main", 0); Background = Map("Background", -1); Hazards = Map("Hazards", 4);
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true;
                    Renderer = Main.gameObject.AddComponent<ZoneRenderer>(); Set("_tilemap", Main); Set("_bgTilemap", Background); Set("_tileStateTilemap", Hazards); Set("_mainCamera", camera.GetComponent<Camera>());
                    Environment = root.AddComponent<EnvironmentSpriteRenderer>(); Environment.Init(root.transform, Main, Background);
                    plot = new Texture2D(128, 24); baseArt = new Texture2D(160, 24);
                    var ctor = typeof(FlatPixelGreyboxCatalog).GetConstructor(Enumerable.Repeat(typeof(Texture2D), 10).ToArray());
                    Assert.NotNull(ctor, "Independent plot and keeper injection must forward the reserve cord atlas.");
                    Catalog = (FlatPixelGreyboxCatalog)ctor.Invoke(new object[] { baseArt, null, null, null, null, null, null, null, plot, null });
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", Instance).SetValue(Environment, Catalog);
                    Foreground = (Tilemap)typeof(EnvironmentSpriteRenderer).GetField("_overlayTilemap", Instance).GetValue(Environment);
                    Set("_envSpriteRenderer", Environment); Set("_flatPixelModeEnabled", true);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    Renderer.SetZone(Zone); Frame(); Assert.True(Renderer.FlatPixelModeActive); Quiet();
                    Assert.NotNull(Decorations, "A separate transparent ground-decoration Tilemap must be installed.");
                }
                catch { Dispose(); throw; }
            }

            Tilemap Map(string name, int order)
            {
                var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); go.transform.SetParent(root.transform, false);
                go.GetComponent<TilemapRenderer>().sortingOrder = order; return go.GetComponent<Tilemap>();
            }
            public Entity Soil(int x, int y)
            {
                var soil = Factory.CreateEntity("Grass"); soil.AddPart(new CultivatedSoilPart());
                Assert.True(Zone.AddEntity(soil, x, y)); return soil;
            }
            public Entity Crop(string name, int x, int y)
            {
                var crop = Factory.CreateEntity(name); Assert.True(Zone.AddEntity(crop, x, y)); return crop;
            }
            static void Reveal(Zone zone)
            { for (int x = 0; x < CavesOfOoo.Core.Zone.Width; x++) for (int y = 0; y < CavesOfOoo.Core.Zone.Height; y++) zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true; }
            public void UseZone(Zone zone)
            { Zone = zone; Manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone; Reveal(zone); Renderer.SetZone(zone); }
            public bool Bound() => BoundDelegate(Claim)(Zone);
            public Vector3Int At(int x, int y) => new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0);
            public FlatPixelGreyboxCell Resolve(int x, int y)
            { Assert.True(Catalog.TryResolve(Zone, x, y, out var result)); return result; }
            public void ExpectCord(int x, int y)
            {
                Assert.NotNull(Decorations); var tile = Decorations.GetTile<Tile>(At(x, y)); Assert.NotNull(tile, "Expected actual submitted cord at " + x + "," + y);
                Assert.AreSame(plot, tile.sprite.texture); Assert.AreEqual(new Rect(112, 0, 16, 24), tile.sprite.rect);
            }
            public void ExpectNoCord(int x, int y) { Assert.NotNull(Decorations); Assert.IsNull(Decorations.GetTile(At(x, y))); }
            public void ExpectGround(int x, int y, FlatPixelGreyboxGround kind)
            { var state = Resolve(x, y); Assert.AreEqual(kind, state.GroundKind); Assert.AreSame(state.GroundTile, Background.GetTile(At(x, y))); Assert.AreEqual(state.GroundColor, Background.GetColor(At(x, y))); }
            public void Frame()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", Instance).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Quiet()
            { Assert.False((bool)typeof(ZoneRenderer).GetField("_fullDirty", Instance).GetValue(Renderer)); Assert.Zero(((HashSet<int>)typeof(ZoneRenderer).GetField("_dirtyCells", Instance).GetValue(Renderer)).Count); }
            void Set(string name, object value) => typeof(ZoneRenderer).GetField(name, Instance).SetValue(Renderer, value);
            public void Dispose()
            {
                try
                {
                    if (root != null) Object.DestroyImmediate(root);
                    Catalog?.Dispose(); // inactive fixture components may never receive OnDestroy
                    if (plot != null) Object.DestroyImmediate(plot); if (baseArt != null) Object.DestroyImmediate(baseArt);
                }
                finally
                {
                    globals?.Dispose(); typeof(TurnManager).GetProperty("Active", Static).SetValue(null, oldTurns);
                    SettlementRuntime.ActiveZone = oldActive; ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull;
                    foreach (var hook in hooks) hook.Key.SetValue(null, hook.Value);
                    boundZone.SetValue(null, oldBound);
                    if (priorBound != null) { priorBound.TileState.OnCellChanged = oldChanged; priorBound.TileState.OnSightChanged = oldSight; }
                }
            }
        }
    }
}
