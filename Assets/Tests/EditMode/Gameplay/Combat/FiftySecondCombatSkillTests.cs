using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondCombatSkillTests
    {
        FiftySecondCombatFixture f;
        [SetUp] public void Setup(){f=new FiftySecondCombatFixture();Diag.ResetAll();}
        [TearDown] public void Teardown()=>f.Dispose();
        [TestCase(true)] [TestCase(false)] public void CausticDrawSpendsExistingPrimerForOneActualHit(bool primed)
        {
            var s=FiftySecondCombatFixture.Skill("Corrosion_CausticDraw");var a=f.Actor(s);var victim=f.Target(13,10);
            if(primed)Assert.True(victim.ApplyEffect(new AcidicEffect(.5f),a,f.Zone));
            Assert.AreEqual(primed,f.Cast(a,s));Assert.AreEqual(primed?992:1000,victim.GetStatValue("Hitpoints"));
            Assert.False(victim.HasEffect<AcidicEffect>());Assert.AreEqual(primed?20:0,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void DryFrontBodyProtectsPrimedBodyFromCausticDraw()
        {
            var s=FiftySecondCombatFixture.Skill("Corrosion_CausticDraw");var a=f.Actor(s);var front=f.Target(11,10);var back=f.Target(13,10);
            back.ApplyEffect(new AcidicEffect(.5f),a,f.Zone);Assert.False(f.Cast(a,s));
            Assert.AreEqual(1000,front.GetStatValue("Hitpoints"));Assert.AreEqual(1000,back.GetStatValue("Hitpoints"));Assert.True(back.HasEffect<AcidicEffect>());
        }
        [TestCase("Melee",true,5)] [TestCase("Melee",false,9)] [TestCase("Spell",true,9)] [TestCase("Poison",true,9)]
        public void EnGardeHalvesOnlyOneAdjacentMeleeHit(string attribute,bool adjacent,int expected)
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_EnGarde");var a=f.Actor(s);var source=f.Target(adjacent?11:13,10);
            Assert.True(f.Cast(a,s));Assert.NotNull(FiftySecondCombatFixture.Effect(a,"EnGardeEffect"));
            var hit=new Damage(9);hit.AddAttribute(attribute);CombatSystem.ApplyDamage(a,hit,source,f.Zone);
            Assert.AreEqual(1000-expected,a.GetStatValue("Hitpoints"));
            var second=new Damage(9);second.AddAttribute("Melee");CombatSystem.ApplyDamage(a,second,source,f.Zone);
            Assert.AreEqual(1000-expected-(attribute=="Melee" || !adjacent ? 9 : 5),a.GetStatValue("Hitpoints"));
        }
        [TestCase(true)] [TestCase(false)] public void EnGardeRequiresSwordAtCastAndAtImpact(bool removeAfterCast)
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_EnGarde");var a=f.Actor(s,removeAfterCast?"Cutting LongBlades":"Piercing");
            Assert.AreEqual(removeAfterCast,f.Cast(a,s));
            if(!removeAfterCast){Assert.AreEqual(0,FiftySecondCombatFixture.Cooldown(a,s));return;}
            var w=SkillCombatHelpers.FindEquippedWeaponOfClass(a,"LongBlades").ParentEntity;
            Assert.True(InventorySystem.UnequipItem(a,w));var source=f.Target();var d=new Damage(10);d.AddAttribute("Melee");CombatSystem.ApplyDamage(a,d,source,f.Zone);
            Assert.AreEqual(990,a.GetStatValue("Hitpoints"));
        }
        [Test] public void EnGardeExpiresAndItsSavedStateDoesNotReapplyStats()
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_EnGarde");var a=f.Actor(s);Assert.True(f.Cast(a,s));
            var restored=PartRoundTripHelper.RoundTripEntityViaTokenGraph(a);var guard=FiftySecondCombatFixture.Effect(restored,"EnGardeEffect");
            Assert.NotNull(guard);Assert.AreEqual(2,guard.Duration);f.Tick(a,2);Assert.Null(FiftySecondCombatFixture.Effect(a,"EnGardeEffect"));
        }
        [TestCase(1,true)] [TestCase(1000,false)] public void FollowThroughAdvancesOnlyAfterItsActualKill(int hp,bool moves)
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_FollowThrough");var a=f.Actor(s);var victim=f.Target(hp:hp);var bystander=f.Target(10,9);
            Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(victim)));Assert.AreEqual(moves?(11,10):(10,10),f.Zone.GetEntityPosition(a));
            Assert.AreEqual(moves,f.Zone.GetEntityCell(victim)==null);Assert.AreEqual(1000,bystander.GetStatValue("Hitpoints"));
            Assert.AreEqual(12,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void FollowThroughMissStillPaysWithoutOpeningTheVictimsCell()
        {
            var s=new LongBlades_FollowThrough();var a=f.Actor(s);var victim=f.Target();victim.GetStat("DV").BaseValue=1000;
            Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(victim)));Assert.AreEqual(1000,victim.GetStatValue("Hitpoints"));
            Assert.AreEqual((10,10),f.Zone.GetEntityPosition(a));Assert.AreEqual(12,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void FollowThroughStillPaysWhenRootedButItsStrikeKills()
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_FollowThrough");var a=f.Actor(s);var victim=f.Target(hp:1);
            a.ApplyEffect(new RootedEffect(),null,f.Zone);Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(victim)));
            Assert.Null(f.Zone.GetEntityCell(victim));Assert.AreEqual((10,10),f.Zone.GetEntityPosition(a));Assert.AreEqual(12,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [Test] public void FollowThroughUsesRealTrapEntryAndEmptySelectionIsFree()
        {
            var s=FiftySecondCombatFixture.Skill("LongBlades_FollowThrough");var a=f.Actor(s);
            Assert.False(f.Cast(a,s,f.Zone.GetCell(11,10)));Assert.AreEqual(0,FiftySecondCombatFixture.Cooldown(a,s));
            var victim=f.Target(hp:1);var trap=new Entity();trap.AddPart(new SpikeTrapTriggerPart { Damage=12 });Assert.True(f.Zone.AddEntity(trap,11,10));
            Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(victim)));Assert.AreEqual((11,10),f.Zone.GetEntityPosition(a));Assert.AreEqual(988,a.GetStatValue("Hitpoints"));Assert.Null(f.Zone.GetEntityCell(trap));
        }
        [TestCase("body")] [TestCase("fragile")] [TestCase("stone")] [TestCase("open")]
        public void SlamCollisionStrikesOneRealObstructionOnly(string kind)
        {
            var s=new Cudgel_Slam();var a=f.Actor(s,"Bludgeoning Cudgel");var victim=f.Target();
            Entity obstruction=kind=="body"?f.Target(12,10):kind=="fragile"?f.Wall(12,10,50):kind=="stone"?f.Wall(12,10):null;
            Assert.True(f.Cast(a,s,f.Zone.GetEntityCell(victim)));
            if(kind=="body")Assert.Less(obstruction.GetStatValue("Hitpoints"),1000,"The collided body takes one impact.");
            if(kind=="fragile")Assert.Less(obstruction.GetPart<DestructiblePart>().HP,50,"The collided structural owner takes one impact.");
            if(kind=="stone")Assert.NotNull(f.Zone.GetEntityCell(obstruction));
            Assert.AreEqual(kind=="open"?(14,10):(11,10),f.Zone.GetEntityPosition(victim));
            Assert.AreEqual(50,FiftySecondCombatFixture.Cooldown(a,s));
        }
        [TestCase(false)] [TestCase(true)] public void VaultPoiseChangesOneActualAdjacentAttackRollEvenWhenItMisses(bool miss)
        {
            var s=new Acrobatics_Vault();var a=f.Actor(s);Assert.True(f.Cast(a,s));Assert.NotNull(FiftySecondCombatFixture.Effect(a,"VaultPoiseEffect"));
            var target=f.Target(13,10);if(miss)target.GetStat("DV").BaseValue=1000;
            var weapon=SkillCombatHelpers.FindEquippedWeaponOfClass(a,"LongBlades");
            CombatSystem.PerformSingleAttack(a,target,weapon,true,f.Zone,f.Rng);
            var first=DiagQuery.Apply(new DiagQuery.Filter { Category="damage",Kind="HitRoll",Limit=1 }).Records.Single();StringAssert.Contains("\"skillHitBonus\":2",first.PayloadJson);
            Assert.Null(FiftySecondCombatFixture.Effect(a,"VaultPoiseEffect"));Diag.ResetAll();
            CombatSystem.PerformSingleAttack(a,target,weapon,true,f.Zone,f.Rng);
            var second=DiagQuery.Apply(new DiagQuery.Filter { Category="damage",Kind="HitRoll",Limit=1 }).Records.Single();StringAssert.Contains("\"skillHitBonus\":0",second.PayloadJson);
        }
        [TestCase("blocked")] [TestCase("rooted")] [TestCase("walking")]
        public void FailedVaultOrOrdinaryMovementCannotProducePoise(string kind)
        {
            var s=new Acrobatics_Vault();var a=f.Actor(s);
            if(kind=="blocked")f.Wall(12,10);if(kind=="rooted")a.ApplyEffect(new RootedEffect(),null,f.Zone);
            if(kind=="walking")Assert.True(MovementSystem.TryMoveTo(a,f.Zone,11,10));else Assert.False(f.Cast(a,s));
            Assert.Null(FiftySecondCombatFixture.Effect(a,"VaultPoiseEffect"));
        }
        [Test] public void VaultPoiseExpiresAndDoesNotBoostLongRangeLunge()
        {
            var s=new Acrobatics_Vault();var a=f.Actor(s);Assert.True(f.Cast(a,s));var target=f.Target(14,10);
            CombatSystem.PerformSingleAttack(a,target,SkillCombatHelpers.FindEquippedWeaponOfClass(a,"LongBlades"),true,f.Zone,f.Rng);
            var record=DiagQuery.Apply(new DiagQuery.Filter { Category="damage",Kind="HitRoll",Limit=1 }).Records.Single();StringAssert.Contains("\"skillHitBonus\":0",record.PayloadJson);
            Assert.NotNull(FiftySecondCombatFixture.Effect(a,"VaultPoiseEffect"));f.Tick(a,2);Assert.Null(FiftySecondCombatFixture.Effect(a,"VaultPoiseEffect"));
        }
    }
}
