using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Populates a lair zone with biome-appropriate guards and loot near the boss.
    /// Priority: VERY_LATE (4000) — after all terrain and connectivity.
    /// </summary>
    public class LairPopulationBuilder : IZoneBuilder
    {
        public string Name => "LairPopulationBuilder";
        public int Priority => 4000;

        private BiomeType _biome;
        private PointOfInterest _poi;

        public LairPopulationBuilder(BiomeType biome, PointOfInterest poi)
        {
            _biome = biome;
            _poi = poi;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return true;

            // Roll lair guards from biome-specific table
            var guardTable = PopulationTable.LairGuards(_biome);
            var guards = guardTable.Roll(rng);
            foreach (var blueprint in guards)
            {
                PlaceEntity(zone, factory, rng, openCells, blueprint);
            }

            // Scatter 1-2 loot items near center (boss area)
            int lootCount = rng.Next(1, 3);
            string[] lootPool = { "LongSword", "LeatherArmor", "ChainMail", "IronHelmet", "HealingTonic" };
            for (int i = 0; i < lootCount; i++)
            {
                string loot = lootPool[rng.Next(lootPool.Length)];
                PlaceEntity(zone, factory, rng, openCells, loot);
            }

            return true;
        }

        private void PlaceEntity(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string blueprint)
        {
            if (openCells.Count == 0) return;

            int idx = rng.Next(openCells.Count);
            var (x, y) = openCells[idx];

            Entity entity = factory.CreateEntity(blueprint);
            if (entity != null)
                zone.AddEntity(entity, x, y);

            openCells.RemoveAt(idx);
        }

        private List<(int x, int y)> GatherOpenCells(Zone zone)
        {
            var cells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (cell.IsPassable())
                    cells.Add((x, y));
            });
            return cells;
        }
    }
}
