using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class FloatingStatusTextTests
    {
        Zone zone;
        Entity actor;

        [SetUp] public void Setup()
        {
            AsciiFxBus.Clear();
            MessageLog.Clear();
            zone=new Zone("floating-status");
            actor=new Entity{ID="status-owner"};
            actor.SetTag("Creature");
            actor.AddPart(new RenderPart{DisplayName="status owner"});
            actor.Statistics["Hitpoints"]=new Stat{Owner=actor,Name="Hitpoints",BaseValue=20,Max=20};
            zone.AddEntity(actor,5,5);
            zone.GetCell(5,5).IsVisible=true;
            zone.GetCell(5,5).Explored=true;
        }
        [TearDown] public void Cleanup() => AsciiFxBus.Clear();

        static List<AsciiFxRequest> Texts()
        {
            var result=new List<AsciiFxRequest>();
            foreach(var request in AsciiFxBus.Drain())
                if(request.Type==AsciiFxRequestType.EventText) result.Add(request);
                else AsciiFxBus.Release(request);
            return result;
        }

        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void AcceptedStun_ShowsDisplayNameOnPlayerAndNpc_WithImplicitOrExplicitZone(bool player,bool implicitZone)
        {
            if(player) actor.SetTag("Player");
            var effect=new StunnedEffect(3);
            Assert.True(actor.ApplyEffect(effect,zone:implicitZone?null:zone));
            var texts=Texts();
            Assert.AreEqual(1,texts.Count);
            Assert.AreEqual("Stunned",texts[0].Text);
            Assert.AreEqual("&R",texts[0].ColorString);
            Assert.AreSame(actor,texts[0].Anchor);
            Assert.AreSame(zone,texts[0].Zone);
            Assert.False(texts[0].BlocksTurnAdvance);
            Assert.AreEqual(3,effect.Duration,"Feedback must not advance the effect's timer.");
        }

        [Test] public void BeneficialStatus_UsesReadableNameAndBeneficialColor()
        {
            Assert.True(actor.ApplyEffect(new WellRestedEffect(10),zone:zone));
            var texts=Texts();
            Assert.AreEqual(1,texts.Count);
            Assert.AreEqual("Well rested",texts[0].Text);
            Assert.AreEqual("&C",texts[0].ColorString);
        }

        [TestCase(false)] [TestCase(true)]
        public void StatSurges_ReportDistinctBonusesButNotRefreshOfTheSameBonus(bool sameStat)
        {
            actor.Statistics["Strength"]=new Stat{Owner=actor,Name="Strength",BaseValue=18,Max=100};
            actor.Statistics["Agility"]=new Stat{Owner=actor,Name="Agility",BaseValue=18,Max=100};
            var strength=new TonicStatSurgeEffect("Strength",4,3);
            Assert.True(actor.ApplyEffect(strength,zone:zone));
            var initial=Texts();
            Assert.AreEqual(1,initial.Count);
            Assert.AreEqual("Strength surge",initial[0].Text);
            Assert.True(actor.ApplyEffect(new TonicStatSurgeEffect(sameStat?"Strength":"Agility",6,9),zone:zone));
            Assert.AreEqual(4,actor.GetStat("Strength").Boost);
            Assert.AreEqual(sameStat?0:6,actor.GetStat("Agility").Boost);
            Assert.AreEqual(sameStat?9:3,strength.Duration);
            Assert.AreEqual(sameStat?1:2,actor.GetPart<StatusEffectsPart>().EffectCount);
            var next=Texts();
            Assert.AreEqual(sameStat?0:1,next.Count);
            if(!sameStat)
            {
                Assert.AreEqual("Agility surge",next[0].Text);
                Assert.AreEqual("&C",next[0].ColorString);
            }
        }

        [Test] public void ChangedSeverityDisplayName_DoesNotTurnARefreshIntoANewStatus()
        {
            var parched=new ParchedEffect();
            Assert.True(actor.ApplyEffect(parched,zone:zone));
            Assert.AreEqual("Parched",Texts()[0].Text);
            Assert.True(actor.ApplyEffect(new ParchedEffect(),zone:zone));
            Assert.True(actor.ApplyEffect(new ParchedEffect(),zone:zone));
            Assert.AreEqual(3,parched.Stacks);
            Assert.AreEqual("parched (badly)",parched.DisplayName);
            Assert.Zero(Texts().Count);
        }

        [TestCase(false)] [TestCase(true)]
        public void ExistingLiveStatus_DoesNotRepeatEvenWhenOnStackAllowsAnotherInstance(bool absorbs)
        {
            var first=new ProbeEffect{AbsorbStack=absorbs};
            Assert.True(actor.ApplyEffect(first,zone:zone));
            Assert.AreEqual(1,Texts().Count,"Counter-control: first onset really notified.");
            Assert.True(actor.ApplyEffect(new ProbeEffect(),zone:zone));
            Assert.AreEqual(absorbs?1:2,actor.GetPart<StatusEffectsPart>().EffectCount);
            Assert.Zero(Texts().Count,"Already-active state must not repeat its onset label.");
        }

        [Test] public void ExpiredStatusRevivedByStack_ReportsItsFirstActiveOnset()
        {
            var first=new ProbeEffect{Duration=0,AbsorbStack=true};
            Assert.True(actor.ApplyEffect(first,zone:zone));
            Assert.Zero(Texts().Count);
            Assert.True(actor.ApplyEffect(new ProbeEffect{Duration=4},zone:zone));
            Assert.AreEqual(4,first.Duration);
            Assert.AreEqual(1,Texts().Count);
        }

        [Test] public void RefreshThatExpiresExistingStatus_IsSilent()
        {
            var first=new ProbeEffect{AbsorbStack=true};
            Assert.True(actor.ApplyEffect(first,zone:zone));
            Assert.AreEqual(1,Texts().Count);
            Assert.True(actor.ApplyEffect(new ProbeEffect{Duration=0},zone:zone));
            Assert.Zero(first.Duration);
            Assert.Zero(Texts().Count);
        }

        [TestCase(false)] [TestCase(true)]
        public void RejectedApplication_IsSilent(bool lateRefusal)
        {
            Assert.False(actor.ApplyEffect(new ProbeEffect{RefuseEarly=!lateRefusal,RefuseApply=lateRefusal},zone:zone));
            Assert.Zero(Texts().Count);
            Assert.Zero(actor.GetPart<StatusEffectsPart>().EffectCount);
        }

        [Test] public void ForcedAcceptedApplication_StillReportsOnset()
        {
            Assert.True(actor.ForceApplyEffect(new ProbeEffect{RefuseEarly=true},zone:zone));
            Assert.AreEqual(1,Texts().Count);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void InactiveOrRemovedDuringAppliedHook_IsSilent(int mode)
        {
            var effect=new ProbeEffect{Duration=mode==0?0:3,RemoveWhenApplied=mode==1,ExpireWhenApplied=mode==2};
            Assert.True(actor.ApplyEffect(effect,zone:zone));
            Assert.Zero(Texts().Count);
        }

        [TestCase("remove")] [TestCase("expire")] [TestCase("kill")] [TestCase("unown")] [TestCase("detach")]
        public void AppliedListenersCanInvalidateNotificationBeforeItIsEmitted(string mode)
        {
            actor.AddPart(new InvalidateAppliedPart{Mode=mode});
            Assert.True(actor.ApplyEffect(new ProbeEffect(),zone:zone));
            Assert.Zero(Texts().Count);
        }

        [TestCase("dead")] [TestCase("prop")] [TestCase("hidden")] [TestCase("fog")] [TestCase("wrong-zone")]
        public void IneligibleTarget_DoesNotReceiveFloatingStatusText(string mode)
        {
            if(mode=="dead") actor.GetStat("Hitpoints").BaseValue=0;
            if(mode=="prop") actor.Tags.Remove("Creature");
            if(mode=="hidden") actor.GetPart<RenderPart>().Visible=false;
            if(mode=="fog") zone.GetCell(5,5).IsVisible=false;
            Assert.True(actor.ApplyEffect(new ProbeEffect(),zone:mode=="wrong-zone"?new Zone("other"):zone));
            Assert.Zero(Texts().Count);
        }

        [Test] public void DistinctNewStatuses_HaveDistinctLabels()
        {
            Assert.True(actor.ApplyEffect(new StunnedEffect(3),zone:zone));
            Assert.True(actor.ApplyEffect(new RootedEffect(3),zone:zone));
            var texts=Texts();
            Assert.AreEqual(2,texts.Count);
            Assert.AreEqual("Stunned",texts[0].Text);
            Assert.AreEqual("Rooted",texts[1].Text);
        }

        [Test] public void TicksAndCurrentSaveRestoration_DoNotReplayOnset()
        {
            Assert.True(actor.ApplyEffect(new ProbeEffect(),zone:zone));
            Assert.AreEqual(1,Texts().Count);
            actor.GetPart<StatusEffectsPart>().GetEffect<ProbeEffect>().JustApplied=false;
            actor.FireEventAndRelease(GameEvent.New("EndTurn"));
            Assert.AreEqual(2,actor.GetPart<StatusEffectsPart>().GetEffect<ProbeEffect>().Duration);
            Assert.Zero(Texts().Count);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.AreEqual(2,loaded.GetPart<StatusEffectsPart>().GetEffect<ProbeEffect>().Duration);
            Assert.Zero(Texts().Count);
        }

        public class ProbeEffect : Effect
        {
            public bool AbsorbStack,RefuseEarly,RefuseApply,RemoveWhenApplied,ExpireWhenApplied;
            public override string DisplayName => "test status";
            public ProbeEffect() { Duration=3; }
            public override bool CanApply(Entity target) => !RefuseEarly;
            public override bool Apply(Entity target) => !RefuseApply;
            public override bool OnStack(Effect incoming)
            {
                if(AbsorbStack) Duration=incoming.Duration;
                return AbsorbStack;
            }
            public override void Applied(Entity target)
            {
                if(RemoveWhenApplied) target.GetPart<StatusEffectsPart>().RemoveEffect(this);
                if(ExpireWhenApplied) Duration=0;
            }
        }

        public class InvalidateAppliedPart : Part
        {
            public string Mode;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID!="EffectApplied") return true;
                var effect=e.GetParameter<Effect>("Effect");
                if(Mode=="remove") ParentEntity.GetPart<StatusEffectsPart>().RemoveEffect(effect);
                if(Mode=="expire") effect.Duration=0;
                if(Mode=="kill") ParentEntity.GetStat("Hitpoints").BaseValue=0;
                if(Mode=="unown") effect.Owner=null;
                if(Mode=="detach") ParentEntity.RemovePart(ParentEntity.GetPart<StatusEffectsPart>());
                return true;
            }
        }
    }
}
