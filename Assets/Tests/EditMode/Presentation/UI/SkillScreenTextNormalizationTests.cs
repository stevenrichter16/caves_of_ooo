using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual grimoire prose must use supported separators before the
    /// retained CP437 label wraps it. This does not verify rendered pixels.</summary>
    public sealed class SkillScreenTextNormalizationTests
    {
        [Test]
        public void ConjureRainSeparatorsRemainReadableWithoutChangingAsciiOrWrapping()
        {
            Assert.IsTrue(GrimoireTooltipData.TryGet("Hydromancy_ConjureRain", out var tooltip));
            string prose = tooltip.Mechanics + "\n" + tooltip.Signature;
            StringAssert.Contains("\u2022", prose, "Exercise the authored unsupported separator, not a synthetic ASCII-only input.");

            const string ascii = "Waters crops - Radius 3 - CD 5\nSoaks soil for 40 ticks - darkens wet earth";
            const string wrapped = "Waters crops - Radius 3\n- CD 5\nSoaks soil for 40 ticks\n- darkens wet earth";
            Assert.AreEqual(ascii, SkillScreenText.Fit(ascii, 48, 2),
                "Counter: already supported prose must remain byte-for-byte unchanged.");
            Assert.AreEqual(wrapped, SkillScreenText.Fit(ascii, 24, 4),
                "Counter: the existing word-wrap contract must remain unchanged.");

            Assert.AreEqual(ascii, SkillScreenText.Fit(prose, 48, 2),
                "Rain's mechanics and signature need plain separators instead of CP437 fallback question marks.");
            string fitted = SkillScreenText.Fit(prose, 24, 4);
            Assert.AreEqual(wrapped, fitted, "Normalization must retain every fact at the same narrow wrap boundaries.");
            foreach (string line in fitted.Split('\n'))
            {
                Assert.LessOrEqual(line.Length, 24);
                foreach (char c in line)
                    Assert.That((int)c, Is.InRange(32, 126), "This tooltip must contain only supported printable ASCII.");
            }
        }
    }
}
