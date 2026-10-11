using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CompanionCombatAdversarialTests
    {
        [SetUp] public void SetUp() { FactionManager.Initialize(); MessageLog.Clear(); }
        [TestCase("roster")] [TestCase("recruit-effect")] [TestCase("expired-effect")]
        [TestCase("effect-owner")] [TestCase("effect-leader")] [TestCase("brain-leader")]
        [TestCase("foreign-zone")] [TestCase("foreign-brain-zone")] [TestCase("dead-member")]
        [TestCase("carried-member")] [TestCase("missing-physics")] [TestCase("conversation")]
        [TestCase("peace")] [TestCase("wrong-follow-leader")] [TestCase("wrong-follow-owner")]
        [TestCase("expired-follow")] [TestCase("busy-goal")] [TestCase("hostile-member")]
        [TestCase("nonplayer-leader")] [TestCase("dead-target")] [TestCase("carried-target")]
        [TestCase("hidden-target")] [TestCase("hidden-leader")] [TestCase("sight-radius")]
        [TestCase("blocked-sight")]
        public void IneligibleRecruitOrIntentNeverJoinsButEquivalentValidRecruitDoes(string changed)
        {
            var f = new CompanionCombatTests.Fixture(); var brain = f.Brain;
            var effect = f.Follower.GetEffect<RecruitedEffect>(); var follow = brain.FindGoal<FollowLeaderGoal>();
            switch (changed)
            {
                case "roster": f.Player.GetPart<BrainPart>().PartyMembers.Remove(f.Follower); break;
                case "recruit-effect": f.Follower.RemoveEffect<RecruitedEffect>(); brain.PushGoal(follow); break;
                case "expired-effect": effect.Duration = 0; break;
                case "effect-owner": effect.Owner = f.Player; break;
                case "effect-leader": effect.Recruiter = f.Enemy; break;
                case "brain-leader": brain.PartyLeader = f.Enemy; break;
                case "foreign-zone":
                    f.Zone.RemoveEntity(f.Follower); var foreign = new Zone("foreign"); foreign.AddEntity(f.Follower, 5, 6); brain.CurrentZone = foreign; break;
                case "foreign-brain-zone": brain.CurrentZone = new Zone(f.Zone.ZoneID); break;
                case "dead-member": f.Follower.GetStat("Hitpoints").BaseValue = 0; break;
                case "carried-member": f.Follower.GetPart<PhysicsPart>().InInventory = f.Player; break;
                case "missing-physics": f.Follower.RemovePart(f.Follower.GetPart<PhysicsPart>()); break;
                case "conversation": brain.InConversation = true; break;
                case "peace": brain.PushGoal(new NoFightGoal(3)); break;
                case "wrong-follow-leader": follow.Leader = f.Enemy; break;
                case "wrong-follow-owner": follow.ParentBrain = f.Player.GetPart<BrainPart>(); break;
                case "expired-follow": follow.Age = follow.MaxAgeBeforeGiveUp + 1; break;
                case "busy-goal": brain.PushGoal(new WaitGoal(5)); break;
                case "hostile-member": brain.SetPersonallyHostile(f.Player, alertAllies: false); break;
                case "nonplayer-leader": f.Player.Tags.Remove("Player"); break;
                case "dead-target": f.Enemy.GetStat("Hitpoints").BaseValue = 0; break;
                case "carried-target": f.Enemy.GetPart<PhysicsPart>().InInventory = f.Player; break;
                case "hidden-target": f.Enemy.GetPart<RenderPart>().Visible = false; break;
                case "hidden-leader": f.Player.GetPart<RenderPart>().Visible = false; break;
                case "sight-radius": brain.SightRadius = 0; break;
                case "blocked-sight":
                    f.Zone.MoveEntity(f.Follower, 2, 5);
                    var wall = new Entity { ID = "wall" }; wall.SetTag("Solid"); wall.AddPart(new PhysicsPart { Solid = true }); f.Zone.AddEntity(wall, 4, 5); break;
            }
            f.Attack(); Assert.Null(CompanionCombatTests.Fight(f.Follower), changed);
            var control = new CompanionCombatTests.Fixture(); control.Attack();
            Assert.NotNull(CompanionCombatTests.Fight(control.Follower), "Identical eligible setup must still join: " + changed);
        }
        [Test] public void PlayerAttackingPartyMemberDoesNotRallyOtherMembers()
        {
            var f = new CompanionCombatTests.Fixture(); var ally = f.AddFollower("ally", 6, 6);
            CombatSystem.PerformMeleeAttack(f.Player, ally, f.Zone, new CompanionCombatTests.LowRandom());
            Assert.Null(CompanionCombatTests.Fight(f.Follower)); Assert.Null(CompanionCombatTests.Fight(ally));
        }
        [TestCase("source-hidden")] [TestCase("victim-hidden")] [TestCase("source-foreign")]
        [TestCase("source-dead")]
        public void ReactiveDefenseRequiresCurrentWitnessedThreat(string changed)
        {
            var f = new CompanionCombatTests.Fixture();
            if (changed == "source-hidden") f.Enemy.GetPart<RenderPart>().Visible = false;
            if (changed == "victim-hidden") f.Player.GetPart<RenderPart>().Visible = false;
            if (changed == "source-foreign") { f.Zone.RemoveEntity(f.Enemy); new Zone("elsewhere").AddEntity(f.Enemy, 6, 5); }
            if (changed == "source-dead") f.Enemy.GetStat("Hitpoints").BaseValue = 0;
            f.Damage(f.Player); Assert.Null(CompanionCombatTests.Fight(f.Follower));
            var control = new CompanionCombatTests.Fixture(); control.Damage(control.Player);
            Assert.NotNull(CompanionCombatTests.Fight(control.Follower));
        }
        [Test] public void RestoredRecruitCanJoinAndRestoredFightCanResumeFollowing()
        {
            var f = new CompanionCombatTests.Fixture(); var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.Follower);
            var leader = loaded.GetPart<BrainPart>().PartyLeader; var zone = new Zone("restored");
            zone.AddEntity(leader, 5, 5); leader.GetPart<BrainPart>().CurrentZone = zone;
            zone.AddEntity(loaded, 5, 6); loaded.GetPart<BrainPart>().CurrentZone = zone;
            var foe = CompanionCombatTests.Actor(zone, "foe", 6, 5, "OutlandRaiders");
            CombatSystem.PerformMeleeAttack(leader, foe, zone, new CompanionCombatTests.LowRandom());
            Assert.AreSame(foe, CompanionCombatTests.Fight(loaded)?.Target);
            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded); var brain = restored.GetPart<BrainPart>();
            var restoredLeader = brain.PartyLeader; var newZone = new Zone("restored-again");
            newZone.AddEntity(restored, 5, 6); brain.CurrentZone = newZone;
            newZone.AddEntity(restoredLeader, 5, 5); restoredLeader.GetPart<BrainPart>().CurrentZone = newZone;
            // Saved target is intentionally absent from this loaded active zone.
            CompanionCombatTests.Turn(restored);
            Assert.Null(CompanionCombatTests.Fight(restored)); Assert.IsInstanceOf<FollowLeaderGoal>(brain.PeekGoal());
        }
        [Test] public void JoinedFollowerUsesFiniteLastSeenSearchRatherThanTrackingHiddenMovement()
        {
            var f = new CompanionCombatTests.Fixture(); f.Attack(); CompanionCombatTests.Turn(f.Follower);
            var goal = CompanionCombatTests.Fight(f.Follower); Assert.NotNull(goal); Assert.True(goal.HasLastSeen);
            int x = goal.LastSeenX, y = goal.LastSeenY; f.Brain.SightRadius = 0;
            f.Zone.MoveEntity(f.Enemy, 20, 20);
            for (int i = 0; i < KillGoal.MaximumSearchActions; i++) goal.TakeAction();
            Assert.True(goal.Abandoned); Assert.AreEqual(x, goal.LastSeenX); Assert.AreEqual(y, goal.LastSeenY);
        }
    }
}
