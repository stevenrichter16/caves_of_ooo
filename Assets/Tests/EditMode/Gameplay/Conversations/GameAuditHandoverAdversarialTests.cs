using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Required handover boundaries, persistence, registries and authored reward chains.</summary>
    public class GameAuditHandoverAdversarialTests
    {
        private EntityFactory _factory;
        private Entity _player;
        private Zone _zone;
        private SettlementManager _manager;
        private InventoryPart Inventory => _player.GetPart<InventoryPart>();
        private const string SettlementId = SettlementSiteDefinitions.StartingVillageZoneId;
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
            Diag.ResetAll(); MessageLog.Clear(); FactionManager.Initialize(); PlayerReputation.Reset();
            ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            SettlementRuntime.Reset(); StoryletRegistry.Reset();
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = new NarrativeStatePart();
            ConversationActions.Factory = _factory;
            foreach (string file in new[] { "FriendlyNPCs.json", "Palimpsest.json" })
                ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                    "Resources/Content/Conversations", file)), file);
            _player = new Entity { BlueprintName = "AuditHandover" }; _player.Tags["Player"] = "";
            _player.AddPart(new InventoryPart()); _player.AddPart(new PhysicsPart());
            _zone = new Zone(SettlementId); _zone.AddEntity(_player, 10, 10); SettlementRuntime.ActiveZone = _zone;
            _manager = new SettlementManager(() => 11, _ => new PointOfInterest(POIType.Village, "Sill", "Villagers", 1));
        }
        [TearDown]
        public void TearDown()
        {
            MessageLog.OnMessage = null; Diag.ResetAll();
            ConversationManager.EndConversation(); ConversationLoader.Reset(); ConversationActions.Reset(); ConversationPredicates.Reset();
            ConversationActions.Factory = null; SettlementRuntime.Reset(); SettlementManager.ResetCurrent();
            StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; PlayerReputation.Reset(); FactionManager.Reset();
        }
        private Entity Carry(string bp, int count = 1, string name = null)
        {
            var item = _factory.CreateEntity(bp);
            Assert.NotNull(item);
            var stacker = item.GetPart<StackerPart>();
            if (count != 1) Assert.NotNull(stacker, bp + " must support authored stacking");
            if (stacker != null) stacker.StackCount = count;
            if (name != null) item.GetPart<RenderPart>().DisplayName = name;
            Assert.IsTrue(Inventory.AddObject(item)); return item;
        }
        private Entity Speaker(string id)
        {
            var speaker = new Entity(); speaker.AddPart(new ConversationPart { ConversationID = id });
            speaker.Properties["SettlementId"] = SettlementId; _zone.AddEntity(speaker, 11, 10); return speaker;
        }
        private static void Choose(string target)
        {
            int i = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == target);
            Assert.GreaterOrEqual(i, 0, target); ConversationManager.SelectChoice(i);
        }

        [TestCase("TakeItem", false)] [TestCase("TakeItem", true)]
        [TestCase("TakeItemWithTag", false)] [TestCase("TakeItemWithTag", true)]
        public void ZeroFirstStack_DoesNotHideLaterValidUnit(string action, bool validLater)
        {
            var empty = Carry("GrimoireCopy", 1, "empty copy"); empty.GetPart<StackerPart>().StackCount = 0;
            Entity valid = validLater ? Carry("GrimoireCopy", 2, "usable copy") : null;
            Assert.AreEqual(validLater, ConversationActions.TryExecute(action, null, _player, "GrimoireCopy"));
            Assert.IsTrue(Inventory.Objects.Contains(empty)); Assert.AreEqual(0, empty.GetPart<StackerPart>().StackCount);
            if (validLater) Assert.AreEqual(1, valid.GetPart<StackerPart>().StackCount);
        }

        [TestCase("TakeItem", 1)] [TestCase("TakeItem", 3)]
        [TestCase("TakeItemWithTag", 1)] [TestCase("TakeItemWithTag", 3)]
        public void SavedDonation_SpendsOnlyOneAfterReload(string action, int count)
        {
            Carry("GrimoireCopy", count);
            _player = PartRoundTripHelper.RoundTripEntityViaTokenGraph(_player);
            var item = Inventory.Objects[0];
            Assert.IsTrue(ConversationActions.TryExecute(action, null, _player, "GrimoireCopy"));
            Assert.AreEqual(count > 1, Inventory.Objects.Contains(item));
            Assert.AreEqual(count > 1 ? count - 1 : 1, item.GetPart<StackerPart>().StackCount);
            if (count > 1) Assert.AreSame(_player, item.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase(false)] [TestCase(true)]
        public void RequiredActionList_StopsRewardWhenPaymentRefuses(bool carried)
        {
            if (carried) Carry("SilverSand");
            var actions = new List<ConversationParam> {
                new ConversationParam { Key = "TakeItem", Value = "SilverSand" },
                new ConversationParam { Key = "SetProperty", Value = "audit_reward:true" } };
            Assert.AreEqual(carried, ConversationActions.TryExecuteAll(actions, null, _player));
            Assert.AreEqual(carried, _player.Properties.ContainsKey("audit_reward"));
        }

        [Test]
        public void LegacyVoidActionList_RetainsItsNontransactionalCompatibilityContract()
        {
            ConversationActions.ExecuteAll(new List<ConversationParam> {
                new ConversationParam { Key = "TakeItem", Value = "SilverSand" },
                new ConversationParam { Key = "SetProperty", Value = "legacy_after:true" } }, null, _player);
            Assert.IsTrue(_player.Properties.ContainsKey("legacy_after"));
        }

        [TestCase(false)] [TestCase(true)]
        public void RegisteredOverrideAndReset_KeepRequiredRegistryConsistent(bool reset)
        {
            ConversationActions.EnsureInitialized();
            ConversationActions.Register("TakeItem", (_, listener, __) => listener.SetIntProperty("override_called", 1));
            if (reset) ConversationActions.Reset();
            Assert.IsTrue(ConversationActions.IsRegistered("TakeItem"));
            Assert.AreEqual(!reset, ConversationActions.TryExecute("TakeItem", null, _player, "SilverSand"));
            Assert.AreEqual(reset ? 0 : 1, _player.GetIntProperty("override_called"));
        }

        [TestCase(false)] [TestCase(true)]
        public void GenericGiveItem_ChecksRefusedGroundPlacement(bool barren)
        {
            // FlowerField weighs zero; an already over-capacity inventory forces
            // this generic handout through ground placement in both controls.
            Carry("SilverSand"); Inventory.MaxWeight = 0;
            // A synthetic handout of vegetation exercises the shared placement contract;
            // ordinary grimoire copies are not vegetation and cannot hit this terrain rule.
            if (barren)
            {
                var marker = new Entity(); marker.Tags["Barren"] = "";
                Assert.IsTrue(_zone.AddEntity(marker, 10, 10));
            }
            Assert.AreEqual(!barren, ConversationActions.TryExecute("GiveItem", null, _player, "FlowerField"));
            Assert.AreEqual(barren ? 0 : 1, _zone.GetAllEntities().Count(e => e.BlueprintName == "FlowerField"));
            Assert.AreEqual(!barren, MessageLog.GetRecent(10).Any(m => m.Contains("at your feet")));
        }

        [TestCase(false)] [TestCase(true)]
        public void PalimpsestGift_ProseAgreesWithActualDeliveryLocation(bool full)
        {
            if (full) Inventory.MaxWeight = 0;
            Assert.IsTrue(ConversationManager.StartConversation(Speaker("PalimpsestEcho_1"), _player));
            // Stage the authored gift node; the selected choice and its entire action list are real.
            ConversationManager.CurrentNode = ConversationManager.CurrentConversation.GetNode("Heartbreaking");
            ConversationManager.RefreshVisibleChoices(); Choose("GaveGift");
            var ground = _zone.GetAllEntities().Where(e => e.BlueprintName == "TemporalShard").ToList();
            Assert.AreEqual(full ? 1 : 0, ground.Count);
            Assert.AreEqual(full ? 0 : 1, Inventory.Objects.Count(e => e.BlueprintName == "TemporalShard"));
            Assert.IsTrue(_player.Properties.ContainsKey("PalimpsestGaveGift"));
            if (full) Assert.IsFalse(MessageLog.GetRecent(10).Any(m => m.Contains("in your hand")),
                "An authored follow-up message cannot claim hand delivery after a ground fallback.");
        }

        [TestCase("missing")] [TestCase("empty")] [TestCase("carried")]
        public void AuthoredCourierDelivery_RequiresPaymentBeforeFactFeeAndCompletion(string state)
        {
            StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Data/Storylets/BogBodyCourier.json")));
            ConversationActions.Execute("StartQuest", null, _player, "BogBodyCourier");
            var item = Carry("SealedBogTakenBody");
            Assert.IsTrue(ConversationManager.StartConversation(Speaker("FilerClerk_1"), _player));
            if (state == "missing") Inventory.RemoveObject(item);
            // The real sealed body is unique/nonstacking. This deliberately
            // malformed quantity models an invalid reference, not authored cargo stacks.
            if (state == "empty") item.AddPart(new StackerPart { StackCount = 0 });
            Choose("Delivered");
            bool paid = state == "carried";
            Assert.AreEqual(paid ? "Delivered" : "Start", ConversationManager.CurrentNode.ID);
            Assert.AreEqual(paid ? 25 : 0, TradeSystem.GetDrams(_player));
            Assert.AreEqual(paid ? 10 : 0, PlayerReputation.Get("PaleCuration"));
            Assert.AreEqual(paid ? 1 : 0, NarrativeStatePart.Current.GetFact("bog_body_reached_marrowstye"));
            Assert.AreEqual(paid, StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
            if (paid) Assert.IsFalse(Inventory.Objects.Contains(item));
        }

        [TestCase(RepairStage.Fouled, false)] [TestCase(RepairStage.Fouled, true)]
        [TestCase(RepairStage.StableRepair, false)] [TestCase(RepairStage.StableRepair, true)]
        [TestCase(RepairStage.ImprovedWithCaretaker, false)] [TestCase(RepairStage.ImprovedWithCaretaker, true)]
        public void Teaching_OnlyStableRepairWithCopyCanCommit(RepairStage stage, bool copyPresent)
        {
            var site = _manager.GetSite(SettlementId, "VillageOven"); site.Stage = stage;
            var copy = copyPresent ? Carry("GrimoireCopy", 3) : null;
            int changes = 0; _manager.SiteStateChanged += (_, __) => changes++;
            bool allowed = stage == RepairStage.StableRepair && copyPresent;
            Assert.AreEqual(allowed, _manager.ApplyRepairMethod(SettlementId, "VillageOven", RepairMethodId.TeachBaker, _player));
            Assert.AreEqual(allowed ? RepairStage.ImprovedWithCaretaker : stage, site.Stage);
            Assert.AreEqual(allowed ? 1 : 0, changes);
            if (copyPresent) Assert.AreEqual(allowed ? 2 : 3, copy.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void FreeCaretakerTeaching_StillNeedsNoDonation()
        {
            var site = _manager.GetSite(SettlementId, "MainWell"); site.Stage = RepairStage.StableRepair;
            Assert.IsEmpty(Inventory.Objects);
            Assert.IsTrue(_manager.ApplyRepairMethod(SettlementId, "MainWell", RepairMethodId.TeachCaretaker, _player));
        }

        [TestCase(false)] [TestCase(true)]
        public void RequiredDiagnostic_RecordsOneOutcomeAndExactMissingUnitReason(bool carried)
        {
            if (carried) Carry("SilverSand");
            Assert.AreEqual(carried, ConversationActions.TryExecute("TakeItem", null, _player, "SilverSand"));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = carried ? "ConversationActionApplied" : "ConversationActionRejected" }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("TakeItem", records[0].PayloadJson);
            if (!carried) StringAssert.Contains("missing_positive_unit", records[0].PayloadJson);
        }

        [Test]
        public void NestedRejectedAction_DoesNotPoisonSuccessfulOuterOutcome()
        {
            Carry("SilverSand", 2);
            bool attempted = false, nested = true;
            MessageLog.OnMessage = m =>
            {
                if (!attempted && m.StartsWith("You hand over"))
                {
                    attempted = true;
                    nested = ConversationActions.TryExecute("TakeItem", null, _player, "WardOil");
                }
            };
            Assert.IsTrue(ConversationActions.TryExecute("TakeItem", null, _player, "SilverSand"));
            Assert.IsTrue(attempted); Assert.IsFalse(nested);
            Assert.AreEqual(1, Inventory.Objects.Single().GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void SiteChangedRetry_CannotSpendAnotherUnitAfterCommittedRepair()
        {
            Carry("OvenBuildersGuide"); var clay = Carry("FireClay", 3);
            int changes = 0; bool retry = true;
            _manager.SiteStateChanged += (_, __) => {
                changes++;
                if (changes == 1) retry = _manager.ApplyRepairMethod(SettlementId, "VillageOven", RepairMethodId.OvenRebuild, _player);
            };
            Assert.IsTrue(_manager.ApplyRepairMethod(SettlementId, "VillageOven", RepairMethodId.OvenRebuild, _player));
            Assert.AreEqual(1, changes); Assert.IsFalse(retry); Assert.AreEqual(2, clay.GetPart<StackerPart>().StackCount);
        }
    }
}
