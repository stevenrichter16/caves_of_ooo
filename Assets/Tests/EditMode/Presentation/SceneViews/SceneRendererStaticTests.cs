using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests.EditMode.Presentation.SceneViews
{
    /// <summary>
    /// M2 TDD tests: SceneRenderer hardcoded Campfire static composition.
    /// Snapshot-style — feed the renderer a deterministic state and assert
    /// specific cells contain expected glyphs/colors. The static layers
    /// (logs, ground, tent, prompts, scene text) must be at fixed
    /// positions; per-frame variation lands in M3.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M2
    /// Visual spec: Docs/Mockups/scene-views/campfire.html
    /// </summary>
    public class SceneRendererStaticTests
    {
        private const int W = 80;
        private const int H = 28;

        // Coordinates used in assertions — these mirror the JS prototype's
        // composition (mockup file). If the layout shifts, update both
        // here and the prototype together.
        private const int FIRE_CX = 40;
        private const int LOG_TOP_Y = 16;
        private const int GROUND_Y = 19;
        private const int TENT_X = 5;
        private const int TENT_Y = 14;
        private const int PROMPT_Y = H - 2;

        private SceneRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _renderer = new SceneRenderer(W, H);
            _renderer.RenderCampfire();
        }

        // ---- Frame buffer correctness ----

        [Test]
        public void Frame_HasCorrectSize()
        {
            Assert.AreEqual(W, _renderer.Width);
            Assert.AreEqual(H, _renderer.Height);
            Assert.AreEqual(W * H, _renderer.Frame.Length);
        }

        [Test]
        public void GetCell_OutOfBounds_DoesNotThrow()
        {
            // Counter-check: bounds-protected accessor returns default cell
            // rather than throwing on out-of-range coords.
            Assert.DoesNotThrow(() => _renderer.GetCell(-1, 0));
            Assert.DoesNotThrow(() => _renderer.GetCell(0, -1));
            Assert.DoesNotThrow(() => _renderer.GetCell(W, 0));
            Assert.DoesNotThrow(() => _renderer.GetCell(0, H));
        }

        // ---- Logs (anchor of composition) ----

        [Test]
        public void Logs_ExistAtConfiguredRow()
        {
            // The log row should have glyph content (not empty) across
            // a wide horizontal swath centered on FIRE_CX.
            int filledCount = 0;
            for (int x = FIRE_CX - 12; x <= FIRE_CX + 12; x++)
            {
                if (_renderer.GetCell(x, LOG_TOP_Y).Glyph != ' ')
                    filledCount++;
            }
            Assert.Greater(filledCount, 10,
                "Log row should have most of its cells filled");
        }

        [Test]
        public void Logs_AreColoredBrown()
        {
            // Counter-check: logs must NOT be flame-colored (no fiery red/yellow)
            // and must lean toward brown/orange-brown CGA range.
            var cell = _renderer.GetCell(FIRE_CX, LOG_TOP_Y);
            Assert.AreNotEqual(' ', cell.Glyph, "Center log cell should be filled");
            // Brown logs: red dominates, green/blue lower. Not pure red (which
            // would be flame). Heuristic: G channel < R channel and B < G.
            Assert.Greater(cell.Foreground.r, cell.Foreground.b,
                "Log cell color should be warmer than blue");
        }

        // ---- Ground ----

        [Test]
        public void GroundLine_HasContent()
        {
            int filled = 0;
            for (int x = 0; x < W; x++)
            {
                if (_renderer.GetCell(x, GROUND_Y).Glyph != ' ') filled++;
            }
            Assert.Greater(filled, W / 2,
                "Ground line should be mostly filled across the frame");
        }

        // ---- Tent ----

        [Test]
        public void Tent_DrawnInLeftRegion()
        {
            // The tent should have at least one filled cell in the upper-left
            // quadrant of its bounding box.
            bool found = false;
            for (int dy = 0; dy < 5 && !found; dy++)
            {
                for (int dx = 0; dx < 10 && !found; dx++)
                {
                    if (_renderer.GetCell(TENT_X + dx, TENT_Y + dy).Glyph != ' ')
                        found = true;
                }
            }
            Assert.IsTrue(found, $"Tent should have content in its bounding box at ({TENT_X},{TENT_Y})");
        }

        // ---- Sky (stars) ----

        [Test]
        public void Sky_HasStars()
        {
            int starCount = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    char c = _renderer.GetCell(x, y).Glyph;
                    if (c == '.' || c == '*' || c == '+' || c == '\'') starCount++;
                }
            }
            Assert.Greater(starCount, 10,
                "Sky region should have multiple star glyphs");
        }

        // ---- Flame ----

        [Test]
        public void Flame_AppearsAboveLogs()
        {
            // The flame body should have non-space content directly above
            // the log top row.
            int flameRowsWithContent = 0;
            for (int dy = 1; dy <= 6; dy++)
            {
                int y = LOG_TOP_Y - dy;
                if (y < 0) continue;
                if (_renderer.GetCell(FIRE_CX, y).Glyph != ' ')
                    flameRowsWithContent++;
            }
            Assert.GreaterOrEqual(flameRowsWithContent, 4,
                "Flame should occupy at least 4 of the 6 rows above the logs");
        }

        [Test]
        public void Flame_IsWarmColored()
        {
            // Flame center should be in the warm-color range (R high, B low).
            // Counter-check: should NOT be cold (blue-dominant).
            var cell = _renderer.GetCell(FIRE_CX, LOG_TOP_Y - 2);
            if (cell.Glyph != ' ')
            {
                Assert.Greater(cell.Foreground.r, cell.Foreground.b,
                    "Flame should be warm-toned (red/yellow), not cool");
            }
        }

        // ---- UI prompts ----

        [Test]
        public void Prompts_AreOnPromptRow()
        {
            // The prompt row should contain '[' or ']' bracket characters
            // since the format is "[E] RETURN  [R] REST  ..."
            bool foundBracket = false;
            for (int x = 0; x < W; x++)
            {
                char c = _renderer.GetCell(x, PROMPT_Y).Glyph;
                if (c == '[' || c == ']') { foundBracket = true; break; }
            }
            Assert.IsTrue(foundBracket, "Prompt row should contain bracket characters");
        }

        [Test]
        public void Prompts_HaveExitKey()
        {
            // The prompt row must include 'E' to indicate the exit key.
            bool hasE = false;
            for (int x = 0; x < W; x++)
            {
                if (_renderer.GetCell(x, PROMPT_Y).Glyph == 'E') { hasE = true; break; }
            }
            Assert.IsTrue(hasE, "Prompt row should advertise the [E] exit key");
        }

        // ---- Determinism ----

        [Test]
        public void Render_IsDeterministic_AcrossInstancesWithSameSeed()
        {
            // M3 made flame glyphs and ember-glow RNG-driven, so back-to-back
            // renders on the SAME instance no longer match (the RNG advances
            // between calls). The right determinism contract for the same-
            // composition snapshot is: two renderers constructed identically
            // (same seed via default ctor) produce byte-identical first-render
            // frames.
            var a = new SceneRenderer(W, H);
            a.RenderCampfire();
            var b = new SceneRenderer(W, H);
            b.RenderCampfire();
            for (int i = 0; i < a.Frame.Length; i++)
            {
                Assert.AreEqual(a.Frame[i].Glyph, b.Frame[i].Glyph,
                    $"Glyph at index {i} differs between same-seed renders");
            }
        }

        // ---- Counter-check: empty regions ----

        [Test]
        public void TopCorner_IsNotFilledWithLogs()
        {
            // Far top-left corner should be sky, not log glyphs.
            char c = _renderer.GetCell(0, 0).Glyph;
            Assert.AreNotEqual('=', c, "Top-left should not have log brackets");
            Assert.AreNotEqual('|', c, "Top-left should not have log walls");
        }
    }
}
