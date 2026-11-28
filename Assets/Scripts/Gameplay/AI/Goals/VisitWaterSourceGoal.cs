namespace CavesOfOoo.Core
{
    /// <summary>Optional water visit. Movement ends if its exact source or safe contact route goes away.</summary>
    public sealed class VisitWaterSourceGoal : GoalHandler
    {
        internal const int TravelLimit = 50;

        // The save graph resolves the exact world owner again on Continue.
        public Entity Source;
        public string ZoneId;

        public VisitWaterSourceGoal(string zoneId, Entity source)
        {
            ZoneId = zoneId;
            Source = source;
        }

        internal static bool IsAvailable(Zone zone, Entity source) => source?.GetPart<WellPart>() is { } well
            && WorldResourceActions.Ground(source, zone) && WellDrinkingService.SourceAvailable(well, zone);

        internal static FindPath Route(Zone zone, Entity actor, Entity source)
        {
            var route = FindPath.ToContact(zone, actor, source, avoidHazards: true);
            return route.Usable ? route : null;
        }

        private bool IsCurrent() => CurrentZone?.ZoneID == ZoneId && ParentBrain?.PartyLeader == null
            && WorldResourceActions.ActorCurrent(ParentEntity, CurrentZone) && IsAvailable(CurrentZone, Source);

        public override bool Finished() => Age >= TravelLimit || !IsCurrent()
            || SpatialQuery.Distance(CurrentZone, ParentEntity, Source) == 1;

        public override string GetDetails() => Source == null ? null : "source=" + Source.GetDisplayName();

        public override void TakeAction()
        {
            if (Finished())
            {
                Pop();
                return;
            }

            var route = Route(CurrentZone, ParentEntity, Source);
            if (route == null || route.Steps.Count == 0 || route.Steps.Count > TravelLimit - Age)
            {
                Pop();
                return;
            }

            var (dx, dy) = route.Steps[0];
            var move = MovementSystem.TryMoveDetailed(ParentEntity, CurrentZone, dx, dy);
            if (!move.Moved && !move.ActionPerformed) Pop();
        }
    }
}
