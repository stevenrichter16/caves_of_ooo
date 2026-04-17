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

            // Biome-specific ambush creatures — dormant until disturbed.
            // Uses AIAmbushPart + DormantGoal; see Phase 6 (M1.3).
            PlaceAmbushers(zone, factory, rng, openCells);

            return true;
        }

        /// <summary>
        /// Spawn dormant ambush creatures based on biome. Each creature type rolls
        /// independently so a lair can have any combination. Mimics appear in all
        /// biomes (disguised as chests are biome-independent); trolls/bandits are
        /// biome-specific.
        ///
        /// Ambush creatures are placed in ROOM cells (via <see cref="GatherRoomCells"/>)
        /// rather than any passable cell. This prevents a MimicChest from spawning
        /// in a 1-cell-wide corridor (where the "chest" is obviously out of place)
        /// or a SleepingTroll from blocking a hallway (where its sleep-particle
        /// animation is immediately visible as you round a corner). If no room
        /// cells are available, falls back to open cells.
        /// </summary>
        private void PlaceAmbushers(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells)
        {
            // Prefer room cells for ambushers (rooms feel more natural for chests,
            // sleeping creatures, and hidden bandits). Fallback to openCells if
            // the lair is all-corridor (rare).
            var roomCells = GatherRoomCells(zone);
            var placementPool = roomCells.Count > 0 ? roomCells : openCells;

            // Biome-specific sleeping creatures
            switch (_biome)
            {
                case BiomeType.Cave:
                    // 25% chance per cave lair to contain a sleeping troll
                    if (rng.Next(100) < 25)
                        PlaceEntity(zone, factory, rng, placementPool, "SleepingTroll");
                    break;
                case BiomeType.Desert:
                    // 30% chance per desert lair to contain an ambushing bandit
                    if (rng.Next(100) < 30)
                        PlaceEntity(zone, factory, rng, placementPool, "AmbushBandit");
                    break;
            }

            // Mimic chests: 0-2 per lair regardless of biome.
            // Uses (0,3) exclusive upper bound → rolls 0, 1, or 2 mimics.
            int mimicCount = rng.Next(3);
            for (int i = 0; i < mimicCount; i++)
            {
                PlaceEntity(zone, factory, rng, placementPool, "MimicChest");
            }
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

        /// <summary>
        /// Filter open cells to those inside a "room" — i.e., at least
        /// <see cref="MinRoomNeighbors"/> of the 8 surrounding cells are passable.
        /// This excludes 1-cell-wide corridors (which have 2 or fewer passable
        /// neighbors) and thin 2-cell corridors (up to 4). The threshold is
        /// calibrated so a cell in the interior of a 3x3 room (8 passable
        /// neighbors) easily qualifies, while corner/edge room cells (5-6
        /// neighbors) also qualify — only corridor cells are excluded.
        /// </summary>
        private const int MinRoomNeighbors = 5;

        private List<(int x, int y)> GatherRoomCells(Zone zone)
        {
            var cells = new List<(int x, int y)>();
            zone.ForEachCell((cell, x, y) =>
            {
                if (!cell.IsPassable()) return;
                int passableNeighbors = 0;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var adj = zone.GetCell(x + dx, y + dy);
                        if (adj != null && adj.IsPassable())
                            passableNeighbors++;
                    }
                }
                if (passableNeighbors >= MinRoomNeighbors)
                    cells.Add((x, y));
            });
            return cells;
        }
    }
}
