using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class WorldAudioMusicIntermissionTests
    {
        [Test] public void FreshBootHasNoArtificialDelay(){var gap=new WorldAudioMusicIntermission();Assert.False(gap.WaitFor("organic"));Assert.Zero(gap.Remaining);}
        [Test] public void CalmNaturalCompletionMakesOneTwelveSecondRest(){var gap=new WorldAudioMusicIntermission();gap.Complete("organic");Assert.True(gap.WaitFor("organic"));Assert.AreEqual(12,gap.Remaining);gap.Advance(11.5f,true);Assert.True(gap.WaitFor("organic"));gap.Advance(.5f,true);Assert.False(gap.WaitFor("organic"));Assert.Zero(gap.Remaining);}
        [Test] public void DangerImmediateSelectionCancelsRest(){var gap=new WorldAudioMusicIntermission();gap.Complete("organic");gap.Advance(5,true);Assert.False(gap.WaitFor("danger"));Assert.Zero(gap.Remaining);Assert.False(gap.WaitFor("organic"));}
        [Test] public void OtherPlaceDoesNotInheritThePreviousPlacesSilence(){var gap=new WorldAudioMusicIntermission();gap.Complete("organic");Assert.False(gap.WaitFor("sodden"));Assert.False(gap.WaitFor("organic"));}
        [Test] public void SilentInvalidContextCancelsRest(){var gap=new WorldAudioMusicIntermission();gap.Complete("organic");Assert.False(gap.WaitFor(null));Assert.Zero(gap.Remaining);}
        [TestCase(null)][TestCase("")][TestCase(" ")][TestCase("danger")]
        public void NonCalmCompletionNeverStartsRest(string id){var gap=new WorldAudioMusicIntermission();gap.Complete(id);Assert.Zero(gap.Remaining);}
        [Test] public void MutedOrPausedAudioCannotSpendTheRest(){var gap=new WorldAudioMusicIntermission();gap.Complete("sodden");gap.Advance(30,false);Assert.AreEqual(12,gap.Remaining);gap.Advance(4,true);Assert.AreEqual(8,gap.Remaining);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)][TestCase(0f)]
        public void MalformedDeltaDoesNotAdvance(float delta){var gap=new WorldAudioMusicIntermission();gap.Complete("organic");gap.Advance(delta,true);Assert.AreEqual(12,gap.Remaining);}
        [Test] public void SplitWallTimeHasTheSameBoundary(){var a=new WorldAudioMusicIntermission();var b=new WorldAudioMusicIntermission();a.Complete("organic");b.Complete("organic");a.Advance(10,true);for(int i=0;i<40;i++)b.Advance(.25f,true);Assert.AreEqual(a.Remaining,b.Remaining);a.Advance(30,true);Assert.Zero(a.Remaining);}
        [Test] public void ExplicitClearCannotResumeOldRest(){var gap=new WorldAudioMusicIntermission();gap.Complete("organic");gap.Clear();Assert.False(gap.WaitFor("organic"));Assert.Zero(gap.Remaining);}
    }
}
