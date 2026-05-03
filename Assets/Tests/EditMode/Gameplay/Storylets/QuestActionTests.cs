using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// QS.3 tests (Docs/QUEST-SYSTEM.md) for the four quest-lifecycle
    /// conversation actions:
    ///
    ///   StartQuest(questId)
    ///   AdvanceQuestStage(questId)  — auto-completes at terminal stage
    ///   CompleteQuest(questId)      — explicit short-circuit
    ///   FailQuest(questId)          — drops from active without
    ///                                  recording (retakeable)
    ///
    /// Also pins the new "quest" diag channel: every successful
    /// lifecycle action emits a record (Started/StageAdvanced/
    /// Completed/Failed) so AI debugging can answer "did this
    /// quest ever progress?" via diag_query.
    ///
    /// Counter-checks for cross-state guards:
    ///   - StartQuest is no-op on already-active OR completed quests
    ///   - AdvanceQuestStage is no-op on never-started quests
    ///   - CompleteQuest is no-op on never-started quests
    /// </summary>
    [TestFixture]
    public class QuestActionTests
    {
        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
            // The AddFact action writes to NarrativeStatePart.Current's
            // FactBag — set up a fresh part so the writes don't crash
            // on a null Current.
            NarrativeStatePart.Current = new NarrativeStatePart();
        }

        [TearDown]
        public void TearDown()
        {
            StoryletPart.Current = null;
            NarrativeStatePart.Current = null;
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Register a 3-stage quest in StoryletRegistry under the
        /// given ID. Stage IDs are "intro", "fetch", "deliver".
        /// Each stage has an optional Effects[] list passed in.
        /// </summary>
        private static void RegisterTestQuest(
            string questId,
            List<ConversationParam> stage0OnEnter = null,
            List<ConversationParam> stage1OnEnter = null,
            List<ConversationParam> stage2OnEnter = null)
        {
            var quest = new StoryletData
            {
                ID = questId,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "intro",   OnEnter = stage0OnEnter ?? new List<ConversationParam>() },
                        new QuestStageData { ID = "fetch",   OnEnter = stage1OnEnter ?? new List<ConversationParam>() },
                        new QuestStageData { ID = "deliver", OnEnter = stage2OnEnter ?? new List<ConversationParam>() },
                    },
                },
            };
            StoryletRegistry.Register(quest);
        }

        // ====================================================================
        // 1. StartQuest — adds to active, fires stage-0 OnEnter immediately
        // ====================================================================

        [Test]
        public void StartQuest_AddsToActiveQuests()
        {
            RegisterTestQuest("Q1");
            StoryletPart.Current = new StoryletPart();

            ConversationActions.Execute("StartQuest", null, null, "Q1");

            Assert.IsTrue(StoryletPart.Current.IsQuestActive("Q1"),
                "After StartQuest, the quest must appear in _quests.");
            var state = StoryletPart.Current.GetQuestState("Q1");
            Assert.IsNotNull(state);
            Assert.AreEqual(0, state.CurrentStageIndex,
                "Fresh start must begin at stage 0.");
        }

        [Test]
        public void StartQuest_FiresStage0OnEnterImmediately()
        {
            // Pin a probe via the existing AddFact action: stage-0
            // OnEnter writes "stage0_entered:1" into the global
            // FactBag (NarrativeStatePart.Current set up in SetUp).
            // If StartQuest fires stage-0 OnEnter, the fact appears
            // immediately — no waiting for a tick.
            RegisterTestQuest("Q1",
                stage0OnEnter: new List<ConversationParam>
                {
                    new ConversationParam { Key = "AddFact", Value = "stage0_entered:1" },
                });
            StoryletPart.Current = new StoryletPart();

            ConversationActions.Execute("StartQuest", null, null, "Q1");

            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("stage0_entered"),
                "Stage-0 OnEnter effects must fire immediately on StartQuest, " +
                "not wait for the next tick. (Without this, content authors " +
                "couldn't reliably script the post-accept dialogue line.)");
        }

        [Test]
        public void StartQuest_OnAlreadyActive_NoOp()
        {
            RegisterTestQuest("Q1");
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 2,  // mid-quest
                EnteredStageAtTurn = 5,
            });
            StoryletPart.Current = sp;

            ConversationActions.Execute("StartQuest", null, null, "Q1");

            // Stage index must NOT have been reset to 0 — that would
            // forfeit progress. The action must short-circuit on
            // already-active quests.
            var state = StoryletPart.Current.GetQuestState("Q1");
            Assert.AreEqual(2, state.CurrentStageIndex,
                "StartQuest on an already-active quest must NOT reset " +
                "progress to stage 0.");
        }

        [Test]
        public void StartQuest_OnAlreadyCompleted_NoOp()
        {
            RegisterTestQuest("Q1");
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1" });
            sp.MarkQuestCompleted("Q1");
            StoryletPart.Current = sp;

            ConversationActions.Execute("StartQuest", null, null, "Q1");

            Assert.IsFalse(StoryletPart.Current.IsQuestActive("Q1"),
                "StartQuest on a completed quest must NOT re-add it " +
                "to active. Players can't re-take finished quests.");
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("Q1"),
                "Completed-set membership unchanged.");
        }

        // ====================================================================
        // 2. AdvanceQuestStage — index increments, OnEnter fires
        // ====================================================================

        [Test]
        public void AdvanceQuestStage_IncrementsStageIndex()
        {
            RegisterTestQuest("Q1");
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 0,
                EnteredStageAtTurn = 5,
            });
            StoryletPart.Current = sp;

            ConversationActions.Execute("AdvanceQuestStage", null, null, "Q1");

            var state = StoryletPart.Current.GetQuestState("Q1");
            Assert.IsNotNull(state, "Quest must still be active after stage advance.");
            Assert.AreEqual(1, state.CurrentStageIndex,
                "AdvanceQuestStage must bump CurrentStageIndex by 1.");
        }

        [Test]
        public void AdvanceQuestStage_FiresNewStageOnEnter()
        {
            // Probe: stage 1's OnEnter sets a fact. Should fire on advance.
            RegisterTestQuest("Q1",
                stage1OnEnter: new List<ConversationParam>
                {
                    new ConversationParam { Key = "AddFact", Value = "stage1_entered:1" },
                });

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            StoryletPart.Current = sp;

            ConversationActions.Execute("AdvanceQuestStage", null, null, "Q1");

            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("stage1_entered"),
                "AdvanceQuestStage must fire the NEW stage's OnEnter effects.");
        }

        [Test]
        public void AdvanceQuestStage_AtTerminalStage_AutoCompletes()
        {
            RegisterTestQuest("Q1");  // 3 stages: 0, 1, 2
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 2,  // at terminal stage
            });
            StoryletPart.Current = sp;

            ConversationActions.Execute("AdvanceQuestStage", null, null, "Q1");

            Assert.IsFalse(StoryletPart.Current.IsQuestActive("Q1"),
                "Advancing past the terminal stage must auto-complete " +
                "(quest moves out of _quests).");
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("Q1"),
                "Auto-completion must move the quest into _completedQuests.");
        }

        // ====================================================================
        // 3. CompleteQuest — explicit short-circuit
        // ====================================================================

        [Test]
        public void CompleteQuest_RemovesFromActive_AddsToCompleted()
        {
            RegisterTestQuest("Q1");
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            StoryletPart.Current = sp;

            ConversationActions.Execute("CompleteQuest", null, null, "Q1");

            Assert.IsFalse(StoryletPart.Current.IsQuestActive("Q1"),
                "CompleteQuest must remove from _quests.");
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("Q1"),
                "CompleteQuest must add to _completedQuests.");
        }

        // ====================================================================
        // 4. FailQuest — removes from active without completing (retakeable)
        // ====================================================================

        [Test]
        public void FailQuest_RemovesFromActive_NotInCompleted()
        {
            RegisterTestQuest("Q1");
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 1 });
            StoryletPart.Current = sp;

            ConversationActions.Execute("FailQuest", null, null, "Q1");

            Assert.IsFalse(StoryletPart.Current.IsQuestActive("Q1"),
                "FailQuest must remove from _quests.");
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("Q1"),
                "FailQuest must NOT add to _completedQuests — failed " +
                "quests are retakeable in v1 (per Docs/QUEST-SYSTEM.md " +
                "self-review 🟡).");
            // Belt-and-braces: the predicate side should report the
            // quest as "not started" again.
            Assert.IsTrue(
                ConversationPredicates.Evaluate(
                    "IfQuestNotStarted", null, null, "Q1"),
                "After FailQuest, IfQuestNotStarted must return true " +
                "(the player can re-take the quest from a quest-giver).");
        }

        // ====================================================================
        // 5. Diag observability — the new "quest" channel records every
        //    lifecycle action. Without these, AI debugging can't answer
        //    "did this quest ever progress?" without instrumentation.
        // ====================================================================

        [Test]
        public void StartQuest_RecordsQuestStartedDiag()
        {
            RegisterTestQuest("Q1");
            StoryletPart.Current = new StoryletPart();
            Diag.ResetAll();

            ConversationActions.Execute("StartQuest", null, null, "Q1");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Kind = "Started",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count,
                $"Exactly one quest/Started diag record per StartQuest. " +
                $"Got {records.Count}.");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"questId\":\"Q1\""),
                $"Payload must include questId=Q1. " +
                $"Payload: {records[0].PayloadJson}");
        }

        [Test]
        public void AdvanceQuestStage_RecordsStageAdvancedDiag()
        {
            RegisterTestQuest("Q1");
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            StoryletPart.Current = sp;
            Diag.ResetAll();

            ConversationActions.Execute("AdvanceQuestStage", null, null, "Q1");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "quest",
                Kind = "StageAdvanced",
                Limit = 10,
            }).Records;

            Assert.AreEqual(1, records.Count);
            Assert.IsTrue(records[0].PayloadJson.Contains("\"fromIndex\":0"),
                $"Payload must include fromIndex. Payload: {records[0].PayloadJson}");
            Assert.IsTrue(records[0].PayloadJson.Contains("\"toIndex\":1"),
                $"Payload must include toIndex. Payload: {records[0].PayloadJson}");
        }

        // ====================================================================
        // 6. Pre-bootstrap (StoryletPart.Current == null) defensive paths
        // ====================================================================

        [Test]
        public void QuestActions_BeforeBootstrap_AreNoOps()
        {
            StoryletPart.Current = null;

            // None of these should throw or crash. They just no-op.
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("StartQuest", null, null, "Q1"));
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("AdvanceQuestStage", null, null, "Q1"));
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("CompleteQuest", null, null, "Q1"));
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("FailQuest", null, null, "Q1"));
        }
    }
}
