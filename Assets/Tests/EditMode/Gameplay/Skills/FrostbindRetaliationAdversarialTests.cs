using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class FrostbindRetaliationAdversarialTests
    {
        [SetUp] public void SetUp() { FactionManager.Initialize(); SkillRegistry.ResetForTests(); MessageLog.Clear(); }
        [TestCase("remove-root")] [TestCase("replace-root")] [TestCase("expire-root")]
        [TestCase("wrong-root-owner")] [TestCase("remove-status")]
        [TestCase("remove-target")] [TestCase("foreign-target")] [TestCase("remove-actor")]
        [TestCase("dead-actor")] [TestCase("dead-target")] [TestCase("become-party")]
        public void EffectLifecycleMutationCannotProvokeFromStaleOrAbsentRestraint(string mutation)
        {
            var f = new FrostbindRetaliationTests.Fixture();
            var observer = new FrostbindRetaliationTests.EffectObserver();
            observer.Callback = e =>
            {
                if (e.ID != "EffectApplied" || !(e.GetParameter<Effect>("Effect") is RootedEffect)) return;
                observer.Callback = null;
                switch (mutation)
                {
                    case "remove-root": f.Target.RemoveEffect<RootedEffect>(); break;
                    case "replace-root": f.Target.RemoveEffect<RootedEffect>(); f.Target.ApplyEffect(new RootedEffect(10), null, f.Zone); break;
                    case "expire-root": f.Target.GetEffect<RootedEffect>().Duration = 0; break;
                    case "wrong-root-owner": f.Target.GetEffect<RootedEffect>().Owner = f.Actor; break;
                    case "remove-status": f.Target.RemovePart(f.Target.GetPart<StatusEffectsPart>()); break;
                    case "remove-target": f.Zone.RemoveEntity(f.Target); break;
                    case "foreign-target": f.Zone.RemoveEntity(f.Target); new Zone("other").AddEntity(f.Target, 6, 5); break;
                    case "remove-actor": f.Zone.RemoveEntity(f.Actor); break;
                    case "dead-actor": f.Actor.GetStat("Hitpoints").BaseValue = 0; break;
                    case "dead-target": f.Target.GetStat("Hitpoints").BaseValue = 0; break;
                    case "become-party": f.Target.ApplyEffect(new RecruitedEffect(f.Actor), f.Actor, f.Zone); break;
                }
            };
            f.Target.AddPart(observer); Assert.True(f.Cast());
            Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor), mutation);
            var control = new FrostbindRetaliationTests.Fixture(); Assert.True(control.Cast());
            Assert.True(control.Target.GetPart<BrainPart>().IsPersonallyHostileTo(control.Actor));
        }
        [TestCase(0)] [TestCase(-1)]
        public void NonpositiveIncomingRestraintDoesNotProvoke(int duration)
        {
            var f = new FrostbindRetaliationTests.Fixture();
            f.Target.AddPart(new FrostbindRetaliationTests.EffectObserver { Callback = e =>
            { if (e.ID == "BeforeApplyEffect") e.GetParameter<Effect>("Effect").Duration = duration; } });
            Assert.True(f.Cast()); Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void IndependentRootInstalledByVetoObserverIsNotAttributedToCast()
        {
            var f = new FrostbindRetaliationTests.Fixture(); var observer = new FrostbindRetaliationTests.EffectObserver();
            observer.Callback = e =>
            {
                if (e.ID != "BeforeApplyEffect") return;
                observer.Callback = null; observer.Veto = false;
                Assert.True(f.Target.ApplyEffect(new RootedEffect(9), null, f.Zone)); observer.Veto = true;
            };
            f.Target.AddPart(observer); Assert.True(f.Cast());
            Assert.AreEqual(9, f.Target.GetEffect<RootedEffect>().Duration);
            Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void RootInstalledBeforeIntrinsicStackStillAttributesOnlyTheAddedDuration()
        {
            var f = new FrostbindRetaliationTests.Fixture(); var observer = new FrostbindRetaliationTests.EffectObserver();
            observer.Callback = e =>
            {
                if (e.ID != "BeforeApplyEffect") return;
                observer.Callback = null; Assert.True(f.Target.ApplyEffect(new RootedEffect(9), null, f.Zone));
            };
            f.Target.AddPart(observer); Assert.True(f.Cast()); Assert.AreEqual(13, f.Target.GetEffect<RootedEffect>().Duration);
            Assert.True(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [TestCase("foreign-cell")] [TestCase("remote-cell")] [TestCase("removed-target")]
        public void InvalidSelectionDoesNotChooseAnotherNeighborOrProvoke(string invalid)
        {
            var f = new FrostbindRetaliationTests.Fixture(); var neighbor = CompanionCombatTests.Actor(f.Zone, "other-neutral", 5, 6, "Villagers");
            Cell selected = invalid == "foreign-cell" ? new Zone("other").GetCell(6, 5)
                : invalid == "remote-cell" ? f.Zone.GetCell(20, 20) : f.Zone.GetEntityCell(f.Target);
            if (invalid == "removed-target") f.Zone.RemoveEntity(f.Target);
            Assert.False(f.Cast(selected)); Assert.AreEqual(0, f.Ability.CooldownRemaining);
            Assert.Null(neighbor.GetEffect<RootedEffect>()); Assert.False(neighbor.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
            Assert.False(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void ExistingPartyGrudgeIsNotForgivenByThisExemption()
        {
            var f = new FrostbindRetaliationTests.Fixture(); f.Target.ApplyEffect(new RecruitedEffect(f.Actor), f.Actor, f.Zone);
            f.Target.GetPart<BrainPart>().SetPersonallyHostile(f.Actor, false); Assert.True(f.Cast());
            Assert.True(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void LostRecruitmentMakesSameTargetAnOutsiderAgain()
        {
            var f = new FrostbindRetaliationTests.Fixture(); f.Target.ApplyEffect(new RecruitedEffect(f.Actor), f.Actor, f.Zone);
            f.Target.GetEffect<RecruitedEffect>().Dismiss(f.Actor); Assert.True(f.Cast());
            Assert.True(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
        }
        [Test] public void CreatureWithoutBrainStillReceivesTheSpellNormally()
        {
            var f = new FrostbindRetaliationTests.Fixture(); f.Target.RemovePart(f.Target.GetPart<BrainPart>());
            Assert.True(f.Cast()); Assert.NotNull(f.Target.GetEffect<RootedEffect>()); Assert.AreEqual(35, f.Ability.CooldownRemaining);
        }
        [Test] public void TargetCannotSeeThroughWallsAfterBecomingHostile()
        {
            var f = new FrostbindRetaliationTests.Fixture(); Assert.True(f.Cast()); Assert.True(f.Target.GetPart<BrainPart>().IsPersonallyHostileTo(f.Actor));
            f.Zone.MoveEntity(f.Actor, 20, 20); CompanionCombatTests.Turn(f.Target);
            Assert.Null(f.Target.GetPart<BrainPart>().FindGoal<KillGoal>());
        }
    }
}
