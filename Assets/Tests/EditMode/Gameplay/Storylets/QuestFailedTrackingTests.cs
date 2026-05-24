using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q6 (Docs/QUEST-FAILED-TRACKING.md) — failed-quest tracking. FailQuest
    /// now records the failure (IsQuestFailed / IfQuestFailed) instead of
    /// leaving it untracked; re-take and completion clear it; the set
    /// round-trips. Failed quests stay re-takeable (IfQuestNotStarted
    /// unchanged) — only IfQuestFailed distinguishes them.
    /// </summary>
    public class QuestFailedTrackingTests
    {
        [SetUp]
        public void SetUp()
        {
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            ConversationPredicates.Reset();
            StoryletRegistry.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        private static StoryletPart RoundTrip(StoryletPart part)
        {
            using var stream = new MemoryStream();
            part.Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = new StoryletPart();
            loaded.Load(new SaveReader(stream, null));
            return loaded;
        }

        // ════════════════ tracking ════════════════

        [Test]
        public void FailQuest_MarksFailed_AndRemovesFromActive()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q" });
            Assert.IsTrue(sp.FailQuest("Q"));
            Assert.IsTrue(sp.IsQuestFailed("Q"));
            Assert.IsFalse(sp.IsQuestActive("Q"));
        }

        [Test]
        public void FailQuest_InactiveQuest_ReturnsFalse_NotTracked()
        {
            var sp = new StoryletPart();
            Assert.IsFalse(sp.FailQuest("NeverStarted"),
                "failing an inactive quest is a no-op");
            Assert.IsFalse(sp.IsQuestFailed("NeverStarted"));
        }

        [Test]
        public void ReStartQuest_ClearsFailed()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q" });
            sp.FailQuest("Q");
            Assert.IsTrue(sp.IsQuestFailed("Q"));
            sp.StartQuest(new QuestState { QuestId = "Q" }); // re-take
            Assert.IsFalse(sp.IsQuestFailed("Q"), "re-taking clears the failed flag");
            Assert.IsTrue(sp.IsQuestActive("Q"));
        }

        [Test]
        public void MarkQuestCompleted_ClearsFailed_CompletedWins()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q" });
            sp.FailQuest("Q");
            sp.MarkQuestCompleted("Q"); // completed-wins
            Assert.IsFalse(sp.IsQuestFailed("Q"));
            Assert.IsTrue(sp.IsQuestCompleted("Q"));
        }

        [Test]
        public void FailRetakeFail_TracksLatestFailure()
        {
            // Adversarial: the fail → retake → fail cycle ends in failed.
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "Q" });
            sp.FailQuest("Q");
            sp.StartQuest(new QuestState { QuestId = "Q" });
            Assert.IsFalse(sp.IsQuestFailed("Q"));
            sp.FailQuest("Q");
            Assert.IsTrue(sp.IsQuestFailed("Q"));
        }

        // ════════════════ predicates ════════════════

        [Test]
        public void IfQuestFailed_Predicate_TracksState()
        {
            var sp = new StoryletPart();
            StoryletPart.Current = sp;
            sp.StartQuest(new QuestState { QuestId = "Q" });
            Assert.IsFalse(ConversationPredicates.Evaluate("IfQuestFailed", null, null, "Q"));
            sp.FailQuest("Q");
            Assert.IsTrue(ConversationPredicates.Evaluate("IfQuestFailed", null, null, "Q"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfNotQuestFailed", null, null, "Q"),
                "auto-inverse IfNotQuestFailed");
        }

        [Test]
        public void IfQuestNotStarted_StillTrueForFailed_ReTakeablePreserved()
        {
            // Documented decision: a failed quest stays re-offerable —
            // IfQuestNotStarted is intentionally unchanged. Content gates
            // re-offers with IfNotQuestFailed if it wants to.
            var sp = new StoryletPart();
            StoryletPart.Current = sp;
            sp.StartQuest(new QuestState { QuestId = "Q" });
            sp.FailQuest("Q");
            Assert.IsTrue(ConversationPredicates.Evaluate("IfQuestNotStarted", null, null, "Q"),
                "a failed quest still reads as not-started (re-takeable) by default");
        }

        // ════════════════ save/load ════════════════

        [Test]
        public void SaveRoundTrip_FailedQuests_Preserved()
        {
            var sp = new StoryletPart();
            sp.StartQuest(new QuestState { QuestId = "QA" });
            sp.StartQuest(new QuestState { QuestId = "QB" });
            sp.FailQuest("QA");
            sp.FailQuest("QB");

            var loaded = RoundTrip(sp);
            Assert.IsTrue(loaded.IsQuestFailed("QA"));
            Assert.IsTrue(loaded.IsQuestFailed("QB"));
        }

        [Test]
        public void Load_PreQ6Save_NoFailedSection_EmptyWithoutThrowing()
        {
            // Back-compat: a pre-Q6 save has fired + quests + completed +
            // objectives sections, but NO failed section. The EOF guard
            // must default _failedQuests to empty.
            using var stream = new MemoryStream();
            var w = new SaveWriter(stream);
            w.Write(0);  // fired
            w.Write(0);  // quests
            w.Write(0);  // completed
            w.Write(0);  // Q3 objectives section (count 0)
            // (no Q6 failed section)
            stream.Position = 0;
            var loaded = new StoryletPart();
            Assert.DoesNotThrow(() => loaded.Load(new SaveReader(stream, null)));
            Assert.AreEqual(0, loaded.GetFailedQuests().Count);
            Assert.IsFalse(loaded.IsQuestFailed("Anything"));
        }
    }
}
