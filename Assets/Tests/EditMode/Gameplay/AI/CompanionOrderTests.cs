using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CompanionOrderTests
    {
        const string Stay = "CompanionStay", Follow = "CompanionFollow";
        Zone zone;
        Entity leader, follower;
        [SetUp] public void SetUp()
        {
            FactionManager.Initialize(); zone = new Zone("companion-orders");
            leader = Creature("leader", 10, 10); leader.SetTag("Player");
            follower = Creature("follower", 11, 10);
            Assert.True(follower.ApplyEffect(new RecruitedEffect(leader), leader, zone));
        }
        Entity Creature(string id, int x, int y)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.SetTag("Creature"); e.SetTag("Faction", "Villagers");
            e.AddPart(new PhysicsPart { Solid = true }); e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new InventoryPart()); e.AddPart(new StatusEffectsPart());
            e.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(18) });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Max = 20 };
            Assert.True(zone.AddEntity(e, x, y)); return e;
        }
        bool Has(string command, Entity actor = null) => WorldInteractionSystem.GatherActions(follower, actor ?? leader).Any(a => a.Command == command);
        bool Command(string command, Entity actor = null, Zone at = null)
        {
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", (object)(actor ?? leader));
            e.SetParameter("Zone", (object)(at ?? zone)); e.SetParameter("Command", command);
            follower.FireEvent(e); bool handled = e.Handled; e.Release(); return handled;
        }
        void Turn() => follower.FireEventAndRelease(GameEvent.New("TakeTurn"));
        [Test] public void NormalWorldMenuOffersOnlyTheCurrentOrderAndPreservesRecruitment()
        {
            Assert.True(Has(Stay)); Assert.False(Has(Follow));
            var effect = follower.GetEffect<RecruitedEffect>(); int slots = leader.GetPart<BrainPart>().PartyMembers.Count;
            Assert.True(Command(Stay)); Assert.True(Has(Follow)); Assert.False(Has(Stay)); Assert.False(Command(Stay));
            Assert.AreSame(effect, follower.GetEffect<RecruitedEffect>());
            Assert.AreSame(leader, follower.GetPart<BrainPart>().PartyLeader); Assert.AreEqual(slots, leader.GetPart<BrainPart>().PartyMembers.Count);
            Assert.True(Command(Follow)); Assert.True(Has(Stay)); Assert.False(Has(Follow)); Assert.False(Command(Follow));
        }
        [TestCase("stranger")] [TestCase("far")] [TestCase("dead-leader")] [TestCase("dead-follower")]
        [TestCase("detached")] [TestCase("changed-leader")] [TestCase("no-effect")]
        public void StaleOrUnauthorizedChoiceCannotSetAnOrder(string mutation)
        {
            Assert.True(Has(Stay), "An authorized real menu choice must have existed before mutation.");
            Entity actor = leader;
            if (mutation == "stranger") actor = Creature("stranger", 11, 11);
            if (mutation == "far") Assert.True(zone.MoveEntity(leader, 20, 10));
            if (mutation == "dead-leader") leader.GetStat("Hitpoints").BaseValue = 0;
            if (mutation == "dead-follower") follower.GetStat("Hitpoints").BaseValue = 0;
            if (mutation == "detached") Assert.True(zone.RemoveEntity(follower));
            if (mutation == "changed-leader") Assert.True(follower.GetPart<BrainPart>().SetPartyLeader(Creature("other-leader", 12, 11)));
            if (mutation == "no-effect") follower.GetEffect<RecruitedEffect>().Dismiss(leader);
            Assert.False(Has(Stay, actor)); Assert.False(Command(Stay, actor));
        }
        [Test] public void StayHoldsForMoreThanFollowTimeoutThenResumeUsesTheSameParty()
        {
            Assert.True(Command(Stay)); var goal = follower.GetPart<BrainPart>().FindGoal<FollowLeaderGoal>(); Assert.NotNull(goal);
            Assert.True(zone.MoveEntity(leader, 20, 10));
            for (int i = 0; i < 230; i++) Turn();
            Assert.AreEqual((11, 10), zone.GetEntityPosition(follower));
            Assert.AreSame(goal, follower.GetPart<BrainPart>().FindGoal<FollowLeaderGoal>());
            Assert.True(zone.MoveEntity(leader, 10, 10)); Assert.True(Command(Follow));
            Assert.True(zone.MoveEntity(leader, 20, 10)); Turn(); Assert.AreEqual((12, 10), zone.GetEntityPosition(follower));
        }
        [Test] public void StaySurvivesLeaveAndReturnThenFollowAllowsNormalTransit()
        {
            Assert.True(Command(Stay)); var next = new Zone("next");
            Assert.True(zone.TryTransferEntityTo(leader, next, 10, 10)); leader.GetPart<BrainPart>().CurrentZone = next;
            ZoneTransitionSystem.TransitPartyMembers(leader, zone, next, 10, 10);
            Assert.NotNull(zone.GetEntityCell(follower)); Assert.Null(next.GetEntityCell(follower)); Assert.AreEqual((11, 10), zone.GetEntityPosition(follower));
            Assert.True(next.TryTransferEntityTo(leader, zone, 10, 10)); leader.GetPart<BrainPart>().CurrentZone = zone;
            Assert.True(Has(Follow)); Assert.True(Command(Follow));
            Assert.True(zone.TryTransferEntityTo(leader, next, 10, 10)); leader.GetPart<BrainPart>().CurrentZone = next;
            ZoneTransitionSystem.TransitPartyMembers(leader, zone, next, 10, 10);
            Assert.NotNull(next.GetEntityCell(follower)); Assert.AreSame(next, follower.GetPart<BrainPart>().CurrentZone);
            Assert.IsNull(zone.GetEntityCell(follower));
        }
        [TestCase(false)] [TestCase(true)] public void TokenGraphSavePreservesTheOrderAndExactLoadedRecruiter(bool staying)
        {
            if (staying) Assert.True(Command(Stay));
            follower = PartRoundTripHelper.RoundTripEntityViaTokenGraph(follower);
            leader = follower.GetEffect<RecruitedEffect>().Recruiter;
            Assert.AreSame(leader, follower.GetPart<BrainPart>().PartyLeader);
            Assert.True(leader.GetPart<BrainPart>().PartyMembers.Contains(follower));
            zone = new Zone("loaded-orders");
            leader.GetPart<BrainPart>().CurrentZone = zone; follower.GetPart<BrainPart>().CurrentZone = zone;
            Assert.True(zone.AddEntity(leader, 10, 10)); Assert.True(zone.AddEntity(follower, 11, 10));
            Assert.True(Has(staying ? Follow : Stay)); Assert.False(Has(staying ? Stay : Follow));
            Assert.True(zone.MoveEntity(leader, 20, 10)); Turn();
            Assert.AreEqual(staying ? (11, 10) : (12, 10), zone.GetEntityPosition(follower));
        }
        [Test] public void DismissalAndNewRecruitmentDoNotInheritOldStayOrder()
        {
            Assert.True(Command(Stay)); follower.GetEffect<RecruitedEffect>().Dismiss(leader);
            Assert.False(Has(Stay)); Assert.False(Has(Follow)); Assert.Null(follower.GetPart<BrainPart>().PartyLeader);
            Assert.True(follower.ApplyEffect(new RecruitedEffect(leader), leader, zone));
            Assert.True(Has(Stay)); Assert.False(Has(Follow));
        }
        [Test] public void ResumeRestoresAMissingFollowGoalWithoutDuplicatingExistingOne()
        {
            Assert.True(Command(Stay)); var brain = follower.GetPart<BrainPart>();
            brain.RemoveGoal(brain.FindGoal<FollowLeaderGoal>());
            Assert.True(Command(Follow)); Assert.NotNull(brain.FindGoal<FollowLeaderGoal>());
            int count = brain.GoalCount;
            Assert.True(Command(Stay)); Assert.True(Command(Follow)); Assert.AreEqual(count, brain.GoalCount);
        }
        public sealed class ThrowAfterAction : Part
        {
            public override string Name => "OrderThrowAfter";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") throw new InvalidOperationException("order rollback probe"); return true; }
        }
        public sealed class ReplaceBrainAfterAction : Part
        {
            public Entity Follower;
            public Zone Zone;
            public override string Name => "OrderReplaceBrainAfter";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "AfterInventoryAction") return true;
                Follower.RemovePart(Follower.GetPart<BrainPart>());
                Follower.AddPart(new BrainPart { CurrentZone = Zone, PartyLeader = ParentEntity });
                return true;
            }
        }
        [Test] public void BrainReplacementDuringInventoryCallbackRejectsCapturedOrderOwner()
        {
            Assert.True(Has(Stay)); var previous = follower.GetPart<BrainPart>();
            leader.AddPart(new ReplaceBrainAfterAction { Follower = follower, Zone = zone });
            Assert.False(InventorySystem.PerformAction(leader, follower, Stay, zone));
            Assert.AreNotSame(previous, follower.GetPart<BrainPart>());
            Assert.True(Has(Stay)); Assert.False(Has(Follow));
        }
        [Test] public void InventoryTransactionFailureRestoresThePreviousOrder()
        {
            Assert.True(Has(Stay)); leader.AddPart(new ThrowAfterAction());
            Assert.False(InventorySystem.PerformAction(leader, follower, Stay, zone));
            Assert.True(Has(Stay)); Assert.False(Has(Follow));
        }
        [TestCase(false)] [TestCase(true)]
        public void StayingResponderDoesNotJoinPlayerAttackButFollowingResponderDoes(bool staying)
        {
            if (staying) Assert.True(Command(Stay));
            var enemy = Creature("enemy", 10, 11); enemy.SetTag("Faction", "OutlandRaiders");
            leader.AddPart(new MeleeWeaponPart { BaseDamage = "1", HitBonus = -1000 });
            Assert.True(CombatSystem.PerformMeleeAttack(leader, enemy, zone, new Random(1)));
            Assert.AreEqual(!staying, follower.GetPart<BrainPart>().HasGoal<KillGoal>());
        }
        [Test] public void FollowingAllyDefendsAStayingVictimWithoutOverridingTheVictimsStay()
        {
            Assert.True(Command(Stay));
            var ally = Creature("ally", 10, 9); Assert.True(ally.ApplyEffect(new RecruitedEffect(leader), leader, zone));
            var enemy = Creature("enemy", 12, 10); enemy.SetTag("Faction", "OutlandRaiders");
            CombatSystem.ApplyDamage(follower, 1, enemy, zone);
            Assert.AreEqual(19, follower.GetStatValue("Hitpoints"));
            Assert.AreSame(enemy, ally.GetPart<BrainPart>().FindGoal<KillGoal>()?.Target);
            Assert.False(follower.GetPart<BrainPart>().HasGoal<KillGoal>());
            Assert.True(Has(Follow));
        }
    }
}
