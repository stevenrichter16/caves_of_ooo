using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Guarantees a plantable grass plot near the player spawn,
    /// regardless of biome. The farming starter kit (seeds + grimoire)
    /// is granted unconditionally at bootstrap, but only some biomes
    /// generate <c>Grass</c> (Jungle, Village) — a Ruins/stone start
    /// would leave the seeds unusable. Called once from GameBootstrap
    /// after the player is placed. See
    /// <c>Docs/CROPS-WATERING-GRIMOIRE.md §5 SM6</c>.
    ///
    /// <para><b>Conservative conversion contract:</b> counts existing
    /// Plantable cells within the plot radius first and no-ops when
    /// there are already enough (grassy biomes untouched). Otherwise it
    /// converts cells nearest-first, but ONLY cells that are: in-bounds,
    /// not solid, not interior (never pave a building's floor), free of
    /// water (<see cref="LiquidPoolPart"/>), and either bare of terrain
    /// or holding nothing but plain replaceable floors
    /// (<see cref="ReplaceableFloors"/> — the generator's Floor/stone
    /// family). Special terrain like riverbank <c>Bank</c> entities is
    /// never replaced. Converted cells end with exactly ONE terrain
    /// entity (the old floor is removed, Grass added).</para>
    /// </summary>
    public static class FarmPlotSeeder
    {
        /// <summary>Minimum plantable cells guaranteed around spawn.</summary>
        public const int MIN_PLANTABLE_CELLS = 6;

        /// <summary>Chebyshev radius of the plot area around the spawn cell.</summary>
        public const int PLOT_RADIUS = 2;

        /// <summary>
        /// Plain generator floors that may be swapped for Grass. An
        /// explicit allowlist — anything not listed (Bank, future
        /// special terrains) is preserved. Covers every open-ground
        /// floor the builders actually lay: Floor/Rubble/stone family
        /// (Cave, Ruins) and Sand (DesertBuilder floors whole zones
        /// with it; VillageBuilder/LairBuilder desert palettes use it
        /// for floor AND path — its omission failed the "regardless of
        /// biome" contract on the entire desert family; SM7d, farming
        /// audit F5).
        /// </summary>
        private static readonly HashSet<string> ReplaceableFloors = new HashSet<string>
        {
            "Floor", "Rubble", "StoneFloor", "SandstoneFloor", "LimestoneFloor",
            "ShaleFloor", "SlateFloor", "QuartziteFloor", "ObsidianFloor",
            "Sand"
        };

        /// <summary>
        /// Ensure at least <see cref="MIN_PLANTABLE_CELLS"/> plantable
        /// cells exist within <see cref="PLOT_RADIUS"/> of the given
        /// center. Returns the number of plantable cells present after
        /// the call (0 on null zone/factory).
        /// </summary>
        public static int EnsurePlantablePlot(Zone zone, int centerX, int centerY, EntityFactory factory)
        {
            if (zone == null || factory == null) return 0;

            int plantable = CountPlantableCells(zone, centerX, centerY);
            if (plantable >= MIN_PLANTABLE_CELLS)
                return plantable;

            int converted = 0;

            // Nearest-first rings so the plot hugs the spawn point.
            for (int ring = 0; ring <= PLOT_RADIUS && plantable < MIN_PLANTABLE_CELLS; ring++)
            {
                for (int y = centerY - ring; y <= centerY + ring && plantable < MIN_PLANTABLE_CELLS; y++)
                {
                    for (int x = centerX - ring; x <= centerX + ring && plantable < MIN_PLANTABLE_CELLS; x++)
                    {
                        // Ring perimeter only (interior covered by smaller rings).
                        int cheb = System.Math.Max(System.Math.Abs(x - centerX), System.Math.Abs(y - centerY));
                        if (cheb != ring) continue;

                        if (TryConvertCell(zone, x, y, factory))
                        {
                            plantable++;
                            converted++;
                        }
                    }
                }
            }

            if (converted > 0)
            {
                if (Diag.IsChannelEnabled("crop"))
                    Diag.Record("crop", "FarmPlotSeeded",
                        payload: new { converted = converted, plantableTotal = plantable, x = centerX, y = centerY });
            }

            // Loud failure branch (SM7d, audit note): a seeder that ran
            // and could NOT meet its guarantee (all-interior spawn,
            // heavily walled area) was previously indistinguishable from
            // the healthy grassy no-op in a diag query. Every gate that
            // can fail emits a record on the failure branch too.
            if (plantable < MIN_PLANTABLE_CELLS)
            {
                if (Diag.IsChannelEnabled("crop"))
                    Diag.Record("crop", "FarmPlotSeedingFailed",
                        payload: new { plantableTotal = plantable, needed = MIN_PLANTABLE_CELLS, converted = converted, x = centerX, y = centerY });
            }

            return plantable;
        }

        private static int CountPlantableCells(Zone zone, int centerX, int centerY)
        {
            int count = 0;
            for (int y = centerY - PLOT_RADIUS; y <= centerY + PLOT_RADIUS; y++)
            {
                for (int x = centerX - PLOT_RADIUS; x <= centerX + PLOT_RADIUS; x++)
                {
                    if (!zone.InBounds(x, y)) continue;
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    if (HasPlantableTerrain(cell)) count++;
                }
            }
            return count;
        }

        private static bool HasPlantableTerrain(Cell cell)
        {
            if (BarrenGroundRules.IsBarren(cell)) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasTag("Terrain") && cell.Objects[i].HasTag("Plantable"))
                    return true;
            return false;
        }

        /// <summary>Convert one cell to Grass if every guard passes.
        /// Returns true only when the cell is now newly plantable.</summary>
        private static bool TryConvertCell(Zone zone, int x, int y, EntityFactory factory)
        {
            if (!zone.InBounds(x, y)) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null) return false;
            if (BarrenGroundRules.IsBarren(cell)) return false;
            if (cell.IsInterior) return false;              // never pave building floors
            if (cell.IsSolid()) return false;               // walls, trees
            if (HasPlantableTerrain(cell)) return false;    // already counted
            if (cell.HasObjectWithPart<LiquidPoolPart>()) return false; // water

            // Gather existing terrain; bail if ANY of it is not a plain
            // replaceable floor (Bank, unknown special terrain).
            List<Entity> toReplace = null;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var obj = cell.Objects[i];
                if (!obj.HasTag("Terrain")) continue;
                if (!ReplaceableFloors.Contains(obj.BlueprintName)) return false;
                (toReplace ??= new List<Entity>(1)).Add(obj);
            }

            var grass = factory.CreateEntity("Grass");
            if (grass == null) return false;

            if (!zone.AddEntity(grass, x, y)) return false;

            if (toReplace != null)
                for (int i = 0; i < toReplace.Count; i++)
                    zone.RemoveEntity(toReplace[i]);

            ZoneRenderHooks.MarkCellDirty(x, y, "FarmPlotSeeded");
            return true;
        }
    }
}
