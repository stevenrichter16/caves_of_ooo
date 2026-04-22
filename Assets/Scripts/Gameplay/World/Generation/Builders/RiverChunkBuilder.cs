using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Along which axis the river flows. East = +x, South = +y.</summary>
    public enum RiverFlowDirection
    {
        East,
        South
    }

    /// <summary>
    /// HTML-faithful ASCII river. Places Water entities (and optionally Bank
    /// entities) using a two-harmonic meander + sinusoidal width variation.
    /// Works either as a full-zone river (default: halfWidth=4.5, banks on)
    /// or as a narrow channel threading through a village (halfWidth=2.0,
    /// skipBanks=true, direction=South).
    ///
    /// <para>Port of river.ascii (steady preset). See the HTML demo for
    /// the math reference.</para>
    ///
    /// <para>Tag scheme on water entities:</para>
    /// <list type="bullet">
    /// <item><c>FlowsEast</c> (direction=East), value = rel ∈ [0,1] as F3 float.</item>
    /// <item><c>FlowsSouth</c> (direction=South), value = rel ∈ [0,1] as F3 float.
    ///   The legacy narrow <see cref="RiverBuilder"/> also uses FlowsSouth but
    ///   with string values <c>"core"</c>/<c>"bank"</c>; the renderer detects
    ///   which format by parsing.</item>
    /// </list>
    /// </summary>
    public class RiverChunkBuilder : IZoneBuilder
    {
        public string Name => "RiverChunkBuilder";
        public int Priority => 3850;

        private const string FlowsEastTag = "FlowsEast";
        private const string FlowsSouthTag = "FlowsSouth";
        private const string BankDepthTag = "BankDepth";
        private const string BankAboveTag = "BankAbove";

        // ---- HTML "steady" preset (fixed; non-parametric) ----
        private const float WidthVarAmp   = 0.4f;
        private const float WidthVarFreq  = 3.7f;
        private const float MeanderAmp1   = 1.5f;
        private const float MeanderFreq1  = 2.5f;
        private const float MeanderPhase1 = 0.3f;
        private const float MeanderAmp2   = 0.5f;
        private const float MeanderFreq2  = 6.0f;
        private const float MeanderPhase2 = 1.1f;

        /// <summary>Bank-depth falloff distance in cells. Controls how quickly
        /// bank glyphs ramp from empty → dense vegetation.</summary>
        private const float BankFadeDistance = 4.5f;

        // ---- Configurable per-instance ----

        private readonly float _halfWidthBase;
        private readonly bool _skipBanks;
        private readonly RiverFlowDirection _direction;
        private readonly int _crossCenterOffset;
        private readonly bool _clearSolidEntities;

        /// <summary>
        /// Construct a river builder.
        /// </summary>
        /// <param name="halfWidthBase">Nominal half-thickness of the water channel in cells.
        /// 4.5 → ~9-cell channel (full-zone rivers). 2.0 → ~4-cell channel (village).</param>
        /// <param name="skipBanks">When true, no Bank entities are placed — cells outside
        /// the channel are left alone so the underlying village terrain shows through.</param>
        /// <param name="direction">East = horizontal +x flow; South = vertical +y flow.</param>
        /// <param name="crossCenterOffset">Shift the channel's centerline away from zone
        /// center by this many cells in the cross-flow axis. For a horizontal river at
        /// the BOTTOM of the zone use +8 (center y ≈ 20). For N→S east-side use +30
        /// (center x ≈ 70).</param>
        /// <param name="clearSolidEntities">When true, water cells forcibly remove any
        /// entity tagged "Solid" (walls, wells, ovens, fences) from the cell before
        /// placing water. When false (default), the channel skips non-passable cells
        /// so the river routes AROUND existing structures. Use true when the river
        /// should take priority over village layout; false when the village should
        /// keep its integrity and the river accommodates it.</param>
        public RiverChunkBuilder(
            float halfWidthBase = 4.5f,
            bool skipBanks = false,
            RiverFlowDirection direction = RiverFlowDirection.East,
            int crossCenterOffset = 0,
            bool clearSolidEntities = false)
        {
            _halfWidthBase = halfWidthBase;
            _skipBanks = skipBanks;
            _direction = direction;
            _crossCenterOffset = crossCenterOffset;
            _clearSolidEntities = clearSolidEntities;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null) return true;
            if (!factory.Blueprints.ContainsKey("WaterPuddle")) return true;
            if (!_skipBanks && !factory.Blueprints.ContainsKey("Bank")) return true;

            // Axis mapping: "along" is the flow direction; "cross" is perpendicular.
            bool isEast = _direction == RiverFlowDirection.East;
            int alongSize = isEast ? Zone.Width  : Zone.Height;
            int crossSize = isEast ? Zone.Height : Zone.Width;
            float crossCenter = crossSize * 0.5f + _crossCenterOffset;

            string flowTag = isEast ? FlowsEastTag : FlowsSouthTag;

            for (int a = 0; a < alongSize; a++)
            {
                float u = a / (float)alongSize;
                float center = crossCenter
                             + MeanderAmp1 * System.MathF.Sin(u * MeanderFreq1 + MeanderPhase1)
                             + MeanderAmp2 * System.MathF.Sin(u * MeanderFreq2 + MeanderPhase2);
                float halfW = _halfWidthBase + WidthVarAmp * System.MathF.Sin(u * WidthVarFreq);

                for (int c = 0; c < crossSize; c++)
                {
                    // Map (along, cross) → (x, y) per direction.
                    int x = isEast ? a : c;
                    int y = isEast ? c : a;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null) continue;

                    float dist = System.MathF.Abs(c - center);

                    if (dist <= halfW)
                    {
                        // Water cell. If clearSolidEntities is on, bulldoze
                        // any walls/wells/ovens in the river's path — the
                        // river takes priority over village layout. When
                        // off, skip non-passable cells so the channel
                        // routes around existing structures.
                        if (_clearSolidEntities && !cell.IsPassable())
                        {
                            var toRemove = new System.Collections.Generic.List<Entity>();
                            foreach (var obj in cell.Objects)
                            {
                                if (obj.HasTag("Solid"))
                                    toRemove.Add(obj);
                            }
                            foreach (var obj in toRemove)
                                zone.RemoveEntity(obj);
                        }

                        if (!cell.IsPassable()) continue;

                        float rel = dist / halfW;
                        if (rel < 0f) rel = 0f;
                        else if (rel > 1f) rel = 1f;

                        Entity water = factory.CreateEntity("WaterPuddle");
                        if (water == null) continue;
                        water.Tags[flowTag] = rel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                        zone.AddEntity(water, x, y);
                    }
                    else if (!_skipBanks)
                    {
                        // Bank cell. Only placed when banks are enabled
                        // (full-zone river chunks).
                        float bankDepth = (dist - halfW) / BankFadeDistance;
                        if (bankDepth < 0f) bankDepth = 0f;
                        else if (bankDepth > 1f) bankDepth = 1f;

                        Entity bank = factory.CreateEntity("Bank");
                        if (bank == null) continue;
                        bank.Tags[BankDepthTag] = bankDepth.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                        // Gravity-aware glyphs need to know which side of the
                        // centerline. In East flow: above means lower y (north
                        // on screen); in South flow we keep the same semantic
                        // (c < center = "above") even though "above" is loose
                        // when the flow is vertical — the pair of glyphs just
                        // alternates by side which still reads as organic.
                        bank.Tags[BankAboveTag] = (c < center) ? "1" : "0";
                        zone.AddEntity(bank, x, y);
                    }
                    // else (dist > halfW && _skipBanks): leave cell alone —
                    // normal village terrain occupies it.
                }
            }

            return true;
        }
    }
}
