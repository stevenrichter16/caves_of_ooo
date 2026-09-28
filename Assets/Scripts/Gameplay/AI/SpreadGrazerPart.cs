using System;

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
