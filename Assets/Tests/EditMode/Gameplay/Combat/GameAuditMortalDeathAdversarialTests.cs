using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public class GameAuditMortalDeathAdversarialTests
    {
        [TestCase(-7)] [TestCase(0)] [TestCase(13)]
        public void DeathPreservesNegativeBaseAndEveryModifier(int initial)
        {using(var f=new MortalDeathFixture()){var hp=f.Victim.GetStat("Hitpoints");hp.BaseValue=initial;hp.Bonus=17;hp.Penalty=2;hp.Boost=3;hp.Min=4;int max=hp.Max;CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);Assert.AreEqual(Math.Min(initial,0),hp.BaseValue);Assert.AreEqual(17,hp.Bonus);Assert.AreEqual(2,hp.Penalty);Assert.AreEqual(3,hp.Boost);Assert.AreEqual(4,hp.Min);Assert.AreEqual(max,hp.Max);Assert.IsTrue(CombatSystem.IsDeathHandled(f.Victim));Assert.Greater(hp.Value,0);f.Dead(f.Attacker);}}
        [Test]
        public void MissingHpIsNotInventedByDirectDeath()
        {using(var f=new MortalDeathFixture()){f.Victim.Statistics.Remove("Hitpoints");CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);Assert.IsNull(f.Victim.GetStat("Hitpoints"));Assert.AreEqual(1,f.Observer.Died);Assert.IsTrue(f.Observer.Committed);Assert.AreSame(f.Attacker,f.Observer.Killer);}}
        [Test]
        public void BodylessDirectDeathStillNormalizesExistingHp()
        {using(var f=new MortalDeathFixture()){f.Victim.RemovePart(f.Body);Assert.IsNull(f.Victim.GetPart<Body>());CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);f.Dead(f.Attacker);}}
        [TestCase(false)] [TestCase(true)]
        public void ZoneNullBodyIsAnatomyOnlyButDirectDeathCommits(bool direct)
        {using(var f=new MortalDeathFixture()){if(direct)CombatSystem.HandleDeath(f.Victim,f.Attacker,null);else Assert.IsTrue(f.Cut(f.Head,f.Attacker,withoutZone:true));Assert.AreEqual(direct?1:0,f.Observer.Died);Assert.AreEqual(direct?0:100,f.Victim.GetStat("Hitpoints").BaseValue);Assert.AreEqual(direct,CombatSystem.IsDeathHandled(f.Victim));Assert.IsNotNull(f.Zone.GetEntityCell(f.Victim));}}
        [TestCase(false)] [TestCase(true)]
        public void PositiveComputedHpCannotReenableCommittedCombatProducer(bool axe)
        {using(var f=new MortalDeathFixture()){var ctx=f.AxeContext();f.Victim.GetStat("Hitpoints").Bonus=10;CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);Assert.Greater(f.Victim.GetStatValue("Hitpoints"),0);int cuts=f.Observer.AfterCuts;var rng=new MortalDeathRng();ctx.Rng=rng;if(axe)SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);else f.CombatCut(f.Head,50,f.Attacker,rng);Assert.AreEqual(0,rng.Calls);Assert.AreEqual(cuts,f.Observer.AfterCuts);Assert.IsNotNull(f.Head.ParentPart);Assert.IsNull(f.Victim.GetEffect<BleedingEffect>());Assert.AreEqual(1,f.Observer.Died);}}
        [Test]
        public void EarlierOrdinaryDamageDeathSuppressesAxeBeforeItsRng()
        {using(var f=new MortalDeathFixture()){var ctx=f.AxeContext();CombatSystem.ApplyDamage(f.Victim,new Damage(200),f.Attacker,f.Zone);var rng=new MortalDeathRng();ctx.Rng=rng;SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);Assert.AreEqual(0,rng.Calls);Assert.AreEqual(0,f.Observer.AfterCuts);Assert.IsNull(f.Victim.GetEffect<BleedingEffect>());f.Dead(f.Attacker);}}
        [TestCase("no_damage")] [TestCase("wrong_attribute")] [TestCase("chance")]
        public void RejectedAxeAttemptsPreserveVictimAndEffectState(string mode)
        {using(var f=new MortalDeathFixture()){var ctx=f.AxeContext();var rng=new MortalDeathRng{Roll=mode=="chance"?99:0};ctx.Rng=rng;if(mode=="no_damage")ctx.ActualDamage=0;if(mode=="wrong_attribute")ctx.Damage=new Damage(1);SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);Assert.AreEqual(mode=="chance"?1:0,rng.Calls);Assert.AreEqual(99,f.Victim.GetStatValue("Hitpoints"));Assert.AreEqual(0,f.Observer.BeforeCuts);Assert.IsNull(f.Victim.GetEffect<BleedingEffect>());}}
        [TestCase(false)] [TestCase(true)]
        public void NonmortalPostCutDeathSkipsLateAxeBleedAndKeepsFirstKiller(bool kill)
        {using(var f=new MortalDeathFixture()){var ctx=f.AxeContext(false);var source=f.Scope.Item("Villager");source.ID=Guid.NewGuid().ToString("N");f.Observer.Hook=e=>{if(kill&&e.ID=="AfterDismember")CombatSystem.ApplyDamage(f.Victim,new Damage(200),source,f.Zone);};SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);Assert.AreEqual(1,f.Observer.AfterCuts);Assert.AreEqual(kill,f.Victim.GetEffect<BleedingEffect>()==null);if(kill){f.Dead(source);Assert.AreEqual(0,f.Attacker.GetStatValue("Experience"));}else Assert.AreEqual(99,f.Victim.GetStatValue("Hitpoints"));}}
        [TestCase(false)] [TestCase(true)]
        public void CombatPermissionCallbackDeathCannotStartAnotherCut(bool kill)
        {using(var f=new MortalDeathFixture()){f.Observer.Hook=e=>{if(kill&&e.ID=="CanBeDismembered")CombatSystem.ApplyDamage(f.Victim,new Damage(200),f.Attacker,f.Zone);};var head=f.Head;f.CombatCut(head,50,f.Attacker,new MortalDeathRng());Assert.AreEqual(kill?0:1,f.Observer.BeforeCuts);Assert.AreEqual(kill,head.ParentPart!=null);f.Dead(f.Attacker);}}
        [Test]
        public void RecursiveSameVictimDeathKeepsOneCallbackAndFirstCredit()
        {using(var f=new MortalDeathFixture()){f.Observer.Hook=e=>{if(e.ID=="Died")CombatSystem.HandleDeath(f.Victim,f.Victim,f.Zone);};Assert.IsTrue(f.Cut(f.Head,f.Attacker));f.Dead(f.Attacker);Assert.AreEqual(10,f.Attacker.GetStatValue("Experience"));}}
        [Test]
        public void NestedIndependentVictimHasItsOwnDeathGuard()
        {using(var f=new MortalDeathFixture()){var other=HaulLifecycleFixture.MakeActor("other victim");var observe=new MortalDeathObserver();other.AddPart(observe);Assert.IsTrue(f.Zone.AddEntity(other,6,5));f.Observer.Hook=e=>{if(e.ID=="Died")CombatSystem.HandleDeath(other,f.Victim,f.Zone);};Assert.IsTrue(f.Cut(f.Head,f.Attacker));f.Dead(f.Attacker);Assert.AreEqual(1,observe.Died);Assert.AreSame(f.Victim,observe.Killer);Assert.IsTrue(observe.Committed);Assert.AreEqual(0,other.GetStat("Hitpoints").BaseValue);Assert.IsNull(f.Zone.GetEntityCell(other));}}
        [TestCase(false)] [TestCase(true)]
        public void RealCorpsePreservesSourceMetadataUnlessSuppressed(bool suppress)
        {
            using(var f=new MortalDeathFixture())
            {
                var factory=ContentFactory();CorpsePart.Factory=factory;var victim=factory.CreateEntity("Villager");victim.ID=Guid.NewGuid().ToString("N");var corpse=victim.GetPart<CorpsePart>();Assert.AreEqual(100,corpse.CorpseChance);Assert.AreEqual("CreatureCorpse",corpse.CorpseBlueprint);if(suppress)victim.Tags["SuppressCorpseDrops"]="";
                Assert.IsTrue(f.Zone.AddEntity(victim,7,5));Assert.IsTrue(victim.GetPart<Body>().Dismember(victim.GetPart<Body>().GetPartsByType("Head").Single(),f.Zone,f.Attacker));
                var drops=f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="CreatureCorpse"&&e.GetProperty("SourceID")==victim.ID).ToList();Assert.AreEqual(suppress?0:1,drops.Count);if(!suppress){Assert.AreEqual(f.Attacker.ID,drops[0].GetProperty("KillerID"));Assert.AreEqual(f.Attacker.BlueprintName,drops[0].GetProperty("KillerBlueprint"));Assert.AreEqual((7,5),f.Zone.GetEntityPosition(drops[0]));}
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualFactionDeathCreditsOnlyPlayerSource(bool playerSource)
        {using(var f=new MortalDeathFixture()){var victim=f.Scope.Item("Villager");victim.ID=Guid.NewGuid().ToString("N");var gives=victim.GetPart<GivesRepPart>();Assert.AreEqual(10,gives.Value);string faction=FactionManager.GetFaction(victim);Assert.AreEqual("Villagers",faction);PlayerReputation.Set(faction,0);Assert.IsTrue(f.Zone.AddEntity(victim,7,5));var killer=playerSource?f.Attacker:f.Victim;Assert.IsTrue(victim.GetPart<Body>().Dismember(victim.GetPart<Body>().GetPartsByType("Head").Single(),f.Zone,killer));Assert.AreEqual(playerSource?-10:0,PlayerReputation.Get(faction));CombatSystem.HandleDeath(victim,f.Attacker,f.Zone);Assert.AreEqual(playerSource?-10:0,PlayerReputation.Get(faction));}}
        [TestCase(false)] [TestCase(true)]
        public void PassiveKillerIsExcludedFromActualWitnessEffect(bool sourced)
        {using(var f=new MortalDeathFixture()){var killer=f.Scope.Item("SummitSinger");killer.ID=Guid.NewGuid().ToString("N");var witness=f.Scope.Item("SummitSinger");witness.ID=Guid.NewGuid().ToString("N");Assert.IsTrue(killer.GetPart<BrainPart>().Passive);Assert.IsTrue(witness.GetPart<BrainPart>().Passive);Assert.IsTrue(f.Zone.AddEntity(killer,6,5));Assert.IsTrue(f.Zone.AddEntity(witness,6,6));Assert.IsTrue(f.Cut(f.Head,sourced?killer:null));Assert.NotNull(witness.GetEffect<WitnessedEffect>());Assert.AreEqual(sourced,killer.GetEffect<WitnessedEffect>()==null);}}
        [TestCase("")] [TestCase("NoDropOnDeath")] [TestCase("Temporary")]
        public void DeathDropPolicyDoesNotSuppressCommittedState(string tag)
        {using(var f=new MortalDeathFixture()){var gear=f.Scope.EquipDuelistBuckler();if(tag!="")f.Victim.Tags[tag]="";Assert.IsTrue(f.Cut(f.Head,f.Attacker));f.Dead(f.Attacker);if(tag=="")f.Scope.Ground(gear);else{Assert.AreSame(f.Victim,gear.GetPart<PhysicsPart>().Equipped);Assert.IsNull(f.Zone.GetEntityCell(gear));Assert.AreEqual(16,f.Victim.GetStatValue("Agility"));}}}
        [Test]
        public void DirectDeadBodyApiStillDetachesWithoutSecondDeath()
        {using(var f=new MortalDeathFixture()){CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);var arm=f.Scope.LeftArm;Assert.IsTrue(f.Body.Dismember(arm,f.Zone,f.Victim));Assert.IsNull(arm.ParentPart);Assert.AreEqual(1,f.Observer.Died);Assert.AreSame(f.Attacker,f.Observer.Killer);Assert.AreEqual(10,f.Attacker.GetStatValue("Experience"));}}
        [TestCase(9)] [TestCase(10)]
        public void FullOrdinaryMeleeThreadsSourceOnlyAtMortalThreshold(int damage)
        {
            using(var f=new MortalDeathFixture())
            {
                MortalDeathFixture.Stat(f.Attacker,"Strength",16,100);MortalDeathFixture.Stat(f.Attacker,"Agility",16,100);MortalDeathFixture.Stat(f.Victim,"Agility",16,100);MortalDeathFixture.Stat(f.Victim,"Hitpoints",20,20);f.Victim.AddPart(new ArmorPart{AV=6});
                var weapon=f.Scope.Item("Battleaxe");Assert.IsTrue(f.Attacker.GetPart<InventoryPart>().AddObject(weapon));Assert.IsTrue(InventorySystem.Equip(f.Attacker,weapon));Assert.AreEqual(6,CombatSystem.GetDV(f.Victim));Assert.AreEqual(6,CombatSystem.GetPartAV(f.Victim,f.Head));
                Assert.IsFalse(Axe_Decapitate.ShouldDecapitate(f.Attacker));Assert.IsNull(f.Attacker.GetPart<SkillsPart>().SkillList.OfType<Axe_Dismember>().FirstOrDefault());
                int offset=f.Body.GetParts().TakeWhile(p=>!ReferenceEquals(p,f.Head)).Where(p=>!p.Abstract&&p.TargetWeight>0).Sum(p=>p.TargetWeight);Assert.Greater(f.Head.TargetWeight,0);
                var head=f.Head;int beforeCut=-1;f.Observer.Hook=e=>{if(e.ID=="BeforeDismember")beforeCut=f.Victim.GetStatValue("Hitpoints");};
                var rng=new MortalMeleeRng(offset,damage==10?5:4,damage==10);Assert.AreSame(head,CombatSystem.SelectHitLocation(f.Body,new MortalDeathRng{Roll=offset}));
                Assert.IsTrue(CombatSystem.PerformMeleeAttack(f.Attacker,f.Victim,f.Zone,rng));Assert.AreEqual(3,rng.PenCalls);Assert.AreEqual(2,rng.DamageCalls);
                if(damage==10){Assert.AreEqual(10,beforeCut);Assert.IsNull(head.ParentPart);f.Dead(f.Attacker);Assert.AreEqual(10,f.Attacker.GetStatValue("Experience"));}else{Assert.AreEqual(11,f.Victim.GetStatValue("Hitpoints"));Assert.IsNotNull(head.ParentPart);Assert.AreEqual(0,f.Observer.Died);Assert.AreEqual(0,f.Attacker.GetStatValue("Experience"));}
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualGrenadeCreatorPoisonSelfDamageKeepsCommittedDeathUnhealed(bool lethal)
        {
            using(var f=new MortalDeathFixture())using(var gases=new MortalGasScope())
            {
                var player=f.Attacker;Assert.AreEqual(10,player.GetStatValue("XPValue"));player.GetStat("Hitpoints").BaseValue=lethal?3:20;player.GetStat("Experience").BaseValue=105;
                var observe=new MortalDeathObserver();player.AddPart(observe);var grenade=f.Scope.Item("PoisonGasGrenade");var part=grenade.GetPart<GasGrenadePart>();Assert.AreEqual("poison-vapor",part.GasId);Assert.AreEqual(9,part.Detonate(player,f.Zone.GetEntityCell(player),f.Zone));
                var gas=f.Zone.GetEntityCell(player).Objects.Single(e=>e.GetPart<GasPoolPart>()!=null);Assert.AreSame(player,gas.GetPart<GasPoolPart>().Creator);Assert.IsTrue(gas.GetPart<GasPoisonPart>().ApplyGas(player,f.Zone));
                Assert.AreEqual(lethal,CombatSystem.IsDeathHandled(player));Assert.AreEqual(lethal?2:1,player.GetStatValue("Level"));Assert.AreEqual(lethal?0:105,player.GetStatValue("Experience"));Assert.AreEqual(lethal?0:15,player.GetStat("Hitpoints").BaseValue);Assert.AreEqual(lethal?1:0,observe.Died);if(lethal){Assert.AreSame(player,observe.Killer);Assert.AreEqual(0,observe.BaseAtDeath);}else Assert.NotNull(player.GetEffect<PoisonedByGasEffect>());
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void ModalAlsoRecognizesMissingHpAndPositiveMinimum(bool missing)
        {using(var f=new HotbarSaveFixture(true,false)){var player=f.Input.PlayerEntity;if(missing)player.Statistics.Remove("Hitpoints");else player.GetStat("Hitpoints").Min=5;CombatSystem.HandleDeath(player,null,f.Input.CurrentZone);typeof(InputHandler).GetMethod("Update",HotbarSaveFixture.Flags).Invoke(f.Input,null);Assert.IsTrue(((DeathScreenController)HotbarSaveFixture.Get(f.Input,"_deathScreenController")).IsActive);if(missing)Assert.IsNull(player.GetStat("Hitpoints"));else{Assert.AreEqual(5,player.GetStatValue("Hitpoints"));Assert.AreEqual(5,player.GetStat("Hitpoints").Min);}}}
        [TestCase(false)] [TestCase(true)]
        public void ActualMissingMpIsStillAbsentAfterLivingOrDeadLevelUp(bool dead)
        {using(var f=new MortalDeathFixture()){var p=f.Attacker;Assert.IsNull(p.GetStat("MP"));Assert.NotNull(p.GetStat("SP"));p.GetStat("Experience").BaseValue=105;int sp=p.GetStatValue("SP");if(dead)CombatSystem.HandleDeath(p,p,f.Zone);else LevelingSystem.AwardKillXP(p,f.Victim,f.Zone);Assert.IsNull(p.GetStat("MP"));Assert.AreEqual(sp+1,p.GetStatValue("SP"));Assert.AreEqual(2,p.GetStatValue("Level"));}}
        [Test]
        public void CommittedPredicateIsPureAndDoesNotInferDeathFromZeroHp()
        {
            using(var f=new MortalDeathFixture())
            {Assert.IsFalse(CombatSystem.IsDeathHandled(null));f.Victim.GetStat("Hitpoints").BaseValue=0;int tags=f.Victim.Tags.Count;Assert.IsFalse(CombatSystem.IsDeathHandled(f.Victim));Assert.AreEqual(tags,f.Victim.Tags.Count);CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);Assert.IsTrue(CombatSystem.IsDeathHandled(f.Victim));f.Victim.Statistics.Remove("Hitpoints");Assert.IsTrue(CombatSystem.IsDeathHandled(f.Victim));Assert.IsNull(f.Victim.GetStat("Hitpoints"));}
        }
        [Test]
        public void FirstDeathMessageSeesCommittedNormalizedStateAndCannotStealCredit()
        {
            using(var f=new MortalDeathFixture())
            {
                int messages=0,baseSeen=99;bool committed=false;var old=MessageLog.OnMessage;
                try{MessageLog.OnMessage=text=>{if(!text.Contains(" is killed by "))return;messages++;baseSeen=f.Victim.GetStat("Hitpoints").BaseValue;committed=CombatSystem.IsDeathHandled(f.Victim);CombatSystem.HandleDeath(f.Victim,f.Victim,f.Zone);};Assert.IsTrue(f.Cut(f.Head,f.Attacker));}
                finally{MessageLog.OnMessage=old;}
                Assert.AreEqual(1,messages);Assert.AreEqual(0,baseSeen);Assert.IsTrue(committed);f.Dead(f.Attacker);Assert.AreEqual(10,f.Attacker.GetStatValue("Experience"));
            }
        }
        [Test]
        public void MissingHpAxeCompatibilityDoesNotTreatAbsenceAsDeath()
        {using(var f=new MortalDeathFixture()){var ctx=f.AxeContext(false);f.Victim.Statistics.Remove("Hitpoints");SkillEventDispatcher.AttackerAfterAttack(f.Attacker,ctx);Assert.AreEqual(1,f.Observer.AfterCuts);Assert.NotNull(f.Victim.GetEffect<BleedingEffect>());Assert.IsFalse(CombatSystem.IsDeathHandled(f.Victim));}}
        [Test]
        public void MortalPostCutIndependentDeathWinsBeforeOuterSourceCommits()
        {using(var f=new MortalDeathFixture()){var source=f.Scope.Item("Villager");source.ID=Guid.NewGuid().ToString("N");f.Observer.Hook=e=>{if(e.ID=="AfterDismember")CombatSystem.ApplyDamage(f.Victim,new Damage(200),source,f.Zone);};Assert.IsTrue(f.Cut(f.Head,f.Attacker));f.Dead(source);Assert.AreEqual(1,f.Observer.AfterCuts);Assert.AreEqual(0,f.Attacker.GetStatValue("Experience"));}}
        [TestCase(false)] [TestCase(true)]
        public void RealPaleSaltKillPrecedesCachedSurvivorAxeDispatch(bool matches)
        {
            using(var f=new MortalDeathFixture())using(var registry=new MortalEnhancementScope())
            {
                var victim=f.Scope.Item("SkeletalSentry");victim.ID=Guid.NewGuid().ToString("N");var material=victim.GetPart<MaterialPart>();Assert.IsTrue(material.HasMaterialTag("Undead"));if(!matches)Assert.IsTrue(material.MaterialTags.Remove("Undead"));
                Assert.IsTrue(f.Zone.AddEntity(victim,4,6));MortalDeathFixture.Stat(victim,"Hitpoints",13,13);MortalDeathFixture.Stat(victim,"Agility",16,100);victim.GetPart<ArmorPart>().AV=6;victim.GetPart<ArmorPart>().DV=0;
                MortalDeathFixture.Stat(f.Attacker,"Strength",16,100);MortalDeathFixture.Stat(f.Attacker,"Agility",16,100);Assert.AreEqual(6,CombatSystem.GetDV(victim));Assert.AreEqual(6,CombatSystem.GetPartAV(victim,victim.GetPart<Body>().GetBody()));
                var skills=f.Attacker.GetPart<SkillsPart>();Assert.IsTrue(skills.AddSkill(new Axe_Dismember(),"GA03h"));Assert.IsTrue(skills.AddSkill(new Axe_Decapitate(),"GA03h"));
                var weapon=f.Scope.Item("Battleaxe");Assert.IsTrue(new PaleSaltTinkerModification().Apply(weapon,out var reason),reason);Assert.AreEqual(4,weapon.GetPart<EnhancementPaleSalt>().BonusDamage);Assert.IsTrue(f.Attacker.GetPart<InventoryPart>().AddObject(weapon));Assert.IsTrue(InventorySystem.Equip(f.Attacker,weapon));
                var observation=new MortalDeathObserver();victim.AddPart(observation);var amounts=new List<int>();var hp=new List<int>();var sourceObserver=new MortalDeathObserver{Hook=e=>{if(e.ID=="DamageDealt"){amounts.Add(e.GetIntParameter("Amount"));hp.Add(victim.GetStatValue("Hitpoints"));}}};f.Attacker.AddPart(sourceObserver);
                var head=victim.GetPart<Body>().GetPartsByType("Head").Single();var rng=new MortalEnhancementRng();Assert.IsTrue(CombatSystem.PerformMeleeAttack(f.Attacker,victim,f.Zone,rng));
                CollectionAssert.AreEqual(matches?new[]{10,4}:new[]{10},amounts);Assert.AreEqual(3,hp[0]);Assert.AreEqual(matches?2:4,rng.SingleCalls);Assert.AreEqual(matches?0:1,observation.AfterCuts);Assert.AreEqual(matches,head.ParentPart!=null);
                Assert.AreEqual(1,observation.Died);Assert.AreSame(f.Attacker,observation.Killer);Assert.IsTrue(CombatSystem.IsDeathHandled(victim));Assert.IsNull(victim.GetEffect<BleedingEffect>());Assert.IsNull(f.Zone.GetEntityCell(victim));
            }
        }
        [TestCase("none",0,false)] [TestCase("none",10,false)]
        [TestCase("target",0,false)] [TestCase("target",10,false)]
        [TestCase("source",0,false)] [TestCase("source",10,false)]
        [TestCase("none",0,true)] [TestCase("none",10,true)]
        public void SameMeleeCallStopsAtCommittedParticipantDeath(string killed,int bonus,bool allowOffhand)
        {
            using(var f=new MortalDeathFixture())
            {
                MortalDeathFixture.Stat(f.Attacker,"Strength",16,100);MortalDeathFixture.Stat(f.Attacker,"Agility",16,100);MortalDeathFixture.Stat(f.Victim,"Agility",16,100);f.Victim.AddPart(new ArmorPart{AV=6});
                f.Attacker.GetStat("Hitpoints").Max=200;f.Victim.GetStat("Hitpoints").Max=200;
                f.Attacker.GetStat("Hitpoints").Bonus=bonus;f.Victim.GetStat("Hitpoints").Bonus=bonus;
                var dagger=f.Scope.Item("Dagger");var sword=f.Scope.Item("ShortSword");var inv=f.Attacker.GetPart<InventoryPart>();Assert.IsTrue(inv.AddObject(dagger));Assert.IsTrue(inv.AddObject(sword));Assert.IsTrue(InventorySystem.Equip(f.Attacker,dagger));Assert.IsTrue(InventorySystem.Equip(f.Attacker,sword));
                Assert.AreEqual(2,inv.GetAllEquipped().Count);Assert.AreEqual(2,f.Attacker.GetPart<Body>().GetPartsByType("Hand").Count(p=>p._Equipped!=null));
                var beforeHp=f.Victim.GetStat("Hitpoints").BaseValue;int triggered=0,hits=0;f.Observer.Hook=e=>{if(killed=="source"&&e.ID=="TakeDamage"){triggered++;CombatSystem.HandleDeath(f.Attacker,f.Victim,f.Zone);}};
                var attackerProbe=new MortalDeathObserver{Hook=e=>{if(e.ID=="DamageDealt")hits++;if(killed=="target"&&e.ID=="DamageDealt"){triggered++;CombatSystem.HandleDeath(f.Victim,f.Attacker,f.Zone);}}};f.Attacker.AddPart(attackerProbe);
                var rng=new MortalOffhandRng{AllowOffhand=allowOffhand};Assert.IsTrue(CombatSystem.PerformMeleeAttack(f.Attacker,f.Victim,f.Zone,rng));Assert.AreEqual(killed=="none"?0:1,triggered);
                Assert.AreEqual(allowOffhand?2:1,hits);Assert.AreEqual(killed=="source"?0:killed=="target"?1:allowOffhand?3:2,rng.ChanceCalls,"Only living participants may continue existing hit/secondary dispatch.");
                if(killed=="none"){Assert.IsFalse(CombatSystem.IsDeathHandled(f.Attacker));Assert.IsFalse(CombatSystem.IsDeathHandled(f.Victim));Assert.Less(f.Victim.GetStat("Hitpoints").BaseValue,beforeHp);}
                else{var dead=killed=="source"?f.Attacker:f.Victim;Assert.IsTrue(CombatSystem.IsDeathHandled(dead));Assert.AreEqual(bonus,dead.GetStatValue("Hitpoints"));Assert.IsNull(f.Zone.GetEntityCell(dead));}
            }
        }
        internal static EntityFactory ContentFactory(){var f=new EntityFactory();f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));return f;}
    }
    public sealed class MortalMeleeRng : Random
    {
        readonly int _offset,_firstDie;readonly bool _cut;int _single;
        public int PenCalls,DamageCalls;
        public MortalMeleeRng(int offset,int firstDie,bool cut){_offset=offset;_firstDie=firstDie;_cut=cut;}
        public override int Next(int maxValue)
        {int index=_single++;int result=index==0?_offset:index==2&&_cut?0:99;Assert.That(result,Is.InRange(0,maxValue-1),"scripted integer roll");return result;}
        public override int Next(int minValue,int maxValue)
        {Assert.AreEqual(1,minValue);if(maxValue==21)return 10;if(maxValue==11){PenCalls++;Assert.LessOrEqual(PenCalls,3);return PenCalls==1?9:1;}if(maxValue==7){DamageCalls++;Assert.LessOrEqual(DamageCalls,2);return DamageCalls==1?_firstDie:5;}throw new InvalidOperationException("Unexpected dice range "+minValue+":"+maxValue);}
    }
    public sealed class MortalEnhancementRng : Random
    {
        int _pens;public int SingleCalls;
        public override int Next(int maxValue){int index=SingleCalls++;int value=index==1?99:0;Assert.That(value,Is.InRange(0,maxValue-1));return value;}
        public override int Next(int minValue,int maxValue){Assert.AreEqual(1,minValue);if(maxValue==21)return 10;if(maxValue==11)return ++_pens==1?9:1;if(maxValue==7)return 5;throw new InvalidOperationException("Unexpected combat dice");}
    }
    public sealed class MortalOffhandRng : Random
    {
        int _pens;public int ChanceCalls;public bool AllowOffhand;
        public override int Next(int maxValue){if(maxValue==100){ChanceCalls++;return AllowOffhand&&ChanceCalls==2?0:99;}return 0;}
        public override int Next(int minValue,int maxValue){Assert.AreEqual(1,minValue);if(maxValue==21)return 10;if(maxValue==11)return ++_pens%3==1?9:1;if(maxValue==5||maxValue==7)return 2;throw new InvalidOperationException("Unexpected secondary combat die "+maxValue);}
    }
    internal sealed class MortalEnhancementScope : IDisposable
    {
        const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
        readonly Dictionary<string,Type> _classes=(Dictionary<string,Type>)typeof(EnhancementFactory).GetField("_byClassName",Flags).GetValue(null);
        readonly Dictionary<string,Type> _names=(Dictionary<string,Type>)typeof(EnhancementFactory).GetField("_byDisplayName",Flags).GetValue(null);
        readonly Dictionary<string,Type> _oldClasses,_oldNames;readonly object _initialized=typeof(EnhancementFactory).GetField("_initialized",Flags).GetValue(null);
        public MortalEnhancementScope(){_oldClasses=new Dictionary<string,Type>(_classes);_oldNames=new Dictionary<string,Type>(_names);EnhancementFactory.Register(typeof(EnhancementPaleSalt));}
        public void Dispose(){_classes.Clear();foreach(var p in _oldClasses)_classes.Add(p.Key,p.Value);_names.Clear();foreach(var p in _oldNames)_names.Add(p.Key,p.Value);typeof(EnhancementFactory).GetField("_initialized",Flags).SetValue(null,_initialized);}
    }
    internal sealed class MortalGasScope : IDisposable
    {
        const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic;
        readonly Dictionary<string,GasDefinition> _dict=(Dictionary<string,GasDefinition>)typeof(GasRegistry).GetField("_byId",Flags).GetValue(null);
        readonly Dictionary<string,GasDefinition> _old;readonly bool _initialized=GasRegistry.IsInitialized;
        readonly Random _rng=GasPoisonPart.TestRng;
        public MortalGasScope(){_old=new Dictionary<string,GasDefinition>(_dict);GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));GasPoisonPart.TestRng=new MortalDeathRng();}
        public void Dispose(){_dict.Clear();foreach(var pair in _old)_dict.Add(pair.Key,pair.Value);typeof(GasRegistry).GetField("_initialized",Flags).SetValue(null,_initialized);GasPoisonPart.TestRng=_rng;}
    }
}
