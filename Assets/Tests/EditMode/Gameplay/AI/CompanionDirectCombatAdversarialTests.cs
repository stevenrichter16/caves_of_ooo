using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CompanionDirectCombatAdversarialTests
    {
        [SetUp] public void SetUp() { FactionManager.Initialize(); MessageLog.Clear(); }
        [TestCase("veto")] [TestCase("zeroed")] [TestCase("resisted")] [TestCase("lethal")]
        [TestCase("stay")] [TestCase("hidden-target")] [TestCase("hidden-player")]
        [TestCase("party-target")] [TestCase("busy")] [TestCase("peace")]
        [TestCase("dismissed")] [TestCase("different-zone")] [TestCase("sight")]
        public void IneligibleDirectSpellDoesNotRallyButEquivalentValidOneDoes(string changed)
        {
            var f = new CompanionCombatTests.Fixture(); Entity target = f.Enemy;
            switch (changed)
            {
                case "veto": target.AddPart(new CompanionCombatTests.Veto { Event = "BeforeTakeDamage" }); break;
                case "zeroed": target.AddPart(new CompanionCombatTests.ZeroDamage()); break;
                case "resisted": target.Statistics["HeatResistance"] = new Stat { Owner = target, Name = "HeatResistance", BaseValue = 100, Min = 0, Max = 100 }; break;
                case "lethal": target.GetStat("Hitpoints").BaseValue = 1; break;
                case "stay": f.Follower.GetEffect<RecruitedEffect>().StayHere = true; break;
                case "hidden-target": target.GetPart<RenderPart>().Visible = false; break;
                case "hidden-player": f.Player.GetPart<RenderPart>().Visible = false; break;
                case "party-target": target = f.AddFollower("friend", 6, 6); break;
                case "busy": f.Brain.PushGoal(new WaitGoal(5)); break;
                case "peace": f.Brain.PushGoal(new NoFightGoal(3)); break;
                case "dismissed": f.Follower.GetEffect<RecruitedEffect>().Dismiss(f.Player); break;
                case "different-zone": f.Zone.RemoveEntity(f.Follower); var elsewhere = new Zone("elsewhere"); elsewhere.AddEntity(f.Follower, 5, 6); f.Brain.CurrentZone = elsewhere; break;
                case "sight": f.Brain.SightRadius = 0; break;
            }
            CompanionDirectCombatTests.Cast(f, target); Assert.Null(CompanionCombatTests.Fight(f.Follower), changed);
            var control = new CompanionCombatTests.Fixture(); CompanionDirectCombatTests.Cast(control, control.Enemy);
            Assert.NotNull(CompanionCombatTests.Fight(control.Follower), "Eligible positive control: " + changed);
        }
        [TestCase("target-removed")] [TestCase("player-dead")] [TestCase("stay")] [TestCase("dismissed")]
        public void PostDamageChangesAreRevalidatedWhenCastCommits(string change)
        {
            var f = new CompanionCombatTests.Fixture();
            var spell = new CompanionDirectCombatTests.DirectSpell { Targets = new[] { f.Enemy }, AfterDamage = () =>
            {
                Assert.Null(CompanionCombatTests.Fight(f.Follower), "A partial resolver is not a completed cast.");
                if (change == "target-removed") f.Zone.RemoveEntity(f.Enemy);
                if (change == "player-dead") f.Player.GetStat("Hitpoints").BaseValue = 0;
                if (change == "stay") f.Follower.GetEffect<RecruitedEffect>().StayHere = true;
                if (change == "dismissed") f.Follower.GetEffect<RecruitedEffect>().Dismiss(f.Player);
            } };
            Assert.True(spell.OnCommand(CompanionDirectCombatTests.Context(f)));
            Assert.Null(CompanionCombatTests.Fight(f.Follower));
            var control = new CompanionCombatTests.Fixture(); CompanionDirectCombatTests.Cast(control, control.Enemy);
            Assert.NotNull(CompanionCombatTests.Fight(control.Follower));
        }
        [Test] public void ThrowingResolverDoesNotLeakReceiptIntoNextCast()
        {
            var f = new CompanionCombatTests.Fixture();
            var broken = new CompanionDirectCombatTests.DirectSpell { Targets = new[] { f.Enemy }, AfterDamage = () => throw new InvalidOperationException("fixture") };
            Assert.Throws<InvalidOperationException>(() => broken.OnCommand(CompanionDirectCombatTests.Context(f)));
            Assert.Null(CompanionCombatTests.Fight(f.Follower));
            var other = CompanionCombatTests.Actor(f.Zone, "other", 7, 6, "OutlandRaiders");
            CompanionDirectCombatTests.Cast(f, other); Assert.AreSame(other, CompanionCombatTests.Fight(f.Follower)?.Target);
        }
        [Test] public void RepeatedTargetsRemainOneGoalAndRestoredGoalHasExactSavedTarget()
        {
            var f = new CompanionCombatTests.Fixture(); CompanionDirectCombatTests.Cast(f, f.Enemy, f.Enemy);
            Assert.AreEqual(94, f.Enemy.GetStatValue("Hitpoints")); Assert.AreEqual(2, f.Brain.GoalCount);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.Follower);
            Assert.AreEqual(f.Enemy.ID, CompanionCombatTests.Fight(loaded)?.Target.ID);
            Assert.AreSame(loaded.GetPart<BrainPart>().PartyLeader, loaded.GetEffect<RecruitedEffect>().Recruiter);
        }
        [Test] public void NonPlayerDirectSpellDoesNotIssuePlayerOrders()
        {
            var f = new CompanionCombatTests.Fixture(); f.Player.Tags.Remove("Player");
            CompanionDirectCombatTests.Cast(f, f.Enemy); Assert.Null(CompanionCombatTests.Fight(f.Follower));
            f.Player.SetTag("Player"); CompanionDirectCombatTests.Cast(f, f.Enemy); Assert.NotNull(CompanionCombatTests.Fight(f.Follower));
        }
        [Test] public void NeutralTargetCanBeDeliberatelyAttackedButPartyTargetCannot()
        {
            var f = new CompanionCombatTests.Fixture(); f.Enemy.SetTag("Faction", "Villagers");
            Assert.False(FactionManager.IsHostile(f.Enemy, f.Player)); CompanionDirectCombatTests.Cast(f, f.Enemy);
            Assert.AreSame(f.Enemy, CompanionCombatTests.Fight(f.Follower)?.Target);
        }
        [Test] public void GuaranteedWeaponHitRefusalDoesNotRally()
        {
            var f = new CompanionCombatTests.Fixture(); var veto = new CompanionCombatTests.Veto { Event = "BeforeTakeDamage" }; f.Enemy.AddPart(veto);
            Assert.AreEqual(0, SkillCombatHelpers.DealGuaranteedHitDamage(f.Player, f.Enemy, null, 3, f.Zone, new Random(42)));
            Assert.Null(CompanionCombatTests.Fight(f.Follower)); f.Enemy.RemovePart(veto);
            Assert.AreEqual(3, SkillCombatHelpers.DealGuaranteedHitDamage(f.Player, f.Enemy, null, 3, f.Zone, new Random(42)));
            Assert.AreSame(f.Enemy, CompanionCombatTests.Fight(f.Follower)?.Target);
        }
    }
}
