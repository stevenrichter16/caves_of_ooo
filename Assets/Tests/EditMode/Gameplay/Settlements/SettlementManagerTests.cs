using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class SettlementManagerTests
    {
        private const string SettlementId = SettlementSiteDefinitions.StartingVillageZoneId;
        private int _turn;
        private SettlementManager _manager;

        [SetUp]
        public void SetUp()
        {
            _turn = 0;
            MessageLog.Clear();
            _manager = new SettlementManager(() => _turn, _ => new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));
        }

        [TearDown]
        public void TearDown()
        {
            SettlementManager.ResetCurrent();
            SettlementRuntime.Reset();
        }

        [Test]
        public void PurifySpell_SetsTemporaryStage_AndRelapsesLater()
        {
            var player = CreatePlayer();
            player.Properties[SettlementSiteDefinitions.StartingVillageKnowledgeProperty] = "true";
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));

            bool applied = _manager.ApplyRepairMethod(SettlementId, SettlementSiteDefinitions.MainWellSiteId, RepairMethodId.PurifySpell, player);

            Assert.IsTrue(applied);
            Assert.AreEqual(RepairStage.TemporarilyPurified, _manager.GetSite(SettlementId, SettlementSiteDefinitions.MainWellSiteId).Stage);

            _turn += SettlementRepairDefinitions.PurifyRelapseTurns;
            bool changed = _manager.AdvanceSettlement(SettlementId, _turn);

            Assert.IsTrue(changed);
            Assert.AreEqual(RepairStage.Fouled, _manager.GetSite(SettlementId, SettlementSiteDefinitions.MainWellSiteId).Stage);
            Assert.IsNotEmpty(_manager.ConsumePendingMessages(SettlementId));
        }

        [Test]
        public void ManualRepair_RequiresManualAndSand_ConsumesOnlySand()
        {
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            inventory.AddObject(CreateItem(SettlementRepairDefinitions.WellMaintenanceManualBlueprint));
            inventory.AddObject(CreateItem(SettlementRepairDefinitions.SilverSandBlueprint));
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));

            bool applied = _manager.ApplyRepairMethod(SettlementId, SettlementSiteDefinitions.MainWellSiteId, RepairMethodId.ManualRepair, player);

            Assert.IsTrue(applied);
            Assert.AreEqual(RepairStage.StableRepair, _manager.GetSite(SettlementId, SettlementSiteDefinitions.MainWellSiteId).Stage);
            Assert.IsTrue(HasInventoryItem(player, SettlementRepairDefinitions.WellMaintenanceManualBlueprint));
            Assert.IsFalse(HasInventoryItem(player, SettlementRepairDefinitions.SilverSandBlueprint));
        }

        [Test]
        public void TeachCaretaker_RequiresStableRepair_AndSetsImprovedCondition()
        {
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            inventory.AddObject(CreateItem(SettlementRepairDefinitions.WellMaintenanceManualBlueprint));
            inventory.AddObject(CreateItem(SettlementRepairDefinitions.SilverSandBlueprint));
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));

            Assert.IsFalse(_manager.ApplyRepairMethod(SettlementId, SettlementSiteDefinitions.MainWellSiteId, RepairMethodId.TeachCaretaker, player));
            Assert.IsTrue(_manager.ApplyRepairMethod(SettlementId, SettlementSiteDefinitions.MainWellSiteId, RepairMethodId.ManualRepair, player));
            Assert.IsTrue(_manager.ApplyRepairMethod(SettlementId, SettlementSiteDefinitions.MainWellSiteId, RepairMethodId.TeachCaretaker, player));
            Assert.AreEqual(RepairStage.ImprovedWithCaretaker, _manager.GetSite(SettlementId, SettlementSiteDefinitions.MainWellSiteId).Stage);
            Assert.IsTrue(_manager.HasCondition(SettlementId, SettlementSiteDefinitions.ImprovedWellCondition));
        }

        private static Entity CreatePlayer()
        {
            var entity = new Entity { BlueprintName = "Player" };
            entity.AddPart(new InventoryPart());
            return entity;
        }

        private static Entity CreateItem(string blueprintName)
        {
            var entity = new Entity { BlueprintName = blueprintName };
            entity.AddPart(new RenderPart { DisplayName = blueprintName });
            entity.AddPart(new PhysicsPart { Takeable = true });
            return entity;
        }

        private static bool HasInventoryItem(Entity player, string blueprintName)
        {
            var inventory = player.GetPart<InventoryPart>();
            if (inventory == null)
                return false;

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                if (inventory.Objects[i].BlueprintName == blueprintName)
                    return true;
            }

            return false;
        }
    }
}
