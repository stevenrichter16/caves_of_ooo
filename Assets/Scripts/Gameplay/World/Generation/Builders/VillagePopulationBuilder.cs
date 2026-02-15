using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Populates a village zone with role-based NPCs (Elder, Merchant, Warden, Villagers)
    /// and decorative objects (Campfire, Well, MarketStall).
    /// Deterministic roles -- not random PopulationTable rolls.
    /// Priority: VERY_LATE (4000) — after all terrain and connectivity.
    /// </summary>
    public class VillagePopulationBuilder : IZoneBuilder
    {
        public string Name => "VillagePopulationBuilder";
        public int Priority => 4000;

        private PointOfInterest _poi;

        public VillagePopulationBuilder(PointOfInterest poi)
        {
            _poi = poi;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            var openCells = GatherOpenCells(zone);
            if (openCells.Count == 0) return true;

            // Place decor first (Campfire, Well in village square area)
            var decorTable = PopulationTable.VillageDecor();
            var decorBlueprints = decorTable.Roll(rng);
            foreach (var bp in decorBlueprints)
            {
                PlaceEntity(zone, factory, rng, openCells, bp);
            }

            // Deterministic NPC roles
            // 1 Elder (always)
            PlaceNPC(zone, factory, rng, openCells, "Elder");

            // 1 Merchant (always)
            PlaceNPC(zone, factory, rng, openCells, "Merchant");

            // 0-1 Tinker (70% chance)
            if (rng.Next(100) < 70)
                PlaceNPC(zone, factory, rng, openCells, "Tinker");

            // 0-1 Warden (60% chance)
            if (rng.Next(100) < 60)
                PlaceNPC(zone, factory, rng, openCells, "Warden");

            // 2-4 Villagers
            int villagerCount = rng.Next(2, 5);
            for (int i = 0; i < villagerCount; i++)
                PlaceNPC(zone, factory, rng, openCells, "Villager");

            return true;
        }

        private void PlaceNPC(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string blueprint)
        {
            Entity npc = PlaceEntity(zone, factory, rng, openCells, blueprint);
            if (npc != null && _poi?.Faction != null)
            {
                // Override faction to match POI
                npc.SetTag("Faction", _poi.Faction);
            }
        }

        private Entity PlaceEntity(Zone zone, EntityFactory factory, System.Random rng,
            List<(int x, int y)> openCells, string blueprint)
        {
            if (openCells.Count == 0) return null;

            int idx = rng.Next(openCells.Count);
            var (x, y) = openCells[idx];

            Entity entity = factory.CreateEntity(blueprint);
            if (entity != null)
                zone.AddEntity(entity, x, y);

            openCells.RemoveAt(idx);
            return entity;
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
