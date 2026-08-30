using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>The tepui's elevation bands. None = not on the mountain.</summary>
    public enum StumpBand
    {
        None = 0,
        /// <summary>Outer chunks: blackwater creeks, waterfalls,
        /// spray-zone pools — the biodiversity hotspots.</summary>
        Foothills,
        /// <summary>Mid ring: fungal forest over root-buttress ridges;
        /// the Grainfield runs here.</summary>
        Slopes,
        /// <summary>Inner: the petrified canopy — bromeliad scrub on
        /// stone domes, cloud passing through.</summary>
        Summit,
    }

    /// <summary>
    /// W6.1 (Docs/FELLING-W6-PLAN.md §3) — elevation bands as authored
    /// data. Canon: "Elevation is the content key" (FELLING-WORLD-DESIGN
    /// §3.6) — bands key population tables, tint, and the terrain
    /// builder family. Authored rows in the house style of
    /// WorldMapAuthoring.BiomeRows/TierRows, NOT derived geometry: the
    /// T region is irregular (the east face drops off faster than the
    /// west), so a distance formula misclassifies its shoulders.
    ///
    /// <para><b>R1 fence:</b> StumpBandTests pins total coverage in
    /// both directions — every T cell banded, every banded cell T — so
    /// drift between these rows and BiomeRows fails loudly.</para>
    /// </summary>
    public static class StumpBands
    {
        // '.'=off-mountain, F=Foothills, S=Slopes, U=Summit.
        // Mirrors BiomeRows rows 0-5; the T region spans cols 1-5.
        private static readonly string[] BandRows =
        {
            "....................", // 0
            "..FFF...............", // 1
            ".FSUSF..............", // 2
            ".FUUUF..............", // 3  (3,3) — the Root sleeps under the summit
            ".FSUF...............", // 4
            "..FSSF..............", // 5  (3,5) — the Felling-Site, base-slope
        };

        public static StumpBand BandAt(int x, int y)
        {
            if (y < 0 || y >= BandRows.Length) return StumpBand.None;
            if (x < 0 || x >= BandRows[y].Length) return StumpBand.None;
            switch (BandRows[y][x])
            {
                case 'F': return StumpBand.Foothills;
                case 'S': return StumpBand.Slopes;
                case 'U': return StumpBand.Summit;
                default:  return StumpBand.None;
            }
        }

        /// <summary>Canon: "Tint shifts with band: warm base → cool
        /// bright summit." The foothills ARE the biome tint; each band
        /// up trades warmth for cool brightness. Pure so the render
        /// pin needs no zone.</summary>
        public static Color TintFor(StumpBand band, Color baseTint)
        {
            switch (band)
            {
                case StumpBand.Slopes:
                    return new Color(baseTint.r - 0.03f, baseTint.g + 0.01f,
                                     Mathf.Min(1f, baseTint.b + 0.04f));
                case StumpBand.Summit:
                    return new Color(baseTint.r - 0.07f, baseTint.g + 0.02f,
                                     Mathf.Min(1f, baseTint.b + 0.08f));
                default:
                    return baseTint;
            }
        }
    }
}
