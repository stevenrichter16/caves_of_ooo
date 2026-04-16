using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Furniture part: a bed/sleeping mat. Responds to IdleQueryEvent.
    /// Same pattern as ChairPart but semantically represents resting furniture.
    ///
    /// Reservation model: Occupied is set at query time and rolled back via the
    /// offer's Cleanup callback if the NPC fails to reach the bed.
    /// </summary>
    public class BedPart : AIBehaviorPart
    {
        public override string Name => "Bed";

        public string Owner = "";
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

            if (!string.IsNullOrEmpty(Owner))
            {
                if (querier.ID != Owner && !querier.HasTag(Owner))
                    return true;
            }

            if (!querier.HasTag("AllowIdleBehavior"))
                return true;

            var querierBrain = querier.GetPart<BrainPart>();
            if (querierBrain?.CurrentZone == null) return true;
            var bedCell = querierBrain.CurrentZone.GetEntityCell(ParentEntity);
            if (bedCell == null) return true;

            // Reserve the bed synchronously to prevent double-booking
            Occupied = true;

            var bedEntity = ParentEntity;
            e.SetParameter("TargetX", bedCell.X);
            e.SetParameter("TargetY", bedCell.Y);
            e.SetParameter("Action", (object)(Action<GoalHandler>)(goal =>
            {
                goal.ParentBrain?.ParentEntity?.ApplyEffect(new SittingEffect(bedEntity));
            }));
            e.SetParameter("Cleanup", (object)(Action<GoalHandler>)(goal =>
            {
                var bedPart = bedEntity.GetPart<BedPart>();
                if (bedPart != null) bedPart.Occupied = false;
            }));
            e.Handled = true;
            return false;
        }
    }
}
