using System.IO;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// End-to-end dialogue tests for "A Message for the Hermit" (SM3,
    /// Docs/QUEST-POOL-EXPANSION.md) — the deliver / talk-to-NPC-Y pool quest.
    /// The RECIPIENT (the hermit) carries the completion: its [Deliver] choice
    /// is gated IfQuestActive + IfFact:hermit_message_delivered:&lt;:1 and runs
    /// SetFact on select. The objective polls that fact, so delivery is
    /// order-independent (you can only deliver after accepting — no soft-lock —
    /// and re-talking after delivery does nothing). Verified through the real
    /// ConversationManager.SelectChoice path.
    /// </summary>
    [TestFixture]
    public class QuestDeliverDialogueTests
    {
        private const string QuestId = "MessageForHermit";

        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            ConversationLoader.Reset();
            StoryletRegistry.Reset();
            ConversationManager.EndConversation();
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = new NarrativeStatePart();

            string convPath = Path.Combine(
                "Assets", "Resources", "Content", "Conversations", "Hermit_Quest.json");
            ConversationLoader.LoadFromJson(File.ReadAllText(convPath), "Hermit_Quest.json");

            StoryletRegistry.Register(new StoryletData
            {
                ID = QuestId,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "deliver" },
                        new QuestStageData { ID = "report" },
                    },
                },
            });
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            ConversationLoader.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
        }

        private static Entity MakeHermit()
        {
            var hermit = new Entity { BlueprintName = "Villager" };
            hermit.AddPart(new ConversationPart { ConversationID = "Hermit_Quest" });
            return hermit;
        }

        private static Entity MakePlayer()
        {
            var p = new Entity { BlueprintName = "Player" };
            p.Tags["Player"] = "";
            p.AddPart(new InventoryPart());
            return p;
        }

        private static List<string> VisibleChoiceTexts(Entity speaker, Entity listener)
        {
            ConversationManager.StartConversation(speaker, listener);
            return ConversationManager.VisibleChoices.Select(c => c.Text).ToList();
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

        [Test]
        public void Deliver_WhenQuestActive_SetsDeliveredFact()
        {
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
            var hermit = MakeHermit();
            var player = MakePlayer();

            var choices = VisibleChoiceTexts(hermit, player);
            Assert.IsTrue(choices.Any(c => c.StartsWith("[Deliver")),
                "with the quest active and undelivered, the hermit offers the deliver choice");

            Assert.IsTrue(SelectChoiceStartingWith("[Deliver"));
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("hermit_message_delivered"),
                "delivering sets the fact the objective polls (IfFact:hermit_message_delivered:>=:1)");
        }

        [Test]
        public void Deliver_BeforeQuestAccepted_ChoiceHidden()
        {
            // Counter-check: talking to the hermit before accepting shows no
            // deliver choice (IfQuestActive gate) — come back after accepting,
            // no soft-lock.
            var hermit = MakeHermit();
            var player = MakePlayer();

            var choices = VisibleChoiceTexts(hermit, player);
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Deliver")),
                "before the quest is active the deliver choice must be hidden");
        }

        [Test]
        public void Deliver_ReTalkAfterDelivered_ChoiceHidden()
        {
            // Counter-check: once delivered, the choice is gated out by
            // IfFact:hermit_message_delivered:<:1 — no re-delivery loop.
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
            var hermit = MakeHermit();
            var player = MakePlayer();

            VisibleChoiceTexts(hermit, player);
            SelectChoiceStartingWith("[Deliver");
            ConversationManager.EndConversation();

            var choices2 = VisibleChoiceTexts(hermit, player);
            Assert.IsFalse(choices2.Any(c => c.StartsWith("[Deliver")),
                "after delivering, the deliver choice must be hidden (IfFact:<:1 fails)");
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("hermit_message_delivered"),
                "the fact stays at 1 (idempotent — no double-delivery)");
        }
    }
}
