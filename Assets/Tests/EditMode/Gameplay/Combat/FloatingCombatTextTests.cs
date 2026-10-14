using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FloatingCombatTextTests
    {
        FiftySecondCombatFixture fixture;
        Entity attacker,defender;
        MeleeWeaponPart weapon;
        readonly List<AsciiFxRequest> drained=new List<AsciiFxRequest>();

        [SetUp] public void Setup()
        {
            fixture=new FiftySecondCombatFixture();
            attacker=fixture.Actor(x:11);defender=fixture.Actor();
            weapon=BladeDefense.FindLongBlade(attacker);
            weapon.Attributes="TestWeapon";weapon.BaseDamage="1d1+8";weapon.PenBonus=3;
            foreach(var actor in new[]{attacker,defender})
            {
                var cell=fixture.Zone.GetEntityCell(actor);cell.IsVisible=cell.Explored=true;
            }
            AsciiFxBus.Clear();
        }
        [TearDown] public void Cleanup()
        {
            foreach(var request in drained)AsciiFxBus.Release(request);
            drained.Clear();AsciiFxBus.Clear();fixture.Dispose();
        }
        AsciiFxRequest[] Events()
        {
            drained.AddRange(AsciiFxBus.Drain());
            return drained.Where(r=>r.Type==AsciiFxRequestType.EventText).ToArray();
        }
        void AssertLabel(string expected,string color)
        {
            var events=Events();Assert.AreEqual(1,events.Length);
            Assert.AreEqual(expected,events[0].Text);Assert.AreEqual(color,events[0].ColorString);
            Assert.AreSame(defender,events[0].Anchor);Assert.AreSame(fixture.Zone,events[0].Zone);
            Assert.False(events[0].BlocksTurnAdvance);
        }
        [TestCase(true,true,0,"Parried","&C")]
        [TestCase(false,true,0,"Parried","&C")]
        [TestCase(true,false,5,"Guarded","&Y")]
        public void EligibleDefenseReportsOnlyItsActualOutcome(bool guarding,bool parried,int damage,string label,string color)
        {
            FiftySecondCombatFixture.Learn(defender,new LongBlades_Deflect());
            if(guarding)Assert.True(defender.ApplyEffect(new EnGardeEffect(),defender,fixture.Zone));
            AsciiFxBus.Clear();
            var rng=new ComposableBladeTests.Rolls(parried?0:99);
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,rng);
            Assert.AreEqual(parried?MeleeStrikeOutcome.FullParry:MeleeStrikeOutcome.Hit,result.Outcome);
            Assert.AreEqual(damage,result.ActualDamage);Assert.AreEqual(1000-damage,defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(1,rng.DefenseDraws);Assert.False(defender.HasEffect<EnGardeEffect>());
            AssertLabel(label,color);
        }
        [TestCase(true)] [TestCase(false)]
        public void OrdinaryMissAndUnguardedHitDoNotCreateOutcomeWords(bool miss)
        {
            if(miss){Assert.True(defender.ApplyEffect(new EnGardeEffect(),defender,fixture.Zone));weapon.HitBonus=-100;}
            AsciiFxBus.Clear();var rng=new ComposableBladeTests.Rolls();
            var result=CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,rng);
            Assert.AreEqual(miss?MeleeStrikeOutcome.NaturalMiss:MeleeStrikeOutcome.Hit,result.Outcome);
            Assert.AreEqual(miss?0:9,result.ActualDamage);Assert.Zero(rng.DefenseDraws);
            Assert.AreEqual(miss,defender.HasEffect<EnGardeEffect>());Assert.IsEmpty(Events());
        }
        [TestCase(true)] [TestCase(false)]
        public void HiddenDefenseNeverEmitsAnEventWordButStillResolves(bool visible)
        {
            fixture.Zone.GetEntityCell(defender).IsVisible=visible;
            FiftySecondCombatFixture.Learn(defender,new LongBlades_Deflect());
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,
                CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,new ComposableBladeTests.Rolls()).Outcome);
            Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));
            Assert.AreEqual(visible?1:0,Events().Length);
        }
        [TestCase(true)] [TestCase(false)]
        public void RiposteReportsAnIssuedCounterEvenWhenItMissesButNotAFailedChance(bool issued)
        {
            FiftySecondCombatFixture.Learn(defender,new LongBlades_Deflect());
            FiftySecondCombatFixture.Learn(defender,new LongBlades_Riposte());
            BladeDefense.FindLongBlade(defender).HitBonus=-100;
            var rng=new ComposableRiposteTests.Rolls(0,issued?0:99);
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,
                CombatSystem.PerformSingleAttack(attacker,defender,weapon,true,fixture.Zone,rng).Outcome);
            Assert.AreEqual(issued,defender.GetPart<RiposteAllowancePart>().Spent);
            Assert.AreEqual(1000,attacker.GetStatValue("Hitpoints"));
            Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));Assert.AreEqual(2,rng.Draws);
            var events=Events();
            CollectionAssert.AreEquivalent(issued?new[]{"Parried","Riposte"}:new[]{"Parried"},events.Select(r=>r.Text).ToArray());
            foreach(var request in events)
            {
                Assert.AreSame(defender,request.Anchor);Assert.AreEqual("&C",request.ColorString);
                Assert.False(request.BlocksTurnAdvance);
            }
        }
        public sealed class DamageGate : Part
        {
            public string Phase="BeforeTakeDamage";
            public bool Veto;
            public int Amount;
            public override bool WantEvent(int id)=>true;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID!=Phase)return true;
                if(Veto)return false;
                e.GetParameter<Damage>("Damage").Amount=Amount;
                return true;
            }
        }
        [TestCase("resistance","Resisted","&C")]
        [TestCase("veto","Blocked","&Y")]
        [TestCase("before-zero","Blocked","&Y")]
        [TestCase("take-zero","Blocked","&Y")]
        public void CompleteDamagePreventionDistinguishesResistanceFromOtherBlocks(string mode,string label,string color)
        {
            defender.Statistics["HeatResistance"]=new Stat{Owner=defender,Name="HeatResistance",BaseValue=mode=="resistance"?100:0,Max=100};
            if(mode!="resistance")defender.AddPart(new DamageGate{Phase=mode=="take-zero"?"TakeDamage":"BeforeTakeDamage",Veto=mode=="veto",Amount=0});
            var damage=new Damage(10);damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(defender,damage,attacker,fixture.Zone);
            Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));AssertLabel(label,color);
            Assert.IsEmpty(drained.Where(r=>r.Type==AsciiFxRequestType.Particle&&char.IsDigit(r.Glyph)));
        }
        [TestCase(true)] [TestCase(false)]
        public void PartialPreventionKeepsDamageDigitsWithoutClaimingFullResistance(bool resistance)
        {
            if(resistance)defender.Statistics["HeatResistance"]=new Stat{Owner=defender,Name="HeatResistance",BaseValue=50,Max=100};
            else defender.AddPart(new DamageGate{Amount=5});
            var damage=new Damage(10);damage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(defender,damage,attacker,fixture.Zone);
            Assert.AreEqual(995,defender.GetStatValue("Hitpoints"));Assert.IsEmpty(Events());
            CollectionAssert.AreEqual(new[]{'5'},drained.Where(r=>r.Type==AsciiFxRequestType.Particle&&char.IsDigit(r.Glyph)).Select(r=>r.Glyph).ToArray());
        }
        [TestCase(0)] [TestCase(-1)]
        public void NonpositiveInputIsNotAResistedAttack(int amount)
        {
            CombatSystem.ApplyDamage(defender,new Damage(amount),attacker,fixture.Zone);
            Assert.AreEqual(1000,defender.GetStatValue("Hitpoints"));Assert.IsEmpty(Events());
        }
    }
}
