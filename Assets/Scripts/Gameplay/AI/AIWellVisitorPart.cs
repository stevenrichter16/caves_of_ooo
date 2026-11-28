namespace CavesOfOoo.Core
{
    /// <summary>An occasional idle walk to the contact edge of an available water source.</summary>
    public class AIWellVisitorPart : AIBehaviorPart
    {
        public override string Name => "AIWellVisitor";

        /// <summary>Percent chance per bored tick to visit water (0–100).</summary>
        public int Chance = 5;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID || !TryVisit())
                return true;

            e.Handled = true;
            return false;
        }

        private bool TryVisit()
        {
            var actor = ParentEntity;
            var brain = actor?.GetPart<BrainPart>();
            var zone = brain?.CurrentZone;
            if (brain?.Rng == null || brain.PartyLeader != null || !WorldResourceActions.ActorCurrent(actor, zone)
                || brain.Rng.Next(100) >= Chance)
                return false;

            Entity destination = null;
            int closest = VisitWaterSourceGoal.TravelLimit;
            foreach (var source in zone.GetReadOnlyEntities())
            {
                if (!VisitWaterSourceGoal.IsAvailable(zone, source)
                    || SpatialQuery.Distance(zone, actor, source) - 1 > closest) continue;
                var route = VisitWaterSourceGoal.Route(zone, actor, source);
                if (route == null) continue;
                int distance = route.Steps.Count;
                if (distance == 0) return false;
                if (distance > closest || (destination != null && distance == closest
                    && string.CompareOrdinal(source.ID, destination.ID) >= 0)) continue;
                destination = source;
                closest = distance;
            }

            if (destination == null) return false;
            brain.PushGoal(new VisitWaterSourceGoal(zone.ZoneID, destination));
            return true;
        }
    }
}
