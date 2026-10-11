using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public abstract class DitchMateSlamFixture
    {
        internal DensityCombatFixture F;
        [SetUp] public void Setup(){F=new DensityCombatFixture();}
        [TearDown] public void Cleanup(){F.Dispose();}
        protected Entity Slammer(){var e=F.Actor(skills:"Cudgel_Slam");F.Equip(e,"Cudgel");return e;}
        protected static ActivatedAbility Ability(Entity e)=>e.GetPart<ActivatedAbilitiesPart>()?.AbilityList.FirstOrDefault(a=>a.Command=="CommandSlam");
    }
    public sealed class DitchMateSlamTests:DitchMateSlamFixture
    {
        [Test] public void AuthoredDitchMateHasActualCudgelSkillAndTruthfulWarning()
        {var e=F.Factory.CreateEntity("SpreadDitchMate");Assert.NotNull(SkillCombatHelpers.FindEquippedWeaponOfClass(e,"Cudgel"));Assert.NotNull(Ability(e));Assert.AreEqual(50,Ability(e).MaxCooldown);StringAssert.Contains("shove",e.GetPart<ExaminablePart>().Text.ToLowerInvariant());StringAssert.Contains("brace",e.GetPart<ExaminablePart>().Text.ToLowerInvariant());}
        [TestCase(-1,-1)] [TestCase(0,-1)] [TestCase(1,-1)] [TestCase(-1,0)] [TestCase(1,0)] [TestCase(-1,1)] [TestCase(0,1)] [TestCase(1,1)]
        public void RealSkillPushesExactEnemyInAllDirections(int dx,int dy)
        {var a=Slammer();var target=F.Target(10+dx,10+dy);Assert.True(F.Cast(a,target));Assert.AreEqual((10+dx*4,10+dy*4),F.Zone.GetEntityPosition(target));Assert.AreEqual(50,Ability(a).CooldownRemaining);Assert.False(F.Cast(a,target));}
        [Test] public void OrdinaryBrainUsesOneSlamInsteadOfAdditionalMelee()
        {var a=Slammer();var target=F.Target(11,10);a.GetPart<BrainPart>().PushGoal(new KillGoal(target));a.FireEventAndRelease(GameEvent.New("TakeTurn"));Assert.AreEqual((14,10),F.Zone.GetEntityPosition(target));Assert.AreEqual(500,target.GetStatValue("Hitpoints"));Assert.Greater(Ability(a).CooldownRemaining,0);}
        [TestCase(false)] [TestCase(true)] public void RealWornBootBraceStopsPushButNotSlamStun(bool braced)
        {var a=Slammer();var target=F.Target(11,10);Assert.True(F.Zone.AddEntity(F.Factory.CreateEntity("Terrain"),11,10));if(braced){var boots=F.Equip(target,"IronshodBoots");Assert.True(target.ApplyEffect(new EquipmentBraceEffect{Equipment=boots,ZoneID=F.Zone.ZoneID,OriginX=11,OriginY=10},target,F.Zone));Assert.True(target.GetEffect<EquipmentBraceEffect>().IsCurrent(target,F.Zone));}Assert.True(F.Cast(a,target));Assert.AreEqual((braced?11:14,10),F.Zone.GetEntityPosition(target));Assert.True(target.HasEffect<StunnedEffect>());Assert.False(target.HasEffect<EquipmentBraceEffect>());}
    }
    public sealed class DitchMateSlamAdversarialTests:DitchMateSlamFixture
    {
        [TestCase(12)] [TestCase(13)] [TestCase(14)] public void FriendInAnyPredictedCollisionCellPreventsSlam(int x)
        {var a=Slammer();var target=F.Target(11,10);var friend=F.Actor(x,10,"");Assert.False(F.Cast(a,target));Assert.AreEqual((11,10),F.Zone.GetEntityPosition(target));Assert.AreEqual(500,friend.GetStatValue("Hitpoints"));Assert.AreEqual(0,Ability(a).CooldownRemaining);F.Zone.RemoveEntity(friend);Assert.True(F.Cast(a,target));}
        [Test] public void PersonallyHostilePartyMemberInCollisionPathStillPreventsSlam()
        {var a=Slammer();var target=F.Target(11,10);var ally=F.Actor(12,10,"");ally.GetPart<BrainPart>().SetPartyLeader(a);a.GetPart<BrainPart>().SetPersonallyHostile(ally);Assert.True(FactionManager.IsHostile(a,ally));Assert.True(BrainPart.ArePartyAligned(a,ally));Assert.False(F.Cast(a,target));Assert.AreEqual(500,ally.GetStatValue("Hitpoints"));ally.GetPart<BrainPart>().SetPartyLeader(null);Assert.True(F.Cast(a,target));Assert.Less(ally.GetStatValue("Hitpoints"),500);}
        [Test] public void HostileCollisionStillUsesExistingImpactMechanic()
        {var a=Slammer();var target=F.Target(11,10);var other=F.Target(12,10);Assert.True(F.Cast(a,target));Assert.Less(other.GetStatValue("Hitpoints"),500);}
        [Test] public void UnrelatedAdjacentFriendCannotStealTheSelectedTarget()
        {var a=Slammer();var friend=F.Actor(10,9,"");var target=F.Target(11,10);Assert.True(F.Cast(a,target));Assert.AreEqual((10,9),F.Zone.GetEntityPosition(friend));Assert.False(friend.HasEffect<StunnedEffect>());}
        [TestCase("no-weapon")] [TestCase("wrong-weapon")] [TestCase("out-of-range")] [TestCase("hidden")] [TestCase("dead")]
        [TestCase("removed")] [TestCase("party")] [TestCase("cooldown")] [TestCase("chance")]
        public void IneligibleAttemptDoesNotSpendCooldownOrMoveTarget(string failure)
        {var a=Slammer();var target=F.Target(11,10);Assert.NotNull(Ability(a));
            if(failure=="no-weapon"||failure=="wrong-weapon"){var w=SkillCombatHelpers.FindEquippedWeaponOfClass(a,"Cudgel");Assert.True(InventorySystem.UnequipItem(a,w.ParentEntity));if(failure=="wrong-weapon")F.Equip(a,"Dagger");}
            if(failure=="out-of-range")F.Zone.MoveEntity(target,12,10);if(failure=="hidden")target.GetPart<RenderPart>().Visible=false;
            if(failure=="dead")target.GetStat("Hitpoints").BaseValue=0;if(failure=="removed")F.Zone.RemoveEntity(target);
            if(failure=="party")target.GetPart<BrainPart>().SetPartyLeader(a);if(failure=="cooldown")Ability(a).CooldownRemaining=4;
            if(failure=="chance")a.GetPart<CombatTacticsPart>().AbilityChance=0;
            var before=F.Zone.GetEntityPosition(target);int cooldown=Ability(a).CooldownRemaining;Assert.False(F.Cast(a,target));Assert.AreEqual(before,F.Zone.GetEntityPosition(target));Assert.AreEqual(cooldown,Ability(a).CooldownRemaining);
        }
        [Test] public void WallStopsPreviewBeforeFriendBehindIt()
        {var a=Slammer();var target=F.Target(11,10);F.Wall(12,10);var friend=F.Actor(13,10,"");Assert.True(F.Cast(a,target));Assert.AreEqual(500,friend.GetStatValue("Hitpoints"));}
        [TestCase(false)] [TestCase(true)] public void RemoteBodyEdgeUsesExactContactAndProtectsASecondRowFriend(bool friendPresent)
        {var a=Slammer();var target=F.Target(12,10);F.Zone.RemoveEntity(target);target.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0;-1,1"});Assert.True(F.Zone.AddEntity(target,12,10));var friend=friendPresent?F.Actor(12,11,""):null;
            Assert.AreEqual(!friendPresent,F.Cast(a,target));Assert.AreEqual((friendPresent?12:15,10),F.Zone.GetEntityPosition(target));if(friendPresent)Assert.AreEqual(500,friend.GetStatValue("Hitpoints"));}
        [Test] public void RemovedActorAndDuplicateSkillGrantCannotCreateAFreeCast()
        {var a=Slammer();var target=F.Target(11,10);a.FireEventAndRelease(GameEvent.New("ObjectCreated"));Assert.AreEqual(1,a.GetPart<ActivatedAbilitiesPart>().AbilityList.Count);F.Zone.RemoveEntity(a);Assert.False(F.Cast(a,target));Assert.AreEqual(0,Ability(a).CooldownRemaining);Assert.False(target.HasEffect<StunnedEffect>());}
        [Test] public void SavedOwnedSlamRetainsCooldownAndRegistration()
        {var a=Slammer();var target=F.Target(11,10);Assert.True(F.Cast(a,target));var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);Assert.AreEqual(50,Ability(loaded).CooldownRemaining);Assert.True(loaded.GetPart<SkillsPart>().HasSkill("Cudgel_Slam"));}
    }
}
