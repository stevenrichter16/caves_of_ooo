using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>A communal nest on a dry sima bank, before random population.
    /// Never placed in a descent, settlement chamber, or surface zone.</summary>
    public sealed class SimaNestBuilder : IZoneBuilder
    {
        public string Name => "SimaNest";
        public int Priority => 3900;
        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;
            foreach (var entity in zone.GetReadOnlyEntities())
                if (entity.BlueprintName == "PricklebrowNest") return true;
            var defenderCells = new System.Collections.Generic.List<Cell>(PricklebrowNestPart.DefenderCount);
            for (int attempt = 0; attempt < 300; attempt++)
            {
                int x = 3 + rng.Next(Zone.Width - 6), y = 3 + rng.Next(Zone.Height - 6);
                var cell = zone.GetCell(x, y);
                if (cell.BlocksMovement() || zone.GenReservedCells.Contains((x, y))
                    || StumpFaunaHabitat.Contains(cell, "MirePool")) continue;
                PricklebrowNestPart.CollectDefenderCells(zone, x, y, defenderCells);
                if (defenderCells.Count != PricklebrowNestPart.DefenderCount) continue;
                BuilderSpawn.TryPlaceOnce(zone, factory, "PricklebrowNest", x, y);
                break;
            }
            return true;
        }
    }
}
