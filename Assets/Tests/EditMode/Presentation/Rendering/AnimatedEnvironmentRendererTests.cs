using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 5 §5A.3 — tests for the glyph-classification logic in
    /// <see cref="AnimatedEnvironmentRenderer"/>. The full Init +
    /// PostRender flow requires a Unity scene with a Tilemap and
    /// is exercised by the showcase scenario's smoke test;
    /// EditMode tests here pin the pure-data contracts.
    /// </summary>
    public class AnimatedEnvironmentRendererTests
    {
        private GameObject _go;
        private AnimatedEnvironmentRenderer _renderer;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("RendererTest");
            _renderer = _go.AddComponent<AnimatedEnvironmentRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ── Water glyph classification ───────────────────────────────────

        [Test]
        public void WaterGlyphs_TildeEqualsDash_AreClassified()
        {
            Assert.IsTrue(_renderer.TestOnly_IsWaterGlyph('~'));
            Assert.IsTrue(_renderer.TestOnly_IsWaterGlyph('='));
            Assert.IsTrue(_renderer.TestOnly_IsWaterGlyph('-'));
        }

        [Test]
        public void NonWaterGlyphs_AreNotClassified()
        {
            // Counter-check: walls + floors + creature glyphs must NOT
            // get re-routed to the water overlay. Otherwise dungeon
            // walls would scroll horizontally — clearly wrong.
            Assert.IsFalse(_renderer.TestOnly_IsWaterGlyph('#'));
            Assert.IsFalse(_renderer.TestOnly_IsWaterGlyph('.'));
            Assert.IsFalse(_renderer.TestOnly_IsWaterGlyph('@'));
            Assert.IsFalse(_renderer.TestOnly_IsWaterGlyph('s'));
            Assert.IsFalse(_renderer.TestOnly_IsWaterGlyph(' '));
            Assert.IsFalse(_renderer.TestOnly_IsWaterGlyph('\0'));
        }

        // ── Grass glyph classification ───────────────────────────────────

        [Test]
        public void GrassGlyphs_CommaSemicolon_AreClassified()
        {
            Assert.IsTrue(_renderer.TestOnly_IsGrassGlyph(','));
            Assert.IsTrue(_renderer.TestOnly_IsGrassGlyph(';'));
        }

        [Test]
        public void NonGrassGlyphs_AreNotClassified()
        {
            Assert.IsFalse(_renderer.TestOnly_IsGrassGlyph('.'));  // floor
            Assert.IsFalse(_renderer.TestOnly_IsGrassGlyph('\''));  // small bush?
            Assert.IsFalse(_renderer.TestOnly_IsGrassGlyph(':'));
        }

        // ── Fire glyph classification ────────────────────────────────────

        [Test]
        public void FireGlyphs_Asterisk_IsClassified()
        {
            Assert.IsTrue(_renderer.TestOnly_IsFireGlyph('*'));
        }

        [Test]
        public void NonFireGlyphs_AreNotClassified()
        {
            // Counter: many CP437 glyphs include other "shiny" chars
            // (♦, ♥, etc.) that should NOT animate as fire. Asterisk is
            // the unambiguous fire glyph in this project.
            Assert.IsFalse(_renderer.TestOnly_IsFireGlyph('+'));
            Assert.IsFalse(_renderer.TestOnly_IsFireGlyph('o'));
            Assert.IsFalse(_renderer.TestOnly_IsFireGlyph('@'));
        }

        // ── Mutual exclusion (a glyph belongs to AT MOST one category) ──

        [Test]
        public void NoGlyphIsClassifiedAsMultipleTypes()
        {
            // Adversarial: if the same glyph were claimed by water AND
            // grass, the post-render scan would paint it onto BOTH
            // overlays — visible as double-rendering / shimmering. Pin
            // the disjointness contract.
            for (int c = 32; c < 127; c++)
            {
                char glyph = (char)c;
                int matches = 0;
                if (_renderer.TestOnly_IsWaterGlyph(glyph)) matches++;
                if (_renderer.TestOnly_IsGrassGlyph(glyph)) matches++;
                if (_renderer.TestOnly_IsFireGlyph(glyph)) matches++;
                Assert.LessOrEqual(matches, 1,
                    $"Glyph '{glyph}' (0x{c:X2}) is claimed by "
                    + $"{matches} animated overlays — must be at most 1.");
            }
        }

        // ── Init pre-condition ───────────────────────────────────────────

        [Test]
        public void IsInitialized_FalseBeforeInit()
        {
            Assert.IsFalse(_renderer.IsInitialized,
                "Component starts uninitialized. Init must be called "
                + "explicitly by ZoneRenderer.");
        }
    }
}
