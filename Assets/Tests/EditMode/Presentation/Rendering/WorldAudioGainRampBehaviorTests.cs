using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Runtime-source observations through pre20 APIs only; no new envelope helper or effective-gain property.
    public sealed class WorldAudioGainRampBehaviorTests
    {
        float music,ambience,sfx;bool pause;GameObject root;AudioClip score,bed,detail,action;WorldAudioPlayer player;
        [SetUp] public void Setup()
        {
            music=WorldAudioSettings.MusicVolume;ambience=WorldAudioSettings.AmbienceVolume;sfx=WorldAudioSettings.SfxVolume;pause=AudioListener.pause;
            WorldAudioSettings.MusicVolume=WorldAudioSettings.AmbienceVolume=WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            root=new GameObject("pre20 actual gain");score=Clip("score");bed=Clip("bed");detail=Clip("detail");action=Clip("action");
            var library=new WorldAudioLibrary();library.AddMusic("organic",score,.4f);library.AddAmbience("field",bed,.3f);library.AddDetail("test",detail,.2f);library.AddSound(WorldAudioCue.Swing,action,.5f);
            player=new WorldAudioPlayer(root.transform,library);player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false));player.Update(3);
            Assert.AreEqual(.4f,Voice(score).volume,.0001f,"Initial source gain must be established before testing a change.");
        }
        static AudioClip Clip(string name)=>AudioClip.Create(name,240000,1,48000,false);
        AudioSource Voice(AudioClip clip)=>root.GetComponentsInChildren<AudioSource>().Single(v=>v.clip==clip);
        [TearDown] public void Cleanup(){player?.Dispose();Object.DestroyImmediate(root);foreach(var c in new[]{score,bed,detail,action})Object.DestroyImmediate(c);WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;AudioListener.pause=pause;}
        [Test] public void PositiveMusicEditHasMidpointThenExactTargetWithoutRestart(){int starts=player.MusicStartCount;WorldAudioSettings.MusicVolume=.2f;player.Update(.175f);Assert.AreEqual(.24f,Voice(score).volume,.0001f,"A positive menu edit should ramp, not jump to .08.");player.Update(.175f);Assert.AreEqual(.08f,Voice(score).volume,.0001f);Assert.AreEqual(starts,player.MusicStartCount);}
        [Test] public void AmbientBedAndExistingDetailRampTogetherWhileActionsStayIndependent(){Assert.True(player.TryPlayDetail("test"));Assert.True(player.TryPlay(WorldAudioCue.Swing));WorldAudioSettings.AmbienceVolume=.2f;player.Update(.175f);Assert.AreEqual(.18f,Voice(bed).volume,.0001f);Assert.AreEqual(.12f,Voice(detail).volume,.0001f);Assert.AreEqual(.5f,Voice(action).volume,.0001f);Assert.AreEqual(.4f,Voice(score).volume,.0001f);}
        [Test] public void NewlyAcceptedDetailStartsAtTheCurrentSmoothedBusGain(){WorldAudioSettings.AmbienceVolume=.2f;player.Update(.175f);Assert.True(player.TryPlayDetail("test"));Assert.AreEqual(.12f,Voice(detail).volume,.0001f);}
        [Test] public void SecondPositiveEditCannotMakeAnImmediateEdge(){WorldAudioSettings.MusicVolume=.2f;player.Update(.175f);float current=Voice(score).volume;WorldAudioSettings.MusicVolume=.8f;player.Update(0);Assert.AreEqual(current,Voice(score).volume,.0001f);player.Update(.175f);Assert.AreEqual(.28f,Voice(score).volume,.0001f);player.Update(.175f);Assert.AreEqual(.32f,Voice(score).volume,.0001f);}
        [Test] public void ZeroImmediatelySilencesItsSourcesEvenAtZeroDelta(){Assert.True(player.TryPlayDetail("test"));WorldAudioSettings.MusicVolume=0;WorldAudioSettings.AmbienceVolume=0;player.Update(0);Assert.Zero(player.ActiveMusicSources);Assert.Zero(player.ActiveAmbienceSources);Assert.Zero(player.ActiveDetailSources);}
        [Test] public void GainEditsKeepRequestedPreferenceAndNeverWriteSavedSettings(){string key=WorldAudioSettings.MusicKey;bool had=PlayerPrefs.HasKey(key);float prior=PlayerPrefs.GetFloat(key);WorldAudioSettings.MusicVolume=.2f;player.Update(.175f);Assert.AreEqual(.2f,WorldAudioSettings.MusicVolume);Assert.AreEqual(had,PlayerPrefs.HasKey(key));if(had)Assert.AreEqual(prior,PlayerPrefs.GetFloat(key));Assert.AreEqual(16,player.AllocatedSources);}
    }
}
