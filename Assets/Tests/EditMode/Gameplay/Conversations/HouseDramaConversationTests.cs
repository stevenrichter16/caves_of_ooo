using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class HouseDramaConversationTests
    {
        private const string SettlementId = SettlementSiteDefinitions.StartingVillageZoneId;
        private const string DramaId = "OrdrenDrama";
        private const string PressurePointId = "Wound";

        private SettlementManager _manager;
        private Entity _speaker;
        private Entity _listener;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            ConversationPredicates.Reset();
            ConversationActions.Reset();
            SettlementRuntime.Reset();

            _manager = new SettlementManager(() => 0, _ => new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));
            _manager.GetOrCreateSettlement(SettlementId, new PointOfInterest(POIType.Village, "Kyakukya", "Villagers", 1));

            _speaker = new Entity { BlueprintName = "HouseElder" };
            _speaker.Properties["SettlementId"] = SettlementId;
            _listener = new Entity { BlueprintName = "Player" };
            _listener.AddPart(new InventoryPart());
        }

        [TearDown]
        public void TearDown()
        {
            SettlementManager.ResetCurrent();
            SettlementRuntime.Reset();
            ConversationPredicates.Reset();
            ConversationActions.Reset();
        }

        // --- ActivateDrama action ---

        [Test]
        public void ActivateDrama_CreatesDramaInActiveState()
        {
            ConversationActions.Execute("ActivateDrama", _speaker, _listener, DramaId);

            HouseDramaState drama = _manager.GetDrama(SettlementId, DramaId);
            Assert.IsNotNull(drama);
            Assert.AreEqual(HouseDramaActivationState.Active, drama.State);
        }

        [Test]
        public void ActivateDrama_IsIdempotent_WhenAlreadyActive()
        {
            ConversationActions.Execute("ActivateDrama", _speaker, _listener, DramaId);
            ConversationActions.Execute("ActivateDrama", _speaker, _listener, DramaId);

            Assert.AreEqual(HouseDramaActivationState.Active, _manager.GetDrama(SettlementId, DramaId).State);
        }

        // --- ResolveDramaPressurePoint action ---

        [Test]
        public void ResolveDramaPressurePoint_SetsResolvedStateAndPathTaken()
        {
            _manager.ActivateDrama(SettlementId, DramaId);

            ConversationActions.Execute("ResolveDramaPressurePoint", _speaker, _listener,
                $"{DramaId}:{PressurePointId}:CounselPath");

            HousePressurePointState pp = _manager.GetDrama(SettlementId, DramaId).GetPressurePoint(PressurePointId);
            Assert.IsNotNull(pp);
            Assert.AreEqual(HouseDramaActivationState.Resolved, pp.State);
            Assert.AreEqual("CounselPath", pp.PathTaken);
            Assert.AreEqual("resolved:complete", pp.Substate);
        }

        // --- FailDramaPressurePoint action ---

        [Test]
        public void FailDramaPressurePoint_SetsFailedStateAndSubstate()
        {
            _manager.ActivateDrama(SettlementId, DramaId);

            ConversationActions.Execute("FailDramaPressurePoint", _speaker, _listener,
                $"{DramaId}:{PressurePointId}:failed:escalated");

            HousePressurePointState pp = _manager.GetDrama(SettlementId, DramaId).GetPressurePoint(PressurePointId);
            Assert.IsNotNull(pp);
            Assert.AreEqual(HouseDramaActivationState.Failed, pp.State);
            Assert.AreEqual("failed:escalated", pp.Substate);
        }

        // --- SetDramaEndState action ---

        [Test]
        public void SetDramaEndState_MarksRestoredAndClosesActivation()
        {
            _manager.ActivateDrama(SettlementId, DramaId);

            ConversationActions.Execute("SetDramaEndState", _speaker, _listener, $"{DramaId}:Restored");

            HouseDramaState drama = _manager.GetDrama(SettlementId, DramaId);
            Assert.AreEqual(HouseDramaEndState.Restored, drama.EndState);
            Assert.AreEqual(HouseDramaActivationState.Resolved, drama.State);
        }

        [Test]
        public void SetDramaEndState_AllEndStatesParseable()
        {
            string[] endStates = { "Restored", "TransformedA", "TransformedB", "Extinct", "Corrupted" };
            foreach (string es in endStates)
            {
                _manager.ActivateDrama(SettlementId, es + "_Drama");
                _speaker.Properties["SettlementId"] = SettlementId;
                ConversationActions.Execute("SetDramaEndState", _speaker, _listener, $"{es}_Drama:{es}");
                HouseDramaState drama = _manager.GetDrama(SettlementId, es + "_Drama");
                Assert.IsNotNull(drama, $"Drama not found for end state {es}");
                Assert.AreEqual(HouseDramaActivationState.Resolved, drama.State, $"State not Resolved for {es}");
            }
        }

        // --- AddDramaCorruption action ---

        [Test]
        public void AddDramaCorruption_AccumulatesScore()
        {
            _manager.ActivateDrama(SettlementId, DramaId);

            ConversationActions.Execute("AddDramaCorruption", _speaker, _listener, $"{DramaId}:2");
            ConversationActions.Execute("AddDramaCorruption", _speaker, _listener, $"{DramaId}:3");

            Assert.AreEqual(5, _manager.GetDrama(SettlementId, DramaId).CorruptionScore);
        }

        // --- IfDramaPressurePointState predicate ---

        [Test]
        public void IfDramaPressurePointState_ReturnsFalse_WhenDramaMissing()
        {
            bool result = ConversationPredicates.Evaluate(
                "IfDramaPressurePointState", _speaker, _listener, $"{DramaId}:{PressurePointId}:Active");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfDramaPressurePointState_ReturnsFalse_WhenPressurePointMissing()
        {
            _manager.ActivateDrama(SettlementId, DramaId);

            bool result = ConversationPredicates.Evaluate(
                "IfDramaPressurePointState", _speaker, _listener, $"{DramaId}:{PressurePointId}:Resolved");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfDramaPressurePointState_ReturnsTrue_AfterResolution()
        {
            _manager.ActivateDrama(SettlementId, DramaId);
            _manager.ResolvePressurePoint(SettlementId, DramaId, PressurePointId, "CounselPath");

            bool result = ConversationPredicates.Evaluate(
                "IfDramaPressurePointState", _speaker, _listener, $"{DramaId}:{PressurePointId}:Resolved");
            Assert.IsTrue(result);
        }

        // --- IfDramaEndState predicate ---

        [Test]
        public void IfDramaEndState_ReturnsFalse_WhenDramaMissing()
        {
            bool result = ConversationPredicates.Evaluate(
                "IfDramaEndState", _speaker, _listener, $"{DramaId}:Restored");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfDramaEndState_ReturnsTrue_AfterEndStateSet()
        {
            _manager.ActivateDrama(SettlementId, DramaId);
            _manager.SetDramaEndState(SettlementId, DramaId, HouseDramaEndState.Restored);

            bool result = ConversationPredicates.Evaluate(
                "IfDramaEndState", _speaker, _listener, $"{DramaId}:Restored");
            Assert.IsTrue(result);
        }

        [Test]
        public void IfDramaEndState_ReturnsFalse_ForWrongEndState()
        {
            _manager.ActivateDrama(SettlementId, DramaId);
            _manager.SetDramaEndState(SettlementId, DramaId, HouseDramaEndState.Corrupted);

            bool result = ConversationPredicates.Evaluate(
                "IfDramaEndState", _speaker, _listener, $"{DramaId}:Restored");
            Assert.IsFalse(result);
        }

        // --- IfDramaCorruptionAtLeast predicate ---

        [Test]
        public void IfDramaCorruptionAtLeast_ReturnsFalse_WhenBelowThreshold()
        {
            _manager.ActivateDrama(SettlementId, DramaId);
            _manager.AddDramaCorruption(SettlementId, DramaId, 2);

            bool result = ConversationPredicates.Evaluate(
                "IfDramaCorruptionAtLeast", _speaker, _listener, $"{DramaId}:3");
            Assert.IsFalse(result);
        }

        [Test]
        public void IfDramaCorruptionAtLeast_ReturnsTrue_WhenAtThreshold()
        {
            _manager.ActivateDrama(SettlementId, DramaId);
            _manager.AddDramaCorruption(SettlementId, DramaId, 3);

            bool result = ConversationPredicates.Evaluate(
                "IfDramaCorruptionAtLeast", _speaker, _listener, $"{DramaId}:3");
            Assert.IsTrue(result);
        }

        // --- DramaStateChanged event ---

        [Test]
        public void DramaStateChanged_FiresOnActivation()
        {
            HouseDramaState eventDrama = null;
            _manager.DramaStateChanged += (_, d) => eventDrama = d;

            _manager.ActivateDrama(SettlementId, DramaId);

            Assert.IsNotNull(eventDrama);
            Assert.AreEqual(DramaId, eventDrama.DramaId);
        }

        [Test]
        public void DramaStateChanged_FiresOnPressurePointResolution()
        {
            _manager.ActivateDrama(SettlementId, DramaId);
            HouseDramaState eventDrama = null;
            _manager.DramaStateChanged += (_, d) => eventDrama = d;

            _manager.ResolvePressurePoint(SettlementId, DramaId, PressurePointId, "CounselPath");

            Assert.IsNotNull(eventDrama);
        }
    }
}
