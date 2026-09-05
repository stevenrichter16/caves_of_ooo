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
    public class GameAuditHandoverBenchTests
    {
        private static IScenario Bench()
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a =>
                a.GetType("CavesOfOoo.Scenarios.Custom.GameAuditHandoverBench")).FirstOrDefault(t => t != null);
            Assert.NotNull(type, "Handover audit must have a launchable native scenario.");
            return (IScenario)Activator.CreateInstance(type);
        }
        [SetUp]
        public void SetUp()
        {
            ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Conversations/FriendlyNPCs.json")), "FriendlyNPCs.json");
            SettlementRuntime.Reset(); SettlementManager.ResetCurrent(); PlayerReputation.Reset();
        }
        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationActions.Reset();
            ConversationPredicates.Reset(); ConversationActions.Factory = null;
            SettlementRuntime.Reset(); SettlementManager.ResetCurrent(); PlayerReputation.Reset(); Diag.ResetAll();
        }
        [Test]
        public void Arena_StagesActualConversationsFullPackAndUnrepairedOven()
        {
            using var harness = new ScenarioTestHarness();
            var ctx = harness.CreateContext(playerBlueprint: "Player", zoneId: WorldMap.StartingZoneID);
            var bench = Bench(); bench.Apply(ctx);
            Assert.AreEqual((20, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
            var scribe = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "Scribe");
            var farmer = ctx.Zone.GetAllEntities().Single(e => e.BlueprintName == "Farmer");
            Assert.AreEqual((21, 12), ctx.Zone.GetEntityPosition(scribe));
            Assert.AreEqual((20, 13), ctx.Zone.GetEntityPosition(farmer));
            Assert.AreEqual("Scribe_1", scribe.GetPart<ConversationPart>().ConversationID);
            Assert.AreEqual("Farmer_1", farmer.GetPart<ConversationPart>().ConversationID);
            Assert.AreEqual(ctx.Zone.ZoneID, farmer.GetProperty("SettlementId"));
            var inv = ctx.PlayerEntity.GetPart<InventoryPart>();
            Assert.AreEqual(inv.GetCarriedWeight(), inv.MaxWeight);
            Assert.AreEqual(3, inv.Objects.Single(e => e.BlueprintName == "FireClay").GetPart<StackerPart>().StackCount);
            Assert.IsTrue(inv.Objects.Any(e => e.BlueprintName == "MendingRiteGrimoire"));
            Assert.IsTrue(inv.Objects.Any(e => e.BlueprintName == "OvenBuildersGuide"));
            Assert.IsFalse(inv.Objects.Any(e => e.BlueprintName == "GrimoireCopy"));
            Assert.AreEqual(RepairStage.Fouled, SettlementManager.Current.GetSite(ctx.Zone.ZoneID, "VillageOven").Stage);
            Assert.AreEqual(0, (int)bench.GetType().GetProperty("Cases").GetValue(bench));
        }
        [TestCase("Scribe")] [TestCase("Farmer")] [TestCase("FireClay")]
        public void MissingContent_RefusesBeforeChangingArena(string missing)
        {
            using var harness = new ScenarioTestHarness();
            var ctx = harness.CreateContext(playerBlueprint: "Player", zoneId: WorldMap.StartingZoneID);
            harness.Factory.Blueprints.Remove(missing);
            Assert.Throws<InvalidOperationException>(() => Bench().Apply(ctx));
            Assert.AreEqual((40, 12), ctx.Zone.GetEntityPosition(ctx.PlayerEntity));
        }
        [TestCase(false)] [TestCase(true)]
        public void NativeObservation_IsRecordedWithoutChangingChannelPreference(bool enabled)
        {
            Diag.ResetAll(); Diag.SetChannel("scenario", enabled);
            var bench = Bench(); bench.GetType().GetMethod("Check").Invoke(bench, new object[] { "probe", true });
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "HandoverNativeAudit" }).Count);
            Assert.AreEqual(enabled, Diag.IsChannelEnabled("scenario"));
        }
    }
}
