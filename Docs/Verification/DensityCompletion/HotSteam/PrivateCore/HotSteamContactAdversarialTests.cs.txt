using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>Callback mutation, serialization, source identity and bounded
    /// multi-body exposure. Fixture stimuli are explicit, not acquisition claims.</summary>
    public sealed class HotSteamContactAdversarialTests : HotSteamContactFixture
    {
        void AssertCanonicalDamage(Action action,bool landed)
        {
            bool old=Diag.IsChannelEnabled("damage");string cause=Guid.NewGuid().ToString("N");
            try
            {
                Diag.SetChannel("damage",true);using(Diag.WithCause(cause))action();
                var records=DiagQuery.Apply(new DiagQuery.Filter{Category="damage",Kind="DamageDealt",Actor=source.ID,Target=actor.ID,CauseTraceId=cause,Limit=10}).Records;
                Assert.AreEqual(landed?1:0,records.Count);foreach(var record in records)Assert.That(record.PayloadJson,Does.Contain("Heat"));
            }finally{Diag.SetChannel("damage",old);}
        }
        sealed class DamageProbe : Part
        {
            public int Attempts;public bool Veto;public Action Before;public Entity LastSource;public string[] Attributes;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID=="BeforeTakeDamage")
                {
                    Attempts++;LastSource=e.GetParameter<Entity>("Source");Attributes=e.GetParameter<Damage>("Damage").Attributes.ToArray();
                    Before?.Invoke();return !Veto;
                }
                return true;
            }
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_SaveRoundTripKeepsExistingTemperatureDensityAndCorrectEffectOwner(bool hot)
        {
            source.GetPart<ThermalPart>().Temperature=hot?180:25;steam.Density=.4f;steam.Duration=7;
            var clone=RoundTrip(source);Assert.AreNotSame(source,clone);Assert.AreEqual(source.ID,clone.ID);
            Assert.AreSame(clone,clone.GetEffect<SteamEffect>().Owner);Assert.AreSame(clone,clone.GetPart<ThermalPart>().ParentEntity);
            Assert.AreEqual(hot?180:25,clone.GetPart<ThermalPart>().Temperature);Assert.AreEqual(.4f,clone.GetEffect<SteamEffect>().Density);
            Assert.AreEqual(7,clone.GetEffect<SteamEffect>().Duration);
            Assert.True(zone.RemoveEntity(source));Assert.True(zone.AddEntity(clone,10,10));source=clone;steam=clone.GetEffect<SteamEffect>();
            int hp=actor.GetStatValue("Hitpoints");DirectPulse();Assert.AreEqual(hot?2:0,hp-actor.GetStatValue("Hitpoints"));
        }
        [TestCase("0,0;0,1",11,10,true)][TestCase("-4,0;-4,1",15,10,true)]
        [TestCase("0,0;0,1",13,10,false)]
        public void Adversarial_MultipleSourceAndTargetCellsDoNotMultiplyOnePulse(string cells,int x,int y,bool contact)
        {
            Assert.True(zone.RemoveEntity(actor));actor.AddPart(new SpatialFootprintPart{CellsRaw=cells});Assert.True(zone.AddEntity(actor,x,y));
            Assert.True(zone.RemoveEntity(source));source.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});Assert.True(zone.AddEntity(source,10,10));
            var probe=new DamageProbe();actor.AddPart(probe);int hp=actor.GetStatValue("Hitpoints");DirectPulse();
            Assert.AreEqual(contact?2:0,hp-actor.GetStatValue("Hitpoints"));Assert.AreEqual(contact?1:0,probe.Attempts);
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_DistinctSourcesAreIndependentButSourceSelfIsExcluded(bool two)
        {
            // A source can itself be a living creature; it must not damage itself.
            source.SetTag("Creature");source.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",Owner=source,BaseValue=40,Max=40};
            source.Statistics["HeatResistance"]=new Stat{Name="HeatResistance",Owner=source,BaseValue=0,Max=100};
            int hp=actor.GetStatValue("Hitpoints");DirectPulse();Assert.AreEqual(2,hp-actor.GetStatValue("Hitpoints"));Assert.AreEqual(40,source.GetStatValue("Hitpoints"));
            if(two)
            {
                var second=scope.Factory.CreateEntity("OilSeep");Assert.True(zone.AddEntity(second,12,10));second.GetPart<ThermalPart>().Temperature=180;second.ApplyEffect(new SteamEffect(.8f));
                var e=GameEvent.New("BeginTakeAction");e.SetParameter("Zone",(object)zone);second.FireEventAndRelease(e);Assert.AreEqual(4,hp-actor.GetStatValue("Hitpoints"));
            }
            else Assert.AreEqual(2,hp-actor.GetStatValue("Hitpoints"));
        }
        [TestCase("remove-source")][TestCase("remove-other")][TestCase("move-other")][TestCase("cool-source")][TestCase("clear-steam")]
        public void Adversarial_CallbackRevalidatesRemainingContactsInsteadOfApplyingAStaleSnapshot(string mutation)
        {
            var other=scope.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(other,9,9));
            other.Statistics["HeatResistance"]=new Stat{Name="HeatResistance",Owner=other,BaseValue=0,Max=100};
            Action<Entity> mutate=remaining=>
            {
                switch(mutation)
                {
                    case "remove-source":zone.RemoveEntity(source);break;
                    case "remove-other":zone.RemoveEntity(remaining);break;
                    case "move-other":Assert.True(zone.MoveEntity(remaining,20,20));break;
                    case "cool-source":source.GetPart<ThermalPart>().Temperature=25;break;
                    case "clear-steam":source.RemoveEffect<SteamEffect>();break;
                }
            };
            var a=new DamageProbe{Before=()=>mutate(other)};var b=new DamageProbe{Before=()=>mutate(actor)};
            actor.AddPart(a);other.AddPart(b);int hpA=actor.GetStatValue("Hitpoints"),hpB=other.GetStatValue("Hitpoints");
            DirectPulse();Assert.AreEqual(1,a.Attempts+b.Attempts);Assert.AreEqual(2,hpA+hpB-actor.GetStatValue("Hitpoints")-other.GetStatValue("Hitpoints"));
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_ActualTypedDamageCanBeVetoedAndCanonicalFeedbackClaimsTheHit(bool veto)
        {
            var probe=new DamageProbe{Veto=veto};actor.AddPart(probe);MessageLog.Clear();int hp=actor.GetStatValue("Hitpoints");AssertCanonicalDamage(DirectPulse,!veto);
            Assert.AreEqual(1,probe.Attempts);Assert.AreSame(source,probe.LastSource);Assert.That(probe.Attributes,Does.Contain("Heat"));
            Assert.AreEqual(veto?0:2,hp-actor.GetStatValue("Hitpoints"));
            Assert.False(MessageLog.GetRecent(50).Any(s=>s.IndexOf("scald",StringComparison.OrdinalIgnoreCase)>=0));
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_ImmuneContactKeepsCoolingWettingButNeverClaimsDamage(bool immune)
        {
            actor.GetStat("HeatResistance").BaseValue=immune?100:0;MessageLog.Clear();int hp=actor.GetStatValue("Hitpoints");AssertCanonicalDamage(DirectPulse,!immune);
            Assert.AreEqual(immune?0:2,hp-actor.GetStatValue("Hitpoints"));Assert.Greater(actor.GetEffect<WetEffect>().Moisture,0);
            Assert.False(MessageLog.GetRecent(50).Any(s=>s.IndexOf("scald",StringComparison.OrdinalIgnoreCase)>=0));
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)]
        public void Adversarial_InvalidDensityCannotBecomeAHeatAttack(float density)
        {steam.Density=density;int hp=actor.GetStatValue("Hitpoints");DirectPulse();Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));}
        [TestCase(false)][TestCase(true)]
        public void Adversarial_OnlyLivingCreatureTargetsAreDamaged(bool creature)
        {
            if(!creature)actor.Tags.Remove("Creature");int hp=actor.GetStatValue("Hitpoints");DirectPulse();Assert.AreEqual(creature?2:0,hp-actor.GetStatValue("Hitpoints"));
        }
        [Test]public void Adversarial_DifferentZoneCannotBorrowSourceCoordinates()
        {
            var elsewhere=new Zone("steam-foreign");Assert.True(zone.RemoveEntity(actor));Assert.True(elsewhere.AddEntity(actor,11,10));
            int hp=actor.GetStatValue("Hitpoints");var e=GameEvent.New("BeginTakeAction");e.SetParameter("Zone",(object)elsewhere);source.FireEventAndRelease(e);
            Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_AnatomicalFootprintUsesCommittedPlacementUntilChanged(bool stale)
        {
            Assert.True(zone.RemoveEntity(actor));var footprint=new SpatialFootprintPart{CellsRaw="0,0"};actor.AddPart(footprint);Assert.True(zone.AddEntity(actor,20,10));
            if(stale)footprint.CellsRaw="-9,0";else Assert.True(zone.TryChangeFootprint(actor,"-9,0"));
            int hp=actor.GetStatValue("Hitpoints");DirectPulse();Assert.AreEqual(stale?0:2,hp-actor.GetStatValue("Hitpoints"));
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_SameSourceReentrantDamageCannotMultiplyItsActivePulse(bool reenter)
        {
            bool nested=false;var probe=new DamageProbe();actor.AddPart(probe);
            if(reenter)probe.Before=()=>{if(!nested){nested=true;DirectPulse();}};
            int hp=actor.GetStatValue("Hitpoints");DirectPulse();Assert.AreEqual(2,hp-actor.GetStatValue("Hitpoints"));Assert.AreEqual(1,probe.Attempts);
            probe.Before=null;DirectPulse();Assert.AreEqual(4,hp-actor.GetStatValue("Hitpoints"));Assert.AreEqual(2,probe.Attempts);
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_NoThermalOnTargetDoesNotGrantHeatImmunity(bool removeThermal)
        {
            if(removeThermal)actor.RemovePart(actor.GetPart<ThermalPart>());int hp=actor.GetStatValue("Hitpoints");DirectPulse();
            Assert.AreEqual(2,hp-actor.GetStatValue("Hitpoints"));
        }
        [Test]public void Adversarial_ScaldingVeilRetainsItsSeparateRetaliationContract()
        {
            source.RemoveEffect<SteamEffect>();source.ApplyEffect(new ScaldingVeilEffect());int hp=actor.GetStatValue("Hitpoints");DirectPulse();
            Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));
        }
    }
}
