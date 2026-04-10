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
        /// Tick non-creature entities that participate in the material
        /// simulation. Burning entities get a full BeginTakeAction (fuel
        /// consumption, damage, heat propagation) followed by EndTurn
        /// (temperature decay, evaporation, extinguish check). Non-burning
        /// entities that are wet or thermally displaced from ambient get
        /// only an EndTurn so their WetEffect evaporates and ThermalPart
        /// cools naturally. Creatures are excluded because they already
        /// tick via TurnManager. Call this once per player turn.
        /// </summary>
        public static void TickMaterialEntities(Zone zone)
        {
            if (zone == null)
                return;

            // Snapshot the relevant entities so newly-ignited or newly-wet
            // entities this tick don't get processed twice in the same turn.
            var burning = new System.Collections.Generic.List<Entity>();
            var passive = new System.Collections.Generic.List<Entity>();
            zone.ForEachCell((cell, x, y) =>
            {
                for (int i = 0; i < cell.Objects.Count; i++)
                {
                    var obj = cell.Objects[i];
                    if (obj.HasTag("Creature"))
                        continue;

                    if (obj.HasEffect<BurningEffect>())
                    {
                        burning.Add(obj);
                        continue;
                    }

                    // Non-burning entities still need ticking if they have
                    // moisture to evaporate, persistent status to decay,
                    // a lifespan to count down, or temperature to drift back
                    // toward ambient.
                    if (obj.HasEffect<WetEffect>()
                        || obj.HasEffect<FrozenEffect>()
                        || obj.HasEffect<AcidicEffect>()
                        || obj.HasEffect<ElectrifiedEffect>()
                        || obj.GetPart<LifespanPart>() != null)
                    {
                        passive.Add(obj);
                        continue;
                    }

                    var thermal = obj.GetPart<ThermalPart>();
                    if (thermal != null && thermal.Temperature != thermal.AmbientTemperature)
                        passive.Add(obj);
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

            for (int i = 0; i < passive.Count; i++)
            {
                Entity entity = passive[i];

                var endTurn = GameEvent.New("EndTurn");
                endTurn.SetParameter("Zone", (object)zone);
                entity.FireEvent(endTurn);
                endTurn.Release();

                // Drive data-driven reactions for non-burning props: hot-but-not-ignited
                // entities run fire_plus_raw_* cooking; frozen brittle metal runs the
                // cold_plus_metal path; acid-coated wood runs acid_plus_organic. Burning
                // entities are skipped here because BurningEffect.OnTurnStart already
                // invoked EvaluateReactions during the burning list pass above.
                MaterialReactionResolver.EvaluateReactions(entity, zone, null);
            }
        }

        /// <summary>
        /// Emit radiant heat from a source entity to all entities with ThermalPart
        /// in the 8 adjacent cells. Joules are split equally among directions.
        /// Negative joules cool adjacent entities (e.g., steam, frost bloom).
        /// </summary>
        public static void EmitHeatToAdjacent(Entity source, Zone zone, float totalJoules)
        {
            if (source == null || zone == null || totalJoules == 0f)
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
