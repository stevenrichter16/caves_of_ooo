using CavesOfOoo.Core; using NUnit.Framework; namespace CavesOfOoo.Tests { public class AIDebugSavedContractRunnerProbe {
        [Test]
        public void GoFetchGoal_GetDetailsUsesThePublicSavedProgressFields()
        {
            // Goal saves serialize mutable fields and bypass constructors. The
            // old private-only pin would restart an acquired fetch after load.
            var attempts = typeof(GoFetchGoal).GetField("WalkAttempts");
            var phase = typeof(GoFetchGoal).GetField("CurrentPhase");
            Assert.NotNull(attempts); Assert.False(attempts.IsInitOnly);
            Assert.NotNull(phase); Assert.False(phase.IsInitOnly);
            Assert.IsNull(typeof(GoFetchGoal).GetProperty("WalkAttempts"),
                "A display-only accessor would still not enter the saved field stream.");
            var goal = new GoFetchGoal(null) { WalkAttempts = 1, CurrentPhase = GoFetchGoal.Phase.Pickup };
            Assert.AreEqual("phase=Pickup | attempts=1/2 | item=null", goal.GetDetails());
        }
} }
