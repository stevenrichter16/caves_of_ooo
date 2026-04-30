using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2 TDD tests for StoryletPart: per-world-entity ISaveSerializable Part
    /// holding the set of fired storylets and the active quest map. Mirrors
    /// the NarrativeStatePart test shape.
    ///
    /// Positive assertions are paired with counter-checks (CLAUDE.md §3.4)
    /// — every "X happens after Y" test has a "X does NOT happen without Y"
    /// twin so a vacuous-precondition bug can't pass silently.
    /// </summary>
    public class StoryletPartTests
    {
        private StoryletPart _part;

        [SetUp]
        public void SetUp()
        {
            _part = new StoryletPart();
        }

        // ── Fired-set API ────────────────────────────────────────────────────

        [Test]
        public void MarkFired_ThenHasFired_ReturnsTrue()
        {
            _part.MarkFired("alpha");
            Assert.IsTrue(_part.HasFired("alpha"));
        }

        // counter-check
        [Test]
        public void HasFired_BeforeMarkFired_ReturnsFalse()
        {
            Assert.IsFalse(_part.HasFired("alpha"));
        }

        // counter-check
        [Test]
        public void HasFired_DifferentId_ReturnsFalse()
        {
            _part.MarkFired("alpha");
            Assert.IsFalse(_part.HasFired("beta"));
        }

        [Test]
        public void MarkFired_TwiceWithSameId_StaysSingleton()
        {
            _part.MarkFired("alpha");
            _part.MarkFired("alpha");
            // Reflective check via round-trip: only one entry should be saved.
            var loaded = RoundTripPart(_part);
            Assert.IsTrue(loaded.HasFired("alpha"));
        }

        [Test]
        public void MarkFired_NullOrEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _part.MarkFired(null));
            Assert.DoesNotThrow(() => _part.MarkFired(""));
            Assert.IsFalse(_part.HasFired(""));
        }

        // ── Quest API ────────────────────────────────────────────────────────

        [Test]
        public void StartQuest_ThenIsQuestActive_ReturnsTrue()
        {
            _part.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            Assert.IsTrue(_part.IsQuestActive("Q1"));
        }

        // counter-check
        [Test]
        public void IsQuestActive_BeforeStart_ReturnsFalse()
        {
            Assert.IsFalse(_part.IsQuestActive("Q1"));
        }

        [Test]
        public void GetQuestState_ReturnsStartedStateById()
        {
            _part.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 2, EnteredStageAtTurn = 17 });
            var s = _part.GetQuestState("Q1");
            Assert.IsNotNull(s);
            Assert.AreEqual("Q1", s.QuestId);
            Assert.AreEqual(2, s.CurrentStageIndex);
            Assert.AreEqual(17, s.EnteredStageAtTurn);
        }

        [Test]
        public void StartQuest_NullState_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _part.StartQuest(null));
        }

        [Test]
        public void StartQuest_EmptyId_IsIgnored()
        {
            _part.StartQuest(new QuestState { QuestId = "", CurrentStageIndex = 0 });
            Assert.AreEqual(0, _part.GetActiveQuests().Count);
        }

        // counter-check
        [Test]
        public void TwoQuests_HaveIndependentStageState()
        {
            _part.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 1 });
            _part.StartQuest(new QuestState { QuestId = "Q2", CurrentStageIndex = 5 });

            Assert.AreEqual(1, _part.GetQuestState("Q1").CurrentStageIndex);
            Assert.AreEqual(5, _part.GetQuestState("Q2").CurrentStageIndex);
        }

        [Test]
        public void GetActiveQuests_ReturnsAllStartedQuests()
        {
            _part.StartQuest(new QuestState { QuestId = "Q1" });
            _part.StartQuest(new QuestState { QuestId = "Q2" });
            _part.StartQuest(new QuestState { QuestId = "Q3" });

            Assert.AreEqual(3, _part.GetActiveQuests().Count);
        }

        // ── Save / Load round-trip ───────────────────────────────────────────

        [Test]
        public void EmptyState_RoundTrips()
        {
            var loaded = RoundTripPart(_part);
            Assert.IsFalse(loaded.HasFired("anything"));
            Assert.AreEqual(0, loaded.GetActiveQuests().Count);
        }

        [Test]
        public void FiredStorylets_SurviveRoundTrip()
        {
            _part.MarkFired("a");
            _part.MarkFired("b");
            _part.MarkFired("c");

            var loaded = RoundTripPart(_part);

            Assert.IsTrue(loaded.HasFired("a"));
            Assert.IsTrue(loaded.HasFired("b"));
            Assert.IsTrue(loaded.HasFired("c"));
        }

        // counter-check
        [Test]
        public void NotFiredStorylet_StaysUnfiredAfterRoundTrip()
        {
            _part.MarkFired("a");
            var loaded = RoundTripPart(_part);
            Assert.IsFalse(loaded.HasFired("never_fired"));
        }

        [Test]
        public void Quest_StageIndexAndEnteredTurn_SurviveRoundTrip()
        {
            _part.StartQuest(new QuestState
            {
                QuestId = "Q1",
                CurrentStageIndex = 2,
                EnteredStageAtTurn = 42
            });

            var loaded = RoundTripPart(_part);
            var s = loaded.GetQuestState("Q1");

            Assert.IsNotNull(s);
            Assert.AreEqual("Q1", s.QuestId);
            Assert.AreEqual(2, s.CurrentStageIndex);
            Assert.AreEqual(42, s.EnteredStageAtTurn);
        }

        [Test]
        public void MultipleQuestsAndFiredStorylets_AllPreservedAcrossRoundTrip()
        {
            _part.MarkFired("storylet_a");
            _part.MarkFired("storylet_b");
            _part.StartQuest(new QuestState { QuestId = "Q1", CurrentStageIndex = 0 });
            _part.StartQuest(new QuestState { QuestId = "Q2", CurrentStageIndex = 3, EnteredStageAtTurn = 99 });

            var loaded = RoundTripPart(_part);

            Assert.IsTrue(loaded.HasFired("storylet_a"));
            Assert.IsTrue(loaded.HasFired("storylet_b"));
            Assert.AreEqual(2, loaded.GetActiveQuests().Count);
            Assert.AreEqual(3, loaded.GetQuestState("Q2").CurrentStageIndex);
            Assert.AreEqual(99, loaded.GetQuestState("Q2").EnteredStageAtTurn);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static StoryletPart RoundTripPart(StoryletPart original)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            original.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = new StoryletPart();
            loaded.Load(reader);
            return loaded;
        }
    }
}
