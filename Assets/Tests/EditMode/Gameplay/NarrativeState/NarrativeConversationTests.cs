using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// M3 TDD tests for narrative conversation predicates and actions.
    ///
    /// Predicates (fail-closed: return false for bad args):
    ///   IfFact:key:op:value  — checks global fact against value using op
    ///   IfNotFact            — auto-inverted by existing IfNot* mechanism
    ///   IfSpeakerKnows:topic:minTier
    ///   IfNotSpeakerKnows    — auto-inverted
    ///
    /// Actions:
    ///   SetFact:key:value
    ///   AddFact:key:delta
    ///   ClearFact:key
    ///   Reveal:Target:topic:tier  (Target ∈ Listener|Speaker)
    /// </summary>
    public class NarrativeConversationTests
    {
        private Entity _speaker;
        private Entity _listener;
        private NarrativeStatePart _statePart;

        [SetUp]
        public void SetUp()
        {
            ConversationPredicates.Reset();
            ConversationActions.Reset();

            var world = new Entity { BlueprintName = "World" };
            _statePart = new NarrativeStatePart();
            world.AddPart(_statePart);
            NarrativeStatePart.Current = _statePart;

            _speaker = new Entity { BlueprintName = "NPC" };
            _speaker.AddPart(new KnowledgePart());

            _listener = new Entity { BlueprintName = "Player" };
            _listener.AddPart(new KnowledgePart());
        }

        [TearDown]
        public void TearDown()
        {
            NarrativeStatePart.Current = null;
            ConversationPredicates.Reset();
            ConversationActions.Reset();
        }

        // ==========================================================
        // IfFact predicate
        // ==========================================================

        [Test]
        public void IfFact_EqualOp_TrueWhenFactEqualsValue()
        {
            _statePart.SetFact("questStage", 3);
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "questStage:=:3");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfFact_EqualOp_FalseWhenFactDiffers()
        {
            _statePart.SetFact("questStage", 2);
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "questStage:=:3");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfFact_GteOp_TrueWhenFactAtThreshold()
        {
            _statePart.SetFact("kills", 3);
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "kills:>=:3");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfFact_GteOp_TrueWhenFactAboveThreshold()
        {
            _statePart.SetFact("kills", 5);
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "kills:>=:3");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfFact_GteOp_FalseWhenFactBelowThreshold()
        {
            _statePart.SetFact("kills", 2);
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "kills:>=:3");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfFact_GtOp_Correct()
        {
            _statePart.SetFact("x", 3);
            Assert.IsTrue(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:>:2"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:>:3"));
        }

        [Test]
        public void IfFact_LtOp_Correct()
        {
            _statePart.SetFact("x", 2);
            Assert.IsTrue(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:<:3"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:<:2"));
        }

        [Test]
        public void IfFact_LteOp_Correct()
        {
            _statePart.SetFact("x", 2);
            Assert.IsTrue(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:<=:2"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:<=:1"));
        }

        [Test]
        public void IfFact_NotEqualOp_Correct()
        {
            _statePart.SetFact("x", 2);
            Assert.IsTrue(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:!=:3"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:!=:2"));
        }

        [Test]
        public void IfFact_UnknownFact_TreatsAsZero()
        {
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "missing:=:0");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfFact_MalformedArg_ReturnsFalse()
        {
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "badarg");
            Assert.IsFalse(result, "IfFact is fail-closed: malformed args return false");
        }

        [Test]
        public void IfFact_NoCurrentNarrativeState_ReturnsFalse()
        {
            NarrativeStatePart.Current = null;
            bool result = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:=:0");
            Assert.IsFalse(result, "IfFact is fail-closed: no state returns false");
        }

        // Auto-inverse via existing IfNot* mechanism
        [Test]
        public void IfNotFact_InvertsIfFact()
        {
            _statePart.SetFact("x", 3);
            bool ifFact = ConversationPredicates.Evaluate("IfFact", _speaker, _listener, "x:=:3");
            bool ifNotFact = ConversationPredicates.Evaluate("IfNotFact", _speaker, _listener, "x:=:3");
            Assert.IsTrue(ifFact);
            Assert.IsFalse(ifNotFact);
        }

        // ==========================================================
        // IfSpeakerKnows predicate
        // ==========================================================

        [Test]
        public void IfSpeakerKnows_TrueWhenKnowledgeMeetsTier()
        {
            _speaker.GetPart<KnowledgePart>().Reveal("conspiracy", 3);
            bool result = ConversationPredicates.Evaluate("IfSpeakerKnows", _speaker, _listener, "conspiracy:3");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfSpeakerKnows_TrueWhenKnowledgeExceedsTier()
        {
            _speaker.GetPart<KnowledgePart>().Reveal("conspiracy", 3);
            bool result = ConversationPredicates.Evaluate("IfSpeakerKnows", _speaker, _listener, "conspiracy:2");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfSpeakerKnows_FalseWhenKnowledgeBelowTier()
        {
            _speaker.GetPart<KnowledgePart>().Reveal("conspiracy", 1);
            bool result = ConversationPredicates.Evaluate("IfSpeakerKnows", _speaker, _listener, "conspiracy:3");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfSpeakerKnows_FalseWhenSpeakerHasNoKnowledgePart()
        {
            var bareNpc = new Entity { BlueprintName = "Bare" };
            bool result = ConversationPredicates.Evaluate("IfSpeakerKnows", bareNpc, _listener, "conspiracy:1");
            Assert.IsFalse(result, "IfSpeakerKnows is fail-closed: no KnowledgePart returns false");
        }

        [Test]
        public void IfSpeakerKnows_MalformedArg_ReturnsFalse()
        {
            bool result = ConversationPredicates.Evaluate("IfSpeakerKnows", _speaker, _listener, "onlykey");
            Assert.IsFalse(result, "IfSpeakerKnows is fail-closed: malformed args return false");
        }

        [Test]
        public void IfNotSpeakerKnows_InvertsIfSpeakerKnows()
        {
            _speaker.GetPart<KnowledgePart>().Reveal("conspiracy", 3);
            bool knows = ConversationPredicates.Evaluate("IfSpeakerKnows", _speaker, _listener, "conspiracy:3");
            bool notKnows = ConversationPredicates.Evaluate("IfNotSpeakerKnows", _speaker, _listener, "conspiracy:3");
            Assert.IsTrue(knows);
            Assert.IsFalse(notKnows);
        }

        // ==========================================================
        // SetFact action
        // ==========================================================

        [Test]
        public void SetFact_SetsGlobalFact()
        {
            ConversationActions.Execute("SetFact", _speaker, _listener, "questStage:5");
            Assert.AreEqual(5, _statePart.GetFact("questStage"));
        }

        [Test]
        public void SetFact_OverwritesExistingValue()
        {
            _statePart.SetFact("x", 1);
            ConversationActions.Execute("SetFact", _speaker, _listener, "x:9");
            Assert.AreEqual(9, _statePart.GetFact("x"));
        }

        [Test]
        public void SetFact_MalformedArg_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("SetFact", _speaker, _listener, "nokeyvalue"));
        }

        // ==========================================================
        // AddFact action
        // ==========================================================

        [Test]
        public void AddFact_IncrementsGlobalFact()
        {
            _statePart.SetFact("score", 3);
            ConversationActions.Execute("AddFact", _speaker, _listener, "score:2");
            Assert.AreEqual(5, _statePart.GetFact("score"));
        }

        [Test]
        public void AddFact_NegativeDelta_Decrements()
        {
            _statePart.SetFact("score", 5);
            ConversationActions.Execute("AddFact", _speaker, _listener, "score:-2");
            Assert.AreEqual(3, _statePart.GetFact("score"));
        }

        // ==========================================================
        // ClearFact action
        // ==========================================================

        [Test]
        public void ClearFact_RemovesGlobalFact()
        {
            _statePart.SetFact("flag", 1);
            ConversationActions.Execute("ClearFact", _speaker, _listener, "flag");
            Assert.AreEqual(0, _statePart.GetFact("flag"));
        }

        // ==========================================================
        // Reveal action
        // ==========================================================

        [Test]
        public void Reveal_Listener_SetsKnowledgeOnListener()
        {
            ConversationActions.Execute("Reveal", _speaker, _listener, "Listener:conspiracy:3");
            Assert.AreEqual(3, _listener.GetPart<KnowledgePart>().GetKnowledge("conspiracy"));
        }

        [Test]
        public void Reveal_Speaker_SetsKnowledgeOnSpeaker()
        {
            ConversationActions.Execute("Reveal", _speaker, _listener, "Speaker:localSecret:2");
            Assert.AreEqual(2, _speaker.GetPart<KnowledgePart>().GetKnowledge("localSecret"));
        }

        [Test]
        public void Reveal_DoesNotDecreaseExistingKnowledge()
        {
            _listener.GetPart<KnowledgePart>().Reveal("secret", 3);
            ConversationActions.Execute("Reveal", _speaker, _listener, "Listener:secret:1");
            Assert.AreEqual(3, _listener.GetPart<KnowledgePart>().GetKnowledge("secret"));
        }

        [Test]
        public void Reveal_MalformedArg_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("Reveal", _speaker, _listener, "badformat"));
        }

        [Test]
        public void Reveal_NoKnowledgePart_DoesNotThrow()
        {
            var bareEntity = new Entity { BlueprintName = "Bare" };
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("Reveal", _speaker, bareEntity, "Listener:secret:2"));
        }
    }
}
