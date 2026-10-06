using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondPreparationBenchTests : FiftySecondPreparationFixture
    {
        [Test] public void DetachedPreparationWitnessCompletesAndPreservesCallerGraph()
        {
            var item = Stock("Dagger"); var before = Pack.Objects.ToArray(); int hp = Actor.GetStatValue("Hitpoints"), tick = Clock.TickCount;
            var oldZone = SettlementRuntime.ActiveZone; var oldSeed = SeedPart.Factory; var oldForge = ForgePart.Factory;
            var bench = new FiftySecondPreparationBench(); bench.Apply(new ScenarioContext(Zone, Factory, Actor, Clock));
            string evidence = string.Join("\n", bench.Audit) + "\n" + string.Join("\n", bench.Observations.Select(o => o?.ToString()));
            Assert.AreEqual(FiftySecondPreparationBench.ExpectedCases, bench.Cases, evidence); Assert.Zero(bench.Failures, evidence);
            Assert.AreEqual(bench.Cases, bench.Observations.Count); CollectionAssert.AreEqual(before, Pack.Objects);
            Assert.AreSame(Actor, item.GetPart<PhysicsPart>().InInventory); Assert.AreSame(Clock, TurnManager.Active); Assert.AreEqual(tick, Clock.TickCount);
            Assert.AreSame(oldZone, SettlementRuntime.ActiveZone); Assert.AreSame(oldSeed, SeedPart.Factory); Assert.AreSame(oldForge, ForgePart.Factory);
            Assert.AreEqual(hp, Actor.GetStatValue("Hitpoints")); Assert.AreSame(Zone.GetCell(10, 10), Zone.GetEntityCell(Actor));
        }
    }
}
