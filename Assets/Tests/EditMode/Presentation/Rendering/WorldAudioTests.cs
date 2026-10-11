using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioPolicyTests
    {
        [TestCase(BiomeType.Spread, "organic", "field")]
        [TestCase(BiomeType.Jungle, "organic", "field")]
        [TestCase(BiomeType.Grovelands, "grovelands", "field")]
        [TestCase(BiomeType.Sodden, "sodden", "water")]
        [TestCase(BiomeType.Desert, "dry", null)]
        [TestCase(BiomeType.Beating, "dry", null)]
        [TestCase(BiomeType.Overwrit, "dry", null)]
        [TestCase(BiomeType.Cave, "underground", "cave")]
        [TestCase(BiomeType.Ruins, "underground", "cave")]
        [TestCase(BiomeType.Stump, "underground", "cave")]
        public void KnownBiomesChoosePlaceIdentity(BiomeType biome, string music, string ambience)
        {
            var context = WorldAudioContext.Select(biome, 0, false);
            Assert.AreEqual(music, context.MusicId); Assert.AreEqual(ambience, context.AmbienceId);
            var dangerous = WorldAudioContext.Select(biome, 0, true);
            Assert.AreEqual("danger", dangerous.MusicId); Assert.AreEqual(ambience, dangerous.AmbienceId);
        }
        [Test] public void DepthOverridesSurfaceButDoesNotManufactureDanger()
        {
            var above = WorldAudioContext.Select(BiomeType.Sodden, 0, false);
            var below = WorldAudioContext.Select(BiomeType.Sodden, 2, false);
            Assert.AreEqual("sodden", above.MusicId); Assert.AreEqual("underground", below.MusicId);
            Assert.AreEqual("cave", below.AmbienceId); Assert.False(below.Danger);
        }
    }

    public sealed class WorldAudioPlayerTests
    {
        GameObject root; AudioClip clip, other; WorldAudioPlayer player;
        float music, ambience, sfx; bool paused; SpellFxMode mode; float speed;
        [SetUp] public void Setup()
        {
            music = WorldAudioSettings.MusicVolume; ambience = WorldAudioSettings.AmbienceVolume; sfx = WorldAudioSettings.SfxVolume;
            paused = AudioListener.pause; mode = SpellFxSettings.Mode; speed = SpellFxSettings.AnimationSpeed;
            WorldAudioSettings.MusicVolume = WorldAudioSettings.AmbienceVolume = WorldAudioSettings.SfxVolume = 1;
            AudioListener.pause = false; root = new GameObject("world-audio-test");
            clip = AudioClip.Create("organic test", 48000, 1, 48000, false);
            other = AudioClip.Create("cave test", 48000, 1, 48000, false);
            var library = new WorldAudioLibrary();
            library.AddMusic("organic", clip, .3f); library.AddMusic("dry", clip, .25f);
            library.AddMusic("underground", other, .3f); library.AddMusic("danger", other, .4f);
            library.AddAmbience("field", clip, .2f); library.AddAmbience("water", other, .2f); library.AddAmbience("cave", other, .2f);
            foreach (WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue))) library.AddSound(cue, clip, .4f);
            player = new WorldAudioPlayer(root.transform, library);
        }
        [TearDown] public void Cleanup()
        {
            player?.Dispose(); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(clip); UnityEngine.Object.DestroyImmediate(other);
            WorldAudioSettings.MusicVolume = music; WorldAudioSettings.AmbienceVolume = ambience; WorldAudioSettings.SfxVolume = sfx;
            AudioListener.pause = paused; SpellFxSettings.Mode = mode; SpellFxSettings.AnimationSpeed = speed;
        }
        [Test] public void FixedPoolAndSameContextNeverRestartTracks()
        {
            Assert.AreEqual(16, player.AllocatedSources);
            var context = WorldAudioContext.Select(BiomeType.Spread, 0, false);
            player.SetContext(context); player.Update(3); int starts = player.MusicStartCount;
            for (int i=0;i<30;i++) { player.SetContext(context); player.Update(.016f); }
            Assert.AreEqual(starts, player.MusicStartCount); Assert.AreEqual(1, starts); Assert.AreEqual(16, player.AllocatedSources);
        }
        [Test] public void PlaceChangeCrossfadesAndRetiresOldVoice()
        {
            player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false)); player.Update(3);
            player.SetContext(WorldAudioContext.Select(BiomeType.Cave,1,false)); player.Update(.25f);
            Assert.AreEqual(2, player.ActiveMusicSources); Assert.AreEqual("underground", player.CurrentMusicId);
            player.Update(3); Assert.AreEqual(1, player.ActiveMusicSources); Assert.AreEqual(2, player.MusicStartCount);
        }
        [Test] public void MusicAndAmbienceIgnoreSpellModeSpeedAndSfxMute()
        {
            SpellFxSettings.Mode=SpellFxMode.Off; SpellFxSettings.AnimationSpeed=4; WorldAudioSettings.SfxVolume=0;
            player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false)); player.Update(3);
            Assert.AreEqual(1,player.ActiveMusicSources); Assert.AreEqual(1,player.ActiveAmbienceSources);
            Assert.False(player.TryPlay(WorldAudioCue.StepSoft));
            foreach(var source in root.GetComponentsInChildren<AudioSource>()) Assert.AreEqual(1,source.pitch);
        }
        [Test] public void IndependentMutesDoNotStopOtherBusesOrReplayMutedActions()
        {
            player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false)); player.Update(3);
            WorldAudioSettings.MusicVolume=0; player.Update(.1f);
            Assert.AreEqual(0,player.ActiveMusicSources); Assert.AreEqual(1,player.ActiveAmbienceSources);
            Assert.True(player.TryPlay(WorldAudioCue.Pickup)); int count=player.AcceptedSoundCount;
            WorldAudioSettings.SfxVolume=0; player.Update(.1f); Assert.AreEqual(0,player.ActiveSoundSources);
            Assert.False(player.TryPlay(WorldAudioCue.Pickup)); WorldAudioSettings.SfxVolume=1; player.Update(1);
            Assert.AreEqual(count,player.AcceptedSoundCount); Assert.AreEqual(0,player.ActiveSoundSources);
            WorldAudioSettings.MusicVolume=1; player.Update(3); Assert.AreEqual(1,player.ActiveMusicSources);
        }
        [Test] public void SoundBurstIsBoundedAndReleasesWithoutSourceGrowth()
        {
            int accepted=0;
            for(int i=0;i<200;i++) if(player.TryPlay((WorldAudioCue)(i%Enum.GetValues(typeof(WorldAudioCue)).Length))) accepted++;
            Assert.Greater(accepted,0); Assert.LessOrEqual(accepted,12); Assert.LessOrEqual(player.ActiveSoundSources,12);
            player.Update(3); Assert.AreEqual(0,player.ActiveSoundSources); Assert.AreEqual(16,player.AllocatedSources);
            Assert.True(player.TryPlay(WorldAudioCue.Pickup));
        }
        [Test] public void RepeatedIdenticalCueIsRateLimitedButCanRearm()
        {
            Assert.True(player.TryPlay(WorldAudioCue.StepSoft)); Assert.False(player.TryPlay(WorldAudioCue.StepSoft));
            player.Update(.3f); Assert.True(player.TryPlay(WorldAudioCue.StepSoft));
        }
        [Test] public void PausedListenerAndDisposeCannotLeakLateSounds()
        {
            Assert.True(player.TryPlay(WorldAudioCue.Pickup)); AudioListener.pause=true; player.Update(.1f);
            Assert.AreEqual(0,player.ActiveSoundSources); Assert.False(player.TryPlay(WorldAudioCue.Equip));
            AudioListener.pause=false; player.Update(2); Assert.AreEqual(0,player.ActiveSoundSources);
            player.Dispose(); Assert.False(player.TryPlay(WorldAudioCue.Pickup)); Assert.AreEqual(0,player.AllocatedSources);
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1f)]
        public void MalformedTimeDoesNotCorruptAudioGain(float delta)
        {
            player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false)); player.Update(delta);
            foreach(var source in root.GetComponentsInChildren<AudioSource>())
                Assert.True(float.IsFinite(source.volume)&&source.volume>=0&&source.volume<=1);
            player.Update(3); Assert.AreEqual(1,player.ActiveMusicSources);
        }
        [Test] public void MissingCueIsSilentAndDoesNotAllocateReplacement()
        {
            player.Dispose(); player=new WorldAudioPlayer(root.transform,new WorldAudioLibrary());
            player.SetContext(WorldAudioContext.Select(BiomeType.Spread,0,false)); player.Update(1);
            Assert.AreEqual(0,player.ActiveMusicSources); Assert.False(player.TryPlay(WorldAudioCue.Pickup));
        }
    }
}
