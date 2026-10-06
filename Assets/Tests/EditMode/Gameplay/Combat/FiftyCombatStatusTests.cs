using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    public sealed class FiftyCombatStatusTests
    {
        internal sealed class FixedRandom : Random
        {
            internal int Calls; readonly int value;
            internal FixedRandom(int value = 0) { this.value = value; }
            public override int Next(int max) { Calls++; return Math.Min(value,max-1); }
            public override int Next(int min, int max) { Calls++; return Math.Min(min+value,max-1); }
        }
        public sealed class DeathWitness : Part
        {
            public Entity Killer; public int Deaths;
            public override bool HandleEvent(GameEvent e) { if(e.ID == "Died") { Killer=e.GetParameter<Entity>("Killer"); Deaths++; } return true; }
        }
        internal static void Tick(Entity entity, Zone zone, string name = "EndTurn")
        {
            var e = GameEvent.New(name); e.SetParameter("Zone",(object)zone);
            try { entity.FireEvent(e); } finally { e.Release(); }
        }
        internal static void Ready(Entity entity) { foreach(var e in entity.GetPart<StatusEffectsPart>().GetAllEffects()) e.JustApplied=false; }
        [TestCase(false,true)] [TestCase(true,true)] [TestCase(false,false)] [TestCase(true,false)]
        public void DelayedDamageCreditsItsRealInflictor(bool bleeding, bool sourced)
        {
            var z=new Zone(); var attacker=F.Owner(creature:true); var target=F.Owner(creature:true);
            attacker.SetTag("Player");
            F.Place(z,attacker,10,10); F.Place(z,target,11,10); target.GetStat("Hitpoints").BaseValue=1;
            attacker.Statistics["Experience"]=new Stat { Owner=attacker,Name="Experience",BaseValue=0,Max=100000 };
            target.Statistics["XPValue"]=new Stat { Owner=target,Name="XPValue",BaseValue=7,Max=100 };
            var witness=new DeathWitness(); target.AddPart(witness);
            Effect dot=bleeding ? (Effect)new BleedingEffect(99,"1d1",new FixedRandom()) : new PoisonedEffect(5,"1d1",new FixedRandom());
            Assert.True(target.ApplyEffect(dot,sourced?attacker:null,z)); Assert.AreSame(target,dot.Owner);
            var e=GameEvent.New("BeginTurn"); e.SetParameter("Zone",(object)z);
            try { dot.OnTurnStart(target,e); } finally { e.Release(); }
            Assert.AreEqual(1,witness.Deaths); Assert.AreSame(sourced?attacker:null,witness.Killer); Assert.AreEqual(sourced?7:0,attacker.GetStatValue("Experience"));
        }
        [TestCase(false)] [TestCase(true)] public void FirstDoseCreditSurvivesStackAndReplacementSave(bool bleeding)
        {
            var z=new Zone(); var first=F.Owner(creature:true); var second=F.Owner(creature:true); var victim=F.Owner(creature:true);
            Effect a=bleeding ? (Effect)new BleedingEffect(99,"1d1") : new PoisonedEffect(5,"1d1");
            Effect b=bleeding ? (Effect)new BleedingEffect(100,"1d1") : new PoisonedEffect(8,"1d1");
            Assert.True(victim.ApplyEffect(a,first,z)); Assert.True(victim.ApplyEffect(b,second,z));
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(victim);
            var restored=loaded.GetPart<StatusEffectsPart>().GetAllEffects().Single();
            var source=restored.GetType().GetField("DamageSource"); Assert.NotNull(source,"Delayed damage stores its source independently of patient Owner.");
            var restoredSource=source.GetValue(restored) as Entity; Assert.NotNull(restoredSource); Assert.AreEqual(first.ID,restoredSource.ID);
            Assert.AreNotSame(first,restoredSource); Assert.AreSame(loaded,restored.Owner);
        }
        [TestCase("1d2","1d10","1d10")] [TestCase("1d10","1d2","1d10")]
        [TestCase("2d4","1d9","2d4")] [TestCase("1d1+8","1d10","1d1+8")]
        [TestCase("1d4+9","2d6","1d4+9")] [TestCase("1d4","zzz","1d4")]
        public void BleedingStacksByExpectedPotencyWithoutUsingRandomness(string prior,string incoming,string expected)
        {
            var rng=new FixedRandom(); var a=new BleedingEffect(10,prior,rng); Assert.True(a.OnStack(new BleedingEffect(20,incoming,rng)));
            Assert.AreEqual(expected,a.DamageDice); Assert.AreEqual(20,a.SaveTarget); Assert.AreEqual(0,rng.Calls);
        }
        [TestCase("dead")] [TestCase("removed")] [TestCase("other-zone")] [TestCase("alive")]
        public void HookCannotOutliveCurrentSameZoneWielder(string state)
        {
            var z=new Zone(); var hooker=F.Owner(creature:true); var target=F.Owner(creature:true); F.Place(z,hooker,10,10); F.Place(z,target,13,10);
            var rng=new FixedRandom(); var effect=new HookedEffect(9,hooker,99,rng); Assert.True(target.ApplyEffect(effect,hooker,z)); Ready(target);
            if(state=="dead") hooker.GetStat("Hitpoints").BaseValue=0;
            if(state=="removed" || state=="other-zone") { Assert.True(z.RemoveEntity(hooker)); if(state=="other-zone") F.Place(new Zone(),hooker,10,10); }
            Tick(target,z);
            Assert.AreEqual(state=="alive",target.HasEffect<HookedEffect>()); Assert.AreEqual(state=="alive"?1:0,rng.Calls);
            Assert.AreEqual(state=="alive"?(12,10):(13,10),z.GetEntityPosition(target));
        }
        [TestCase("natural",true)] [TestCase("removed",false)] [TestCase("death",false)]
        public void OnlyCompletedLivingOrdinaryBurnCreatesCharred(string ending,bool charred)
        {
            var z=new Zone(); var victim=F.Owner(creature:true); F.Place(z,victim,10,10);
            Assert.True(victim.ApplyEffect(new BurningEffect(1),null,z)); Ready(victim);
            if(ending=="removed") victim.GetPart<StatusEffectsPart>().RemoveEffect<BurningEffect>();
            else if(ending=="death") CombatSystem.ApplyDamage(victim,2000,null,z);
            else for(int i=0;i<3;i++) Tick(victim,z);
            Assert.False(victim.HasEffect<BurningEffect>()); Assert.AreEqual(charred,victim.HasEffect<CharredEffect>());
        }
        [Test] public void NaturalBurnDoesNotDuplicateExistingCharred()
        {
            var z=new Zone(); var victim=F.Owner(creature:true); F.Place(z,victim,10,10);
            Assert.True(victim.ApplyEffect(new CharredEffect(),null,z)); Assert.True(victim.ApplyEffect(new BurningEffect(1),null,z)); Ready(victim);
            for(int i=0;i<3;i++) Tick(victim,z);
            Assert.AreEqual(1,victim.GetPart<StatusEffectsPart>().GetAllEffects().Count(e=>e is CharredEffect));
        }
    }
}
