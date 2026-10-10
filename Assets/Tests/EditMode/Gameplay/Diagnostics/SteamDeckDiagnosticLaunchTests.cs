using System.Reflection;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckDiagnosticLaunchTests
    {
        [TestCase(false, null, false)][TestCase(false, "", false)][TestCase(false, "0", false)]
        [TestCase(false, "1", true)][TestCase(false, "true", false)][TestCase(false, " 1 ", false)]
        [TestCase(true, null, true)][TestCase(true, "0", true)][TestCase(true, "1", true)]
        public void LaunchCaptureRequiresExplicitRetailOptInAndPreservesDevelopmentDefault(bool development, string option, bool expected)
        {
            var method = typeof(Diag).GetMethod("CaptureEnabledByDefault", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "A testable launch-policy boundary must make COO_DIAGNOSTICS=1 an actual retail opt-in.");
            Assert.AreEqual(expected, method.Invoke(null, new object[] { development, option }));
        }
    }
}
