namespace CavesOfOoo.Core
{
    /// <summary>
    /// M3.3 — when the NPC is bored and HP dips below
    /// <see cref="FleeThreshold"/>, scans the zone for the nearest
    /// <see cref="SanctuaryPart"/>-bearing entity and pushes
    /// <see cref="FleeLocationGoal"/> targeting that cell. If no
    /// sanctuary exists in the zone, the event is NOT consumed — other
    /// behavior parts (notably <see cref="AISelfPreservationPart"/>)
    /// run normally and can fall back to their own retreat targets.
    ///
    /// Layering with <see cref="AISelfPreservationPart"/>:
    /// - Blueprints that wear BOTH should declare <c>AIFleeToShrine</c>
    ///   FIRST in the Parts array. Event dispatch is insertion-order
    ///   (<c>BlueprintLoader.Bake</c> → <c>Entity.FireEvent</c> iterates
    ///   parts in add order), so AIFleeToShrine sees the bored event
    ///   first.
    /// - When AIFleeToShrine pushes a FleeLocationGoal, it consumes
    ///   the event (<c>e.Handled = true; return false</c>). Entity's
    ///   FireEvent loop stops propagation → AISelfPreservation never
    ///   sees the event that turn → no competing RetreatGoal push.
    /// - When no shrine is found OR HP is above threshold, the event
    ///   is passed through unconsumed (<c>return true</c>) and
    ///   AISelfPreservation runs as normal. The shrine preference
    ///   degrades gracefully to the existing home-retreat behavior.
    ///
    /// FleeThreshold semantics:
    /// - Field default is 0.4f — matches AISelfPreservationPart's
    ///   default RetreatThreshold so both parts fire at roughly the
    ///   same HP and the blueprint-order priority note above actually
    ///   matters.
    /// - Distinct from <see cref="BrainPart.FleeThreshold"/> (default
    ///   0.25), which drives <see cref="FleeGoal"/> panic-flee from
    ///   the nearest threat. AIFleeToShrine is the "graceful retreat
    ///   to safety" complement, not the "panic" one.
    /// - Per-blueprint override via the Params array.
    ///
    /// Shared quirk with AISelfPreservation: since NoFightGoal
    /// suppresses AIBoredEvent, a pacified NPC at low HP will NOT
    /// flee to a shrine until the NoFightGoal expires. Same trade-off
    /// AISelfPreservation already documents.
    /// </summary>
    public class AIFleeToShrinePart : AIBehaviorPart
    {
        public override string Name => "AIFleeToShrine";

        /// <summary>
        /// HP fraction at or below which the part will push FleeLocationGoal
        /// on a bored tick. Default 0.4 matches AISelfPreservationPart's
        /// RetreatThreshold so both parts fire at similar HP and the
        /// blueprint-order priority applies.
        /// </summary>
        public float FleeThreshold = 0.4f;

        /// <summary>
        /// Turns the pushed FleeLocationGoal will chase its waypoint
        /// before giving up. Matches the plan spec (maxTurns: 50).
        /// </summary>
        public int MaxTurns = 50;

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
            if (brain == null || brain.CurrentZone == null)
                return true;

            // Idempotency — don't stack a second FleeLocationGoal if one
            // is already on the stack from this or any other source.
            // Uses the string variant to mirror AIWellVisitorPart /
            // AIGuardPart idiom.
            if (brain.HasGoal("FleeLocationGoal"))
                return true;

            // HP gate — only flee when wounded. GetStatValue returns the
            // computed Value (with Penalty applied), which is what the
            // player observes; matches BoredGoal.ShouldFlee semantics.
            int hp = ParentEntity.GetStatValue("Hitpoints", 0);
            int maxHp = ParentEntity.GetStat("Hitpoints")?.Max ?? 0;
            if (hp <= 0 || maxHp <= 0) return true;
            float fraction = (float)hp / maxHp;
            if (fraction > FleeThreshold) return true;

            // Scan for the nearest SanctuaryPart-bearing entity. If none
            // exists in the zone, degrade gracefully: do NOT consume the
            // event so AISelfPreservation (if present) can fall through
            // to its own retreat logic.
            Entity shrine = FindNearestSanctuary(brain.CurrentZone);
            if (shrine == null)
                return true;

            var shrineCell = brain.CurrentZone.GetEntityCell(shrine);
            if (shrineCell == null)
                return true;

            // endWhenNotFleeing: false — critical. By default, FleeLocationGoal
            // finishes as soon as HP is back above BrainPart.FleeThreshold (0.25).
            // AIFleeToShrinePart's FleeThreshold (0.8 on Scribe/Elder) is much
            // higher than BrainPart's, so with endWhenNotFleeing=true the goal
            // would pop immediately — the NPC is wounded by AIFleeToShrine's
            // standards but NOT by BrainPart.ShouldFlee()'s standards. Semantic:
            // shrine-seeking means "make the pilgrimage to sanctuary," not
            // "abort the moment you feel a little better." Let it run to
            // MaxTurns or until the shrine cell is reached.
            brain.PushGoal(new FleeLocationGoal(shrineCell.X, shrineCell.Y, MaxTurns, endWhenNotFleeing: false));
            return false; // consumed — suppress AISelfPreservation's fallback
        }

        private Entity FindNearestSanctuary(Zone zone)
        {
            var myCell = zone.GetEntityCell(ParentEntity);
            if (myCell == null) return null;

            Entity nearest = null;
            int nearestDist = int.MaxValue;

            // GetReadOnlyEntities is safe to iterate here: we only READ
            // part presence and cell positions, no zone mutations in this
            // loop. (Methodology Template §7.2 snapshot-discipline note.)
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity == ParentEntity) continue;
                if (entity.GetPart<SanctuaryPart>() == null) continue;

                var cell = zone.GetEntityCell(entity);
                if (cell == null) continue;

                int dist = AIHelpers.ChebyshevDistance(myCell.X, myCell.Y, cell.X, cell.Y);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = entity;
                }
            }

            return nearest;
        }
    }
}
