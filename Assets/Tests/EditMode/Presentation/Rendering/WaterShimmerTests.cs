using NUnit.Framework;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Wx optimization review §2 (Docs/FELLING-WX-OPTIMIZATION-REVIEW.md)
    /// — the stationary water shimmer skip. The RED here was the compile
    /// error (WaterShimmer did not exist); these pins now hold the two
    /// contracts the ZoneRenderer glue leans on: (1) the palette index is
    /// the branch's historical formula unchanged, (2) ClaimPaint returns
    /// true exactly when a repaint is owed — first paint after a cache
    /// reset, a band change, an invalidation, or no cache at all.
    /// </summary>
    public class WaterShimmerTests
    {
        // ════════════════════════════════════════════════════════════
        //   The index formula — unchanged from the shipped branch
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ColorIndex_MatchesTheHistoricalFormula()
        {
            // ((int)(t*2 + x*0.7 + y*1.3)) % len, negative-wrapped.
            Assert.AreEqual(0, WaterShimmer.ColorIndex(0f, 0, 0, 4));
            Assert.AreEqual(((int)(1.7f * 2f + 3 * 0.7f + 7 * 1.3f)) % 4,
                WaterShimmer.ColorIndex(1.7f, 3, 7, 4));
        }

        [Test]
        public void ColorIndex_AdvancesOneBandPerHalfSecond()
        {
            // The whole §2 win rests on this rate: 2 band changes per
            // second per cell means ~97% of 60fps frames repeat the
            // previous index.
            Assert.AreEqual(WaterShimmer.ColorIndex(0.20f, 0, 0, 8),
                            WaterShimmer.ColorIndex(0.45f, 0, 0, 8),
                "same half-second, same band");
            Assert.AreNotEqual(WaterShimmer.ColorIndex(0.45f, 0, 0, 8),
                               WaterShimmer.ColorIndex(0.55f, 0, 0, 8),
                "the band advances at the half-second");
        }

        [Test]
        public void ColorIndex_AlwaysInPaletteRange()
        {
            for (int x = 0; x < 80; x += 7)
                for (int y = 0; y < 25; y += 3)
                {
                    int idx = WaterShimmer.ColorIndex(123.4f, x, y, 4);
                    Assert.That(idx, Is.InRange(0, 3), $"({x},{y})");
                }
        }

        // ════════════════════════════════════════════════════════════
        //   ClaimPaint — paint exactly when owed
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ClaimPaint_FirstFrameAfterReset_Paints_ThenSkips()
        {
            var cache = new byte[3];
            for (int i = 0; i < cache.Length; i++) cache[i] = WaterShimmer.Unpainted;

            Assert.IsTrue(WaterShimmer.ClaimPaint(cache, 1, 2),
                "a reset cell owes one repaint (the full redraw stomped it)");
            Assert.IsFalse(WaterShimmer.ClaimPaint(cache, 1, 2),
                "same band again — this is the ~97% skip");
            Assert.IsFalse(WaterShimmer.ClaimPaint(cache, 1, 2));
        }

        [Test]
        public void ClaimPaint_BandChange_PaintsAgain()
        {
            var cache = new byte[] { WaterShimmer.Unpainted };
            WaterShimmer.ClaimPaint(cache, 0, 2);
            Assert.IsTrue(WaterShimmer.ClaimPaint(cache, 0, 3),
                "the band moved — repaint");
            Assert.IsFalse(WaterShimmer.ClaimPaint(cache, 0, 3),
                "and settles again");
        }

        [Test]
        public void ClaimPaint_Invalidate_ForcesOneRepaint()
        {
            // A creature stepped onto the cell and its glyph took the
            // tile; when it leaves, the shimmer must not skip on the
            // stale match.
            var cache = new byte[] { WaterShimmer.Unpainted };
            WaterShimmer.ClaimPaint(cache, 0, 2);
            WaterShimmer.Invalidate(cache, 0);
            Assert.IsTrue(WaterShimmer.ClaimPaint(cache, 0, 2),
                "invalidated cell repaints even in the same band");
        }

        [Test]
        public void ClaimPaint_NoCacheOrBadIndex_AlwaysPaints()
        {
            // Fail-open: a missing or short cache must never suppress a
            // repaint — the failure mode is wasted native calls, never a
            // stale tile.
            Assert.IsTrue(WaterShimmer.ClaimPaint(null, 0, 1));
            var cache = new byte[2];
            Assert.IsTrue(WaterShimmer.ClaimPaint(cache, 5, 1), "out of range");
            Assert.IsTrue(WaterShimmer.ClaimPaint(cache, -1, 1));
            Assert.DoesNotThrow(() => WaterShimmer.Invalidate(cache, 9));
            Assert.DoesNotThrow(() => WaterShimmer.Invalidate(null, 0));
        }
    }
}
