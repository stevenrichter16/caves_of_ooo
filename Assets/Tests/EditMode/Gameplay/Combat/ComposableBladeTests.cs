using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ComposableBladeTests
    {
        FiftySecondCombatFixture f;
        Entity defender, attacker;
        MeleeWeaponPart weapon;
        [SetUp] public void Setup()
        {
            f = new FiftySecondCombatFixture();
            defender = f.Actor(); attacker = f.Actor(weapon:"Cudgel", x:11);
            weapon = SkillCombatHelpers.FindEquippedWeaponOfClass(attacker,"Cudgel");
            weapon.BaseDamage="1d1+8"; weapon.PenBonus=3; weapon.Attributes="TestWeapon";
        }
        [TearDown] public void Cleanup() => f.Dispose();
        void Deflect(int rank)
        {
            var skill = new LongBlades_Deflect();
            FiftySecondCombatFixture.Learn(defender,skill); skill.Rank=rank;
        }
        internal sealed class Rolls : Random
        {
            readonly int defense, hit;
            public int DefenseDraws;
            public Rolls(int defense=0,int hit=10) { this.defense=defense;this.hit=hit; }
            public override int Next(int maxValue)
            { if(maxValue==100) { DefenseDraws++; return defense; } return 0; }
            public override int Next(int minValue,int maxValue) => maxValue==21 ? hit : minValue;
        }
        [TestCase(1,14,true)] [TestCase(1,15,false)]
        [TestCase(2,21,true)] [TestCase(2,22,false)]
        [TestCase(3,27,true)] [TestCase(3,28,false)]
        public void RankBoundaryUsesOneWouldHitRoll(int rank,int roll,bool parries)
        {
            Deflect(rank);var rng=new Rolls(roll);
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,f.Zone,rng);
            Assert.AreEqual(parries,result.Outcome==MeleeStrikeOutcome.FullParry);
            Assert.AreEqual(1,rng.DefenseDraws);
            Assert.AreEqual(parries?0:9,result.ActualDamage);
        }
        [TestCase(0,29,true)] [TestCase(0,30,false)]
        [TestCase(3,57,true)] [TestCase(3,58,false)]
        public void GuardAddsThirtyAndFailedParryHalvesAggregate(int rank,int roll,bool parries)
        {
            if(rank>0)Deflect(rank);defender.ApplyEffect(new EnGardeEffect(),defender,f.Zone);
            var rng=new Rolls(roll);
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,f.Zone,rng);
            Assert.AreEqual(parries?0:5,result.ActualDamage);
            Assert.AreEqual(parries,result.Outcome==MeleeStrikeOutcome.FullParry);
            Assert.False(defender.HasEffect<EnGardeEffect>());
            Assert.AreEqual(1,rng.DefenseDraws);
        }
        [Test] public void NaturalMissPreservesGuardAndDoesNotRollDefense()
        {
            Deflect(3); defender.ApplyEffect(new EnGardeEffect(),defender,f.Zone);
            weapon.HitBonus=-100;var rng=new Rolls();
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,f.Zone,rng);
            Assert.AreEqual(MeleeStrikeOutcome.NaturalMiss,result.Outcome);
            Assert.True(defender.HasEffect<EnGardeEffect>());Assert.Zero(rng.DefenseDraws);
        }
        [Test] public void NaturalTwentyStillParriesBeforeCriticalContactEffects()
        {
            Deflect(3);weapon.HitBonus=-100;var rng=new Rolls(hit:20);
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,f.Zone,rng);
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,result.Outcome);
            Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));
            Assert.IsEmpty(defender.GetPart<StatusEffectsPart>().GetAllEffects());
        }
        [Test] public void RawMeleeDamageHasNoGuardOpportunity()
        {
            defender.ApplyEffect(new EnGardeEffect(),defender,f.Zone);
            var damage=new Damage(9);damage.AddAttribute("Melee");
            CombatSystem.ApplyDamage(defender,damage,attacker,f.Zone);
            Assert.AreEqual(991,defender.GetStatValue("Hitpoints"));
            Assert.True(defender.HasEffect<EnGardeEffect>());
        }
        [TestCase(true)] [TestCase(false)] public void ContactRiderCannotBypassFullParry(bool conk)
        {
            Deflect(3);BaseSkillPart skill=conk?(BaseSkillPart)new Cudgel_Conk():new Axe_HookAndDrag();
            weapon.Attributes=conk?"Cudgel":"Axe";FiftySecondCombatFixture.Learn(attacker,skill);
            Assert.True(f.Cast(attacker,skill,f.Zone.GetEntityCell(defender),rng:new Rolls()));
            Assert.IsEmpty(defender.GetPart<StatusEffectsPart>().GetAllEffects());
        }
    }
}
