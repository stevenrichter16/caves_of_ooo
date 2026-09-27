using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>One owned weakening contribution, including the live book/rite route.
    /// No cooldown resets, fabricated weapon source or save re-application.</summary>
    public sealed class WeakenedStackingTests
    {
        WeakenedAuditPresentationScope presentation;
        object oldData; bool oldInitialized; IDictionary resonance;
        Dictionary<object, object> oldEntries;
        [SetUp] public void Setup()
        {
            presentation=new WeakenedAuditPresentationScope();
            var type=typeof(ResonanceSystem);
            oldData=type.GetField("_data",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            oldInitialized=ResonanceSystem.IsInitialized;
            resonance=(IDictionary)type.GetField("_byElement",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            oldEntries=new Dictionary<object,object>();foreach(DictionaryEntry pair in resonance)oldEntries.Add(pair.Key,pair.Value);
            ResonanceSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Resonance/Resonance.json")));
        }
        [TearDown] public void Cleanup()
        {
            presentation?.Dispose(); presentation=null;
            if(oldEntries==null)return;
            resonance.Clear();foreach(var pair in oldEntries)resonance.Add(pair.Key,pair.Value);
            typeof(ResonanceSystem).GetField("_data",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,oldData);
            typeof(ResonanceSystem).GetField("<IsInitialized>k__BackingField",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,oldInitialized);
        }
        static Entity Actor(string id="target",int strength=16)
        {
            var e=new Entity{ID=id,BlueprintName="WeakenedAuditActor"};e.Tags["Creature"]="";
            e.Statistics["Strength"]=new Stat{Owner=e,Name="Strength",BaseValue=strength,Min=1,Max=100};
            e.Statistics["Hitpoints"]=new Stat{Owner=e,Name="Hitpoints",BaseValue=40,Min=0,Max=40};
            e.AddPart(new RenderPart{DisplayName=id});return e;
        }
        static WeakenedEffect Apply(Entity e,int penalty,int duration)
        {Assert.True(e.ApplyEffect(new WeakenedEffect(penalty,duration)));return e.GetEffect<WeakenedEffect>();}
        static void Tick(Entity e)
        {var end=GameEvent.New("EndTurn");try{e.FireEvent(end);}finally{end.Release();}}

        [TestCase(16,0,0,16,13,"you",4)]
        [TestCase(16,0,0,16,13,"Sella",4)]
        [TestCase(16,2,4,14,11,"you",4)]
        [TestCase(16,2,4,14,11,"Sella",4)]
        [TestCase(2,0,0,2,1,"you",4)]
        [TestCase(2,0,0,2,1,"Sella",4)]
        [TestCase(100,4,0,100,100,"you",4)]
        [TestCase(100,4,0,100,100,"Sella",4)]
        [TestCase(16,2,4,14,11,"you",-1)]
        [TestCase(16,2,4,14,11,"Sella",-1)]
        public void ActualStrengthFeedbackUsesEffectiveValuesAndPreservesIndependentModifiers(
            int basis,int bonus,int penalty,int before,int after,string name,int duration)
        {
            var target=Actor(name,basis);var strength=target.GetStat("Strength");
            strength.Bonus=bonus;strength.Penalty=penalty;
            Assert.AreEqual(before,strength.Value);
            string published=null;var previous=MessageLog.OnMessage;
            try
            {
                MessageLog.OnMessage=line=>published=line;
                Apply(target,3,duration);Assert.AreEqual(after,strength.Value);
                string applied=MessageLog.GetLast(),appliedUi=published;
                Assert.True(target.RemoveEffect<WeakenedEffect>());
                Assert.AreEqual(before,strength.Value);Assert.AreEqual(penalty,strength.Penalty);
                Assert.AreEqual(bonus,strength.Bonus);Assert.False(target.HasEffect<WeakenedEffect>());
                string subject=name=="you"?"You":name;
                string durationText=duration==-1?"until removed":"for 4 turns";
                string expectedApply=subject+(name=="you"?" are":" is")
                    +" weakened: Strength "+before+" to "+after+" "+durationText+".";
                string expectedRemove=subject+(name=="you"?" recover":" recovers")
                    +" from weakness: Strength "+before+".";
                CollectionAssert.AreEqual(new[]{expectedApply,expectedApply,expectedRemove,expectedRemove},
                    new[]{applied,appliedUi,MessageLog.GetLast(),published});
            }
            finally {MessageLog.OnMessage=previous;}
        }

        [TestCase(5)][TestCase(3)][TestCase(1)]
        public void ReapplicationKeepsOnlyStrongestOwnedPenalty(int incoming)
        {
            var target=Actor();var original=Apply(target,3,4);var next=new WeakenedEffect(incoming,2);
            Assert.True(target.ApplyEffect(next));int expected=Math.Max(3,incoming);
            Assert.AreSame(original,target.GetEffect<WeakenedEffect>());Assert.IsNull(next.Owner);
            Assert.AreEqual(1,target.GetPart<StatusEffectsPart>().EffectCount);
            Assert.AreEqual(expected,original.StrPenalty);Assert.AreEqual(expected,target.GetStat("Strength").Penalty);
            Assert.AreEqual(16-expected,target.GetStatValue("Strength"));
            Assert.True(target.RemoveEffect<WeakenedEffect>());Assert.AreEqual(16,target.GetStatValue("Strength"));Assert.Zero(target.GetStat("Strength").Penalty);
        }
        [TestCase(5)][TestCase(3)][TestCase(1)]
        public void RemovingAfterReapplicationPreservesIndependentPenaltyAndBonus(int incoming)
        {
            var target=Actor();var str=target.GetStat("Strength");str.Penalty=4;str.Bonus=2;
            Apply(target,3,4);Apply(target,incoming,4);Assert.True(target.RemoveEffect<WeakenedEffect>());
            Assert.AreEqual(4,str.Penalty);Assert.AreEqual(2,str.Bonus);Assert.AreEqual(14,str.Value);
            Assert.False(target.RemoveEffect<WeakenedEffect>());Assert.AreEqual(4,str.Penalty);
        }
        [TestCase(1,5,5)][TestCase(5,1,5)][TestCase(3,3,3)]
        [TestCase(-1,5,-1)][TestCase(5,-1,-1)]
        public void ReapplicationRetainsLongerDurationIncludingIndefinite(int oldDuration,int incoming,int expected)
        {var target=Actor();var original=Apply(target,2,oldDuration);Apply(target,2,incoming);Assert.AreEqual(expected,original.Duration);Assert.AreEqual(2,target.GetStat("Strength").Penalty);}
        [Test] public void ExpiryAfterUpgradeRestoresExactlyOnce()
        {
            var target=Actor();Apply(target,2,1);Apply(target,5,3);
            Tick(target);Assert.NotNull(target.GetEffect<WeakenedEffect>(),"Refreshed effect must survive the first tick.");Assert.AreEqual(2,target.GetEffect<WeakenedEffect>().Duration);Assert.AreEqual(11,target.GetStatValue("Strength"));
            Tick(target);Assert.NotNull(target.GetEffect<WeakenedEffect>());Tick(target);
            Assert.IsNull(target.GetEffect<WeakenedEffect>());Assert.Zero(target.GetStat("Strength").Penalty);Tick(target);Assert.AreEqual(16,target.GetStatValue("Strength"));
        }
        [TestCase(5)][TestCase(2)]
        public void SaveUpgradeAndRemovalRestoreOwnedPenaltyWithoutReapplying(int incoming)
        {
            var target=Actor();target.GetStat("Strength").Penalty=4;Apply(target,2,4);Apply(target,incoming,7);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(target);var effect=loaded.GetEffect<WeakenedEffect>();
            Assert.AreSame(loaded,effect.Owner);Assert.AreNotSame(target,loaded);Assert.AreEqual(Math.Max(2,incoming),effect.StrPenalty);
            Assert.AreEqual(4+Math.Max(2,incoming),loaded.GetStat("Strength").Penalty);Assert.AreEqual(7,effect.Duration);
            Assert.True(loaded.RemoveEffect<WeakenedEffect>());Assert.AreEqual(4,loaded.GetStat("Strength").Penalty);
            Assert.NotNull(target.GetEffect<WeakenedEffect>());
        }
        [Test] public void LoadedEffectCanUpgradeUsingItsRestoredOwner()
        {
            var target=Actor();Apply(target,2,4);var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(target);
            Apply(loaded,5,5);Assert.AreEqual(5,loaded.GetStat("Strength").Penalty);Assert.AreEqual(2,target.GetStat("Strength").Penalty);
            loaded.RemoveEffect<WeakenedEffect>();Assert.AreEqual(16,loaded.GetStatValue("Strength"));
        }
        [Test] public void MinimumClampedStrengthDoesNotLoseTheActualContribution()
        {var target=Actor(strength:2);Apply(target,3,4);Apply(target,8,4);Assert.AreEqual(1,target.GetStatValue("Strength"));target.RemoveEffect<WeakenedEffect>();Assert.AreEqual(2,target.GetStatValue("Strength"));Assert.Zero(target.GetStat("Strength").Penalty);}
        [Test] public void MissingStrengthRemainsANoOpAndRemovesWithoutThrowing()
        {var target=Actor();target.Statistics.Remove("Strength");Apply(target,2,4);Apply(target,5,6);Assert.True(target.RemoveEffect<WeakenedEffect>());Assert.IsNull(target.GetStat("Strength"));}
        [Test] public void IncomingActorAndUnrelatedEffectRemainUntouched()
        {
            var target=Actor();var other=Actor("other");Apply(target,2,4);Apply(other,7,2);var source=other.GetEffect<WeakenedEffect>();
            Assert.True(target.GetEffect<WeakenedEffect>().OnStack(source));Assert.AreEqual(7,other.GetStat("Strength").Penalty);Assert.AreSame(other,source.Owner);Assert.AreEqual(2,source.Duration);
            var before=target.GetStat("Strength").Penalty;Assert.False(target.GetEffect<WeakenedEffect>().OnStack(new BerserkEffect()));Assert.False(target.GetEffect<WeakenedEffect>().OnStack(null));Assert.AreEqual(before,target.GetStat("Strength").Penalty);
        }
        sealed class Veto : Part {public bool Enabled;public override string Name=>"WeakenedAuditVeto";public override bool HandleEvent(GameEvent e)=>!Enabled||e.ID!="BeforeApplyEffect";}
        [TestCase(true)][TestCase(false)]
        public void VetoedReapplicationDoesNotMutateTheExistingEffect(bool reject)
        {
            var target=Actor();var veto=new Veto();target.AddPart(veto);var effect=Apply(target,2,3);veto.Enabled=reject;
            Assert.AreEqual(!reject,target.ApplyEffect(new WeakenedEffect(5,8)));Assert.AreEqual(reject?2:5,target.GetStat("Strength").Penalty);Assert.AreEqual(reject?3:8,effect.Duration);
        }
        [Test] public void OnHitFactoryMagnitudeFeedsTheSameUpgradeContract()
        {
            var target=Actor();foreach(int magnitude in new[]{2,5,3})
            {var spec=OnHitEffectSpec.Parse("Weakened,100,,4,"+magnitude)[0];Assert.True(target.ApplyEffect(OnHitEffectFactory.Create(spec,null,new System.Random(1))));}
            Assert.AreEqual(5,target.GetStat("Strength").Penalty);target.RemoveEffect<WeakenedEffect>();Assert.AreEqual(16,target.GetStatValue("Strength"));
        }
        [TestCase(true)][TestCase(false)]
        public void ActualBookLearnedRiteRefreshesOnlyMarkedSurvivor(bool marked)
        {
            var factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            var zone=new Zone("WeakenedAudit");var caster=Actor("caster");caster.AddPart(new SkillsPart());caster.AddPart(new ActivatedAbilitiesPart());caster.AddPart(new InventoryPart{MaxWeight=150});Assert.True(zone.AddEntity(caster,10,10));
            var target=Actor();Assert.True(zone.AddEntity(target,11,10));var effect=Apply(target,2,1);if(marked)Assert.True(target.ApplyEffect(new WetEffect(1f),caster,zone));
            var book=factory.CreateEntity("SunderingWordGrimoire");Assert.NotNull(book);caster.GetPart<InventoryPart>().AddObject(book);
            var read=InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(book,"ReadGrimoire"),caster,zone);Assert.True(read.Success,read.ErrorMessage);
            var skill=caster.GetPart<Rites_SunderingWord>();Assert.NotNull(skill);var ability=caster.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);Assert.Zero(ability.CooldownRemaining);
            Assert.True(Cast(caster,zone));Assert.AreEqual(45,ability.CooldownRemaining);Assert.AreEqual(9,book.GetPart<GrimoireChargePart>().Charges);Assert.Greater(target.GetStatValue("Hitpoints"),0);
            Assert.AreSame(effect,target.GetEffect<WeakenedEffect>());Assert.AreEqual(marked?3:1,effect.Duration);Assert.AreEqual(2,target.GetStat("Strength").Penalty);
            Assert.False(Cast(caster,zone),"A real cooling ability must refuse; no test cooldown reset.");Assert.AreEqual(9,book.GetPart<GrimoireChargePart>().Charges);
        }
        static bool Cast(Entity caster,Zone zone)
        {var cmd=GameEvent.New("CommandSunderingWord");try{cmd.SetParameter("Zone",(object)zone);cmd.SetParameter("RNG",(object)new System.Random(7));cmd.SetParameter("SourceCell",(object)zone.GetEntityCell(caster));caster.FireEvent(cmd);return cmd.Handled;}finally{cmd.Release();}}
    }
    // These fixtures execute actual book/cast feedback. Preserve pre-existing
    // pending requests and announcements so editor play after tests sees no audit.
    internal sealed class WeakenedAuditPresentationScope : IDisposable
    {
        const BindingFlags Flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        readonly Queue<AsciiFxRequest> pending=(Queue<AsciiFxRequest>)typeof(AsciiFxBus).GetField("PendingRequests",Flags).GetValue(null);
        readonly Stack<AsciiFxRequest> pool=(Stack<AsciiFxRequest>)typeof(AsciiFxBus).GetField("Pool",Flags).GetValue(null);
        readonly List<SpellFxSequence> spells=(List<SpellFxSequence>)typeof(SpellFxBus).GetField("Pending",Flags).GetValue(null);
        readonly Queue<string> announcements=(Queue<string>)typeof(MessageLog).GetField("Announcements",Flags).GetValue(null);
        readonly List<Action> restore=new List<Action>();bool disposed;
        public WeakenedAuditPresentationScope()
        {
            var oldPending=pending.ToArray();var oldPool=pool.ToArray();var oldSpells=spells.ToArray();var oldAnnouncements=announcements.ToArray();
            restore.Add(()=>{pending.Clear();foreach(var p in oldPending)pending.Enqueue(p);pool.Clear();for(int i=oldPool.Length-1;i>=0;i--)pool.Push(oldPool[i]);spells.Clear();spells.AddRange(oldSpells);announcements.Clear();foreach(var p in oldAnnouncements)announcements.Enqueue(p);});
            foreach(string name in new[]{"Messages","Ticks","Serials"})
            {var list=(IList)typeof(MessageLog).GetField(name,Flags).GetValue(null);var copy=list.Cast<object>().ToArray();restore.Add(()=>{list.Clear();foreach(var x in copy)list.Add(x);});}
            Save(typeof(MessageLog),"NextSerial");Save(typeof(MessageLog),"FlashStamp");Save(typeof(AsciiFxBus),"<ClearVersion>k__BackingField");Save(typeof(SpellFxCapture),"_cosmeticSerial");
            var onMessage=MessageLog.OnMessage;restore.Add(()=>MessageLog.OnMessage=onMessage);MessageLog.OnMessage=null;
            pending.Clear();pool.Clear();spells.Clear();announcements.Clear();
        }
        void Save(Type type,string name){var field=type.GetField(name,Flags);var value=field.GetValue(null);restore.Add(()=>field.SetValue(null,value));}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var action in restore)action();}
    }

}
