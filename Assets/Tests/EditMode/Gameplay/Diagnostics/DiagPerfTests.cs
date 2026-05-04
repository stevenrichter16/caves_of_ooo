using System;
using System.Diagnostics;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D1.5 micro-benchmarks for the Diag substrate + RemoveEffect hook.
    ///
    /// Plan ref: <c>Docs/D1-SPIKE-PLAN.md</c> §5 D1.5.
    /// Performance contract: <c>Docs/AI-OBSERVABILITY.md</c> §3 Layer 0
    /// "off-default for chatty categories" + §10 budget.
    ///
    /// These are coarse, environment-dependent timing tests — the goal
    /// isn't to assert hard nanosecond ceilings (an editor running
    /// alongside other processes can vary by 5-10×), it's to catch
    /// regressions of orders-of-magnitude. Thresholds are intentionally
    /// loose: if a future change makes the disabled-channel path 100×
    /// slower, this fails. If it makes it 2× slower, it doesn't —
    /// micro-benchmarks in NUnit can't reliably distinguish that.
    ///
    /// Three benchmarks per the plan:
    ///   17. Disabled-channel overhead — IsChannelEnabled short-circuit
    ///       makes Record() effectively free. Target: ≤ 200ns/call
    ///       averaged over 100k iterations.
    ///   18. Enabled-channel overhead with payload — full path including
    ///       JSON serialization. Target: ≤ 50µs/call averaged over 10k
    ///       iterations.
    ///   19. RemoveEffect with the D1.2 hook firing — 1000 apply/remove
    ///       cycles with channel on. Target: ≤ 500ms total.
    ///
    /// If a future Diag change blows past these, the hook should be
    /// gated more aggressively (call-site IsChannelEnabled checks,
    /// off-default for the offending category, payload size cap, etc.).
    /// </summary>
    [Category("Performance")]
    public class DiagPerfTests
    {
        private const int DisabledIterations = 100_000;
        private const int EnabledIterations = 10_000;
        private const int RemoveEffectIterations = 1_000;

        private const double DisabledNsPerCallCeiling = 200.0;
        private const double EnabledNsPerCallCeiling = 50_000.0;   // 50 µs
        private const long RemoveEffectMsCeiling = 500;

        [SetUp]
        public void SetUp() => Diag.ResetAll();

        // ====================================================================
        // 17. Disabled-channel overhead (the IsChannelEnabled short-circuit)
        // ====================================================================

        [Test]
        public void Diag_DisabledChannelOverhead_BoundedPerCall()
        {
            // Channel default off. Warm up to amortize JIT.
            Diag.SetChannel("perf_disabled", false);
            for (int i = 0; i < 1000; i++)
                Diag.Record("perf_disabled", "Warmup", payload: new { x = i });

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < DisabledIterations; i++)
                Diag.Record("perf_disabled", "Iter", payload: new { x = i });
            sw.Stop();

            double nsPerCall = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / DisabledIterations;

            // Sanity: nothing was actually recorded
            Assert.AreEqual(0, Diag.Snapshot(10).Count,
                "Disabled-channel records must not write to the buffer");

            Assert.Less(nsPerCall, DisabledNsPerCallCeiling,
                $"Disabled-channel Diag.Record overhead {nsPerCall:F0} ns/call exceeded ceiling " +
                $"{DisabledNsPerCallCeiling:F0} ns/call. The IsChannelEnabled short-circuit " +
                "may have regressed (extra allocation? lock contention? full-method dispatch?).");
        }

        // ====================================================================
        // 18. Enabled-channel overhead with payload
        // ====================================================================

        [Test]
        public void Diag_EnabledChannelOverhead_BoundedPerCall()
        {
            Diag.SetChannel("perf_enabled", true);
            // Warm up.
            for (int i = 0; i < 1000; i++)
                Diag.Record("perf_enabled", "Warmup", payload: new { idx = i, blob = "tag" });
            Diag.ResetAll();
            Diag.SetChannel("perf_enabled", true);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < EnabledIterations; i++)
                Diag.Record("perf_enabled", "Iter", payload: new { idx = i, blob = "tag" });
            sw.Stop();

            double nsPerCall = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / EnabledIterations;

            // Sanity: ring buffer holds at most BufferCapacity (1024); we
            // wrote EnabledIterations (10k) so dropped count must reflect
            // that everything past capacity overflowed.
            int held = Diag.Snapshot(EnabledIterations * 2).Count;
            Assert.Greater(held, 0, "Some records must have been written");

            Assert.Less(nsPerCall, EnabledNsPerCallCeiling,
                $"Enabled-channel Diag.Record overhead {nsPerCall:F0} ns/call exceeded ceiling " +
                $"{EnabledNsPerCallCeiling:F0} ns/call (50 µs). Likely culprit: payload " +
                "JSON serialization grew expensive. Consider a payload size cap or " +
                "off-default channel for chatty consumers.");
        }

        // ====================================================================
        // 19. RemoveEffect path with the D1.2 hook firing
        // ====================================================================

        [Test]
        public void RemoveEffect_With_Diag_Hook_CompletesUnderBudget()
        {
            Diag.SetChannel("effect", true);

            var entity = MakePerfTestEntity();
            // Warm up — first iteration JITs ApplyEffect / RemoveEffect.
            for (int i = 0; i < 100; i++)
            {
                entity.ApplyEffect(new StunnedEffect(duration: 1));
                entity.RemoveEffect<StunnedEffect>();
            }

            // Reset diag state so the buffer doesn't get pinned at full
            // capacity and start measuring overflow cost instead of
            // remove cost.
            Diag.ResetAll();
            Diag.SetChannel("effect", true);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < RemoveEffectIterations; i++)
            {
                entity.ApplyEffect(new StunnedEffect(duration: 1));
                entity.RemoveEffect<StunnedEffect>();
            }
            sw.Stop();

            // Sanity: every remove fired the hook (cap at buffer size,
            // overflow drops the rest).
            int recorded = Diag.Snapshot(RemoveEffectIterations * 2).Count;
            Assert.Greater(recorded, 0,
                "Hook must have produced at least some diag records (1000 removes ran)");

            Assert.Less(sw.ElapsedMilliseconds, RemoveEffectMsCeiling,
                $"1000 apply+remove cycles with the diag hook on took " +
                $"{sw.ElapsedMilliseconds}ms — exceeded {RemoveEffectMsCeiling}ms ceiling. " +
                "Likely culprits: payload growth, hook contention, ring-buffer lock cost.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakePerfTestEntity()
        {
            var e = new Entity
            {
                BlueprintName = "PerfTestCreature",
                ID = "perf-" + Guid.NewGuid().ToString("N").Substring(0, 6)
            };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 100, Max = 100, Owner = e };
            e.Statistics["DV"] = new Stat
            { Name = "DV", BaseValue = 4, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "perf creature" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }
    }
}
