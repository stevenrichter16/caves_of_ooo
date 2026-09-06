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
    public class GameAuditActionFeedbackBenchTests
    {
        EntityFactory _forge, _still; TurnManager _turns;
        static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditActionFeedbackBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Action feedback needs real refusal, repair and retry routes."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp] public void Setup() { _forge = ForgePart.Factory; _still = AlchemyStillPart.Factory; _turns = TurnManager.Active; }
        [TearDown] public void Cleanup()
        { ForgePart.Factory = _forge; AlchemyStillPart.Factory = _still; typeof(TurnManager).GetProperty("Active").GetSetMethod(true).Invoke(null,new object[]{_turns}); Diag.ResetAll(); }
        [Test] public void ArenaStagesActualFullSackComponentsAndMissingTinkerBit()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint:"Player"); var bench = Bench(); bench.Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            Entity Field(string name)=>(Entity)bench.GetType().GetField(name).GetValue(bench);
            Assert.AreEqual((20,12),ctx.Zone.GetEntityPosition(ctx.PlayerEntity));Assert.AreEqual((20,12),ctx.Zone.GetEntityPosition(Field("Sack")));
            Assert.AreEqual(6,Field("Sack").GetPart<ContainerPart>().Contents.Count);Assert.AreEqual(6,Field("Sack").GetPart<ContainerPart>().MaxItems);
            Assert.AreEqual(2,Field("CarriedDagger").GetPart<StackerPart>().StackCount);Assert.AreEqual(1,Field("EquippedDagger").GetPart<StackerPart>().StackCount);Assert.IsTrue(InventorySystem.IsEquipped(ctx.PlayerEntity,Field("EquippedDagger")));
            Assert.AreEqual("SilverSand",Field("Filler").BlueprintName);Assert.AreEqual(1,Field("Moss").GetPart<StackerPart>().StackCount);
            var bits=ctx.PlayerEntity.GetPart<BitLockerPart>();Assert.AreEqual(0,bits.GetBitCount('B'));Assert.AreEqual(1,bits.GetBitCount('C'));CollectionAssert.AreEquivalent(new[]{"craft_dagger"},bits.GetKnownRecipes());
            Assert.IsTrue(inv.Objects.All(e=>!CraftingMarkPart.IsMarked(e)));Assert.AreEqual(0,(int)bench.GetType().GetProperty("Cases").GetValue(bench));
            Assert.AreEqual((21,12),ctx.Zone.GetEntityPosition(Field("Station")));
        }
        [TestCase("Sack")] [TestCase("Dagger")] [TestCase("FireMoss")] [TestCase("StoneFloor")] [TestCase("TinkersForge")]
        public void MissingBlueprintRefusesBeforeMutation(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx=h.CreateContext(playerBlueprint:"Player"); h.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(()=>Bench().Apply(ctx)); Assert.AreEqual((40,12),ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); Assert.AreSame(_forge,ForgePart.Factory);
        }
        [TestCase(false)] [TestCase(true)] public void CheckPreservesDiagnosticPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario",enabled);var b=Bench();b.GetType().GetMethod("Check").Invoke(b,new object[]{"probe",true});
            Assert.AreEqual(enabled,Diag.IsChannelEnabled("scenario"));Assert.AreEqual(1,DiagQuery.Count(new DiagQuery.Filter{Kind="ActionFeedbackNativeAudit"}).Count);
        }
    }
}
