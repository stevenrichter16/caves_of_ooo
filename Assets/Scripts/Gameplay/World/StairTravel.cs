using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Qud parity — pressing <c>&gt;</c> or <c>&lt;</c> when you are not
    /// standing on the staircase walks you to it. Qud's CmdMoveD does
    /// not scold you for being in the wrong cell; it travels, and it
    /// stops when something is worth stopping for.
    ///
    /// <para>This is the pure half — which staircase, what path, and
    /// when to stop — so all three are testable without an input loop.
    /// InputHandler owns the per-turn stepping.</para>
    /// </summary>
    public static class StairTravel
    {
        /// <summary>Nearest reachable staircase of the requested kind, or
        /// null when there is none to walk to — in which case the caller
        /// keeps its old behaviour (world-map ascent, or the honest
        /// "no stairs here").</summary>
        public static (int x, int y)? FindTarget(Zone zone, Entity actor, bool goingDown)
        {
            if (zone == null || actor == null) return null;
            var from = zone.GetEntityPosition(actor);
            if (from.x < 0) return null;

            (int x, int y)? best = null;
            int bestDist = int.MaxValue;
            foreach (var e in zone.GetAllEntities())
            {
                bool match = goingDown
                    ? e.GetPart<StairsDownPart>() != null
                    : e.GetPart<StairsUpPart>() != null;
                if (!match) continue;
                var p = zone.GetEntityPosition(e);
                if (p.x < 0) continue;

                int dist = System.Math.Max(System.Math.Abs(p.x - from.x),
                                           System.Math.Abs(p.y - from.y));
                if (dist >= bestDist) continue;
                // Nearest by walking distance, not as the crow flies:
                // an unreachable staircase is not a target at all.
                if (dist > 0 && PathTo(zone, actor, p.x, p.y) == null) continue;
                bestDist = dist;
                best = (p.x, p.y);
            }
            return best;
        }

        /// <summary>The steps from the actor to a cell, or null when no
        /// route exists. Creatures are ignored for planning (they move);
        /// the per-step walk re-checks and gives up if it is blocked.</summary>
        public static List<(int dx, int dy)> PathTo(Zone zone, Entity actor, int x, int y)
        {
            if (zone == null || actor == null) return null;
            var from = zone.GetEntityPosition(actor);
            if (from.x < 0) return null;
            var path = FindPath.Search(zone, from.x, from.y, x, y,
                ignoreCreatures: true, actor: actor);
            if (!path.Usable) return null;
            return path.Steps;
        }

        /// <summary>Stop walking. Travel that continues while something
        /// hostile closes on you is how a player loses a character to a
        /// convenience feature — so any hostile in the zone ends it.</summary>
        public static bool ShouldInterrupt(Zone zone, Entity actor)
        {
            if (zone == null || actor == null) return true;
            foreach (var e in zone.GetAllEntities())
            {
                if (e == actor || !e.Tags.ContainsKey("Creature")) continue;
                if (e.GetStatValue("Hitpoints", 0) <= 0) continue;
                if (FactionManager.IsHostile(e, actor)) return true;
            }
            return false;
        }
    }
}
