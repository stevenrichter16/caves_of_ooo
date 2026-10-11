using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioAnimalRestAdversarialTests : WorldAudioAnimalRestFixture
    {
        [TestCase("sleep")][TestCase("hibernate")][TestCase("freeze")]
        public void ForeignEffectOwnerCannotSilenceTheCurrentAnimal(string kind)
        {var owner=Animal();var effect=Rest(kind);Assert.True(owner.ApplyEffect(effect));effect.Owner=Actor;Start();}
        [TestCase("sleep",0)][TestCase("hibernate",0)][TestCase("sleep",-2)][TestCase("hibernate",-2)]
        public void ExpiredOrInvalidSleepDurationDoesNotInventActiveStasis(string kind,int duration)
        {var owner=Animal();var effect=Rest(kind);Assert.True(owner.ApplyEffect(effect));effect.Duration=duration;Start();}
        [TestCase("sleep")][TestCase("hibernate")]
        public void IndefiniteSleepIsStillActive(string kind)
        {var owner=Animal();var effect=Rest(kind);Assert.True(owner.ApplyEffect(effect));effect.Duration=Effect.DURATION_INDEFINITE;Observe();Assert.AreEqual(0,Scene.SourceCount);}
        [TestCase(0f)][TestCase(-1f)]
        public void FrozenMagnitudeRatherThanItsNameControlsQuiet(float cold)
        {var owner=Animal();var effect=new FrozenEffect(.5f);Assert.True(owner.ApplyEffect(effect));effect.Cold=cold;Start();}
        [Test] public void SleepOnAnotherEntityNeverSilencesTheCurrentWitness()
        {var owner=Animal();Assert.True(Actor.ApplyEffect(new AsleepByGasEffect(5)));Start();}
        [Test] public void WakeDoesNotDiscoverTheFormerOwnerDuringIdle()
        {
            var owner=Animal();var effect=new AsleepByGasEffect(5);Assert.True(owner.ApplyEffect(effect));Observe();int scans=Scene.ScanCount;
            owner.GetPart<StatusEffectsPart>().RemoveEffect(effect);for(int i=0;i<60;i++)Scene.Tick(1,true,false);
            Assert.AreEqual(0,Audio.AcceptedDetailCount);Assert.AreEqual(scans,Scene.ScanCount);Start();Assert.AreEqual(16,Audio.AllocatedSources);
        }
        [TestCase("hidden")][TestCase("dead")][TestCase("removed")][TestCase("inventory")]
        public void WakingDoesNotBypassCurrentSourceAuthority(string mutation)
        {
            var owner=Animal();var effect=new AsleepByGasEffect(5);Assert.True(owner.ApplyEffect(effect));Observe();
            owner.GetPart<StatusEffectsPart>().RemoveEffect(effect);
            if(mutation=="hidden")Zone.GetEntityCell(owner).IsVisible=false;if(mutation=="dead")owner.GetStat("Hitpoints").BaseValue=0;
            if(mutation=="removed")Zone.RemoveEntity(owner);if(mutation=="inventory")owner.GetPart<PhysicsPart>().InInventory=Actor;
            Observe();Assert.AreEqual(0,Scene.SourceCount);
        }
        [Test] public void SleepingNearestFrogDoesNotHideAnAwakeSecondAnimal()
        {var sleeping=Animal();Assert.True(sleeping.ApplyEffect(new AsleepByGasEffect(5)));Animal(x:13);Start();Assert.AreEqual(1,Scene.SourceCount);}
        [Test] public void WakingSilentSpeciesDoesNotInventARecording()
        {var owner=Animal("CascadeFather");var effect=new FrozenEffect(.5f);Assert.True(owner.ApplyEffect(effect));Observe();owner.GetPart<StatusEffectsPart>().RemoveEffect(effect);Observe();Assert.AreEqual(0,Scene.SourceCount);}
        [Test] public void RemovedSleepObjectCannotContinueSuppressingItsOldOwner()
        {var owner=Animal();var effect=new AsleepByGasEffect(5);Assert.True(owner.ApplyEffect(effect));Observe();owner.GetPart<StatusEffectsPart>().RemoveEffect(effect);effect.Duration=5;Start();}
        [Test] public void ARealDamagingWakeEndsSleepEvenBeforeCollectionSweep()
        {
            var owner=Animal();var effect=new AsleepByGasEffect(5);Assert.True(owner.ApplyEffect(effect));Observe();
            var e=GameEvent.New("TakeDamage");e.SetParameter("Damage",new Damage(1));effect.OnTakeDamage(owner,e);e.Release();Assert.AreEqual(0,effect.Duration);Start();
        }
    }
}
