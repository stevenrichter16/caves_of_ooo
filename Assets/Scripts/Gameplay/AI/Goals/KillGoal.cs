using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that tracks and attacks a hostile target.
    /// Uses the goal stack with CoO's bounded perception/search policy.
    /// Tracks observed positions only. After losing sight, spends a finite
    /// search at the last observed cell, then leaves combat without forgiving
    /// faction/personal hostility. Memory fields round-trip through SaveGoal.
    ///
    /// Approach strategy:
    /// - Authored tactics → one safe supported ability, when selected
    /// - Adjacent fallback → melee attack
    /// - Not adjacent → try ranged ability, else walk toward target
    /// - Walking uses TryApproachWithPathfinding: greedy-first with A* fallback
    ///   when blocked by walls, so creatures navigate around obstacles to reach
    ///   moving targets (no more marlbacks stuck on building walls).
    /// </summary>
    public class KillGoal : GoalHandler
    {
        public Entity Target;
        public const int MaximumSearchActions = 6;
        // Mutable public fields are saved by SaveGoal. LoadGoal bypasses all
        // constructors: the legacy all-zero state MUST mean no prior knowledge.
        public bool HasLastSeen, Searching, Abandoned;
        public int LastSeenX, LastSeenY, SearchRemaining;

        public KillGoal(Entity target)
        {
            Target = target;
        }

        public override bool Finished()
        {
            return Abandoned || Target == null || Target == ParentEntity
                || CurrentZone?.GetEntityCell(Target) == null
                || Target.GetStatValue("Hitpoints", 1) <= 0 || CombatSystem.IsDeathHandled(Target);
        }

        public override void OnPop()
        {
            // Clearing/replacing goals cannot cancel an already-paid fixed-ray
            // strike. The commitment owns that action independently of this goal.
            if (ParentBrain != null && ParentBrain.Target == Target
                && ParentEntity?.GetPart<CommittedMeleePart>()?.IsWindingUp != true)
                ParentBrain.Target = null;
        }

        public override string GetDetails()
        {
            if (Target == null) return null;
            string name = Target.GetDisplayName();
            if (string.IsNullOrEmpty(name)) return null;
            return Searching ? $"target={name} | searching=({LastSeenX},{LastSeenY}) | actions={SearchRemaining}"
                : $"target={name}";
        }

        public override void TakeAction()
        {
            if (Finished() || ParentEntity == null || CurrentZone.GetEntityCell(ParentEntity) == null)
            { Abandon("target-unavailable"); return; }

            bool visible = AIHelpers.TryGetVisibleTargetCell(ParentEntity, Target, CurrentZone,
                ParentBrain.SightRadius, out var observed);
            if (visible)
            {
                bool reacquired = Searching;
                HasLastSeen = true; LastSeenX = observed.X; LastSeenY = observed.Y;
                SearchRemaining = MaximumSearchActions; Searching = false;
                if (reacquired) Announce("PursuitReacquired", "sees " + Target.GetDisplayName() + " again.");
            }
            else
            {
                if (!HasLastSeen || !CurrentZone.InBounds(LastSeenX, LastSeenY) || SearchRemaining <= 0)
                { Abandon("no-observed-position"); return; }
                // A corrupt/older supplied budget cannot create endless search.
                SearchRemaining = Math.Min(MaximumSearchActions, SearchRemaining);
                if (!Searching)
                {
                    Searching = true;
                    Announce("PursuitLostSight", "loses sight of " + Target.GetDisplayName() + " and searches its last position.");
                }
                SearchRemaining--;
            }

            ParentBrain.CurrentState = AIState.Chase;
            ParentBrain.Target = Target;

            var myPos = CurrentZone.GetEntityPosition(ParentEntity);

            // A finite carried treatment replaces this whole action, including
            // low-health retreat. Hidden self-treatment/recovery still spends a
            // search opportunity; neither action reads the target's coordinates.
            bool usedAction = ParentEntity.GetPart<TacticalSupplyPart>()?.TryUseEmergencyWater(Target, CurrentZone) == true
                || ParentEntity.GetPart<FieldMedicinePart>()?.TryUseMedicine(Target, CurrentZone, Rng) == true
                || ParentEntity.GetPart<WeaponRecoveryPart>()?.TryRecover(CurrentZone) == true;
            if (usedAction)
            {
                if (!visible && SearchRemaining <= 0) Abandon("search-spent");
                return;
            }

            // Keep the established treatment-before-retreat priority even when
            // the threat has just disappeared. A wounded observer must not
            // chase toward a remembered enemy merely because it lost sight.
            if (ShouldFlee())
            {
                Think("low hp, breaking off attack");
                Abandon("low-health-retreat");
                FailToParent();
                return;
            }
            if (!visible)
            {
                if (myPos.x != LastSeenX || myPos.y != LastSeenY)
                    AIHelpers.TryApproachWithPathfinding(ParentEntity, CurrentZone, myPos.x, myPos.y, LastSeenX, LastSeenY);
                if (SearchRemaining <= 0) Abandon("search-spent");
                return;
            }
            var targetPos = CurrentZone.GetEntityPosition(Target);

            // Only opted-in actors replace immediate melee with a commitment.
            // A refused adjacent start cannot fall through into an instant swing.
            var commitment = ParentEntity.GetPart<CommittedMeleePart>();
            if (commitment != null && commitment.TryBegin(Target, CurrentZone)) return;

            var tactics = ParentEntity.GetPart<CombatTacticsPart>();
            if (tactics != null && tactics.TryUseAbility(Target, CurrentZone, Rng)) return;
            if (tactics != null && tactics.TryPositionForShot(Target, CurrentZone)) return;

            if (SpatialQuery.Distance(CurrentZone,ParentEntity,Target) == 1)
            {
                if (commitment != null) return;
                Think($"attacking {Target.GetDisplayName()}");
                CombatSystem.PerformMeleeAttack(ParentEntity, Target, CurrentZone, Rng);
            }
            else
            {
                if (tactics != null || !AIHelpers.TryUseRangedAbility(ParentEntity, CurrentZone, Rng, myPos, targetPos))
                {
                    Think($"closing on {Target.GetDisplayName()}");
                    if (ParentEntity.HasPart<SpatialFootprintPart>() || Target.HasPart<SpatialFootprintPart>())
                    {
                        var path=FindPath.ToContact(CurrentZone,ParentEntity,Target);
                        if(path.Usable && path.Steps.Count>0)
                            MovementSystem.TryMove(ParentEntity,CurrentZone,path.Steps[0].dx,path.Steps[0].dy);
                    }
                    else AIHelpers.TryApproachWithPathfinding(ParentEntity, CurrentZone, myPos.x, myPos.y, targetPos.x, targetPos.y);
                }
            }
        }

        private void Abandon(string reason)
        {
            if (Abandoned) return;
            Abandoned = true;
            if (ParentBrain != null && ParentBrain.Target == Target) ParentBrain.Target = null;
            if (ParentBrain != null) ParentBrain.CurrentState = AIState.Idle;
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", "PursuitAbandoned", ParentEntity, Target,
                    new { reason, hasLastSeen = HasLastSeen, lastSeenX = LastSeenX, lastSeenY = LastSeenY });
            Think("search ended: " + reason);
        }

        private void Announce(string kind, string text)
        {
            if (Diag.IsChannelEnabled("ai"))
                Diag.Record("ai", kind, ParentEntity, Target,
                    new { lastSeenX = LastSeenX, lastSeenY = LastSeenY, remaining = SearchRemaining });
            Think(text);
            if (CurrentZone?.GetEntityCell(ParentEntity)?.IsVisible == true
                && ParentEntity.GetPart<RenderPart>()?.Visible != false)
                MessageLog.Add(ParentEntity.GetDisplayName() + " " + text);
        }
    }
}
