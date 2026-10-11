using System;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    // Reflection intentionally produces an NUnit assertion, not a missing-symbol compile error before18.
    public sealed class WorldAudioMusicIntermissionBehaviorTests
    {
        static object Rest(){var t=typeof(WorldAudioPlayer).Assembly.GetType("CavesOfOoo.Rendering.WorldAudioMusicIntermission");Assert.NotNull(t,"Calm natural completion currently lacks its bounded period of environmental silence.");return Activator.CreateInstance(t);}
        static object Call(object o,string name,Type[] types,params object[] args){var m=o.GetType().GetMethod(name,types);Assert.NotNull(m,name);return m.Invoke(o,args);}
        static void Complete(object r,string id)=>Call(r,"Complete",new[]{typeof(string)},id);
        static bool Wait(object r,string id)=>(bool)Call(r,"WaitFor",new[]{typeof(string)},id);
        static void Advance(object r,float delta,bool enabled)=>Call(r,"Advance",new[]{typeof(float),typeof(bool)},delta,enabled);
        static float Remaining(object r){var p=r.GetType().GetProperty("Remaining");Assert.NotNull(p);return (float)p.GetValue(r);}
        [Test] public void BootStartsImmediatelyButARealCalmCompletionHasTwelveSecondsRest(){var r=Rest();Assert.False(Wait(r,"organic"));Complete(r,"organic");Assert.True(Wait(r,"organic"));Assert.AreEqual(12,Remaining(r));Advance(r,11.5f,true);Assert.True(Wait(r,"organic"));Advance(r,.5f,true);Assert.False(Wait(r,"organic"));Assert.Zero(Remaining(r));}
        [Test] public void DangerCancelsTheRestRatherThanWaitingBehindIt(){var r=Rest();Complete(r,"organic");Assert.True(Wait(r,"organic"));Assert.False(Wait(r,"danger"));Assert.Zero(Remaining(r));Assert.False(Wait(r,"organic"));}
        [Test] public void AnotherRegionDoesNotInheritTheOldRegionsSilence(){var r=Rest();Complete(r,"organic");Assert.False(Wait(r,"sodden"));Assert.False(Wait(r,"organic"));}
        [Test] public void PausedOrMutedTimeDoesNotSpendTheRest(){var r=Rest();Complete(r,"organic");Advance(r,30,false);Assert.AreEqual(12,Remaining(r));Advance(r,4,true);Assert.AreEqual(8,Remaining(r));}
        [Test] public void DangerCompletionNeverCreatesAnotherRest(){var r=Rest();Complete(r,"danger");Assert.False(Wait(r,"danger"));Assert.Zero(Remaining(r));}
    }
}
