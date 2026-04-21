using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Carves a gently-meandering 2-wide river of <c>WaterPuddle</c> entities
    /// down the east side of the starting village zone, flowing north → south.
    /// Each river cell gets a <c>FlowsSouth</c> tag so
    /// <c>ZoneRenderer.UpdateAmbientAnimations</c> can render a directional
    /// color wave instead of the stationary shimmer that village puddles use.
    ///
    /// Runs after terrain/connectivity/cave-entrance builders (priority 3850)
    /// but BEFORE <see cref="VillagePopulationBuilder"/> (4000) so that NPC
    /// placement in <c>PopulationBuilder</c> / <c>VillagePopulationBuilder</c>
    /// sees river cells as occupied and spawns villagers on dry ground. (The
    /// village's open-cell gather uses <c>IsPassable</c>; water puddles are
    /// passable but the population tables generally clump in the center of
    /// the map, not the east edge, so the river stays mostly unpopulated.)
    ///
    /// Path shape:
    ///   x = baseX + amplitude * sin(y * 2π / wavelength)
    ///   baseX        = Zone.Width - EastOffset  (defaults to col 72)
    ///   amplitude    = 3 cells
    ///   wavelength   = 10 rows
    ///   width        = 2 cells (center column + one east bank)
    ///
    /// The meander is deterministic — no RNG — so the river looks the same
    /// every time a fresh zone is generated. Keeps playtests predictable and
    /// lets scenario smoke tests make structural assertions about the river's
    /// approximate shape.
    ///
    /// Scope note: v1 only runs in the starting village zone
    /// (<c>Overworld.10.10.0</c>). Generalizing to cave/dungeon zones is a v2
    /// consideration — those zones have denser packing and the meander math
    /// would fight with their existing layout.
    /// </summary>
    public class RiverBuilder : IZoneBuilder
    {
        public string Name => "RiverBuilder";
        public int Priority => 3850;

        private const string StartingVillageZoneId = "Overworld.10.10.0";

        /// <summary>Columns from the east edge the river's center-x is pinned to.</summary>
        private const int EastOffset = 8;

        /// <summary>Meander amplitude in cells (+/- from baseX).</summary>
        private const int MeanderAmplitude = 3;

        /// <summary>Meander wavelength in rows (one full S-curve over this many Y).</summary>
        private const float MeanderWavelength = 10f;

        /// <summary>Width of the river channel in cells (including both banks).</summary>
        private const int RiverWidth = 2;

        /// <summary>Tag stamped on every river entity; read by the renderer to pick the flow animation.</summary>
        private const string FlowsSouthTag = "FlowsSouth";

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null) return true;
            if (zone.ZoneID != StartingVillageZoneId) return true;
            if (!factory.Blueprints.ContainsKey("WaterPuddle")) return true;

            int baseX = Zone.Width - EastOffset;
            int height = Zone.Height;

            for (int y = 0; y < height; y++)
            {
                // Center column for this row — sin() meander around baseX.
                // Using y directly (not y/wavelength * 2π cast through a float)
                // keeps the curve visibly gentle: over a 25-tall zone with
                // wavelength 10 we get 2.5 full oscillations.
                float angle = (y * 2f * System.MathF.PI) / MeanderWavelength;
                int centerX = baseX + (int)System.MathF.Round(MeanderAmplitude * System.MathF.Sin(angle));

                // Place RiverWidth tiles horizontally at each row.
                // Bank grows eastward (center + 0, center + 1, ...) so the
                // channel's WEST edge stays on a stable column relative to
                // centerX — gives the meander a cleaner silhouette.
                for (int bank = 0; bank < RiverWidth; bank++)
                {
                    int x = centerX + bank;
                    if (!zone.InBounds(x, y)) continue;

                    Cell cell = zone.GetCell(x, y);
                    if (cell == null || !cell.IsPassable()) continue;

                    Entity water = factory.CreateEntity("WaterPuddle");
                    if (water == null) continue;

                    // Tag for the renderer's directional-flow animation.
                    water.Tags[FlowsSouthTag] = string.Empty;

                    zone.AddEntity(water, x, y);
                }
            }

            return true;
        }
    }
}
