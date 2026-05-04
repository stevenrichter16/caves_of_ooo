using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.SceneViews
{
    /// <summary>
    /// M4 TDD tests: SceneRenderer dissolve transition (radial reveal/conceal
    /// from the center). Forward dissolve runs on scene entry — mask transitions
    /// from all-1 (pre-scene) to all-0 (scene visible). Reverse runs on exit.
    ///
    /// Mask values: 0 = scene fully visible at this cell; 1 = scene fully
    /// hidden (overlay cleared so the world tilemap below shows through).
    /// Soft-edge values in between produce the radial fade.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M4
    /// Visual spec: Docs/Mockups/scene-views/campfire.html
    /// </summary>
    public class SceneRendererDissolveTests
    {
        private const int W = 80;
        private const int H = 28;
        private const float EPSILON = 1e-5f;

        // ---- Initial state ----

        [Test]
        public void NewRenderer_IsNotDissolving()
        {
            var r = new SceneRenderer(W, H);
            Assert.IsFalse(r.IsDissolving,
                "Fresh renderer should not be in a dissolve");
        }

        [Test]
        public void StartDissolve_SetsIsDissolving()
        {
            var r = new SceneRenderer(W, H);
            r.StartDissolve();
            Assert.IsTrue(r.IsDissolving);
            Assert.IsFalse(r.DissolveIsReverse,
                "Default StartDissolve() should be a forward dissolve");
        }

        [Test]
        public void StartDissolve_Reverse_SetsReverseFlag()
        {
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: true);
            Assert.IsTrue(r.IsDissolving);
            Assert.IsTrue(r.DissolveIsReverse);
        }

        // ---- Forward dissolve mask shape ----

        [Test]
        public void Forward_Progress0_AllCellsFullyMasked()
        {
            // At progress=0, the iris hasn't opened — every cell shows the
            // pre-scene (mask=1, world below shows through the cleared overlay).
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: false);
            r.UpdateDissolve(0f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Assert.AreEqual(1f, r.GetMask(x, y), EPSILON,
                        $"Forward p=0: cell ({x},{y}) should be mask=1");
        }

        [Test]
        public void Forward_ProgressMid_HasSoftEdgeCells()
        {
            // Mid-dissolve: at least one cell should be in the soft-edge range
            // (0, 1) — that's the radial transition band.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: false);
            r.UpdateDissolve(SceneRenderer.DISSOLVE_DURATION * 0.5f);

            int softEdgeCount = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float m = r.GetMask(x, y);
                    if (m > EPSILON && m < 1f - EPSILON) softEdgeCount++;
                }
            Assert.Greater(softEdgeCount, 0,
                "Mid-dissolve should produce at least one soft-edge cell at the iris boundary");
        }

        [Test]
        public void Forward_ProgressEnd_AllCellsFullyRevealed()
        {
            // At progress=1, the iris is fully open — every cell is scene
            // (mask=0). Reveal radius is maxR * 1.2 so the corner cells are
            // still safely inside the revealed area.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: false);
            r.UpdateDissolve(SceneRenderer.DISSOLVE_DURATION);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Assert.AreEqual(0f, r.GetMask(x, y), EPSILON,
                        $"Forward p=1: cell ({x},{y}) should be mask=0");
        }

        [Test]
        public void Forward_PastDuration_StaysComplete_AndNotDissolving()
        {
            // Counter-check: after the dissolve completes, IsDissolving must
            // clear so SceneViewUI knows the transition is done.
            var r = new SceneRenderer(W, H);
            r.StartDissolve();
            r.UpdateDissolve(SceneRenderer.DISSOLVE_DURATION);
            Assert.IsFalse(r.IsDissolving,
                "After progress=1, IsDissolving should clear");

            // Subsequent UpdateDissolve calls should be no-ops (mask stays 0)
            r.UpdateDissolve(99f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Assert.AreEqual(0f, r.GetMask(x, y), EPSILON,
                        $"Post-dissolve UpdateDissolve must not re-mask cell ({x},{y})");
        }

        // ---- Reverse dissolve mask shape (mirror of forward) ----

        [Test]
        public void Reverse_Progress0_AllCellsFullyRevealed()
        {
            // At progress=0 of reverse, the iris is still fully open from
            // the prior forward dissolve — every cell shows the scene (mask=0).
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: true);
            r.UpdateDissolve(0f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Assert.AreEqual(0f, r.GetMask(x, y), EPSILON,
                        $"Reverse p=0: cell ({x},{y}) should be mask=0 (scene visible)");
        }

        [Test]
        public void Reverse_ProgressEnd_AllCellsFullyMasked()
        {
            // At progress=1 of reverse, the iris is closed — scene is hidden,
            // overlay cells cleared, world below shows through.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: true);
            r.UpdateDissolve(SceneRenderer.DISSOLVE_DURATION);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    Assert.AreEqual(1f, r.GetMask(x, y), EPSILON,
                        $"Reverse p=1: cell ({x},{y}) should be mask=1");
        }

        [Test]
        public void Reverse_PastDuration_IsNotDissolving()
        {
            // Counter-check symmetric to Forward_PastDuration.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: true);
            r.UpdateDissolve(SceneRenderer.DISSOLVE_DURATION);
            Assert.IsFalse(r.IsDissolving,
                "After progress=1, reverse dissolve must clear IsDissolving too");
        }

        // ---- Temporal monotonicity (per-cell mask values march in one direction) ----

        [Test]
        public void Forward_PerCellMask_DecreasesMonotonically()
        {
            // Track a known-non-center cell across many sub-ticks. Forward
            // dissolve should never RAISE a cell's mask — once a cell starts
            // revealing, it stays revealed.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: false);

            int testX = W / 2 + 5;
            int testY = H / 2;
            float prev = float.PositiveInfinity;
            for (int step = 0; step <= 16; step++)
            {
                float deltaT = SceneRenderer.DISSOLVE_DURATION / 16f;
                r.UpdateDissolve(deltaT);
                float m = r.GetMask(testX, testY);
                Assert.LessOrEqual(m, prev + EPSILON,
                    $"Forward dissolve mask should be monotonically non-increasing; step {step}: {m} > prev {prev}");
                prev = m;
            }
        }

        [Test]
        public void Reverse_PerCellMask_IncreasesMonotonically()
        {
            // Symmetric counter-check.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: true);

            int testX = W / 2 + 5;
            int testY = H / 2;
            float prev = float.NegativeInfinity;
            for (int step = 0; step <= 16; step++)
            {
                float deltaT = SceneRenderer.DISSOLVE_DURATION / 16f;
                r.UpdateDissolve(deltaT);
                float m = r.GetMask(testX, testY);
                Assert.GreaterOrEqual(m, prev - EPSILON,
                    $"Reverse dissolve mask should be monotonically non-decreasing; step {step}: {m} < prev {prev}");
                prev = m;
            }
        }

        // ---- Render integration ----

        [Test]
        public void NoActiveDissolve_RenderProducesSameSceneAsBaseline()
        {
            // Counter-check: when IsDissolving is false, RenderCampfire must
            // not apply any dissolve overlay. Two identical (no-dissolve)
            // renders share the same RNG seed and therefore identical Frames.
            var baseline = new SceneRenderer(W, H);
            baseline.RenderCampfire();

            var r = new SceneRenderer(W, H);
            r.RenderCampfire();

            for (int i = 0; i < baseline.Frame.Length; i++)
                Assert.AreEqual(baseline.Frame[i].Glyph, r.Frame[i].Glyph,
                    $"Without dissolve, glyph at index {i} should match baseline");
        }

        [Test]
        public void DissolveAtFullMask_RenderClearsCells()
        {
            // At forward p=0 (or reverse p=1), every cell is fully masked,
            // so the rendered glyph at every cell should be ' ' (cleared) —
            // exposing the world tilemap below the overlay.
            var r = new SceneRenderer(W, H);
            r.StartDissolve(reverse: false);
            r.UpdateDissolve(0f);
            r.RenderCampfire();
            int nonSpaceCount = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (r.GetCell(x, y).Glyph != ' ' && r.GetCell(x, y).Glyph != '\0')
                        nonSpaceCount++;
            Assert.AreEqual(0, nonSpaceCount,
                "Fully-masked dissolve frame should render every cell as ' ' (cleared)");
        }

        // ---- Boundary protection ----

        [Test]
        public void GetMask_OutOfBounds_DoesNotThrow()
        {
            var r = new SceneRenderer(W, H);
            r.StartDissolve();
            r.UpdateDissolve(0f);
            Assert.DoesNotThrow(() => r.GetMask(-1, 0));
            Assert.DoesNotThrow(() => r.GetMask(0, -1));
            Assert.DoesNotThrow(() => r.GetMask(W, 0));
            Assert.DoesNotThrow(() => r.GetMask(0, H));
        }
    }
}
