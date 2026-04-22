using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part: a sittable chair. Responds to IdleQueryEvent.
    /// Optionally has an Owner to restrict who can sit.
    /// Mirrors Qud's Chair part's IdleQueryEvent handling.
    ///
    /// Reservation model: Occupied is set to true at query time (synchronously,
    /// inside HandleIdleQuery), not when the NPC actually sits. This prevents
    /// two NPCs from both receiving offers for the same chair. If the NPC fails
    /// to reach the chair, the Cleanup callback on the offer rolls back Occupied.
    /// </summary>
    public class ChairPart : AIBehaviorPart
    {
        public override string Name => "Chair";

        /// <summary>
        /// Owner restriction. If set, only entities with this tag or matching ID can use the chair.
        /// Empty = anyone can sit.
        /// </summary>
        public string Owner = "";

        /// <summary>
        /// True if the chair is currently claimed (offer outstanding) or occupied (NPC sitting).
        /// Cleared by SittingEffect.OnRemove (when NPC stands) or by the offer's Cleanup
        /// callback (when NPC fails to reach the chair).
        /// </summary>
        public bool Occupied;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == IdleQueryEvent.ID)
                return HandleIdleQuery(e);
            return true;
        }

        private bool HandleIdleQuery(GameEvent e)
        {
            if (Occupied) return true;

            var querier = e.GetParameter<Entity>("Querier");
            if (querier == null) return true;

            // Owner filtering
            if (!string.IsNullOrEmpty(Owner))
            {
                if (querier.ID != Owner && !querier.HasTag(Owner))
                    return true;
            }

            // Require AllowIdleBehavior tag on querier
            if (!querier.HasTag("AllowIdleBehavior"))
                return true;

            // Find chair's cell
            var querierBrain = querier.GetPart<BrainPart>();
            if (querierBrain?.CurrentZone == null) return true;
            var chairCell = querierBrain.CurrentZone.GetEntityCell(ParentEntity);
            if (chairCell == null) return true;

            // Reserve the chair synchronously to prevent double-booking
            Occupied = true;

            var chairEntity = ParentEntity;
            e.SetParameter("TargetX", chairCell.X);
            e.SetParameter("TargetY", chairCell.Y);
            e.SetParameter("Action", (object)(Action<GoalHandler>)(goal =>
            {
                // On arrival: apply the sitting effect. Occupied is already true.
                goal.ParentBrain?.ParentEntity?.ApplyEffect(new SittingEffect(chairEntity));
            }));
            e.SetParameter("Cleanup", (object)(Action<GoalHandler>)(goal =>
            {
                // Rollback: NPC never reached the chair. Release the reservation.
                var chairPart = chairEntity.GetPart<ChairPart>();
                if (chairPart != null) chairPart.Occupied = false;
            }));
            e.Handled = true;
            return false;
        }
    }
}
