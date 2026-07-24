using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A planted crop growing in a zone cell. Data-driven via blueprint
    /// params (all public fields — EntityFactory reflection sets them,
    /// and SaveSystem's Tier-3 <c>WritePublicFields</c> round-trips them
    /// automatically). See <c>Docs/CROPS-WATERING-GRIMOIRE.md</c>.
    ///
    /// <para><b>Growth model:</b> two visible stages — 0 (seed) and
    /// 1 (sprout). Growth advances ONLY while <see cref="MoistureTicks"/>
    /// &gt; 0; a dry crop pauses, it never dies (RPG framing — see
    /// PROJECT-IDENTITY). Completing the sprout stage replaces the crop
    /// with <see cref="YieldCount"/> × <see cref="YieldBlueprint"/>
    /// produce items lying in the cell (the ground-pickup flow closes
    /// the loop; no separate harvest UX in v1). All ticking happens in
    /// <see cref="CropSystem.OnTickEnd"/> — this Part holds state only,
    /// so there is exactly one decrement path (no double-tick risk).</para>
    ///
    /// <para><b>Wet-soil visual:</b> while moist, the crop's own
    /// <see cref="RenderPart.BackgroundColor"/> is set to
    /// <see cref="WET_SOIL_BG"/> — the renderer paints it as a solid
    /// block behind the glyph, auto-darkened
    /// (<c>QudColorParser.DarkenForBackground</c>, ZoneRenderer.cs:933),
    /// which reads as dark wet earth around the plant. The renderer only
    /// ever paints the TOP visible entity per cell, so darkening the
    /// terrain entity underneath would be invisible — the bg block on
    /// the crop itself is the correct mechanism (see the plan doc's
    /// correction C2). Crop blueprints must not author their own
    /// BackgroundColor: dry-out "restores" by clearing to empty.</para>
    /// </summary>
    public class CropPart : Part
    {
        public override string Name => "Crop";

        /// <summary>Background color code applied while moist. `^w`
        /// (brown) renders as a dark wet-earth block behind the crop
        /// glyph after the renderer's automatic background darkening.</summary>
        public const string WET_SOIL_BG = "^w";

        /// <summary>0 = seed, 1 = sprout. Completing stage 1 converts
        /// the crop into its produce (handled by CropSystem).</summary>
        public int GrowthStage = 0;

        /// <summary>Ticks of growth accumulated inside the current stage.
        /// Only advances while moist.</summary>
        public int TicksInStage = 0;

        /// <summary>Ticks required to complete each stage. Blueprint
        /// param. NOTE: TickEnd fires once per ACTOR-turn (N×/round in a
        /// busy zone) — values are tick-denominated, same convention as
        /// the gas system.</summary>
        public int TicksPerStage = 20;

        /// <summary>Remaining moisture. Growth advances and moisture
        /// decrements only while &gt; 0 (both in CropSystem's tick).</summary>
        public int MoistureTicks = 0;

        /// <summary>CSV of one glyph per stage, e.g. ".,τ". Parsed
        /// leniently: whitespace-trimmed, first char of each entry.</summary>
        public string StageGlyphsRaw = "";

        /// <summary>CSV of one color code per stage, e.g. "&amp;w,&amp;g".</summary>
        public string StageColorsRaw = "";

        /// <summary>Produce blueprint spawned when the sprout stage
        /// completes.</summary>
        public string YieldBlueprint = "";

        /// <summary>How many produce items drop at maturity.</summary>
        public int YieldCount = 1;

        /// <summary>
        /// Water this crop: top-up semantics — moisture becomes
        /// max(current, <paramref name="ticks"/>), never additive (two
        /// casts don't stack to 80). Applies the wet-soil background and
        /// repaints the cell. Idempotent under repeat watering.
        /// </summary>
        public void Water(int ticks)
        {
            if (ticks <= 0) return;
            if (ticks > MoistureTicks)
                MoistureTicks = ticks;

            var render = ParentEntity?.GetPart<RenderPart>();
            if (render != null && render.BackgroundColor != WET_SOIL_BG)
            {
                render.BackgroundColor = WET_SOIL_BG;
                MarkOwnCellDirty("CropWatered");
            }

            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "CropWatered", target: ParentEntity,
                    payload: new { moistureTicks = MoistureTicks });
        }

        /// <summary>
        /// Dry out: clear the wet-soil background and repaint. Called by
        /// CropSystem exactly once, when MoistureTicks reaches 0.
        /// </summary>
        public void OnDriedOut()
        {
            var render = ParentEntity?.GetPart<RenderPart>();
            if (render != null && !string.IsNullOrEmpty(render.BackgroundColor))
            {
                render.BackgroundColor = "";
                MarkOwnCellDirty("SoilDried");
            }

            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "SoilDried", target: ParentEntity);
        }

        /// <summary>Glyph for a given stage from the CSV, or '\0' when
        /// the CSV has no usable entry for that stage (caller keeps the
        /// current glyph).</summary>
        public char GlyphForStage(int stage)
        {
            string entry = CsvEntry(StageGlyphsRaw, stage);
            return string.IsNullOrEmpty(entry) ? '\0' : entry[0];
        }

        /// <summary>Color code for a given stage from the CSV, or null
        /// when absent (caller keeps the current color).</summary>
        public string ColorForStage(int stage)
        {
            string entry = CsvEntry(StageColorsRaw, stage);
            return string.IsNullOrEmpty(entry) ? null : entry;
        }

        private static string CsvEntry(string csv, int index)
        {
            if (string.IsNullOrEmpty(csv) || index < 0) return null;
            string[] parts = csv.Split(',');
            if (index >= parts.Length) return null;
            return parts[index].Trim();
        }

        private void MarkOwnCellDirty(string source)
        {
            var zone = SettlementRuntime.ActiveZone;
            if (zone == null || ParentEntity == null) return;
            var pos = zone.GetEntityPosition(ParentEntity);
            if (pos.x >= 0)
                ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, source);
        }
    }
}
