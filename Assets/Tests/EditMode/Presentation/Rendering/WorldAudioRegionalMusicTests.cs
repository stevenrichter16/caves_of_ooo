using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioRegionalMusicTests
    {
        [TestCase(BiomeType.Sodden,"sodden","water")]
        [TestCase(BiomeType.Grovelands,"grovelands","field")]
        [TestCase(BiomeType.Spread,"organic","field")]
        [TestCase(BiomeType.Jungle,"organic","field")]
        [TestCase(BiomeType.Beating,"dry",null)]
        [TestCase(BiomeType.Overwrit,"dry",null)]
        [TestCase(BiomeType.Stump,"underground","cave")]
        public void RegionalIdentitiesAreDistinctWithoutChangingEnvironmentPolicy(BiomeType biome,string music,string ambience)
        {
            var context=WorldAudioContext.Select(biome,0,false);Assert.AreEqual(music,context.MusicId);Assert.AreEqual(ambience,context.AmbienceId);
            Assert.False(context.Danger);
        }
        [TestCase(BiomeType.Sodden)][TestCase(BiomeType.Grovelands)]
        public void DangerAndUndergroundStillOverrideRegionalExploration(BiomeType biome)
        {
            Assert.AreEqual("danger",WorldAudioContext.Select(biome,0,true).MusicId);
            Assert.AreEqual("underground",WorldAudioContext.Select(biome,1,false).MusicId);
            Assert.AreEqual("danger",WorldAudioContext.Select(biome,1,true).MusicId);
        }
        [Test] public void NewCatalogBindsDifferentCompleteStreamedCompositions()
        {
            var library=WorldAudioLibrary.LoadResources();var sodden=library.Music("sodden");var grove=library.Music("grovelands");
            Assert.NotNull(sodden);Assert.NotNull(grove);Assert.AreNotSame(sodden.Clip,grove.Clip);
            foreach(var entry in new[]{sodden,grove})
            {
                Assert.Greater(entry.Clip.length,300);Assert.AreEqual(2,entry.Clip.channels);Assert.AreEqual(44100,entry.Clip.frequency);
                Assert.AreEqual(AudioClipLoadType.Streaming,entry.Clip.loadType);
                foreach(string id in new[]{"organic","dry","underground"})Assert.AreNotSame(library.Music(id).Clip,entry.Clip);
            }
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/SignalToNoiseNoPiano"),sodden.Clip);
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/Cirrus"),grove.Clip);
        }
        [Test] public void ExistingTracksAndSharedDangerWaveformRemainBound()
        {
            var library=WorldAudioLibrary.LoadResources();
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/Ephemera"),library.Music("organic").Clip);
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/ShadowsAndDust"),library.Music("dry").Clip);
            Assert.AreSame(library.Music("underground").Clip,library.Music("danger").Clip);
            Assert.Greater(library.Music("danger").Gain,library.Music("underground").Gain);
        }
        [Test] public void RealCreditsMenuContainsNewLicenseAndBothSuppliedMixes()
        {
            using(var isolation=new HotbarSaveFixture(false,false))
            {
                MessageLog.Clear();var menu=new PauseMenuController();menu.Open();menu.ClickSelect(PauseMenuController.AudioIndex,null,null);
                menu.ClickSelect(PauseMenuController.AudioCreditsIndex,null,null);string text=MessageLog.ConsumeAnnouncement();
                var credits=Resources.Load<TextAsset>("Audio/World/REGIONAL-MUSIC-CREDITS");Assert.NotNull(credits);StringAssert.Contains(credits.text,text);
                StringAssert.Contains("'Signal to Noise' by Scott Buckley - released under CC-BY 4.0.",text);
                StringAssert.Contains("'Cirrus' by Scott Buckley - released under CC-BY 4.0.",text);
                StringAssert.Contains("No Piano Melody",text);StringAssert.Contains("https://creativecommons.org/licenses/by/4.0/",text);
                foreach(string id in new[]{"CREDITS","PLACES-CREDITS","ATMOSPHERIC-CREDITS"})StringAssert.Contains(Resources.Load<TextAsset>("Audio/World/"+id).text,text);
            }
        }
        [Test] public void NewRegionsPreserveDifferentClipBookmarksAndFixedPool()
        {
            float oldMusic=WorldAudioSettings.MusicVolume;bool oldPause=AudioListener.pause;var root=new GameObject("regional-music-transition");
            var sodden=AudioClip.Create("sodden-test",48000*8,1,48000,false);var grove=AudioClip.Create("grove-test",48000*8,1,48000,false);
            WorldAudioPlayer player=null;
            try
            {
                WorldAudioSettings.MusicVolume=1;AudioListener.pause=false;var library=new WorldAudioLibrary();library.AddMusic("sodden",sodden,.3f);library.AddMusic("grovelands",grove,.3f);
                player=new WorldAudioPlayer(root.transform,library);player.SetContext(WorldAudioContext.Select(BiomeType.Sodden,0,false));player.Update(3);
                var voice=root.GetComponentsInChildren<AudioSource>().Single(s=>s.clip==sodden);voice.timeSamples=96000;Assert.AreEqual(96000,voice.timeSamples);
                player.SetContext(WorldAudioContext.Select(BiomeType.Grovelands,0,false));player.Update(3);
                Assert.AreEqual(0,root.GetComponentsInChildren<AudioSource>().Single(s=>s.clip==grove).timeSamples);
                player.SetContext(WorldAudioContext.Select(BiomeType.Sodden,0,false));player.Update(3);
                Assert.AreEqual(96000,root.GetComponentsInChildren<AudioSource>().Single(s=>s.clip==sodden).timeSamples);Assert.AreEqual(16,player.AllocatedSources);
            }
            finally{player?.Dispose();Object.DestroyImmediate(root);Object.DestroyImmediate(sodden);Object.DestroyImmediate(grove);WorldAudioSettings.MusicVolume=oldMusic;AudioListener.pause=oldPause;}
        }
    }
}
