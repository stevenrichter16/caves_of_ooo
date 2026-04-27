using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// M2 TDD tests for NarrativeStatePart: global fact store + event log on world entity.
    /// </summary>
    public class NarrativeStatePartTests
    {
        private Entity _world;
        private NarrativeStatePart _part;

        [SetUp]
        public void SetUp()
        {
            _world = new Entity { BlueprintName = "World" };
            _part = new NarrativeStatePart();
            _world.AddPart(_part);
        }

        // --- Fact delegation ---

        [Test]
        public void GetFact_Unknown_ReturnsZero()
        {
            Assert.AreEqual(0, _part.GetFact("x"));
        }

        [Test]
        public void SetFact_ThenGetFact_ReturnsValue()
        {
            _part.SetFact("reputation", 3);
            Assert.AreEqual(3, _part.GetFact("reputation"));
        }

        [Test]
        public void AddFact_IncrementsValue()
        {
            _part.SetFact("suspicion", 2);
            _part.AddFact("suspicion", 3);
            Assert.AreEqual(5, _part.GetFact("suspicion"));
        }

        [Test]
        public void ClearFact_ResetsToZero()
        {
            _part.SetFact("x", 7);
            _part.ClearFact("x");
            Assert.AreEqual(0, _part.GetFact("x"));
        }

        // --- Event log ---

        [Test]
        public void LogEvent_AppendsToLog()
        {
            _part.LogEvent("PlayerKilledGuard");
            Assert.AreEqual(1, _part.EventLog.Count);
            Assert.AreEqual("PlayerKilledGuard", _part.EventLog[0]);
        }

        [Test]
        public void LogEvent_MultipleEntries_AllPreserved()
        {
            _part.LogEvent("A");
            _part.LogEvent("B");
            _part.LogEvent("C");
            Assert.AreEqual(3, _part.EventLog.Count);
            Assert.AreEqual("A", _part.EventLog[0]);
            Assert.AreEqual("C", _part.EventLog[2]);
        }

        [Test]
        public void EventLog_InitiallyEmpty()
        {
            Assert.AreEqual(0, _part.EventLog.Count);
        }

        // --- Save / Load round-trip ---

        [Test]
        public void Facts_SurviveRoundTrip()
        {
            _part.SetFact("questStage", 4);
            _part.SetFact("kills", 2);

            NarrativeStatePart loaded = RoundTrip(_part);

            Assert.AreEqual(4, loaded.GetFact("questStage"));
            Assert.AreEqual(2, loaded.GetFact("kills"));
        }

        [Test]
        public void EventLog_SurvivesRoundTrip()
        {
            _part.LogEvent("ChapterOne");
            _part.LogEvent("MeetingWithElder");

            NarrativeStatePart loaded = RoundTrip(_part);

            Assert.AreEqual(2, loaded.EventLog.Count);
            Assert.AreEqual("ChapterOne", loaded.EventLog[0]);
            Assert.AreEqual("MeetingWithElder", loaded.EventLog[1]);
        }

        [Test]
        public void EmptyState_RoundTrips_WithoutError()
        {
            NarrativeStatePart loaded = RoundTrip(_part);
            Assert.AreEqual(0, loaded.GetFact("anything"));
            Assert.AreEqual(0, loaded.EventLog.Count);
        }

        // --- Helper ---

        private static NarrativeStatePart RoundTrip(NarrativeStatePart original)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            original.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = new NarrativeStatePart();
            loaded.Load(reader);
            return loaded;
        }
    }
}
