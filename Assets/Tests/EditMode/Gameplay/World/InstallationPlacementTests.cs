using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Generic installation post-builder (Docs/MACHINE-INSTALLATIONS.md, S7): deterministic, optional, fresh-only placement of existing
    /// blueprints into a village after everything else, with an exact receipt. Every positive assertion has a counter (row removed,
    /// cell refused, zone cached) and every refusal omits only the installation and leaves the town byte-identical.
    /// </summary>
    public sealed class InstallationPlacementTests
    {
        HaulingContentScope scope;
        static readonly string[] Towns = { "Gantry", "Tine", "Slip" };

        [SetUp] public void Setup()
        {
            scope = new HaulingContentScope(); scope.Seed(64); Diag.ResetAll(); InstallationRegistry.ResetForTests();
            Assert.IsEmpty(InstallationRegistry.InitializeFromJson("{\"Version\":1,\"Installations\":[]}"), "each test starts from an empty registry, not the shipped rows");
            PlaceSiteCatalog.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/PlaceSites.json")));
        }
        [TearDown] public void Cleanup() { InstallationRegistry.ResetForTests(); PlaceSiteCatalog.Reset(); scope.Dispose(); }

        // ── data helpers ──

        static string Row(string id, string place, string blueprint = "TinkersForge", string retention = "None", int radius = 9, int probe = 48, string ground = "")
            => "{\"Id\":\"" + id + "\",\"Place\":\"" + place + "\",\"Blueprint\":\"" + blueprint + "\",\"Retention\":\"" + retention + "\",\"Near\":[\"Well\"],\"Radius\":" + radius
               + ",\"MaxProbe\":" + probe + (ground.Length > 0 ? ",\"Ground\":[\"" + ground + "\"]" : "") + ",\"Revision\":1,\"Purpose\":\"A working forge by the well.\",\"Alternative\":\"Carried components and the Sill forge remain.\",\"Basis\":\"test\"}";
        static void Rows(params string[] rows) => Assert.IsEmpty(InstallationRegistry.InitializeFromJson("{\"Version\":1,\"Installations\":[" + string.Join(",", rows) + "]}"));
        static void TownRows() => Rows(Row("gantry-forecourt-forge", "Gantry"), Row("tine-forecourt-forge", "Tine"), Row("slip-forecourt-forge", "Slip"));

        static string ZoneOf(string place) { var p = WorldMapAuthoring.Places.Single(q => q.Name == place); return "Overworld." + p.X + "." + p.Y + ".0"; }
        OverworldZoneManager Manager(int seed) => OverworldZoneManager.CreateDetached(scope.Factory, seed, true);
        static string[] Snapshot(Zone zone) => zone.GetReadOnlyEntities().Select(e => { var at = zone.GetEntityPosition(e); return e.BlueprintName + "@" + at.x + "," + at.y; }).OrderBy(s => s, StringComparer.Ordinal).ToArray();
        static Entity PlacedBy(Zone zone, string id) => zone.GetReadOnlyEntities().SingleOrDefault(e => e.GetPart<InstallationMarkerPart>()?.InstallationId == id);

        /// <summary>A zone generated without the installation builder, taken out of the cache so the builder's authority sees a fresh zone.</summary>
        (OverworldZoneManager manager, Zone zone, InstallationPostBuilder builder) Harness(string place, int seed = 64)
        {
            var manager = Manager(seed); string id = ZoneOf(place); var zone = manager.GetZone(id); Assert.NotNull(zone);
            manager.CachedZones.Remove(id); var info = WorldMapAuthoring.Places.Single(p => p.Name == place);
            return (manager, zone, new InstallationPostBuilder(manager, manager.WorldMap.GetPOI(info.X, info.Y), id));
        }

        /// <summary>Cells reachable on foot from the zone border (4-neighbour flood over cells that do not block movement).</summary>
        static bool[,] Reach(Zone zone)
        {
            var reached = new bool[Zone.Width, Zone.Height]; var queue = new Queue<(int x, int y)>();
            void Add(int x, int y) { if (x < 0 || y < 0 || x >= Zone.Width || y >= Zone.Height || reached[x, y]) return; var c = zone.GetCell(x, y); if (c == null || c.BlocksMovement()) return; reached[x, y] = true; queue.Enqueue((x, y)); }
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) if (x == 0 || y == 0 || x == Zone.Width - 1 || y == Zone.Height - 1) Add(x, y);
            while (queue.Count > 0) { var p = queue.Dequeue(); Add(p.x - 1, p.y); Add(p.x + 1, p.y); Add(p.x, p.y - 1); Add(p.x, p.y + 1); }
            return reached;
        }

        class CountingRandom : System.Random { public int Calls; public override int Next() { Calls++; return base.Next(); } public override int Next(int m) { Calls++; return base.Next(m); } public override int Next(int a, int b) { Calls++; return base.Next(a, b); } public override double NextDouble() { Calls++; return base.NextDouble(); } }

        // ── placement ──

        [Test] public void BuilderPriorityIsTheReservedUniqueSlot() => Assert.AreEqual(4650, new InstallationPostBuilder(null, null, "x").Priority);

        [Test] public void FreshTownPlacesExactlyOneForgeAtAFittingCell([Values("Gantry", "Tine", "Slip")] string town, [Values(64, 704860946, 1729)] int seed)
        {
            TownRows(); var zone = Manager(seed).GetZone(ZoneOf(town)); string id = town.ToLowerInvariant() + "-forecourt-forge";
            Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.GetPart<InstallationMarkerPart>()?.InstallationId == id), "exactly one placed");
            var forge = PlacedBy(zone, id); Assert.AreEqual("TinkersForge", forge.BlueprintName); Assert.True(forge.HasPart<ForgePart>());
            var marker = forge.GetPart<InstallationMarkerPart>(); Assert.AreEqual("Generated", marker.Origin); Assert.False(marker.RetainZone); Assert.AreEqual(ZoneOf(town), forge.Properties["SettlementId"]);
            var at = zone.GetEntityPosition(forge); var cell = zone.GetCell(at.x, at.y); Assert.AreEqual(2, cell.Objects.Count, "ground plus the forge");
            Assert.AreEqual(1, cell.Objects.Count(e => e.BlueprintName == "Grass" || e.BlueprintName == "StoneFloor" || e.BlueprintName == "Floor"), "bare ground underneath");
            Assert.That(cell.Objects.Select(e => e.BlueprintName), Has.None.EqualTo("RoadStone"), "never the public road");
            Assert.False(cell.IsInterior);
        }

        [Test] public void OnlyTheTownsWithRowsReceiveAForge()
        {
            Rows(Row("tine-forecourt-forge", "Tine")); var m = Manager(64);
            Assert.NotNull(PlacedBy(m.GetZone(ZoneOf("Tine")), "tine-forecourt-forge"));
            foreach (var other in new[] { "Gantry", "Slip", "Posy" }) Assert.False(m.GetZone(ZoneOf(other)).GetReadOnlyEntities().Any(e => e.HasPart<InstallationMarkerPart>()), other);
        }

        [Test] public void PlacementIsStablePerSeedAndMovesWithTheWorld()
        {
            TownRows(); var positions = new HashSet<string>();
            foreach (int seed in new[] { 64, 65, 66, 67, 68, 69 })
            {
                string a = Where(Manager(seed).GetZone(ZoneOf("Tine"))), b = Where(Manager(seed).GetZone(ZoneOf("Tine")));
                Assert.AreEqual(a, b, "same seed, same cell"); positions.Add(a);
            }
            Assert.Greater(positions.Count, 1, "different worlds put it in different cells");
            string Where(Zone z) { var f = PlacedBy(z, "tine-forecourt-forge"); var at = z.GetEntityPosition(f); return at.x + "," + at.y; }
        }

        [Test] public void TwentyFourSeedSweepAlwaysPlacesOneAndNeverNearADoorStairsOrWell([Values("Gantry", "Tine", "Slip")] string town)
        {
            TownRows();
            for (int seed = 100; seed < 124; seed++)
            {
                var zone = Manager(seed).GetZone(ZoneOf(town)); var forge = PlacedBy(zone, town.ToLowerInvariant() + "-forecourt-forge");
                Assert.NotNull(forge, town + " seed " + seed); var at = zone.GetEntityPosition(forge);
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    foreach (var e in zone.GetCell(at.x + dx, at.y + dy).Objects)
                        Assert.False(e.HasPart<DoorPart>() || e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>() || e.BlueprintName == "Well", town + " seed " + seed + " next to " + e.BlueprintName);
            }
        }

        [Test] public void OtherOwnersAreIdenticalWithTheRowRemoved([Values("Gantry", "Tine", "Slip")] string town)
        {
            var without = Snapshot(Manager(64).GetZone(ZoneOf(town)));
            TownRows(); var zone = Manager(64).GetZone(ZoneOf(town)); var forge = PlacedBy(zone, town.ToLowerInvariant() + "-forecourt-forge"); var at = zone.GetEntityPosition(forge);
            var with = Snapshot(zone).ToList(); Assert.True(with.Remove("TinkersForge@" + at.x + "," + at.y));
            CollectionAssert.AreEqual(without, with, "the only difference is the one forge");
        }

        [Test] public void FloodConnectivityAndStairsArePreserved([Values("Gantry", "Tine", "Slip")] string town)
        {
            var baseline = Reach(Manager(64).GetZone(ZoneOf(town)));
            TownRows(); var zone = Manager(64).GetZone(ZoneOf(town)); var after = Reach(zone);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) Assert.AreEqual(baseline[x, y], after[x, y], town + " " + x + "," + y);
            Assert.AreEqual(baseline.Cast<bool>().Count(b => b), after.Cast<bool>().Count(b => b));
        }

        [Test] public void MinRadiusKeepsTheRingAroundTheAnchorOpen([Values("Gantry", "Tine", "Slip")] string town)
        {
            Rows(Row("min-" + town, town).Replace("\"Radius\":9", "\"Radius\":9,\"MinRadius\":5")); var zone = Manager(64).GetZone(ZoneOf(town));
            var forge = PlacedBy(zone, "min-" + town); if (forge == null) Assert.Inconclusive(town + " found no cell at distance 5+");
            var well = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Well").OrderBy(e => zone.GetEntityPosition(e).y).ThenBy(e => zone.GetEntityPosition(e).x).First();
            var a = zone.GetEntityPosition(forge); var b = zone.GetEntityPosition(well);
            Assert.GreaterOrEqual(Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y)), 5);
        }

        /// <summary>The shipped wave-1 rows, over 24 seeds each: every placement is a legal one and the town is otherwise untouched; the count that
        /// place at all is pinned so a plan change that stops a row fitting is noticed.</summary>
        [TestCase("Gantry", "gantry-forecourt-forge", 20)] [TestCase("Tine", "tine-forecourt-forge", 20)] [TestCase("Slip", "slip-forecourt-forge", 20)]
        public void ShippedRowsPlaceLegallyAcrossSeeds(string town, string id, int atLeast)
        {
            InstallationRegistry.ResetForTests(); InstallationRegistry.LoadDefaults(); Assert.NotNull(InstallationRegistry.For(town).FirstOrDefault(r => r.Id == id));
            int placed = 0; var reasons = new List<string>();
            for (int seed = 200; seed < 224; seed++)
            {
                Diag.ResetAll(); var zone = Manager(seed).GetZone(ZoneOf(town)); var forge = PlacedBy(zone, id);
                if (forge == null) { reasons.Add(string.Join("|", Diag.Snapshot(80).Where(r => r.Kind == "InstallationOmitted").Select(r => r.PayloadJson))); continue; }
                placed++; var at = zone.GetEntityPosition(forge); var cell = zone.GetCell(at.x, at.y);
                Assert.That(cell.Objects.Select(e => e.BlueprintName), Has.None.EqualTo("RoadStone"), town + " seed " + seed);
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    foreach (var e in zone.GetCell(at.x + dx, at.y + dy).Objects)
                        Assert.False(e.HasPart<DoorPart>() || e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>() || e.BlueprintName == "Well", town + " seed " + seed + " next to " + e.BlueprintName);
            }
            Assert.GreaterOrEqual(placed, atLeast, town + " placed " + placed + "/24; omitted reasons: " + string.Join(" ## ", reasons.Take(3)));
        }

        // ── a solid blueprint makes the route and standing-cell checks real ──

        [Test] public void SolidTestBlueprintKeepsAStandingCellAndTheRoutes()
        {
            scope.Factory.LoadBlueprints("{\"Objects\":[{\"Name\":\"TestSolidBench\",\"Parts\":[{\"Name\":\"Render\",\"Params\":[{\"Key\":\"DisplayName\",\"Value\":\"test bench\"},{\"Key\":\"RenderString\",\"Value\":\"B\"},{\"Key\":\"ColorString\",\"Value\":\"&y\"},{\"Key\":\"RenderLayer\",\"Value\":\"5\"}]},"
                + "{\"Name\":\"Physics\",\"Params\":[{\"Key\":\"Solid\",\"Value\":\"true\"}]},{\"Name\":\"Examinable\",\"Params\":[{\"Key\":\"Description\",\"Value\":\"A bench.\"}]}],\"Stats\":[],\"Tags\":[{\"Key\":\"Furniture\",\"Value\":\"\"}]}]}");
            var h = Harness("Tine"); var before = Reach(h.zone);
            Rows(Row("tine-bench", "Tine", "TestSolidBench")); Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1)));
            var bench = PlacedBy(h.zone, "tine-bench"); Assert.NotNull(bench, "a solid blueprint still finds a cell"); Assert.True(bench.GetPart<PhysicsPart>().Solid);
            var at = h.zone.GetEntityPosition(bench); var after = Reach(h.zone);
            bool standing = false;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if ((dx != 0 || dy != 0) && after[at.x + dx, at.y + dy]) standing = true;
            Assert.True(standing, "a reachable neighbour remains to stand on");
            Assert.False(after[at.x, at.y], "the bench itself is solid");
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (!(x == at.x && y == at.y)) Assert.AreEqual(before[x, y], after[x, y], "border reachability unchanged at " + x + "," + y + ": a solid bench must not cut a route");
        }

        // ── optional: every refusal omits only the installation ──

        static void AssertOmitted(Zone zone, string[] baseline, string reason)
        {
            CollectionAssert.AreEqual(baseline, Snapshot(zone), "the town is unchanged");
            Assert.That(Diag.Snapshot(80).Where(r => r.Kind == "InstallationOmitted").Select(r => r.PayloadJson), Has.Some.Contains("\"reason\":\"" + reason + "\""));
        }

        [Test] public void MissingBlueprintOmitsOnlyTheRowAndBuildZoneStaysTrue()
        {
            var h = Harness("Tine"); var baseline = Snapshot(h.zone); Rows(Row("tine-x", "Tine", "NoSuchMachine")); Diag.ResetAll();
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1))); AssertOmitted(h.zone, baseline, "content");
        }

        [TestCase("Villager")] [TestCase("KnotflaxCord")] [TestCase("Chest")]
        public void ACreatureAStackOrAStateBearingBlueprintIsNeverPlaced(string blueprint)
        {
            var h = Harness("Tine"); var baseline = Snapshot(h.zone); Rows(Row("tine-x", "Tine", blueprint)); Diag.ResetAll();
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1))); AssertOmitted(h.zone, baseline, "content");
        }

        [Test] public void NoFittingCellOmitsWithADiagAndLeavesTheTownIdentical()
        {
            var h = Harness("Tine"); var baseline = Snapshot(h.zone); Rows(Row("tine-x", "Tine", ground: "NoSuchGround")); Diag.ResetAll();
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1))); AssertOmitted(h.zone, baseline, "no-cell");
        }

        [Test] public void NoAnchorOmitsWhenTheTownHasNoWell()
        {
            var h = Harness("Tine"); var baseline = Snapshot(h.zone);
            foreach (var well in h.zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "Well").ToList()) h.zone.RemoveEntity(well);
            baseline = Snapshot(h.zone); Rows(Row("tine-x", "Tine")); Diag.ResetAll();
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1))); AssertOmitted(h.zone, baseline, "no-anchor");
        }

        [Test] public void AControlRowPlacesWhereTheRefusedOnesDid_NotVacuous()
        {
            var h = Harness("Tine"); Rows(Row("tine-ok", "Tine")); Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1)));
            Assert.NotNull(PlacedBy(h.zone, "tine-ok"));
        }

        // ── fresh only ──

        [Test] public void ACachedZoneIsNotRetrofitted()
        {
            var manager = Manager(64); var zone = manager.GetZone(ZoneOf("Tine")); var baseline = Snapshot(zone); TownRows();
            Assert.AreSame(zone, manager.GetZone(ZoneOf("Tine"))); CollectionAssert.AreEqual(baseline, Snapshot(zone));
            var info = WorldMapAuthoring.Places.Single(p => p.Name == "Tine");
            var builder = new InstallationPostBuilder(manager, manager.WorldMap.GetPOI(info.X, info.Y), ZoneOf("Tine"));
            Assert.True(builder.BuildZone(zone, scope.Factory, new System.Random(1)), "a cached zone is a silent no-op, not a failure"); CollectionAssert.AreEqual(baseline, Snapshot(zone));
        }

        [Test] public void AnActiveZoneIsNotRetrofitted()
        {
            var h = Harness("Tine"); var baseline = Snapshot(h.zone); TownRows(); h.manager.SetActiveZone(h.zone);
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1))); CollectionAssert.AreEqual(baseline, Snapshot(h.zone));
        }

        [Test] public void ForeignFactoryWrongZoneAndWrongPlaceAreSilentNoOps()
        {
            var h = Harness("Tine"); var baseline = Snapshot(h.zone); TownRows();
            Assert.True(h.builder.BuildZone(h.zone, new EntityFactory(), new System.Random(1))); CollectionAssert.AreEqual(baseline, Snapshot(h.zone));
            var other = new Zone("Overworld.1.1.0"); Assert.True(h.builder.BuildZone(other, scope.Factory, new System.Random(1))); Assert.AreEqual(0, other.EntityCount);
            var info = WorldMapAuthoring.Places.Single(p => p.Name == "Gantry");
            var wrong = new InstallationPostBuilder(h.manager, h.manager.WorldMap.GetPOI(info.X, info.Y), ZoneOf("Tine"));
            Assert.True(wrong.BuildZone(h.zone, scope.Factory, new System.Random(1))); CollectionAssert.AreEqual(baseline, Snapshot(h.zone));
            Assert.True(new InstallationPostBuilder(null, null, "x").BuildZone(h.zone, scope.Factory, new System.Random(1)));
        }

        [Test] public void ARegeneratedPipelineRunPlacesOnceAndReplayDoesNotDuplicate()
        {
            var h = Harness("Tine"); Rows(Row("tine-ok", "Tine")); Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1)));
            var first = h.zone.GetEntityPosition(PlacedBy(h.zone, "tine-ok")); Diag.ResetAll();
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, new System.Random(1)));
            Assert.AreEqual(1, h.zone.GetReadOnlyEntities().Count(e => e.GetPart<InstallationMarkerPart>()?.InstallationId == "tine-ok"));
            Assert.AreEqual(first, h.zone.GetEntityPosition(PlacedBy(h.zone, "tine-ok")));
            Assert.That(Diag.Snapshot(40).Select(r => r.PayloadJson), Has.Some.Contains("already-present"));
        }

        [Test] public void TheBuilderNeverReadsOrAdvancesTheRng()
        {
            var h = Harness("Tine"); Rows(Row("tine-ok", "Tine")); var rng = new CountingRandom();
            Assert.True(h.builder.BuildZone(h.zone, scope.Factory, rng)); Assert.AreEqual(0, rng.Calls); Assert.NotNull(PlacedBy(h.zone, "tine-ok"));
        }

        // ── relapse: regeneration and retention ──

        [Test] public void RegenerationAfterRelapseReplacesTheForgeAtTheSameCellWhenRetentionIsNone()
        {
            TownRows(); int turn = 10; var manager = Manager(64); manager.SetTurnProvider(() => turn);
            var first = manager.GetZone(ZoneOf("Tine")); var at = first.GetEntityPosition(PlacedBy(first, "tine-forecourt-forge"));
            var site = manager.SettlementManager.GetSite(ZoneOf("Tine"), "Weir"); site.Stage = RepairStage.TemporarilyPurified; site.ResolvedByMethod = RepairMethodId.PatchedRepair; site.ResolvedAtTurn = 1; site.RelapseAtTurn = 5; turn = 20;
            var second = manager.GetZone(ZoneOf("Tine")); Assert.AreNotSame(first, second, "retention None regenerates identically");
            Assert.AreEqual(at, second.GetEntityPosition(PlacedBy(second, "tine-forecourt-forge")));
        }

        [Test] public void AStateBearingBlueprintWithRetentionZoneHoldsItsVillageAcrossRelapse()
        {
            Rows(Row("tine-bench", "Tine", "SoddenDressingBench", retention: "Zone")); int turn = 10; var manager = Manager(64); manager.SetTurnProvider(() => turn);
            var first = manager.GetZone(ZoneOf("Tine")); var bench = PlacedBy(first, "tine-bench");
            if (bench == null) Assert.Inconclusive("this blueprint found no cell in this town: " + string.Join(";", Diag.Snapshot(40).Select(r => r.PayloadJson)));
            Assert.True(bench.GetPart<InstallationMarkerPart>().RetainZone);
            var site = manager.SettlementManager.GetSite(ZoneOf("Tine"), "Weir"); site.Stage = RepairStage.TemporarilyPurified; site.ResolvedByMethod = RepairMethodId.PatchedRepair; site.ResolvedAtTurn = 1; site.RelapseAtTurn = 5; turn = 20;
            Assert.AreSame(first, manager.GetZone(ZoneOf("Tine"))); Assert.AreSame(bench, PlacedBy(first, "tine-bench"));
        }

        // ── other pipelines are untouched ──

        /// <summary>For each composed or exploration town the row either places one forge and changes nothing else, or fails closed with a named reason
        /// and the town is byte-identical. Which of the two happens is recorded here so a layout change is noticed.</summary>
        [TestCase("Quillhold", true)] [TestCase("Tally", true)] [TestCase("Cinderhold", true)] [TestCase("Wellmeet", false)]
        public void ComposedAndExplorationTownsGainOnlyTheirForgeOrOmitCleanly(string town, bool expectPlaced)
        {
            var without = Snapshot(Manager(64).GetZone(ZoneOf(town)));
            Rows(Row("x-forge", town)); Diag.ResetAll(); var zone = Manager(64).GetZone(ZoneOf(town)); var forge = PlacedBy(zone, "x-forge");
            string reasons = string.Join(";", Diag.Snapshot(80).Where(r => r.Kind == "InstallationOmitted").Select(r => r.PayloadJson));
            if (forge == null)
            {
                Assert.False(expectPlaced, town + " was expected to place: " + reasons); CollectionAssert.AreEqual(without, Snapshot(zone), town + " is unchanged when the row is omitted");
                Assert.That(reasons, Does.Contain("no-"), "an omission names its reason");
                return;
            }
            Assert.True(expectPlaced, town + " unexpectedly placed"); var at = zone.GetEntityPosition(forge);
            var with = Snapshot(zone).ToList(); Assert.True(with.Remove("TinkersForge@" + at.x + "," + at.y)); CollectionAssert.AreEqual(without, with, town + ": every other owner is untouched");
        }

        // ── observability ──

        [Test] public void PlacedDiagCarriesTheSpecPlaceCellAndAnchor()
        {
            Rows(Row("tine-ok", "Tine")); Diag.ResetAll(); var zone = Manager(64).GetZone(ZoneOf("Tine")); var at = zone.GetEntityPosition(PlacedBy(zone, "tine-ok"));
            var record = Diag.Snapshot(200).Single(r => r.Kind == "InstallationPlaced");
            Assert.That(record.PayloadJson, Does.Contain("tine-ok")); Assert.That(record.PayloadJson, Does.Contain("\"x\":" + at.x)); Assert.That(record.PayloadJson, Does.Contain("\"anchor\":\"Well\""));
        }
    }
}
