using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Saved travel orders for an exact recruited companion. Orders
    /// use the ordinary world-action surface and cost no world turn, like its
    /// other conversational commands. Existing unrelated active goals are not
    /// cancelled; stay prevents following, new party assistance and transit.</summary>
    public static class CompanionOrders
    {
        public const string StayCommand = "CompanionStay";
        public const string FollowCommand = "CompanionFollow";

        /// <summary>True only while the saved order belongs to this companion's
        /// current recruitment. Works across zones so leave/return honors stay.</summary>
        public static bool IsStaying(Entity follower)
        {
            var effect = follower?.GetEffect<RecruitedEffect>();
            var brain = follower?.GetPart<BrainPart>();
            return effect?.StayHere == true && effect.Duration != 0 && effect.Owner == follower
                && brain?.ParentEntity == follower && effect.Recruiter != null
                && brain.PartyLeader == effect.Recruiter
                && effect.Recruiter.GetPart<BrainPart>()?.PartyMembers.Contains(follower) == true;
        }

        internal static bool HandleEvent(Entity follower, GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (Eligible(actor, follower, follower?.SpatialZone, out var effect))
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction(
                        effect.StayHere ? "FollowMe" : "StayHere",
                        effect.StayHere ? "follow me" : "stay here",
                        effect.StayHere ? FollowCommand : StayCommand, effect.StayHere ? 'f' : 's', 25);
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            if (command != StayCommand && command != FollowCommand) return true;
            var leader = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone");
            var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if (!TryOrder(leader, follower, zone, command == StayCommand, transaction)) return true;
            e.Handled = true;
            return false;
        }

        private static bool TryOrder(Entity leader, Entity follower, Zone zone, bool stay, InventoryTransaction transaction)
        {
            if (!Eligible(leader, follower, zone, out var effect)) return Reject(leader, follower, "not-current-adjacent-recruit");
            if (effect.StayHere == stay) return Reject(leader, follower, "already-ordered");
            bool ownsTransaction = transaction == null;
            transaction ??= new InventoryTransaction();
            if (!transaction.TryClaim(follower, leader, stay ? StayCommand : FollowCommand)) return false;
            bool previous = effect.StayHere;
            var brain = follower.GetPart<BrainPart>();
            var goal = MatchingFollow(brain, leader);
            int previousAge = goal?.Age ?? 0;
            transaction.Do(() => { effect.StayHere = stay; if (goal != null) goal.Age = 0; },
                () => { effect.StayHere = previous; if (goal != null) goal.Age = previousAge; });
            transaction.BeforeCommit(() => Eligible(leader, follower, zone, out var current)
                && ReferenceEquals(current, effect) && effect.StayHere == stay
                && ReferenceEquals(follower.GetPart<BrainPart>(), brain)
                && ReferenceEquals(MatchingFollow(brain, leader), goal));
            transaction.AfterCommit(() =>
            {
                // A timed-out follower may have no follow goal. Restoring one
                // also gives a newly ordered stay its persistent idle owner.
                if (MatchingFollow(brain, leader) == null)
                    brain.PushFollowGoal(new FollowLeaderGoal(leader));
                MessageLog.Add(follower.GetDisplayName() + (stay ? " will stay here." : " will follow you."));
                if (Diag.IsChannelEnabled("ai")) Diag.Record("ai", "CompanionOrderChanged", leader, follower,
                    payload: new { order = stay ? "stay" : "follow" });
            });
            if (ownsTransaction)
            {
                try { transaction.Commit(); }
                catch { transaction.Rollback(); return Reject(leader, follower, "order-changed-before-commit"); }
            }
            return true;
        }

        private static FollowLeaderGoal MatchingFollow(BrainPart brain, Entity leader)
        {
            for (int i = 0; i < brain.GoalCount; i++)
                if (brain.PeekGoalAt(i) is FollowLeaderGoal goal && goal.ParentBrain == brain && goal.Leader == leader) return goal;
            return null;
        }

        private static bool Eligible(Entity leader, Entity follower, Zone zone, out RecruitedEffect effect)
        {
            effect = follower?.GetEffect<RecruitedEffect>();
            var brain = follower?.GetPart<BrainPart>();
            return leader != null && follower != null && leader != follower && zone != null
                && effect?.Owner == follower && effect.Duration != 0 && effect.Recruiter == leader
                && brain?.ParentEntity == follower && brain.PartyLeader == leader && brain.CurrentZone == zone
                && leader.GetPart<BrainPart>()?.PartyMembers.Contains(follower) == true
                && Live(leader, zone) && Live(follower, zone)
                && SpatialQuery.Distance(zone, leader, follower) <= 1;
        }
        private static bool Live(Entity actor, Zone zone)
        {
            var body = actor.GetPart<PhysicsPart>();
            return actor.HasTag("Creature") && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
                && actor.SpatialZone == zone && zone.GetEntityCell(actor) != null
                && body?.ParentEntity == actor && body.InInventory == null && body.Equipped == null;
        }
        private static bool Reject(Entity leader, Entity follower, string reason)
        {
            Diag.Record("ai", "CompanionOrderRejected", leader, follower, payload: new { reason });
            return false;
        }
    }
}
