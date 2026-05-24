using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q3 (Docs/QUEST-PARALLEL-OBJECTIVES.md) — dedicated ADVERSARIAL sweep
    /// for the parallel-objective feature (Q3.1–Q3.3), per CLAUDE.md's
    /// adversarial-gate (≥2 surfaces apply: parser, state atomicity,
    /// stacking, save/load reach, diag emission). Probes bug classes the
    /// per-phase happy-path + counter tests don't see. Grouped by surface.
    ///
    /// Out of scope (noted, not probed here): conversation action-exception
    /// sandboxing — `ConversationActions.Execute` doesn't try/catch the
    /// action func, but that's a pre-existing, system-wide trust model
    /// (stage OnEnter + storylet Effects share it), not Q3-specific.
    /// </summary>
    public class QuestObjectiveAdversarialTests
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

        // ── fixtures ──
        private static QuestObjectiveData Obj(string id, bool optional = false, bool hidden = false)
            => new QuestObjectiveData { ID = id, Optional = optional, Hidden = hidden };

        private static QuestStageData Stage(string id, params QuestObjectiveData[] objs)
        {
            var s = new QuestStageData { ID = id };
            if (objs != null) s.Objectives.AddRange(objs);
            return s;
        }

        /// <summary>Register + start a quest, install as StoryletPart.Current
        /// (what the conversation action/predicate read), return the part.</summary>
        private static StoryletPart Make(string questId, params QuestStageData[] stages)
        {
            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            foreach (var s in stages) sd.Quest.Stages.Add(s);
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = questId, CurrentStageIndex = 0 });
            StoryletPart.Current = part;
            return part;
        }

        private static int DiagCount(string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter { Category = "quest", Kind = kind, Limit = 500 })
                .Records.Count;

        // ════════════════════════════════════════════════════════════
        //   PARSER — FinishObjective action + IfObjectiveFinished predicate
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Parser_Action_EmptyObjIdAfterColon_NoOp()
        {
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.DoesNotThrow(() => ConversationActions.Execute("FinishObjective", null, null, "Q:"));
            Assert.IsFalse(p.IsObjectiveFinished("Q", "a"));
            Assert.AreEqual(0, p.GetQuestState("Q").CurrentStageIndex);
        }

        [Test]
        public void Parser_Action_DoubleTilde_SkipsEmptyMember()
        {
            // "a~~b" → ["a","","b"]; the empty member is skipped, a + b finish.
            var p = Make("Q", Stage("s0", Obj("a"), Obj("b"), Obj("c")), Stage("s1"));
            ConversationActions.Execute("FinishObjective", null, null, "Q:a~~b");
            Assert.IsTrue(p.IsObjectiveFinished("Q", "a"));
            Assert.IsTrue(p.IsObjectiveFinished("Q", "b"));
            Assert.IsFalse(p.IsObjectiveFinished("Q", "c"));
        }

        [Test]
        public void Parser_Action_TrailingTilde_SkipsEmptyMember()
        {
            var p = Make("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            Assert.DoesNotThrow(() => ConversationActions.Execute("FinishObjective", null, null, "Q:a~"));
            Assert.IsTrue(p.IsObjectiveFinished("Q", "a"));
        }

        [Test]
        public void Parser_Action_NullAndWhitespace_NoOp()
        {
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.DoesNotThrow(() => ConversationActions.Execute("FinishObjective", null, null, null));
            Assert.DoesNotThrow(() => ConversationActions.Execute("FinishObjective", null, null, "   "));
            Assert.IsFalse(p.IsObjectiveFinished("Q", "a"));
        }

        [Test]
        public void Parser_Predicate_MalformedArgs_False()
        {
            Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfObjectiveFinished", null, null, "Q"),
                "no colon → false");
            Assert.IsFalse(ConversationPredicates.Evaluate("IfObjectiveFinished", null, null, ""),
                "empty → false");
            Assert.IsFalse(ConversationPredicates.Evaluate("IfObjectiveFinished", null, null, "Q:"),
                "empty objId → false (not finished)");
        }

        // ════════════════════════════════════════════════════════════
        //   BOUNDARY — FinishObjective / IsObjectiveFinished API
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Boundary_FinishObjective_NullAndEmptyArgs_False()
        {
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.IsFalse(p.FinishObjective(null, "a"));
            Assert.IsFalse(p.FinishObjective("Q", null));
            Assert.IsFalse(p.FinishObjective("", ""));
            Assert.IsFalse(p.FinishObjective("Q", ""));
        }

        [Test]
        public void Boundary_FinishObjective_InactiveQuest_False()
        {
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            Assert.IsFalse(p.FinishObjective("NoSuchQuest", "a"));
            Assert.IsFalse(p.IsObjectiveFinished("NoSuchQuest", "a"));
        }

        [Test]
        public void Boundary_FinishObjective_NoObjectivesStage_False()
        {
            // A stage with no objectives can't have objectives finished
            // (legacy linear stage — advances via stage Triggers instead).
            var p = Make("Q", Stage("s0"), Stage("s1"));
            Assert.IsFalse(p.FinishObjective("Q", "anything"));
        }

        [Test]
        public void Boundary_FinishObjective_StageIndexOutOfBounds_NoCrash()
        {
            // A QuestState whose CurrentStageIndex is past the end (corrupt
            // save / content removed) must not crash FinishObjective.
            var sd = new StoryletData { ID = "Q", Quest = new QuestData() };
            sd.Quest.Stages.Add(Stage("s0", Obj("a")));
            StoryletRegistry.Register(sd);
            var p = new StoryletPart();
            p.StartQuest(new QuestState { QuestId = "Q", CurrentStageIndex = 99 });
            Assert.DoesNotThrow(() => p.FinishObjective("Q", "a"));
            Assert.IsFalse(p.FinishObjective("Q", "a"));
        }

        // ════════════════════════════════════════════════════════════
        //   STACKING / re-entry
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Stacking_FinishObjective_AlreadyFinished_ReturnsFalse()
        {
            var p = Make("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            Assert.IsTrue(p.FinishObjective("Q", "a"));
            Assert.IsFalse(p.FinishObjective("Q", "a"));
        }

        [Test]
        public void Stacking_SameObjIdAcrossStages_TrackedPerStage()
        {
            // The SAME objective ID exists in stage 0 AND stage 1. Finishing
            // it in s0 advances; the s1 instance must then be independently
            // unfinished + finishable (FinishedObjectives is stage-scoped).
            var p = Make("Q", Stage("s0", Obj("x")), Stage("s1", Obj("x")), Stage("s2"));
            p.FinishObjective("Q", "x");
            Assert.AreEqual(1, p.GetQuestState("Q").CurrentStageIndex, "s0 'x' advanced to s1");
            Assert.IsFalse(p.IsObjectiveFinished("Q", "x"), "s1 'x' is a fresh, unfinished instance");
            Assert.IsTrue(p.FinishObjective("Q", "x"), "s1 'x' is finishable");
            Assert.AreEqual(2, p.GetQuestState("Q").CurrentStageIndex);
        }

        // ════════════════════════════════════════════════════════════
        //   SAVE / LOAD reach
        // ════════════════════════════════════════════════════════════

        private static StoryletPart RoundTrip(StoryletPart part)
        {
            using var stream = new MemoryStream();
            part.Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = new StoryletPart();
            loaded.Load(new SaveReader(stream, null));
            return loaded;
        }

        [Test]
        public void SaveLoad_OrphanObjectivesEntry_DoesNotCrash()
        {
            // Hand-write a save whose objectives section references a quest
            // key NOT in the quest section (corrupt/edited save). Load must
            // skip the orphan without throwing.
            using var stream = new MemoryStream();
            var w = new SaveWriter(stream);
            w.Write(0);            // fired
            w.Write(1);            // quests
            w.WriteString("Q"); w.WriteString("Q"); w.Write(0); w.Write(0);
            w.Write(0);            // completed
            w.Write(1);            // objectives section: 1 entry
            w.WriteString("GHOST"); w.Write(1); w.WriteString("x"); // orphan key
            stream.Position = 0;

            var loaded = new StoryletPart();
            Assert.DoesNotThrow(() => loaded.Load(new SaveReader(stream, null)));
            Assert.IsNotNull(loaded.GetQuestState("Q"), "the real quest still loads");
        }

        [Test]
        public void SaveLoad_AfterStageAdvance_ObjectivesCleared()
        {
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            p.FinishObjective("Q", "a"); // advances to s1, clears objectives
            var loaded = RoundTrip(p);
            Assert.AreEqual(1, loaded.GetQuestState("Q").CurrentStageIndex);
            Assert.AreEqual(0, loaded.GetQuestState("Q").FinishedObjectives.Count,
                "cleared-on-advance objective set round-trips empty");
        }

        [Test]
        public void SaveLoad_LargeObjectiveSet_Preserved()
        {
            // 50 objectives in one stage, 49 finished (1 left so the stage
            // doesn't advance + clear). All 49 must round-trip.
            var stage = Stage("s0");
            for (int i = 0; i < 50; i++) stage.Objectives.Add(Obj("o" + i));
            var p = Make("Q", stage, Stage("s1"));
            for (int i = 0; i < 49; i++) p.FinishObjective("Q", "o" + i);

            var loaded = RoundTrip(p);
            Assert.AreEqual(49, loaded.GetQuestState("Q").FinishedObjectives.Count);
            Assert.IsTrue(loaded.IsObjectiveFinished("Q", "o0"));
            Assert.IsTrue(loaded.IsObjectiveFinished("Q", "o48"));
            Assert.IsFalse(loaded.IsObjectiveFinished("Q", "o49"));
        }

        [Test]
        public void SaveLoad_FinishedObjectiveIsRegistryIndependent()
        {
            // IsObjectiveFinished reads runtime state, not the registry —
            // so a finished objective stays "finished" even if its blueprint
            // is later removed (content churn between sessions).
            var p = Make("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            p.FinishObjective("Q", "a");
            StoryletRegistry.Reset(); // blueprint gone
            Assert.IsTrue(p.IsObjectiveFinished("Q", "a"),
                "finished-state survives registry removal (it's just an ID set)");
        }

        // ════════════════════════════════════════════════════════════
        //   DIAG emission (observability invariant — CLAUDE.md)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Diag_FinishObjective_EmitsObjectiveFinished()
        {
            var p = Make("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            p.FinishObjective("Q", "a");
            Assert.AreEqual(1, DiagCount("ObjectiveFinished"),
                "a successful finish emits exactly one quest/ObjectiveFinished record");
        }

        [Test]
        public void Diag_FinishObjective_NoOpPaths_DoNotEmit()
        {
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1"));
            p.FinishObjective("Q", "a");                 // advances (1 emit)
            int after = DiagCount("ObjectiveFinished");
            p.FinishObjective("Q", "nonexistent");        // not found → no emit
            p.FinishObjective("NoSuchQuest", "a");        // inactive → no emit
            Assert.AreEqual(after, DiagCount("ObjectiveFinished"),
                "not-found / inactive-quest finishes emit no ObjectiveFinished record");
        }

        // ════════════════════════════════════════════════════════════
        //   TICK dispatch
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Dispatch_CrossQuest_FinishesObjectivesInMultipleQuests()
        {
            var pa = Make("QA", Stage("s0", Obj("a")), Stage("s1"));
            // second quest on the same part
            var sd = new StoryletData { ID = "QB", Quest = new QuestData() };
            sd.Quest.Stages.Add(Stage("s0", Obj("b")));
            sd.Quest.Stages.Add(Stage("s1"));
            StoryletRegistry.Register(sd);
            pa.StartQuest(new QuestState { QuestId = "QB", CurrentStageIndex = 0 });

            pa.OnTickEnd(new NarrativeStatePart());

            Assert.AreEqual(1, pa.GetQuestState("QA").CurrentStageIndex, "QA advanced");
            Assert.AreEqual(1, pa.GetQuestState("QB").CurrentStageIndex, "QB advanced");
        }

        [Test]
        public void Dispatch_StageAdvance_DoesNotCascadeToNewStageSameTick()
        {
            // Single-pass invariant: s0's objective finishing + advancing to
            // s1 must NOT also finish s1's objective the same tick (it wasn't
            // in this tick's snapshot). It finishes on the NEXT tick.
            // (2-stage quest so tick 2 — finishing s1's objective, the last
            // required of the last stage — advances PAST the end → completes.)
            var p = Make("Q", Stage("s0", Obj("a")), Stage("s1", Obj("b")));
            p.OnTickEnd(new NarrativeStatePart());
            Assert.AreEqual(1, p.GetQuestState("Q").CurrentStageIndex, "advanced to s1");
            Assert.IsFalse(p.IsObjectiveFinished("Q", "b"),
                "s1's objective must NOT finish the same tick the stage advanced");

            p.OnTickEnd(new NarrativeStatePart());
            Assert.IsTrue(p.IsQuestCompleted("Q"),
                "next tick finishes s1's (last) objective → quest completes");
        }

        [Test]
        public void Dispatch_HiddenNonOptional_CountsAsRequired()
        {
            // Hidden is a UI flag only — a Hidden non-Optional objective
            // still gates advancement (unlike Optional).
            var p = Make("Q", Stage("s0", Obj("h", hidden: true), Obj("v")), Stage("s1"));
            p.FinishObjective("Q", "h");
            Assert.AreEqual(0, p.GetQuestState("Q").CurrentStageIndex,
                "Hidden objective done but visible one isn't → no advance (Hidden still required)");
            p.FinishObjective("Q", "v");
            Assert.AreEqual(1, p.GetQuestState("Q").CurrentStageIndex);
        }

        [Test]
        public void Dispatch_AllOptionalStage_AdvancesOnAnyFinish()
        {
            // Pin the edge: a stage with ONLY optional objectives has no
            // required ones, so AllRequiredObjectivesFinished is vacuously
            // true — finishing any optional advances the stage.
            var p = Make("Q", Stage("s0", Obj("o1", optional: true), Obj("o2", optional: true)),
                Stage("s1"));
            p.FinishObjective("Q", "o1");
            Assert.AreEqual(1, p.GetQuestState("Q").CurrentStageIndex,
                "all-optional stage completes once any optional objective finishes");
        }
    }
}
