using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q3.2 (Docs/QUEST-PARALLEL-OBJECTIVES.md) — the parallel-objective
    /// DISPATCH + API: <c>FinishObjective</c> (mark / run OnEnter / advance
    /// when all non-Optional done / idempotent) and the tick dispatch
    /// (objective-based stages finish eligible objectives; no-objective
    /// stages keep the legacy stage-trigger advance).
    /// </summary>
    public class QuestObjectiveDispatchTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            Diag.ResetAll();
            NarrativeStatePart.Current = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            NarrativeStatePart.Current = null;
        }

        // ── fixture helpers ──
        private static QuestObjectiveData Obj(string id, bool optional = false,
            List<ConversationParam> onEnter = null, params ConversationParam[] triggers)
        {
            var o = new QuestObjectiveData { ID = id, Optional = optional };
            if (triggers != null) o.Triggers.AddRange(triggers);
            if (onEnter != null) o.OnEnter.AddRange(onEnter);
            return o;
        }

        private static QuestStageData Stage(string id, params QuestObjectiveData[] objs)
        {
            var s = new QuestStageData { ID = id };
            if (objs != null) s.Objectives.AddRange(objs);
            return s;
        }

        private static StoryletPart PartWithQuest(string questId, params QuestStageData[] stages)
        {
            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            foreach (var s in stages) sd.Quest.Stages.Add(s);
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = questId, CurrentStageIndex = 0 });
            return part;
        }

        // ════════════════ FinishObjective ════════════════

        [Test]
        public void FinishObjective_MarksFinished_ReturnsTrue()
        {
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            Assert.IsTrue(part.FinishObjective("Q", "a"));
            Assert.IsTrue(part.IsObjectiveFinished("Q", "a"));
            Assert.IsFalse(part.IsObjectiveFinished("Q", "b"));
            Assert.AreEqual(0, part.GetQuestState("Q").CurrentStageIndex,
                "one of two required objectives done — stage must NOT advance yet");
        }

        [Test]
        public void FinishObjective_AllRequiredFinished_AdvancesStage_AndClearsObjectives()
        {
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            part.FinishObjective("Q", "a");
            part.FinishObjective("Q", "b");

            var st = part.GetQuestState("Q");
            Assert.AreEqual(1, st.CurrentStageIndex,
                "all required objectives done → stage advances");
            Assert.AreEqual(0, st.FinishedObjectives.Count,
                "FinishedObjectives is cleared for the new stage");
        }

        [Test]
        public void FinishObjective_OptionalUnfinished_StillAdvances()
        {
            // Counter-check: an Optional objective does NOT gate advancement.
            var part = PartWithQuest("Q",
                Stage("s0", Obj("required"), Obj("bonus", optional: true)), Stage("s1"));

            part.FinishObjective("Q", "required");

            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "stage advances when all NON-optional objectives are done, " +
                "even though the optional one is unfinished");
        }

        [Test]
        public void FinishObjective_AlreadyFinished_ReturnsFalse_Idempotent()
        {
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            Assert.IsTrue(part.FinishObjective("Q", "a"), "first finish succeeds");
            Assert.IsFalse(part.FinishObjective("Q", "a"),
                "re-finishing an already-finished objective is a no-op (false)");
        }

        [Test]
        public void FinishObjective_ObjectiveNotInCurrentStage_ReturnsFalse()
        {
            var part = PartWithQuest("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.IsFalse(part.FinishObjective("Q", "does_not_exist"),
                "an objective not in the current stage cannot be finished");
            Assert.IsFalse(part.FinishObjective("NoSuchQuest", "a"),
                "an inactive quest cannot have objectives finished");
        }

        [Test]
        public void FinishObjective_RunsObjectiveOnEnter()
        {
            // The objective's OnEnter effects run on finish (Qud per-step
            // reward parity). Use SetFact ("key:value") as an observable effect.
            NarrativeStatePart.Current = new NarrativeStatePart();
            var onEnter = new List<ConversationParam>
            { new ConversationParam { Key = "SetFact", Value = "found_relic:1" } };
            var part = PartWithQuest("Q", Stage("s0", Obj("a", false, onEnter)), Stage("s1"));

            part.FinishObjective("Q", "a");

            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact("found_relic"),
                "the objective's OnEnter effects must run when it finishes");
        }

        // ════════════════ Tick dispatch ════════════════

        [Test]
        public void OnTickEnd_EmptyTriggerObjective_FinishesAndAdvances()
        {
            // An objective with no Triggers is trivially eligible (CheckAll
            // of an empty list is true), so the tick finishes it; being the
            // only required objective, the stage then advances.
            var part = PartWithQuest("Q", Stage("s0", Obj("auto")), Stage("s1"));

            part.OnTickEnd(new NarrativeStatePart());

            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "tick finishes the eligible objective → stage advances");
        }

        [Test]
        public void OnTickEnd_NoObjectiveStage_LegacyStageAdvanceUnchanged()
        {
            // Back-compat counter-check: a stage with NO objectives still
            // advances via its own (empty → always-true) stage Triggers,
            // exactly as before Q3. The objective path must not regress it.
            var part = PartWithQuest("Q", Stage("s0"), Stage("s1"));
            // Sanity: s0 has no objectives.
            Assert.AreEqual(0, StoryletRegistry.FindQuest("Q").Stages[0].Objectives.Count);

            part.OnTickEnd(new NarrativeStatePart());

            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "a no-objective stage advances via stage Triggers (legacy path intact)");
        }
    }
}
