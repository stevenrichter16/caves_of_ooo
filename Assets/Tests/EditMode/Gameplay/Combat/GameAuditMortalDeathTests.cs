using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal sealed class MortalDeathFixture : IDisposable
    {
        public readonly EquipmentLifecycleFixture Scope = new EquipmentLifecycleFixture();
        public Entity Victim => Scope.Actor;
        public Body Body => Scope.Body;
        public Zone Zone => Scope.Zone;
        public readonly Entity Attacker;
        public readonly MortalDeathObserver Observer = new MortalDeathObserver();
        public BodyPart Head => Body.GetPartsByType("Head").Single();
        public MortalDeathFixture()
        {
            try
            {
                Victim.Tags.Remove("Player");
                Stat(Victim,"Hitpoints",100,100);Stat(Victim,"XPValue",10,1000);
                Stat(Victim,"Toughness",16,100);Victim.AddPart(new StatusEffectsPart());
                Attacker = Scope.Item("Player");Attacker.ID=Guid.NewGuid().ToString("N");
                Stat(Attacker,"Experience",0,100000);Stat(Attacker,"Level",1,100);
                Assert.IsTrue(Zone.AddEntity(Attacker,4,5));
                Victim.AddPart(Observer);
                Assert.IsTrue(Head.Mortal && Head.IsSeverable());
                Assert.IsNotNull(Head.ParentPart);
            }
            catch { Dispose(); throw; }
        }
        public static void Stat(Entity e,string name,int value,int max)
        {e.Statistics[name]=new Stat{Owner=e,Name=name,BaseValue=value,Max=max};}
        // Runtime RED can observe lost source before the optional argument exists.
        public bool Cut(BodyPart part,Entity source=null,Zone zone=null,bool withoutZone=false)
        {
            var m=typeof(Body).GetMethod("Dismember");var z=withoutZone?null:zone??Zone;
            return (bool)m.Invoke(Body,m.GetParameters().Length==3?new object[]{part,z,source}:new object[]{part,z});
        }
        public void CombatCut(BodyPart part,int damage,Entity source,Random rng)
        {
            var m=typeof(CombatSystem).GetMethod("CheckCombatDismemberment");
            m.Invoke(null,m.GetParameters().Length==7?new object[]{Victim,Body,part,damage,Zone,rng,source}:new object[]{Victim,Body,part,damage,Zone,rng});
        }
        public SkillEventContext AxeContext(bool decapitate=true,bool dismember=true,Random rng=null)
        {
            var skills=Attacker.GetPart<SkillsPart>();Assert.NotNull(skills);
            if(dismember)Assert.IsTrue(skills.AddSkill(new Axe_Dismember(),source:"GA03h"));
            if(decapitate)Assert.IsTrue(skills.AddSkill(new Axe_Decapitate(),source:"GA03h"));
            var weapon=Scope.Item("Battleaxe");Assert.That(weapon.GetPart<MeleeWeaponPart>().Attributes,Does.Contain("Axe"));
            var damage=new Damage(1);damage.AddAttribute("Axe");
            int before=Victim.GetStatValue("Hitpoints");CombatSystem.ApplyDamage(Victim,damage,Attacker,Zone);
            return new SkillEventContext{Attacker=Attacker,Defender=Victim,Weapon=weapon.GetPart<MeleeWeaponPart>(),WeaponEntity=weapon,Damage=damage,ActualDamage=before-Victim.GetStatValue("Hitpoints"),Zone=Zone,Rng=rng??new MortalDeathRng()};
        }
        public void Dead(Entity killer)
        {
            Assert.AreEqual(1,Observer.Died);Assert.IsTrue(Observer.Committed);
            Assert.LessOrEqual(Observer.BaseAtDeath,0);Assert.LessOrEqual(Victim.GetStat("Hitpoints").BaseValue,0);
            Assert.AreSame(killer,Observer.Killer);Assert.AreSame(Victim,Observer.Target);Assert.AreSame(Zone,Observer.Zone);
            Assert.IsTrue(Observer.ResidentAtDeath);Assert.IsNull(Zone.GetEntityCell(Victim));
        }
        public void Dispose()=>Scope.Dispose();
    }
    public sealed class MortalDeathObserver : Part
    {
        public int Died,BaseAtDeath,BeforeCuts,AfterCuts,Effects;
        public bool Veto,Committed,ResidentAtDeath;
        public Entity Killer,Target,EffectSource;
        public Zone Zone;
        public Action<GameEvent> Hook;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="BeforeDismember"){BeforeCuts++;if(Veto)return false;}
            if(e.ID=="AfterDismember")AfterCuts++;
            if(e.ID=="EffectApplied"){Effects++;EffectSource=e.GetParameter<Entity>("Source");}
            if(e.ID=="Died")
            {Died++;BaseAtDeath=ParentEntity.GetStat("Hitpoints")?.BaseValue??int.MinValue;Committed=ParentEntity.HasTag("_DeathHandled");Killer=e.GetParameter<Entity>("Killer");Target=e.GetParameter<Entity>("Target");Zone=e.GetParameter<Zone>("Zone");ResidentAtDeath=Zone?.GetEntityCell(ParentEntity)!=null;}
            Hook?.Invoke(e);return true;
        }
    }
    public sealed class MortalDeathRng : Random
    {
        public int Calls; public int Roll;
        public override int Next(int maxValue){Calls++;return Math.Min(Roll,maxValue-1);}
        public override int Next(int minValue,int maxValue){Calls++;return minValue;}
    }
    public class GameAuditMortalDeathTests
    {
        [TestCase(false)] [TestCase(true)]
        public void OnlyMortalLossCommitsDeath(bool mortal)
        {
            using(var f=new MortalDeathFixture())
            {Assert.IsTrue(f.Cut(mortal?f.Head:f.Scope.LeftArm));if(mortal)f.Dead(null);else{Assert.AreEqual(100,f.Victim.GetStatValue("Hitpoints"));Assert.AreEqual(0,f.Observer.Died);Assert.IsNotNull(f.Zone.GetEntityCell(f.Victim));}}
        }
        [TestCase(false)] [TestCase(true)]
        public void OptionalSourceCreditsOnlyActualKiller(bool sourced)
        {
            using(var f=new MortalDeathFixture())
            {Assert.IsTrue(f.Cut(f.Head,sourced?f.Attacker:null));Assert.AreSame(sourced?f.Attacker:null,f.Observer.Killer);Assert.AreEqual(sourced?10:0,f.Attacker.GetStatValue("Experience"));f.Dead(sourced?f.Attacker:null);}
        }
        [Test]
        public void CombatHelperThreadsSourceOnNonlethalMortalThreshold()
        {
            using(var f=new MortalDeathFixture())
            {CombatSystem.ApplyDamage(f.Victim,new Damage(50),f.Attacker,f.Zone);Assert.AreEqual(50,f.Victim.GetStatValue("Hitpoints"));f.CombatCut(f.Head,50,f.Attacker,new MortalDeathRng());Assert.AreSame(f.Attacker,f.Observer.Killer);Assert.AreEqual(10,f.Attacker.GetStatValue("Experience"));f.Dead(f.Attacker);}
        }
        [TestCase(false)] [TestCase(true)]
        public void AxePassiveBleedsOnlySurvivingNonmortalVictim(bool decapitate)
        {
            using(var f=new MortalDeathFixture())
            {var head=f.Head;var ctx=f.AxeContext(decapitate);Assert.AreEqual(1,ctx.ActualDamage);SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);Assert.AreEqual(1,f.Observer.AfterCuts);if(decapitate){Assert.IsNull(head.ParentPart);Assert.IsNull(f.Victim.GetEffect<BleedingEffect>());Assert.AreSame(f.Attacker,f.Observer.Killer);f.Dead(f.Attacker);}else{Assert.IsNotNull(head.ParentPart);Assert.NotNull(f.Victim.GetEffect<BleedingEffect>());Assert.AreSame(f.Attacker,f.Observer.EffectSource);Assert.AreEqual(99,f.Victim.GetStatValue("Hitpoints"));Assert.AreEqual(0,f.Observer.Died);}}
        }
        [Test]
        public void DecapitateMarkerAloneActuallyDispatchesWithoutCutting()
        {using(var f=new MortalDeathFixture()){var ctx=f.AxeContext(true,false);SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);Assert.AreEqual(99,f.Victim.GetStatValue("Hitpoints"));Assert.AreEqual(0,f.Observer.BeforeCuts);Assert.AreEqual(0,f.Observer.Died);Assert.IsNotNull(f.Head.ParentPart);Assert.IsNull(f.Victim.GetEffect<BleedingEffect>());}}
        [TestCase(false,0)] [TestCase(true,0)] [TestCase(true,10)]
        public void ActualInputRecognizesCommittedDeathBeforeTurnGates(bool die,int bonus)
        {
            using(var f=new HotbarSaveFixture(true,false))
            {
                var player=f.Input.PlayerEntity;var hp=player.GetStat("Hitpoints");hp.Bonus=bonus;
                var body=new Body();body.SetBody(AnatomyFactory.CreateHumanoid());player.AddPart(body);
                f.Input.TurnManager.RestoreSavedState(17,false,null,new List<TurnManager.SavedTurnEntry>());
                var modal=(DeathScreenController)HotbarSaveFixture.Get(f.Input,"_deathScreenController");Assert.IsFalse(modal.IsActive);
                if(die)Assert.IsTrue(body.Dismember(body.GetPartsByType("Head").Single(),f.Input.CurrentZone));
                typeof(InputHandler).GetMethod("Update",HotbarSaveFixture.Flags).Invoke(f.Input,null);
                Assert.AreEqual(die,modal.IsActive);Assert.AreEqual(bonus,hp.Bonus);if(!die)Assert.AreEqual(20,hp.BaseValue);
            }
        }
        [Test]
        public void OrdinaryLethalDamageKeepsFirstKillerAndSingleDeath()
        {using(var f=new MortalDeathFixture()){CombatSystem.ApplyDamage(f.Victim,new Damage(101),f.Attacker,f.Zone);f.Dead(f.Attacker);CombatSystem.HandleDeath(f.Victim,f.Victim,f.Zone);Assert.AreEqual(1,f.Observer.Died);Assert.AreEqual(10,f.Attacker.GetStatValue("Experience"));}}
        [TestCase(false)] [TestCase(true)]
        public void DirectMortalVetoPreservesHpGearAndCredit(bool veto)
        {using(var f=new MortalDeathFixture()){var gear=f.Scope.EquipDuelistBuckler();f.Observer.Veto=veto;Assert.AreEqual(!veto,f.Cut(f.Head,f.Attacker));if(veto){Assert.AreEqual(100,f.Victim.GetStatValue("Hitpoints"));Assert.IsNotNull(f.Head.ParentPart);Assert.AreSame(f.Victim,gear.GetPart<PhysicsPart>().Equipped);Assert.AreEqual(0,f.Attacker.GetStatValue("Experience"));Assert.AreEqual(0,f.Observer.Died);}else{f.Dead(f.Attacker);f.Scope.Ground(gear);}}}
        [TestCase(false)] [TestCase(true)]
        public void LevelUpKeepsRewardsButDoesNotHealCommittedSelfDeath(bool selfDeath)
        {
            using(var f=new MortalDeathFixture())
            {
                var player=f.Attacker;Assert.AreEqual(10,player.GetStatValue("XPValue"));
                player.GetStat("Experience").BaseValue=105;var hp=player.GetStat("Hitpoints");hp.BaseValue=3;int max=hp.Max;
                // Factory Player lacks bootstrap progression stats; configure explicit economy controls.
                MortalDeathFixture.Stat(player,"MP",2,100);MortalDeathFixture.Stat(player,"SP",3,100);
                int mp=player.GetStatValue("MP"),sp=player.GetStatValue("SP");
                if(selfDeath)CombatSystem.HandleDeath(player,player,f.Zone);else LevelingSystem.AwardKillXP(player,f.Victim,f.Zone);
                Assert.AreEqual(2,player.GetStatValue("Level"));Assert.AreEqual(0,player.GetStatValue("Experience"));Assert.AreEqual(max+2,hp.Max);
                Assert.AreEqual(selfDeath?0:max+2,hp.BaseValue);Assert.AreEqual(mp+1,player.GetStatValue("MP"));Assert.AreEqual(sp+1,player.GetStatValue("SP"));
            }
        }
    }
}
