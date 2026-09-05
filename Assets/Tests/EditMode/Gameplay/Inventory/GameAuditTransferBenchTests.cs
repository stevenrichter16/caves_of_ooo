using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditTransferBenchTests
    {
        private static IScenario Bench()
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditTransferBench"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Transfer audit must be a launchable native scenario."); return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp]
        public void SetUp()
        {
            ConversationLoader.Reset(); ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Conversations/FriendlyNPCs.json")), "FriendlyNPCs.json");
            TraderPart.Factory = null; PlayerReputation.Reset();
        }
        [TearDown]
        public void TearDown()
        { ConversationManager.EndConversation(); ConversationLoader.Reset(); TraderPart.Factory = null; PlayerReputation.Reset(); Diag.ResetAll(); }
        [Test]
        public void Arena_StagesRealFullSackAndDistinctEquippedDagger()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player");
            var bench = Bench(); bench.Apply(ctx); var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            var carried = inv.Objects.Single(e => e.BlueprintName == "Dagger"); var equipped = inv.EquippedItems.Values.Distinct().Single();
            Assert.AreNotSame(carried, equipped); Assert.AreEqual(2, carried.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(1, equipped.GetPart<StackerPart>().StackCount);
            var sack = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "Sack");
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(sack)); Assert.IsFalse(sack.GetPart<PhysicsPart>().Solid);
            Assert.AreEqual(6, sack.GetPart<ContainerPart>().Contents.Count); Assert.AreEqual(6, sack.GetPart<ContainerPart>().MaxItems);
            Assert.IsFalse(sack.GetPart<ContainerPart>().Contents.Any(e => e.BlueprintName == "Dagger"));
            Assert.AreEqual(0, (int)bench.GetType().GetProperty("Cases").GetValue(bench));
        }
        [Test]
        public void Arena_TraderUsesAuthoredCapacityAndTwoNaturalAppleStacks()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var trader = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "Merchant"); var shelf = trader.GetPart<InventoryPart>();
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(trader)); Assert.AreEqual("Merchant_1", trader.GetPart<ConversationPart>().ConversationID);
            Assert.AreEqual(150, shelf.MaxWeight); Assert.AreEqual(150, shelf.GetCarriedWeight());
            CollectionAssert.AreEquivalent(new[] { 99, 51 }, shelf.Objects.Select(e => e.GetPart<StackerPart>().StackCount));
            Assert.IsTrue(shelf.Objects.All(e => e.BlueprintName == "Starapple" && e.GetPart<StackerPart>().MaxStack == 99));
        }
        [TestCase("Sack")] [TestCase("Merchant")] [TestCase("Dagger")]
        public void MissingContent_RefusesBeforeChangingArena(string missing)
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); harness.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx)); Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
        [TestCase(false)] [TestCase(true)]
        public void NativeObservation_PreservesChannelPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled); var bench = Bench();
            bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "TransferNativeAudit" }).Count);
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
        }
    }
}
