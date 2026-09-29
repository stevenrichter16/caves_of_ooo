using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>One finite feed from two explicitly reserved field owners.
    /// Healthy proximity flight is separate from the health-based FleeGoal.</summary>
    public sealed class SpreadGrazerPart : Part
    {
        public override string Name => "SpreadGrazer";
        public bool Configured, Fed;
        public string ZoneID;
        public Entity Food, ReservedRow;
        public int FoodX, FoodY, ReservedX, ReservedY;
        public int ApproachAttempts;

        // Separate saved hunt state: finite-food fields above retain their ownership.
        public Entity Hunter;
        public string HuntZoneID;
        public int FlightRemaining, ThreatX, ThreatY;
        const int HuntFlightMemory = 3, EscapeLookahead = 4, HiddenEscapeScore = 10000;
        // Value-only scratch; saved hunt state above remains authoritative. The
        // read-only search finishes and clears both buffers before movement can
        // notify subscribers (including a reentrant action on this actor).
        [NonSerialized] private Queue<(int x,int y,int depth,int dx,int dy)> _flightPending;
        [NonSerialized] private HashSet<(int,int)> _flightSeen;
        bool TryHuntFlight(BrainPart brain, Zone zone)
        {
            var hunter = Hunter?.GetPart<SpreadPredatorPart>();
            if (hunter == null || !hunter.IsCurrentPair(this, zone))
            { Hunter = null; FlightRemaining = 0; return false; }
            // Saved memory is local and finite. Reject malformed coordinates
            // before passing them to LOS/path helpers; never extend this flight.
            if (FlightRemaining < 0 || FlightRemaining > HuntFlightMemory
                || (FlightRemaining > 0 && !zone.InBounds(ThreatX, ThreatY)))
            { FlightRemaining = 0; return true; }
            var threat = SpreadActorContext.Nearest(ParentEntity, zone, 3,
                e => !BrainPart.ArePartyAligned(ParentEntity, e)
                    && (e == Hunter || e.HasTag("Player") || FactionManager.IsHostile(ParentEntity, e)));
            if (threat != null)
            {
                var from = zone.GetEntityCell(threat); ThreatX = from.X; ThreatY = from.Y; FlightRemaining = HuntFlightMemory;
            }
            if (FlightRemaining <= 0) return false;
            FlightRemaining--; brain.CurrentState = AIState.Wander;
            var here = zone.GetEntityCell(ParentEntity);
            int distance = AIHelpers.ChebyshevDistance(here.X, here.Y, ThreatX, ThreatY);
            var pending = _flightPending ?? (_flightPending = new Queue<(int x,int y,int depth,int dx,int dy)>((EscapeLookahead*2+1)*(EscapeLookahead*2+1)));
            var seen = _flightSeen ?? (_flightSeen = new HashSet<(int,int)>((EscapeLookahead*2+1)*(EscapeLookahead*2+1)));
            pending.Clear(); seen.Clear(); seen.Add((here.X,here.Y));
            pending.Enqueue((here.X,here.Y,0,0,0));
            int bestScore = int.MinValue, bestX = 0, bestY = 0;
            while (pending.Count > 0)
            {
                var c = pending.Dequeue(); if (c.depth >= EscapeLookahead) continue;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = c.x + dx, y = c.y + dy;
                    if (!zone.InBounds(x,y) || x < 1 || y < 1 || x >= Zone.Width-1 || y >= Zone.Height-1
                        || seen.Contains((x,y)) || zone.GetCell(x,y).BlocksMovement(ParentEntity)) continue;
                    int away = AIHelpers.ChebyshevDistance(x,y,ThreatX,ThreatY);
                    if (away < distance) continue;
                    seen.Add((x,y)); int firstX = c.depth == 0 ? dx : c.dx, firstY = c.depth == 0 ? dy : c.dy;
                    pending.Enqueue((x,y,c.depth+1,firstX,firstY));
                    bool hidden = !AIHelpers.HasLineOfSight(zone,ThreatX,ThreatY,x,y);
                    int score = (hidden ? HiddenEscapeScore : 0) + away*100 - c.depth-1;
                    if ((hidden || away > distance) && score > bestScore)
                    { bestScore = score; bestX = firstX; bestY = firstY; }
                }
            }
            pending.Clear(); seen.Clear();
            if (bestScore != int.MinValue) MovementSystem.TryMove(ParentEntity,zone,bestX,bestY);
            return true;
        }

        /// <summary>Only an ordinary pacing step may yield to this exact pair's
        /// currently visible nearby hunter. The owning wander/effect remains on
        /// its stack and keeps its normal progress; work, Calm and combat win.</summary>
        internal bool TryWanderFlight(BrainPart brain, Zone zone, WanderRandomlyGoal step)
        {
            var hunter = Hunter?.GetPart<SpreadPredatorPart>();
            if (brain == null || brain.ParentEntity != ParentEntity || brain.InConversation
                || ParentEntity?.GetPart<SpreadGrazerPart>() != this
                || step?.ParentBrain != brain || brain.PeekGoal() != step
                || hunter == null || !hunter.IsCurrentPair(this, zone)) return false;
            int goalCount = brain.GoalCount;
            for (int i = 0; i < goalCount; i++)
            {
                var goal = brain.PeekGoalAt(i);
                var type = goal.GetType();
                if (type != typeof(BoredGoal) && type != typeof(WanderDurationGoal)
                    && type != typeof(WanderRandomlyGoal)) return false;
                if (goal.ParentHandler != null)
                {
                    bool parentPresent = false;
                    for (int j = 0; j < goalCount; j++)
                        if (brain.PeekGoalAt(j) == goal.ParentHandler) { parentPresent = true; break; }
                    if (!parentPresent) return false;
                }
            }
            var at = zone.GetEntityCell(ParentEntity); var threat = zone.GetEntityCell(Hunter);
            if (AIHelpers.ChebyshevDistance(at.X, at.Y, threat.X, threat.Y) > 3
                || !AIHelpers.HasLineOfSight(zone, at.X, at.Y, threat.X, threat.Y)) return false;
            return TryHuntFlight(brain, zone);
        }

        public bool ConfigureForage(Zone zone, Entity targetRow, Entity reservedRow)
        {
            if (Configured || Fed || ParentEntity == null || ParentEntity.GetPart<SpreadGrazerPart>() != this
                || !SpreadActorContext.Actor(ParentEntity, zone, out _) || targetRow == reservedRow
                || !Ripe(targetRow, zone) || !Ripe(reservedRow, zone)) return false;
            var food = zone.GetEntityCell(targetRow); var reserve = zone.GetEntityCell(reservedRow);
            var at = zone.GetEntityCell(ParentEntity);
            // The reserved row is player supply, not an animal travel destination.
            if (AIHelpers.ChebyshevDistance(at.X, at.Y, food.X, food.Y) > 12) return false;
            Food = targetRow; ReservedRow = reservedRow; FoodX = food.X; FoodY = food.Y;
            ReservedX = reserve.X; ReservedY = reserve.Y; ZoneID = zone.ZoneID;
            ApproachAttempts = 0; Configured = true; return true;
        }

        internal bool TakeIdleAction(BrainPart brain, Zone zone)
        {
            if (!SpreadActorContext.Actor(ParentEntity, zone, out var actual) || actual != brain
                || ParentEntity.GetPart<SpreadGrazerPart>() != this) return true;
            if (brain.PartyLeader != null) return false;
            if (TryHuntFlight(brain, zone)) return true;
            var threat = SpreadActorContext.Nearest(ParentEntity, zone, 3,
                e => !BrainPart.ArePartyAligned(ParentEntity, e)
                    && (e.HasTag("Player") || FactionManager.IsHostile(ParentEntity, e)));
            if (threat != null)
            {
                var at = zone.GetEntityCell(ParentEntity); var from = zone.GetEntityCell(threat);
                brain.CurrentState = AIState.Wander;
                AIHelpers.TryStepAway(ParentEntity, zone, at.X, at.Y, from.X, from.Y);
                return true;
            }
            brain.CurrentState = AIState.Idle;
            if (!CurrentFood(zone) || ApproachAttempts >= 24) return true;
            var here = zone.GetEntityCell(ParentEntity);
            if (AIHelpers.ChebyshevDistance(here.X, here.Y, FoodX, FoodY) <= 1)
            {
                // No callbacks between validation and these two saved mutations.
                var actor=ParentEntity; var meal=Food; var reserve=ReservedRow;
                var field=meal.GetPart<FieldHarvestPart>(); var reserveField=reserve.GetPart<FieldHarvestPart>();
                var mealCell=zone.GetEntityCell(meal); var reserveCell=zone.GetEntityCell(reserve);
                Fed = true;
                field.ConsumeByGrazer();
                // Dirty-view subscribers run after consumption. Revalidate before
                // publishing the gesture; never turn it into another game action.
                if(ParentEntity==actor && actor.GetPart<SpreadGrazerPart>()==this && Configured && Fed
                    && Food==meal && ReservedRow==reserve && ZoneID==zone.ZoneID
                    && SpreadActorContext.Actor(actor,zone,out var currentBrain) && currentBrain==brain
                    && zone.GetEntityCell(actor)==here && SpreadActorContext.Ground(meal,zone)
                    && zone.GetEntityCell(meal)==mealCell && mealCell.X==FoodX && mealCell.Y==FoodY
                    && meal.BlueprintName=="RipeCropRow" && meal.GetPart<FieldHarvestPart>()==field && field.ParentEntity==meal && field.Harvested
                    && Ripe(reserve,zone) && reserve.GetPart<FieldHarvestPart>()==reserveField
                    && zone.GetEntityCell(reserve)==reserveCell && reserveCell.X==ReservedX && reserveCell.Y==ReservedY)
                    EntityVisualHooks.EmitInteraction(actor,meal,zone);
            }
            else
            {
                ApproachAttempts++;
                AIHelpers.TryApproachWithPathfinding(ParentEntity, zone, here.X, here.Y, FoodX, FoodY);
            }
            return true;
        }
        bool CurrentFood(Zone zone)
        {
            if (!Configured || Fed || ZoneID != zone.ZoneID || Food == ReservedRow
                || !Ripe(Food, zone) || !Ripe(ReservedRow, zone)) return false;
            var food = zone.GetEntityCell(Food); var reserve = zone.GetEntityCell(ReservedRow);
            return food.X == FoodX && food.Y == FoodY && reserve.X == ReservedX && reserve.Y == ReservedY;
        }
        static bool Ripe(Entity row, Zone zone)
        {
            if (!SpreadActorContext.Ground(row, zone) || row.BlueprintName != "RipeCropRow") return false;
            var field = row.GetPart<FieldHarvestPart>();
            var render = row.GetPart<RenderPart>(); var examine = row.GetPart<ExaminablePart>();
            return field != null && field.ParentEntity == row && !field.Harvested
                && render != null && render.ParentEntity == row && examine != null && examine.ParentEntity == row;
        }
    }
}
