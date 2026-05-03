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
    /// QS.4 tests (Docs/QUEST-SYSTEM.md) for the quest stage-trigger
    /// dispatch loop in <see cref="StoryletPart.OnTickEnd"/> — the
    /// "M4 territory" placeholder finally landed.
    ///
    /// Inherits the M3 single-pass deterministic-dispatch contract:
    /// snapshot eligibility, then mutate. A quest stage advance whose
    /// OnEnter flips the next stage's Trigger does NOT cause the next
    /// stage to fire the same tick. Pinned by the
    /// SinglePass_DoesNotCascade counter-check.
    /// </summary>
    [TestFixture]
    public class QuestDispatchTests
    {
        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
            // Tick-driven AddFact effects need a Current narrative state.
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
        /// Register a 3-stage quest "Q1" with stages "intro", "fetch",
        /// "deliver". Each stage's Triggers + OnEnter are caller-supplied.
        /// </summary>
        private static void RegisterTestQuest(
            string questId,
            (List<ConversationParam> Triggers, List<ConversationParam> OnEnter) stage0,
            (List<ConversationParam> Triggers, List<ConversationParam> OnEnter) stage1,
            (List<ConversationParam> Triggers, List<ConversationParam> OnEnter) stage2)
        {
            var quest = new StoryletData
            {
                ID = questId,
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "intro",
                            Triggers = stage0.Triggers ?? new List<ConversationParam>(),
                            OnEnter = stage0.OnEnter ?? new List<ConversationParam>() },
                        new QuestStageData { ID = "fetch",
                            Triggers = stage1.Triggers ?? new List<ConversationParam>(),
                            OnEnter = stage1.OnEnter ?? new List<ConversationParam>() },
                        new QuestStageData { ID = "deliver",
                            Triggers = stage2.Triggers ?? new List<ConversationParam>(),
                            OnEnter = stage2.OnEnter ?? new List<ConversationParam>() },
                    },
                },
            };
            StoryletRegistry.Register(quest);
        }

        /// <summary>
        /// Build a Triggers list that requires fact `factKey` to be at
        /// least `min` using the existing IfFact predicate. The
        /// predicate uses "key:op:value" format — we always use ">=".
        /// </summary>
        private static List<ConversationParam> Trigger_IfFactAtLeast(string factKey, int min)
        {
            return new List<ConversationParam>
            {
                new ConversationParam
                {
                    Key = "IfFact",
                    Value = factKey + ":>=:" + min.ToString(),
                },
            };
        }

        // ====================================================================
        // 1. Stage trigger satisfied → advance + OnEnter fires
        // ====================================================================

        [Test]
        public void QuestStage_TriggersSatisfied_AdvanceWhenSatisfied_FiresOnEnter()
        {
            // Stage 0 trigger requires fact "ready"=1; OnEnter logs nothing.
            // Stage 1 OnEnter sets fact "stage1_entered"=1 — pinned by assertion.
            RegisterTestQuest("Q1",
                stage0: (Trigger_IfFactAtLeast("ready", 1), null),
                stage1: (null, new List<ConversationParam>
                {
                    new ConversationParam { Key = "AddFact", Value = "stage1_entered:1" },
                }),
                stage2: (null, null));

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            StoryletPart.Current = sp;
            // Set the fact that satisfies stage 0's trigger.
            NarrativeStatePart.Current.SetFact("ready", 1);

            sp.OnTickEnd(NarrativeStatePart.Current);

            var state = sp.GetQuestState("Q1");
            Assert.IsNotNull(state, "Quest must still be active.");
            Assert.AreEqual(1, state.CurrentStageIndex,
                "Stage 0 trigger satisfied → advance to stage 1.");
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("stage1_entered"),
                "Stage 1's OnEnter must fire on advance.");
        }

        // ====================================================================
        // 2. Stage trigger unsatisfied → NO advance
        // ====================================================================

        [Test]
        public void QuestStage_TriggersUnsatisfied_NoAdvance()
        {
            RegisterTestQuest("Q1",
                stage0: (Trigger_IfFactAtLeast("ready", 1), null),
                stage1: (null, null),
                stage2: (null, null));

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            StoryletPart.Current = sp;
            // Don't set the "ready" fact — trigger should fail.

            sp.OnTickEnd(NarrativeStatePart.Current);

            var state = sp.GetQuestState("Q1");
            Assert.AreEqual(0, state.CurrentStageIndex,
                "Trigger NOT satisfied → stage stays at 0.");
        }

        // ====================================================================
        // 3. Auto-complete at terminal stage
        // ====================================================================

        [Test]
        public void QuestStage_AutoCompletesAtTerminal()
        {
            RegisterTestQuest("Q1",
                stage0: (null, null),
                stage1: (null, null),
                stage2: (Trigger_IfFactAtLeast("done", 1), null));  // 3rd/last stage

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 2 });
            StoryletPart.Current = sp;
            NarrativeStatePart.Current.SetFact("done", 1);  // terminal trigger

            sp.OnTickEnd(NarrativeStatePart.Current);

            Assert.IsFalse(sp.IsQuestActive("Q1"),
                "Advancing past terminal stage must auto-complete.");
            Assert.IsTrue(sp.IsQuestCompleted("Q1"),
                "Quest must move to _completedQuests on auto-completion.");
        }

        // ====================================================================
        // 4. EnteredStageAtTurn updates on advance
        // ====================================================================

        [Test]
        public void QuestState_EnteredStageAtTurn_UpdatesOnAdvance()
        {
            RegisterTestQuest("Q1",
                stage0: (Trigger_IfFactAtLeast("ready", 1), null),
                stage1: (null, null),
                stage2: (null, null));

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 0,
                EnteredStageAtTurn = 5,  // initial
            });
            StoryletPart.Current = sp;
            NarrativeStatePart.Current.SetFact("ready", 1);

            sp.OnTickEnd(NarrativeStatePart.Current);

            var state = sp.GetQuestState("Q1");
            // EnteredStageAtTurn should have been refreshed to the
            // current TurnManager tick (or 0 if no TurnManager).
            // Either way, the OLD value (5) must NOT persist.
            Assert.AreNotEqual(5, state.EnteredStageAtTurn,
                "EnteredStageAtTurn must be refreshed when stage advances " +
                "(it tracks 'how long has the player been on this stage').");
        }

        // ====================================================================
        // 5. Single-pass deterministic dispatch — counter-check
        //
        // The most important QS.4 contract: advancing stage 0 must NOT
        // cascade into stage 1 the same tick, even if stage 0's OnEnter
        // flips stage 1's trigger. This preserves M3's deterministic-
        // dispatch invariant from the storylet path.
        //
        // Without this counter-check, a long quest chain could "instantly
        // resolve" in one tick if the OnEnter actions chain into each
        // other's triggers — surprising for content authors AND a
        // potential infinite-loop footgun.
        // ====================================================================

        [Test]
        public void QuestStage_DispatchSinglePass_DoesNotCascadeMultipleAdvancesPerTick()
        {
            // Pre-state: fact "go" >= 1 satisfies BOTH stage 0's AND
            // stage 1's trigger at tick 1 start. With single-pass
            // dispatch the eligibility snapshot only sees stage 0
            // (the CURRENT stage); after the advance, stage 1's
            // trigger is NOT re-evaluated this tick.
            //
            // If single-pass works:   tick 1 → 0→1 only;  tick 2 → 1→2.
            // If cascade bug exists:  tick 1 → 0→2 in one tick (the
            //                         dispatch keeps re-evaluating
            //                         the new current stage's trigger
            //                         after each advance).
            //
            // This shape avoids depending on stage 0's OnEnter firing
            // (which only happens via the StartQuest CONVERSATION
            // ACTION, not the StoryletPart.StartQuest method called
            // directly in this test). Pre-set facts give a cleaner
            // signal isolated from QS.3's startup path.
            RegisterTestQuest("Q1",
                stage0: (Trigger_IfFactAtLeast("go", 1), null),
                stage1: (Trigger_IfFactAtLeast("go", 1), null),
                stage2: (null, null));

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            StoryletPart.Current = sp;
            NarrativeStatePart.Current.SetFact("go", 1);

            // Tick 1: stage 0 trigger satisfied → advance 0→1.
            // Stage 1 trigger ALSO satisfied at this moment, but
            // single-pass must NOT cascade to advance 1→2 the same tick.
            sp.OnTickEnd(NarrativeStatePart.Current);

            var state = sp.GetQuestState("Q1");
            Assert.IsNotNull(state, "Quest must still be active after first advance.");
            Assert.AreEqual(1, state.CurrentStageIndex,
                "Single-pass dispatch: only the eligibility snapshot " +
                "from the top of the tick fires advances. Even though " +
                "stage 1's trigger was ALSO satisfied at tick start, " +
                "the post-advance cascade is forbidden. If this fails " +
                "(CurrentStageIndex == 2), the M4 dispatch is " +
                "cascading and breaks M3's deterministic-dispatch contract.");

            // Tick 2: stage 1's trigger is still satisfied. Snapshot
            // includes it. Advance 1→2.
            sp.OnTickEnd(NarrativeStatePart.Current);
            state = sp.GetQuestState("Q1");
            Assert.IsNotNull(state);
            Assert.AreEqual(2, state.CurrentStageIndex,
                "Tick 2: stage 1's trigger satisfied → advance to 2.");
        }

        // ====================================================================
        // 6. Multiple parallel quests advance independently in one tick
        // ====================================================================

        [Test]
        public void MultipleQuests_AdvanceIndependently_InOneTick()
        {
            RegisterTestQuest("Q1",
                stage0: (Trigger_IfFactAtLeast("a", 1), null),
                stage1: (null, null), stage2: (null, null));
            RegisterTestQuest("Q2",
                stage0: (Trigger_IfFactAtLeast("b", 1), null),
                stage1: (null, null), stage2: (null, null));

            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q1" });
            sp.StartQuest(new QuestState { QuestId = "Q2" });
            StoryletPart.Current = sp;
            NarrativeStatePart.Current.SetFact("a", 1);
            NarrativeStatePart.Current.SetFact("b", 1);

            sp.OnTickEnd(NarrativeStatePart.Current);

            Assert.AreEqual(1, sp.GetQuestState("Q1").CurrentStageIndex,
                "Q1 must advance (its trigger 'a' is satisfied).");
            Assert.AreEqual(1, sp.GetQuestState("Q2").CurrentStageIndex,
                "Q2 must advance independently (its trigger 'b' is satisfied).");
        }

        // ====================================================================
        // 7. Counter-check: no quest in registry → no advance, no crash
        // ====================================================================

        [Test]
        public void QuestDispatch_QuestActiveButNotInRegistry_NoCrashNoAdvance()
        {
            // StartQuest a quest ID with no registered QuestData.
            // (Adversarial: simulates a save-game that referenced a
            // since-removed quest.)
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState
            {
                QuestId = "NonexistentQuest",
                CurrentStageIndex = 0,
            });
            StoryletPart.Current = sp;

            Assert.DoesNotThrow(() =>
                sp.OnTickEnd(NarrativeStatePart.Current),
                "Tick dispatch must skip quests with no registered QuestData " +
                "(without crashing). Save-game forward-compat surface.");
            Assert.IsTrue(sp.IsQuestActive("NonexistentQuest"),
                "Quest stays active (the registry just couldn't resolve it " +
                "this tick — content might come back next session).");
        }
    }
}
