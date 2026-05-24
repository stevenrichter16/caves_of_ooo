using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// HYPOTHESIS-DRIVEN DEEP AUDIT of the quest/objective system
    /// (CLAUDE.md → "Hypothesis-driven deep audit"). After Q1–Q7 + Q3.5
    /// shipped with green per-phase suites and a clean cold-eye pass, these
    /// tests probe PLAYER-FLOW scenarios the per-phase tests don't simulate —
    /// the gaps that hide between systems (Apply ↔ dispatch ↔ save/load ↔
    /// events ↔ completion). Each test states the hypothesis it probes.
    ///
    /// Classification (filled after the run):
    /// - All 9 went GREEN on first run → pinned-as-correct invariants
    ///   (permanent regression infrastructure). 0 confirmed bugs. The value
    ///   is the counter-checks the per-phase suites lacked — e.g. the
    ///   no-double-reward and no-phantom-StageAdvanced invariants would let
    ///   a plausible refactor regress silently without these pins.
    /// </summary>
    public class QuestObjectiveHypothesisTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            Diag.ResetAll();
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        // ── fixture helpers (mirror QuestObjectiveDispatchTests) ──

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

        private static List<ConversationParam> SetFactOnEnter(string keyVal)
            => new List<ConversationParam> { new ConversationParam { Key = "SetFact", Value = keyVal } };

        /// <summary>Register the quest + start it. No LocalPlayer ⇒ quest
        /// GameEvents are skipped (FireQuestEvent null-guards), which is fine
        /// for state/effect tests.</summary>
        private static StoryletPart PartWithQuest(string questId, params QuestStageData[] stages)
        {
            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            foreach (var s in stages) sd.Quest.Stages.Add(s);
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            StoryletPart.Current = part;
            part.StartQuest(new QuestState { QuestId = questId, CurrentStageIndex = 0 });
            return part;
        }

        /// <summary>Captures quest GameEvents fired on its entity.</summary>
        private sealed class QuestEventCapture : Part
        {
            public override string Name => "QuestEventCapture";
            public readonly List<string> Events = new List<string>();
            public override bool WantEvent(int eventID) => true;
            public override bool HandleEvent(GameEvent e)
            {
                switch (e.ID)
                {
                    case "QuestStarted":
                    case "QuestObjectiveFinished":
                    case "QuestStageAdvanced":
                    case "QuestCompleted":
                    case "QuestFailed":
                        Events.Add(e.ID);
                        break;
                }
                return true;
            }
            public int Count(string id) => Events.Count(x => x == id);
        }

        /// <summary>Register quest + install a capturing LocalPlayer. Does
        /// NOT start the quest (the test does, so QuestStarted is observable).</summary>
        private static (StoryletPart part, QuestEventCapture cap) PartWithCapture(
            string questId, params QuestStageData[] stages)
        {
            var player = new Entity { ID = "player", BlueprintName = "Player" };
            var cap = new QuestEventCapture();
            player.AddPart(cap);
            StoryletPart.LocalPlayer = player;

            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            foreach (var s in stages) sd.Quest.Stages.Add(s);
            StoryletRegistry.Register(sd);

            var part = new StoryletPart();
            StoryletPart.Current = part;
            return (part, cap);
        }

        private static StoryletPart RoundTrip(StoryletPart part)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            part.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = new StoryletPart();
            loaded.Load(reader);
            return loaded;
        }

        // ════════════════ H6 — anti-exploit: no double reward ════════════════

        [Test]
        public void Hypothesis_RefinishObjective_DoesNotRerunOnEnter()
        {
            // HYPOTHESIS: a player who hits a finished objective's completion
            // path twice (re-enters a conversation node, re-triggers the kill)
            // must NOT collect its OnEnter reward twice. Probes whether the
            // idempotency guard (FinishedObjectives.Add) sits BEFORE the
            // OnEnter execution. The existing idempotency test only asserts
            // the bool return — a refactor that ran OnEnter before the dedup
            // check would double-reward and still pass it.
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            // Two required objectives so finishing "a" does NOT advance/clear —
            // "a" stays in the finished set, exercising the re-finish no-op.
            var part = PartWithQuest("Q",
                Stage("s0", Obj("a", false, SetFactOnEnter("reward:1")), Obj("b")),
                Stage("s1"));

            Assert.IsTrue(part.FinishObjective("Q", "a"), "first finish succeeds");
            Assert.AreEqual(1, ns.GetFact("reward"), "OnEnter ran exactly once");

            ns.SetFact("reward", 0); // isolate: clear the marker
            Assert.IsFalse(part.FinishObjective("Q", "a"), "re-finish is a no-op (false)");
            Assert.AreEqual(0, ns.GetFact("reward"),
                "re-finishing must NOT re-run OnEnter — no double reward (anti-exploit)");
        }

        [Test]
        public void Hypothesis_OptionalObjectiveOnEnter_RunsExactlyOnceOnFinish()
        {
            // HYPOTHESIS: an OPTIONAL objective's OnEnter reward fires when it
            // is finished (even though optional objectives don't gate stage
            // advance) and fires exactly once, same as a required one.
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var part = PartWithQuest("Q",
                Stage("s0", Obj("opt", true, SetFactOnEnter("bonus:1")), Obj("a")),
                Stage("s1"));

            Assert.IsTrue(part.FinishObjective("Q", "opt"), "optional finishes");
            Assert.AreEqual(1, ns.GetFact("bonus"), "optional OnEnter ran once");
            Assert.AreEqual(0, part.GetQuestState("Q").CurrentStageIndex,
                "optional finish does not advance the stage");

            ns.SetFact("bonus", 0);
            Assert.IsFalse(part.FinishObjective("Q", "opt"), "re-finish optional is a no-op");
            Assert.AreEqual(0, ns.GetFact("bonus"), "optional OnEnter does not re-run");
        }

        // ════════════════ H8 — completion event stream (no phantom) ════════════════

        [Test]
        public void Hypothesis_TerminalStageLastObjective_FiresCompleted_NotPhantomStageAdvanced()
        {
            // HYPOTHESIS: finishing the last required objective of the TERMINAL
            // stage completes the quest, firing QuestCompleted but NOT a
            // phantom QuestStageAdvanced for the past-terminal index. (The
            // per-phase test only covers the NON-terminal advance.)
            var (part, cap) = PartWithCapture("Q", Stage("s0", Obj("a")));
            part.StartQuest(new QuestState { QuestId = "Q" });

            part.FinishObjective("Q", "a"); // last required of the only (terminal) stage

            Assert.AreEqual(1, cap.Count("QuestObjectiveFinished"), "objective finished fires once");
            Assert.AreEqual(1, cap.Count("QuestCompleted"), "quest completes");
            Assert.AreEqual(0, cap.Count("QuestStageAdvanced"),
                "no phantom QuestStageAdvanced for the past-terminal index");
            Assert.IsTrue(part.IsQuestCompleted("Q"));
        }

        [Test]
        public void Hypothesis_NonTerminalLastObjective_FiresStageAdvanced_NotCompleted()
        {
            // COUNTER-CHECK to the terminal case: finishing the last required
            // objective of a NON-terminal stage advances (QuestStageAdvanced)
            // and does NOT complete (QuestCompleted stays 0). The existing
            // test asserts StageAdvanced fires but never counter-asserts that
            // Completed does NOT — a buggy impl that completed on every advance
            // would pass it.
            var (part, cap) = PartWithCapture("Q", Stage("s0", Obj("a")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });

            part.FinishObjective("Q", "a"); // last required of stage 0 (non-terminal)

            Assert.AreEqual(1, cap.Count("QuestStageAdvanced"), "advances to the next stage");
            Assert.AreEqual(0, cap.Count("QuestCompleted"),
                "a non-terminal advance must NOT complete the quest");
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex);
        }

        [Test]
        public void Hypothesis_SingleStageObjectiveQuest_CompletesOnLastRequired()
        {
            // HYPOTHESIS (state side): a one-stage quest whose only stage has
            // required objectives completes when the last required objective
            // finishes — no separate empty terminal stage needed.
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")));

            Assert.IsTrue(part.FinishObjective("Q", "a"));
            Assert.IsFalse(part.IsQuestCompleted("Q"), "one of two required done — not complete");
            Assert.IsTrue(part.FinishObjective("Q", "b"));
            Assert.IsTrue(part.IsQuestCompleted("Q"), "all required done on the only stage → complete");
            Assert.IsFalse(part.IsQuestActive("Q"), "completed quest leaves the active set");
        }

        // ════════════════ H5 — cross-system: resume after save/load ════════════════

        [Test]
        public void Hypothesis_ContinueAfterSaveLoad_FinishRemainingRequired_Advances()
        {
            // HYPOTHESIS: a quest saved MID-STAGE (one of two required
            // objectives done) resumes correctly after load — finishing the
            // remaining required objective advances the stage. Probes the gap
            // BETWEEN save/load (state preservation) and dispatch (advance
            // logic): round-trip tests stop at "objectives preserved"; they
            // don't then drive the advance on the loaded part.
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            Assert.IsTrue(part.FinishObjective("Q", "a"));
            Assert.AreEqual(0, part.GetQuestState("Q").CurrentStageIndex);

            var loaded = RoundTrip(part);
            StoryletPart.Current = loaded; // the live part after a load
            Assert.IsTrue(loaded.IsObjectiveFinished("Q", "a"), "progress survives the load");
            Assert.AreEqual(0, loaded.GetQuestState("Q").CurrentStageIndex);

            Assert.IsTrue(loaded.FinishObjective("Q", "b"), "remaining required finishes post-load");
            Assert.AreEqual(1, loaded.GetQuestState("Q").CurrentStageIndex,
                "finishing the last required objective AFTER a load advances the stage");
        }

        // ════════════════ H4 — cross-stage reused objective ID ════════════════

        [Test]
        public void Hypothesis_CrossStageReusedObjectiveId_IsIndependentPerStage()
        {
            // HYPOTHESIS: an objective ID reused across stages is tracked
            // per-stage (FinishedObjectives clears on advance), so finishing
            // "talk" in stage 0 advances, and "talk" is then unfinished and
            // re-finishable in stage 1.
            var part = PartWithQuest("Q",
                Stage("s0", Obj("talk")),
                Stage("s1", Obj("talk")),
                Stage("s2")); // terminal

            Assert.IsTrue(part.FinishObjective("Q", "talk"), "stage 0 'talk' finishes");
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex, "advanced to stage 1");
            Assert.IsFalse(part.IsObjectiveFinished("Q", "talk"),
                "the same ID is unfinished again in the new stage (set was cleared)");

            Assert.IsTrue(part.FinishObjective("Q", "talk"), "stage 1 'talk' finishes independently");
            Assert.AreEqual(2, part.GetQuestState("Q").CurrentStageIndex, "advanced to terminal stage");
        }

        // ════════════════ post-completion + fail/retake lifecycle ════════════════

        [Test]
        public void Hypothesis_FinishObjectiveAfterQuestCompleted_IsNoOp()
        {
            // HYPOTHESIS: once a quest is completed (removed from the active
            // dict), finishing any of its objectives is a no-op — no
            // resurrection back into the active set, no extra events.
            var (part, cap) = PartWithCapture("Q", Stage("s0", Obj("a")));
            part.StartQuest(new QuestState { QuestId = "Q" });
            part.FinishObjective("Q", "a"); // completes the single-stage quest
            Assert.IsTrue(part.IsQuestCompleted("Q"));
            int finishedEvents = cap.Count("QuestObjectiveFinished");

            Assert.IsFalse(part.FinishObjective("Q", "a"),
                "finishing an objective of a completed quest is a no-op");
            Assert.IsFalse(part.IsQuestActive("Q"), "no resurrection into the active set");
            Assert.AreEqual(finishedEvents, cap.Count("QuestObjectiveFinished"),
                "the no-op fires no further QuestObjectiveFinished event");
        }

        [Test]
        public void Hypothesis_FailThenRetake_ObjectiveProgressWorks_AndNotFailed()
        {
            // HYPOTHESIS: after failing a quest and re-taking it, objective
            // progress works again and IsQuestFailed clears. Probes the Q6
            // fail-set interaction with the Q3 objective dispatch across a
            // re-take.
            var part = PartWithQuest("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.IsTrue(part.FailQuest("Q"));
            Assert.IsTrue(part.IsQuestFailed("Q"));
            Assert.IsFalse(part.IsQuestActive("Q"));

            part.StartQuest(new QuestState { QuestId = "Q", CurrentStageIndex = 0 });
            Assert.IsFalse(part.IsQuestFailed("Q"), "re-taking clears the failed flag");

            Assert.IsTrue(part.FinishObjective("Q", "a"),
                "objective progress works again after a re-take");
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex);
        }
    }
}
