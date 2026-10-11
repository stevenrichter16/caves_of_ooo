using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class FrostbindRetaliationTests
    {
        [SetUp] public void SetUp() { FactionManager.Initialize(); SkillRegistry.ResetForTests(); MessageLog.Clear(); }
        internal sealed class Fixture
        {
            public Zone Zone = new Zone("frostbind-retaliation");
            public Entity Actor, Target;
            public Cryomancy_Frostbind Skill = new Cryomancy_Frostbind();
            public Fixture()
            {
                Actor = CompanionCombatTests.Actor(Zone, "caster", 5, 5, "Player"); Actor.SetTag("Player");
                Target = CompanionCombatTests.Actor(Zone, "neutral", 6, 5, "Villagers");
                Actor.AddPart(new ActivatedAbilitiesPart()); Actor.AddPart(new SkillsPart());
                Assert.True(Actor.GetPart<SkillsPart>().AddSkill(Skill, "test"));
            }
            public ActivatedAbility Ability => Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(Skill.ActivatedAbilityID);
            public bool Cast(Cell selected = null)
            {
                var command = GameEvent.New("CommandFrostbind");
                command.SetParameter("Zone", (object)Zone); command.SetParameter("RNG", (object)new Random(7));
                command.SetParameter("SourceCell", (object)Zone.GetEntityCell(Actor));
                command.SetParameter("TargetCell", (object)(selected ?? Zone.GetEntityCell(Target)));
                try { Actor.FireEvent(command); return command.Handled; }
                finally { command.Release(); }
            }
        }
        internal sealed class EffectObserver : Part
        {
            public Action<GameEvent> Callback;
            public bool Veto;
            public override string Name => "FrostbindTestObserver";
            public override bool HandleEvent(GameEvent e)
            {
                Callback?.Invoke(e);
                return !Veto || e.ID != "BeforeApplyEffect";
            }
        }
        [Test] public void SuccessfulNeutralRestraintProvokesWithoutDamageAndPaysOneCooldown()
        {
            var f = new Fixture(); f.Target.GetPart<BrainPart>().InConversation = true;
            Assert.False(FactionManager.IsHostile(f.Target, f.Actor)); Assert.True(f.Cast());
            Assert.True(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
            Assert.False(f.Target.GetPart<BrainPart>().InConversation);
            Assert.AreEqual(4, f.Target.GetEffect<RootedEffect>().Duration); Assert.AreEqual(100, f.Target.GetStatValue("Hitpoints"));
            Assert.AreEqual(35, f.Ability.CooldownRemaining);
            Assert.False(f.Cast()); Assert.AreEqual(4, f.Target.GetEffect<RootedEffect>().Duration);
        }
        [Test] public void MeaningfulExtensionAlsoProvokes()
        {
            var f = new Fixture(); Assert.True(f.Target.ApplyEffect(new RootedEffect(2), null, f.Zone));
            Assert.True(f.Cast()); Assert.AreEqual(6, f.Target.GetEffect<RootedEffect>().Duration);
            Assert.True(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void VetoedEffectPreservesExistingCastPaymentButDoesNotProvoke()
        {
            var f = new Fixture(); f.Target.AddPart(new EffectObserver { Veto = true });
            Assert.True(f.Cast()); Assert.AreEqual(35, f.Ability.CooldownRemaining);
            Assert.Null(f.Target.GetEffect<RootedEffect>()); Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void RecruitedPartyTargetCanBeRestrainedWithoutChangingAllegiance()
        {
            var f = new Fixture(); Assert.True(f.Target.ApplyEffect(new RecruitedEffect(f.Actor), f.Actor, f.Zone));
            Assert.True(f.Cast()); Assert.NotNull(f.Target.GetEffect<RootedEffect>());
            Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor)); Assert.AreSame(f.Actor, f.Target.GetPart<BrainPart>().PartyLeader);
        }
        [Test] public void SelectedEmptyCellRefusesWithoutRetaliationOrCooldown()
        {
            var f = new Fixture(); Assert.False(f.Cast(f.Zone.GetCell(5, 4)));
            Assert.AreEqual(0, f.Ability.CooldownRemaining); Assert.Null(f.Target.GetEffect<RootedEffect>());
            Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void ExistingHostilityRemainsOnePersonalEnemy()
        {
            var f = new Fixture(); f.Target.GetPart<BrainPart>().SetPersonallyHostile(f.Actor, false);
            Assert.True(f.Cast()); Assert.AreEqual(1, f.Target.GetPart<BrainPart>().PersonalEnemies.Count);
        }
        [Test] public void SavedRestraintAndGrudgeKeepTheSameCasterReference()
        {
            var f = new Fixture(); Assert.True(f.Cast());
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.Target);
            var brain = loaded.GetPart<BrainPart>(); Assert.AreEqual(1, brain.PersonalEnemies.Count);
            Assert.AreEqual(4, loaded.GetEffect<RootedEffect>().Duration);
            foreach (var enemy in brain.PersonalEnemies) { Assert.AreEqual("caster", enemy.ID); Assert.AreSame(enemy, brain.Target); }
        }
        [Test] public void RootedPassiveNeutralFightsBackThroughOrdinaryBrainTurn()
        {
            var f = new Fixture(); f.Target.GetPart<BrainPart>().Passive = true;
            Assert.True(f.Cast()); CompanionCombatTests.Turn(f.Target);
            Assert.AreSame(f.Actor, f.Target.GetPart<BrainPart>().FindGoal<KillGoal>()?.Target);
            Assert.AreEqual((6, 5), f.Zone.GetEntityPosition(f.Target));
        }
    }
}
