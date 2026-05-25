using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// OBSERVABILITY alignment (CLAUDE.md → "every gate that can reject emits a
    /// record... kind=Rejected with a reason field naming which gate fired").
    /// The quest gates emitted rich SUCCESS diags (ObjectiveFinished /
    /// StageAdvanced / Completed / Failed) but were SILENT on their no-op /
    /// reject branches — the exact skill-system gap the rule was written for
    /// ("FinishObjective doesn't hit → no diag trace → debug degrades to grep").
    /// These tests pin a <c>quest/Rejected</c> record (payload: gate, reason,
    /// questId, objectiveId) on the reject branches, with a counter-check that
    /// the success path emits NO Rejected.
    /// </summary>
    public class QuestRejectionDiagTests
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

        private static QuestObjectiveData Obj(string id) => new QuestObjectiveData { ID = id };

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

        private static IReadOnlyList<Diag.Entry> Rejections()
            => DiagQuery.Apply(new DiagQuery.Filter { Category = "quest", Kind = "Rejected", Limit = 50 }).Records;

        private static bool AnyRejectionWithReason(string reason)
        {
            foreach (var r in Rejections())
                if ((r.PayloadJson ?? "").Contains("\"reason\":\"" + reason + "\"")) return true;
            return false;
        }

        // ════════════════ FinishObjective reject branches ════════════════

        [Test]
        public void FinishObjective_QuestNotActive_EmitsRejectedDiag()
        {
            var part = new StoryletPart(); // nothing started
            Diag.ResetAll();
            Assert.IsFalse(part.FinishObjective("NoSuchQuest", "obj"));
            Assert.IsTrue(AnyRejectionWithReason("quest_not_active"),
                "a FinishObjective on an inactive quest must emit quest/Rejected (reason=quest_not_active)");
        }

        [Test]
        public void FinishObjective_ObjectiveNotInStage_EmitsRejectedDiag()
        {
            var part = PartWithQuest("Q", Stage("s0", Obj("a")), Stage("s1"));
            Diag.ResetAll();
            Assert.IsFalse(part.FinishObjective("Q", "not_an_objective"));
            Assert.IsTrue(AnyRejectionWithReason("objective_not_in_current_stage"),
                "finishing an objective not in the current stage must emit a reason");
        }

        [Test]
        public void FinishObjective_AlreadyFinished_EmitsRejectedDiag()
        {
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            part.FinishObjective("Q", "a"); // first finish (success)
            Diag.ResetAll();
            Assert.IsFalse(part.FinishObjective("Q", "a")); // re-finish → no-op
            Assert.IsTrue(AnyRejectionWithReason("already_finished"),
                "re-finishing an already-finished objective must emit a reason (idempotent no-op is observable)");
        }

        [Test]
        public void FinishObjective_Success_DoesNotEmitRejected()
        {
            // Counter-check: the success path emits ObjectiveFinished, NOT Rejected.
            var part = PartWithQuest("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            Diag.ResetAll();
            Assert.IsTrue(part.FinishObjective("Q", "a"));
            Assert.AreEqual(0, Rejections().Count, "a successful finish emits no Rejected record");
        }

        // ════════════════ other quest gates ════════════════

        [Test]
        public void CompleteQuest_AlreadyInactive_EmitsRejectedDiag()
        {
            var part = new StoryletPart(); // quest never started
            Diag.ResetAll();
            Assert.IsFalse(part.CompleteQuest("Ghost"));
            Assert.IsTrue(AnyRejectionWithReason("quest_not_active"),
                "CompleteQuest on an inactive quest must emit a reason");
        }

        [Test]
        public void FailQuest_AlreadyInactive_EmitsRejectedDiag()
        {
            var part = new StoryletPart();
            Diag.ResetAll();
            Assert.IsFalse(part.FailQuest("Ghost"));
            Assert.IsTrue(AnyRejectionWithReason("quest_not_active"),
                "FailQuest on an inactive quest must emit a reason");
        }
    }
}
