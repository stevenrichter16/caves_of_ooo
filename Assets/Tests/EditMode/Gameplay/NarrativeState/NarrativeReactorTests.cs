using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// M4b TDD tests: INarrativeReactor registry and dispatch.
    ///
    /// Invariants:
    ///   - A registered reactor's OnTickEnd is called when TickEnd fires on the world entity.
    ///   - Multiple reactors all receive dispatch.
    ///   - Unregistering a reactor stops it from receiving dispatches.
    ///   - Reactors can read current NarrativeStatePart facts via the callback.
    /// </summary>
    public class NarrativeReactorTests
    {
        private Entity _world;
        private NarrativeStatePart _statePart;

        [SetUp]
        public void SetUp()
        {
            _world = new Entity { BlueprintName = "World" };
            _statePart = new NarrativeStatePart();
            _world.AddPart(_statePart);
            NarrativeStatePart.Current = _statePart;
            TurnManager.World = _world;
        }

        [TearDown]
        public void TearDown()
        {
            NarrativeStatePart.Current = null;
            TurnManager.World = null;
        }

        // --- Registration and dispatch ---

        [Test]
        public void RegisteredReactor_ReceivesOnTickEnd_WhenTickEndFires()
        {
            var reactor = new CountingReactor();
            _statePart.RegisterReactor(reactor);

            FireTickEnd();

            Assert.AreEqual(1, reactor.TickEndCount,
                "Registered reactor should receive OnTickEnd after TickEnd event");
        }

        [Test]
        public void UnregisteredReactor_DoesNotReceiveOnTickEnd()
        {
            var reactor = new CountingReactor();
            _statePart.RegisterReactor(reactor);
            _statePart.UnregisterReactor(reactor);

            FireTickEnd();

            Assert.AreEqual(0, reactor.TickEndCount);
        }

        [Test]
        public void MultipleReactors_AllReceiveDispatch()
        {
            var r1 = new CountingReactor();
            var r2 = new CountingReactor();
            _statePart.RegisterReactor(r1);
            _statePart.RegisterReactor(r2);

            FireTickEnd();

            Assert.AreEqual(1, r1.TickEndCount);
            Assert.AreEqual(1, r2.TickEndCount);
        }

        [Test]
        public void Reactor_CanReadFactsFromStatePart()
        {
            _statePart.SetFact("questStage", 4);
            string capturedKey = null;
            int capturedValue = -1;

            var reactor = new LambdaReactor(state =>
            {
                capturedKey = "questStage";
                capturedValue = state.GetFact("questStage");
            });
            _statePart.RegisterReactor(reactor);

            FireTickEnd();

            Assert.AreEqual("questStage", capturedKey);
            Assert.AreEqual(4, capturedValue);
        }

        [Test]
        public void NoReactors_TickEnd_DoesNotThrow()
        {
            Assert.DoesNotThrow(FireTickEnd);
        }

        [Test]
        public void RegisterSameReactorTwice_OnlyDispatchedOnce()
        {
            var reactor = new CountingReactor();
            _statePart.RegisterReactor(reactor);
            _statePart.RegisterReactor(reactor);

            FireTickEnd();

            Assert.AreEqual(1, reactor.TickEndCount,
                "Duplicate registration should be idempotent");
        }

        // --- Helpers ---

        private void FireTickEnd()
        {
            var e = GameEvent.New("TickEnd");
            _world.FireEvent(e);
            e.Release();
        }

        private sealed class CountingReactor : INarrativeReactor
        {
            public int TickEndCount;
            public void OnTickEnd(NarrativeStatePart state) => TickEndCount++;
        }

        private sealed class LambdaReactor : INarrativeReactor
        {
            private readonly System.Action<NarrativeStatePart> _callback;
            public LambdaReactor(System.Action<NarrativeStatePart> callback) => _callback = callback;
            public void OnTickEnd(NarrativeStatePart state) => _callback(state);
        }
    }
}
