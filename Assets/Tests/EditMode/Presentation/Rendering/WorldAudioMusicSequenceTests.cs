using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioMusicSequenceTests
    {
        AudioClip first,second,third;WorldAudioLibrary library;
        [SetUp] public void SetUp()
        {
            first=AudioClip.Create("first",48000*8,1,48000,false);second=AudioClip.Create("second",48000*8,1,48000,false);third=AudioClip.Create("danger",48000*8,1,48000,false);
            library=new WorldAudioLibrary();library.AddMusic("organic",first,.3f);library.AddMusicChoice("organic",second,.3f);library.AddMusic("danger",third,.4f);library.AddMusic("underground",third,.3f);
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(first);Object.DestroyImmediate(second);Object.DestroyImmediate(third);}
        [Test] public void ExplicitAppendMakesTwoChoicesWithoutChangingFirstCompatibilityBinding()
        {Assert.AreEqual(2,library.MusicCount("organic"));Assert.AreSame(first,library.Music("organic").Clip);Assert.AreSame(second,library.Music("organic",1).Clip);}
        [Test] public void OriginalAddMusicStillReplacesTheBinding()
        {library.AddMusic("organic",third,.2f);Assert.AreEqual(1,library.MusicCount("organic"));Assert.AreSame(third,library.Music("organic").Clip);}
        [Test] public void DuplicateClipRowsCannotPretendToBeVariety()
        {library.AddMusicChoice("organic",first,.1f);Assert.AreEqual(2,library.MusicCount("organic"));Assert.AreEqual(.3f,library.Music("organic").Gain);}
        [TestCase(null)][TestCase("")][TestCase(" ")]
        public void InvalidIdsNeverCreateASequence(string id)
        {library.AddMusicChoice(id,first,.3f);Assert.AreEqual(0,library.MusicCount(id));Assert.Null(library.Music(id));}
        [Test] public void MissingDestroyedAndInvalidChoicesDoNotPoisonExistingMusic()
        {
            library.AddMusicChoice("organic",null,.3f);library.AddMusicChoice("organic",third,float.NaN);Assert.AreEqual(2,library.MusicCount("organic"));
            Object.DestroyImmediate(first);Assert.AreEqual(1,library.MusicCount("organic"));Assert.AreSame(second,library.Music("organic").Clip);
            Assert.AreEqual(0,library.MusicCount("missing"));Assert.Null(library.Music("missing"));
        }
        [Test] public void SelectionAndContextInterruptionsDoNotRotateAnUnfinishedComposition()
        {
            var sequence=new WorldAudioMusicSequence(library);Assert.AreSame(first,sequence.Select("organic").Clip);
            for(int i=0;i<100;i++)Assert.AreSame(first,sequence.Select("organic").Clip);
            Assert.AreSame(third,sequence.Select("danger").Clip);Assert.AreSame(first,sequence.Select("organic").Clip);
        }
        [Test] public void CompletionRotatesOnlyTheFinishedContextsAndDoesNotRepeatImmediately()
        {
            var sequence=new WorldAudioMusicSequence(library);sequence.Select("organic");sequence.Select("danger");
            Assert.True(sequence.Complete(first));Assert.AreSame(third,sequence.Select("danger").Clip);Assert.AreSame(second,sequence.Select("organic").Clip);
            Assert.False(sequence.Complete(first),"the completed outgoing piece cannot advance twice");Assert.AreSame(second,sequence.Select("organic").Clip);
            Assert.True(sequence.Complete(second));Assert.AreSame(first,sequence.Select("organic").Clip);
        }
        [Test] public void OneClipAliasesKeepTheirActualGainAndRemainSelectableAfterCompletion()
        {
            var sequence=new WorldAudioMusicSequence(library);sequence.Select("underground");sequence.Select("danger");Assert.True(sequence.Complete(third));
            Assert.AreSame(third,sequence.Select("underground").Clip);Assert.AreEqual(.3f,sequence.Select("underground").Gain);
            Assert.AreSame(third,sequence.Select("danger").Clip);Assert.AreEqual(.4f,sequence.Select("danger").Gain);
        }
        [Test] public void FreshSequenceAndExplicitReplacementDoNotInheritUnrelatedCursorPositions()
        {
            var sequence=new WorldAudioMusicSequence(library);sequence.Select("organic");sequence.Complete(first);Assert.AreSame(second,sequence.Select("organic").Clip);
            Assert.AreSame(first,new WorldAudioMusicSequence(library).Select("organic").Clip);
            library.AddMusic("organic",third,.2f);Assert.AreSame(third,sequence.Select("organic").Clip);
        }
        [Test] public void MissingAndSilentContextsDoNotConsumeOtherChoices()
        {
            var sequence=new WorldAudioMusicSequence(library);sequence.Select("organic");Assert.Null(sequence.Select(null));Assert.Null(sequence.Select("missing"));
            Assert.False(sequence.Complete(third));Assert.AreSame(first,sequence.Select("organic").Clip);
        }
        [Test] public void GenuineObservedEndCanBeAcceptedOnlyOnce()
        {
            var watch=new WorldAudioPlaybackWatch();Assert.False(watch.Observe(first,true,0,10));Assert.False(watch.Observe(first,true,7*48000,17));
            Assert.True(watch.Observe(first,false,0,18.01));Assert.False(watch.Observe(first,false,0,20));
        }
        [Test] public void NeverStartedStreamingSourceIsNotACompletedComposition()
        {
            var watch=new WorldAudioPlaybackWatch();Assert.False(watch.Observe(first,false,0,10));Assert.False(watch.Observe(first,false,0,100));
            Assert.False(watch.Observe(first,true,0,101));Assert.True(watch.Observe(first,false,0,109.01));
        }
        [Test] public void PrematureStopAndSampleRewindDoNotClaimNaturalCompletion()
        {
            var watch=new WorldAudioPlaybackWatch();watch.Observe(first,true,48000,10);Assert.False(watch.Observe(first,false,0,10.2));
            Assert.False(watch.Observe(first,true,0,10.3));Assert.False(watch.Observe(first,true,7*48000,17.3));Assert.True(watch.Observe(first,false,0,18.31));
        }
        [Test] public void ExplicitRetirementResetProtectsMuteAndPauseFromEndDetection()
        {
            var watch=new WorldAudioPlaybackWatch();watch.Observe(first,true,7*48000,10);watch.Reset();Assert.False(watch.Observe(first,false,0,99));
            Assert.False(watch.Observe(first,true,7*48000,100));Assert.True(watch.Observe(first,false,0,101.01));
        }
        [Test] public void AStoppedEarlySourceCannotCompleteByRemainingSilent()
        {
            var watch=new WorldAudioPlaybackWatch();watch.Observe(first,true,48000,10);
            Assert.False(watch.Observe(first,false,0,10.2));Assert.False(watch.Observe(first,false,0,100));
            Assert.False(watch.Observe(first,true,48000,101));Assert.True(watch.Observe(first,false,0,108.01));
        }
        [Test] public void ClipSwitchCannotReuseAnotherTracksObservedEnd()
        {
            var watch=new WorldAudioPlaybackWatch();watch.Observe(first,true,7*48000,10);Assert.False(watch.Observe(second,false,0,20));
            Assert.False(watch.Observe(second,true,0,21));Assert.True(watch.Observe(second,false,0,29.01));
        }
        [TestCase(double.NaN)][TestCase(double.PositiveInfinity)][TestCase(-1d)]
        public void MalformedDspTimeCannotManufactureCompletion(double time)
        {var watch=new WorldAudioPlaybackWatch();watch.Observe(first,true,7*48000,10);Assert.False(watch.Observe(first,false,0,time));}
        [TestCase(-1)][TestCase(384000)]
        public void InvalidSampleCannotManufactureCompletion(int sample)
        {var watch=new WorldAudioPlaybackWatch();watch.Observe(first,true,7*48000,10);Assert.False(watch.Observe(first,false,sample,20));}
        [Test] public void MusicUsesNonLoopingSourcesWhileEnvironmentStillLoops()
        {
            var root=new GameObject("sequence-source-contract");WorldAudioPlayer player=null;
            try
            {
                player=new WorldAudioPlayer(root.transform,library);Assert.AreEqual(16,player.AllocatedSources);
                foreach(var source in root.GetComponentsInChildren<AudioSource>())
                {if(source.name.StartsWith("Music "))Assert.False(source.loop);if(source.name.StartsWith("Ambience "))Assert.True(source.loop);}
            }
            finally{player?.Dispose();Object.DestroyImmediate(root);}
        }
        [Test] public void PreparedOrganicCatalogContainsTwoDifferentFullCompositions()
        {
            var actual=WorldAudioLibrary.LoadResources();Assert.AreEqual(2,actual.MusicCount("organic"));
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/Ephemera"),actual.Music("organic",0).Clip);
            Assert.AreSame(Resources.Load<AudioClip>("Audio/World/Music/Meanwhile"),actual.Music("organic",1).Clip);
            Assert.Greater(actual.Music("organic",1).Clip.length,300);Assert.AreEqual(AudioClipLoadType.Streaming,actual.Music("organic",1).Clip.loadType);
        }
    }
}
