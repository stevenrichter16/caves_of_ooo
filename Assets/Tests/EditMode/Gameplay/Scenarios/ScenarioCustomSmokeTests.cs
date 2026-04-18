using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// Cheap smoke tests for every scenario in <c>Custom/</c>: build a context,
    /// call <c>Apply(ctx)</c>, assert no exception. Scales with every future
    /// scenario and catches:
    /// - Unknown blueprint references (silent warning in live game, failure here)
    /// - Broken builder chains (compile-time + runtime types)
    /// - Missing parts the scenario relies on (caught at first access)
    ///
    /// These are NOT behavior tests — they don't drive turns or verify end
    /// state. They prove the setup code runs cleanly. Behavior verification
    /// happens when the scenario is launched manually from the menu.
    /// </summary>
    [TestFixture]
    public class ScenarioCustomSmokeTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        private static ScenarioContext FreshContext() =>
            _harness.CreateContext(playerBlueprint: "Player");

        // ======================================================
        // Existing scenarios (baseline — catch regressions)
        // ======================================================

        [Test] public void FiveSnapjawAmbush_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new FiveSnapjawAmbush().Apply(FreshContext()));

        [Test] public void SnapjawRingAmbush_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new SnapjawRingAmbush().Apply(FreshContext()));

        [Test] public void StoutSnapjaw_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new StoutSnapjaw().Apply(FreshContext()));

        [Test] public void MimicSurprise_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new MimicSurprise().Apply(FreshContext()));

        [Test] public void EmptyStartingZone_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new EmptyStartingZone().Apply(FreshContext()));

        [Test]
        public void CalmTestSetup_Applies_WithoutThrowing()
        {
            // CalmTestSetup references CalmMutation which doesn't exist until M2.
            // Expect a warning from PlayerBuilder.AddMutation's fail-soft path.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() => new CalmTestSetup().Apply(FreshContext()));
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        // ======================================================
        // New — M1 scenarios (Cornered Warden, Ignored Scribe, Sleeping Troll)
        // ======================================================

        [Test] public void CorneredWarden_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new CorneredWarden().Apply(FreshContext()));

        [Test] public void IgnoredScribe_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new IgnoredScribe().Apply(FreshContext()));

        [Test] public void SleepingTroll_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new SleepingTroll().Apply(FreshContext()));
    }
}
