using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditShortcutBenchTests
    {
        EntityFactory _forge, _still; TurnManager _turns;
        static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditShortcutBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Shortcut audit needs actual ground, container, station and dialogue routes."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp] public void Setup() { _forge = ForgePart.Factory; _still = AlchemyStillPart.Factory; _turns = TurnManager.Active; }
        [TearDown] public void Cleanup()
        { ForgePart.Factory = _forge; AlchemyStillPart.Factory = _still; typeof(TurnManager).GetProperty("Active").GetSetMethod(true).Invoke(null,new object[]{_turns}); Diag.ResetAll(); ConversationManager.EndConversation(); ConversationLoader.Reset(); }
        [Test] public void ArenaStagesRealDroppedItemsAndSameCellCrowdedContainers()
        {
            using var h=new ScenarioTestHarness();var ctx=h.CreateContext(playerBlueprint:"Player");var bench=Bench();bench.Apply(ctx);
            var ground=(System.Collections.Generic.List<Entity>)bench.GetType().GetField("Ground").GetValue(bench);
            var sacks=(System.Collections.Generic.List<Entity>)bench.GetType().GetField("Sacks").GetValue(bench);
            Assert.AreEqual(7,ground.Count);Assert.AreEqual(7,ground.Select(x=>x.BlueprintName).Distinct().Count());
            foreach(var item in ground){Assert.AreEqual((20,12),ctx.Zone.GetEntityPosition(item));Assert.IsFalse(ctx.PlayerEntity.GetPart<InventoryPart>().Contains(item));}
            Assert.AreEqual(7,sacks.Count);foreach(var sack in sacks){Assert.AreEqual((24,12),ctx.Zone.GetEntityPosition(sack));Assert.IsFalse(sack.GetPart<PhysicsPart>().Solid);Assert.AreEqual(1,sack.GetPart<ContainerPart>().Contents.Count);}
            var glimmer=(Entity)bench.GetType().GetField("Glimmer").GetValue(bench);Assert.AreEqual(3,glimmer.GetPart<StackerPart>().StackCount);Assert.IsFalse(CraftingMarkPart.IsMarked(glimmer));
            var speaker=(Entity)bench.GetType().GetField("Speaker").GetValue(bench);Assert.IsTrue(ConversationManager.StartConversation(speaker,ctx.PlayerEntity));Assert.AreEqual(10,ConversationManager.VisibleChoices.Count);ConversationManager.EndConversation();
            Assert.AreEqual(0,(int)bench.GetType().GetProperty("Cases").GetValue(bench));
        }
        [TestCase("Sack")] [TestCase("Dagger")] [TestCase("Villager")] [TestCase("StoneFloor")] [TestCase("AlchemyStill")]
        public void MissingBlueprintRefusesBeforeMutation(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx=h.CreateContext(playerBlueprint:"Player"); h.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(()=>Bench().Apply(ctx)); Assert.AreEqual((40,12),ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); Assert.AreSame(_forge,ForgePart.Factory);
        }
        [TestCase(false)] [TestCase(true)] public void CheckPreservesDiagnosticPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario",enabled);var b=Bench();b.GetType().GetMethod("Check").Invoke(b,new object[]{"probe",true});
            Assert.AreEqual(enabled,Diag.IsChannelEnabled("scenario"));Assert.AreEqual(1,DiagQuery.Count(new DiagQuery.Filter{Kind="ShortcutNativeAudit"}).Count);
        }
    }
}
