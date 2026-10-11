using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Bounded player-party combat signals. They select an existing KillGoal;
    /// they never execute an attack, pay energy, or retain a second target cache.
    /// Only current recruits actively following this player can join. Their
    /// ordinary goal owns observed pursuit, save state and eventual follow resume.
    /// </summary>
    public static class CompanionCombat
    {
        /// <summary>A real player melee attempt, after the attack veto, may
        /// rally visible recruits even when the committed swing misses.</summary>
        public static void AfterPlayerMelee(Entity player, Entity target, Zone zone)
        {
            if (player?.HasTag("Player") != true || !LiveActor(player, zone)
                || !LiveActor(target, zone) || BrainPart.ArePartyAligned(player, target)) return;
            Rally(player, target, player, zone);
        }

        /// <summary>A completed direct spell or weapon hit can request the
        /// same assistance. Call only at the direct action boundary: ordinary
        /// attributed poison, terrain and reflected damage are not orders.</summary>
        public static void AfterPlayerDirectDamage(Entity player, Entity target, Zone zone, int actualDamage)
        {
            if (actualDamage > 0) AfterPlayerMelee(player, target, zone);
        }

        /// <summary>Actual HP loss may prompt local defense of a living player
        /// or direct recruit against an existing hostile. This is not a general
        /// NPC retaliation/alert rule. Each responder must see both participants.</summary>
        public static void AfterHostileDamage(Entity victim, Entity source, Zone zone)
        {
            if (!LiveActor(victim, zone) || !LiveActor(source, zone)
                || BrainPart.ArePartyAligned(victim, source)
                || (!FactionManager.IsHostile(source, victim) && !FactionManager.IsHostile(victim, source))) return;
            var player = victim.HasTag("Player") ? victim : victim.GetEffect<RecruitedEffect>()?.Recruiter;
            if (player?.HasTag("Player") != true || !LiveActor(player, zone)) return;
            if (victim != player && !CurrentRecruit(victim, player, zone)) return;
            if (BrainPart.ArePartyAligned(player, source)) return;
            Rally(player, source, victim, zone);
        }

        private static void Rally(Entity player, Entity target, Entity witness, Zone zone)
        {
            var roster = player.GetPart<BrainPart>()?.PartyMembers;
            if (roster == null || roster.Count == 0) return;
            // Local party size only; snapshot protects goal dispatch from later
            // lifecycle changes without a zone scan or recursive party relay.
            foreach (var member in new List<Entity>(roster))
            {
                if (!CurrentRecruit(member, player, zone) || member == target || CompanionOrders.IsStaying(member)) continue;
                var brain = member.GetPart<BrainPart>();
                if (brain.InConversation || brain.HasGoal<NoFightGoal>()
                    || !(brain.PeekGoal() is FollowLeaderGoal follow) || follow.ParentBrain != brain
                    || follow.Leader != player || follow.Finished()
                    || FactionManager.IsHostile(member, player)
                    || FactionManager.IsHostile(player, member)) continue;
                if (!Seen(member, target, zone, brain.SightRadius)
                    || !Seen(member, witness, zone, brain.SightRadius)) continue;
                brain.PushGoal(new KillGoal(target));
            }
        }

        private static bool Seen(Entity observer, Entity target, Zone zone, int sight)
            => target.GetPart<RenderPart>()?.Visible != false
                && AIHelpers.TryGetVisibleTargetCell(observer, target, zone, sight, out _);

        private static bool CurrentRecruit(Entity member, Entity player, Zone zone)
        {
            if (!LiveActor(member, zone) || member.HasTag("Player")) return false;
            var brain = member.GetPart<BrainPart>(); var recruited = member.GetEffect<RecruitedEffect>();
            return brain?.ParentEntity == member && brain.CurrentZone == zone && brain.PartyLeader == player
                && recruited?.Owner == member && recruited.Recruiter == player && recruited.Duration != 0
                && player.GetPart<BrainPart>()?.PartyMembers?.Contains(member) == true;
        }

        private static bool LiveActor(Entity actor, Zone zone)
        {
            var body = actor?.GetPart<PhysicsPart>();
            return actor != null && zone != null && actor.HasTag("Creature")
                && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
                && actor.SpatialZone == zone && zone.GetEntityCell(actor) != null
                && body?.ParentEntity == actor && body.InInventory == null && body.Equipped == null;
        }
    }
}
