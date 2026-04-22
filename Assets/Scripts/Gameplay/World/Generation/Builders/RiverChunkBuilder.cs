using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Fills an entire 80×25 zone with a faithful port of the river.ascii
    /// HTML demo: a horizontal (+x) river flowing across the whole width,
    /// flanked on both sides by noise-driven bank vegetation.
    ///
    /// This is distinct from <see cref="RiverBuilder"/>, which carves a
    /// narrow 2-wide N→S slice through a village. RiverChunk is the whole
    /// zone — there's no village content, only water and bank entities.
    ///
    /// Runs on any zone attached to a POI of type RiverChunk (routed by
    /// OverworldZoneManager.CreateRiverChunkPipeline).
    ///
    /// <para>Math (ported from river.ascii "steady" preset):</para>
    /// <list type="bullet">
    /// <item>centerline y = middle + 1.5·sin(u·2.5+0.3) + 0.5·sin(u·6.0+1.1)
    ///   — a two-harmonic meander, where u = x/Zone.Width.</item>
    /// <item>halfWidth = 4.5 + 0.4·sin(u·3.7) — gentle width breathing.</item>
    /// <item>Cells inside [center − halfW, center + halfW] are water;
    ///   everything else is bank.</item>
    /// </list>
    ///
    /// Entities carry tags that the renderer reads:
    /// <list type="bullet">
    /// <item>Water: <c>FlowsEast</c> with value = rel ∈ [0,1] (0 at center, 1 at edge).</item>
    /// <item>Bank:  <c>BankDepth</c> = [0,1], <c>BankAbove</c> = "1" if above centerline, "0" otherwise.</item>
    /// </list>
    /// The renderer samples a scalar field per frame and picks glyph + color
    /// from thresholds. Generation just lays down the entity skeleton.
    /// </summary>
    public class RiverChunkBuilder : IZoneBuilder
    {
        public string Name => "RiverChunkBuilder";
        public int Priority => 2000;

        /// <summary>Tag placed on every water entity; tag value = rel ∈ [0,1].</summary>
        private const string FlowsEastTag = "FlowsEast";

        /// <summary>Tag on every bank entity; tag value = bankDepth ∈ [0,1].</summary>
        private const string BankDepthTag = "BankDepth";

        /// <summary>Tag on every bank entity; "1" = above centerline, "0" = below.</summary>
        private const string BankAboveTag = "BankAbove";

        // ---- HTML "steady" preset ----
        // HTML halfW for steady preset = 0.36 of a ~15-cell-tall screen
        // ≈ 5.4 cells. Our 25-row zone scales up; using 4.5 keeps plenty
        // of room for banks on both sides (25 - 9 = 16 bank rows total).
        private const float HalfWidthBase = 4.5f;
        private const float HalfWidthVar  = 0.4f;
        private const float WidthVarFreq  = 3.7f;

        // Meander amplitudes (cells). HTML: 0.06·rows and 0.02·rows.
        private const float MeanderAmp1  = 1.5f;
        private const float MeanderFreq1 = 2.5f;
        private const float MeanderPhase1 = 0.3f;
        private const float MeanderAmp2  = 0.5f;
        private const float MeanderFreq2 = 6.0f;
        private const float MeanderPhase2 = 1.1f;

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null) return true;
            if (!factory.Blueprints.ContainsKey("WaterPuddle")) return true;
            if (!factory.Blueprints.ContainsKey("Bank")) return true;

            int w = Zone.Width;
            int h = Zone.Height;
            float centerY = h * 0.5f;

            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w;
                float center = centerY
                             + MeanderAmp1 * System.MathF.Sin(u * MeanderFreq1 + MeanderPhase1)
                             + MeanderAmp2 * System.MathF.Sin(u * MeanderFreq2 + MeanderPhase2);
                float halfW = HalfWidthBase + HalfWidthVar * System.MathF.Sin(u * WidthVarFreq);

                for (int y = 0; y < h; y++)
                {
                    float dist = System.MathF.Abs(y - center);
                    Cell cell = zone.GetCell(x, y);
                    if (cell == null) continue;

                    if (dist <= halfW)
                    {
                        // Water cell.
                        float rel = dist / halfW; // 0 at center, 1 at channel edge
                        Entity water = factory.CreateEntity("WaterPuddle");
                        if (water == null) continue;

                        // Clamp rel to [0, 1] in case of rounding slop at the edge.
                        if (rel < 0f) rel = 0f;
                        else if (rel > 1f) rel = 1f;

                        // Tag value is the rel as a short string so the
                        // renderer can parse it back to float without
                        // recomputing centerline.
                        water.Tags[FlowsEastTag] = rel.ToString("F3");
                        zone.AddEntity(water, x, y);
                    }
                    else
                    {
                        // Bank cell. bankDepth grows with distance from the
                        // water's edge; HTML caps it at 1 after ~4.5 cells
                        // of bank (0.18 of a 25-row screen in HTML units).
                        float bankDepth = (dist - halfW) / 4.5f;
                        if (bankDepth < 0f) bankDepth = 0f;
                        else if (bankDepth > 1f) bankDepth = 1f;

                        Entity bank = factory.CreateEntity("Bank");
                        if (bank == null) continue;
                        bank.Tags[BankDepthTag] = bankDepth.ToString("F3");
                        bank.Tags[BankAboveTag] = (y < center) ? "1" : "0";
                        zone.AddEntity(bank, x, y);
                    }
                }
            }

            return true;
        }
    }
}
