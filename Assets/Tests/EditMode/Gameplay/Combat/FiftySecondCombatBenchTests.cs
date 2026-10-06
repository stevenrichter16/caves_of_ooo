using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondCombatBenchTests
    {
        [Test] public void DetachedTacticalCommandsAndReplacementWitnessCompletes()
        {
            var factory=new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            var zone=new Zone("controlled-context");
            var caller=MultiCellAbilityConsumerTests.Owner(creature:true); caller.SetTag("Player");
            Assert.IsTrue(zone.AddEntity(caller,10,10));
            var previousTurns=TurnManager.Active;
            var previousWorld=TurnManager.World;
            var previousFactory=MaterialReactionResolver.Factory;
            var callerTurns=new TurnManager(); callerTurns.AddEntity(caller);
            try
            {
                var bench=new FiftySecondCombatBench();
                bench.Apply(new ScenarioContext(zone,factory,caller,callerTurns));
                string evidence=string.Join("\n",bench.Audit)+"\n"+string.Join("\n",bench.Observations.ConvertAll(observed=>observed?.ToString()??"null"));
                Assert.AreEqual(FiftySecondCombatBench.ExpectedCases,bench.Cases,evidence);
                Assert.AreEqual(0,bench.Failures,evidence); Assert.AreEqual(bench.Cases,bench.Observations.Count,evidence);
                Assert.AreSame(callerTurns,TurnManager.Active);
                Assert.AreSame(previousWorld,TurnManager.World);
                Assert.AreSame(previousFactory,MaterialReactionResolver.Factory);
                Assert.AreSame(zone.GetCell(10,10),zone.GetEntityCell(caller));
                Assert.AreEqual(1000,caller.GetStatValue("Hitpoints"));
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null,previousTurns); }
        }
    }
}
