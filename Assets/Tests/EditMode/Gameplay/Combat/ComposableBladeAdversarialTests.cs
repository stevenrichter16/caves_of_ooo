using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ComposableBladeAdversarialTests
    {
        FiftySecondCombatFixture f;Entity a,d;MeleeWeaponPart sword,weapon;
        [SetUp] public void Setup()
        {
            f=new FiftySecondCombatFixture();d=f.Actor(new LongBlades_Deflect());a=f.Actor(x:11);
            sword=BladeDefense.FindLongBlade(d);weapon=BladeDefense.FindLongBlade(a);
            weapon.BaseDamage="1d1+8";weapon.PenBonus=3;weapon.Attributes="TestWeapon";
        }
        [TearDown] public void Cleanup()=>f.Dispose();
        MeleeStrikeResult Strike(int roll=0)=>CombatSystem.PerformSingleAttack(a,d,weapon,true,f.Zone,new ComposableBladeTests.Rolls(roll));
        [TestCase("normal",true)] [TestCase("token",false)] [TestCase("backlink",false)]
        [TestCase("carried",false)] [TestCase("destroyed",false)] [TestCase("gone",false)]
        [TestCase("grips",false)] [TestCase("abstract",false)] [TestCase("unsupported",false)]
        [TestCase("world",false)] [TestCase("broken",true)] [TestCase("detached",false)]
        public void Adversarial_CurrentWeaponStateControlsDefense(string change,bool eligible)
        {
            var item=sword.ParentEntity;var hands=d.GetPart<Body>().GetBody().GetParts().Where(p=>p.Equipped==item).ToArray();
            switch(change)
            {
                case "token":sword.Attributes="NotLongBlades";break;
                case "backlink":item.GetPart<PhysicsPart>().Equipped=a;break;
                case "carried":item.GetPart<PhysicsPart>().InInventory=d;break;
                case "destroyed":item.AddPart(new DestructiblePart{HP=0,MaxHP=10});break;
                case "gone":item.AddPart(new DestructiblePart{HP=10,MaxHP=10,Gone=true});break;
                case "grips":item.GetPart<EquippablePart>().UsesSlots="Hand,Hand";break;
                case "abstract":foreach(var h in hands)h.Abstract=true;break;
                case "unsupported":foreach(var h in hands)h.DependsOn="missing support";break;
                case "world":f.Zone.AddEntity(item,15,15);break;
                case "broken":item.ApplyEffect(new BrokenEffect());break;
                case "detached":foreach(var h in hands)h._Equipped=null;break;
            }
            Assert.AreEqual(eligible,Strike().Outcome==MeleeStrikeOutcome.FullParry);
        }
        [TestCase("normal",true)] [TestCase("rooted",true)] [TestCase("stunned",false)]
        [TestCase("frozen",false)] [TestCase("paralyzed",false)] [TestCase("sight",false)]
        [TestCase("range",false)] [TestCase("removed",false)]
        public void Adversarial_LiveReadOnlyReactionEligibility(string change,bool eligible)
        {
            switch(change)
            {
                case "rooted":d.ApplyEffect(new RootedEffect());break;
                case "stunned":d.ApplyEffect(new StunnedEffect());break;
                case "frozen":d.ApplyEffect(new FrozenEffect(1));break;
                case "paralyzed":d.ApplyEffect(new ParalyzedEffect());break;
                case "sight":d.AddPart(new BrainPart{SightRadius=0});break;
                case "range":f.Zone.MoveEntity(a,12,10);break;
                case "removed":f.Zone.RemoveEntity(a);break;
            }
            d.ApplyEffect(new EnGardeEffect(),d,f.Zone);
            Assert.AreEqual(eligible,Strike().Outcome==MeleeStrikeOutcome.FullParry);
            Assert.AreEqual(!eligible,d.HasEffect<EnGardeEffect>());
        }
        [Test] public void Adversarial_ArmorStoppedStillSpendsGuard()
        {
            weapon.PenBonus=-20;d.ApplyEffect(new EnGardeEffect(),d,f.Zone);
            Assert.AreEqual(MeleeStrikeOutcome.ArmorStopped,Strike(99).Outcome);
            Assert.False(d.HasEffect<EnGardeEffect>());
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_RemoteAnchorUsesActualBodyContact(bool wide)
        {
            f.Zone.RemoveEntity(a);
            if(wide)a.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0"});
            Assert.True(f.Zone.AddEntity(a,12,10));
            Assert.AreEqual(wide,Strike().Outcome==MeleeStrikeOutcome.FullParry);
        }
        [Test] public void Adversarial_ParryPreservesExistingPoisonAndDoesNotTickIt()
        {
            var poison=new PoisonedEffect(5,"1d1",new Random(7));d.ApplyEffect(poison,a,f.Zone);
            int duration=poison.Duration;
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,Strike().Outcome);
            Assert.AreSame(poison,d.GetPart<StatusEffectsPart>().GetEffect<PoisonedEffect>());
            Assert.AreEqual(duration,poison.Duration);Assert.AreEqual(1000,d.GetStatValue("Hitpoints"));
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ParrySuppressesWeaponProc(bool parries)
        {
            weapon.OnHitEffectsRaw="Burning,100,,5,1.0";
            Strike(parries?0:99);Assert.AreEqual(!parries,d.HasEffect<BurningEffect>());
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ParryStopsActualCriticalRider(bool parries)
        {
            weapon.Attributes="Cudgel";FiftySecondCombatFixture.Learn(a,new CudgelSkill());
            var result=CombatSystem.PerformSingleAttack(a,d,weapon,true,f.Zone,new ComposableBladeTests.Rolls(parries?0:99,20));
            Assert.AreEqual(parries,result.Outcome==MeleeStrikeOutcome.FullParry);
            Assert.AreEqual(!parries,d.HasEffect<StunnedEffect>());
        }
        public sealed class MissProbe : BaseSkillPart
        {
            public int Calls;
            public override void OnAttackerMeleeMiss(SkillEventContext ctx)=>Calls++;
            public override void OnDefenderAfterAttackMissed(SkillEventContext ctx)=>Calls++;
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ParryIsNotMissDispatch(bool parries)
        {
            var attackProbe=new MissProbe();var defendProbe=new MissProbe();
            FiftySecondCombatFixture.Learn(a,attackProbe);FiftySecondCombatFixture.Learn(d,defendProbe);
            if(!parries)weapon.HitBonus=-100;
            Assert.AreEqual(parries?MeleeStrikeOutcome.FullParry:MeleeStrikeOutcome.NaturalMiss,Strike().Outcome);
            Assert.AreEqual(parries?0:1,attackProbe.Calls);Assert.AreEqual(parries?0:1,defendProbe.Calls);
        }
        public sealed class DamageProbe : Part
        {
            public Action<GameEvent> Callback;
            public override bool HandleEvent(GameEvent e) { Callback?.Invoke(e);return true; }
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_GuardHalvesAggregateBeforeMutation(bool guard)
        {
            // Two penetrations of 9: aggregate half is 9, not 5+5. Then subtract 2.
            weapon.PenBonus=5;if(guard)d.ApplyEffect(new EnGardeEffect(),d,f.Zone);
            int observed=-1;d.AddPart(new DamageProbe{Callback=e=>{if(e.ID=="BeforeTakeDamage")
            {var damage=e.GetParameter<Damage>("Damage");observed=damage.Amount;damage.Amount-=2;}}});
            var result=Strike(99);Assert.AreEqual(guard?9:18,observed);Assert.AreEqual(guard?7:16,result.ActualDamage);
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ParentDamageExcludesNestedDamage(bool nested)
        {
            bool busy=false;a.AddPart(new DamageProbe{Callback=e=>{
                if(e.ID!="DamageDealt" || busy || !nested)return;busy=true;
                CombatSystem.ApplyDamage(d,new Damage(7),a,f.Zone);busy=false;
            }});
            var result=Strike(99);Assert.AreEqual(9,result.ActualDamage);
            Assert.AreEqual(nested?984:991,d.GetStatValue("Hitpoints"));
        }
        [Test] public void Adversarial_ReflectionDeathKeepsLandedResult()
        {
            d.AddPart(new DamageProbe{Callback=e=>{if(e.ID=="TakeDamage")CombatSystem.ApplyDamage(a,new Damage(2000),d,f.Zone);}});
            var result=Strike(99);Assert.AreEqual(MeleeStrikeOutcome.Hit,result.Outcome);Assert.AreEqual(9,result.ActualDamage);
            Assert.True(CombatSystem.IsDeathHandled(a));
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_FollowThroughPreservesContactEffectKill(bool parries)
        {
            weapon.Attributes="LongBlades";d.GetStat("Hitpoints").BaseValue=10;d.ApplyEffect(new CharredEffect());
            var follow=new LongBlades_FollowThrough();FiftySecondCombatFixture.Learn(a,follow);
            FiftySecondCombatFixture.Learn(a,new Pyromancy_Charsplit());
            Assert.True(f.Cast(a,follow,f.Zone.GetEntityCell(d),rng:new ComposableBladeTests.Rolls(parries?0:99)));
            Assert.AreEqual(parries?(11,10):(10,10),f.Zone.GetEntityPosition(a));
            Assert.AreEqual(!parries,CombatSystem.IsDeathHandled(d));
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ContactRiderStillWorksOnNaturalMiss(bool conk)
        {
            BaseSkillPart skill=conk?(BaseSkillPart)new Cudgel_Conk():new Axe_HookAndDrag();
            weapon.Attributes=conk?"Cudgel":"Axe";weapon.HitBonus=-100;FiftySecondCombatFixture.Learn(a,skill);
            Assert.True(f.Cast(a,skill,f.Zone.GetEntityCell(d),rng:new ComposableBladeTests.Rolls()));
            Assert.NotNull(FiftySecondCombatFixture.Effect(d,conk?"StunnedEffect":"HookedEffect"));
        }
    }
}
