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
    /// C2 (Docs/PLACE-WORK-AND-PROGRESSION.md §2): a concern is raised by its owner in its
    /// own place, accepted or refused aloud, followed up, and reported, entirely inside the
    /// resident's existing conversation. Exercised through ConversationManager on Sill's
    /// shipped well concern, with the real settlement repair site and the Concord contract.
    /// </summary>
    public sealed class ConcernConversationTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player, keeper;
        Zone sill;

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
            new SettlementManager(() => 5, id => id == "Overworld.10.10.0" ? poi : null);
            sill = new Zone("Overworld.10.10.0");
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
        static int Index(string text)
        {
            var list = ConversationManager.VisibleChoices;
            for (int i = 0; i < list.Count; i++) if (list[i].Text.Contains(text)) return i;
            Assert.Fail("no choice containing '" + text + "' in: " + string.Join(" | ", list.Select(c => c.Text)));
            return -1;
        }
        static bool Has(string text) => ConversationManager.VisibleChoices.Any(c => c.Text.Contains(text));
        static string Q => ConcernRegistry.QuestIdFor("sill.well");

        // ════════════════ who raises it, where ════════════════

        [Test] public void TheWellKeeperInSillRaisesTheWell()
        {
            Talk(keeper);
            Assert.True(Has("Is something wrong here?"));
        }

        [Test] public void ANonOwnerOrTheOwnerElsewhereDoesNot()
        {
            var farmer = factory.CreateEntity("Villager"); sill.AddEntity(farmer, 12, 10);
            Talk(farmer); Assert.False(Has("Is something wrong here?"), "not the owner");
            var elsewhere = new Zone("Overworld.7.8.0"); var other = factory.CreateEntity("WellKeeper"); elsewhere.AddEntity(other, 5, 5);
            Talk(other); Assert.False(Has("Is something wrong here?"), "the owner role in another place");
        }

        [Test] public void OnlyOnTheAuthoredStartNode()
        {
            Talk(keeper);
            var deeper = new NodeData { ID = "Deeper", Text = "x" };
            ConversationManager.CurrentConversation.Nodes.Add(deeper); ConversationManager.CurrentNode = deeper;
            ConversationManager.RefreshVisibleChoices();
            Assert.False(Has("Is something wrong here?"));
        }

        // ════════════════ accept, refuse ════════════════

        [Test] public void AcceptingOpensTheActWithItsGiver()
        {
            Talk(keeper); ConversationManager.SelectChoice(Index("Is something wrong here?"));
            StringAssert.Contains("filtration ring", ConversationManager.CurrentText);
            ConversationManager.SelectChoice(Index("I'll see to it."));
            Assert.True(storylets.IsQuestActive(Q));
            var entry = storylets.GetClosure(Q);
            Assert.AreEqual(ClosureState.Open, entry.State); Assert.AreEqual(keeper.ID, entry.GiverId);
            Assert.True(Has("About the bitter well"), "now it is followed up, not offered");
            Assert.False(Has("Is something wrong here?"));
        }

        [Test] public void RefusingAloudEndsItRefusedAndItIsNotOfferedAgain()
        {
            Talk(keeper); ConversationManager.SelectChoice(Index("Is something wrong here?"));
            ConversationManager.SelectChoice(Index("I can't take this on"));
            StringAssert.Contains("Better said than left hanging", ConversationManager.CurrentText);
            Assert.AreEqual(ClosureState.Refused, storylets.GetClosure(Q).State);
            Assert.True(storylets.ReadLedger().Clean, "a spoken no is a closure");
            Talk(keeper); Assert.False(Has("Is something wrong here?"));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "concern", Kind = "Refused", Limit = 5 }).Records.Count);
        }

        [Test] public void GivingUpAnAcceptedConcernIsAlsoSpoken()
        {
            Talk(keeper); ConversationManager.SelectChoice(Index("Is something wrong here?")); ConversationManager.SelectChoice(Index("I'll see to it."));
            ConversationManager.SelectChoice(Index("About the bitter well"));
            ConversationManager.SelectChoice(Index("giving this up"));
            Assert.AreEqual(ClosureState.Refused, storylets.GetClosure(Q).State);
        }

        // ════════════════ the Concord's offer, the work and the report ════════════════

        [Test] public void TheConcordContractRepairsTheWellAndTheOwnerHearsIt()
        {
            Talk(keeper); ConversationManager.SelectChoice(Index("Is something wrong here?")); ConversationManager.SelectChoice(Index("I'll see to it."));
            int rep = PlayerReputation.Get("SaccharineConcord");
            ConversationManager.SelectChoice(Index("About the bitter well"));
            ConversationManager.SelectChoice(Index("Pay the Concord for a mason"));
            Assert.AreEqual(40, TradeSystem.GetDrams(player), "60 drams paid");
            Assert.AreEqual(rep + 5, PlayerReputation.Get("SaccharineConcord"));
            var site = PlaceSites.Site("Sill", "MainWell");
            Assert.AreEqual(RepairStage.StableRepair, site.Stage); Assert.AreEqual(RepairMethodId.Contracted, site.ResolvedByMethod);
            Assert.True(SettlementManager.Current.HasCondition("Overworld.10.10.0", SettlementManager.ContractCondition("MainWell")));

            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.True(storylets.GetQuestState(Q).CurrentStageIndex >= 1, "the contract approach finished the work");
            Assert.AreEqual(2, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("sill.well")), "contract was approach 2");

            int xp = player.GetStatValue("Experience", 0);
            Talk(keeper); ConversationManager.SelectChoice(Index("It's done"));
            ConversationManager.SelectChoice(Index("Glad to help"));
            Assert.True(storylets.IsQuestCompleted(Q));
            Assert.AreEqual(ClosureState.Closed, storylets.GetClosure(Q).State);
            Assert.AreEqual(50, TradeSystem.GetDrams(player), "10 drams reward");
            Assert.AreEqual(xp + 60, player.GetStatValue("Experience", 0), "the concern's 60 XP");
            Talk(keeper); Assert.False(Has("It's done") || Has("Is something wrong here?") || Has("About the bitter well"), "done is done");
        }

        [Test] public void AnUnaffordableContractChargesNothing()
        {
            Talk(keeper); ConversationManager.SelectChoice(Index("Is something wrong here?")); ConversationManager.SelectChoice(Index("I'll see to it."));
            TradeSystem.SetDrams(player, 20);
            ConversationManager.SelectChoice(Index("About the bitter well"));
            ConversationManager.SelectChoice(Index("Pay the Concord for a mason"));
            Assert.AreEqual(20, TradeSystem.GetDrams(player));
            Assert.AreEqual(RepairStage.Fouled, PlaceSites.Site("Sill", "MainWell").Stage);
        }

        [Test] public void RebuildingByHandFinishesTheOtherApproach()
        {
            Talk(keeper); ConversationManager.SelectChoice(Index("Is something wrong here?")); ConversationManager.SelectChoice(Index("I'll see to it."));
            var site = PlaceSites.Site("Sill", "MainWell");
            site.Stage = RepairStage.StableRepair; site.ResolvedByMethod = RepairMethodId.ManualRepair;
            storylets.OnTickEnd(NarrativeStatePart.Current);
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact("sill.well")), "by hand was approach 1");
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("sill_well_rebuilt_by_hand"));
            Assert.True(SettlementManager.Current.HasCondition("Overworld.10.10.0", "RebuiltByHand.MainWell"), "the place remembers how");
            Assert.False(SettlementManager.Current.HasCondition("Overworld.10.10.0", SettlementManager.ContractCondition("MainWell")), "counter-check");
        }

        // ════════════════ discoverability ════════════════

        [Test] public void TheOwnerCarriesAQuestCueUntilTheConcernIsDone()
        {
            Assert.AreEqual(QuestCueState.Available, QuestCueStateQuery.Evaluate(keeper, sill, player));
            var villager = factory.CreateEntity("Villager"); sill.AddEntity(villager, 12, 10);
            Assert.AreEqual(QuestCueState.None, QuestCueStateQuery.Evaluate(villager, sill, player), "counter-check: not an owner");
            Assert.IsNull(ConcernConversation.Accept(keeper, player, "sill.well"));
            Assert.AreEqual(QuestCueState.Active, QuestCueStateQuery.Evaluate(keeper, sill, player));
            Assert.IsNull(ConcernConversation.Refuse(keeper, player, "sill.well"));
            Assert.AreEqual(QuestCueState.None, QuestCueStateQuery.Evaluate(keeper, sill, player), "a spoken no clears the cue");
        }

        // ════════════════ the Elder ════════════════

        [Test] public void TheElderNamesWhatTroublesSill()
        {
            var elder = factory.CreateEntity("Elder"); sill.AddEntity(elder, 9, 10);
            Talk(elder); ConversationManager.SelectChoice(Index("What troubles this place?"));
            string text = ConversationManager.CurrentText;
            StringAssert.Contains("The Bitter Well", text); StringAssert.Contains("well keeper", text);
            StringAssert.Contains("The Watch Lantern", text);
        }
    }
}
