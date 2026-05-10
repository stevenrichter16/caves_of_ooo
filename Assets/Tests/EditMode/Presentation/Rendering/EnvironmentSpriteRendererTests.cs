using NUnit.Framework;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 7 §7C.1 — tests for EnvironmentSpriteRenderer's pure
    /// classification + variant-index logic. Full Init + PostRender
    /// requires Sprites + Tilemaps + Zone, exercised by the showcase.
    /// </summary>
    public class EnvironmentSpriteRendererTests
    {
        // ── FloorVariantIndex: deterministic + bounded ───────────────────

        [Test]
        public void FloorVariantIndex_SameInputs_SameOutput()
        {
            int a = EnvironmentSpriteRenderer.FloorVariantIndex(5, 7);
            int b = EnvironmentSpriteRenderer.FloorVariantIndex(5, 7);
            Assert.AreEqual(a, b,
                "FloorVariantIndex must be pure / deterministic so the "
                + "same cell shows the same floor variant across reloads.");
        }

        [Test]
        public void FloorVariantIndex_AllResultsInRange0to3()
        {
            for (int x = -50; x < 50; x++)
            {
                for (int y = -50; y < 50; y++)
                {
                    int v = EnvironmentSpriteRenderer.FloorVariantIndex(x, y);
                    Assert.GreaterOrEqual(v, 0,
                        $"FloorVariantIndex({x},{y}) returned {v} — must be ≥ 0 to index a 4-element array.");
                    Assert.Less(v, 4,
                        $"FloorVariantIndex({x},{y}) returned {v} — must be < 4 (only 4 variants in atlas).");
                }
            }
        }

        [Test]
        public void FloorVariantIndex_DifferentCells_OftenDifferent()
        {
            // Adversarial: a buggy hash that always returned 0 would
            // pass the determinism test but make every floor look
            // identical. Sample many cells and verify variety.
            var distinct = new System.Collections.Generic.HashSet<int>();
            for (int x = 0; x < 20; x++)
                for (int y = 0; y < 20; y++)
                    distinct.Add(EnvironmentSpriteRenderer.FloorVariantIndex(x, y));
            Assert.GreaterOrEqual(distinct.Count, 3,
                "Across 400 cells, the hash should distribute across at "
                + "least 3 of the 4 variants. Bug catch: a constant-return "
                + "hash would only produce 1.");
        }

        // ── WallVariantIndex: nesting test (requires Zone) ───────────────
        // The pure-static WallVariantIndex(zone, x, y) needs a Zone object.
        // Constructing a test-friendly Zone is heavy; the wall-variant
        // logic is exercised end-to-end by the showcase scenario.
        // We test the pure helper FloorVariantIndex here.
    }
}
