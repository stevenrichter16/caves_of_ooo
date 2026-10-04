namespace CavesOfOoo.Core
{
    /// <summary>Pure public combat state, shared by Look, Examine and the local
    /// ray cue. Only current, visible physical owners may expose a commitment.</summary>
    public static class CombatIntentReadout
    {
        public static bool IsVisibleActor(Entity actor, Zone zone)
        {
            if (actor == null || zone == null || string.IsNullOrWhiteSpace(actor.ID) || actor.SpatialZone != zone
                || !actor.HasTag("Creature") || actor.GetStatValue("Hitpoints", 0) <= 0
                || CombatSystem.IsDeathHandled(actor)) return false;
            var cell = zone.GetEntityCell(actor);
            var render = actor.GetPart<RenderPart>();
            var physical = actor.GetPart<PhysicsPart>();
            return VisibleCell(zone, cell) && cell.Objects.Contains(actor)
                && render?.ParentEntity == actor && render.Visible
                && physical?.ParentEntity == actor && physical.InInventory == null && physical.Equipped == null;
        }

        /// <summary>Current actor tell; null for idle, fogged or malformed owners.</summary>
        public static string ActorLine(Entity actor, Zone zone)
        {
            var intent = CurrentIntent(actor, zone);
            if (intent == null || (!intent.IsWindingUp && !intent.IsRecovering)) return null;
            // A removed/moved windup may await its next authoritative AI action.
            // Do not describe its old direction at a different physical origin.
            var at = zone.GetEntityCell(actor);
            if (intent.IsWindingUp && (intent.CommittedZoneId != zone.ZoneID
                || at.X != intent.OriginX || at.Y != intent.OriginY)) return null;
            string text = intent.Describe();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>The same visible segment predicate serves text and geometry.
        /// A hidden intermediate cell never leaks a warning into a visible far tile.</summary>
        public static bool ThreatensVisibleCell(Entity actor, Zone zone, Cell cell)
        {
            var intent = CurrentIntent(actor, zone);
            if (intent == null || !intent.IsWindingUp || !VisibleCell(zone, cell)) return false;
            for (int step = 1; step <= 2; step++)
            {
                int x = intent.OriginX + intent.DirectionX * step;
                int y = intent.OriginY + intent.DirectionY * step;
                var rayCell = zone.GetCell(x, y);
                if (!VisibleCell(zone, rayCell) || !intent.ThreatensCell(zone, x, y)) return false;
                if (rayCell == cell) return true;
            }
            return false;
        }

        /// <summary>Warn about a real visible prepared strike, without predicting damage.</summary>
        public static string ThreatLine(Zone zone, Cell cell)
        {
            if (!VisibleCell(zone, cell)) return null;
            foreach (var actor in zone.GetReadOnlyEntities())
                if (ThreatensVisibleCell(actor, zone, cell))
                {
                    var intent = actor.GetPart<CommittedMeleePart>();
                    return "In the path of " + actor.GetDisplayName() + "'s " + intent.AttackName
                        + ". Step aside or interrupt the attack.";
                }
            return null;
        }

        private static CommittedMeleePart CurrentIntent(Entity actor, Zone zone)
        {
            if (!IsVisibleActor(actor, zone)) return null;
            var intent = actor.GetPart<CommittedMeleePart>();
            if (intent?.ParentEntity != actor || actor.HasTag("Player")) return null;
            int count = 0;
            for (int i = 0; i < actor.Parts.Count; i++)
                if (actor.Parts[i] is CommittedMeleePart) count++;
            return count == 1 ? intent : null;
        }

        private static bool VisibleCell(Zone zone, Cell cell) => zone != null && cell != null
            && cell.ParentZone == zone && zone.GetCell(cell.X, cell.Y) == cell && cell.IsVisible && cell.Explored;
    }
}
