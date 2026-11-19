using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Station contract (Docs/MACHINE-STATIONS.md, S4). The legacy ForgeWorkReceipt.Ready/Near/Resolve and AlchemyStillPart.IsNearStill are copied here verbatim as ORACLES and
    /// compared with the capability-based contract over a truth table of layouts, so replacing the scans changed no behaviour. Internal helpers the oracle needs are reached by reflection.
    /// </summary>
    public sealed class StationQueryParityTests
    {
        const BindingFlags Hidden = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly MethodInfo IsRigMethod = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.FieldWorkRigRules").GetMethod("IsRig", Hidden, null, new[] { typeof(Entity) }, null);
        static readonly MethodInfo PayloadMethod = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.FieldWorkRigRules").GetMethod("Payload", Hidden, null, new[] { typeof(Entity), typeof(bool) }, null);
        static readonly FieldInfo SpatialZoneProperty = typeof(Entity).GetField("SpatialZone", Hidden);
        static bool IsRig(Entity e) => (bool)IsRigMethod.Invoke(null, new object[] { e });
        static bool Payload(Entity e) => (bool)PayloadMethod.Invoke(null, new object[] { e, false });
        static Zone SpatialZoneOf(Entity e) => (Zone)SpatialZoneProperty.GetValue(e);

        // ── ORACLE: the legacy code, copied ──
        static class Legacy
        {
            internal static bool Current(Zone zone)
            {
                if (zone == null) return false;
                var manager = WorldLocationContext.For(zone);
                return manager == null || manager.CachedZones.TryGetValue(zone.ZoneID, out var current) && current == zone;
            }
            internal static bool Ready(Entity owner, Zone zone)
            {
                if (owner == null || zone == null) return false;
                var cell = zone?.GetEntityCell(owner);
                if (owner == null || !Current(zone) || cell == null || SpatialZoneOf(owner) != zone || !cell.Objects.Contains(owner)
                    || cell.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell
                    || owner.GetPart<ForgePart>()?.ParentEntity != owner || owner.Parts.Count(p => p is ForgePart) != 1
                    || RepairablePart.BlocksFunction(owner) || owner.HasPart<DraggedPart>()
                    || owner.GetPart<HarvestablePart>()?.Harvested == true
                    || owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0)) return false;
                return !IsRig(owner) || Payload(owner);
            }
            internal static bool Near(Entity actor, Entity owner, Zone zone)
                => actor != null && zone?.GetEntityCell(actor) is Cell cell && SpatialZoneOf(actor) == zone && cell.Objects.Contains(actor)
                    && cell.ParentZone == zone && zone.GetCell(cell.X, cell.Y) == cell
                    && Ready(owner, zone) && SpatialQuery.Distance(zone, actor, owner) <= 1;
            internal static Entity Resolve(Entity actor, Zone zone)
            {
                if (actor == null || zone == null) return null;
                var at = zone?.GetEntityCell(actor); if (at == null || SpatialZoneOf(actor) != zone || !Current(zone)) return null;
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    var cell = zone.GetCell(at.X + dx, at.Y + dy); if (cell == null) continue;
                    foreach (var owner in cell.Objects) if (Ready(owner, zone)) return owner;
                }
                return null;
            }
            internal static bool IsNearStill(Entity actor, Zone zone)
            {
                if (actor == null || zone == null) return false;
                (int x, int y) = zone.GetEntityPosition(actor);
                if (x < 0 || y < 0) return false;
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    Cell cell = zone.GetCell(x + dx, y + dy); if (cell == null) continue;
                    for (int i = 0; i < cell.Objects.Count; i++) { Entity obj = cell.Objects[i]; if (obj != null && obj.HasPart<AlchemyStillPart>()) return true; }
                }
                return false;
            }
        }

        HaulingContentScope scope; Zone zone; Entity actor;
        [SetUp] public void Setup()
        {
            scope = new HaulingContentScope(); scope.Seed(64); Diag.ResetAll();
            zone = new Zone("Overworld.1.1.0"); actor = scope.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(actor, 10, 10));
        }
        [TearDown] public void Cleanup() => scope.Dispose();

        static readonly string[] States =
            { "ready", "faulted", "repaired", "hauled", "harvested", "unharvested", "gone", "hp0", "twoForgeParts", "noForgePart", "rigGood", "rigBad", "forgeAndStill" };

        Entity Station(string state)
        {
            var e = scope.Factory.CreateEntity(state == "rigGood" || state == "rigBad" ? "AbandonedFieldWorkRig" : "TinkersForge"); Assert.NotNull(e, state);
            if (state == "rigGood") e.GetPart<RepairablePart>().Repaired = true;   // a repaired rig is a working station; an unrepaired one is faulted
            switch (state)
            {
                case "faulted": e.AddPart(new RepairablePart { Repaired = false }); break;
                case "repaired": e.AddPart(new RepairablePart { Repaired = true }); break;
                case "hauled": e.AddPart(new DraggedPart()); break;
                case "harvested": e.AddPart(new HarvestablePart { Harvested = true }); break;
                case "unharvested": e.AddPart(new HarvestablePart { Harvested = false }); break;
                case "gone": e.AddPart(new DestructiblePart { Gone = true }); break;
                case "hp0": e.AddPart(new DestructiblePart { HP = 0 }); break;
                case "twoForgeParts": e.AddPart(new ForgePart()); break;
                case "noForgePart": e.RemovePart(e.GetPart<ForgePart>()); break;
                case "rigBad": e.GetPart<RepairablePart>().Repaired = true; e.AddPart(new StackerPart()); break;
                case "forgeAndStill": e.AddPart(new AlchemyStillPart()); break;
            }
            return e;
        }

        // ── the truth table ──

        [Test] public void ReadinessNearAndResolveMatchTheLegacyCodeOverTheWholeTable()
        {
            int cases = 0;
            foreach (string state in States)
                for (int dx = -2; dx <= 2; dx++) for (int dy = -2; dy <= 2; dy++)
                {
                    var z = new Zone("Overworld.1.1.0"); var a = scope.Factory.CreateEntity("Player"); Assert.True(z.AddEntity(a, 10, 10));
                    var s = Station(state); Assert.True(z.AddEntity(s, 10 + dx, 10 + dy));
                    string where = state + " @" + dx + "," + dy;
                    Assert.AreEqual(Legacy.Ready(s, z), StationReadiness.Evaluate(s, z, "forge", out _), "Ready " + where);
                    Assert.AreEqual(Legacy.Near(a, s, z), StationQuery.Near(a, s, z, "forge"), "Near " + where);
                    Assert.AreSame(Legacy.Resolve(a, z), StationQuery.Resolve(a, z, "forge"), "Resolve " + where); cases++;
                }
            Assert.GreaterOrEqual(cases, 300);
        }

        [Test] public void TheTableIsNotVacuousBothOutcomesOccur()
        {
            int ready = 0, refused = 0;
            foreach (string state in States) { var z = new Zone("Overworld.1.1.0"); var a = scope.Factory.CreateEntity("Player"); z.AddEntity(a, 10, 10); var s = Station(state); z.AddEntity(s, 11, 10); if (StationQuery.Near(a, s, z, "forge")) ready++; else refused++; }
            Assert.Greater(ready, 2); Assert.Greater(refused, 5);
        }

        [Test] public void ActorOnTheStationCellAndActorsOutsideTheZoneMatch()
        {
            var s = Station("ready"); Assert.True(zone.AddEntity(s, 10, 10));
            Assert.AreEqual(Legacy.Near(actor, s, zone), StationQuery.Near(actor, s, zone, "forge"));
            var stranger = scope.Factory.CreateEntity("Player"); stranger.ID = "stranger";   // never placed in any zone
            Assert.AreEqual(Legacy.Near(stranger, s, zone), StationQuery.Near(stranger, s, zone, "forge")); Assert.AreSame(Legacy.Resolve(stranger, zone), StationQuery.Resolve(stranger, zone, "forge"));
            var other = new Zone("Overworld.2.2.0"); var elsewhere = scope.Factory.CreateEntity("Player"); other.AddEntity(elsewhere, 3, 3);
            Assert.AreEqual(Legacy.Near(elsewhere, s, zone), StationQuery.Near(elsewhere, s, zone, "forge"));
            Assert.AreEqual(Legacy.Ready(s, other), StationReadiness.Evaluate(s, other, "forge", out _));
            Assert.False(StationQuery.Near(null, s, zone, "forge")); Assert.False(StationQuery.Near(actor, null, zone, "forge")); Assert.Null(StationQuery.Resolve(null, zone, "forge")); Assert.Null(StationQuery.Resolve(actor, null, "forge"));
        }

        [Test] public void TwoAdjacentForgesResolveInTheLegacyScanOrder()
        {
            var a = Station("ready"); var b = Station("ready"); Assert.True(zone.AddEntity(a, 11, 9)); Assert.True(zone.AddEntity(b, 9, 11));
            Assert.AreSame(Legacy.Resolve(actor, zone), StationQuery.Resolve(actor, zone, "forge")); Assert.AreSame(b, StationQuery.Resolve(actor, zone, "forge"), "dx runs -1..1 on the outside: the WEST forge is scanned first");
            b.AddPart(new DraggedPart()); Assert.AreSame(a, StationQuery.Resolve(actor, zone, "forge"), "a hauled forge is skipped for the next ready one");
        }

        [Test] public void AStationInTwoCellsOfObjectsKeepsTheCellObjectOrder()
        {
            var a = Station("ready"); var b = Station("ready"); Assert.True(zone.AddEntity(a, 11, 10)); Assert.True(zone.AddEntity(b, 11, 10));
            Assert.AreSame(Legacy.Resolve(actor, zone), StationQuery.Resolve(actor, zone, "forge"));
        }

        // ── the still keeps its presence-only rule ──

        [Test] public void StillPresenceOnlyMatchesTheLegacyScanOverTheTable()
        {
            for (int dx = -2; dx <= 2; dx++) for (int dy = -2; dy <= 2; dy++)
            {
                var z = new Zone("Overworld.1.1.0"); var a = scope.Factory.CreateEntity("Player"); z.AddEntity(a, 10, 10);
                var still = scope.Factory.CreateEntity("AlchemyStill"); z.AddEntity(still, 10 + dx, 10 + dy);
                Assert.AreEqual(Legacy.IsNearStill(a, z), AlchemyStillPart.IsNearStill(a, z), "still @" + dx + "," + dy);
            }
            Assert.False(AlchemyStillPart.IsNearStill(null, zone)); Assert.False(AlchemyStillPart.IsNearStill(actor, null));
        }

        [Test] public void StillPresenceOnlyKeepsItsLegacyLeniency()
        {
            // a still with a repair fault, hauled and half-destroyed is STILL near: presence only, no readiness (the older rule is preserved on purpose)
            var still = scope.Factory.CreateEntity("AlchemyStill"); still.AddPart(new RepairablePart { Repaired = false }); still.AddPart(new DraggedPart()); still.AddPart(new DestructiblePart { HP = 0 });
            Assert.True(zone.AddEntity(still, 11, 10)); Assert.True(Legacy.IsNearStill(actor, zone)); Assert.True(AlchemyStillPart.IsNearStill(actor, zone));
            Assert.False(StationQuery.Near(actor, still, zone, "still"), "the strict contract would refuse it; PresenceOnly is the legacy choice, kept until a later slice proves it unreachable");
        }

        [Test] public void AForgeIsNotAStillAndAStillIsNotAForge()
        {
            var forge = Station("ready"); Assert.True(zone.AddEntity(forge, 11, 10)); Assert.False(AlchemyStillPart.IsNearStill(actor, zone)); Assert.True(StationQuery.Near(actor, forge, zone, "forge"));
            var still = scope.Factory.CreateEntity("AlchemyStill"); zone.AddEntity(still, 9, 10); Assert.True(AlchemyStillPart.IsNearStill(actor, zone)); Assert.False(StationReadiness.Evaluate(still, zone, "forge", out _));
        }

        // ── capability: found by what a Part provides, not by its type ──

        sealed class BenchPart : Part, IStationCapability
        {
            public override string Name => "TestBench"; static readonly string[] K = { "bench" };
            public IReadOnlyList<string> Kinds => K; public int Reach => 2; public ReadinessMode Readiness => ReadinessMode.Strict;
        }
        sealed class VetoPart : Part, IStationReadinessGuard
        {
            public override string Name => "TestVeto"; public bool Block = true;
            public bool TryBlock(Entity station, Zone z, out string code) { code = Block ? "out-of-fuel" : null; return Block; }
        }
        sealed class DenyPart : Part, IStationAccessGuard
        {
            public override string Name => "TestDeny"; public int Asked;
            public bool TryDeny(Entity a, Entity station, Zone z, string kind, string command, out string code, out string reason) { Asked++; code = "closed"; reason = "The keeper is away."; return true; }
        }
        Entity Bench()
        {
            var e = scope.Factory.CreateEntity("Chair"); e.AddPart(new BenchPart()); return e;   // any ordinary blueprint plus the capability
        }

        [Test] public void ANewKindIsFoundByCapabilityWithNoEditToTheScanners()
        {
            var bench = Bench(); Assert.True(zone.AddEntity(bench, 12, 10));   // two cells away: inside the bench's own Reach of 2
            Assert.AreSame(null, StationQuery.Resolve(actor, zone, "bench"), "Resolve scans the 3x3 like every shipped station; reach is checked by Near");
            Assert.True(StationQuery.Near(actor, bench, zone, "bench"), "Near honours the provider's reach");
            var far = Bench(); Assert.True(zone.AddEntity(far, 13, 10)); Assert.False(StationQuery.Near(actor, far, zone, "bench"), "three cells is beyond a reach of two");
            var near = Bench(); Assert.True(zone.AddEntity(near, 11, 10)); Assert.AreSame(near, StationQuery.Resolve(actor, zone, "bench"));
            Assert.AreSame(null, StationQuery.Resolve(actor, zone, "forge"), "a bench is not a forge");
        }

        [Test] public void ABareNamedBlueprintWithoutAProviderPartIsNotAStation()
        {
            var forge = Station("noForgePart"); Assert.True(zone.AddEntity(forge, 11, 10)); Assert.AreEqual("TinkersForge", forge.BlueprintName);
            Assert.False(StationReadiness.Evaluate(forge, zone, "forge", out string code)); Assert.AreEqual(StationReadiness.NoProvider, code); Assert.Null(StationQuery.Resolve(actor, zone, "forge"));
        }

        [TestCase("faulted", "faulted")] [TestCase("hauled", "hauled")] [TestCase("harvested", "harvested")] [TestCase("gone", "destroyed")] [TestCase("hp0", "destroyed")]
        [TestCase("twoForgeParts", "two-providers")] [TestCase("noForgePart", "no-provider")] [TestCase("rigBad", "rig-payload")]
        public void ReadinessReasonCodesAreStable(string state, string expected)
        {
            var s = Station(state); Assert.True(zone.AddEntity(s, 11, 10));
            Assert.False(StationReadiness.Evaluate(s, zone, "forge", out string code)); Assert.AreEqual(expected, code);
        }
        [Test] public void AReadyStationHasNoBlockCode() { var s = Station("ready"); zone.AddEntity(s, 11, 10); Assert.True(StationReadiness.Evaluate(s, zone, "forge", out string code)); Assert.Null(code); }

        [Test] public void AReadinessGuardPartCanVetoAndLiftingItRestoresReadiness()
        {
            var s = Station("ready"); var veto = new VetoPart(); s.AddPart(veto); Assert.True(zone.AddEntity(s, 11, 10));
            Assert.False(StationReadiness.Evaluate(s, zone, "forge", out string code)); Assert.AreEqual("out-of-fuel", code); Assert.False(StationQuery.Near(actor, s, zone, "forge"));
            veto.Block = false; Assert.True(StationQuery.Near(actor, s, zone, "forge"), "counter: with the guard lifted the same station is ready");
        }

        [Test] public void CollectIncludesUnreadyStationsWithTheirBlockCode()
        {
            var good = Station("ready"); var bad = Station("faulted"); var decoy = scope.Factory.CreateEntity("Chair");
            Assert.True(zone.AddEntity(good, 11, 10)); Assert.True(zone.AddEntity(bad, 9, 10)); Assert.True(zone.AddEntity(decoy, 10, 11));
            var into = new List<StationCandidate>(); StationQuery.Collect(actor, zone, "forge", into);
            Assert.AreEqual(2, into.Count); Assert.True(into.Single(c => c.Station == good).Ready); Assert.AreEqual("faulted", into.Single(c => c.Station == bad).BlockCode);
        }

        // ── the access seam is inert until a guard exists ──

        [Test] public void NoAccessGuardMeansAllowedAndAGuardDeniesBeforeAnyClaim()
        {
            var s = Station("ready"); Assert.True(zone.AddEntity(s, 11, 10));
            Assert.True(StationAccess.Allowed(actor, s, zone, "forge", "Craft", out string c0, out string r0)); Assert.Null(c0); Assert.Null(r0);
            var deny = new DenyPart(); s.AddPart(deny);
            Assert.False(StationAccess.Allowed(actor, s, zone, "forge", "Craft", out string code, out string reason)); Assert.AreEqual("closed", code); Assert.AreEqual("The keeper is away.", reason); Assert.AreEqual(1, deny.Asked);
            Assert.True(StationAccess.Allowed(actor, null, zone, "forge", "Craft", out _, out _), "no station, nothing to guard");
        }

        // ── the receipt retains the exact station through the commit ──

        sealed class ProbePart : Part
        {
            public override string Name => "StationProbe"; public int Calls; public Func<InventoryTransaction, StationReceipt> Begin; public Action Mutate; public StationReceipt Receipt;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "Probe") return true;
                Calls++; var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction"); Receipt = Begin(tx);
                if (Receipt != null && Mutate != null) tx.Do(Mutate, () => { });
                e.Handled = Receipt != null; return true;
            }
        }
        InventoryCommandResult Run(Entity station, Action mutate, out ProbePart probe)
        {
            scope.Factory.LoadBlueprints("{\"Objects\":[{\"Name\":\"TestProbeItem\",\"Parts\":[{\"Name\":\"Render\",\"Params\":[{\"Key\":\"DisplayName\",\"Value\":\"probe\"},{\"Key\":\"RenderString\",\"Value\":\"p\"},{\"Key\":\"ColorString\",\"Value\":\"&y\"},{\"Key\":\"RenderLayer\",\"Value\":\"5\"}]},{\"Name\":\"Physics\",\"Params\":[{\"Key\":\"Solid\",\"Value\":\"false\"}]}],\"Stats\":[],\"Tags\":[]}]}");
            var item = scope.Factory.CreateEntity("TestProbeItem"); probe = new ProbePart(); item.AddPart(probe); Assert.True(zone.AddEntity(item, 9, 9));
            var p = probe; p.Begin = tx => StationReceipt.Begin(actor, zone, tx, "Probe", "forge"); p.Mutate = mutate;
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item, "Probe"), actor, zone);
        }

        [Test] public void BeginRetainsTheExactStationAndTheControlCommits()
        {
            var s = Station("ready"); Assert.True(zone.AddEntity(s, 11, 10));
            var ok = Run(s, null, out var probe); Assert.True(ok.Success, ok.ErrorMessage + " calls=" + probe.Calls); Assert.AreSame(s, probe.Receipt.Station);
        }

        [Test] public void ACallbackThatMovesTheStationAwayRefusesTheCommit()
        {
            var s = Station("ready"); Assert.True(zone.AddEntity(s, 11, 10));
            var result = Run(s, () => zone.MoveEntity(s, 14, 14), out _); Assert.False(result.Success, "the station left reach before the commit");
        }

        [Test] public void ACallbackThatFoulsTheStationRefusesTheCommit()
        {
            var s = Station("ready"); Assert.True(zone.AddEntity(s, 11, 10));
            var result = Run(s, () => s.AddPart(new RepairablePart { Repaired = false }), out _); Assert.False(result.Success);
        }

        [Test] public void EveryRefusedBeginEmitsAStationRefusedDiagAndNoStationMeansNoReceipt()
        {
            Diag.ResetAll(); var result = Run(null, null, out var probe);   // no forge anywhere
            Assert.False(result.Success); Assert.Null(probe.Receipt);
            Assert.That(Diag.Snapshot(20).Where(r => r.Kind == "StationRefused").Select(r => r.PayloadJson), Has.Some.Contains("no-station"));
        }

        [Test] public void AnAccessGuardRefusesTheBeginBeforeAnyClaimIsTaken()
        {
            var s = Station("ready"); var deny = new DenyPart(); s.AddPart(deny); Assert.True(zone.AddEntity(s, 11, 10)); Diag.ResetAll();
            var result = Run(s, null, out var probe); Assert.False(result.Success); Assert.Null(probe.Receipt); Assert.AreEqual(1, deny.Asked);
            Assert.That(Diag.Snapshot(20).Select(r => r.PayloadJson), Has.Some.Contains("closed"));
        }

        // ── the scans are really gone ──

        [Test] public void TheLegacyScansAreForwardersWithNoLoopOfTheirOwn()
        {
            string root = UnityEngine.Application.dataPath + "/Scripts/Gameplay/";
            string forge = System.IO.File.ReadAllText(root + "Weaponcraft/ForgeWorkReceipt.cs"), still = System.IO.File.ReadAllText(root + "Alchemy/AlchemyStillPart.cs");
            Assert.That(forge, Does.Not.Contain("for (int dx"), "ForgeWorkReceipt must not carry its own 3x3 scan"); Assert.That(still, Does.Not.Contain("for (int dx"), "AlchemyStillPart must not carry its own 3x3 scan");
            Assert.That(forge, Does.Contain("StationQuery.Resolve")); Assert.That(still, Does.Contain("StationQuery.NearPresence"));
        }

        [Test] public void ShippedStationsAdvertiseTheirKinds()
        {
            var forge = scope.Factory.CreateEntity("TinkersForge").GetPart<ForgePart>(); CollectionAssert.AreEqual(new[] { "forge", "cold-work" }, forge.Kinds); Assert.AreEqual(ReadinessMode.Strict, forge.Readiness); Assert.AreEqual(1, forge.Reach);
            var still = scope.Factory.CreateEntity("AlchemyStill").GetPart<AlchemyStillPart>(); CollectionAssert.AreEqual(new[] { "still" }, still.Kinds); Assert.AreEqual(ReadinessMode.PresenceOnly, still.Readiness);
        }
    }
}
