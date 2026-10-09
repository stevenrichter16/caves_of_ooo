using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Runs away from observed threat contacts, with CoO's bounded memory
    /// after losing sight. If cornered at a currently observed adjacent
    /// contact, fights back as a last resort.
    /// </summary>
    public class FleeGoal : GoalHandler
    {
        public Entity FleeFrom;
        public int MaxTurns;
        public const int MaximumUnseenActions = 6;
        // SaveGoal restores named public fields without constructors. Missing
        // legacy fields must mean no observed contact, never a default target.
        public bool HasLastSeen, RetreatingUnseen, Abandoned;
        public int LastSeenX, LastSeenY, UnseenRemaining;

        public FleeGoal(Entity fleeFrom, int maxTurns = 20)
        {
            FleeFrom = fleeFrom;
            MaxTurns = maxTurns;
        }

        public override bool CanFight() => false;

        public override bool Finished()
        {
            if (Abandoned) return true;
            if (FleeFrom == null) return true;
            if (CurrentZone?.GetEntityCell(FleeFrom) == null) return true;
            if (Age > MaxTurns) return true;
            if (!ShouldFlee()) return true;
            return false;
        }

        public override void OnPop()
        {
            // The goal cannot clear a newer target or cancel a paid strike.
            if (ParentBrain != null && ParentBrain.Target == FleeFrom
                && ParentEntity?.GetPart<CommittedMeleePart>()?.IsWindingUp != true)
                ParentBrain.Target = null;
        }

        public override string GetDetails()
        {
            string name = FleeFrom?.GetDisplayName() ?? "null";
            string details = $"from={name} | age={Age}/{MaxTurns}";
            return RetreatingUnseen
                ? details + $" | unseen=({LastSeenX},{LastSeenY}) | actions={UnseenRemaining}" : details;
        }

        public override void TakeAction()
        {
            // Brain checks age/health before incrementing age. Rechecking those
            // here would shorten the established maximum-age boundary by one.
            if (Abandoned || ParentEntity == null || CurrentZone == null
                || CurrentZone.GetEntityCell(ParentEntity) == null || FleeFrom == null
                || CurrentZone.GetEntityCell(FleeFrom) == null)
            { Abandon("target-unavailable"); return; }

            bool visible = AIHelpers.TryGetVisibleTargetCell(ParentEntity, FleeFrom, CurrentZone,
                ParentBrain.SightRadius, out var observed);
            if (visible)
            {
                bool reacquired = RetreatingUnseen;
                HasLastSeen = true; LastSeenX = observed.X; LastSeenY = observed.Y;
                UnseenRemaining = MaximumUnseenActions; RetreatingUnseen = false;
                if (reacquired) Announce("RetreatReacquired", "sees " + FleeFrom.GetDisplayName() + " again.");
            }
            else
            {
                if (!HasLastSeen || !CurrentZone.InBounds(LastSeenX, LastSeenY) || UnseenRemaining <= 0)
                { Abandon("no-observed-position"); return; }
                UnseenRemaining = Math.Min(MaximumUnseenActions, UnseenRemaining);
                if (!RetreatingUnseen)
                {
                    RetreatingUnseen = true;
                    Announce("RetreatLostSight", "loses sight of " + FleeFrom.GetDisplayName() + " and keeps retreating.");
                }
                UnseenRemaining--;
            }

            // BoredGoal can enter Flee directly on first hostile acquisition,
            // so retreat must share the same finite treatment decision as Kill.
            // A hidden treatment spends memory and this entire opportunity.
            if (ParentEntity.GetPart<TacticalSupplyPart>()?.TryUseEmergencyWater(FleeFrom, CurrentZone) == true
                || ParentEntity.GetPart<FieldMedicinePart>()?.TryUseMedicine(FleeFrom, CurrentZone, Rng) == true)
            {
                FinishUnseenOpportunity(visible);
                return;
            }

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);

            if (!AIHelpers.TryStepAway(ParentEntity, CurrentZone, myPos.x, myPos.y, LastSeenX, LastSeenY))
            {
                // Remembered adjacency is not permission to attack a hidden
                // body that has since moved. Real adjacent mist contact is seen.
                if (visible && SpatialQuery.Distance(CurrentZone, ParentEntity, FleeFrom) == 1)
                {
                    Think($"cornered by {FleeFrom?.GetDisplayName()}, fighting back");
                    CombatSystem.PerformMeleeAttack(ParentEntity, FleeFrom, CurrentZone, Rng);
                }
            }
            else
            {
                Think($"fleeing from {FleeFrom?.GetDisplayName()}");
            }
            FinishUnseenOpportunity(visible);
        }

        private void FinishUnseenOpportunity(bool visible)
        {
            if (!visible && UnseenRemaining <= 0) Abandon("memory-spent");
        }

        private void Abandon(string reason)
        {
            if (Abandoned) return;
            Abandoned = true;
            if (ParentBrain != null && ParentBrain.Target == FleeFrom
                && ParentEntity?.GetPart<CommittedMeleePart>()?.IsWindingUp != true)
            {
                ParentBrain.Target = null;
                ParentBrain.CurrentState = AIState.Idle;
            }
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", "RetreatAbandoned", ParentEntity, FleeFrom,
                    new { reason, hasLastSeen = HasLastSeen, lastSeenX = LastSeenX, lastSeenY = LastSeenY,
                        remaining = UnseenRemaining });
            Think("retreat ended: " + reason);
        }

        private void Announce(string kind, string text)
        {
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", kind, ParentEntity, FleeFrom,
                    new { lastSeenX = LastSeenX, lastSeenY = LastSeenY, remaining = UnseenRemaining });
            Think(text);
            MessageLog.AddObserved(ParentEntity, ParentEntity.GetDisplayName() + " " + text);
        }
    }
}
