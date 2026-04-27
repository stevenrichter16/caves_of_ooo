using System.IO;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// M2 TDD tests for KnowledgePart: per-NPC int-tier knowledge store.
    ///
    /// Tier semantics (convention, not enforced by KnowledgePart itself):
    ///   0 = ignorant (never heard of it)
    ///   1 = does-not-know (aware topic exists but lacks info)
    ///   2 = suspects
    ///   3 = knows
    /// </summary>
    public class KnowledgePartTests
    {
        private Entity _npc;
        private KnowledgePart _part;

        [SetUp]
        public void SetUp()
        {
            _npc = new Entity { BlueprintName = "Villager" };
            _part = new KnowledgePart();
            _npc.AddPart(_part);
        }

        // --- Get / Reveal ---

        [Test]
        public void GetKnowledge_Unknown_ReturnsZero()
        {
            Assert.AreEqual(0, _part.GetKnowledge("secretLocation"));
        }

        [Test]
        public void Reveal_SetsKnowledgeTier()
        {
            _part.Reveal("houseConflict", 2);
            Assert.AreEqual(2, _part.GetKnowledge("houseConflict"));
        }

        [Test]
        public void Reveal_CannotDecreaseExistingKnowledge()
        {
            _part.Reveal("fact", 3);
            _part.Reveal("fact", 1);
            Assert.AreEqual(3, _part.GetKnowledge("fact"),
                "Knowledge should never decrease once learned");
        }

        [Test]
        public void Reveal_CanIncreaseKnowledge()
        {
            _part.Reveal("fact", 1);
            _part.Reveal("fact", 3);
            Assert.AreEqual(3, _part.GetKnowledge("fact"));
        }

        [Test]
        public void MultipleTopics_AreIndependent()
        {
            _part.Reveal("a", 1);
            _part.Reveal("b", 3);
            Assert.AreEqual(1, _part.GetKnowledge("a"));
            Assert.AreEqual(3, _part.GetKnowledge("b"));
        }

        // --- Knows helper ---

        [Test]
        public void Knows_ReturnsTrueAtOrAboveThreshold()
        {
            _part.Reveal("secret", 3);
            Assert.IsTrue(_part.Knows("secret", 3));
        }

        [Test]
        public void Knows_ReturnsFalseBelowThreshold()
        {
            _part.Reveal("secret", 2);
            Assert.IsFalse(_part.Knows("secret", 3));
        }

        [Test]
        public void Knows_ReturnsFalseForUnknownTopic()
        {
            Assert.IsFalse(_part.Knows("unknown", 1));
        }

        // --- Save / Load round-trip ---

        [Test]
        public void Knowledge_SurvivesRoundTrip()
        {
            _part.Reveal("conspiracy", 2);
            _part.Reveal("elderSecret", 3);

            KnowledgePart loaded = RoundTrip(_part);

            Assert.AreEqual(2, loaded.GetKnowledge("conspiracy"));
            Assert.AreEqual(3, loaded.GetKnowledge("elderSecret"));
        }

        [Test]
        public void EmptyKnowledge_RoundTrips_WithoutError()
        {
            KnowledgePart loaded = RoundTrip(_part);
            Assert.AreEqual(0, loaded.GetKnowledge("anything"));
        }

        // --- Helper ---

        private static KnowledgePart RoundTrip(KnowledgePart original)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            original.Save(writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, null);
            var loaded = new KnowledgePart();
            loaded.Load(reader);
            return loaded;
        }
    }
}
