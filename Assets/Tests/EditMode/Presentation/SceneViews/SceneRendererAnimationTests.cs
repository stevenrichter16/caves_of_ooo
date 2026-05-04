using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.SceneViews
{
    /// <summary>
    /// M3 TDD tests: SceneRenderer animation port (probabilistic flame
    /// glyphs, spark particles, star twinkle, crackles, wind gusts).
    /// All tests use seeded RNG so they're deterministic.
    ///
    /// Static composition (logs/ground/tent/prompts/text) is covered by
    /// SceneRendererStaticTests; this file targets the per-frame
    /// animation behavior introduced in M3.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M3
    /// Visual spec: Docs/Mockups/scene-views/campfire.html
    /// </summary>
    public class SceneRendererAnimationTests
    {
        private const int W = 80;
        private const int H = 28;
        private const int SEED = 1234;
        private const int FIRE_CX = 40;
        private const int LOG_TOP_Y = 16;
        private const int SKY_BOTTOM = 8;
        private const float DT = 0.04f;

        // ---- Tick semantics ----

        [Test]
        public void Tick_AdvancesTime()
        {
            var r = new SceneRenderer(W, H, SEED);
            float t0 = r.Time;
            r.Tick(DT);
            Assert.Greater(r.Time, t0,
                "Tick should advance the renderer's internal time");
        }

        [Test]
        public void Tick_AloneDoesNotMutateFrame()
        {
            // Counter-check: Tick is state-only — it must not touch the
            // frame buffer until RenderCampfire is called explicitly.
            var r = new SceneRenderer(W, H, SEED);
            r.RenderCampfire();
            var snapshot = (SceneCell[])r.Frame.Clone();
            r.Tick(DT);
            for (int i = 0; i < snapshot.Length; i++)
                Assert.AreEqual(snapshot[i].Glyph, r.Frame[i].Glyph,
                    $"Tick alone should not modify Frame[{i}]");
        }

        // ---- Determinism ----

        [Test]
        public void RenderWithSameSeed_ProducesIdenticalFrame()
        {
            var a = new SceneRenderer(W, H, SEED);
            a.RenderCampfire();
            var b = new SceneRenderer(W, H, SEED);
            b.RenderCampfire();
            for (int i = 0; i < a.Frame.Length; i++)
            {
                Assert.AreEqual(a.Frame[i].Glyph, b.Frame[i].Glyph,
                    $"Glyph at frame index {i} differs between same-seed renders");
            }
        }

        [Test]
        public void RenderWithDifferentSeed_ProducesDifferentFlame()
        {
            // Counter-check: prove the seed actually drives the RNG —
            // otherwise determinism is vacuous (everything's static).
            var a = new SceneRenderer(W, H, 100);
            a.RenderCampfire();
            var b = new SceneRenderer(W, H, 200);
            b.RenderCampfire();
            int diffs = 0;
            for (int dy = 1; dy <= 6; dy++)
            {
                for (int dx = -5; dx <= 5; dx++)
                {
                    var ag = a.GetCell(FIRE_CX + dx, LOG_TOP_Y - dy).Glyph;
                    var bg = b.GetCell(FIRE_CX + dx, LOG_TOP_Y - dy).Glyph;
                    if (ag != bg) diffs++;
                }
            }
            Assert.Greater(diffs, 0,
                "Different seeds should yield at least one different flame glyph");
        }

        // ---- Sparks ----

        [Test]
        public void NoTicks_NoSparks()
        {
            var r = new SceneRenderer(W, H, SEED);
            Assert.AreEqual(0, r.SparkCount,
                "Fresh renderer (no Tick) should have zero sparks");
        }

        [Test]
        public void Tick_SpawnsSparks_OverTime()
        {
            // JS spawn rate: ~40% per tick → after 50 ticks, sparks must exist.
            var r = new SceneRenderer(W, H, SEED);
            for (int i = 0; i < 50; i++) r.Tick(DT);
            Assert.Greater(r.SparkCount, 0,
                "After 50 ticks, sparks should have spawned per JS spawn rate");
        }

        [Test]
        public void Sparks_DoNotAccumulateUnboundedly()
        {
            // Counter-check: max age cleanup actually runs.
            // Sparks live ≤ ~24 frames per JS (max = 14 + rand*10*intensity).
            // After warmup we should reach a steady-state count, not grow
            // by anywhere near the 200 ticks we add.
            var r = new SceneRenderer(W, H, SEED);
            for (int i = 0; i < 100; i++) r.Tick(DT);
            int afterWarmup = r.SparkCount;
            for (int i = 0; i < 200; i++) r.Tick(DT);
            int afterMore = r.SparkCount;
            Assert.Less(afterMore, afterWarmup + 100,
                $"Sparks should reach equilibrium, not grow forever (warmup {afterWarmup} → after {afterMore})");
        }

        // ---- Crackle ----

        [Test]
        public void Crackle_FiresWithinFirstSeconds()
        {
            // JS: nextCrackle = t + 3 + rand*4 → first fires within 7s.
            // 250 ticks * 0.04s = 10s, plenty of headroom.
            var r = new SceneRenderer(W, H, SEED);
            bool sawCrackle = false;
            for (int i = 0; i < 250; i++)
            {
                r.Tick(DT);
                if (r.CrackleLevel > 0.6f) { sawCrackle = true; break; }
            }
            Assert.IsTrue(sawCrackle,
                "Crackle should trigger within first 10s of ticking (per JS schedule)");
        }

        [Test]
        public void Crackle_DecaysOverTicks()
        {
            // Counter-check: post-fire, crackle level decays (JS: *=0.92/tick).
            var r = new SceneRenderer(W, H, SEED);
            for (int i = 0; i < 250 && r.CrackleLevel < 0.6f; i++) r.Tick(DT);
            Assert.Greater(r.CrackleLevel, 0.6f,
                "Test precondition: crackle must have fired within window");
            float peak = r.CrackleLevel;
            for (int i = 0; i < 50; i++) r.Tick(DT);
            Assert.Less(r.CrackleLevel, peak,
                "Crackle level should decay below the peak after ticks elapse");
        }

        // ---- Wind ----

        [Test]
        public void WindGust_FiresWithinFirstSeconds()
        {
            // JS: nextGust = t + 4 + rand*5 → first fires within 9s.
            var r = new SceneRenderer(W, H, SEED);
            bool saw = false;
            for (int i = 0; i < 250; i++)
            {
                r.Tick(DT);
                if (Mathf.Abs(r.WindGust) > 0.3f) { saw = true; break; }
            }
            Assert.IsTrue(saw,
                "Wind gust should fire within first 10s of ticking");
        }

        [Test]
        public void WindGust_DecaysOverTicks()
        {
            // Counter-check: post-fire, wind gust decays (JS: *=0.96/tick).
            var r = new SceneRenderer(W, H, SEED);
            for (int i = 0; i < 300 && Mathf.Abs(r.WindGust) < 0.3f; i++) r.Tick(DT);
            Assert.Greater(Mathf.Abs(r.WindGust), 0.3f,
                "Test precondition: wind gust must have fired within window");
            float peakAbs = Mathf.Abs(r.WindGust);
            for (int i = 0; i < 60; i++) r.Tick(DT);
            Assert.Less(Mathf.Abs(r.WindGust), peakAbs,
                "Wind gust magnitude should decay below the peak after ticks elapse");
        }

        // ---- Stars ----

        [Test]
        public void Stars_PopulateSkyRegion()
        {
            // Per JS: 60 stars seeded into y < 8.
            var r = new SceneRenderer(W, H, SEED);
            r.RenderCampfire();
            int starCount = 0;
            for (int y = 0; y < SKY_BOTTOM; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    char c = r.GetCell(x, y).Glyph;
                    if (c == '.' || c == '*' || c == '+' || c == '\'') starCount++;
                }
            }
            Assert.GreaterOrEqual(starCount, 30,
                "Per JS prototype, ~60 stars seeded; allow overlap, expect ≥30");
        }

        [Test]
        public void Stars_TwinkleAcrossTicks()
        {
            // Star glyph should change for at least some stars as their
            // sine phase advances (JS: ch picked from . ' + * by phase tier).
            var r = new SceneRenderer(W, H, SEED);
            r.RenderCampfire();
            char[] firstSky = new char[W * SKY_BOTTOM];
            for (int y = 0; y < SKY_BOTTOM; y++)
                for (int x = 0; x < W; x++)
                    firstSky[y * W + x] = r.GetCell(x, y).Glyph;

            for (int i = 0; i < 200; i++) r.Tick(DT);
            r.RenderCampfire();

            int diffs = 0;
            for (int y = 0; y < SKY_BOTTOM; y++)
                for (int x = 0; x < W; x++)
                    if (r.GetCell(x, y).Glyph != firstSky[y * W + x]) diffs++;
            Assert.Greater(diffs, 0,
                "Star twinkle should produce at least some glyph changes over 200 ticks");
        }

        // ---- Static layer invariance under animation ----

        [Test]
        public void StaticLayers_PersistAcrossTicks()
        {
            // Counter-check that animation doesn't trample the static
            // anchor (logs row stays drawn even after long ticking).
            var r = new SceneRenderer(W, H, SEED);
            for (int i = 0; i < 500; i++) r.Tick(DT);
            r.RenderCampfire();
            int filled = 0;
            for (int x = FIRE_CX - 12; x <= FIRE_CX + 12; x++)
                if (r.GetCell(x, LOG_TOP_Y).Glyph != ' ') filled++;
            Assert.Greater(filled, 10,
                "Logs row should remain populated after 500 ticks of animation");
        }
    }
}
