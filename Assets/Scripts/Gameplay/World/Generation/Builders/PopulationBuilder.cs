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

            // Categorize open cells (passable and not already occupied by a solid entity)
            var openCells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
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

                Entity entity = factory.CreateEntity(blueprintName);
                if (entity != null)
                    zone.AddEntity(entity, x, y);

                // Remove used cell to prevent double-placement of solid entities
                openCells.RemoveAt(idx);
            }

            return true;
        }
    }
}
