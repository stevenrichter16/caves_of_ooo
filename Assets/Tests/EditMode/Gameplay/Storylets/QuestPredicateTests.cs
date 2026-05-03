using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// QS.2 tests (Docs/QUEST-SYSTEM.md) for the four new
    /// quest-related conversation predicates:
    ///
    ///   IfQuestActive(questId)           — in StoryletPart._quests
    ///   IfQuestCompleted(questId)        — in _completedQuests
    ///   IfQuestNotStarted(questId)       — in NEITHER (disjoint check)
    ///   IfQuestStage(questId:stageRef)   — current stage matches
    ///                                       index OR stage ID
    ///
    /// All predicates evaluate against <see cref="StoryletPart.Current"/>
    /// — pre-bootstrap (Current == null) every predicate fails closed
    /// EXCEPT IfQuestNotStarted (which returns true since nothing
    /// has been started yet — matches the "first NPC interaction
    /// after world gen offers all possible quests" semantics).
    /// </summary>
    [TestFixture]
    public class QuestPredicateTests
    {
        [SetUp]
        public void SetUp()
        {
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            // Each test installs StoryletPart.Current itself (or leaves
            // it null to test the pre-bootstrap path).
            StoryletPart.Current = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletPart.Current = null;
        }

        // ====================================================================
        // 1. IfQuestActive — true when quest started, false otherwise
        // ====================================================================

        [Test]
        public void IfQuestActive_ReturnsTrue_WhenQuestStarted()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 0,
                EnteredStageAtTurn = 5,
            });
            StoryletPart.Current = sp;

            Assert.IsTrue(
                ConversationPredicates.Evaluate("IfQuestActive", null, null, "Q1"),
                "IfQuestActive must return true when the quest is in _quests.");
        }

        [Test]
        public void IfQuestActive_ReturnsFalse_WhenQuestNotStarted()
        {
            StoryletPart.Current = new StoryletPart();
            // No StartQuest call.
            Assert.IsFalse(
                ConversationPredicates.Evaluate("IfQuestActive", null, null, "Q1"),
                "IfQuestActive must return false on never-started quests.");
        }

        // ====================================================================
        // 2. IfQuestStage — numeric index AND stage-ID lookup
        // ====================================================================

        [Test]
        public void IfQuestStage_ReturnsTrue_WhenIndexMatches()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 2,
                EnteredStageAtTurn = 5,
            });
            StoryletPart.Current = sp;

            Assert.IsTrue(
                ConversationPredicates.Evaluate("IfQuestStage", null, null, "Q1:2"),
                "IfQuestStage must match by numeric stage index.");
            Assert.IsFalse(
                ConversationPredicates.Evaluate("IfQuestStage", null, null, "Q1:1"),
                "Counter-check: non-matching index returns false.");
        }

        [Test]
        public void IfQuestStage_ReturnsTrue_WhenStageIdMatches()
        {
            // Register a quest with 3 named stages so the predicate
            // can resolve the stage-ID string back to its index.
            var quest = new StoryletData
            {
                ID = "Q1",
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "intro" },
                        new QuestStageData { ID = "fetch_key" },
                        new QuestStageData { ID = "deliver" },
                    },
                },
            };
            StoryletRegistry.Register(quest);

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 1,  // "fetch_key"
                EnteredStageAtTurn = 5,
            });
            StoryletPart.Current = sp;

            Assert.IsTrue(
                ConversationPredicates.Evaluate(
                    "IfQuestStage", null, null, "Q1:fetch_key"),
                "IfQuestStage must match the stage's ID at the current index.");
            Assert.IsFalse(
                ConversationPredicates.Evaluate(
                    "IfQuestStage", null, null, "Q1:intro"),
                "Counter-check: stage-ID at a different index returns false.");
            Assert.IsFalse(
                ConversationPredicates.Evaluate(
                    "IfQuestStage", null, null, "Q1:nonexistent_stage"),
                "Counter-check: unknown stage ID returns false (no false positive).");
        }

        // ====================================================================
        // 3. IfQuestNotStarted — true ONLY if neither active NOR completed
        // ====================================================================

        [Test]
        public void IfQuestNotStarted_ReturnsTrue_WhenNeverStarted()
        {
            StoryletPart.Current = new StoryletPart();

            Assert.IsTrue(
                ConversationPredicates.Evaluate(
                    "IfQuestNotStarted", null, null, "Q1"),
                "IfQuestNotStarted must return true when neither active " +
                "nor completed.");
        }

        [Test]
        public void IfQuestNotStarted_ReturnsFalse_WhenActive()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1" });
            StoryletPart.Current = sp;

            Assert.IsFalse(
                ConversationPredicates.Evaluate(
                    "IfQuestNotStarted", null, null, "Q1"),
                "Active quests are not 'not-started' — predicate must " +
                "return false.");
        }

        // ====================================================================
        // 4. IfQuestCompleted — true after MarkQuestCompleted
        // ====================================================================

        [Test]
        public void IfQuestCompleted_ReturnsTrue_AfterCompletion()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1" });
            sp.MarkQuestCompleted("Q1");
            StoryletPart.Current = sp;

            Assert.IsTrue(
                ConversationPredicates.Evaluate(
                    "IfQuestCompleted", null, null, "Q1"),
                "IfQuestCompleted must return true after MarkQuestCompleted.");
        }

        // ====================================================================
        // 5. Counter-check: a completed quest is NOT active (disjoint sets)
        //
        // This is the most important counter-check in QS.2 — it pins the
        // contract that the active and completed sets are disjoint.
        // Without this, a bug that left completed quests in _quests would
        // sail through every other test (since all the IfQuestActive
        // tests above assert true on freshly-started quests, never
        // touching the completion path).
        // ====================================================================

        [Test]
        public void IfQuestActive_AfterCompletion_ReturnsFalse()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1" });
            sp.MarkQuestCompleted("Q1");
            StoryletPart.Current = sp;

            Assert.IsFalse(
                ConversationPredicates.Evaluate(
                    "IfQuestActive", null, null, "Q1"),
                "After completion, the quest must NOT be active. Active " +
                "and completed sets must be disjoint.");

            // Belt-and-braces: also covers IfQuestNotStarted's behavior
            // on completed quests (which is the whole reason
            // IfQuestNotStarted is distinct from IfNotQuestActive).
            Assert.IsFalse(
                ConversationPredicates.Evaluate(
                    "IfQuestNotStarted", null, null, "Q1"),
                "Completed quests are not 'not-started' — predicate " +
                "must return false. This is the semantic difference " +
                "between IfQuestNotStarted and the auto-inverse " +
                "IfNotQuestActive.");
        }

        // ====================================================================
        // 6. Pre-bootstrap (StoryletPart.Current == null) defensive paths
        // ====================================================================

        [Test]
        public void QuestPredicates_BeforeBootstrap_FailClosedExceptNotStarted()
        {
            StoryletPart.Current = null;

            Assert.IsFalse(
                ConversationPredicates.Evaluate("IfQuestActive", null, null, "Q1"),
                "Pre-bootstrap IfQuestActive must fail closed (return false).");
            Assert.IsFalse(
                ConversationPredicates.Evaluate("IfQuestCompleted", null, null, "Q1"),
                "Pre-bootstrap IfQuestCompleted must fail closed.");
            Assert.IsFalse(
                ConversationPredicates.Evaluate("IfQuestStage", null, null, "Q1:0"),
                "Pre-bootstrap IfQuestStage must fail closed.");
            Assert.IsTrue(
                ConversationPredicates.Evaluate("IfQuestNotStarted", null, null, "Q1"),
                "Pre-bootstrap IfQuestNotStarted must return TRUE (nothing " +
                "has been started yet — semantically correct).");
        }
    }
}
