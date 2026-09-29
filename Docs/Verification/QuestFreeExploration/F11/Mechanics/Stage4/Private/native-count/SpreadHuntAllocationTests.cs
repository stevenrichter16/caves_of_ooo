using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Profiling;
#endif
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadHuntAllocationTests
    {
        SpreadExplorationActorTests.Scope scope;
        readonly Dictionary<string, bool> channels = new Dictionary<string, bool>();
        EntityFactory factory;
        Entity hunter, prey, player;
        Zone zone;
        SpreadPredatorPart role;
        SpreadGrazerPart grazer;
        BrainPart brain;
        Func<BrainPart, Zone, bool> idle;
        static object allocationSink;

        [SetUp] public void Setup()
        {
            scope = new SpreadExplorationActorTests.Scope();
            channels.Clear();
            var defaults = (string[])typeof(Diag).GetField("DefaultOnCategories", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var name in defaults.Concat(new[] { "ai", "turn-verbose" }).Distinct())
            { channels[name] = Diag.IsChannelEnabled(name); Diag.SetChannel(name, false); }
            EntityVisualHooks.Reset();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Factions.json")));
            factory = new EntityFactory();
            string candidate = Environment.GetEnvironmentVariable("COO_FURROWSTALKER_BLUEPRINTS");
            factory.LoadBlueprints(File.ReadAllText(string.IsNullOrEmpty(candidate) ? Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json") : candidate));
            zone = new Zone("Overworld.8.8.0");
            hunter = Place("Furrowstalker", 14, 6); prey = Place("ReedbackGrazer", 14, 10); player = Place("Player", 65, 20);
            Bind(); Assert.True(role.Configure(zone, prey)); ResetPair();
        }
        [TearDown] public void Cleanup()
        { foreach (var channel in channels) Diag.SetChannel(channel.Key, channel.Value); scope.Dispose(); allocationSink = null; }
        Entity Place(string blueprint, int x, int y)
        {
            var e = factory.CreateEntity(blueprint); Assert.NotNull(e, blueprint); Assert.True(zone.AddEntity(e, x, y));
            var b = e.GetPart<BrainPart>(); if (b != null) { b.CurrentZone = zone; b.Rng = new Random(1); }
            return e;
        }
        void Bind()
        {
            role = hunter.GetPart<SpreadPredatorPart>(); grazer = prey.GetPart<SpreadGrazerPart>(); brain = prey.GetPart<BrainPart>();
            idle = IdleFor(grazer); brain.CurrentZone = zone; hunter.GetPart<BrainPart>().CurrentZone = zone;
        }
        static Func<BrainPart, Zone, bool> IdleFor(SpreadGrazerPart g) => (Func<BrainPart, Zone, bool>)typeof(SpreadGrazerPart)
            .GetMethod("TakeIdleAction", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate(typeof(Func<BrainPart, Zone, bool>), g);
        void ResetPair()
        {
            Assert.True(zone.MoveEntity(hunter, 14, 8)); Assert.True(zone.MoveEntity(prey, 14, 10));
            grazer.Hunter = hunter; grazer.FlightRemaining = 0; brain.ClearGoals(); hunter.GetPart<BrainPart>().ClearGoals();
            role.Prey = prey; role.PreyID = prey.ID; role.Phase = SpreadHuntPhase.Watching; role.HasLastSeen = false;
            role.PursuitRemaining = 24; role.SearchRemaining = 6;
        }
        static WanderRandomlyGoal Pacing(BrainPart b)
        { var root = new BoredGoal(); b.PushGoal(root); var duration = new WanderDurationGoal(20); root.PushChildGoal(duration); var step = new WanderRandomlyGoal(); duration.PushChildGoal(step); return step; }
        void Act() { Assert.True(idle(brain, zone)); }
        string State() => zone.GetEntityPosition(prey) + "|" + grazer.FlightRemaining + "|" + grazer.ThreatX + "," + grazer.ThreatY + "|" + prey.GetPart<RenderPart>().VisualFacing + "|" + prey.GetStatValue("Hitpoints");

        // Unity's Mono returns zero from GetAllocatedBytesForCurrentThread on
        // this editor. Its GC.Alloc marker records allocation EVENTS with timing
        // values, not byte sizes. Never interpret either native value as bytes.
#if UNITY_EDITOR
        const string AllocationUnit = "native GC.Alloc events";
        const long AllocationLimit = 4;
        static long MeasureAllocation(Action action)
        {
            const int capacity = 128;
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", capacity,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                action(); recorder.Stop();
                if (!recorder.Valid)
                    Assert.Inconclusive("Native current-thread GC.Alloc event counter unavailable; this is not a zero-allocation pass.");
                Assert.False(recorder.WrappedAround, "Native allocation samples overflowed; do not report a truncated low count.");
                Assert.Less(recorder.Count, capacity, "Native allocation samples filled their capacity; count may be truncated.");
                long events = 0;
                for (int i = 0; i < recorder.Count; i++) events += recorder.GetSample(i).Count;
                return events;
            }
        }
#else
        const string AllocationUnit = "private managed-runtime bytes";
        const long AllocationLimit = 512;
        static long MeasureAllocation(Action action)
        { long before = GC.GetAllocatedBytesForCurrentThread(); action(); return GC.GetAllocatedBytesForCurrentThread() - before; }
#endif
        static void RequireCalibratedCounter()
        {
            var arrays = new byte[5][];
            Action empty = () => { };
            Action single = () => allocationSink = new byte[1024];
            Action multiple = () => { for (int i = 0; i < arrays.Length; i++) arrays[i] = new byte[256]; allocationSink = arrays; };
            MeasureAllocation(single); // Runtime/counter initialization is outside calibration.
            long zero = MeasureAllocation(empty), one = MeasureAllocation(single), five = MeasureAllocation(multiple);
            TestContext.WriteLine(AllocationUnit + ": empty=" + zero + ", single=" + one + ", multiple=" + five);
#if UNITY_EDITOR
            if (zero != 0 || one != 1 || five != 5)
                Assert.Inconclusive("Native GC.Alloc events failed empty/single/multiple calibration; not a byte measurement or a zero-allocation pass.");
#else
            if (zero != 0 || one < 1024 || five < 1280)
                Assert.Inconclusive("Managed byte counter failed positive calibration; never accept a vacuous zero-byte pass.");
#endif
        }
        [Test] public void AllocationCounterCalibratesEmptySingleAndMultipleAllocations()
        { RequireCalibratedCounter(); }
        [TestCase(false)] [TestCase(true)]
        public void WarmedFlightStepAvoidsRecurringSearchAllocations(bool pacing)
        {
            RequireCalibratedCounter(); // Each budget case independently refuses unsupported counters.
            WanderRandomlyGoal step = null;
            Action prepare = () => { ResetPair(); if (pacing) step = Pacing(brain); };
            Action action = pacing ? (Action)(() => step.TakeAction()) : () => idle(brain, zone);
            for (int i = 0; i < 64; i++) { prepare(); action(); }
            long worst = 0;
            for (int i = 0; i < 64; i++)
            {
                prepare(); long amount = MeasureAllocation(action); worst = Math.Max(worst, amount);
                Assert.AreEqual((13, 11), zone.GetEntityPosition(prey), "The measured call must actually perform the same flight step.");
                Assert.AreEqual(2, grazer.FlightRemaining);
            }
            TestContext.WriteLine(AllocationUnit + ": pacing=" + pacing + ", worst=" + worst + ", limit=" + AllocationLimit);
            Assert.LessOrEqual(worst, AllocationLimit, "Warmed actual flight must not recreate its bounded search collections per action. " + AllocationUnit + "; diagnostics and setup are outside the measured call.");
        }
        [TestCase(false)] [TestCase(true)]
        public void ReusedSearchPreservesRouteAndRespondsToChangedBlocker(bool blocked)
        {
            Act(); Assert.AreEqual((13, 11), zone.GetEntityPosition(prey)); ResetPair();
            if (blocked) Place("Tree", 13, 11);
            Act(); Assert.AreEqual(blocked ? (14, 11) : (13, 11), zone.GetEntityPosition(prey));
            Assert.AreEqual(2, grazer.FlightRemaining); Assert.AreEqual(10, prey.GetStatValue("Hitpoints"));
        }
        [TestCase(false)] [TestCase(true)]
        public void HiddenHunterPositionDoesNotChangeRememberedFlightAfterWarmOrSave(bool save)
        {
            Act(); ResetPair(); for (int x = 0; x < Zone.Width; x++) Place("Tree", x, 9);
            string[] Trace(int hx)
            {
                ResetPair(); Assert.True(zone.MoveEntity(hunter, hx, 8));
                grazer.FlightRemaining = 2; grazer.ThreatX = 14; grazer.ThreatY = 8;
                Assert.False(AIHelpers.HasLineOfSight(zone, hx, 8, 14, 10)); if (save) RoundTrip();
                var states = new List<string>(); for (int i = 0; i < 3; i++) { Act(); states.Add(State()); }
                return states.ToArray();
            }
            var near = Trace(14); var farther = Trace(20); CollectionAssert.AreEqual(near, farther);
            Assert.AreEqual(0, grazer.FlightRemaining); Assert.AreEqual(14, grazer.ThreatX); Assert.AreEqual(8, grazer.ThreatY);
        }
        [Test] public void TwoGrazersKeepIndependentSearchAndState()
        {
            var h2 = Place("Furrowstalker", 50, 6); var p2 = Place("ReedbackGrazer", 50, 10);
            Assert.True(h2.GetPart<SpreadPredatorPart>().Configure(zone, p2)); Assert.True(zone.MoveEntity(h2, 50, 8));
            var g2 = p2.GetPart<SpreadGrazerPart>(); var idle2 = IdleFor(g2);
            Act(); string first = State(); Assert.True(idle2(p2.GetPart<BrainPart>(), zone));
            Assert.AreEqual((49, 11), zone.GetEntityPosition(p2)); Assert.AreEqual(first, State());
            ResetPair(); Act(); Assert.AreEqual(first, State()); Assert.AreEqual(2, g2.FlightRemaining); Assert.AreSame(h2, g2.Hunter);
        }
        [TestCase(false)] [TestCase(true)]
        public void ReplacementSaveKeepsStateAndFirstStepIndependentOfWarmBuffers(bool warm)
        {
            if (warm) Act(); ResetPair(); string prior = State(); var old = grazer; RoundTrip();
            Assert.AreNotSame(old, grazer); Assert.AreEqual(prior, State()); Assert.AreSame(hunter, grazer.Hunter); Assert.AreSame(prey, role.Prey);
            Act(); Assert.AreEqual((13, 11), zone.GetEntityPosition(prey)); Assert.AreEqual(2, grazer.FlightRemaining);
        }
        void RoundTrip()
        {
            string hi = hunter.ID, pi = prey.ID; var manager = OverworldZoneManager.CreateDetached(factory, 64); manager.SetActiveZone(zone);
            var state = GameSessionState.Capture("hunt-allocation", "prototype", manager, new TurnManager(), player);
            using (var bytes = new MemoryStream())
            { state.Save(new SaveWriter(bytes)); bytes.Position = 0; var loaded = GameSessionState.Load(new SaveReader(bytes, factory)); zone = loaded.ZoneManager.ActiveZone; player = loaded.Player; }
            hunter = zone.GetReadOnlyEntities().Single(e => e.ID == hi); prey = zone.GetReadOnlyEntities().Single(e => e.ID == pi); Bind();
        }
        [TestCase(false)] [TestCase(true)]
        public void MovementCallbackReentryMatchesSequentialSearch(bool reenter)
        {
            Act(); Act(); string expected = State(); ResetPair(); int moves = 0; bool inCallback = false;
            EntityVisualHooks.MovedCallback = (e, z, ox, oy, nx, ny, forced) =>
            {
                if (e != prey) return; moves++;
                if (reenter && !inCallback) { inCallback = true; Act(); }
            };
            Act(); if (!reenter) Act();
            Assert.AreEqual(2, moves); Assert.AreEqual(expected, State());
        }
        [TestCase(false)] [TestCase(true)]
        public void IndexedPacingAdmissionStillRequiresParentOnCurrentStack(bool detached)
        {
            var step = Pacing(brain); if (detached) step.ParentHandler = new WanderDurationGoal(20);
            var method = typeof(SpreadGrazerPart).GetMethod("TryWanderFlight", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(!detached, method.Invoke(grazer, new object[] { brain, zone, step }));
            Assert.AreEqual(detached ? (14, 10) : (13, 11), zone.GetEntityPosition(prey));
        }
    }
}
