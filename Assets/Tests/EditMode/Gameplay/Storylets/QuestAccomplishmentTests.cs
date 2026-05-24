using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q7 (Docs/QUEST-ACCOMPLISHMENTS.md) — on quest completion, the quest's
    /// Accomplishment deed text is logged into the narrative event log
    /// (NarrativeStatePart) + a quest/Accomplishment diag fires. Qud parity
    /// with Quest.Accomplishment / JournalAPI.AddAccomplishment.
    /// </summary>
    public class QuestAccomplishmentTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = new NarrativeStatePart();
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null;
        }

        private const string Deed = "Finn recovered the Enchiridion from the Candy Kingdom.";

        private static StoryletPart RegisterAndStart(string questId, string accomplishment,
            params QuestObjectiveData[] stage0Objectives)
        {
            var sd = new StoryletData
            {
                ID = questId,
                Quest = new QuestData { Accomplishment = accomplishment },
            };
            var s0 = new QuestStageData { ID = "s0" };
            if (stage0Objectives != null) s0.Objectives.AddRange(stage0Objectives);
            sd.Quest.Stages.Add(s0);
            StoryletRegistry.Register(sd);
            var sp = new StoryletPart();
            StoryletPart.Current = sp;
            sp.StartQuest(new QuestState { QuestId = questId });
            return sp;
        }

        private static int AccomplishmentDiagCount() =>
            DiagQuery.Apply(new DiagQuery.Filter
            { Category = "quest", Kind = "Accomplishment", Limit = 50 }).Records.Count;

        [Test]
        public void CompleteQuest_WithAccomplishment_LogsToNarrativeEventLog()
        {
            var sp = RegisterAndStart("Q", Deed);
            sp.CompleteQuest("Q");
            Assert.IsTrue(NarrativeStatePart.Current.EventLog.Any(e => e == Deed),
                "the accomplishment deed is appended to the narrative event log");
            Assert.AreEqual(1, AccomplishmentDiagCount(),
                "a quest/Accomplishment diag fires once on completion");
        }

        [Test]
        public void CompleteQuest_NoAccomplishment_LogsNothing()
        {
            // Counter-check: a quest without an Accomplishment logs no deed.
            var sp = RegisterAndStart("Q", accomplishment: null);
            int before = NarrativeStatePart.Current.EventLog.Count;
            sp.CompleteQuest("Q");
            Assert.AreEqual(before, NarrativeStatePart.Current.EventLog.Count,
                "no accomplishment text → no event-log entry");
            Assert.AreEqual(0, AccomplishmentDiagCount());
        }

        [Test]
        public void CompleteQuest_NullNarrativeState_NoThrow()
        {
            var sp = RegisterAndStart("Q", Deed);
            NarrativeStatePart.Current = null; // e.g. pre-bootstrap
            Assert.DoesNotThrow(() => sp.CompleteQuest("Q"));
        }

        [Test]
        public void CompleteQuest_Idempotent_LogsAccomplishmentOnce()
        {
            var sp = RegisterAndStart("Q", Deed);
            sp.CompleteQuest("Q");
            sp.CompleteQuest("Q"); // already completed → no-op
            Assert.AreEqual(1, NarrativeStatePart.Current.EventLog.Count(e => e == Deed),
                "re-completing an already-completed quest does not re-log the deed");
        }

        [Test]
        public void AutoComplete_ViaLastObjective_LogsAccomplishment()
        {
            // The main path: finishing the last required objective auto-
            // completes the (1-stage) quest → CompleteQuest → deed logged.
            var sp = RegisterAndStart("Q", Deed, new QuestObjectiveData { ID = "slay_lich" });
            sp.FinishObjective("Q", "slay_lich");
            Assert.IsTrue(sp.IsQuestCompleted("Q"), "single-objective single-stage quest completes");
            Assert.IsTrue(NarrativeStatePart.Current.EventLog.Any(e => e == Deed),
                "auto-completion logs the accomplishment too (shared CompleteQuest path)");
        }
    }
}
