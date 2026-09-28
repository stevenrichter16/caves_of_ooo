using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadRarePairClueTests
    {
        const string ClueKey = "SpreadRare.PairClue";
        DensityLootTestScope scope; OverworldZoneManager manager;
        [SetUp] public void Setup() { scope = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(scope.Factory, 64); }
        [TearDown] public void Cleanup() { ClueMutationPart.Callback = null; scope?.Dispose(); }
        public sealed class ClueMutationPart : Part
        {
            public static Action<Entity> Callback;
            public override string Name => "PairClueMutation";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "ObjectCreated") Callback?.Invoke(ParentEntity); return true; }
        }
        void ObserveSign(Action<Entity> callback)
        {
            scope.Factory.RegisterPartType<ClueMutationPart>("PairClueMutation");
            scope.Factory.Blueprints["Signpost"].Parts["PairClueMutation"] = new Dictionary<string, string>();
            ClueMutationPart.Callback = callback;
        }
        Zone Pocket()
        {
            var z = new Zone(manager.RareEncounters.PairZoneID);
            Assert.True(z.AddEntity(scope.Factory.CreateEntity("Hedge"), 5, 2)); return z;
        }
        static Entity[] Pair(Zone z) => z.GetReadOnlyEntities().Where(e => e.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey)).ToArray();
        static Entity[] Clues(Zone z) => z.GetReadOnlyEntities().Where(e => e.Properties.ContainsKey(ClueKey)).ToArray();
        static string PairState(Zone z) => string.Join(";", Pair(z).OrderBy(e => e.ID).Select(e => e.ID + ":" + e.BlueprintName + ":" + z.GetEntityPosition(e) + ":" + string.Join(",", DensityLootTestScope.Gear(e).OrderBy(i => i.ID).Select(i => i.ID + ":" + i.BlueprintName + ":" + i.GetPart<PhysicsPart>().Equipped?.ID))));
        static void CheckPair(Zone z)
        {
            var pair = Pair(z); Assert.AreEqual(2, pair.Length);
            CollectionAssert.AreEquivalent(new[] { SpreadRareEncounterPlan.PairLeader, SpreadRareEncounterPlan.PairMate }, pair.Select(e => e.BlueprintName));
            Assert.AreEqual((5, 5), z.GetEntityPosition(pair.Single(e => e.BlueprintName == SpreadRareEncounterPlan.PairLeader)));
            Assert.AreEqual((7, 5), z.GetEntityPosition(pair.Single(e => e.BlueprintName == SpreadRareEncounterPlan.PairMate)));
            CollectionAssert.AreEquivalent(new[] { "ShortSword", "LeatherCap", "Cudgel" }, pair.SelectMany(DensityLootTestScope.Gear).Select(e => e.BlueprintName));
            Assert.True(pair.SelectMany(DensityLootTestScope.Gear).All(e => pair.Contains(e.GetPart<PhysicsPart>().Equipped)));
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void OptionalClueUsesRealSignAndAVisibleDryApproachOutsideBothInitialSightRadii(int seed)
        {
            manager = OverworldZoneManager.CreateDetached(scope.Factory, seed); var z = Pocket();
            Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory)); CheckPair(z);
            var signs = Clues(z); Assert.AreEqual(1, signs.Length, "A legal open source has one optional physical clue.");
            var sign = signs[0]; var p = z.GetEntityPosition(sign); Assert.AreEqual("Signpost", sign.BlueprintName);
            Assert.AreEqual(z.ZoneID, sign.GetProperty(ClueKey)); Assert.False(sign.Properties.ContainsKey(SpreadRareEncounterBuilder.SourceKey));
            Assert.True(sign.GetPart<PhysicsPart>().Solid); Assert.False(sign.GetPart<PhysicsPart>().Takeable);
            Assert.AreSame(sign, sign.GetPart<RenderPart>().ParentEntity); Assert.True(sign.GetPart<RenderPart>().Visible);
            Assert.AreEqual("I", sign.GetPart<RenderPart>().RenderString); Assert.NotNull(sign.GetPart<RegionalSignpostPart>());
            Assert.False(z.GenReservedCells.Contains(p)); Assert.True(Dry(z, p));
            Assert.True(Pair(z).All(e => Distance(p, z.GetEntityPosition(e)) > e.GetPart<BrainPart>().SightRadius));
            var readings = Neighbors(p).Where(n => Clear(z, n) && OutsideSight(z, n)).ToArray();
            Assert.IsNotEmpty(readings);
            Assert.True(readings.Any(n => AIHelpers.HasLineOfSight(z, n.x, n.y, p.x, p.y) && ReachesEdge(z, n)));
            foreach (var n in Neighbors(p)) Assert.True(Clear(z, n), "The solid sign must leave the surrounding local bypass open.");
        }
        [Test]
        public void SignFactoryRunsOnlyAfterExactPairAndEquipmentCommit()
        {
            var z = Pocket(); string captured = null; int calls = 0;
            ObserveSign(e => { calls++; CheckPair(z); captured = PairState(z); Assert.False(z.GetReadOnlyEntities().Contains(e)); });
            Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory)); Assert.AreEqual(1, calls);
            Assert.AreEqual(captured, PairState(z)); Assert.AreEqual(1, Clues(z).Length);
        }
        [Test]
        public void ClueDescribesHistoricalTracesWithoutCurrentEnemyLootOrSafetyPromises()
        {
            var z = Pocket(); Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory));
            Assert.AreEqual(1, Clues(z).Length); string text = Clues(z)[0].GetPart<ExaminablePart>().Description.ToLowerInvariant();
            StringAssert.Contains("old", text); StringAssert.Contains("shale", text); StringAssert.Contains("scraps", text); StringAssert.Contains("ditch-cutters", text);
            foreach (string forbidden in new[] { "is waiting", "are waiting", "still here", "will drop", "short sword", "safe route", "safe path" }) StringAssert.DoesNotContain(forbidden, text);
        }
        [TestCase("missing")] [TestCase("throw")] [TestCase("hidden")] [TestCase("carried")]
        [TestCase("duplicate-id")] [TestCase("foreign-owner")] [TestCase("map")]
        [TestCase("appearance")] [TestCase("footprint")]
        public void OptionalFactoryRefusalNeverRollsBackTheAlreadyCommittedPair(string change)
        {
            var z = Pocket(); var foreign = new Zone("Overworld.1.1.0"); Entity staged = null; int calls = 0; string captured = null;
            if (change == "missing") scope.Factory.Blueprints.Remove("Signpost");
            else ObserveSign(e => {
                calls++; staged = e; CheckPair(z); captured = PairState(z);
                if (change == "throw") throw new InvalidOperationException("fixture marker failure");
                if (change == "hidden") e.GetPart<RenderPart>().Visible = false;
                if (change == "appearance") e.GetPart<RenderPart>().VisualID = "ring-tree-0";
                if (change == "footprint") e.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
                if (change == "carried") e.GetPart<PhysicsPart>().InInventory = new Entity();
                if (change == "duplicate-id") e.ID = Pair(z)[0].ID;
                if (change == "foreign-owner") Assert.True(foreign.AddEntity(e, 3, 3));
                if (change == "map") { var p = WorldMap.FromZoneID(z.ZoneID); manager.WorldMap.Tiles[p.x, p.y] = BiomeType.Beating; }
            });
            bool placed = false; Assert.DoesNotThrow(() => placed = new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory));
            Assert.True(placed); CheckPair(z); Assert.IsEmpty(Clues(z));
            if (change != "missing") { Assert.AreEqual(1, calls, "The optional source must be attempted before its callback refusal is meaningful."); Assert.AreEqual(captured, PairState(z)); }
            if (change == "foreign-owner") Assert.AreSame(staged, foreign.GetCell(3, 3).Occupants.Single());
        }
        [TestCase("reserved")] [TestCase("heat")] [TestCase("coating")]
        public void NoLegalMarkerSpaceStillReturnsTheUnmodifiedPair(string change)
        {
            var z = Pocket(); z.ForEachCell((c, x, y) => {
                if (change == "reserved" && !(x >= 4 && x <= 8 && y >= 4 && y <= 6)) z.GenReservedCells.Add((x, y));
                if (change == "heat") z.TileState.AddHeat(x, y, 1);
                if (change == "coating") z.TileState.WriteCoating(x, y, "water", 3);
            });
            string tiles = z.TileState.ToSaveString(); var reserved = z.GenReservedCells.ToArray();
            Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory)); CheckPair(z); Assert.IsEmpty(Clues(z));
            Assert.AreEqual(tiles, z.TileState.ToSaveString()); CollectionAssert.AreEquivalent(reserved, z.GenReservedCells);
        }
        [Test]
        public void ExistingRegionalSignIsNotRewrittenOrAdoptedAsTheClue()
        {
            var z = Pocket(); var old = scope.Factory.CreateEntity("Signpost"); old.GetPart<ExaminablePart>().Description = "Existing regional words."; z.AddEntity(old, 30, 15);
            Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory)); Assert.AreEqual(1, Clues(z).Length);
            Assert.AreNotSame(old, Clues(z)[0]); Assert.False(old.Properties.ContainsKey(ClueKey)); Assert.AreEqual((30, 15), z.GetEntityPosition(old));
            Assert.AreEqual("Existing regional words.", old.GetPart<ExaminablePart>().Description);
        }
        [Test]
        public void RemovedClueIsNotRecreatedWhenTheExistingSourceIsVisitedAgain()
        {
            var z = Pocket(); var builder = new SpreadRareEncounterBuilder(manager); Assert.True(builder.TryPlace(z, scope.Factory));
            Assert.AreEqual(1, Clues(z).Length); z.RemoveEntity(Clues(z)[0]); string pair = PairState(z); var before = z.GetReadOnlyEntities().ToArray();
            Assert.False(builder.TryPlace(z, scope.Factory)); Assert.IsEmpty(Clues(z)); Assert.AreEqual(pair, PairState(z)); CollectionAssert.AreEquivalent(before, z.GetReadOnlyEntities());
        }
        [Test]
        public void OldPairWithoutClueIsNeverRetrofittedAndDoesNotCallTheSignFactory()
        {
            var z = Pocket(); scope.Factory.Blueprints.Remove("Signpost"); Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory));
            string pair = PairState(z); Assert.False(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory)); Assert.IsEmpty(Clues(z)); Assert.AreEqual(pair, PairState(z));
        }
        [Test]
        public void OptionalSourceDoesNotConsumeLoadoutRandomOrChangePairRolls()
        {
            scope.Seed(64); var a = Pocket(); Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(a, scope.Factory)); int next = LoadoutPart.Rng.Next();
            scope.Seed(64); scope.Factory.Blueprints.Remove("Signpost"); var b = Pocket(); Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(b, scope.Factory));
            Assert.AreEqual(next, LoadoutPart.Rng.Next());
            CollectionAssert.AreEqual(Pair(a).SelectMany(DensityLootTestScope.Gear).Select(e => e.BlueprintName), Pair(b).SelectMany(DensityLootTestScope.Gear).Select(e => e.BlueprintName));
            CollectionAssert.AreEqual(Pair(a).Select(a.GetEntityPosition), Pair(b).Select(b.GetEntityPosition));
        }
        [Test]
        public void OptionalMarkerDiagnosticsStaySeparateFromTheExistingPairCommit()
        {
            bool old = Diag.IsChannelEnabled("worldgen"); var before = new HashSet<string>(Diag.Snapshot(Diag.BufferCapacity).Select(e => e.TraceId)); Diag.SetChannel("worldgen", true);
            try {
                var a = Pocket(); Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(a, scope.Factory));
                scope.Factory.Blueprints.Remove("Signpost"); var b = Pocket(); Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(b, scope.Factory));
                var rows = Diag.Snapshot(Diag.BufferCapacity).Where(e => !before.Contains(e.TraceId)).ToArray();
                Assert.AreEqual(2, rows.Count(e => e.Kind == "SpreadRareCommitted" && e.PayloadJson.Contains("ditch-cutters")));
                foreach (string kind in new[] { "SpreadRareClueCommitted", "SpreadRareClueRejected" }) {
                    var row = rows.SingleOrDefault(e => e.Kind == kind); Assert.NotNull(row, kind); StringAssert.Contains(a.ZoneID, row.PayloadJson); StringAssert.Contains("ditch-cutters", row.PayloadJson);
                }
            } finally { Diag.SetChannel("worldgen", old); }
        }
        [Test]
        public void ExistingLatchcoilWarningKeepsItsExactSourceAndSouthernBypass()
        {
            var z = new Zone(manager.RareEncounters.ViperZoneID); z.AddEntity(scope.Factory.CreateEntity("Hedge"), 12, 6);
            Assert.True(new SpreadRareEncounterBuilder(manager).TryPlace(z, scope.Factory)); Assert.IsEmpty(Clues(z)); Assert.AreEqual(2, Pair(z).Length);
            var snake = Pair(z).Single(e => e.BlueprintName == "SpreadLatchcoil"); var sign = Pair(z).Single(e => e.BlueprintName == "Signpost"); var p = z.GetEntityPosition(snake);
            Assert.AreEqual((p.x - 5, p.y), z.GetEntityPosition(sign)); Assert.AreEqual(3, snake.GetPart<BrainPart>().SightRadius);
            Assert.AreEqual("Old cuts warn of chalk-ring vipers in these hedges and poisonous bites. A scratched arrow points south, around the hedge corner.", sign.GetPart<ExaminablePart>().Description);
        }
        static int Distance((int x, int y) a, (int x, int y) b) => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        static IEnumerable<(int x, int y)> Neighbors((int x, int y) p) { for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) if (x != 0 || y != 0) yield return (p.x + x, p.y + y); }
        static bool Dry(Zone z, (int x, int y) p) { var c = z.GetCell(p.x, p.y); var state = z.TileState.Get(p.x, p.y); return c != null && (state == null || state.IsEmpty) && !c.Occupants.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>()); }
        static bool Clear(Zone z, (int x, int y) p) => Dry(z, p) && !z.GetCell(p.x, p.y).BlocksMovement();
        static bool OutsideSight(Zone z, (int x, int y) p) => Pair(z).All(e => Distance(p, z.GetEntityPosition(e)) > e.GetPart<BrainPart>().SightRadius);
        static bool ReachesEdge(Zone z, (int x, int y) start)
        {
            var q = new Queue<(int x, int y)>(); var seen = new HashSet<(int x, int y)>(); q.Enqueue(start); seen.Add(start);
            while (q.Count > 0) { var p = q.Dequeue(); if (p.x == 0 || p.y == 0 || p.x == Zone.Width - 1 || p.y == Zone.Height - 1) return true;
                foreach (var n in new[] { (p.x - 1, p.y), (p.x + 1, p.y), (p.x, p.y - 1), (p.x, p.y + 1) })
                    if (!seen.Contains(n) && Clear(z, n) && OutsideSight(z, n)) { seen.Add(n); q.Enqueue(n); }
            } return false;
        }
    }
}
