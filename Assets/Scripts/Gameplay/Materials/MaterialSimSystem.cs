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
        /// Tick all burning non-creature entities in a zone, advancing fire
        /// simulation (fuel consumption, damage, heat propagation, extinguish
        /// checks). Creatures are excluded because they already tick via
        /// TurnManager. Call this once per player turn.
        /// </summary>
        public static void TickBurningEntities(Zone zone)
        {
            if (zone == null)
                return;

            // Snapshot the burning entities so newly-ignited entities this tick
            // don't get processed twice in the same turn.
            var burning = new System.Collections.Generic.List<Entity>();
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var obj = cell.Objects[i];
                    if (obj.HasTag("Creature"))
                        continue;
                    if (obj.HasEffect<BurningEffect>())
                        burning.Add(obj);
                }
            });

            for (int i = 0; i < burning.Count; i++)
            {
                Entity entity = burning[i];

                var beginTurn = GameEvent.New("BeginTakeAction");
                beginTurn.SetParameter("Zone", (object)zone);
                entity.FireEvent(beginTurn);
                beginTurn.Release();

                var endTurn = GameEvent.New("EndTurn");
                endTurn.SetParameter("Zone", (object)zone);
                entity.FireEvent(endTurn);
                endTurn.Release();
            }
        }

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
