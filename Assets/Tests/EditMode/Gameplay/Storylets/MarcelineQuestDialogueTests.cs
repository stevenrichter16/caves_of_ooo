using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests for the Marceline_Quest conversation tree shipped with
    /// QuestShowcase. Pins the dialogue gating contract:
    ///
    ///   - "[Take the quest]" choice visible iff IfQuestNotStarted
    ///   - "[Status]" choice visible iff IfQuestStage:0
    ///   - "[Hand over the key]" choice visible iff IfQuestStage:1
    ///   - "[Already finished]" choice visible iff IfQuestCompleted
    ///
    /// These wire the QS.2 predicate dispatch to the QS.3 action
    /// dispatch through the actual ConversationManager.SelectChoice
    /// path — verifying the player flow works end-to-end without
    /// the test bypassing dialogue (which the original
    /// QuestShowcaseDiagTests did).
    /// </summary>
    [TestFixture]
    public class MarcelineQuestDialogueTests
    {
        private const string QuestId = "IronKeyShowcaseQuest";

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

            // Load the actual shipped JSON from disk so the test
            // exercises the same content content authors will hit.
            string convPath = Path.Combine(
                "Assets", "Resources", "Content", "Conversations",
                "Marceline_Quest.json");
            string convJson = File.ReadAllText(convPath);
            ConversationLoader.LoadFromJson(convJson, "Marceline_Quest.json");

            // Seed the quest definition so IfQuestStage's stage-ID
            // lookup can resolve. (Production: a JSON storylet file
            // in Resources/Content/Data/Storylets/ would auto-load,
            // but the test seeds inline for self-containedness.)
            StoryletRegistry.Register(new StoryletData
            {
                ID = QuestId,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "find_iron_key" },
                        new QuestStageData { ID = "deliver_to_marceline" },
                    },
                },
            });
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static (Entity speaker, Entity listener) MakeSpeakerListener()
        {
            var listener = new Entity { BlueprintName = "Player" };
            listener.Tags["Player"] = "";
            listener.AddPart(new InventoryPart());

            var speaker = new Entity { BlueprintName = "Marceline" };
            speaker.AddPart(new ConversationPart { ConversationID = "Marceline_Quest" });
            return (speaker, listener);
        }

        private static List<string> StartConversationVisibleChoiceTexts(
            Entity speaker, Entity listener)
        {
            ConversationManager.StartConversation(speaker, listener);
            return ConversationManager.VisibleChoices
                .Select(c => c.Text).ToList();
        }

        // ====================================================================
        // 1. Pristine state — only "[Take the quest]" appears
        //    (plus the always-on "Bye." farewell)
        // ====================================================================

        [Test]
        public void Start_BeforeQuestTaken_OnlyTakeQuestChoiceVisible()
        {
            var (speaker, listener) = MakeSpeakerListener();
            var choices = StartConversationVisibleChoiceTexts(speaker, listener);

            Assert.IsTrue(choices.Any(c => c.StartsWith("[Take the quest")),
                $"[Take the quest] choice must be visible before the quest " +
                $"is started. Got choices: [{string.Join(",", choices)}]");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Status]")),
                "Status choice must be HIDDEN before the quest is started.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Hand over")),
                "Hand-over choice must be HIDDEN before the quest is started.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Already finished")),
                "Already-finished choice must be HIDDEN before the quest is started.");
        }

        // ====================================================================
        // 2. Stage 0 (looking for key) — Status choice appears, others hide
        // ====================================================================

        [Test]
        public void Start_AtStage0_StatusChoiceVisible_OthersHidden()
        {
            StoryletPart.Current.StartQuest(new QuestState
            {
                QuestId = QuestId, CurrentStageIndex = 0,
            });

            var (speaker, listener) = MakeSpeakerListener();
            var choices = StartConversationVisibleChoiceTexts(speaker, listener);

            Assert.IsTrue(choices.Any(c => c.StartsWith("[Status]")),
                "Status choice must be visible at stage 0 (player is looking).");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Take the quest")),
                "Take-quest choice must be HIDDEN once the quest is active " +
                "(IfQuestNotStarted excludes active quests).");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Hand over")),
                "Hand-over choice must be HIDDEN at stage 0.");
        }

        // ====================================================================
        // 3. Stage 1 (delivery) — Hand-over choice appears
        // ====================================================================

        [Test]
        public void Start_AtStage1_HandOverChoiceVisible()
        {
            StoryletPart.Current.StartQuest(new QuestState
            {
                QuestId = QuestId, CurrentStageIndex = 1,
            });

            var (speaker, listener) = MakeSpeakerListener();
            var choices = StartConversationVisibleChoiceTexts(speaker, listener);

            Assert.IsTrue(choices.Any(c => c.StartsWith("[Hand over")),
                "Hand-over choice must be visible at stage 1.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Status]")),
                "Status choice (stage 0) must be HIDDEN once at stage 1.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Take the quest")),
                "Take-quest choice must remain HIDDEN.");
        }

        // ====================================================================
        // 4. Completed — Already-finished choice appears
        // ====================================================================

        [Test]
        public void Start_AfterCompletion_AlreadyFinishedChoiceVisible()
        {
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId });
            StoryletPart.Current.MarkQuestCompleted(QuestId);

            var (speaker, listener) = MakeSpeakerListener();
            var choices = StartConversationVisibleChoiceTexts(speaker, listener);

            Assert.IsTrue(choices.Any(c => c.StartsWith("[Already finished")),
                "Already-finished choice must be visible after completion.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Take the quest")),
                "Players cannot re-take a finished quest — Take-quest hidden.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Status]")),
                "No active stage 0 — Status hidden.");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Hand over")),
                "No active stage 1 — Hand-over hidden.");
        }

        // ====================================================================
        // 5. End-to-end via dialogue: SelectChoice on "[Take the quest]"
        //    actually fires StartQuest → quest becomes active
        // ====================================================================

        [Test]
        public void SelectTakeQuestChoice_FiresStartQuestAction()
        {
            var (speaker, listener) = MakeSpeakerListener();
            StartConversationVisibleChoiceTexts(speaker, listener);

            // Find the index of "[Take the quest]" in visible choices.
            int idx = -1;
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text
                        .StartsWith("[Take the quest"))
                {
                    idx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(idx, 0,
                "Take-quest choice must be present pre-selection.");

            ConversationManager.SelectChoice(idx);

            Assert.IsTrue(StoryletPart.Current.IsQuestActive(QuestId),
                "Selecting [Take the quest] must invoke StartQuest action " +
                "via the conversation system → quest becomes active.");
        }

        // ====================================================================
        // 6. End-to-end: SelectChoice on "[Hand over the key]" advances
        //    the quest past terminal → auto-completes
        // ====================================================================

        [Test]
        public void SelectHandOverChoice_FiresAdvanceQuestStage_AutoCompletes()
        {
            // Pre-state: quest at stage 1.
            StoryletPart.Current.StartQuest(new QuestState
            {
                QuestId = QuestId, CurrentStageIndex = 1,
            });

            var (speaker, listener) = MakeSpeakerListener();
            StartConversationVisibleChoiceTexts(speaker, listener);

            int idx = -1;
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text
                        .StartsWith("[Hand over"))
                {
                    idx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(idx, 0,
                "Hand-over choice must be present at stage 1.");

            ConversationManager.SelectChoice(idx);

            Assert.IsFalse(StoryletPart.Current.IsQuestActive(QuestId),
                "[Hand over] selection must advance past terminal → auto-complete.");
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(QuestId),
                "Quest must be in _completedQuests after the dialogue completion.");
        }
    }
}
