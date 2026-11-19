using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The pure placement check (Docs/HOME-BASE-DESIGN.md section 7.2, PB1): one reason code per refusal, saved data only, no mutation.
    /// Every refusal has its counter-case: the same cell with the offending property removed passes.
    /// </summary>
    public sealed class PlacementCheckTests
    {
        HaulingContentScope scope; OverworldZoneManager manager; string wildId;
        const int X = 20, Y = 12;
        static readonly PlacementSpec Box = new PlacementSpec { Solid = true, GroundClass = PlacementCheck.OpenGround };
        static readonly PlacementSpec Bench = new PlacementSpec { Solid = false, GroundClass = PlacementCheck.OpenGround };

        [SetUp] public void Setup()
        {
            scope = new HaulingContentScope(); scope.Seed(64); manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
        }
        [TearDown] public void Cleanup() { scope.Dispose(); }

        Entity New(string blueprint) { var e = scope.Factory.CreateEntity(blueprint); Assert.NotNull(e, blueprint); return e; }
        /// <summary>A bare synthetic zone of grass; the check reads only the zone's saved cells and the manager for class and world cap.</summary>
        Zone Field(string id = null, string ground = "Grass")
        {
            var zone = new Zone(id ?? wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(zone.AddEntity(New(ground), x, y));
            return zone;
        }
        PlacementResult Check(Zone zone, int x = X, int y = Y, PlacementSpec? spec = null, Entity fixture = null) => PlacementCheck.Evaluate(manager, zone, x, y, spec ?? Box, fixture);
        static void Refuses(PlacementResult r, string code) { CollectionAssert.Contains(r.Reasons, code); Assert.False(r.Ok); }
        static void DoesNotRefuse(PlacementResult r, string code) => CollectionAssert.DoesNotContain(r.Reasons, code);
        Entity Marked(string origin) { var e = New("Chest"); e.AddPart(new InstallationMarkerPart { Origin = origin, RetainZone = true }); return e; }

        [Test] public void BareGrassInAWildZoneIsOkAndSaysTheProtectionCheckIsDormant()
        {
            var r = Check(Field()); Assert.True(r.Ok, string.Join(",", r.Reasons)); Assert.False(r.ProtectedNearChecked, "no protection capability exists, so the check is neither run nor claimed");
            Assert.Null(r.First); Assert.AreEqual(0, r.Others);
        }

        // ── zone class ──
        [Test] public void AVillageZoneIsRefusedAsPoiZone_AndAWildZoneIsNot()
        {
            var r = Check(Field("Overworld.13.7.0")); Refuses(r, PlacementCheck.PoiZone); Assert.AreEqual(PlacementCheck.PoiZone, r.First);
            DoesNotRefuse(Check(Field()), PlacementCheck.PoiZone);
        }
        [Test] public void AnOpeningZoneIsProtectedGround() => Refuses(Check(Field("Overworld.11.9.0")), PlacementCheck.ProtectedGround);
        [Test] public void AnUndergroundZoneIsColumnPending() => Refuses(Check(Field("Overworld.13.7.1")), PlacementCheck.ColumnPending);
        [Test] public void ANonOverworldZoneIsUnsupportedAndANullManagerIsToo()
        {
            Refuses(Check(Field("fifty-world-tests")), PlacementCheck.UnsupportedZone);
            Refuses(PlacementCheck.Evaluate(null, Field(), X, Y, Box), PlacementCheck.UnsupportedZone);
        }

        // ── ground ──
        [Test] public void RoadStoneIsThePublicRouteAndRefused()
        {
            var z = Field(ground: "RoadStone"); Refuses(Check(z), PlacementCheck.Ground); DoesNotRefuse(Check(Field()), PlacementCheck.Ground);
        }
        [TestCase("Grass")] [TestCase("Floor")] [TestCase("StoneFloor")]
        public void TheRigsVerifiedGroundSetPasses(string ground) => DoesNotRefuse(Check(Field(ground: ground)), PlacementCheck.Ground);
        [Test] public void ASolidTerrainObjectIsNotGround_AndAWallOnTheCellIsAnOccupantNotGround()
        {
            var z = Field(); var solid = New("Grass"); solid.SetTag("Solid"); Assert.True(z.AddEntity(solid, X, Y)); Refuses(Check(z), PlacementCheck.Ground);
            var walled = Field(); Assert.True(walled.AddEntity(New("SandstoneWall"), X, Y)); var r = Check(walled); Refuses(r, PlacementCheck.CellOccupied); DoesNotRefuse(r, PlacementCheck.Ground);
        }
        [Test] public void APoolOrAStepTriggerOrAFlameIsNotGround()
        {
            foreach (var part in new Func<Part>[] { () => new LiquidPoolPart(), () => new GasPoolPart(), () => new RuneFlameTriggerPart() })
            {
                var z = Field(); var hazard = New("Chest"); hazard.AddPart(part()); Assert.True(z.AddEntity(hazard, X, Y));
                Refuses(Check(z), PlacementCheck.Ground);
            }
            DoesNotRefuse(Check(Field()), PlacementCheck.Ground);
        }
        [Test] public void HeatColdChargeCloudOrACoatingOnTheTileIsNotGround()
        {
            var z = Field(); z.TileState.AddHeat(X, Y, 5); Refuses(Check(z), PlacementCheck.Ground);
            var z2 = Field(); z2.TileState.AddCold(X, Y, 5); Refuses(Check(z2), PlacementCheck.Ground);
            var z3 = Field(); z3.TileState.AddCharge(X, Y, 5); Refuses(Check(z3), PlacementCheck.Ground);
            var z4 = Field(); z4.TileState.WriteCloud(X, Y, "smoke", 5); Refuses(Check(z4), PlacementCheck.Ground);
            var z5 = Field(); z5.TileState.WriteCoating(X, Y, "water", 3); Refuses(Check(z5), PlacementCheck.Ground);
            DoesNotRefuse(Check(Field()), PlacementCheck.Ground);
        }
        [Test] public void ACellWithNoTerrainAtAllIsNotGround()
        {
            var z = new Zone(wildId); Refuses(Check(z), PlacementCheck.Ground);
        }
        [Test] public void OutOfBoundsAndNullZoneAreGroundRefusalsNotExceptions()
        {
            var z = Field(); Refuses(Check(z, -1, 5), PlacementCheck.Ground); Refuses(Check(z, 5, Zone.Height), PlacementCheck.Ground); Refuses(PlacementCheck.Evaluate(manager, null, 1, 1, Box), PlacementCheck.Ground);
        }

        // ── occupancy, neighbours, edges, stairs, caps, state, class ──
        [Test] public void AnItemOrACreatureOnTheCellIsAnOccupant_AndTheFixtureItselfIsNot()
        {
            var z = Field(); var item = New("Dagger"); Assert.True(z.AddEntity(item, X, Y)); Refuses(Check(z), PlacementCheck.CellOccupied);
            DoesNotRefuse(Check(z, fixture: item), PlacementCheck.CellOccupied);
            DoesNotRefuse(Check(Field()), PlacementCheck.CellOccupied);
        }
        [Test] public void ASolidFixtureNeedsAllEightNeighboursOpenAndABenchOnlyTheFourOrthogonalOnes()
        {
            var diag = Field(); Assert.True(diag.AddEntity(New("SandstoneWall"), X + 1, Y + 1));
            Refuses(Check(diag), PlacementCheck.OpenNeighbours); DoesNotRefuse(Check(diag, spec: Bench), PlacementCheck.OpenNeighbours);
            var orth = Field(); Assert.True(orth.AddEntity(New("SandstoneWall"), X, Y + 1));
            Refuses(Check(orth), PlacementCheck.OpenNeighbours); Refuses(Check(orth, spec: Bench), PlacementCheck.OpenNeighbours);
            DoesNotRefuse(Check(Field()), PlacementCheck.OpenNeighbours);
        }
        [Test] public void AHazardousNeighbourPlugsTheOpening()
        {
            var z = Field(); var pool = New("Chest"); pool.AddPart(new LiquidPoolPart()); Assert.True(z.AddEntity(pool, X + 1, Y)); Refuses(Check(z), PlacementCheck.OpenNeighbours);
        }
        [TestCase(2, 12, true)] [TestCase(3, 12, false)] [TestCase(Zone.Width - 4, 12, false)] [TestCase(Zone.Width - 3, 12, true)]
        [TestCase(20, 2, true)] [TestCase(20, 3, false)] [TestCase(20, Zone.Height - 4, false)] [TestCase(20, Zone.Height - 3, true)]
        public void ThreeCellsFromEveryEdgeAreReserved(int x, int y, bool refused)
        {
            var r = Check(Field(), x, y);
            if (refused) CollectionAssert.Contains(r.Reasons, PlacementCheck.EdgeMargin); else CollectionAssert.DoesNotContain(r.Reasons, PlacementCheck.EdgeMargin);
        }
        [TestCase("VillageDoor", 2, true)] [TestCase("VillageDoor", 3, false)] [TestCase("StairsDown", 1, true)] [TestCase("StairsUp", 2, true)] [TestCase("Well", 2, true)] [TestCase("Well", 3, false)]
        public void DoorsStairsAndWellsKeepTwoCellsClear(string blueprint, int distance, bool refused)
        {
            var z = Field(); var e = scope.Factory.CreateEntity(blueprint); Assert.NotNull(e, blueprint); Assert.True(z.AddEntity(e, X + distance, Y));
            var r = Check(z); if (refused) CollectionAssert.Contains(r.Reasons, PlacementCheck.StairsDoors); else CollectionAssert.DoesNotContain(r.Reasons, PlacementCheck.StairsDoors);
        }
        [Test] public void TheZoneCapIsEightHeldFixturesAndOnlyPlayerOrRelocatedOriginsCount()
        {
            var z = Field(); for (int i = 0; i < 7; i++) Assert.True(z.AddEntity(Marked("Player"), 5 + i, 3));
            DoesNotRefuse(Check(z), PlacementCheck.CapZone); Assert.AreEqual(7, PlacementCheck.CountInZone(z));
            Assert.True(z.AddEntity(Marked("Relocated"), 5, 6)); Refuses(Check(z), PlacementCheck.CapZone); Assert.AreEqual(8, PlacementCheck.CountInZone(z));
            var generated = Field(); for (int i = 0; i < 12; i++) Assert.True(generated.AddEntity(Marked("Generated"), 5 + i, 3));
            DoesNotRefuse(Check(generated), PlacementCheck.CapZone); Assert.AreEqual(0, PlacementCheck.CountInZone(generated), "authored installations never count against the player's cap");
        }
        [Test] public void TheWorldCapIsThirtyTwoAcrossEveryCachedZone()
        {
            var zones = new Dictionary<string, Zone>(); int placed = 0;
            for (int i = 0; placed < PlacementCheck.WorldCap - 1; i++)
            {
                var id = WorldMap.ToZoneID(1 + i, 1, 0); var z = new Zone(id); zones[id] = z;
                for (int k = 0; k < 8 && placed < PlacementCheck.WorldCap - 1; k++) { Assert.True(z.AddEntity(Marked("Player"), 5 + k, 3)); placed++; }
            }
            manager.ReplaceLoadedState(zones, zones.Keys.First(), new Dictionary<string, List<ZoneConnection>>());
            var field = Field(); DoesNotRefuse(Check(field), PlacementCheck.CapWorld); Assert.AreEqual(PlacementCheck.WorldCap - 1, PlacementCheck.CountInWorld(manager));
            var last = zones.Values.Last(); Assert.True(last.AddEntity(Marked("Player"), 30, 8));
            Refuses(Check(field), PlacementCheck.CapWorld); Assert.AreEqual(PlacementCheck.WorldCap, PlacementCheck.CountInWorld(manager));
        }
        [Test] public void ADestroyedEquippedOrDraggedFixtureIsRefusedAsState()
        {
            var z = Field();
            var healthy = New("Chest"); DoesNotRefuse(Check(z, fixture: healthy), PlacementCheck.State);
            var destroyed = New("Chest"); var dp = destroyed.GetPart<DestructiblePart>(); if (dp == null) { dp = new DestructiblePart(); destroyed.AddPart(dp); } dp.Gone = true; Refuses(Check(z, fixture: destroyed), PlacementCheck.State);
            var dragged = New("Chest"); dragged.AddPart(new DraggedPart()); Refuses(Check(z, fixture: dragged), PlacementCheck.State);
            var worn = New("Chest"); worn.GetPart<PhysicsPart>().Equipped = New("Player"); Refuses(Check(z, fixture: worn), PlacementCheck.State);
            DoesNotRefuse(Check(z), PlacementCheck.State);
        }
        [Test] public void BareGroundRefusesVegetationAndAnUnknownClassFailsClosed()
        {
            var bare = new PlacementSpec { Solid = false, GroundClass = PlacementCheck.BareGround };
            Refuses(Check(Field(), spec: bare), PlacementCheck.ClassSpecific); DoesNotRefuse(Check(Field(ground: "Floor"), spec: bare), PlacementCheck.ClassSpecific);
            Refuses(Check(Field(), spec: new PlacementSpec { GroundClass = "SomethingNew" }), PlacementCheck.ClassSpecific);
            Refuses(Check(Field(), spec: new PlacementSpec { GroundClass = null }), PlacementCheck.ClassSpecific);
            DoesNotRefuse(Check(Field()), PlacementCheck.ClassSpecific);
        }

        // ── contract ──
        [Test] public void EveryFailingReasonIsListedInOrderWithTheFirstAndTheCount()
        {
            var z = Field("Overworld.13.7.0", "RoadStone"); var r = Check(z, 1, 1);
            Assert.AreEqual(PlacementCheck.PoiZone, r.First); CollectionAssert.Contains(r.Reasons, PlacementCheck.Ground); CollectionAssert.Contains(r.Reasons, PlacementCheck.EdgeMargin);
            Assert.AreEqual(r.Reasons.Count - 1, r.Others); Assert.Less(r.Reasons.IndexOf(PlacementCheck.PoiZone), r.Reasons.IndexOf(PlacementCheck.Ground));
            Assert.Less(r.Reasons.IndexOf(PlacementCheck.Ground), r.Reasons.IndexOf(PlacementCheck.EdgeMargin));
        }
        [Test] public void TheCheckMutatesNothing()
        {
            var z = Field(); var item = New("Dagger"); z.AddEntity(item, X + 1, Y + 1); int count = z.EntityCount; var cellBefore = z.GetCell(X, Y).Objects.ToArray();
            for (int i = 0; i < 3; i++) Check(z);
            Assert.AreEqual(count, z.EntityCount); CollectionAssert.AreEqual(cellBefore, z.GetCell(X, Y).Objects); Assert.AreEqual(0, manager.CachedZones.Count);
        }
        [Test] public void AGenerationTimeReservationIsNotSavedDataAndIsNeverRead()
        {
            var z = Field(); z.GenReservedCells.Add((X, Y)); var r = Check(z);
            Assert.True(r.Ok, "GenReservedCells is not serialised, so a decision that read it would differ after a save round trip");
        }
        [Test] public void ThePlacementDecisionIsTheSameBeforeAndAfterASaveRoundTripOfTheCellsContents()
        {
            var z = Field(); var wall = New("SandstoneWall"); z.AddEntity(wall, X + 1, Y + 1);
            var before = Check(z).Reasons.ToArray();
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(wall); z.RemoveEntity(wall); z.AddEntity(copy, X + 1, Y + 1);
            CollectionAssert.AreEqual(before, Check(z).Reasons.ToArray());
        }
        [Test] public void CountingAThreeHundredZoneWorldIsCheap()
        {
            var zones = new Dictionary<string, Zone>();
            for (int i = 0; i < 300; i++) { var z = new Zone("synthetic-" + i); z.AddEntity(New("Chest"), 5, 5); zones[z.ZoneID] = z; }
            manager.ReplaceLoadedState(zones, "synthetic-0", new Dictionary<string, List<ZoneConnection>>());
            var watch = System.Diagnostics.Stopwatch.StartNew(); int n = PlacementCheck.CountInWorld(manager); watch.Stop();
            Assert.AreEqual(0, n); Assert.Less(watch.ElapsedMilliseconds, 250, "an action-time scan of a 300-zone cache, not a per-frame cost");
        }
    }
}
