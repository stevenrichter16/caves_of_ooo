using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Populates a zone with creatures and items from a PopulationTable.
    /// Mirrors Qud's PopTableZoneBuilder: categorizes cells by type,
    /// then places entities in appropriate cells.
    ///
    /// Priority: VERY_LATE (4000) -- after all terrain and connectivity.
    /// </summary>
    public class PopulationBuilder : IZoneBuilder
    {
        public string Name => "PopulationBuilder";
        public int Priority => 4000;
        public PopulationTable Table;

        public PopulationBuilder(PopulationTable table)
        {
            Table = table;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (Table == null) return true;

            // Categorize open cells (passable and not already occupied by a solid entity).
            // BIOME-OVERHAUL A2: cells claimed by structure stamps
            // (Zone.GenReservedCells) are excluded — no random spawns
            // inside authored interiors.
            var openCells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable() && !zone.GenReservedCells.Contains((x, y)))
                    openCells.Add((x, y));
            });

            // Roll the population table
            var toSpawn = Table.Roll(rng);

            // Place each entity in a random open cell
            foreach (var blueprintName in toSpawn)
            {
                if (openCells.Count == 0) break;

                int idx = rng.Next(openCells.Count);
                var (x, y) = openCells[idx];

                // BuilderSpawn rather than CreateEntity directly: a table
                // entry naming a blueprint this factory does not have is a
                // content problem, not a crash-or-log-spam problem. Test
                // fixtures deliberately load reduced blueprint sets, and
                // every other builder in worldgen already guards this way
                // (see BuilderSpawn, added when a stray 'Grass' entry did
                // the same thing). Missing blueprints in SHIPPED content are
                // caught by the per-biome table tests instead.
                BuilderSpawn.TryPlace(zone, factory, blueprintName, x, y);

                // Remove used cell to prevent double-placement of solid entities
                openCells.RemoveAt(idx);
            }

            return true;
        }
    }
}
