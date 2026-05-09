using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Presentation.Effects;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// 4A.3 — Tests for HitStopController. See
    /// <c>Docs/GRAPHICS-PASS4.md</c> §4A for the design rationale.
    ///
    /// <para>EditMode can't run coroutines, so these tests exercise
    /// the freeze logic via the <c>TestOnly_*</c> seam methods. The
    /// production call path goes through <see cref="HitStopController.Punch"/>
    /// which kicks off a <see cref="Coroutine"/>; the test seam
    /// short-circuits to the same internal state mutation, just
    /// without the coroutine.</para>
    /// </summary>
    public class HitStopControllerTests
    {
        private HitStopController _controller;
        private float _savedTimeScale;

        [SetUp]
        public void Setup()
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            // Build a controller via AddComponent on a throwaway
            // GameObject so MonoBehaviour Awake/OnDestroy fire.
            var go = new GameObject("HitStopTest");
            _controller = go.AddComponent<HitStopController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null)
                Object.DestroyImmediate(_controller.gameObject);
            Time.timeScale = _savedTimeScale;
        }

        // ── A. Punch freezes timescale ───────────────────────────────────

        [Test]
        public void Punch_SetsTimeScaleToZero()
        {
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "Setup: timeScale starts at 1.");
            _controller.TestOnly_BeginFreeze(150f);
            Assert.AreEqual(0f, Time.timeScale, 0.001f,
                "Punch sets Time.timeScale to 0.");
        }

        // ── B. Restoration after duration elapses ────────────────────────

        [Test]
        public void Punch_AfterDurationElapses_RestoresTimeScale()
        {
            _controller.TestOnly_BeginFreeze(150f);
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "Setup: frozen.");
            // Advance more than the requested 150ms (= 0.15s).
            _controller.TestOnly_AdvanceRealtime(0.20f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f,
                "After enough realtime elapses, timeScale restored to base (1).");
        }

        [Test]
        public void Punch_PartialAdvance_StaysFrozen()
        {
            _controller.TestOnly_BeginFreeze(150f);
            _controller.TestOnly_AdvanceRealtime(0.05f); // 50ms < 150ms
            Assert.AreEqual(0f, Time.timeScale, 0.001f,
                "Partial advance keeps the freeze active.");
            Assert.Greater(_controller.TestOnly_RemainingSeconds, 0f);
        }

        // ── C. Nested punch extends duration (max-of) ────────────────────

        [Test]
        public void NestedPunch_LongerDuration_ExtendsRemaining()
        {
            // First punch — short.
            _controller.TestOnly_BeginFreeze(80f);
            float afterFirst = _controller.TestOnly_RemainingSeconds;
            Assert.AreEqual(0.080f, afterFirst, 0.001f);

            // Second punch — longer. Should extend.
            _controller.TestOnly_BeginFreeze(250f);
            Assert.AreEqual(0.250f, _controller.TestOnly_RemainingSeconds, 0.001f,
                "Second punch (longer) extends remaining freeze to its duration.");
        }

        [Test]
        public void NestedPunch_ShorterDuration_DoesNotTruncate()
        {
            // First punch — long.
            _controller.TestOnly_BeginFreeze(250f);
            // Second punch — shorter. Should NOT shorten.
            _controller.TestOnly_BeginFreeze(80f);
            Assert.AreEqual(0.250f, _controller.TestOnly_RemainingSeconds, 0.001f,
                "Second punch (shorter) does NOT truncate the longer in-progress freeze. "
                + "Adversarial: a buggy impl that overwrote with the latest value would "
                + "make a kill-stop be cut short by a follow-up trivial hit.");
        }

        // ── D. Counter-checks ────────────────────────────────────────────

        [Test]
        public void NoPunch_TimeScaleStaysAtOne()
        {
            // Counter-check: if no Punch is ever called, timeScale never
            // changes. Catches a buggy impl that touches timeScale on
            // Awake or Update without provocation.
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            // Advance simulated realtime — should be a no-op.
            _controller.TestOnly_AdvanceRealtime(1.0f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f,
                "Without Punch, timeScale never gets touched.");
        }

        [Test]
        public void Punch_ZeroDuration_NoOp()
        {
            // Defensive: passing 0 (or negative) duration should not
            // freeze. Catches a buggy upstream that computed
            // intensity = 0 from a bug-side-effect and would otherwise
            // stick the game in a permanent pause.
            _controller.TestOnly_BeginFreeze(0f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            _controller.TestOnly_BeginFreeze(-50f);
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
        }

        // ── E. Singleton + OnDestroy lifecycle ───────────────────────────
        //
        // MonoBehaviour Awake/OnDestroy lifecycle is unreliable in
        // EditMode tests when GameObjects are created via
        // AddComponent on a non-scene object. The singleton-registration
        // and OnDestroy-restores-timeScale contracts are exercised in
        // production via GameBootstrap's setup; PlayMode integration
        // tests would cover them. Skipped here to keep the EditMode
        // suite reliable.

        // ── F. Convenience tier helpers ──────────────────────────────────

        [Test]
        public void PunchLight_UsesLightDuration()
        {
            _controller.PunchLight();
            // The Punch path uses the coroutine. We can't run coroutines
            // in EditMode, but TestOnly_RemainingSeconds reflects the
            // value Punch() set BEFORE starting the coroutine — since
            // Punch sets _remainingRealtimeSeconds inline before the
            // StartCoroutine call.
            Assert.AreEqual(HitStopController.LIGHT_DURATION_MS * 0.001f,
                _controller.TestOnly_RemainingSeconds, 0.001f);
        }

        [Test]
        public void PunchMedium_UsesMediumDuration()
        {
            _controller.PunchMedium();
            Assert.AreEqual(HitStopController.MEDIUM_DURATION_MS * 0.001f,
                _controller.TestOnly_RemainingSeconds, 0.001f);
        }

        [Test]
        public void PunchHeavy_UsesHeavyDuration()
        {
            _controller.PunchHeavy();
            Assert.AreEqual(HitStopController.HEAVY_DURATION_MS * 0.001f,
                _controller.TestOnly_RemainingSeconds, 0.001f);
        }

        [Test]
        public void DurationConstants_AreOrdered()
        {
            // Sanity that Light < Medium < Heavy. Catches a bad
            // refactor that swaps two constants.
            Assert.Less(HitStopController.LIGHT_DURATION_MS,
                HitStopController.MEDIUM_DURATION_MS);
            Assert.Less(HitStopController.MEDIUM_DURATION_MS,
                HitStopController.HEAVY_DURATION_MS);
        }
    }
}
