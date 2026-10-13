using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ComposableRiposteAdversarialTests
    {
        FiftySecondCombatFixture f;Entity a,d;MeleeWeaponPart attack,sword;
        [SetUp] public void Setup()
        {
            f=new FiftySecondCombatFixture();a=f.Actor(x:11);d=f.Actor(new LongBlades_Deflect());
            FiftySecondCombatFixture.Learn(d,new LongBlades_Riposte());
            attack=BladeDefense.FindLongBlade(a);sword=BladeDefense.FindLongBlade(d);
            attack.Attributes="TestWeapon";attack.PenBonus=3;sword.PenBonus=3;
        }
        [TearDown] public void Cleanup()=>f.Dispose();
        public sealed class MutableRolls : Random
        {
            public int PercentDraws,HitDraws;
            public Action<int> OnPercent;
            public override int Next(int max)
            { if(max==100){PercentDraws++;OnPercent?.Invoke(PercentDraws);}return 0; }
            public override int Next(int min,int max) {if(max==21){HitDraws++;return 10;}return min;}
        }
        [TestCase(true)] [TestCase(false)] public void OrdinaryShieldOrEmptyOffhandRetainsItsNaturalSwing(bool shield)
        {
            var actor=f.Factory.CreateEntity("Player");Assert.True(f.Zone.AddEntity(actor,20,10));var target=f.Target(21,10);
            var weapon=f.Factory.CreateEntity("Dagger");Assert.True(actor.GetPart<InventoryPart>().AddObject(weapon));Assert.True(InventorySystem.Equip(actor,weapon));
            if(shield){var buckler=f.Factory.CreateEntity("Buckler");Assert.True(actor.GetPart<InventoryPart>().AddObject(buckler));Assert.True(InventorySystem.Equip(actor,buckler));}
            actor.Statistics["MultiWeaponSkillBonus"]=new Stat{Name="MultiWeaponSkillBonus",BaseValue=100};target.Statistics["DV"]=new Stat{Name="DV",BaseValue=1000};
            var rolls=new MutableRolls();Assert.True(CombatSystem.PerformMeleeAttack(actor,target,f.Zone,rolls));Assert.AreEqual(2,rolls.HitDraws,"Primary dagger and unchanged natural offhand both make ordinary accuracy rolls.");
        }
        MeleeStrikeResult Strike(Random rng,MeleeStrikeProvenance p=default)
            => CombatSystem.PerformSingleAttack(a,d,attack,true,f.Zone,rng,provenance:p);
        [TestCase("normal",true)] [TestCase("disarm",false)] [TestCase("move",false)]
        [TestCase("stun",false)] [TestCase("remove",false)] [TestCase("dead",false)]
        [TestCase("skill",false)] [TestCase("destroy",false)]
        public void Adversarial_RevalidateAfterProcBeforeSpending(string change,bool issued)
        {
            var rng=new MutableRolls{OnPercent=n=>{if(n!=2)return;switch(change)
            {
                case "disarm":sword.ParentEntity.GetPart<PhysicsPart>().Equipped=null;break;
                case "move":f.Zone.MoveEntity(a,15,10);break;
                case "stun":d.ApplyEffect(new StunnedEffect());break;
                case "remove":f.Zone.RemoveEntity(a);break;
                case "dead":a.GetStat("Hitpoints").BaseValue=0;break;
                case "skill":d.GetPart<SkillsPart>().RemoveSkill(d.GetPart<LongBlades_Riposte>());break;
                case "destroy":sword.ParentEntity.AddPart(new DestructiblePart{HP=0,MaxHP=10});break;
            }}};
            Assert.AreEqual(MeleeStrikeOutcome.FullParry,Strike(rng).Outcome);
            Assert.AreEqual(issued,d.GetPart<RiposteAllowancePart>().Spent);
            Assert.AreEqual(issued?2:1,rng.HitDraws);
        }
        [Test] public void Adversarial_ProcExceptionReleasesReservationAndScope()
        {
            var rng=new MutableRolls{OnPercent=n=>{if(n==2)throw new InvalidOperationException("test proc");}};
            Assert.Throws<InvalidOperationException>(()=>Strike(rng));
            Assert.False(MeleeStrikeScope.IsResolving);Assert.False(d.GetPart<RiposteAllowancePart>().Spent);
            Strike(new MutableRolls());Assert.True(d.GetPart<RiposteAllowancePart>().Spent);
        }
        [TestCase("normal",true)] [TestCase("dead",false)] [TestCase("remote",false)] [TestCase("stunned",false)]
        public void Adversarial_LegacyRejoinderRevalidatesAfterEarlierReaction(string change,bool issues)
        {
            sword.Attributes="Piercing";
            if(change=="dead")a.GetStat("Hitpoints").BaseValue=0;
            if(change=="remote")f.Zone.MoveEntity(a,15,10);
            if(change=="stunned")d.ApplyEffect(new StunnedEffect());
            var rng=new MutableRolls();
            new ShortBlades_Rejoinder().OnDefenderAfterAttackMissed(new SkillEventContext{Attacker=a,Defender=d,Zone=f.Zone,Rng=rng});
            Assert.AreEqual(issues?1:0,rng.HitDraws);
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_ReactionAncestorSurvivesExtraAttack(bool reactive)
        {
            var p=MeleeStrikeProvenance.Action(MeleeStrikeOrigin.Ordinary).Child(MeleeStrikeOrigin.Ordinary);
            if(reactive)p=p.Child(MeleeStrikeOrigin.ReactiveCounter);
            p=p.Child(MeleeStrikeOrigin.ExtraAttack);var rng=new MutableRolls();Strike(rng,p);
            Assert.AreEqual(!reactive,d.GetPart<RiposteAllowancePart>().Spent);
            Assert.AreEqual(reactive?1:2,rng.HitDraws);
        }
        [Test] public void Adversarial_CounterDamageRetainsTypedParentIdentity()
        {
            MeleeStrikeProvenance counter=default;
            d.AddPart(new ComposableBladeAdversarialTests.DamageProbe{Callback=e=>{
                if(e.ID=="DamageDealt")counter=e.GetParameter<Damage>("Damage").Provenance;
            }});
            var incoming=Strike(new MutableRolls()).Provenance;
            Assert.AreEqual(incoming.ActionId,counter.ActionId);Assert.AreEqual(incoming.StrikeId,counter.ParentStrikeId);
            Assert.AreEqual(incoming.RootStrikeId,counter.RootStrikeId);Assert.AreEqual(incoming.Depth+1,counter.Depth);
            Assert.AreEqual(MeleeStrikeOrigin.ReactiveCounter,counter.Origin);Assert.True(counter.SuppressFollowups);
        }
        [Test] public void Adversarial_StrikeDiagnosticsUseOwnCause()
        {
            bool prior=CavesOfOoo.Diagnostics.Diag.DetailedCaptureEnabled;
            try
            {
                CavesOfOoo.Diagnostics.Diag.DetailedCaptureEnabled=true;
                var p=Strike(new MutableRolls()).Provenance;
                var records=CavesOfOoo.Diagnostics.Diag.Snapshot(100).Where(e=>e.Kind=="StrikeStarted" && e.PayloadJson.Contains(p.ActionId)).ToArray();
                Assert.AreEqual(2,records.Length);
                foreach(var record in records)StringAssert.Contains("\"StrikeId\":\""+record.CauseTraceId+"\"",record.PayloadJson);
            }
            finally {CavesOfOoo.Diagnostics.Diag.DetailedCaptureEnabled=prior;}
        }
        [TestCase("stun")] [TestCase("disarm")] [TestCase("move")] [TestCase("kill")]
        public void Adversarial_CounterStopsInvalidatedRemainingFlurry(string change)
        {
            attack.Attributes="Piercing";var flurry=new ShortBlades_Flurry();FiftySecondCombatFixture.Learn(a,flurry);
            if(change=="stun")sword.OnHitEffectsRaw="Stunned,100,,3,1";
            if(change=="kill")a.GetStat("Hitpoints").BaseValue=1;
            d.AddPart(new ComposableBladeAdversarialTests.DamageProbe{Callback=e=>{
                if(e.ID!="DamageDealt")return;
                if(change=="disarm")attack.ParentEntity.GetPart<PhysicsPart>().Equipped=null;
                if(change=="move")f.Zone.MoveEntity(a,15,10);
            }});
            var rng=new MutableRolls();Assert.True(f.Cast(a,flurry,f.Zone.GetEntityCell(d),rng:rng));
            Assert.AreEqual(2,rng.HitDraws);Assert.AreEqual(1000,d.GetStatValue("Hitpoints"));
        }
        [Test] public void Adversarial_FlurrySharesActionButNotStrikeIdentity()
        {
            d.GetPart<SkillsPart>().RemoveSkill(d.GetPart<LongBlades_Deflect>());attack.Attributes="Piercing";
            var flurry=new ShortBlades_Flurry();FiftySecondCombatFixture.Learn(a,flurry);
            var records=new List<MeleeStrikeProvenance>();
            a.AddPart(new ComposableBladeAdversarialTests.DamageProbe{Callback=e=>{
                if(e.ID=="DamageDealt")records.Add(e.GetParameter<Damage>("Damage").Provenance);
            }});
            Assert.True(f.Cast(a,flurry,f.Zone.GetEntityCell(d),rng:new ComposableRiposteTests.Rolls(99,99,99)));
            Assert.AreEqual(3,records.Count);Assert.AreEqual(1,records.Select(p=>p.ActionId).Distinct().Count());
            Assert.AreEqual(3,records.Select(p=>p.StrikeId).Distinct().Count());
            Assert.True(records.All(p=>p.Origin==MeleeStrikeOrigin.ActiveAbility));
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_OwnerBoundaryRequiresScheduledEnergy(bool enough)
        {
            var budget=d.GetPart<RiposteAllowancePart>();budget.Spent=true;
            var turns=new TurnManager();turns.RestoreSavedState(0,true,d,new List<TurnManager.SavedTurnEntry>{
                new TurnManager.SavedTurnEntry{Entity=d,Energy=enough?1000:999}});
            int calls=0;d.AddPart(new ComposableBladeAdversarialTests.DamageProbe{Callback=e=>{
                if(e.ID!="EndTurn")return;calls++;turns.EndTurn(d,f.Zone);Assert.True(budget.Spent);
            }});
            turns.EndTurn(d,f.Zone);Assert.AreEqual(1,calls);Assert.AreEqual(!enough,budget.Spent);
        }
        [Test] public void Adversarial_SaveRequestsRefuseUnresolvedStrike()
        {
            bool? sync=null,async=null;
            var rng=new MutableRolls{OnPercent=n=>{if(n==2){sync=SaveGameService.QuickSave();async=SaveGameService.RequestQuickSave();}}};
            Strike(rng);Assert.AreEqual(false,sync);Assert.AreEqual(false,async);Assert.False(MeleeStrikeScope.IsResolving);
        }
        [TestCase(true)] [TestCase(false)] public void Adversarial_PrimaryBladePreferredThenBodyOrder(bool markPrimary)
        {
            var donor=MultiCellAbilityConsumerTests.ArmedActor("LongBlades");var second=BladeDefense.FindLongBlade(donor).ParentEntity;
            var hands=d.GetPart<Body>().GetBody().GetParts().Where(p=>p.Type=="Hand").ToArray();
            var first=hands.First(p=>p.Equipped==sword.ParentEntity);var other=hands.First(p=>p!=first);
            second.GetPart<PhysicsPart>().Equipped=null;
            d.GetPart<InventoryPart>().EquipToBodyPart(second,other);
            first.Primary=first.DefaultPrimary=false;other.DefaultPrimary=false;other.Primary=markPrimary;
            Assert.AreSame(markPrimary?second:sword.ParentEntity,BladeDefense.FindLongBlade(d).ParentEntity);
        }
    }
}
