using System.IO;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3.6 (Docs/FELLING-W3-PLAN.md §3) — the body-courier contract.
    /// The Curation Sorter offers carriage; the sealed intake is a real
    /// burden (Weight 30 — an overburdened courier cannot MOVE); the
    /// filer-clerk at Marrowstye's window closes the entry. The offer
    /// quotes Clause the fifth of the Concord carriage contract
    /// VERBATIM (Lore/Codex/06_ConcordCarriageContract.md:31) — pinned
    /// to the canon string, so a paraphrase is a test failure.
    /// </summary>
    [TestFixture]
    public class BodyCourierTests
    {
        private const string QuestId = "BogBodyCourier";
        private const string ClauseTheFifth =
            "The Concord will not carry: unboxed spores; bodies not sealed "
            + "to Curation intake standard; anything that sings; anything "
            + "the Bower-Folk have asked after twice.";

        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            ConversationLoader.Reset();
            StoryletRegistry.Reset();
            ConversationManager.EndConversation();
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = new NarrativeStatePart();
            PlayerReputation.Reset();
            ConversationActions.Factory = _factory;

            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json")),
                "FriendlyNPCs.json");
            StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Storylets/BogBodyCourier.json")));
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            ConversationLoader.Reset();
            StoryletRegistry.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
            ConversationActions.Factory = null;
            PlayerReputation.Reset();
        }

        private static Entity Npc(string conversationId)
        {
            var npc = new Entity { BlueprintName = "npc" };
            npc.AddPart(new ConversationPart { ConversationID = conversationId });
            return npc;
        }

        private static Entity MakePlayer(int strength = 16)
        {
            var p = new Entity { ID = "player", BlueprintName = "Player" };
            p.Tags["Player"] = "";
            p.Tags["Creature"] = "";
            p.AddPart(new RenderPart { DisplayName = "you" });
            p.AddPart(new InventoryPart());
            p.Statistics["Hitpoints"] = new Stat { Owner = p, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            p.Statistics["Strength"] = new Stat { Owner = p, Name = "Strength", BaseValue = strength };
            return p;
        }

        private static bool SelectChoiceStartingWith(string prefix)
        {
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text.StartsWith(prefix))
                {
                    ConversationManager.SelectChoice(i);
                    return true;
                }
            }
            return false;
        }

        private static bool HasItem(Entity e, string blueprint)
            => e.GetPart<InventoryPart>().Objects.Any(o => o.BlueprintName == blueprint);

        // ════════════════════════════════════════════════════════
        // The words
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheOffer_QuotesClauseTheFifth_Verbatim()
        {
            var conv = ConversationLoader.Get("CurationSorter_1");
            var carriage = conv.GetNode("Carriage");
            Assert.IsNotNull(carriage, "the sorter offers carriage");
            StringAssert.Contains(ClauseTheFifth, carriage.Text,
                "the clause is quoted, not paraphrased — the Concord's "
                + "paper in the Concord's register");
        }

        [Test]
        public void TheFactString_MatchesBetweenClerkAndQuest()
        {
            // Cross-file lint: the clerk's SetFact and the objective's
            // IfFact name the same fact. A typo in either would complete
            // conversations but never the journal — silently.
            string conv = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Conversations/FriendlyNPCs.json"));
            string quest = File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Storylets/BogBodyCourier.json"));
            StringAssert.Contains("bog_body_reached_marrowstye:1", conv);
            StringAssert.Contains("bog_body_reached_marrowstye:>=:1", quest);
        }

        // ════════════════════════════════════════════════════════
        // The loop
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheFullLoop_OfferCarryDeliverPaid()
        {
            var player = MakePlayer();
            var sorter = Npc("CurationSorter_1");
            var clerk = Npc("FilerClerk_1");

            // Offer, at the Ledger.
            ConversationManager.StartConversation(sorter, player);
            Assert.IsTrue(SelectChoiceStartingWith("Is there work?"), "the offer stands");
            Assert.IsTrue(SelectChoiceStartingWith("I will carry it."), "and is taken");
            ConversationManager.EndConversation();

            Assert.IsTrue(StoryletPart.Current.IsQuestActive(QuestId), "the entry is open");
            Assert.IsTrue(HasItem(player, "SealedBogTakenBody"), "the burden is on your back");

            // Delivery, at the window.
            ConversationManager.StartConversation(clerk, player);
            Assert.IsTrue(SelectChoiceStartingWith("[Deliver]"), "the window recognizes paper");

            Assert.IsFalse(HasItem(player, "SealedBogTakenBody"), "handed over");
            Assert.AreEqual(25, TradeSystem.GetDrams(player), "the fee is counted");
            Assert.AreEqual(10, PlayerReputation.Get("PaleCuration"), "and the file remembers you");
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(QuestId), "the window closes the entry");
        }

        [Test]
        public void Refusal_SaidAloud_StartsNothing()
        {
            var player = MakePlayer();
            var sorter = Npc("CurationSorter_1");

            ConversationManager.StartConversation(sorter, player);
            SelectChoiceStartingWith("Is there work?");
            Assert.IsTrue(SelectChoiceStartingWith("No -- and I say it aloud."),
                "the spoken no is a real choice");
            ConversationManager.EndConversation();

            Assert.IsFalse(StoryletPart.Current.IsQuestActive(QuestId), "nothing entered");
            Assert.IsFalse(HasItem(player, "SealedBogTakenBody"), "nothing carried");
        }

        [Test]
        public void TheDeliverChoice_NeedsBothPaperAndParcel()
        {
            var player = MakePlayer();
            var clerk = Npc("FilerClerk_1");

            // Quest active, no body: the window is a wall.
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
            ConversationManager.StartConversation(clerk, player);
            Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Text.StartsWith("[Deliver]")),
                "no parcel, no delivery");
            ConversationManager.EndConversation();

            // Body, no quest: still a wall (found bodies are not paper).
            StoryletPart.Current = new StoryletPart();
            var body = _factory.CreateEntity("SealedBogTakenBody");
            player.GetPart<InventoryPart>().AddObject(body);
            ConversationManager.StartConversation(clerk, player);
            Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Text.StartsWith("[Deliver]")),
                "no paper, no delivery");
        }

        [Test]
        public void TheOffer_DoesNotRepeat_AfterCompletion()
        {
            // v1 divergence, pinned as the shipped contract: the quest
            // substrate is one-shot per id (StartQuest refuses completed
            // ids), so the offer hides once the entry is closed. True
            // repeatability is future work, recorded in the plan.
            var player = MakePlayer();
            var sorter = Npc("CurationSorter_1");
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
            StoryletPart.Current.CompleteQuest(QuestId);

            ConversationManager.StartConversation(sorter, player);
            Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Text.StartsWith("Is there work?")),
                "one carriage per courier, for now — the entry is closed");
        }

        // ════════════════════════════════════════════════════════
        // The burden
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheBurden_IsReal()
        {
            // Weight 30 against Strength 1 (capacity 15): an overburdened
            // courier cannot MOVE. Weight is the contract's cost, not a
            // number on a tooltip.
            var zone = new Zone("Z");
            var weakling = MakePlayer(strength: 1);
            zone.AddEntity(weakling, 10, 10);
            var body = _factory.CreateEntity("SealedBogTakenBody");
            weakling.GetPart<InventoryPart>().AddObject(body);

            Assert.IsFalse(MovementSystem.TryMove(weakling, zone, 1, 0),
                "thirty pounds of sealed intake against fifteen of capacity");
            Assert.AreEqual((10, 10), zone.GetEntityPosition(weakling));

            weakling.GetPart<InventoryPart>().RemoveObject(body);
            Assert.IsTrue(MovementSystem.TryMove(weakling, zone, 1, 0),
                "set it down and the legs work again");
        }
    }
}
