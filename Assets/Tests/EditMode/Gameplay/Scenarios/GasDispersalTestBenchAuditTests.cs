using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// G.12 — deterministic validation of the GasDispersalTestBench's
    /// self-auditing matrix. Per the playbook (Rule 7), a smoke test only
    /// proves Apply() doesn't throw; this test proves the matrix actually
    /// PRODUCES CORRECT DATA. It runs the bench through the scenario
    /// harness (which loads the gas registry via Resources.LoadAll, so the
    /// matrix runs for real — not the phantom-abort path) and asserts the
    /// gasbench diag records. This replaces the live Play-mode audit with
    /// a CI-friendly, focus-independent equivalent.
    /// </summary>
    [TestFixture]
    public class GasDispersalTestBenchAuditTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp] public void OneTimeSetUp() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown] public void OneTimeTearDown() => _harness?.Dispose();

        private static System.Collections.Generic.IReadOnlyList<Diag.Entry> Query(string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter { Category = "gasbench", Kind = kind, Limit = 50 }).Records;

        [Test]
        public void Bench_ProducesValidMatrix_NotPhantomAbort()
        {
            Diag.ResetAll();
            var ctx = _harness.CreateContext(playerBlueprint: "Player");
            new GasDispersalTestBench().Apply(ctx);

            // Rule 4 guard: the matrix must NOT have aborted on a missing
            // registry. If it did, every downstream assertion is moot —
            // surface it clearly.
            var skipped = Query("MatrixAuditSkipped");
            Assert.IsFalse(skipped.Any(r => r.PayloadJson.Contains("registry_unavailable")),
                "bench aborted — GasRegistry not loaded in the harness (Resources.LoadAll failed)");

            Assert.AreEqual(1, Query("MatrixAuditRun").Count, "exactly one run marker");
        }

        [Test]
        public void Bench_ApplyAudit_AllSixDeterministicGasesApply()
        {
            Diag.ResetAll();
            new GasDispersalTestBench().Apply(_harness.CreateContext(playerBlueprint: "Player"));

            var apply = Query("ApplyAudit");
            Assert.AreEqual(7, apply.Count, "one ApplyAudit per behavior gas type");

            // The 6 deterministic behaviors always dose a creature once the
            // filter passes. (FungalSpores is probabilistic — excluded.)
            foreach (var id in new[] { "poison-vapor", "stun-vapor", "confusion-vapor",
                                       "cryo-mist", "sleep-vapor", "plasma-gas" })
            {
                var rec = apply.FirstOrDefault(r => r.PayloadJson.Contains("\"gasId\":\"" + id + "\""));
                Assert.IsNotNull(rec, $"ApplyAudit record for {id}");
                StringAssert.Contains("\"applied\":true", rec.PayloadJson, $"{id} should apply");
            }
        }

        [Test]
        public void Bench_DispersalAudit_DecaysAndSpreads()
        {
            Diag.ResetAll();
            new GasDispersalTestBench().Apply(_harness.CreateContext(playerBlueprint: "Player"));

            var disp = Query("DispersalAudit");
            Assert.AreEqual(1, disp.Count, "one dispersal-lifecycle record");
            StringAssert.Contains("\"decayed\":true", disp[0].PayloadJson, "unstable cloud decays");
            StringAssert.Contains("\"spread\":true", disp[0].PayloadJson, "high-density cloud spreads");
        }

        [Test]
        public void Bench_DefenseAudit_ImmuneVetoed_BareDosed()
        {
            Diag.ResetAll();
            new GasDispersalTestBench().Apply(_harness.CreateContext(playerBlueprint: "Player"));

            var def = Query("DefenseAudit");
            Assert.AreEqual(3, def.Count, "bare / masked / immune");

            var bare = def.FirstOrDefault(r => r.PayloadJson.Contains("\"defense\":\"bare\""));
            var immune = def.FirstOrDefault(r => r.PayloadJson.Contains("\"defense\":\"immune\""));
            Assert.IsNotNull(bare); Assert.IsNotNull(immune);
            StringAssert.Contains("\"applied\":true", bare.PayloadJson, "bare dummy is dosed");
            StringAssert.Contains("\"applied\":false", immune.PayloadJson, "immune dummy is vetoed");
        }
    }
}
