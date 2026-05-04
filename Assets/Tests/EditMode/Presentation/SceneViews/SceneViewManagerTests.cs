using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Presentation.SceneViews
{
    /// <summary>
    /// M1 TDD tests: SceneViewManager state-machine invariants.
    /// Mirrors the ConversationManager.IsActive static-singleton pattern.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M1
    /// </summary>
    public class SceneViewManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            SceneViewManager.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SceneViewManager.Reset();
        }

        // ---- Initial state ----

        [Test]
        public void InitialState_IsNotActive()
        {
            Assert.IsFalse(SceneViewManager.IsActive);
            Assert.IsNull(SceneViewManager.ActiveSceneID);
        }

        // ---- Activate ----

        [Test]
        public void Activate_SetsIsActive()
        {
            SceneViewManager.Activate("Campfire");
            Assert.IsTrue(SceneViewManager.IsActive);
        }

        [Test]
        public void Activate_StoresSceneID()
        {
            SceneViewManager.Activate("Campfire");
            Assert.AreEqual("Campfire", SceneViewManager.ActiveSceneID);
        }

        [Test]
        public void Activate_WithNullID_DoesNotActivate()
        {
            SceneViewManager.Activate(null);
            Assert.IsFalse(SceneViewManager.IsActive,
                "Null scene ID is not a valid activation");
        }

        [Test]
        public void Activate_WithEmptyID_DoesNotActivate()
        {
            SceneViewManager.Activate("");
            Assert.IsFalse(SceneViewManager.IsActive,
                "Empty scene ID is not a valid activation");
        }

        // ---- Deactivate ----

        [Test]
        public void Deactivate_ClearsIsActive()
        {
            SceneViewManager.Activate("Campfire");
            SceneViewManager.Deactivate();
            Assert.IsFalse(SceneViewManager.IsActive);
        }

        [Test]
        public void Deactivate_ClearsSceneID()
        {
            SceneViewManager.Activate("Campfire");
            SceneViewManager.Deactivate();
            Assert.IsNull(SceneViewManager.ActiveSceneID);
        }

        [Test]
        public void Deactivate_WhenNotActive_DoesNotThrow()
        {
            // Counter-check: deactivating without prior activation is safe.
            Assert.DoesNotThrow(() => SceneViewManager.Deactivate());
            Assert.IsFalse(SceneViewManager.IsActive);
        }

        // ---- Re-activate ----

        [Test]
        public void Activate_WhenAlreadyActive_ReplacesScene()
        {
            SceneViewManager.Activate("Campfire");
            SceneViewManager.Activate("Vista");
            Assert.IsTrue(SceneViewManager.IsActive);
            Assert.AreEqual("Vista", SceneViewManager.ActiveSceneID);
        }

        // ---- Events ----

        [Test]
        public void Activate_FiresOnActivatedEvent()
        {
            string capturedID = null;
            int callCount = 0;
            void Handler(string id) { capturedID = id; callCount++; }
            SceneViewManager.OnActivated += Handler;
            try
            {
                SceneViewManager.Activate("Campfire");
            }
            finally { SceneViewManager.OnActivated -= Handler; }

            Assert.AreEqual(1, callCount);
            Assert.AreEqual("Campfire", capturedID);
        }

        [Test]
        public void Deactivate_FiresOnDeactivatedEvent()
        {
            int callCount = 0;
            void Handler() { callCount++; }
            SceneViewManager.OnDeactivated += Handler;
            try
            {
                SceneViewManager.Activate("Campfire");
                SceneViewManager.Deactivate();
            }
            finally { SceneViewManager.OnDeactivated -= Handler; }

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Deactivate_WhenNotActive_DoesNotFireEvent()
        {
            // Counter-check: deactivating an already-inactive manager
            // must not fire the deactivated event (subscribers shouldn't
            // see spurious events).
            int callCount = 0;
            void Handler() { callCount++; }
            SceneViewManager.OnDeactivated += Handler;
            try
            {
                SceneViewManager.Deactivate();
            }
            finally { SceneViewManager.OnDeactivated -= Handler; }

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Activate_WithInvalidID_DoesNotFireEvent()
        {
            // Counter-check: invalid activations don't fire events
            int callCount = 0;
            void Handler(string id) { callCount++; }
            SceneViewManager.OnActivated += Handler;
            try
            {
                SceneViewManager.Activate("");
                SceneViewManager.Activate(null);
            }
            finally { SceneViewManager.OnActivated -= Handler; }

            Assert.AreEqual(0, callCount);
        }

        // ---- Reset (for tests) ----

        [Test]
        public void Reset_ClearsState()
        {
            SceneViewManager.Activate("Campfire");
            SceneViewManager.Reset();
            Assert.IsFalse(SceneViewManager.IsActive);
            Assert.IsNull(SceneViewManager.ActiveSceneID);
        }

        [Test]
        public void Reset_ClearsEventSubscribers()
        {
            // After Reset(), previously-subscribed handlers must not fire.
            int callCount = 0;
            void Handler(string id) { callCount++; }
            SceneViewManager.OnActivated += Handler;
            SceneViewManager.Reset();
            SceneViewManager.Activate("Campfire");

            Assert.AreEqual(0, callCount,
                "Reset should clear event subscriptions to prevent test cross-pollination");
        }
    }
}
