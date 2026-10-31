using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for place concerns (C1/C2, Docs/PLACE-WORK-AND-PROGRESSION.md):
    /// parser input, the Concord contract's money transaction, double-actions on the
    /// accept/refuse/report/offer chain, place and speaker gates, predicate argument
    /// shapes and diag emission. Each test names the bug class it probes.
    /// </summary>
    public sealed class ConcernAdversarialTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player, keeper;
        Zone sill;
        const string SillZone = "Overworld.10.10.0";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            StoryletRegistry.Reset(); ConcernRegistry.LoadAll(); MessageLog.Clear(); Diag.ResetAll();
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            var poi = new PointOfInterest(POIType.Village, "Sill");
            new SettlementManager(() => 5, id => id == SillZone ? poi : null);
            sill = new Zone(SillZone);
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            keeper = factory.CreateEntity("WellKeeper");
            sill.AddEntity(player, 10, 10); sill.AddEntity(keeper, 11, 10);
            TradeSystem.SetDrams(player, 100);
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation();
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; SettlementManager.ResetCurrent();
        }

        void Talk(Entity speaker)
        {
            ConversationManager.Speaker = speaker; ConversationManager.Listener = player;
            var start = new NodeData { ID = "Start", Text = "authored" };
            start.Choices.Add(new ChoiceData { Text = "[Leave]", Target = "End" });
            ConversationManager.CurrentConversation = new ConversationData { ID = "Probe", Nodes = { start } };
            ConversationManager.CurrentNode = start;
            ConversationManager.RefreshVisibleChoices();
        }
        static bool Has(string text) => ConversationManager.VisibleChoices.Any(c => c.Text.Contains(text));
        static string Q => ConcernRegistry.QuestIdFor("sill.well");
        static RepairableSiteState Well => PlaceSites.Site("Sill", "MainWell");
        static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "concern", Kind = kind, Limit = 50 }).Records.Count;
        void AcceptWell() => Assert.IsNull(ConcernConversation.Accept(keeper, player, "sill.well"), "precondition: accepted");

        // ════════════════ parser: malformed concern files ════════════════

        [TestCase("")] [TestCase("{}")] [TestCase("{\"Concerns\":null}")] [TestCase("{\"Concerns\":[]}")]
        public void Adversarial_EmptyOrHollowFilesLoadNothingAndDoNotThrow(string json)
        {
            ConcernRegistry.Reset(); int before = ConcernRegistry.All.Count;
            Assert.DoesNotThrow(() => ConcernRegistry.LoadFromJson(json, "probe"));
            Assert.AreEqual(before, ConcernRegistry.All.Count);
        }

        [Test] public void Adversarial_AConcernWithoutAnIdIsSkippedAndADuplicateKeepsTheFirst()
        {
            ConcernRegistry.Reset();
            ConcernRegistry.LoadFromJson("{\"Concerns\":[{\"Title\":\"anonymous\"},{\"Id\":\"x.one\",\"Title\":\"First\"},{\"Id\":\"x.one\",\"Title\":\"Second\"}]}", "probe");
            Assert.AreEqual(1, ConcernRegistry.All.Count);
            Assert.AreEqual("First", ConcernRegistry.Get("x.one").Title, "the first definition wins; a duplicate must not replace live quests");
        }

        [Test] public void Adversarial_LintFlagsAnOfferWithoutTextAndAnUnknownKind()
        {
            var c = ConcernRegistry.Get("sill.well");
            var copy = JsonUtility.FromJson<ConcernData>(JsonUtility.ToJson(c));
            copy.Approaches[1].OfferText = ""; copy.Approaches[0].Kind = "Wish";
            var lines = string.Join("|", ConcernRegistry.Lint(copy, factory));
            StringAssert.Contains("offer needs its text", lines); StringAssert.Contains("unknown kind", lines);
            Assert.IsEmpty(ConcernRegistry.Lint(c, factory), "counter-check: the shipped concern is clean");
        }

        // ════════════════ predicate argument shapes ════════════════

        [TestCase("Sill:MainWell:3")] [TestCase("Sill:MainWell:-1")] [TestCase("Sill:MainWell:stablerepair")]
        [TestCase("Sill::StableRepair")] [TestCase(":MainWell:StableRepair")] [TestCase(null)] [TestCase("")]
        public void Adversarial_SiteStageRejectsNumericLowercaseAndHollowArguments(string arg)
        {
            Well.Stage = RepairStage.ImprovedWithCaretaker;
            Assert.False(PlaceSites.StageAtLeast(arg), "only a named stage of a named site counts: '" + arg + "'");
            Assert.True(PlaceSites.StageAtLeast("Sill:MainWell:StableRepair"), "counter-check: the named form is read");
        }

        [TestCase("Sill:MainWell:9")] [TestCase("Sill:MainWell:Bribed")]
        public void Adversarial_SiteMethodRejectsUndefinedMethods(string arg)
        {
            Well.ResolvedByMethod = RepairMethodId.Contracted;
            Assert.False(PlaceSites.ResolvedBy(arg));
            Assert.True(PlaceSites.ResolvedBy("Sill:MainWell:Contracted"), "counter-check");
        }

        [Test] public void Adversarial_SitePredicatesWithoutASettlementManagerAreFalseNotThrown()
        {
            SettlementManager.ResetCurrent();
            Assert.False(PlaceSites.StageAtLeast("Sill:MainWell:Fouled"));
            Assert.False(PlaceSites.ResolvedBy("Sill:MainWell:None"));
        }

        // ════════════════ who, where, when ════════════════

        [Test] public void Adversarial_AWellAlreadyRepairedIsNotPitchedAsBroken()
        {
            Well.Stage = RepairStage.StableRepair; Well.ResolvedByMethod = RepairMethodId.ManualRepair;
            Talk(keeper);
            Assert.False(Has("Is something wrong here?"), "the keeper must not ask for help with a well that already runs clear");
            Well.Stage = RepairStage.TemporarilyPurified;
            Talk(keeper);
            Assert.True(Has("Is something wrong here?"), "counter-check: a merely purified well still turns bitter");
        }

        [Test] public void Adversarial_ASpeakerOutsideAnyZoneOffersNothing()
        {
            var adrift = factory.CreateEntity("WellKeeper");
            Assert.DoesNotThrow(() => Talk(adrift));
            Assert.False(Has("Is something wrong here?"));
        }

        [Test] public void Adversarial_TheElderStaysSilentWhenNothingIsOpen()
        {
            var elder = factory.CreateEntity("Elder"); sill.AddEntity(elder, 9, 10);
            foreach (var c in ConcernRegistry.All.Where(c => c.Place == "Sill"))
            { storylets.StartQuest(new QuestState { QuestId = ConcernRegistry.QuestIdFor(c.Id) }); storylets.RefuseQuest(ConcernRegistry.QuestIdFor(c.Id)); }
            Talk(elder);
            Assert.False(Has("What troubles this place?"), "nothing to name, no question offered");
        }

        [Test] public void Adversarial_AnotherPlacesElderDoesNotNameSillsTroubles()
        {
            var elsewhere = new Zone("Overworld.7.8.0"); var elder = factory.CreateEntity("Elder"); elsewhere.AddEntity(elder, 5, 5);
            Talk(elder);
            Assert.False(Has("What troubles this place?") && ConcernRegistry.All.All(c => c.Place == "Sill"),
                "only concerns of the elder's own place are listed");
        }

        [Test] public void Adversarial_SyntheticConcernNodesNeverStackMoreConcernChoices()
        {
            Talk(keeper);
            ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Text.Contains("Is something wrong here?")));
            StringAssert.StartsWith(ConcernConversation.NodePrefix, ConversationManager.CurrentNode.ID);
            Assert.False(Has("Is something wrong here?"), "the pitch node does not offer itself again");
        }

        [Test] public void Adversarial_UnmetRequirementsHideTheConcern()
        {
            var c = ConcernRegistry.Get("sill.well");
            var saved = c.Requires; c.Requires = new List<ConversationParam> { new ConversationParam { Key = "IfFact", Value = "probe_never:>=:1" } };
            try
            {
                Assert.AreEqual(ConcernConversation.State.Hidden, ConcernConversation.StateOf(c, keeper, player));
                NarrativeStatePart.Current.SetFact("probe_never", 1);
                Assert.AreEqual(ConcernConversation.State.Offerable, ConcernConversation.StateOf(c, keeper, player), "counter-check");
            }
            finally { c.Requires = saved; }
        }

        // ════════════════ double actions and wrong-state actions ════════════════

        [Test] public void Adversarial_AcceptingTwiceDoesNotRestartTheAct()
        {
            AcceptWell();
            Assert.AreEqual("not_offerable", ConcernConversation.Accept(keeper, player, "sill.well"));
            Assert.AreEqual(1, Count("Accepted"));
        }

        [Test] public void Adversarial_ReportingWhileStillWorkingPaysNothing()
        {
            AcceptWell(); int xp = player.GetStatValue("Experience", 0);
            Assert.AreEqual("not_reportable", ConcernConversation.Report(keeper, player, "sill.well"));
            Assert.AreEqual(100, TradeSystem.GetDrams(player)); Assert.AreEqual(xp, player.GetStatValue("Experience", 0));
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.ReportedFact("sill.well")), "no reported fact either");
        }

        [Test] public void Adversarial_ReportingTwicePaysOnce()
        {
            AcceptWell(); Well.Stage = RepairStage.StableRepair; Well.ResolvedByMethod = RepairMethodId.ManualRepair;
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.IsNull(ConcernConversation.Report(keeper, player, "sill.well"));
            int drams = TradeSystem.GetDrams(player), xp = player.GetStatValue("Experience", 0);
            Assert.AreEqual("not_reportable", ConcernConversation.Report(keeper, player, "sill.well"));
            Assert.AreEqual(drams, TradeSystem.GetDrams(player)); Assert.AreEqual(xp, player.GetStatValue("Experience", 0));
            Assert.AreEqual(1, Count("Reported"));
        }

        [Test] public void Adversarial_RefusingAFinishedConcernIsRejected()
        {
            AcceptWell(); Well.Stage = RepairStage.StableRepair; Well.ResolvedByMethod = RepairMethodId.ManualRepair;
            storylets.OnTickEnd(NarrativeStatePart.Current);
            ConcernConversation.Report(keeper, player, "sill.well");
            Assert.AreEqual("nothing_to_refuse", ConcernConversation.Refuse(keeper, player, "sill.well"));
            Assert.AreEqual(ClosureState.Closed, storylets.GetClosure(Q).State, "a kept promise is not turned into a refusal");
        }

        [Test] public void Adversarial_TheOfferIsOnlyTakenWhileWorking()
        {
            Assert.AreEqual("not_working", ConcernConversation.TakeOffer(keeper, player, "sill.well|contract"), "not yet accepted");
            Assert.AreEqual(100, TradeSystem.GetDrams(player));
            AcceptWell();
            Assert.AreEqual("no_offer", ConcernConversation.TakeOffer(keeper, player, "sill.well|rebuild"), "rebuilding has no offer");
            Assert.AreEqual("no_offer", ConcernConversation.TakeOffer(keeper, player, "sill.well|nonesuch"));
            Assert.AreEqual("unknown_concern", ConcernConversation.TakeOffer(keeper, player, "sill.well"));
            Assert.AreEqual("unknown_concern", ConcernConversation.TakeOffer(keeper, player, null));
            Assert.AreEqual(100, TradeSystem.GetDrams(player));
            Assert.IsNull(ConcernConversation.TakeOffer(keeper, player, "sill.well|contract"), "counter-check: the real offer");
            Assert.AreEqual(40, TradeSystem.GetDrams(player));
        }

        [Test] public void Adversarial_UnknownConcernIdsAreRejectedEverywhere()
        {
            Assert.AreEqual("unknown_concern", ConcernConversation.Accept(keeper, player, "nowhere.nothing"));
            Assert.AreEqual("unknown_concern", ConcernConversation.Refuse(keeper, player, null));
            Assert.AreEqual("unknown_concern", ConcernConversation.Report(keeper, player, ""));
            Assert.IsNull(ConcernConversation.BuildNode("pitch|nowhere.nothing", keeper, player));
            Assert.IsNull(ConcernConversation.BuildNode("garbage", keeper, player));
            Assert.IsNull(ConcernConversation.BuildNode("wander|sill.well", keeper, player));
        }

        // ════════════════ the contract: a money transaction ════════════════

        [Test] public void Adversarial_ExactFundsPayAndOneDramShortPaysNothing()
        {
            TradeSystem.SetDrams(player, 59);
            Assert.AreEqual("insufficient_drams", ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, "Sill:MainWell:60"));
            Assert.AreEqual(59, TradeSystem.GetDrams(player)); Assert.AreEqual(RepairStage.Fouled, Well.Stage);
            TradeSystem.SetDrams(player, 60);
            Assert.IsNull(ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, "Sill:MainWell:60"));
            Assert.AreEqual(0, TradeSystem.GetDrams(player)); Assert.AreEqual(RepairStage.StableRepair, Well.Stage);
        }

        [Test] public void Adversarial_ContractingASoundWellRefundsInFull()
        {
            Well.Stage = RepairStage.StableRepair; Well.ResolvedByMethod = RepairMethodId.ManualRepair;
            int rep = PlayerReputation.Get("SaccharineConcord");
            Assert.AreEqual("already_repaired", ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, "Sill:MainWell:60"));
            Assert.AreEqual(100, TradeSystem.GetDrams(player), "nothing taken");
            Assert.AreEqual(rep, PlayerReputation.Get("SaccharineConcord"), "no standing for a refused job");
            Assert.AreEqual(RepairMethodId.ManualRepair, Well.ResolvedByMethod, "the hand repair stands");
        }

        [Test] public void Adversarial_ContractingTwiceChargesOnce()
        {
            Assert.IsNull(ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, "Sill:MainWell:60"));
            Assert.AreEqual("already_repaired", ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, "Sill:MainWell:60"),
                "a sound well is named as sound, not as unaffordable");
            Assert.AreEqual(40, TradeSystem.GetDrams(player));
        }

        [TestCase("Sill:MainWell:-5", "invalid_argument")] [TestCase("Sill:MainWell:lots", "invalid_argument")]
        [TestCase("Sill:MainWell", "invalid_argument")] [TestCase("Atlantis:MainWell:10", "unknown_place")]
        [TestCase("Sill:NoSuchSite:10", "unknown_site")]
        public void Adversarial_MalformedContractsChargeNothing(string arg, string reason)
        {
            Assert.AreEqual(reason, ConversationActions.ExecuteRequired("ContractPlaceRepair", keeper, player, arg));
            Assert.AreEqual(100, TradeSystem.GetDrams(player), "never charged, never minted");
        }

        // ════════════════ approach exclusivity ════════════════

        [Test] public void Adversarial_ARepairByHandAfterTheContractDoesNotReRecordTheApproach()
        {
            AcceptWell();
            Assert.IsNull(ConcernConversation.TakeOffer(keeper, player, "sill.well|contract"));
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Well.ResolvedByMethod = RepairMethodId.ManualRepair;
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(2, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("sill.well")), "the first approach finished stands");
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact("sill_well_rebuilt_by_hand"));
        }

        [Test] public void Adversarial_AContractedRepairDoesNotCountAsTheHandRebuild()
        {
            AcceptWell();
            Well.Stage = RepairStage.StableRepair; Well.ResolvedByMethod = RepairMethodId.Contracted;
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(2, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("sill.well")));
        }

        // ════════════════ diag ════════════════

        [Test] public void Adversarial_AFailedOfferEmitsNoOfferTaken()
        {
            AcceptWell(); TradeSystem.SetDrams(player, 5);
            Assert.AreEqual("offer_refused", ConcernConversation.TakeOffer(keeper, player, "sill.well|contract"));
            Assert.AreEqual(0, Count("OfferTaken"));
            TradeSystem.SetDrams(player, 100);
            ConcernConversation.TakeOffer(keeper, player, "sill.well|contract");
            Assert.AreEqual(1, Count("OfferTaken"), "counter-check");
        }

        [Test] public void Adversarial_ReloadingTheRegistryKeepsARefusalRefused()
        {
            Assert.IsNull(ConcernConversation.Refuse(keeper, player, "sill.well"));
            StoryletRegistry.Reset(); ConcernRegistry.LoadAll();
            Talk(keeper);
            Assert.False(Has("Is something wrong here?"), "a spoken no survives a content reload (load game)");
        }
    }
}
