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

        /// <summary>Name of the reed decoration blueprint.</summary>
        private const string ReedsBlueprint = "Reeds";

        /// <summary>
        /// Modulus for reed-row selection — a row qualifies as a reed row
        /// when <c>(hash(y) % ReedDensityModulus) &lt; ReedDensityThreshold</c>.
        /// With modulus 7 and threshold 2 we hit ~2/7 ≈ 28% of rows, giving
        /// about 7 reeds per 25-row zone.
        /// </summary>
        private const int ReedDensityModulus = 7;

        /// <summary>Inclusive threshold used with ReedDensityModulus.</summary>
        private const int ReedDensityThreshold = 2;

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
                    // Value identifies depth: bank 0 (centerX) is "core"
                    // (deeper, darker palette); bank 1 (centerX + 1) is
                    // "bank" (shallower, brighter palette). ZoneRenderer
                    // selects the palette from this tag value.
                    water.Tags[FlowsSouthTag] = bank == 0 ? "core" : "bank";

                    // Override the default "~" glyph with dashes for the
                    // starting static look. The renderer overrides this
                    // glyph every frame in UpdateAmbientAnimations using
                    // FlowGlyphs, so this only controls the very first
                    // paint (before the animation loop runs).
                    var render = water.GetPart<RenderPart>();
                    if (render != null)
                        render.GlyphVariants = "---";

                    zone.AddEntity(water, x, y);
                }
            }

            // Reeds: sparse, deterministic vegetation on cells flanking the
            // river. Runs after the river loop so we never place a reed in
            // a cell we just flooded. Skipped silently if the Reeds
            // blueprint is unavailable (keeps builder forward-compatible).
            if (factory.Blueprints.ContainsKey(ReedsBlueprint))
                PlaceReeds(zone, factory, baseX, height);

            return true;
        }

        /// <summary>
        /// Place vertical reed glyphs on cells immediately west or east of
        /// the river channel. Alternates sides and skips most rows so the
        /// vegetation reads as a few accents rather than a continuous wall.
        ///
        /// Determinism is load-bearing: the hash uses only <c>y</c>, so a
        /// fresh zone generation produces identical reed positions every
        /// time — same promise the river itself makes.
        /// </summary>
        private void PlaceReeds(Zone zone, EntityFactory factory, int baseX, int height)
        {
            for (int y = 0; y < height; y++)
            {
                // Knuth multiplicative hash — cheap, well-distributed, no
                // RNG dependency. Mask to 31 bits so the modulo is safe
                // from negative-int wrap.
                uint h = ((uint)(y * 2654435761U)) & 0x7FFFFFFF;
                if ((h % ReedDensityModulus) >= ReedDensityThreshold) continue;

                // Recompute centerX rather than passing it in — keeps the
                // reed loop's body self-contained and the river loop free
                // of per-row side structures.
                float angle = (y * 2f * System.MathF.PI) / MeanderWavelength;
                int centerX = baseX + (int)System.MathF.Round(MeanderAmplitude * System.MathF.Sin(angle));

                bool westSide = (h & 1U) == 0U;
                int reedX = westSide ? centerX - 1 : centerX + RiverWidth;

                if (!zone.InBounds(reedX, y)) continue;

                Cell cell = zone.GetCell(reedX, y);
                if (cell == null || !cell.IsPassable()) continue;

                // Don't stack reeds on top of existing decor, water, or
                // props. Layer 0 = floor terrain (fine to place over);
                // layer 1+ = water/walls/furniture/NPCs (skip).
                if (CellHasDecorOrAbove(cell)) continue;

                Entity reed = factory.CreateEntity(ReedsBlueprint);
                if (reed == null) continue;

                zone.AddEntity(reed, reedX, y);
            }
        }

        /// <summary>
        /// True if any entity on this cell has a RenderPart with RenderLayer
        /// &gt;= 1 — i.e. something more than bare floor terrain. Includes
        /// water, walls, furniture, villager spawn markers, etc.
        /// </summary>
        private static bool CellHasDecorOrAbove(Cell cell)
        {
            foreach (var obj in cell.Objects)
            {
                var r = obj.GetPart<RenderPart>();
                if (r != null && r.RenderLayer >= 1) return true;
            }
            return false;
        }
    }
}
