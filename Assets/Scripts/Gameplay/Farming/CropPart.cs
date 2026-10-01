using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A planted crop growing in a zone cell. Data-driven via blueprint
    /// params (all public fields — EntityFactory reflection sets them,
    /// and SaveSystem's Tier-3 <c>WritePublicFields</c> round-trips them
    /// automatically). See <c>Docs/CROPS-WATERING-GRIMOIRE.md</c>.
    ///
    /// <para><b>Growth model:</b> 0 (seed) and 1 (sprout) advance only
    /// while moist. Legacy crops then drop produce automatically. Opt-in
    /// <see cref="HarvestAtMaturity"/> crops remain at 2 (ripe) until the
    /// player harvests them. Moisture still dries after ripening, but the
    /// harvest does not spoil. CropSystem owns all growth/moisture ticks;
    /// this Part owns the explicit harvest event and saved definition.</para>
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
        /// the crop into produce, or enters standing ripe stage 2 when
        /// HarvestAtMaturity is enabled (handled by CropSystem).</summary>
        public int GrowthStage = 0;

        /// <summary>Ticks of growth accumulated inside the current stage.
        /// Only advances while moist.</summary>
        public int TicksInStage = 0;

        /// <summary>Ticks required to complete each stage. Blueprint
        /// param. CropSystemPart accepts player-stamped TickEnd once per
        /// active player round; un-stamped test events remain supported.</summary>
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
        /// completes (legacy) or is harvested (cultivated).</summary>
        public string YieldBlueprint = "";

        /// <summary>How many produce items drop at maturity.</summary>
        public int YieldCount = 1;

        /// <summary>Opt-in cultivated crops remain at stage 2 until harvested.
        /// False preserves the original seed/sprout/automatic-drop lifecycle.</summary>
        public bool HarvestAtMaturity;
        /// <summary>Optional real seed blueprint and units returned alongside
        /// produce. Only the opt-in cultivated harvest uses these saved fields.</summary>
        public string SeedYieldBlueprint = "";
        public int SeedYieldCount;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                if (HarvestAtMaturity && GrowthStage == 2)
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction("Harvest", "harvest", "HarvestCultivatedCrop", 'h', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "HarvestCultivatedCrop") return true;
            var actor = e.GetParameter<Entity>("Actor");
            if (actor == null) return true;
            if (!CropYieldService.TryRelease(this, e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone,
                actor, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true;
            return false;
        }


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

        /// <summary>Read-only visible-world description. A detached, hidden or
        /// foreign crop cannot advertise live growth through a stale reader.</summary>
        internal string DescribeGrowth(Zone zone, Cell cell)
        {
            var owner = ParentEntity;
            if (owner == null || !ReferenceEquals(owner.GetPart<CropPart>(), this)
                || zone == null || !ReferenceEquals(SettlementRuntime.ActiveZone, zone)
                || cell == null || cell.ParentZone != zone || !cell.IsVisible
                || owner.SpatialZone != zone || !ReferenceEquals(zone.GetEntityCell(owner), cell)
                || !cell.Objects.Contains(owner) || GrowthStage < 0 || GrowthStage > (HarvestAtMaturity ? 2 : 1)
                || MoistureTicks < 0 || TicksPerStage <= 0 || TicksInStage < 0)
                return null;
            var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            if (render == null || render.ParentEntity != owner || !render.Visible
                || physics == null || physics.ParentEntity != owner
                || physics.InInventory != null || physics.Equipped != null)
                return null;
            if (HarvestAtMaturity && GrowthStage == 2)
                return "Growth: ripe. Harvest this crop by hand; its produce and saved seed remain here to pick up. The prepared bed can be planted again.";
            return "Growth: " + (GrowthStage == 0 ? "seed" : "sprout") + ". Soil is "
                + (MoistureTicks > 0 ? "moist; growth continues." : "dry; growth is paused.")
                + " Conjure Rain nearby to water this crop. "
                + (HarvestAtMaturity ? "At maturity, Harvest by hand; produce and saved seed remain here to pick up."
                    : "At maturity, produce falls here to pick up.")
                + " Crops grow only while you are in this area.";
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
