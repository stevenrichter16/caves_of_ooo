using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CompanionCombatTests
    {
        [SetUp] public void SetUp() { FactionManager.Initialize(); MessageLog.Clear(); }

        internal sealed class LowRandom : Random
        {
            public override int Next(int maxValue) => 0;
            public override int Next(int minValue, int maxValue) => minValue;
        }
        internal sealed class Veto : Part
        {
            public string Event;
            public override string Name => "CompanionTestVeto";
            public override bool HandleEvent(GameEvent e) => e.ID != Event;
        }
        internal sealed class Fixture
        {
            public readonly Zone Zone = new Zone("companion-combat");
            public Entity Player, Follower, Enemy;
            public Fixture()
            {
                Player = Actor(Zone, "player", 5, 5, "Player"); Player.SetTag("Player");
                Follower = Actor(Zone, "follower", 5, 6, "Villagers");
                Assert.True(Follower.ApplyEffect(new RecruitedEffect(Player), Player, Zone));
                Enemy = Actor(Zone, "enemy", 6, 5, "OutlandRaiders");
            }
            public BrainPart Brain => Follower.GetPart<BrainPart>();
            public bool Attack() => CombatSystem.PerformMeleeAttack(Player, Enemy, Zone, new LowRandom());
            public void Damage(Entity victim, Entity source = null, int amount = 1)
                => CombatSystem.ApplyDamage(victim, amount, source ?? Enemy, Zone);
            public Entity AddFollower(string name = "other", int x = 4, int y = 6)
            {
                var member = Actor(Zone, name, x, y, "Villagers");
                Assert.True(member.ApplyEffect(new RecruitedEffect(Player), Player, Zone));
                return member;
            }
        }
        internal static Entity Actor(Zone zone, string id, int x, int y, string faction)
        {
            var actor = new Entity { ID = id, BlueprintName = "TestCompanion" };
            actor.SetTag("Creature"); actor.SetTag("Faction", faction);
            foreach (var name in new[] { "Strength", "Agility", "Toughness", "Speed" })
                actor.Statistics[name] = new Stat { Name = name, Owner = actor, BaseValue = name == "Speed" ? 100 : 16, Min = 0, Max = 200 };
            actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = actor, BaseValue = 100, Min = 0, Max = 100 };
            actor.AddPart(new RenderPart { DisplayName = id });
            actor.AddPart(new PhysicsPart { Solid = true });
            actor.AddPart(new MeleeWeaponPart { BaseDamage = "1", HitBonus = -1000 });
            actor.AddPart(new BrainPart { CurrentZone = zone, Rng = new LowRandom(), SightRadius = 10, FleeThreshold = 0 });
            zone.AddEntity(actor, x, y);
            return actor;
        }
        internal static KillGoal Fight(Entity member) => member.GetPart<BrainPart>().FindGoal<KillGoal>();
        internal static void Turn(Entity actor) => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));

        [Test] public void PlayerMeleeMissRalliesRecruitWithoutPlayerKillGoalOrFreeFollowerAttack()
        {
            var f = new Fixture(); int hp = f.Enemy.GetStatValue("Hitpoints"); var pos = f.Zone.GetEntityPosition(f.Follower);
            Assert.False(f.Player.GetPart<BrainPart>().HasGoal<KillGoal>());
            Assert.True(f.Attack());
            Assert.That(Fight(f.Follower)?.Target, Is.SameAs(f.Enemy));
            Assert.That(f.Enemy.GetStatValue("Hitpoints"), Is.EqualTo(hp), "The guaranteed miss and rally cause no extra strike.");
            Assert.That(f.Zone.GetEntityPosition(f.Follower), Is.EqualTo(pos));
            Assert.That(f.Brain.GoalCount, Is.EqualTo(2));
        }
        [Test] public void VetoedMeleeDoesNotRallyButRemovingVetoDoes()
        {
            var f = new Fixture(); var veto = new Veto { Event = "BeforeMeleeAttack" }; f.Player.AddPart(veto);
            Assert.False(f.Attack()); Assert.Null(Fight(f.Follower));
            f.Player.RemovePart(veto); Assert.True(f.Attack()); Assert.NotNull(Fight(f.Follower));
        }
        [Test] public void RepeatedAttacksKeepOneExactCombatGoal()
        {
            var f = new Fixture(); f.Attack(); var first = Fight(f.Follower);
            for (int i = 0; i < 5; i++) f.Attack();
            Assert.NotNull(first); Assert.AreSame(first, Fight(f.Follower)); Assert.AreEqual(2, f.Brain.GoalCount);
        }
        [Test] public void OrdinaryFollowerTurnUsesExistingCombatThenReturnsToFollowingAfterTargetLeaves()
        {
            var f = new Fixture(); f.Attack(); Assert.NotNull(Fight(f.Follower)); Turn(f.Follower);
            Assert.True(Fight(f.Follower).HasLastSeen);
            f.Zone.RemoveEntity(f.Enemy); Turn(f.Follower);
            Assert.Null(Fight(f.Follower)); Assert.IsInstanceOf<FollowLeaderGoal>(f.Brain.PeekGoal());
        }
        [Test] public void SavedCombatGoalKeepsLeaderAndTargetIdentity()
        {
            var f = new Fixture(); f.Attack(); Assert.NotNull(Fight(f.Follower));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.Follower); var brain = loaded.GetPart<BrainPart>();
            Assert.That(brain.FindGoal<KillGoal>()?.Target.ID, Is.EqualTo("enemy"));
            Assert.AreSame(brain.PartyLeader, brain.FindGoal<FollowLeaderGoal>().Leader);
            Assert.AreSame(brain.PartyLeader, loaded.GetEffect<RecruitedEffect>().Recruiter);
            Assert.AreEqual(2, brain.GoalCount);
        }
        [TestCase("player")] [TestCase("self")] [TestCase("member")]
        public void ActualHostileDamagePromptsWitnessedPartyDefense(string who)
        {
            var f = new Fixture(); var victim = who == "player" ? f.Player : who == "self" ? f.Follower : f.AddFollower();
            int hp = victim.GetStatValue("Hitpoints"); f.Damage(victim);
            Assert.AreEqual(hp - 1, victim.GetStatValue("Hitpoints")); Assert.AreSame(f.Enemy, Fight(f.Follower)?.Target);
        }
        [TestCase("BeforeTakeDamage")] [TestCase("TakeDamage")]
        public void RefusedOrZeroedDamageDoesNotRally(string eventName)
        {
            var f = new Fixture();
            if (eventName == "BeforeTakeDamage") f.Player.AddPart(new Veto { Event = eventName });
            else f.Player.AddPart(new ZeroDamage());
            f.Damage(f.Player); Assert.AreEqual(100, f.Player.GetStatValue("Hitpoints")); Assert.Null(Fight(f.Follower));
        }
        internal sealed class ZeroDamage : Part
        {
            public override string Name => "CompanionZeroDamage";
            public override bool HandleEvent(GameEvent e) { if (e.ID == "TakeDamage") e.GetParameter<Damage>("Damage").Amount = 0; return true; }
        }
        [Test] public void PartyDamageDoesNotCreateFriendlyFireWar()
        {
            var f = new Fixture(); var other = f.AddFollower(); f.Damage(f.Follower, other);
            Assert.Null(Fight(f.Follower)); Assert.Null(Fight(other));
        }
        [Test] public void NeutralDamageDoesNotIntroduceGeneralFactionRetaliation()
        {
            var f = new Fixture(); var neutral = Actor(f.Zone, "neutral", 6, 6, "Villagers");
            Assert.False(FactionManager.IsHostile(neutral, f.Player)); f.Damage(f.Player, neutral);
            Assert.Null(Fight(f.Follower));
        }
        [Test] public void EnvironmentalDamageDoesNotInventAnAttacker()
        {
            var f = new Fixture(); CombatSystem.ApplyDamage(f.Follower, 1, null, f.Zone); Assert.Null(Fight(f.Follower));
        }
        [Test] public void ExistingCombatIsNotReplacedByLaterLeaderAttack()
        {
            var f = new Fixture(); var other = Actor(f.Zone, "older-enemy", 7, 6, "OutlandRaiders");
            var old = new KillGoal(other); f.Brain.PushGoal(old); f.Attack();
            Assert.AreSame(old, f.Brain.PeekGoal()); Assert.AreEqual(2, f.Brain.GoalCount);
        }
        [Test] public void OrdinaryAiLeaderAssistStillUsesExistingFollowGoal()
        {
            var f = new Fixture(); f.Player.Tags.Remove("Player"); var leaderBrain = f.Player.GetPart<BrainPart>();
            leaderBrain.Target = f.Enemy; leaderBrain.PushGoal(new KillGoal(f.Enemy));
            f.Brain.FindGoal<FollowLeaderGoal>().TakeAction(); Assert.AreSame(f.Enemy, Fight(f.Follower)?.Target);
        }
    }
}
