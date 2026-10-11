using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioGainRampTests
    {
        float music,ambience,sfx;bool pause;GameObject root;AudioClip score,bed,detail,action;WorldAudioPlayer player;
        [SetUp] public void Setup()
        {
            music=WorldAudioSettings.MusicVolume;ambience=WorldAudioSettings.AmbienceVolume;sfx=WorldAudioSettings.SfxVolume;pause=AudioListener.pause;
            WorldAudioSettings.MusicVolume=1;WorldAudioSettings.AmbienceVolume=1;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            root=new GameObject("gain ramps");score=Clip("score");bed=Clip("bed");detail=Clip("detail");action=Clip("action");
            var library=new WorldAudioLibrary();library.AddMusic("organic",score,.4f);library.AddAmbience("field",bed,.3f);library.AddDetail("test",detail,.2f);library.AddSound(WorldAudioCue.Swing,action,.5f);
            player=new WorldAudioPlayer(root.transform,library);player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));player.Update(3);
        }
        static AudioClip Clip(string name)=>AudioClip.Create(name,48000*5,1,48000,false);
        AudioSource Voice(AudioClip clip)=>root.GetComponentsInChildren<AudioSource>().Single(v=>v.clip==clip);
        [TearDown] public void Cleanup(){player.Dispose();Object.DestroyImmediate(root);foreach(var clip in new[]{score,bed,detail,action})Object.DestroyImmediate(clip);WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;AudioListener.pause=pause;}
        [Test] public void EstablishedPositiveMusicChangeHasARealMidpointThenExactTarget()
        {int starts=player.MusicStartCount;WorldAudioSettings.MusicVolume=.2f;player.Update(.175f);Assert.AreEqual(.24f,Voice(score).volume,.0001f);player.Update(.175f);Assert.AreEqual(.08f,Voice(score).volume,.0001f);Assert.AreEqual(starts,player.MusicStartCount);Assert.AreEqual(.2f,WorldAudioSettings.MusicVolume);}
        [Test] public void AmbientBedAndAnExistingDetailShareTheRampButActionDoesNot()
        {Assert.True(player.TryPlayDetail("test"));Assert.True(player.TryPlay(WorldAudioCue.Swing));WorldAudioSettings.AmbienceVolume=.2f;player.Update(.175f);Assert.AreEqual(.18f,Voice(bed).volume,.0001f);Assert.AreEqual(.12f,Voice(detail).volume,.0001f);Assert.AreEqual(.5f,Voice(action).volume,.0001f);Assert.AreEqual(.4f,Voice(score).volume,.0001f);}
        [Test] public void NewDetailsUseCurrentSmoothedBusRatherThanJumpingToRequestedTarget()
        {WorldAudioSettings.AmbienceVolume=.2f;player.Update(.175f);Assert.True(player.TryPlayDetail("test"));Assert.AreEqual(.12f,Voice(detail).volume,.0001f);}
        [Test] public void ZeroImmediatelyClearsItsWaveformsAndDetailsEvenAtZeroDelta()
        {player.TryPlayDetail("test");WorldAudioSettings.MusicVolume=0;WorldAudioSettings.AmbienceVolume=0;player.Update(0);Assert.Zero(player.ActiveMusicSources);Assert.Zero(player.ActiveAmbienceSources);Assert.Zero(player.ActiveDetailSources);Assert.Zero(player.EffectiveMusicVolume);Assert.Zero(player.EffectiveAmbienceVolume);}
        [Test] public void UnmutingSmoothsFromZeroWhileMaintainingAnExactRequestedPreference()
        {WorldAudioSettings.MusicVolume=0;player.Update(0);WorldAudioSettings.MusicVolume=1;player.Update(.175f);Assert.AreEqual(.5f,player.EffectiveMusicVolume,.0001f);Assert.AreEqual(1,WorldAudioSettings.MusicVolume);player.Update(.175f);Assert.AreEqual(1,player.EffectiveMusicVolume);}
        [Test] public void PauseStopsAudioAndFreezesPositiveRampUntilResumed()
        {WorldAudioSettings.MusicVolume=.2f;AudioListener.pause=true;player.Update(30);Assert.Zero(player.ActiveMusicSources);Assert.AreEqual(1,player.EffectiveMusicVolume);AudioListener.pause=false;player.Update(.175f);Assert.AreEqual(.6f,player.EffectiveMusicVolume,.0001f);}
        [Test] public void CombatFocusMultipliesTheSmoothedGainWithoutChangingRequests()
        {WorldAudioSettings.MusicVolume=.2f;player.Update(.175f);Assert.True(player.TryPlayCombat(WorldAudioCue.Swing));player.Update(.1f);Assert.AreEqual(.6f,player.AtmosphereMix,.0001f);Assert.AreEqual(player.EffectiveMusicVolume*.4f*.6f,Voice(score).volume,.0001f);Assert.AreEqual(.2f,WorldAudioSettings.MusicVolume);Assert.AreEqual(16,player.AllocatedSources);}
        [Test] public void SavedPreferencesAreNeverWrittenByGainSmoothing()
        {string key=WorldAudioSettings.MusicKey;bool had=PlayerPrefs.HasKey(key);float before=PlayerPrefs.GetFloat(key);WorldAudioSettings.MusicVolume=.2f;player.Update(.175f);Assert.AreEqual(had,PlayerPrefs.HasKey(key));if(had)Assert.AreEqual(before,PlayerPrefs.GetFloat(key));}
        [Test] public void ASecondPositiveChangeStartsAtTheCurrentGainWithoutAnEdge()
        {var gain=new WorldAudioGainEnvelope(1);gain.Advance(.2f,.175f);Assert.AreEqual(.6f,gain.Value,.0001f);gain.Advance(.8f,0);Assert.AreEqual(.6f,gain.Value,.0001f);gain.Advance(.8f,.175f);Assert.AreEqual(.7f,gain.Value,.0001f);gain.Advance(.8f,.175f);Assert.AreEqual(.8f,gain.Value,.0001f);}
        [Test] public void SplitWallDeltaMatchesOneWholeRampSegment()
        {var a=new WorldAudioGainEnvelope(1);var b=new WorldAudioGainEnvelope(1);a.Advance(.2f,.2f);for(int i=0;i<4;i++)b.Advance(.2f,.05f);Assert.AreEqual(a.Value,b.Value,.0001f);a.Advance(.2f,30);Assert.AreEqual(.2f,a.Value);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(float.NegativeInfinity)][TestCase(-1f)][TestCase(0f)]
        public void InvalidDeltaCannotAdvancePositiveGain(float delta)
        {var gain=new WorldAudioGainEnvelope(1);gain.Advance(.2f,delta);Assert.AreEqual(1,gain.Value);gain.Advance(.2f,.35f);Assert.AreEqual(.2f,gain.Value,.0001f);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(float.NegativeInfinity)]
        public void NonFiniteRequestedGainCannotCorruptAnExistingRamp(float request)
        {var gain=new WorldAudioGainEnvelope(1);gain.Advance(.2f,.175f);gain.Advance(request,30);Assert.AreEqual(.6f,gain.Value,.0001f);gain.Advance(.2f,.175f);Assert.AreEqual(.2f,gain.Value,.0001f);}
        [Test] public void ClampedZeroRemainsImmediateWhilePaused(){var gain=new WorldAudioGainEnvelope(1);gain.Advance(-5,0,false);Assert.Zero(gain.Value);gain.Advance(4,.35f);Assert.AreEqual(1,gain.Value);}
    }
}
