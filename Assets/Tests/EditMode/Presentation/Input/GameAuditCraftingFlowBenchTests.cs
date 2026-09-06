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
    public class GameAuditCraftingFlowBenchTests
    {
        EntityFactory _forge, _still; TurnManager _turns;
        static IScenario Bench()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditCraftingFlowBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Crafting flow needs native controls and a measured performance arena."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp] public void Setup() { _forge = ForgePart.Factory; _still = AlchemyStillPart.Factory; _turns = TurnManager.Active; }
        [TearDown] public void Cleanup()
        { ForgePart.Factory = _forge; AlchemyStillPart.Factory = _still; typeof(TurnManager).GetProperty("Active").GetSetMethod(true).Invoke(null,new object[]{_turns}); Diag.ResetAll(); }
        [Test] public void ArenaUsesActualAlternativeBladesForNativeSelection()
        {
            using var h = new ScenarioTestHarness(); var ctx = h.CreateContext(playerBlueprint:"Player"); var bench = Bench(); bench.Apply(ctx);
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            CollectionAssert.AreEqual(new[]{"SteelBladeComponent","OakHaftComponent","LeatherBindingComponent","IronSpikeComponent","GlimmerBrine","SparkRoot"}, inv.Objects.Select(e=>e.BlueprintName));
            Assert.AreEqual((20,12),ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); Assert.AreEqual(0,inv.GetAllEquipped().Count);
            Assert.IsTrue(inv.Objects.All(e=>!CraftingMarkPart.IsMarked(e))); Assert.AreEqual(0,(int)bench.GetType().GetProperty("Cases").GetValue(bench));
            Assert.AreEqual((21,12),ctx.Zone.GetEntityPosition(ctx.Zone.GetAllEntities().Single(e=>e.BlueprintName=="TinkersForge")));
        }
        [TestCase("SteelBladeComponent")] [TestCase("IronSpikeComponent")] [TestCase("OakHaftComponent")] [TestCase("StoneFloor")] [TestCase("TinkersForge")]
        public void MissingBlueprintRefusesBeforeMutation(string missing)
        {
            using var h = new ScenarioTestHarness(); var ctx=h.CreateContext(playerBlueprint:"Player"); h.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(()=>Bench().Apply(ctx)); Assert.AreEqual((40,12),ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); Assert.AreSame(_forge,ForgePart.Factory);
        }
        [TestCase(false)] [TestCase(true)] public void CheckPreservesDiagnosticPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario",enabled);var b=Bench();b.GetType().GetMethod("Check").Invoke(b,new object[]{"probe",true});
            Assert.AreEqual(enabled,Diag.IsChannelEnabled("scenario"));Assert.AreEqual(1,DiagQuery.Count(new DiagQuery.Filter{Kind="CraftingFlowNativeAudit"}).Count);
        }
    }
}
