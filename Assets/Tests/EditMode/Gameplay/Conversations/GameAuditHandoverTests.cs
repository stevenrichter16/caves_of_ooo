using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A17/A18: real authored handovers spend one unit and must succeed before dialogue advances.</summary>
    public class GameAuditHandoverTests
    {
        private const string SettlementId = SettlementSiteDefinitions.StartingVillageZoneId;
        private EntityFactory _factory;
        private Entity _player;
        private Zone _zone;
        private SettlementManager _manager;
        private int _changes, _dirty;
        private readonly List<string> _messages = new List<string>();
        private InventoryPart Inventory => _player.GetPart<InventoryPart>();

        [OneTimeSetUp]
        public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize(); PlayerReputation.Reset();
            ConversationManager.EndConversation(); ConversationLoader.Reset();
            ConversationActions.Reset(); ConversationPredicates.Reset(); SettlementRuntime.Reset();
            ConversationActions.Factory = _factory;
            foreach (string file in new[] { "FriendlyNPCs.json", "Wardens.json" })
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                    "Resources/Content/Conversations", file)), file);
            _player = new Entity { BlueprintName = "AuditHandoverPlayer" };
            _player.AddPart(new InventoryPart()); _player.AddPart(new PhysicsPart());
            _zone = new Zone(SettlementId); _zone.AddEntity(_player, 10, 10);
            SettlementRuntime.ActiveZone = _zone;
            _manager = new SettlementManager(() => 17, _ => new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
            _changes = _dirty = 0;
            _manager.SiteStateChanged += (_, __) => _changes++;
            SettlementRuntime.ZoneDirtyCallback = () => _dirty++;
            MessageLog.Clear(); _messages.Clear(); MessageLog.OnMessage = m => _messages.Add(m);
        }
        [TearDown]
        public void TearDown()
        {
            MessageLog.OnMessage = null;
            ConversationManager.EndConversation(); ConversationLoader.Reset();
            ConversationActions.Reset(); ConversationPredicates.Reset(); ConversationActions.Factory = null;
            SettlementRuntime.Reset(); SettlementManager.ResetCurrent(); PlayerReputation.Reset(); FactionManager.Reset();
        }
        private Entity Carry(string bp, int count = 1)
        {
            var item = _factory.CreateEntity(bp); Assert.NotNull(item, bp);
            if (count != 1) { Assert.NotNull(item.GetPart<StackerPart>(), bp + " must really stack"); item.GetPart<StackerPart>().StackCount = count; }
            Assert.IsTrue(Inventory.AddObject(item)); return item;
        }
        private Entity Speaker(string conversation)
        {
            var npc = new Entity(); npc.AddPart(new ConversationPart { ConversationID = conversation });
            npc.Properties["SettlementId"] = SettlementId; _zone.AddEntity(npc, 11, 10); return npc;
        }
        private static void ChooseTarget(string target)
        {
            int index = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == target);
            Assert.GreaterOrEqual(index, 0, "Actual authored choice must be visible: " + target);
            ConversationManager.SelectChoice(index);
        }
        private void SpentOne(Entity item, int count)
        {
            Assert.AreEqual(count > 1, Inventory.Objects.Contains(item));
            Assert.AreEqual(count > 1 ? count - 1 : 1, item.GetPart<StackerPart>().StackCount);
            if (count > 1) Assert.AreSame(_player, item.GetPart<PhysicsPart>().InInventory);
            else Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
            Assert.IsFalse(_messages.Any(m => m.StartsWith("You hand over ") && m.Contains("(x")),
                "Handover prose describes one unit, not the remaining stack.");
        }

        [TestCase("MainWell", RepairMethodId.ManualRepair, "WellMaintenanceManual", "SilverSand", 1)]
        [TestCase("MainWell", RepairMethodId.ManualRepair, "WellMaintenanceManual", "SilverSand", 3)]
        [TestCase("VillageOven", RepairMethodId.OvenRebuild, "OvenBuildersGuide", "FireClay", 1)]
        [TestCase("VillageOven", RepairMethodId.OvenRebuild, "OvenBuildersGuide", "FireClay", 3)]
        [TestCase("VillageLantern", RepairMethodId.LanternReforge, "LanternOilRecipe", "WardOil", 1)]
        [TestCase("VillageLantern", RepairMethodId.LanternReforge, "LanternOilRecipe", "WardOil", 3)]
        public void ActualRepair_SpendsOneMaterialAndRetainsManual(string site, RepairMethodId method, string manual, string material, int count)
        {
            Entity book = Carry(manual), item = Carry(material, count);
            Assert.IsTrue(_manager.ApplyRepairMethod(SettlementId, site, method, _player));
            Assert.AreEqual(RepairStage.StableRepair, _manager.GetSite(SettlementId, site).Stage);
            Assert.AreEqual(1, _changes); Assert.IsTrue(Inventory.Objects.Contains(book)); SpentOne(item, count);
        }

        [TestCase("TakeItem", "SilverSand", "SilverSand", 1)]
        [TestCase("TakeItem", "SilverSand", "SilverSand", 3)]
        [TestCase("TakeItemWithTag", "GrimoireCopy", "GrimoireCopy", 1)]
        [TestCase("TakeItemWithTag", "GrimoireCopy", "GrimoireCopy", 3)]
        public void ConversationDonation_SpendsOne(string action, string bp, string argument, int count)
        {
            var item = Carry(bp, count);
            ConversationActions.Execute(action, null, _player, argument); SpentOne(item, count);
        }

        [TestCase("Farmer_1", "VillageOven", "OvenRebuild", "AfterOvenRebuild", "OvenBuildersGuide", "FireClay", false)]
        [TestCase("Farmer_1", "VillageOven", "OvenRebuild", "AfterOvenRebuild", "OvenBuildersGuide", "FireClay", true)]
        [TestCase("Warden_Lantern_1", "VillageLantern", "LanternReforge", "AfterReforge", "LanternOilRecipe", "WardOil", false)]
        [TestCase("Warden_Lantern_1", "VillageLantern", "LanternReforge", "AfterReforge", "LanternOilRecipe", "WardOil", true)]
        public void StaleRepairConfirmation_DoesNotEnterSuccessNode(string conversation, string site, string confirmation, string success, string manual, string material, bool removeMaterial)
        {
            Carry(manual); var item = Carry(material, 3);
            Assert.IsTrue(ConversationManager.StartConversation(Speaker(conversation), _player));
            ChooseTarget(confirmation);
            if (removeMaterial) Inventory.RemoveObject(item);
            else _manager.GetSite(SettlementId, site).Stage = RepairStage.StableRepair;
            ChooseTarget(success);
            Assert.AreEqual(confirmation, ConversationManager.CurrentNode.ID,
                "Required repair refusal must not enter the congratulatory/reward node.");
            Assert.AreEqual(0, _changes); Assert.AreEqual(0, _dirty);
            Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
        }

        [TestCase("Farmer_1", "VillageOven", "TeachBaker", "AfterTeachBaker", 1)]
        [TestCase("Farmer_1", "VillageOven", "TeachBaker", "AfterTeachBaker", 3)]
        [TestCase("Warden_Lantern_1", "VillageLantern", "TeachWarden", "AfterTeachWarden", 1)]
        [TestCase("Warden_Lantern_1", "VillageLantern", "TeachWarden", "AfterTeachWarden", 3)]
        public void AuthoredTeaching_ImprovesAndSpendsExactlyOneCopy(string conversation, string site, string confirmation, string success, int count)
        {
            _manager.GetSite(SettlementId, site).Stage = RepairStage.StableRepair;
            var copy = Carry("GrimoireCopy", count);
            Assert.IsTrue(ConversationManager.StartConversation(Speaker(conversation), _player));
            ChooseTarget(confirmation); ChooseTarget(success);
            Assert.AreEqual(success, ConversationManager.CurrentNode.ID);
            Assert.AreEqual(RepairStage.ImprovedWithCaretaker, _manager.GetSite(SettlementId, site).Stage);
            Assert.AreEqual(1, _changes); Assert.AreEqual(1, _dirty); SpentOne(copy, count);
        }

        [TestCase("Farmer_1", "VillageOven", "TeachBaker", "AfterTeachBaker")]
        [TestCase("Warden_Lantern_1", "VillageLantern", "TeachWarden", "AfterTeachWarden")]
        public void MissingCopyAfterConfirmation_ChangesNeitherSiteNorSuccessNode(string conversation, string site, string confirmation, string success)
        {
            _manager.GetSite(SettlementId, site).Stage = RepairStage.StableRepair;
            var copy = Carry("GrimoireCopy");
            Assert.IsTrue(ConversationManager.StartConversation(Speaker(conversation), _player));
            ChooseTarget(confirmation); Inventory.RemoveObject(copy); ChooseTarget(success);
            Assert.AreEqual(RepairStage.StableRepair, _manager.GetSite(SettlementId, site).Stage);
            Assert.AreEqual(confirmation, ConversationManager.CurrentNode.ID);
            Assert.AreEqual(0, _changes); Assert.AreEqual(0, _dirty);
        }

        [TestCase(false, true)] [TestCase(true, true)] [TestCase(true, false)]
        public void AuthoredScribeCopy_ReportsOnlyActualDelivery(bool full, bool hasGround)
        {
            var original = Carry("WateringGrimoire");
            if (full) Inventory.MaxWeight = Inventory.GetCarriedWeight();
            Assert.IsTrue(ConversationManager.StartConversation(Speaker("Scribe_1"), _player));
            ChooseTarget("CopyConfirm");
            if (!hasGround) SettlementRuntime.ActiveZone = null;
            ChooseTarget("AfterCopy");
            bool allowed = !full || hasGround;
            var copies = Inventory.Objects.Concat(_zone.GetAllEntities()).Where(e => e.BlueprintName == "GrimoireCopy").ToList();
            Assert.AreEqual(allowed ? 1 : 0, copies.Count);
            Assert.AreEqual(allowed ? "AfterCopy" : "CopyConfirm", ConversationManager.CurrentNode.ID);
            Assert.IsTrue(Inventory.Objects.Contains(original));
            if (allowed)
            {
                Assert.AreEqual("Hydromancy_ConjureRain", copies[0].GetPart<GrimoirePart>().SkillClassName);
                Assert.AreEqual(full, _zone.GetEntityCell(copies[0]) != null);
                if (full) Assert.IsTrue(_messages.Any(m => m.Contains("at your feet")));
            }
            else Assert.IsFalse(_messages.Any(m => m.Contains("You receive")));
        }
        [TestCase("IfHaveItem", "SilverSand", "SilverSand", 0)]
        [TestCase("IfHaveItem", "SilverSand", "SilverSand", 1)]
        [TestCase("IfHaveItemWithTag", "GrimoireCopy", "GrimoireCopy", 0)]
        [TestCase("IfHaveItemWithTag", "GrimoireCopy", "GrimoireCopy", 1)]
        public void ItemPredicates_RequireAPositiveCarriedUnit(string predicate, string bp, string argument, int count)
        {
            var item = Carry(bp); item.GetPart<StackerPart>().StackCount = count;
            Assert.AreEqual(count > 0, ConversationPredicates.Evaluate(predicate, null, _player, argument));
        }

        [TestCase("CurationSorter_1", "Carriage", "BogBodyCourier", "SealedBogTakenBody", false)]
        [TestCase("CurationSorter_1", "Carriage", "BogBodyCourier", "SealedBogTakenBody", true)]
        [TestCase("ConcordFactor_1", "Contract", "PruningContract", "PruningWrit", false)]
        [TestCase("ConcordFactor_1", "Contract", "PruningContract", "PruningWrit", true)]
        public void AuthoredQuestOffer_RequiresActualCargoDeliveryBeforeStarting(string conversation, string confirmation, string quest, string cargo, bool ground)
        {
            StoryletRegistry.Reset(); StoryletPart.Current = new StoryletPart();
            NarrativeStatePart.Current = new NarrativeStatePart();
            try
            {
                StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                    "Resources/Content/Data/Storylets", quest + ".json")));
                Inventory.MaxWeight = 0;
                Assert.IsTrue(ConversationManager.StartConversation(Speaker(conversation), _player));
                ChooseTarget(confirmation);
                if (!ground) SettlementRuntime.ActiveZone = null;
                ChooseTarget("Accepted");
                Assert.AreEqual(ground, StoryletPart.Current.IsQuestActive(quest));
                Assert.AreEqual(ground ? "Accepted" : confirmation, ConversationManager.CurrentNode.ID);
                Assert.AreEqual(ground ? 1 : 0, _zone.GetAllEntities().Count(e => e.BlueprintName == cargo));
                Assert.IsFalse(Inventory.Objects.Any(e => e.BlueprintName == cargo));
            }
            finally
            {
                StoryletRegistry.Reset(); StoryletPart.Current = null;
                StoryletPart.LocalPlayer = null; NarrativeStatePart.Current = null;
            }
        }
    }
}
