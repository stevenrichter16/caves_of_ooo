using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioUtilityWaterResourceTests
    {
        [Test] public void ReachableCreditsIncludeTheCompleteUtilitySourceNotice()
        {
            using(var isolation=new HotbarSaveFixture(false,false))
            {
                MessageLog.Clear();var menu=new PauseMenuController();menu.Open();
                menu.ClickSelect(PauseMenuController.AudioIndex,null,null);menu.ClickSelect(PauseMenuController.AudioCreditsIndex,null,null);
                string text=MessageLog.ConsumeAnnouncement();var credits=Resources.Load<TextAsset>("Audio/World/UTILITY-CREDITS");
                Assert.NotNull(credits);StringAssert.Contains(credits.text,text);StringAssert.Contains("Vehicle",text);
            }
        }
        [Test] public void WaterUsesTwoRealShortClipsAndAnExplicitQuieterDrinkAlias()
        {
            Assert.True(Enum.TryParse("WaterFill",out WorldAudioCue fill));
            Assert.True(Enum.TryParse("WaterDrink",out WorldAudioCue drink));
            Assert.True(Enum.TryParse("WaterPour",out WorldAudioCue pour));
            var lib=WorldAudioLibrary.LoadResources();var f=lib.Sound(fill,0);var d=lib.Sound(drink,0);var p=lib.Sound(pour,0);
            Assert.NotNull(f);Assert.NotNull(d);Assert.NotNull(p);Assert.AreNotSame(f.Clip,p.Clip);Assert.AreSame(d.Clip,p.Clip);
            Assert.Less(d.Gain,p.Gain,"honest shared water movement, not a third gulp recording");
            foreach(var row in new[]{f,p})
            {Assert.NotNull(row.Clip);Assert.AreEqual(1,row.Clip.channels);Assert.That(row.Clip.length,Is.GreaterThan(.1f).And.LessThan(3.5f));Assert.AreEqual(AudioClipLoadType.DecompressOnLoad,row.Clip.loadType);}
            Assert.AreSame(f.Clip,lib.Sound(fill,1).Clip,"one source take must not imply a second recording");
        }
    }
}
