using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>Pure core source-instance replacement witness. No factory, Unity
    /// objects or rendered scene; native execution separately verifies integration.</summary>
    public sealed class HotSteamReentryTests
    {
        bool oldEffectChannel,oldDamageChannel;
        [SetUp]public void Setup(){oldEffectChannel=Diag.IsChannelEnabled("effect");oldDamageChannel=Diag.IsChannelEnabled("damage");Diag.SetChannel("effect",true);Diag.SetChannel("damage",true);}
        [TearDown]public void Cleanup(){Diag.SetChannel("effect",oldEffectChannel);Diag.SetChannel("damage",oldDamageChannel);}
        static void AssertAttempt(Entity source,Entity actor,int count)
        {
            var records=DiagQuery.Apply(new DiagQuery.Filter{Category="effect",Kind="SteamScaldAttempt",Actor=source.ID,Target=actor.ID,Limit=20}).Records;
            Assert.AreEqual(count,records.Count);foreach(var record in records){Assert.That(record.PayloadJson,Does.Contain("\"requestedAmount\":2"));Assert.That(record.PayloadJson,Does.Not.Contain("outcome"));}
            Assert.AreEqual(0,DiagQuery.Apply(new DiagQuery.Filter{Category="effect",Kind="SteamScald",Actor=source.ID,Target=actor.ID,Limit=20}).Records.Count);
        }
        sealed class BeforeDamage : Part
        {
            public Action Before;public Func<GameEvent,bool> Gate;public int Calls;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="BeforeTakeDamage"){Calls++;Before?.Invoke();return Gate?.Invoke(e)??true;}return true;}
        }
        static Entity Source(Zone zone,int x)
        {
            var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="hot-steam-source-probe"};
            e.AddPart(new PhysicsPart());e.AddPart(new ThermalPart{Temperature=180,AmbientDecayRate=0});
            e.ApplyEffect(new SteamEffect(.8f));Assert.True(zone.AddEntity(e,x,10));return e;
        }
        static void Pulse(Zone zone,Entity source)
        {var e=GameEvent.New("BeginTakeAction");e.SetParameter("Zone",(object)zone);source.FireEventAndRelease(e);}
        [TestCase(false)][TestCase(true)]
        public void ReplacementEffectCannotReenterTheSameSourceButItsNextPulseWorks(bool replace)
        {
            var zone=new Zone("hot-steam-replacement");var source=Source(zone,10);
            var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="hot-steam-target-probe"};actor.SetTag("Creature");
            actor.AddPart(new PhysicsPart());actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=40,Max=40};
            Assert.True(zone.AddEntity(actor,11,10));var probe=new BeforeDamage();actor.AddPart(probe);bool entered=false;
            probe.Before=()=>
            {
                if(entered)return;entered=true;
                if(replace){source.RemoveEffect<SteamEffect>();source.ApplyEffect(new SteamEffect(.8f));}
                Pulse(zone,source);
            };
            Pulse(zone,source);Assert.AreEqual(38,actor.GetStatValue("Hitpoints"));Assert.AreEqual(1,probe.Calls);
            probe.Before=null;Pulse(zone,source);Assert.AreEqual(36,actor.GetStatValue("Hitpoints"));Assert.AreEqual(2,probe.Calls);
            AssertAttempt(source,actor,2);
            var rejected=DiagQuery.Apply(new DiagQuery.Filter{Category="effect",Kind="SteamPulseRejected",Actor=source.ID,Limit=10}).Records;
            Assert.AreEqual(1,rejected.Count);Assert.That(rejected[0].PayloadJson,Does.Contain("reentrant-pulse"));
            source.GetPart<ThermalPart>().Temperature=25;Pulse(zone,source);Assert.AreEqual(36,actor.GetStatValue("Hitpoints"));AssertAttempt(source,actor,2);
        }
        [Test]public void CallbackExceptionReleasesSourceGuardBeforeItsNextPulse()
        {
            var zone=new Zone("hot-steam-exception");var source=Source(zone,10);
            var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="hot-steam-target-probe"};actor.SetTag("Creature");
            actor.AddPart(new PhysicsPart());actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=40,Max=40};
            Assert.True(zone.AddEntity(actor,11,10));var probe=new BeforeDamage{Before=()=>{throw new InvalidOperationException("steam-callback-probe");}};actor.AddPart(probe);
            Assert.Throws<InvalidOperationException>(()=>Pulse(zone,source));Assert.AreEqual(40,actor.GetStatValue("Hitpoints"));
            probe.Before=null;Pulse(zone,source);Assert.AreEqual(38,actor.GetStatValue("Hitpoints"));Assert.AreEqual(2,probe.Calls);AssertAttempt(source,actor,2);
        }
        [TestCase(false)][TestCase(true)]public void DistinctSourceMayPulseInsideAnotherSourcesDamageCallback(bool vetoOuter)
        {
            var zone=new Zone("hot-steam-distinct-reentry");var first=Source(zone,10);var second=Source(zone,12);
            var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="hot-steam-target-probe"};actor.SetTag("Creature");
            actor.AddPart(new PhysicsPart());actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=40,Max=40};
            Assert.True(zone.AddEntity(actor,11,10));var probe=new BeforeDamage();actor.AddPart(probe);bool entered=false;
            probe.Before=()=>{if(entered)return;entered=true;Pulse(zone,second);};
            probe.Gate=e=>!vetoOuter||e.GetParameter<Entity>("Source")!=first;
            Pulse(zone,first);Assert.AreEqual(vetoOuter?38:36,actor.GetStatValue("Hitpoints"));Assert.AreEqual(2,probe.Calls);
            AssertAttempt(first,actor,1);AssertAttempt(second,actor,1);
            foreach(var source in new[]{first,second})
            {
                var dealt=DiagQuery.Apply(new DiagQuery.Filter{Category="damage",Kind="DamageDealt",Actor=source.ID,Target=actor.ID,Limit=10}).Records;
                Assert.AreEqual(vetoOuter&&source==first?0:1,dealt.Count);foreach(var record in dealt)Assert.That(record.PayloadJson,Does.Contain("\"amount\":2"));
            }
        }
        [TestCase(1)][TestCase(40)]public void CanonicalOverkillAndOrdinaryDamageRemainDistinctFromAnAttempt(int hp)
        {
            var zone=new Zone("hot-steam-overkill");var source=Source(zone,10);
            var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="hot-steam-target-probe"};actor.SetTag("Creature");
            actor.AddPart(new PhysicsPart());actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=hp,Max=40};
            Assert.True(zone.AddEntity(actor,11,10));var lines=new System.Collections.Generic.List<string>();var oldMessage=MessageLog.OnMessage;
            try{MessageLog.OnMessage=lines.Add;Pulse(zone,source);}finally{MessageLog.OnMessage=oldMessage;}
            Assert.AreEqual(Math.Max(0,hp-2),actor.GetStat("Hitpoints").BaseValue);Assert.AreEqual(hp==1,actor.HasTag("_DeathHandled"));AssertAttempt(source,actor,1);
            var dealt=DiagQuery.Apply(new DiagQuery.Filter{Category="damage",Kind="DamageDealt",Actor=source.ID,Target=actor.ID,Limit=10}).Records;
            Assert.AreEqual(1,dealt.Count);Assert.That(dealt[0].PayloadJson,Does.Contain("\"amount\":2"));Assert.That(dealt[0].PayloadJson,Does.Contain("Heat"));
            Assert.False(lines.Any(x=>x.IndexOf("scald",StringComparison.OrdinalIgnoreCase)>=0),"No duplicate per-source numeric prose is inferred from net callback HP changes.");
        }
    }
}
