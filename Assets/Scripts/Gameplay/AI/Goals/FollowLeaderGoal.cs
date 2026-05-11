namespace CavesOfOoo.Core
{
    /// <summary>
    /// Goal that keeps a party member close to their leader.
    /// Pushed onto a follower's goal stack when the follower should
    /// move toward its <see cref="BrainPart.PartyLeader"/>.
    ///
    /// <para><b>Behavior:</b> each <see cref="TakeAction"/>, if the
    /// follower is farther than <see cref="CloseEnoughDistance"/>
    /// (Chebyshev) from the leader, take one step toward the
    /// leader's current cell via the greedy step path
    /// (<see cref="AIHelpers.TryStepToward"/>). Greedy is the right
    /// primitive for a moving target — A* would need to re-plan
    /// every tick since the leader can move.</para>
    ///
    /// <para><b>Termination cases (<see cref="Finished"/>):</b></para>
    /// <list type="bullet">
    ///   <item>Leader is null.</item>
    ///   <item>Leader has no <see cref="BrainPart"/> (no reachable zone).</item>
    ///   <item>Leader's <see cref="BrainPart.CurrentZone"/> is null or
    ///         differs from the follower's zone — cross-zone pursuit is
    ///         <b>out of scope for F.1</b>; deferred to F.4+.</item>
    ///   <item><see cref="GoalHandler.Age"/> exceeds
    ///         <see cref="MaxAgeBeforeGiveUp"/> (defensive timeout).
    ///         <see cref="TakeAction"/> resets Age to 0 while close-enough,
    ///         so the timeout only fires when the follower has been
    ///         continuously failing to reach the leader.</item>
    /// </list>
    ///
    /// <para><b>Persistent semantics (F.2.6 fix):</b> the goal does NOT
    /// finish when the follower is within
    /// <see cref="CloseEnoughDistance"/>. Following is ongoing, not
    /// one-shot — when the leader walks away the follower needs to
    /// re-pursue. Early F.1.5 design finished on close-enough; that
    /// broke recruit-from-adjacent because the goal popped on tick 1
    /// (recruiter is by definition adjacent at recruit time → distance
    /// 1 ≤ default 2 → Finished). When close, <see cref="TakeAction"/>
    /// idles (no-op) and resets Age; when far, it steps toward.</para>
    ///
    /// <para><b>Qud parity:</b> Qud doesn't ship a dedicated
    /// FollowLeaderGoal — it composes the same observable behavior from
    /// PartyMember status feeding the Wander/Move goals. CoO factors
    /// it into a dedicated goal for clarity and to make the contract
    /// pinnable in tests. See <c>Docs/FOLLOWERS.md §F.1.5</c>.</para>
    ///
    /// <para><b>Persistence:</b> public fields only, so
    /// <see cref="XRL.SaveSystem.SaveGoal"/>'s reflective
    /// <c>WritePublicFields</c> serializes them automatically. Pinned by
    /// <c>FollowLeaderGoalTests.Goal_RoundTrips_With_LeaderReference</c>.</para>
    /// </summary>
    public class FollowLeaderGoal : GoalHandler
    {
        /// <summary>The leader entity this follower is tracking.</summary>
        public Entity Leader;

        /// <summary>
        /// Chebyshev distance at or under which the goal is satisfied.
        /// Default 2 — close enough to participate in the leader's
        /// adjacent-cell combat but not so close as to require occupying
        /// the leader's exact cell (which is impossible while leader is alive).
        /// </summary>
        public int CloseEnoughDistance = 2;

        /// <summary>
        /// Defensive timeout. If the follower can't reach the leader
        /// within this many ticks, give up. Prevents pathological
        /// "unreachable leader" states from spinning forever.
        /// </summary>
        public int MaxAgeBeforeGiveUp = 200;

        public FollowLeaderGoal() { }

        public FollowLeaderGoal(Entity leader)
        {
            Leader = leader;
        }

        public override string GetDetails()
        {
            if (Leader == null) return "leader=null";
            return $"leader={Leader.BlueprintName} age={Age}/{MaxAgeBeforeGiveUp}";
        }

        public override bool Finished()
        {
            if (Leader == null) return true;
            if (Age > MaxAgeBeforeGiveUp) return true;

            var leaderBrain = Leader.GetPart<BrainPart>();
            if (leaderBrain == null) return true;

            var leaderZone = leaderBrain.CurrentZone;
            if (leaderZone == null) return true;

            // Cross-zone is out of scope for F.1 — give up if zones
            // mismatch. F.4+ may add a zone-transition pursuit step.
            if (leaderZone != CurrentZone) return true;

            // Follow is persistent — do NOT finish on close-enough.
            // TakeAction idles when close (and resets Age) so the goal
            // stays on the stack and re-pursues when the leader moves.
            // See class docstring's "Persistent semantics" section for
            // why this changed from the original F.1.5 design.
            return false;
        }

        public override void TakeAction()
        {
            // Defensive: Finished() is the canonical fail path, but
            // TakeAction must not NRE if it's called on a finished
            // goal (race conditions, manual invocation in tests).
            if (Leader == null) return;
            var leaderBrain = Leader.GetPart<BrainPart>();
            if (leaderBrain == null) return;
            var leaderZone = leaderBrain.CurrentZone;
            if (leaderZone == null || leaderZone != CurrentZone) return;

            if (WithinCloseEnoughDistance())
            {
                // Idle when close enough. Reset Age so the
                // MaxAgeBeforeGiveUp timeout only fires when the
                // follower has been continuously failing to reach the
                // leader (e.g. pathologically unreachable), not after
                // 200 turns of happy following.
                Age = 0;
                return;
            }

            // Look up positions fresh each tick — the leader can move.
            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            var leaderPos = leaderZone.GetEntityPosition(Leader);
            if (myPos.x < 0 || leaderPos.x < 0) return;

            // Greedy step toward leader. Handles thin obstacles via
            // diagonal/cardinal fallback internally. For wall-routed
            // pursuit (around buildings) we'd need A* — but the
            // typical follow scenario is open ground, so greedy is
            // the right primitive AND cheaper per tick.
            AIHelpers.TryStepToward(
                ParentEntity, CurrentZone,
                myPos.x, myPos.y,
                leaderPos.x, leaderPos.y);
        }

        /// <summary>
        /// Chebyshev distance check. Returns true if the follower is
        /// already within <see cref="CloseEnoughDistance"/> of the leader.
        /// Null-safe — returns false on missing positions.
        /// </summary>
        private bool WithinCloseEnoughDistance()
        {
            if (CurrentZone == null) return false;
            var leaderBrain = Leader?.GetPart<BrainPart>();
            var leaderZone = leaderBrain?.CurrentZone;
            if (leaderZone == null) return false;
            var myPos = CurrentZone.GetEntityPosition(ParentEntity);
            var leaderPos = leaderZone.GetEntityPosition(Leader);
            if (myPos.x < 0 || leaderPos.x < 0) return false;

            int dx = System.Math.Abs(myPos.x - leaderPos.x);
            int dy = System.Math.Abs(myPos.y - leaderPos.y);
            int chebyshev = dx > dy ? dx : dy;
            return chebyshev <= CloseEnoughDistance;
        }
    }
}
