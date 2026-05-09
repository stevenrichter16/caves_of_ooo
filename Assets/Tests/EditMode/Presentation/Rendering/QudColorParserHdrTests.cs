using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// 3.B.2 — HDR color codes for QudColorParser. See
    /// <c>Docs/GRAPHICS.md</c> for the Pass 3 plan.
    ///
    /// <para>The Pass 1 URP Bloom effect is configured with
    /// <c>threshold=1.05</c> so only HDR-bright pixels (RGB > 1.05)
    /// trigger bloom. The CGA palette in <c>QudColorParser.cs</c> is
    /// fully SDR (max value 1.0). Without HDR variants, no glyph
    /// in the game ever blooms.</para>
    ///
    /// <para>This file pins the contract for HDR codes:
    /// <list type="bullet">
    ///   <item><c>&amp;*R</c> = HDR red (~2.0 in the red channel)</item>
    ///   <item><c>&amp;*Y</c> = HDR yellow</item>
    ///   <item><c>&amp;*C</c> = HDR cyan</item>
    ///   <item><c>&amp;*G</c> = HDR green</item>
    ///   <item><c>&amp;*M</c> = HDR magenta</item>
    /// </list>
    /// Existing <c>&amp;X</c> single-char codes are unchanged.</para>
    /// </summary>
    public class QudColorParserHdrTests
    {
        // ── A. Existing codes still work (regression) ────────────────────

        [Test]
        public void Parse_ExistingCode_BrightRed_Unchanged()
        {
            Color c = QudColorParser.Parse("&R");
            Assert.AreEqual(QudColorParser.BrightRed, c,
                "Existing single-char code &R returns the SDR BrightRed.");
        }

        [Test]
        public void Parse_ExistingCode_DarkGreen_Unchanged()
        {
            Color c = QudColorParser.Parse("&g");
            Assert.AreEqual(QudColorParser.DarkGreen, c);
        }

        // ── B. HDR codes return HDR colors (RGB can exceed 1) ─────────────

        [Test]
        public void Parse_HdrCode_StarR_ReturnsHdrBrightRed()
        {
            Color c = QudColorParser.Parse("&*R");
            // Channel that's "lit" should exceed 1.05 (the bloom threshold).
            // Other channels stay relatively low to keep the hue.
            Assert.Greater(c.r, 1.05f,
                "HDR red exceeds bloom threshold (1.05). Pinned by GRAPHICS.md "
                + "Pass 3 §3.B.2.");
            Assert.Less(c.g, 0.6f, "Green channel stays in low-saturation range.");
            Assert.Less(c.b, 0.6f, "Blue channel stays in low-saturation range.");
        }

        [Test]
        public void Parse_HdrCode_StarY_ReturnsHdrBrightYellow()
        {
            Color c = QudColorParser.Parse("&*Y");
            Assert.Greater(c.r, 1.05f, "Red channel HDR for yellow.");
            Assert.Greater(c.g, 1.05f, "Green channel HDR for yellow.");
            Assert.Less(c.b, 0.6f, "Blue stays low for yellow hue.");
        }

        [Test]
        public void Parse_HdrCode_StarC_ReturnsHdrBrightCyan()
        {
            Color c = QudColorParser.Parse("&*C");
            Assert.Less(c.r, 0.6f, "Red stays low for cyan hue.");
            Assert.Greater(c.g, 1.05f);
            Assert.Greater(c.b, 1.05f);
        }

        [Test]
        public void Parse_HdrCode_StarG_ReturnsHdrBrightGreen()
        {
            Color c = QudColorParser.Parse("&*G");
            Assert.Less(c.r, 0.6f);
            Assert.Greater(c.g, 1.05f);
            Assert.Less(c.b, 0.6f);
        }

        [Test]
        public void Parse_HdrCode_StarM_ReturnsHdrBrightMagenta()
        {
            Color c = QudColorParser.Parse("&*M");
            Assert.Greater(c.r, 1.05f);
            Assert.Less(c.g, 0.6f);
            Assert.Greater(c.b, 1.05f);
        }

        // ── C. Counter-checks: HDR codes are HOTTER than SDR equivalents ──

        [Test]
        public void Parse_HdrCode_StarR_BrighterThan_SdrBrightR()
        {
            // Counter-check: a buggy impl that treated &*R the same as &R
            // would return BrightRed (SDR). Verify the HDR variant is
            // STRICTLY hotter.
            Color sdr = QudColorParser.Parse("&R");
            Color hdr = QudColorParser.Parse("&*R");
            Assert.Greater(hdr.r, sdr.r,
                "HDR red is strictly brighter than SDR bright red.");
        }

        // ── D. Malformed input ────────────────────────────────────────────

        [Test]
        public void Parse_HdrCode_StarUnknown_FallsBackToGray()
        {
            // &* followed by non-recognized char: graceful fallback, not crash.
            Color c = QudColorParser.Parse("&*?");
            Assert.AreEqual(QudColorParser.Gray, c,
                "Unknown HDR code falls back to Gray (same as unknown SDR code).");
        }

        [Test]
        public void Parse_TruncatedHdrPrefix_AmpStarNothing_FallsBack()
        {
            // String "&*" with no following char: no code to read.
            Color c = QudColorParser.Parse("&*");
            // Either Gray (default fallback) or some sentinel — just check
            // it doesn't crash and returns a valid Color.
            Assert.IsTrue(c.a >= 0f && c.a <= 1f);
        }
    }
}
