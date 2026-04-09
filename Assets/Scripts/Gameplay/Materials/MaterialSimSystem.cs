namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static utility for material simulation operations.
    /// Handles heat propagation between adjacent cells.
    /// Consistent with CombatSystem/MovementSystem static patterns.
    /// </summary>
    public static class MaterialSimSystem
    {
        /// <summary>
        /// Emit radiant heat from a source entity to all entities with ThermalPart
        /// in the 8 adjacent cells. Joules are split equally among directions.
        /// </summary>
        public static void EmitHeatToAdjacent(Entity source, Zone zone, float totalJoules)
        {
            if (source == null || zone == null || totalJoules <= 0f)
                return;

            var sourceCell = zone.GetEntityCell(source);
            if (sourceCell == null)
                return;

            float joulesPerDir = totalJoules / 8f;

            for (int dir = 0; dir < 8; dir++)
            {
                var cell = zone.GetCellInDirection(sourceCell.X, sourceCell.Y, dir);
                if (cell == null)
                    continue;

                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var target = cell.Objects[i];
                    if (target == source)
                        continue;

                    var thermal = target.GetPart<ThermalPart>();
                    if (thermal == null)
                        continue;

                    var heatEvent = GameEvent.New("ApplyHeat");
                    heatEvent.SetParameter("Joules", (object)joulesPerDir);
                    heatEvent.SetParameter("Radiant", (object)true);
                    heatEvent.SetParameter("Source", (object)source);
                    heatEvent.SetParameter("Zone", (object)zone);
                    target.FireEvent(heatEvent);
                    heatEvent.Release();
                }
            }
        }
    }
}
