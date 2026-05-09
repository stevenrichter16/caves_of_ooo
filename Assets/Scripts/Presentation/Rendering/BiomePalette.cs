using UnityEngine;
using CavesOfOoo.Core;

namespace CavesOfOoo.Presentation.Rendering
{
    /// <summary>
    /// Per-biome post-processing parameters. Pure data + static
    /// lookup table. Used by a future runtime patcher
    /// (`BiomeColorPatcher`, deferred to Pass 4) to apply biome-
    /// specific color grading and vignette intensity to the active
    /// URP Volume profile when the player's zone changes.
    ///
    /// <para><b>Why a struct rather than 4 separate Volume Profile
    /// assets:</b> the MCP <c>volume_add_effect</c> tool was
    /// observed to clobber existing effects on a profile when
    /// targeting via <c>profile_path</c>. Working around that gap
    /// is painful — the cleaner shape is to keep ONE Volume +
    /// ONE Profile in the scene, and patch its parameter overrides
    /// at runtime from this data table. Easier to tune + version-
    /// control + test.</para>
    ///
    /// <para>See <c>Docs/GRAPHICS.md</c> §3.C for the per-biome
    /// aesthetic targets table that drives the constants below.</para>
    /// </summary>
    public readonly struct BiomePalette
    {
        public readonly BiomeType Biome;
        public readonly Color ColorFilter;     // multiplicative tint, RGB ≥ 0
        public readonly float Contrast;        // [-100, 100] — URP ColorAdjustments.contrast
        public readonly float Saturation;      // [-100, 100] — URP ColorAdjustments.saturation
        public readonly float VignetteIntensity; // [0, 1] — URP Vignette.intensity

        public BiomePalette(BiomeType biome, Color colorFilter,
            float contrast, float saturation, float vignetteIntensity)
        {
            Biome = biome;
            ColorFilter = colorFilter;
            Contrast = contrast;
            Saturation = saturation;
            VignetteIntensity = vignetteIntensity;
        }

        /// <summary>
        /// Look up the palette for a given biome. Returns the
        /// `Cave` palette as a fallback for unknown biomes (rather
        /// than throwing) so a future biome enum addition doesn't
        /// crash the game before its palette is authored.
        /// </summary>
        public static BiomePalette GetForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Cave:   return Cave;
                case BiomeType.Desert: return Desert;
                case BiomeType.Jungle: return Jungle;
                case BiomeType.Ruins:  return Ruins;
                default:               return Cave;
            }
        }

        // ── Biome palette constants ──────────────────────────────────────
        //
        // Tuning targets per Docs/GRAPHICS.md §3.C aesthetic table:
        //
        // Cave   — warm amber, low contrast, low saturation, strong vignette
        //          (intimate underground feel — torches push warmth, dark
        //          corners pull eye to player)
        // Desert — washed-out, high contrast, high saturation, default vignette
        //          (bright bleaching daylight, sand glare, no need for
        //          vignette since the open world IS the focus)
        // Jungle — green tint, medium contrast, high saturation, medium vignette
        //          (dappled canopy, lush)
        // Ruins  — desaturated cool, low contrast, very low saturation,
        //          strong vignette (melancholic, stripped of color)

        public static readonly BiomePalette Cave = new BiomePalette(
            BiomeType.Cave,
            colorFilter: new Color(1.05f, 0.88f, 0.70f, 1f),
            contrast: -5f,
            saturation: -10f,
            vignetteIntensity: 0.47f /* base 0.32 + 0.15 boost */);

        public static readonly BiomePalette Desert = new BiomePalette(
            BiomeType.Desert,
            colorFilter: new Color(1.10f, 1.05f, 0.85f, 1f),
            contrast: 12f,
            saturation: 15f,
            vignetteIntensity: 0.32f /* base — no boost */);

        public static readonly BiomePalette Jungle = new BiomePalette(
            BiomeType.Jungle,
            colorFilter: new Color(0.85f, 1.05f, 0.90f, 1f),
            contrast: 5f,
            saturation: 12f,
            vignetteIntensity: 0.32f);

        public static readonly BiomePalette Ruins = new BiomePalette(
            BiomeType.Ruins,
            colorFilter: new Color(0.95f, 0.95f, 1.00f, 1f),
            contrast: -8f,
            saturation: -25f,
            vignetteIntensity: 0.47f);
    }
}
