using System.Collections.Generic;
using UnityEngine.Pool;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Static utility for material simulation operations.
    /// Handles heat propagation between adjacent cells.
    /// Consistent with CombatSystem/MovementSystem static patterns.
    /// </summary>
    public static class MaterialSimSystem
    {
        /// <summary>Candidate owners inspected by the most recent material turn, before creature/activity gates.</summary>
        public static int LastCandidateCount { get; private set; }

        /// <summary>
        /// Tick non-creature entities that participate in the material
        /// simulation. Burning or steam-bearing entities get a full BeginTakeAction (fuel
        /// consumption, damage, heat propagation) followed by EndTurn
        /// (temperature decay, evaporation, extinguish check). Other non-burning
        /// entities that are wet or thermally displaced from ambient get
        /// only an EndTurn so their WetEffect evaporates and ThermalPart
        /// cools naturally. Creatures are excluded because they already
        /// tick via TurnManager. Call this once per player turn.
        /// </summary>
        public static void TickMaterialEntities(Zone zone)
        {
            if (zone == null)
                return;

            // Snapshot only potentially active owners, but read their current
            // mutable fields every turn. Preserve the old X/Y/object order and
            // take all activity decisions before any callbacks can ignite others.
            var candidates = ListPool<Entity>.Get();
            var unique = HashSetPool<Entity>.Get();
            var active = ListPool<Entity>.Get();
            var passive = ListPool<Entity>.Get();
            try
            {
                Collect(zone.GetReadOnlyEntitiesWithPart<ThermalPart>(), unique, candidates);
                Collect(zone.GetReadOnlyEntitiesWithPart<StatusEffectsPart>(), unique, candidates);
                Collect(zone.GetReadOnlyEntitiesWithPart<LifespanPart>(), unique, candidates);
                zone.SortInCellOrder(candidates);
                LastCandidateCount = candidates.Count;
                for (int i = 0; i < candidates.Count; i++)
                {
                    var obj = candidates[i];
                    if (obj.HasTag("Creature")) continue;
                    if (obj.HasEffect<BurningEffect>() || obj.HasEffect<SteamEffect>())
                    { active.Add(obj); continue; }
                    if (obj.HasEffect<WetEffect>() || obj.HasEffect<FrozenEffect>()
                        || obj.HasEffect<AcidicEffect>() || obj.HasEffect<ElectrifiedEffect>()
                        || obj.GetPart<LifespanPart>() != null)
                    { passive.Add(obj); continue; }
                    var thermal = obj.GetPart<ThermalPart>();
                    if (thermal != null && thermal.Temperature != thermal.AmbientTemperature)
                        passive.Add(obj);
                }

                for (int i = 0; i < active.Count; i++)
                {
                    Entity entity = active[i];
                    if (zone.GetEntityCell(entity) == null) continue;
                    bool wasBurning = entity.HasEffect<BurningEffect>();

                    var beginTurn = GameEvent.New("BeginTakeAction");
                    beginTurn.SetParameter("Zone", (object)zone);
                    entity.FireEvent(beginTurn);
                    beginTurn.Release();
                    if (zone.GetEntityCell(entity) == null) continue;

                    var endTurn = GameEvent.New("EndTurn");
                    endTurn.SetParameter("Zone", (object)zone);
                    entity.FireEvent(endTurn);
                    endTurn.Release();

                    // Steam-only props previously belonged to the passive path.
                    // Preserve their data reactions without re-evaluating burning
                    // reactions already dispatched by BurningEffect.OnTurnStart.
                    if (!wasBurning && zone.GetEntityCell(entity) != null)
                        MaterialReactionResolver.EvaluateReactions(entity, zone, null);
                }

                for (int i = 0; i < passive.Count; i++)
                {
                    Entity entity = passive[i];
                    if (zone.GetEntityCell(entity) == null) continue;

                    var endTurn = GameEvent.New("EndTurn");
                    endTurn.SetParameter("Zone", (object)zone);
                    entity.FireEvent(endTurn);
                    endTurn.Release();

                    // Drive data-driven reactions for non-burning props: hot-but-not-ignited
                    // entities run fire_plus_raw_* cooking; frozen brittle metal runs the
                    // cold_plus_metal path; acid-coated wood runs acid_plus_organic. Burning
                    // entities are skipped here because BurningEffect.OnTurnStart already
                    // invoked EvaluateReactions during the active list pass above.
                    if (zone.GetEntityCell(entity) != null)
                        MaterialReactionResolver.EvaluateReactions(entity, zone, null);
                }
            }
            finally
            {
                ListPool<Entity>.Release(candidates); HashSetPool<Entity>.Release(unique);
                ListPool<Entity>.Release(active); ListPool<Entity>.Release(passive);
            }
        }

        private static void Collect(IReadOnlyList<Entity> owners, HashSet<Entity> unique, List<Entity> result)
        {
            for (int i = 0; i < owners.Count; i++) if (unique.Add(owners[i])) result.Add(owners[i]);
        }

        /// <summary>
        /// Emit heat from a source entity to all entities with ThermalPart
        /// in the 8 adjacent cells. The transfer is modeled as direct energy
        /// delivery per neighbor so small positive values always warm nearby
        /// props instead of asymptotically cooling them toward a low target.
        /// Negative values still cool adjacent entities (e.g., frost bloom).
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
                    heatEvent.SetParameter("Radiant", (object)false);
                    heatEvent.SetParameter("Source", (object)source);
                    heatEvent.SetParameter("Zone", (object)zone);
                    target.FireEvent(heatEvent);
                    heatEvent.Release();
                }
            }
        }
    }
}
