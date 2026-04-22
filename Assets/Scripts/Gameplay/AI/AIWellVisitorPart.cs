using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// AI behavior part that occasionally sends the NPC to walk near the village well.
    /// Proves the AIBoredEvent pipeline works for generic NPC behaviors beyond guarding.
    ///
    /// Attach to NPCs via blueprint:
    ///   { "Name": "AIWellVisitor", "Params": [{ "Key": "Chance", "Value": "5" }] }
    ///
    /// Chance is a percentage (0-100) per bored tick. Default 5 = 5% per tick.
    /// The NPC walks to a passable cell adjacent to the well, not onto it.
    /// </summary>
    public class AIWellVisitorPart : AIBehaviorPart
    {
        public override string Name => "AIWellVisitor";

        /// <summary>Percent chance per bored tick to visit the well (0-100).</summary>
        public int Chance = 5;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == AIBoredEvent.ID)
            {
                bool result = HandleBored();
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleBored()
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain?.Rng == null || brain.CurrentZone == null)
                return true;

            // TODO: When party/follower system exists (Phase 8), gate this with
            // CanAIDoIndependentBehavior to prevent followers from wandering to the well.

            // Probability gate
            if (brain.Rng.Next(100) >= Chance)
                return true;

            // Find the well in the zone
            Entity well = FindWell(brain.CurrentZone);
            if (well == null)
                return true;

            var wellCell = brain.CurrentZone.GetEntityCell(well);
            if (wellCell == null)
                return true;

            // Pick a random passable cell adjacent to the well (don't walk ON the well)
            var adjacent = GetPassableAdjacentCells(brain.CurrentZone, wellCell.X, wellCell.Y);
            if (adjacent.Count == 0)
                return true;

            var target = adjacent[brain.Rng.Next(adjacent.Count)];
            brain.PushGoal(new MoveToGoal(target.x, target.y, 50));
            return false; // consumed
        }

        private static Entity FindWell(Zone zone)
        {
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.BlueprintName == "Well")
                    return entity;
            }
            return null;
        }

        private static List<(int x, int y)> GetPassableAdjacentCells(Zone zone, int cx, int cy)
        {
            var result = new List<(int x, int y)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (!zone.InBounds(nx, ny)) continue;
                    var cell = zone.GetCell(nx, ny);
                    if (cell != null && cell.IsPassable())
                        result.Add((nx, ny));
                }
            }
            return result;
        }
    }
}
