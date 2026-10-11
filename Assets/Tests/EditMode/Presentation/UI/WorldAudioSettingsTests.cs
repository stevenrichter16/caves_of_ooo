using System.Collections.Generic;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioSettingsTests
    {
        readonly Dictionary<string,float?> saved=new Dictionary<string,float?>();
        float music,ambience,sfx,oldSpell; SpellFxMode oldMode;
        const string Music="CavesOfOoo.Audio.Music", Ambience="CavesOfOoo.Audio.Ambience", Sfx="CavesOfOoo.Audio.Sfx";
        [SetUp] public void Setup()
        {
            music=WorldAudioSettings.MusicVolume; ambience=WorldAudioSettings.AmbienceVolume; sfx=WorldAudioSettings.SfxVolume;
            oldSpell=SpellFxSettings.SoundVolume; oldMode=SpellFxSettings.Mode;
            foreach(var key in new[]{Music,Ambience,Sfx}) {saved[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetFloat(key):(float?)null;PlayerPrefs.DeleteKey(key);}
        }
        [TearDown] public void Cleanup()
        {
            foreach(var pair in saved) if(pair.Value.HasValue)PlayerPrefs.SetFloat(pair.Key,pair.Value.Value);else PlayerPrefs.DeleteKey(pair.Key);
            WorldAudioSettings.MusicVolume=music;WorldAudioSettings.AmbienceVolume=ambience;WorldAudioSettings.SfxVolume=sfx;
            SpellFxSettings.SoundVolume=oldSpell;SpellFxSettings.Mode=oldMode;
        }
        [Test] public void DefaultsAreRestrainedAndDoNotWritePreferencesOrOverrideLegacySpellGain()
        {
            SpellFxSettings.SoundVolume=.25f;WorldAudioSettings.Load();
            Assert.AreEqual(.55f,WorldAudioSettings.MusicVolume);Assert.AreEqual(.45f,WorldAudioSettings.AmbienceVolume);Assert.AreEqual(1,WorldAudioSettings.SfxVolume);
            Assert.AreEqual(.25f,SpellFxSettings.SoundVolume);Assert.False(PlayerPrefs.HasKey(Music));Assert.False(PlayerPrefs.HasKey(Sfx));
        }
        [Test] public void ExplicitIndependentLevelsRoundTripIncludingMusicOff()
        {
            WorldAudioSettings.MusicVolume=0;WorldAudioSettings.AmbienceVolume=.3f;WorldAudioSettings.SfxVolume=.7f;WorldAudioSettings.Save();
            WorldAudioSettings.MusicVolume=WorldAudioSettings.AmbienceVolume=WorldAudioSettings.SfxVolume=1;WorldAudioSettings.Load();
            Assert.AreEqual(0,WorldAudioSettings.MusicVolume);Assert.AreEqual(.3f,WorldAudioSettings.AmbienceVolume);Assert.AreEqual(.7f,WorldAudioSettings.SfxVolume);
        }
        [TestCase(float.NaN)] [TestCase(float.NegativeInfinity)] [TestCase(float.PositiveInfinity)]
        public void InvalidLevelsUseFiniteDefaults(float invalid)
        {
            WorldAudioSettings.MusicVolume=invalid;WorldAudioSettings.AmbienceVolume=invalid;WorldAudioSettings.SfxVolume=invalid;
            Assert.AreEqual(.55f,WorldAudioSettings.MusicVolume);Assert.AreEqual(.45f,WorldAudioSettings.AmbienceVolume);Assert.AreEqual(1,WorldAudioSettings.SfxVolume);
        }
        [Test] public void LevelsClampAtSilenceAndUnityRange()
        {
            WorldAudioSettings.MusicVolume=-2;WorldAudioSettings.AmbienceVolume=3;WorldAudioSettings.SfxVolume=-1;
            Assert.AreEqual(0,WorldAudioSettings.MusicVolume);Assert.AreEqual(1,WorldAudioSettings.AmbienceVolume);Assert.AreEqual(0,WorldAudioSettings.SfxVolume);
        }
        [Test] public void AudioPageChangesOnlySelectedBusAndBackReturnsToAudioEntry()
        {
            WorldAudioSettings.MusicVolume=.5f;WorldAudioSettings.AmbienceVolume=.4f;WorldAudioSettings.SfxVolume=.7f;SpellFxSettings.Mode=SpellFxMode.Reduced;
            var menu=new PauseMenuController();menu.Open();menu.ClickSelect(PauseMenuController.AudioIndex,null,null);
            Assert.True(menu.IsAudioOpen);Assert.True(menu.IsOpen);Assert.AreEqual(5,menu.VisibleItemCount);
            menu.MoveSelectionForTest(PauseMenuController.MusicVolumeIndex);menu.Tick(new Probe(KeyCode.RightArrow),null,null);
            Assert.AreEqual(.6f,WorldAudioSettings.MusicVolume,.0001f);Assert.AreEqual(.4f,WorldAudioSettings.AmbienceVolume);Assert.AreEqual(.7f,WorldAudioSettings.SfxVolume);
            Assert.AreEqual(SpellFxMode.Reduced,SpellFxSettings.Mode);Assert.AreEqual(.6f,PlayerPrefs.GetFloat(Music),.0001f);
            menu.Tick(new Probe(KeyCode.Escape),null,null);Assert.True(menu.IsOpen);Assert.False(menu.IsAudioOpen);Assert.AreEqual(PauseMenuController.AudioIndex,menu.SelectedIndex);
            menu.Tick(new Probe(KeyCode.Escape),null,null);Assert.False(menu.IsOpen);
        }
        [Test] public void AudioPageTabClosesAllAndReopenResetsToSave()
        {
            var menu=new PauseMenuController();menu.Open();menu.ClickSelect(PauseMenuController.AudioIndex,null,null);
            menu.Tick(new Probe(KeyCode.Tab),null,null);Assert.False(menu.IsOpen);Assert.False(menu.IsAudioOpen);
            menu.Open();Assert.AreEqual(PauseMenuController.SaveIndex,menu.SelectedIndex);Assert.False(menu.IsAudioOpen);
        }
        sealed class Probe:IInputProbe
        {readonly KeyCode key;public Probe(KeyCode value){key=value;}public bool GetKeyDown(KeyCode value)=>value==key;}
    }
}
