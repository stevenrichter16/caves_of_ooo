using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ComposableRiposteTests
    {
        FiftySecondCombatFixture f;Entity a,d;MeleeWeaponPart attack,sword;
        [SetUp] public void Setup()
        {
            f=new FiftySecondCombatFixture();a=f.Actor(x:11);d=f.Actor(new LongBlades_Deflect());
            attack=BladeDefense.FindLongBlade(a);sword=BladeDefense.FindLongBlade(d);
            attack.Attributes="TestWeapon";sword.PenBonus=3;
        }
        [TearDown] public void Cleanup()=>f.Dispose();
        void Learn(int rank=1)
        {var s=new LongBlades_Riposte();FiftySecondCombatFixture.Learn(d,s);s.Rank=rank;}
        internal sealed class Rolls : Random
        {
            readonly Queue<int> percent;
            public int Draws;
            public Rolls(params int[] rolls) {percent=new Queue<int>(rolls);}
            public override int Next(int max) {if(max!=100)return 0;Draws++;return percent.Count>0?percent.Dequeue():99;}
            public override int Next(int min,int max)=>max==21?10:min;
        }
        MeleeStrikeResult Strike(Rolls rng)=>CombatSystem.PerformSingleAttack(a,d,attack,true,f.Zone,rng);
        [TestCase(1,44,true)] [TestCase(1,45,false)] [TestCase(2,64,true)]
        [TestCase(2,65,false)] [TestCase(3,79,true)] [TestCase(3,80,false)]
        public void ConditionalRankBoundaries(int rank,int roll,bool counter)
        {
            Learn(rank);var rng=new Rolls(0,roll);Assert.AreEqual(MeleeStrikeOutcome.FullParry,Strike(rng).Outcome);
            Assert.AreEqual(counter?999:1000,a.GetStatValue("Hitpoints"));
            Assert.AreEqual(counter,d.GetPart<RiposteAllowancePart>().Spent);
        }
        [Test] public void FailedProcPreservesBudget_IssuedMissSpendsIt()
        {
            Learn();sword.HitBonus=-100;
            Strike(new Rolls(0,99));Assert.False(d.GetPart<RiposteAllowancePart>().Spent);
            Strike(new Rolls(0,0));Assert.True(d.GetPart<RiposteAllowancePart>().Spent);
            var rng=new Rolls(0,0);Strike(rng);Assert.AreEqual(1,rng.Draws,"Spent allowance skips proc RNG.");
            Assert.AreEqual(1000,a.GetStatValue("Hitpoints"));
        }
        [Test] public void RiposteWithoutSourceNeverRolls_EnGardeAloneIsSource()
        {
            Learn();d.GetPart<SkillsPart>().RemoveSkill(d.GetPart<LongBlades_Deflect>());
            var noSource=new Rolls(0,0);Strike(noSource);Assert.Zero(noSource.Draws);
            d.ApplyEffect(new EnGardeEffect(),d,f.Zone);Strike(new Rolls(0,0));
            Assert.True(d.GetPart<RiposteAllowancePart>().Spent);Assert.AreEqual(999,a.GetStatValue("Hitpoints"));
        }
        [Test] public void SkillRemovalAndReacquisitionDoesNotRefillAllowance()
        {
            Learn();Strike(new Rolls(0,0));var budget=d.GetPart<RiposteAllowancePart>();
            d.GetPart<SkillsPart>().RemoveSkill(d.GetPart<LongBlades_Riposte>());Learn();
            Assert.AreSame(budget,d.GetPart<RiposteAllowancePart>());Assert.True(budget.Spent);
        }
        [TestCase(true)] [TestCase(false)] public void CurrentSavePreservesAllowance(bool spent)
        {
            Learn();if(spent)Strike(new Rolls(0,0));
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(d);
            Assert.AreEqual(spent,loaded.GetPart<RiposteAllowancePart>().Spent);
        }
        [Test] public void OnlyScheduledOwnerCompletionResets_AfterItsEndEffects()
        {
            Learn();Strike(new Rolls(0,0));var budget=d.GetPart<RiposteAllowancePart>();
            var turns=new TurnManager();turns.AddEntity(d);turns.AddEntity(a);
            turns.EndTurn(d,f.Zone);Assert.True(budget.Spent,"Unscheduled EndTurn is not an owner boundary.");
            bool sawSpent=false;d.AddPart(new ComposableBladeAdversarialTests.DamageProbe{Callback=e=>{if(e.ID=="EndTurn")sawSpent=budget.Spent;}});
            turns.RestoreSavedState(0,true,d,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=d,Energy=1000}});
            turns.EndTurn(d,f.Zone);Assert.True(sawSpent);Assert.False(budget.Spent);
            budget.Spent=true;turns.EndTurn(d,f.Zone);Assert.True(budget.Spent,"Duplicate completion cannot renew allowance.");
        }
        [Test] public void CounterMissCannotSpawnRejoinderOrBackswing()
        {
            Learn();sword.Attributes="LongBlades Cudgel";sword.HitBonus=-100;
            FiftySecondCombatFixture.Learn(d,new Cudgel_Backswing());
            attack.Attributes="Piercing";FiftySecondCombatFixture.Learn(a,new ShortBlades_Rejoinder());
            var rng=new Rolls(0,0,0,0,0);Strike(rng);
            Assert.AreEqual(2,rng.Draws,"Reaction follow-up gates run before RNG.");
        }
        [Test] public void BothSidesCanParryButCounterNeverCountersCounter()
        {
            Learn();FiftySecondCombatFixture.Learn(a,new LongBlades_Deflect());FiftySecondCombatFixture.Learn(a,new LongBlades_Riposte());
            attack.Attributes="LongBlades";var rng=new Rolls(0,0,0,0);Strike(rng);
            Assert.AreEqual(3,rng.Draws);Assert.True(d.GetPart<RiposteAllowancePart>().Spent);
            Assert.False(a.GetPart<RiposteAllowancePart>().Spent);Assert.AreEqual(1000,a.GetStatValue("Hitpoints"));
        }
    }
}
