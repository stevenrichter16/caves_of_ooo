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
    /// End-to-end dialogue tests for "Strongest in Ooo" (SM6,
    /// Docs/QUEST-POOL-EXPANSION.md) — the STAT-GATED pool quest. The lift
    /// choice is gated IfStatAtLeast:Strength:18; the "too weak" branch uses
    /// the auto-inverse IfNotStatAtLeast (come back stronger — no soft-lock).
    /// Completion rewards faction reputation (ChangeFactionFeeling →
    /// PlayerReputation) on top of XP/drams — first world use of both a stat
    /// gate and a rep reward in a quest. Verified through the real
    /// ConversationManager.SelectChoice path.
    /// </summary>
    [TestFixture]
    public class QuestStrongmanDialogueTests
    {
        private const string QuestId = "StrongestInOoo";

        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            ConversationLoader.Reset();
            StoryletRegistry.Reset();
            PlayerReputation.Reset();
            ConversationManager.EndConversation();
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = new NarrativeStatePart();

            string convPath = Path.Combine(
                "Assets", "Resources", "Content", "Conversations", "Strongman_Quest.json");
            ConversationLoader.LoadFromJson(File.ReadAllText(convPath), "Strongman_Quest.json");

            StoryletRegistry.Register(new StoryletData
            {
                ID = QuestId,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "prove" },
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
            PlayerReputation.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
        }

        private static (Entity speaker, Entity player) MakePair(int playerStrength)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.AddPart(new InventoryPart());
            // A bare Entity has no stats; SetStatValue only updates EXISTING
            // stats. Create Strength so IfStatAtLeast reads it (a real player
            // has this from its blueprint).
            player.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = playerStrength, Min = 1, Max = 50 };

            var speaker = new Entity { BlueprintName = "Strongman" };
            speaker.AddPart(new ConversationPart { ConversationID = "Strongman_Quest" });
            return (speaker, player);
        }

        private static List<string> VisibleChoiceTexts(Entity speaker, Entity player)
        {
            ConversationManager.StartConversation(speaker, player);
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

        // ════════════════ gating ════════════════

        [Test]
        public void Start_BeforeAccept_OffersAcceptOnly()
        {
            var (speaker, player) = MakePair(20);
            var choices = VisibleChoiceTexts(speaker, player);
            Assert.IsTrue(choices.Any(c => c.StartsWith("[Accept")),
                "a fresh quest offers Accept");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[Lift")),
                "the lift choice is hidden before accepting (gated to the prove stage)");
        }

        [Test]
        public void AtProveStage_TooWeak_ShowsComeBackStronger_NotLift()
        {
            // Counter-check: a weak player (Str 16 < 18) sees the come-back
            // branch (IfNotStatAtLeast), NOT the lift — and the quest is NOT
            // soft-locked (they can return after training).
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
            var (speaker, player) = MakePair(16);
            var choices = VisibleChoiceTexts(speaker, player);

            Assert.IsFalse(choices.Any(c => c.StartsWith("[Lift")),
                "Strength 16 < 18 → the lift choice must be HIDDEN (IfStatAtLeast gate)");
            Assert.IsTrue(choices.Any(c => c.StartsWith("[I'm not strong")),
                "a too-weak player sees the come-back-stronger branch (IfNotStatAtLeast auto-inverse)");
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(QuestId),
                "no soft-lock: the quest stays active for the player to return stronger");
        }

        [Test]
        public void AtProveStage_StrongEnough_LiftShown_SetsTheFact()
        {
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 0 });
            var (speaker, player) = MakePair(18); // exactly the threshold (inclusive)
            var choices = VisibleChoiceTexts(speaker, player);

            Assert.IsTrue(choices.Any(c => c.StartsWith("[Lift")),
                "Strength 18 >= 18 → the lift choice appears (threshold inclusive)");
            Assert.IsFalse(choices.Any(c => c.StartsWith("[I'm not strong")),
                "a strong player does NOT see the come-back branch");

            Assert.IsTrue(SelectChoiceStartingWith("[Lift"));
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("strongman_lifted"),
                "lifting sets the fact the prove objective polls (advances to report)");
        }

        // ════════════════ reward (XP/drams + faction reputation) ════════════════

        [Test]
        public void AtReportStage_Report_CompletesAndGrantsReputation()
        {
            StoryletPart.Current.StartQuest(new QuestState { QuestId = QuestId, CurrentStageIndex = 1 });
            var (speaker, player) = MakePair(20);
            int repBefore = PlayerReputation.Get("SaccharineConcord");

            var choices = VisibleChoiceTexts(speaker, player);
            Assert.IsTrue(choices.Any(c => c.StartsWith("[Report")),
                "at the report stage the report choice is available");

            Assert.IsTrue(SelectChoiceStartingWith("[Report"));

            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(QuestId),
                "reporting completes the quest");
            Assert.Greater(PlayerReputation.Get("SaccharineConcord"), repBefore,
                "completion grants faction reputation (ChangeFactionFeeling → PlayerReputation)");
        }
    }
}
