using System;
using CavesOfOoo.Core.Inventory;

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
        private SittingEffect _reservation;

        public override void FinalizeLoad(SaveReader reader)
        {
            var bed = ParentEntity;
            var zone = bed?.SpatialZone;
            if (bed?.GetPart<BedPart>() != this || !WorldResourceActions.Ground(bed, zone)
                || bed.GetPart<PhysicsPart>().Takeable) return;

            // Arrival callbacks don't survive a save. Only the physical sleeper
            // can carry a reservation across Continue.
            _reservation = null;
            var cell = zone.GetEntityCell(bed);
            foreach (var occupant in cell.Objects)
            {
                if (!occupant.HasTag("Creature") || !WorldResourceActions.Ground(occupant, zone)
                    || zone.GetEntityCell(occupant) != cell || CombatSystem.IsDeathHandled(occupant)
                    || occupant.GetStatValue("Hitpoints") <= 0) continue;

                var sitting = occupant.GetEffect<SittingEffect>();
                if (sitting == null || sitting.Owner != occupant || sitting.Furniture != bed || sitting.Duration == 0) continue;
                _reservation = sitting;
                break;
            }
            Occupied = _reservation != null;
        }

        internal void Release(SittingEffect reservation)
        {
            if (reservation == null || _reservation != reservation || ParentEntity?.GetPart<BedPart>() != this) return;
            _reservation = null;
            Occupied = false;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions" && e.GetParameter<Entity>("Actor")?.HasTag("Player") == true)
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Sleep", "sleep in bed", PlayerBedService.RestCommand, 's', 10);
                actions?.AddAction("SleepNextBand", "sleep until the next time of day", PlayerBedService.NextBandCommand, 'n', 9);
            }
            if (e.ID == "InventoryAction")
            {
                if (PlayerBedService.TryAct(e.GetParameter<Entity>("Actor"), ParentEntity,
                    e.GetParameter<Zone>("Zone"), e.GetStringParameter("Command"),
                    e.GetParameter<InventoryTransaction>("InventoryTransaction")))
                { e.Handled = true; return false; }
                return true;
            }
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

            // Keep the exact receipt through both arrival and cancellation.
            var reservation = new SittingEffect(ParentEntity);
            _reservation = reservation;
            Occupied = true;

            e.SetParameter("TargetX", bedCell.X);
            e.SetParameter("TargetY", bedCell.Y);
            e.SetParameter("Action", (object)(Action<GoalHandler>)(goal => CompleteOffer(goal, querier, reservation)));
            e.SetParameter("Cleanup", (object)(Action<GoalHandler>)(_ =>
            {
                if (querier.GetEffect<SittingEffect>() != reservation) Release(reservation);
            }));
            e.Handled = true;
            return false;
        }

        private void CompleteOffer(GoalHandler goal, Entity querier, SittingEffect reservation)
        {
            if (_reservation != reservation) return;
            var bed = reservation.Furniture;
            var brain = goal?.ParentBrain;
            var zone = brain?.CurrentZone;
            var cell = zone?.GetEntityCell(bed);
            bool Current() => _reservation == reservation && bed == ParentEntity && bed?.GetPart<BedPart>() == this
                && reservation.Furniture == bed && cell != null && brain.ParentEntity == querier
                && zone.GetEntityCell(bed) == cell && zone.GetEntityCell(querier) == cell;

            if (!Current())
            {
                Release(reservation);
                return;
            }

            try { querier.ApplyEffect(reservation); }
            finally
            {
                if (!Current() || reservation.Owner != querier || querier.GetEffect<SittingEffect>() != reservation)
                {
                    Release(reservation);
                    querier.GetPart<StatusEffectsPart>()?.RemoveEffect(reservation);
                }
            }
        }
    }
}
