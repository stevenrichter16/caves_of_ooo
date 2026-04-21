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
        // M1 scenarios (Cornered Warden, Ignored Scribe, Sleeping Troll)
        // ======================================================

        [Test] public void CorneredWarden_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new CorneredWarden().Apply(FreshContext()));

        [Test] public void IgnoredScribe_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new IgnoredScribe().Apply(FreshContext()));

        [Test] public void SleepingTroll_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new SleepingTroll().Apply(FreshContext()));

        // ======================================================
        // M2.2 scenarios (Calm-based)
        // ======================================================

        [Test]
        public void PacifiedWarden_Applies_WithoutThrowing()
        {
            // Like CalmTestSetup, references CalmMutation / Calm effect and
            // may log soft-fail warnings during setup; ignore them so we can
            // focus on the "no exception" signal.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() => new PacifiedWarden().Apply(FreshContext()));
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        // ======================================================
        // M2.3 scenarios (Witness pipeline)
        // ======================================================

        [Test] public void ScribeWitnessesSnapjawKill_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new ScribeWitnessesSnapjawKill().Apply(FreshContext()));

        [Test] public void WitnessLineOfSightWall_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new WitnessLineOfSightWall().Apply(FreshContext()));

        [Test] public void WitnessRadiusBoundary_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new WitnessRadiusBoundary().Apply(FreshContext()));

        [Test] public void WitnessStacksOnSecondDeath_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new WitnessStacksOnSecondDeath().Apply(FreshContext()));

        [Test]
        public void CalmThenWitness_Applies_WithoutThrowing()
        {
            // Combines Calm + Witness paths; Calm setup may log soft-fail warnings.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                Assert.DoesNotThrow(() => new CalmThenWitness().Apply(FreshContext()));
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        // ======================================================
        // M3.1 scenario (AIPetter)
        // ======================================================

        [Test] public void VillageChildrenPetting_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new VillageChildrenPetting().Apply(FreshContext()));

        // ======================================================
        // M3.2 scenarios (AIHoarder + AIRetriever)
        // ======================================================

        [Test] public void MagpieFetchesGold_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new MagpieFetchesGold().Apply(FreshContext()));

        [Test] public void PetDogFetchesBone_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new PetDogFetchesBone().Apply(FreshContext()));

        // ======================================================
        // M3.3 scenario (AIFleeToShrine)
        // ======================================================

        [Test] public void WoundedScribeFleesToShrine_Applies_WithoutThrowing() =>
            Assert.DoesNotThrow(() => new WoundedScribeFleesToShrine().Apply(FreshContext()));

        // ======================================================
        // Phase 10 scenario (AI goal-stack inspector)
        // ======================================================

        [Test]
        public void InspectAIGoals_Applies_WithoutThrowing()
        {
            // Scenario flips the global AIDebug.AIInspectorEnabled flag.
            // Reset it in a finally so this smoke test doesn't leak static
            // state into other fixtures.
            try
            {
                Assert.DoesNotThrow(() => new InspectAIGoals().Apply(FreshContext()));
            }
            finally
            {
                CavesOfOoo.Diagnostics.AIDebug.AIInspectorEnabled = false;
            }
        }
    }
}
