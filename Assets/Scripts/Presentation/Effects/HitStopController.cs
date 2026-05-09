using System.Collections;
using UnityEngine;

namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>
    /// Combat impact-frame controller. Briefly freezes
    /// <see cref="Time.timeScale"/> on hit events to make crits/
    /// kills/dismembers feel weighty. Mirrors Hyper Light Drifter's
    /// "hit-stop" — a short pause + screen shake at the moment of
    /// impact reads as a punch, not a tap.
    ///
    /// <para><b>Three intensity tiers:</b>
    /// <list type="bullet">
    ///   <item><see cref="LIGHT_DURATION_MS"/> ≈80ms — every melee
    ///         hit (default off; opt in with PunchLight).</item>
    ///   <item><see cref="MEDIUM_DURATION_MS"/> ≈150ms — crits.</item>
    ///   <item><see cref="HEAVY_DURATION_MS"/> ≈250ms — kills,
    ///         dismembers.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Singleton pattern:</b> a single global
    /// <see cref="Instance"/> per scene, set by
    /// <c>GameBootstrap</c>. Combat code calls
    /// <see cref="Instance"/>.<see cref="PunchHeavy"/>() etc. without
    /// having to plumb a reference.</para>
    ///
    /// <para><b>Coroutine semantics:</b> the freeze coroutine uses
    /// <see cref="WaitForSecondsRealtime"/> so it unfreezes even
    /// when <c>Time.timeScale = 0</c>. Nested punches extend the
    /// remaining freeze time (rather than truncating).</para>
    ///
    /// <para>See <c>Docs/GRAPHICS-PASS4.md</c> §4A for the design
    /// rationale.</para>
    /// </summary>
    public class HitStopController : MonoBehaviour
    {
        public const float LIGHT_DURATION_MS = 80f;
        public const float MEDIUM_DURATION_MS = 150f;
        public const float HEAVY_DURATION_MS = 250f;

        public static HitStopController Instance { get; private set; }

        // Tracks remaining freeze duration so nested punches can
        // extend (max-of) rather than overwrite. Stored in real-time
        // seconds; counts down via the coroutine.
        private float _remainingRealtimeSeconds;
        private Coroutine _activeCoroutine;
        private float _baseTimeScale = 1f;

        private void Awake()
        {
            // Last-instance-wins. Scene reloads (per project memory:
            // entering Play mode resets the scene) re-create the
            // controller; that's fine.
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            // Defensive: if we're destroyed mid-freeze, restore
            // timeScale so the game doesn't get stuck paused.
            if (Time.timeScale == 0f)
                Time.timeScale = _baseTimeScale;
        }

        /// <summary>
        /// Freeze for the LIGHT duration (~80ms). Use sparingly —
        /// every-melee-hit at this duration adds up fast.
        /// </summary>
        public void PunchLight() => Punch(LIGHT_DURATION_MS);

        /// <summary>
        /// Freeze for the MEDIUM duration (~150ms). Default for
        /// critical hits.
        /// </summary>
        public void PunchMedium() => Punch(MEDIUM_DURATION_MS);

        /// <summary>
        /// Freeze for the HEAVY duration (~250ms). Default for
        /// kills, dismembers, and other "big moment" events.
        /// </summary>
        public void PunchHeavy() => Punch(HEAVY_DURATION_MS);

        /// <summary>
        /// Freeze for the given duration in milliseconds. Public
        /// so combat code can pass custom durations (e.g., scaled
        /// by damage amount).
        /// </summary>
        public void Punch(float durationMs)
        {
            if (durationMs <= 0f) return;
            float realtimeSeconds = durationMs * 0.001f;

            // Nested-punch behavior: extend the remaining time to
            // the max of (current remaining, new request). Avoids
            // truncating an in-progress big freeze with a smaller
            // one (e.g., a kill mid-flicker shouldn't shorten its
            // own punch).
            if (realtimeSeconds > _remainingRealtimeSeconds)
                _remainingRealtimeSeconds = realtimeSeconds;

            if (_activeCoroutine == null)
            {
                _baseTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                _activeCoroutine = StartCoroutine(FreezeCoroutine());
            }
        }

        private IEnumerator FreezeCoroutine()
        {
            Time.timeScale = 0f;
            // Loop in case nested punches extended the duration
            // mid-freeze — we sleep in small slices and re-check.
            const float TICK_REALTIME_SECONDS = 0.02f; // 20ms slice
            while (_remainingRealtimeSeconds > 0f)
            {
                float slice = Mathf.Min(TICK_REALTIME_SECONDS, _remainingRealtimeSeconds);
                yield return new WaitForSecondsRealtime(slice);
                _remainingRealtimeSeconds -= slice;
            }
            Time.timeScale = _baseTimeScale;
            _activeCoroutine = null;
        }

        // ── Test seams ───────────────────────────────────────────────────
        // EditMode tests can't run coroutines, so they exercise the
        // logic directly via these test-only methods.

        /// <summary>
        /// Test seam: invoke the freeze logic synchronously without
        /// the coroutine. EditMode-only. Production code calls
        /// <see cref="Punch"/>.
        /// </summary>
        public void TestOnly_BeginFreeze(float durationMs)
        {
            if (durationMs <= 0f) return;
            float realtimeSeconds = durationMs * 0.001f;
            if (realtimeSeconds > _remainingRealtimeSeconds)
                _remainingRealtimeSeconds = realtimeSeconds;
            if (Time.timeScale > 0f)
                _baseTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Test seam: peek at remaining freeze (real-time seconds).
        /// </summary>
        public float TestOnly_RemainingSeconds => _remainingRealtimeSeconds;

        /// <summary>
        /// Test seam: simulate elapsed real-time and end the freeze
        /// when the remaining time reaches zero.
        /// </summary>
        public void TestOnly_AdvanceRealtime(float deltaSeconds)
        {
            if (Time.timeScale != 0f) return;
            _remainingRealtimeSeconds -= deltaSeconds;
            if (_remainingRealtimeSeconds <= 0f)
            {
                _remainingRealtimeSeconds = 0f;
                Time.timeScale = _baseTimeScale;
            }
        }
    }
}
