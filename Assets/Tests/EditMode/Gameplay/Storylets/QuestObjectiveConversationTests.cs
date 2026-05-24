using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q3.3 (Docs/QUEST-PARALLEL-OBJECTIVES.md) — conversation integration
    /// for parallel objectives: the <c>FinishObjective</c> action (incl.
    /// ~-delimited multi-finish, Qud parity) and the
    /// <c>IfObjectiveFinished</c> predicate (+ auto-inverse
    /// <c>IfNotObjectiveFinished</c>). Invoked through the same
    /// ConversationActions.Execute / ConversationPredicates.Evaluate
    /// entry points dialogue uses.
    /// </summary>
    public class QuestObjectiveConversationTests
    {
        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
        }

        [TearDown]
        public void TearDown()
        {
            ConversationActions.Reset();
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            StoryletPart.Current = null;
        }

        /// <summary>Register a quest whose stage 0 has the given objective IDs
        /// (all non-Optional), plus a trivial stage 1, start it, and install
        /// it as StoryletPart.Current (what the actions/predicates read).</summary>
        private static void SetupQuest(string questId, params string[] stage0Objectives)
        {
            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            var s0 = new QuestStageData { ID = "s0" };
            foreach (var o in stage0Objectives)
                s0.Objectives.Add(new QuestObjectiveData { ID = o });
            sd.Quest.Stages.Add(s0);
            sd.Quest.Stages.Add(new QuestStageData { ID = "s1" });
            StoryletRegistry.Register(sd);

            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = questId, CurrentStageIndex = 0 });
            StoryletPart.Current = part;
        }

        // ════════════════ FinishObjective action ════════════════

        [Test]
        public void Action_FinishObjective_FinishesSingleObjective()
        {
            SetupQuest("Q", "a", "b", "c");
            ConversationActions.Execute("FinishObjective", null, null, "Q:a");
            Assert.IsTrue(StoryletPart.Current.IsObjectiveFinished("Q", "a"));
            Assert.IsFalse(StoryletPart.Current.IsObjectiveFinished("Q", "b"));
        }

        [Test]
        public void Action_FinishObjective_MultiFinish_TildeDelimited()
        {
            // Qud FinishQuestStep("a~b") parity: one action finishes several
            // objectives. Use 3 objectives + finish 2 so the stage does NOT
            // advance (which would clear the set and hide the assertion).
            SetupQuest("Q", "a", "b", "c");
            ConversationActions.Execute("FinishObjective", null, null, "Q:a~b");
            Assert.IsTrue(StoryletPart.Current.IsObjectiveFinished("Q", "a"));
            Assert.IsTrue(StoryletPart.Current.IsObjectiveFinished("Q", "b"));
            Assert.IsFalse(StoryletPart.Current.IsObjectiveFinished("Q", "c"));
            Assert.AreEqual(0, StoryletPart.Current.GetQuestState("Q").CurrentStageIndex,
                "2 of 3 required done — stage must not advance");
        }

        [Test]
        public void Action_FinishObjective_AllRequired_AdvancesStage()
        {
            SetupQuest("Q", "a", "b");
            ConversationActions.Execute("FinishObjective", null, null, "Q:a~b");
            Assert.AreEqual(1, StoryletPart.Current.GetQuestState("Q").CurrentStageIndex,
                "finishing all required objectives via one action advances the stage");
        }

        [Test]
        public void Action_FinishObjective_MalformedArg_NoColon_NoOp()
        {
            // Defensive counter-check: an arg with no ':' separator is a
            // no-op (no objective finished, no throw).
            SetupQuest("Q", "a");
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("FinishObjective", null, null, "Q"));
            Assert.IsFalse(StoryletPart.Current.IsObjectiveFinished("Q", "a"));
        }

        // ════════════════ IfObjectiveFinished predicate ════════════════

        [Test]
        public void Predicate_IfObjectiveFinished_TrueWhenFinished()
        {
            SetupQuest("Q", "a", "b");
            ConversationActions.Execute("FinishObjective", null, null, "Q:a");
            Assert.IsTrue(ConversationPredicates.Evaluate("IfObjectiveFinished", null, null, "Q:a"));
        }

        [Test]
        public void Predicate_IfObjectiveFinished_FalseWhenNotFinished()
        {
            SetupQuest("Q", "a", "b");
            Assert.IsFalse(ConversationPredicates.Evaluate("IfObjectiveFinished", null, null, "Q:b"));
        }

        [Test]
        public void Predicate_IfNotObjectiveFinished_AutoInverse()
        {
            // The IfNot* mechanism auto-derives IfNotObjectiveFinished by
            // inverting IfObjectiveFinished — no separate registration.
            SetupQuest("Q", "a", "b");
            Assert.IsTrue(ConversationPredicates.Evaluate("IfNotObjectiveFinished", null, null, "Q:a"),
                "not finished yet -> IfNot is true");
            ConversationActions.Execute("FinishObjective", null, null, "Q:a");
            Assert.IsFalse(ConversationPredicates.Evaluate("IfNotObjectiveFinished", null, null, "Q:a"),
                "finished -> IfNot is false");
        }
    }
}
