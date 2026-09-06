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
    public class GameAuditAcquisitionBenchTests
    {
        private static IScenario Bench()
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditAcquisitionBench"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Acquisition audit must be a launchable native scenario."); return (IScenario)Activator.CreateInstance(type);
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
        public void Arena_StagesRealGoldAndCapacityBoundarySack()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player");
            var bench = Bench(); bench.Apply(ctx); var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity)); Assert.AreEqual(150, inv.MaxWeight);
            Assert.AreEqual(52, inv.GetCarriedWeight()); Assert.AreEqual("Starapple", inv.Objects.Single().BlueprintName);
            var gold = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "GoldCoin");
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(gold)); Assert.AreEqual(3, gold.GetPart<StackerPart>().StackCount);
            var sack = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "Sack");
            Assert.AreEqual((20, 13), ctx.Zone.GetEntityPosition(sack));
            CollectionAssert.AreEqual(new[] { 99, 1 }, sack.GetPart<ContainerPart>().Contents.Select(e => e.GetPart<StackerPart>().StackCount));
            Assert.AreEqual(0, (int)bench.GetType().GetProperty("Cases").GetValue(bench));
        }
        [Test]
        public void Arena_TraderUsesAuthoredCapacityAndSourceSiblingOrder()
        {
            using var harness = new ScenarioTestHarness(); var ctx = harness.CreateContext(playerBlueprint: "Player"); Bench().Apply(ctx);
            var trader = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "Merchant"); var shelf = trader.GetPart<InventoryPart>();
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(trader)); Assert.AreEqual("Merchant_1", trader.GetPart<ConversationPart>().ConversationID);
            Assert.AreEqual(150, shelf.MaxWeight); Assert.AreEqual(100, shelf.GetCarriedWeight());
            CollectionAssert.AreEqual(new[] { 99, 1 }, shelf.Objects.Select(e => e.GetPart<StackerPart>().StackCount));
            Assert.IsTrue(shelf.Objects.All(e => e.BlueprintName == "Starapple" && e.GetPart<StackerPart>().MaxStack == 99));
        }
        [TestCase("Sack")] [TestCase("Merchant")] [TestCase("GoldCoin")]
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
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "AcquisitionNativeAudit" }).Count);
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
        }
    }
}
