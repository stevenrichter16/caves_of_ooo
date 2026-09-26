using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityCombatSchedulerBenchTests
    {
        EntityFactory factory;
        TurnManager previousActive, turns;
        ScenarioContext context;
        [SetUp] public void Setup()
        {
            previousActive = TurnManager.Active;
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            var zone = new Zone("scheduler-bench-live-control");
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 20, 10);
            turns = new TurnManager(); turns.AddEntity(player);
            context = new ScenarioContext(zone, factory, player, turns);
        }
        [TearDown] public void Teardown() => typeof(TurnManager).GetProperty("Active").SetValue(null, previousActive);

        [Test] public void OrdinaryFactoryActorsCompleteSchedulerCasesWithoutChangingLiveContext()
        {
            var originalFactory = LoadoutPart.Factory; var originalRng = LoadoutPart.Rng;
            var originalWorld = TurnManager.World;
            var messages = MessageLog.GetAllEntries(); var announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            int hp = context.PlayerEntity.GetStatValue("Hitpoints"); int tick = turns.TickCount;
            var bench = new DensityCombatSchedulerBench(); bench.Apply(context);
            Assert.Zero(bench.Failures, string.Join("\n", bench.Audit));
            Assert.AreEqual(7, bench.Cases); Assert.AreEqual(7, bench.Audit.Count);
            Assert.IsTrue(bench.Audit.All(s => s.StartsWith("PASS ")));
            Assert.IsTrue(bench.Audit.Any(s => s.Contains("wall_control")));
            Assert.IsTrue(bench.Audit.Any(s => s.Contains("injured_retreat")));
            Assert.AreSame(turns, TurnManager.Active); Assert.AreSame(originalWorld, TurnManager.World);
            Assert.AreSame(originalFactory, LoadoutPart.Factory); Assert.AreSame(originalRng, LoadoutPart.Rng);
            Assert.AreEqual(hp, context.PlayerEntity.GetStatValue("Hitpoints")); Assert.AreEqual(tick, turns.TickCount);
            Assert.AreEqual((20, 10), context.Zone.GetEntityPosition(context.PlayerEntity));
            Assert.AreEqual(1, context.Zone.GetAllEntities().Count);
            CollectionAssert.AreEqual(messages.Select(m => m.Text), MessageLog.GetAllEntries().Select(m => m.Text));
            CollectionAssert.AreEqual(announcements, MessageLog.GetPendingAnnouncementsSnapshot());
        }
        [Test] public void RepeatRunDoesNotAccumulateCasesOrAdvanceLiveScheduler()
        {
            var bench = new DensityCombatSchedulerBench(); bench.Apply(context); string first = bench.RunId;
            bench.Apply(context); Assert.AreNotEqual(first, bench.RunId);
            Assert.AreEqual(7, bench.Cases); Assert.Zero(bench.Failures, string.Join("\n", bench.Audit));
            Assert.AreEqual(0, turns.GetEnergy(context.PlayerEntity)); Assert.AreEqual(0, turns.TickCount);
        }
        [Test] public void PendingLiveEffectsAndMessageCallbacksSurviveDetachedAudit()
        {
            var queue = (Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var oldAscii = AsciiFxBus.Drain(); var oldSpells = SpellFxBus.Drain();
            var callback = MessageLog.OnMessage; var tickProvider = MessageLog.TickProvider;
            try
            {
                AsciiFxBus.EmitParticle(context.Zone, 20, 10, '!', "&R", .25f);
                var expectedAscii = queue.ToArray();
                var spell = new SpellFxSequence("sentinel", context.Zone, context.PlayerEntity, new Point(20, 10));
                SpellFxBus.Emit(spell); int clearVersion = AsciiFxBus.ClearVersion;
                int callbackCount = 0; Action<string> sentinel = _ => callbackCount++;
                MessageLog.OnMessage = sentinel;
                var bench = new DensityCombatSchedulerBench(); bench.Apply(context);
                Assert.Zero(bench.Failures, string.Join("\n", bench.Audit));
                Assert.Zero(callbackCount); Assert.AreSame(sentinel, MessageLog.OnMessage);
                Assert.AreSame(tickProvider, MessageLog.TickProvider); Assert.AreEqual(clearVersion, AsciiFxBus.ClearVersion);
                CollectionAssert.AreEqual(expectedAscii, queue.ToArray());
                CollectionAssert.AreEqual(new[] { spell }, SpellFxBus.Drain());
            }
            finally
            {
                foreach (var request in AsciiFxBus.Drain()) AsciiFxBus.Release(request);
                foreach (var request in oldAscii) queue.Enqueue(request);
                SpellFxBus.Clear(); foreach (var spell in oldSpells) SpellFxBus.Emit(spell);
                MessageLog.OnMessage = callback; MessageLog.TickProvider = tickProvider;
            }
        }
        [Test] public void MissingBlueprintFailsExplicitlyAndRestoresBorrowedGlobals()
        {
            var emptyFactory = new EntityFactory();
            var invalid = new ScenarioContext(context.Zone, emptyFactory, context.PlayerEntity, turns);
            var originalFactory = LoadoutPart.Factory; var originalRng = LoadoutPart.Rng; var originalWorld = TurnManager.World;
            var bench = new DensityCombatSchedulerBench();
            Assert.Throws<InvalidOperationException>(() => bench.Apply(invalid));
            Assert.Greater(bench.Failures, 0); Assert.Zero(bench.Cases);
            Assert.AreSame(turns, TurnManager.Active); Assert.AreSame(originalWorld, TurnManager.World);
            Assert.AreSame(originalFactory, LoadoutPart.Factory); Assert.AreSame(originalRng, LoadoutPart.Rng);
        }
    }
}
