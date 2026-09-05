using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original summit ecology. A healthy cryptic lizard takes
    /// one ordinary step toward visible bromeliad cover when approached.
    /// Selection is bounded and allocates no collections or goal objects.</summary>
    public sealed class AIBromeliadRetreatPart : AIBehaviorPart
    {
        public override string Name => "AIBromeliadRetreat";
        public int ApproachRadius = 3;
        public int CoverRadius = 5;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != AIBoredEvent.ID) return true;
            using (PerformanceMarkers.Turns.BromeliadRetreat.Auto())
                return Retreat(e);
        }

        private bool Retreat(GameEvent e)
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            var zone = brain?.CurrentZone;
            var here = zone?.GetEntityCell(ParentEntity);
            if (here == null) return true;
            e.Handled = true;
            if (StumpFaunaHabitat.Contains(here, "TankBrocchinia"))
                return Decision("already_sheltered", here);
            // Quiet turns keep the animal still. Injured/retaliatory behavior
            // remains the normal Brain branch preceding AIBored.
            Cell threat = null;
            int nearest = int.MaxValue;
            int radius = Math.Clamp(ApproachRadius, 0, 6);
            for (int x = here.X - radius; x <= here.X + radius; x++)
                for (int y = here.Y - radius; y <= here.Y + radius; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    int distance = AIHelpers.ChebyshevDistance(here.X, here.Y, x, y);
                    if (distance >= nearest) continue;
                    for (int i = 0; i < cell.Objects.Count; i++)
                    {
                        var visitor = cell.Objects[i];
                        if (visitor == ParentEntity || !visitor.HasTag("Creature")
                            || visitor.GetStatValue("Hitpoints") <= 0
                            || FactionManager.GetFaction(visitor) == FactionManager.GetFaction(ParentEntity)) continue;
                        if (!AIHelpers.HasLineOfSight(zone, here.X, here.Y, x, y)) continue;
                        threat = cell; nearest = distance; break;
                    }
                }
            if (threat == null) return Decision("no_threat", here);

            Cell cover = null;
            int best = int.MaxValue;
            radius = Math.Clamp(CoverRadius, 0, 6);
            for (int x = here.X - radius; x <= here.X + radius; x++)
                for (int y = here.Y - radius; y <= here.Y + radius; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null || cell.BlocksMovement() || !StumpFaunaHabitat.Contains(cell, "TankBrocchinia")) continue;
                    int distance = AIHelpers.ChebyshevDistance(here.X, here.Y, x, y);
                    if (distance >= best || AIHelpers.ChebyshevDistance(x, y, threat.X, threat.Y) < nearest) continue;
                    if (!AIHelpers.HasLineOfSight(zone, here.X, here.Y, x, y)) continue;
                    cover = cell; best = distance;
                }
            if (cover == null) return Decision("no_cover", here);
            int stepX = 0, stepY = 0;
            for (int d = 0; d < 8; d++)
            {
                var (dx, dy) = MovementSystem.DirectionToDelta(d);
                int x = here.X + dx, y = here.Y + dy;
                var cell = zone.GetCell(x, y);
                if (cell == null || cell.BlocksMovement()) continue;
                int distance = AIHelpers.ChebyshevDistance(x, y, cover.X, cover.Y);
                if (distance >= best || AIHelpers.ChebyshevDistance(x, y, threat.X, threat.Y) < nearest) continue;
                best = distance; stepX = dx; stepY = dy;
            }
            if (stepX != 0 || stepY != 0)
            {
                bool moved = MovementSystem.TryMove(ParentEntity, zone, stepX, stepY);
                return Decision(moved ? "moved" : "movement_veto", here, cover);
            }
            return Decision("no_safe_step", here, cover);
        }

        private bool Decision(string decision, Cell here, Cell cover = null)
        {
            // Opt-in AI detail avoids filling the default diagnostic ring on
            // quiet turns. No payload is allocated when the channel is off.
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", "BromeliadRetreat", ParentEntity, null,
                    new { decision, moved = decision == "moved", x = here.X, y = here.Y,
                        coverX = cover?.X ?? -1, coverY = cover?.Y ?? -1 });
            return false;
        }
    }
}
