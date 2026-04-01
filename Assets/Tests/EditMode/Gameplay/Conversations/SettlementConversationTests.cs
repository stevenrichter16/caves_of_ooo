using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class SettlementConversationTests
    {
        private const string SettlementId = SettlementSiteDefinitions.StartingVillageZoneId;
        private SettlementManager _manager;
        private Entity _speaker;
        private Entity _listener;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            ConversationPredicates.Reset();
            ConversationActions.Reset();
            SettlementRuntime.Reset();

            _manager = new SettlementManager(() => 0, _ => new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));

            _speaker = new Entity { BlueprintName = "WellKeeper" };
            _speaker.Properties["SettlementId"] = SettlementId;
            _listener = new Entity { BlueprintName = "Player" };
            _listener.AddPart(new InventoryPart());
        }

        [TearDown]
        public void TearDown()
        {
            SettlementManager.ResetCurrent();
            SettlementRuntime.Reset();
            ConversationPredicates.Reset();
            ConversationActions.Reset();
        }

        [Test]
        public void IfSettlementSiteStage_UsesSpeakerSettlementContext()
        {
            bool fouled = ConversationPredicates.Evaluate("IfSettlementSiteStage", _speaker, _listener, "MainWell:Fouled");
            bool stable = ConversationPredicates.Evaluate("IfSettlementSiteStage", _speaker, _listener, "MainWell:StableRepair");

            Assert.IsTrue(fouled);
            Assert.IsFalse(stable);
        }

        [Test]
        public void ResolveSettlementSite_AppliesRepairMethodThroughDialogueAction()
        {
            _listener.Properties[SettlementSiteDefinitions.StartingVillageKnowledgeProperty] = "true";

            ConversationActions.Execute("ResolveSettlementSite", _speaker, _listener, "MainWell:PurifySpell");

            Assert.AreEqual(RepairStage.TemporarilyPurified, _manager.GetSite(SettlementId, SettlementSiteDefinitions.MainWellSiteId).Stage);
        }
    }
}
