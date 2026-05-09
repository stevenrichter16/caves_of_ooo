using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Presentation.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// 3.C — BiomePalette tests. Pins the per-biome aesthetic
    /// constants from <c>Docs/GRAPHICS.md</c> §3.C so a future tuning
    /// pass can adjust values intentionally (the test breaks visibly,
    /// the new value is updated in both the constant + test together).
    /// </summary>
    public class BiomePaletteTests
    {
        // ── A. GetForBiome dispatches correctly ─────────────────────────

        [Test]
        public void GetForBiome_Cave_ReturnsCavePalette()
        {
            var p = BiomePalette.GetForBiome(BiomeType.Cave);
            Assert.AreEqual(BiomeType.Cave, p.Biome);
        }

        [Test]
        public void GetForBiome_Desert_ReturnsDesertPalette()
        {
            var p = BiomePalette.GetForBiome(BiomeType.Desert);
            Assert.AreEqual(BiomeType.Desert, p.Biome);
        }

        [Test]
        public void GetForBiome_Jungle_ReturnsJunglePalette()
        {
            var p = BiomePalette.GetForBiome(BiomeType.Jungle);
            Assert.AreEqual(BiomeType.Jungle, p.Biome);
        }

        [Test]
        public void GetForBiome_Ruins_ReturnsRuinsPalette()
        {
            var p = BiomePalette.GetForBiome(BiomeType.Ruins);
            Assert.AreEqual(BiomeType.Ruins, p.Biome);
        }

        // ── B. Aesthetic-target invariants (per GRAPHICS.md §3.C) ───────

        [Test]
        public void Cave_IsWarmTinted()
        {
            // Plan target: warm amber. Verify red-channel is the
            // strongest, blue-channel is the weakest.
            var p = BiomePalette.Cave;
            Assert.Greater(p.ColorFilter.r, p.ColorFilter.b,
                "Cave color filter is warm: red > blue.");
            Assert.Greater(p.ColorFilter.r, p.ColorFilter.g,
                "Cave color filter is warm: red > green.");
        }

        [Test]
        public void Cave_HasReducedContrastAndSaturation()
        {
            var p = BiomePalette.Cave;
            Assert.Less(p.Contrast, 0f, "Cave has reduced contrast.");
            Assert.Less(p.Saturation, 0f, "Cave has reduced saturation.");
        }

        [Test]
        public void Cave_HasStrongerVignetteThanGlobalDefault()
        {
            // Pass 1 set global vignette to 0.32. Cave should boost
            // beyond that to pull eye toward player.
            var p = BiomePalette.Cave;
            Assert.Greater(p.VignetteIntensity, 0.32f,
                "Cave vignette is stronger than the Pass 1 global "
                + "baseline (0.32).");
        }

        [Test]
        public void Desert_IsHighContrastHighSaturation()
        {
            var p = BiomePalette.Desert;
            Assert.Greater(p.Contrast, 0f,
                "Desert pumps contrast for sand-glare feel.");
            Assert.Greater(p.Saturation, 0f,
                "Desert pumps saturation.");
        }

        [Test]
        public void Desert_DoesNotBoostVignette()
        {
            // Counter to Cave/Ruins: deserts should feel open. No
            // vignette boost beyond global baseline.
            var p = BiomePalette.Desert;
            Assert.LessOrEqual(p.VignetteIntensity, 0.33f,
                "Desert vignette stays at or below the 0.32 baseline "
                + "(plus a small float-comparison epsilon).");
        }

        [Test]
        public void Jungle_IsGreenTinted()
        {
            // Green-channel is the strongest in the color filter.
            var p = BiomePalette.Jungle;
            Assert.Greater(p.ColorFilter.g, p.ColorFilter.r,
                "Jungle color filter is green: green > red.");
            Assert.Greater(p.ColorFilter.g, p.ColorFilter.b,
                "Jungle color filter is green: green > blue.");
        }

        [Test]
        public void Ruins_IsDesaturated()
        {
            var p = BiomePalette.Ruins;
            Assert.Less(p.Saturation, -20f,
                "Ruins is heavily desaturated (saturation < -20).");
        }

        [Test]
        public void Ruins_IsCoolTinted()
        {
            // Counter to Cave: blue-channel is the strongest (cool).
            var p = BiomePalette.Ruins;
            Assert.GreaterOrEqual(p.ColorFilter.b, p.ColorFilter.r,
                "Ruins color filter is cool: blue >= red.");
            Assert.GreaterOrEqual(p.ColorFilter.b, p.ColorFilter.g,
                "Ruins color filter is cool: blue >= green.");
        }

        // ── C. Determinism: the static lookup is stable ─────────────────

        [Test]
        public void GetForBiome_Cave_TwoCallsReturnEqualPalettes()
        {
            // Counter-check: GetForBiome is a pure function. Two
            // calls return equal data.
            var a = BiomePalette.GetForBiome(BiomeType.Cave);
            var b = BiomePalette.GetForBiome(BiomeType.Cave);
            Assert.AreEqual(a.ColorFilter, b.ColorFilter);
            Assert.AreEqual(a.Contrast, b.Contrast);
            Assert.AreEqual(a.Saturation, b.Saturation);
            Assert.AreEqual(a.VignetteIntensity, b.VignetteIntensity);
        }

        // ── D. Distinctness: no two biomes share identical params ───────

        [Test]
        public void Adversarial_AllFourBiomes_HaveDistinctColorFilters()
        {
            // If two biomes accidentally got the same color filter,
            // a player wouldn't be able to distinguish them. Pin
            // color filters as distinct.
            var c = BiomePalette.Cave.ColorFilter;
            var d = BiomePalette.Desert.ColorFilter;
            var j = BiomePalette.Jungle.ColorFilter;
            var r = BiomePalette.Ruins.ColorFilter;

            Assert.AreNotEqual(c, d);
            Assert.AreNotEqual(c, j);
            Assert.AreNotEqual(c, r);
            Assert.AreNotEqual(d, j);
            Assert.AreNotEqual(d, r);
            Assert.AreNotEqual(j, r);
        }
    }
}
