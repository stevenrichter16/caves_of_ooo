using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioMusicBookmarkTests
    {
        GameObject root; WorldAudioPlayer audio; WorldAudioLibrary library;
        readonly List<AudioClip> clips=new List<AudioClip>();
        AudioClip organic,dry,danger;float music,ambience,sfx;bool paused;
        static WorldAudioContext Field=>WorldAudioContext.Select(BiomeType.Spread,0,false);
        static WorldAudioContext Dry=>WorldAudioContext.Select(BiomeType.Beating,0,false);
        static WorldAudioContext Danger=>WorldAudioContext.Select(BiomeType.Spread,0,true);
        static WorldAudioContext Below=>WorldAudioContext.Select(BiomeType.Cave,1,false);

        [SetUp] public void SetUp()
        {
            music=WorldAudioSettings.MusicVolume;ambience=WorldAudioSettings.AmbienceVolume;sfx=WorldAudioSettings.SfxVolume;paused=AudioListener.pause;
            WorldAudioSettings.MusicVolume=WorldAudioSettings.AmbienceVolume=WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
            root=new GameObject("music-bookmark-test");organic=Clip("organic");dry=Clip("dry");danger=Clip("danger");
            library=new WorldAudioLibrary();library.AddMusic("organic",organic,.3f);library.AddMusic("dry",dry,.25f);
            library.AddMusic("danger",danger,.4f);library.AddMusic("underground",danger,.3f);
            library.AddAmbience("field",organic,.2f);library.AddSound(WorldAudioCue.Pickup,organic,.4f);
            audio=new WorldAudioPlayer(root.transform,library);
        }
        [TearDown] public void TearDown()
        {
            audio?.Dispose();UnityEngine.Object.DestroyImmediate(root);
            foreach(var clip in clips)UnityEngine.Object.DestroyImmediate(clip);clips.Clear();
            WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;AudioListener.pause=paused;
        }
        AudioClip Clip(string name){var clip=AudioClip.Create(name,48000*8,1,48000,false);clips.Add(clip);return clip;}
        AudioSource Voice(AudioClip clip,string prefix="Music")=>root.GetComponentsInChildren<AudioSource>().Single(s=>s.name.StartsWith(prefix,StringComparison.Ordinal)&&s.clip==clip);
        void Select(WorldAudioContext context){audio.SetContext(context);audio.Update(3);}
        static void SetPosition(AudioSource voice,int sample)
        {
            voice.timeSamples=sample;
            Assert.AreEqual(sample,voice.timeSamples,"fixture premise: Unity must retain the explicitly assigned synthetic PCM sample; this is not actual playback evidence");
        }
        [Test] public void CompletedDangerInterruptionResumesRegionalComposition()
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(Danger);
            Assert.False(root.GetComponentsInChildren<AudioSource>().Any(s=>s.name.StartsWith("Music")&&s.clip==organic),"regional voice must really have retired");
            SetPosition(Voice(danger),48000);Select(Field);
            Assert.AreEqual(96000,Voice(organic).timeSamples);Assert.AreEqual(1,audio.ActiveMusicSources);
            Select(Danger);Assert.AreEqual(48000,Voice(danger).timeSamples,"each composition retains its own position");
        }
        [Test] public void UnvisitedCompositionStartsAtZeroRatherThanOtherClipsSample()
        {
            Select(Field);SetPosition(Voice(organic),144000);Select(Dry);
            Assert.AreEqual(0,Voice(dry).timeSamples);Select(Field);Assert.AreEqual(144000,Voice(organic).timeSamples);
        }
        [TestCase(false)][TestCase(true)]
        public void ImmediateMusicMuteOrListenerPauseResumesCurrentComposition(bool listenerPause)
        {
            Select(Field);SetPosition(Voice(organic),120000);
            if(listenerPause)AudioListener.pause=true;else WorldAudioSettings.MusicVolume=0;
            audio.Update(.01f);Assert.AreEqual(0,audio.ActiveMusicSources);
            for(int i=0;i<20;i++)audio.Update(.5f);
            AudioListener.pause=false;WorldAudioSettings.MusicVolume=1;audio.Update(3);
            Assert.AreEqual(120000,Voice(organic).timeSamples);Assert.AreEqual(1,audio.ActiveMusicSources);
        }
        [Test] public void MutedContextChangeResumesOnlyTheCurrentRegionsOwnBookmark()
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(Dry);SetPosition(Voice(dry),48000);
            WorldAudioSettings.MusicVolume=0;audio.Update(.1f);audio.SetContext(Field);audio.Update(.1f);
            Assert.AreEqual(0,audio.ActiveMusicSources);WorldAudioSettings.MusicVolume=1;audio.Update(3);
            Assert.AreEqual("organic",audio.CurrentMusicId);Assert.AreEqual(96000,Voice(organic).timeSamples);
            Assert.False(root.GetComponentsInChildren<AudioSource>().Any(s=>s.name.StartsWith("Music")&&s.clip==dry));
            Select(Dry);Assert.AreEqual(48000,Voice(dry).timeSamples);
        }
        [Test] public void ContextChangedSeveralTimesWhileMutedDoesNotEraseExistingBookmarks()
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(Dry);SetPosition(Voice(dry),48000);Select(Danger);SetPosition(Voice(danger),72000);
            WorldAudioSettings.MusicVolume=0;audio.Update(.1f);
            foreach(var context in new[]{Field,Dry,Danger,Field}){audio.SetContext(context);audio.Update(.1f);Assert.AreEqual(0,audio.ActiveMusicSources);}
            WorldAudioSettings.MusicVolume=1;audio.Update(3);Assert.AreEqual(96000,Voice(organic).timeSamples);
            Select(Dry);Assert.AreEqual(48000,Voice(dry).timeSamples);Select(Danger);Assert.AreEqual(72000,Voice(danger).timeSamples);
        }
        [Test] public void ReturningBeforeFadeCompletesKeepsTheLiveVoiceInsteadOfSeekingOldBookmark()
        {
            Select(Field);SetPosition(Voice(organic),48000);Select(Danger);Select(Field);
            var live=Voice(organic);SetPosition(live,144000);audio.SetContext(Dry);audio.Update(.25f);int starts=audio.MusicStartCount;
            audio.SetContext(Field);audio.Update(.25f);
            Assert.AreSame(live,Voice(organic));Assert.AreEqual(144000,live.timeSamples);Assert.AreEqual(starts,audio.MusicStartCount);
            audio.Update(3);Assert.AreEqual(1,audio.ActiveMusicSources);
        }
        [Test] public void SharedUndergroundDangerClipHasOnePositionAndNoDuplicateVoice()
        {
            Select(Below);SetPosition(Voice(danger),144000);int starts=audio.MusicStartCount;
            audio.SetContext(Danger);audio.Update(.1f);Assert.AreEqual(starts,audio.MusicStartCount);Assert.AreEqual(144000,Voice(danger).timeSamples);
            Select(Field);Select(Below);Assert.AreEqual(144000,Voice(danger).timeSamples);Assert.AreEqual(1,audio.ActiveMusicSources);
        }
        [Test] public void DistinctMusicAliasesResumeTheSameRetiredClip()
        {
            library.AddMusic("dry",organic,.25f);Select(Field);SetPosition(Voice(organic),120000);Select(Danger);Select(Dry);
            Assert.AreEqual(120000,Voice(organic).timeSamples);Assert.AreEqual(.25f,Voice(organic).volume,.001f);
        }
        [Test] public void SilentContextRetiresButDoesNotForgetTheCurrentComposition()
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(default);
            Assert.AreEqual(0,audio.ActiveMusicSources);Select(Field);Assert.AreEqual(96000,Voice(organic).timeSamples);
        }
        [Test] public void GraphSoundCleanupKeepsCurrentMusicAndItsRetiredBookmark()
        {
            Select(Field);SetPosition(Voice(organic),120000);Select(Danger);SetPosition(Voice(danger),48000);int starts=audio.MusicStartCount;
            Assert.True(audio.TryPlay(WorldAudioCue.Pickup));audio.ClearSounds();audio.Update(.01f);
            Assert.AreEqual(0,audio.ActiveSoundSources);Assert.AreEqual(starts,audio.MusicStartCount);Assert.AreEqual(48000,Voice(danger).timeSamples);
            Select(Field);Assert.AreEqual(120000,Voice(organic).timeSamples);
        }
        [Test] public void AmbientLoopsAndOneShotsNeverReuseMusicBookmarks()
        {
            Select(Field);SetPosition(Voice(organic),144000);SetPosition(Voice(organic,"Ambience"),72000);
            Select(Danger);Select(default);Select(Field);
            Assert.AreEqual(144000,Voice(organic).timeSamples);Assert.AreEqual(0,Voice(organic,"Ambience").timeSamples);
            Assert.True(audio.TryPlay(WorldAudioCue.Pickup));Assert.AreEqual(0,Voice(organic,"Action").timeSamples);
        }
        [Test] public void ANewAudioLifetimeStartsAtTheBeginning()
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(Danger);audio.Dispose();
            audio=new WorldAudioPlayer(root.transform,library);Select(Field);Assert.AreEqual(0,Voice(organic).timeSamples);Assert.AreEqual(16,audio.AllocatedSources);
        }
        [Test] public void RebindingAnIdToANewClipDoesNotTransferTheOldClipsPosition()
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(Danger);
            var replacement=Clip("replacement organic");library.AddMusic("organic",replacement,.3f);Select(Field);Assert.AreEqual(0,Voice(replacement).timeSamples);
            SetPosition(Voice(replacement),48000);Select(Danger);library.AddMusic("organic",organic,.3f);Select(Field);Assert.AreEqual(96000,Voice(organic).timeSamples);
        }
        [TestCase(0)][TestCase(383999)]
        public void WrappedZeroAndLastValidPcmSampleReplaceAnOlderBookmark(int sample)
        {
            Select(Field);SetPosition(Voice(organic),96000);Select(Danger);Select(Field);
            SetPosition(Voice(organic),sample);Select(Danger);Select(Field);Assert.AreEqual(sample,Voice(organic).timeSamples);
        }
        [Test] public void BookmarkStorageIsBoundedWhenRegisteredMusicBindingsAreReplacedRepeatedly()
        {
            var visited=new List<AudioClip>();
            for(int i=0;i<20;i++)
            {
                var clip=Clip("visited "+i);visited.Add(clip);library.AddMusic("organic",clip,.3f);Select(Field);SetPosition(Voice(clip),1000+i);
            }
            Select(default);library.AddMusic("organic",visited[19],.3f);Select(Field);Assert.AreEqual(1019,Voice(visited[19]).timeSamples);
            Select(default);library.AddMusic("organic",visited[0],.3f);Select(Field);Assert.AreEqual(0,Voice(visited[0]).timeSamples,"the oldest entry must be evicted from the fixed sixteen-clip cache");
            Assert.AreEqual(16,audio.AllocatedSources);Assert.AreEqual(1,audio.ActiveMusicSources);
        }
        [Test] public void SameContextTicksDoNotSeekOrRestartAnAdvancingComposition()
        {
            Select(Field);SetPosition(Voice(organic),48000);Select(Danger);Select(Field);var live=Voice(organic);int starts=audio.MusicStartCount;
            SetPosition(live,144000);for(int i=0;i<30;i++){audio.SetContext(Field);audio.Update(.1f);}
            Assert.AreSame(live,Voice(organic));Assert.AreEqual(144000,live.timeSamples);Assert.AreEqual(starts,audio.MusicStartCount);Assert.AreEqual(16,audio.AllocatedSources);
        }
    }
}
