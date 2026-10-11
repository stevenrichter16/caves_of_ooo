using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    // First-import fixture: only pre-17 API symbols are statically referenced.
    public sealed class WorldAudioMusicSequenceBehaviorTests
    {
        AudioClip first,second;WorldAudioLibrary library;
        [SetUp] public void Setup(){first=AudioClip.Create("first",384000,1,48000,false);second=AudioClip.Create("second",384000,1,48000,false);library=new WorldAudioLibrary();library.AddMusic("organic",first,.3f);}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(first);Object.DestroyImmediate(second);}
        static MethodInfo Method(Type t,string name,params Type[] args){var m=t.GetMethod(name,args);Assert.NotNull(m,"Required playback behavior is absent: "+t.Name+"."+name);return m;}
        void Append(){Method(typeof(WorldAudioLibrary),"AddMusicChoice",typeof(string),typeof(AudioClip),typeof(float)).Invoke(library,new object[]{"organic",second,.3f});}
        object Sequence(){var t=typeof(WorldAudioLibrary).Assembly.GetType("CavesOfOoo.Rendering.WorldAudioMusicSequence");Assert.NotNull(t,"An unfinished score needs a session-local sequence cursor.");return Activator.CreateInstance(t,new object[]{library});}
        WorldAudioLibrary.Entry Select(object s,string id)=>(WorldAudioLibrary.Entry)Method(s.GetType(),"Select",typeof(string)).Invoke(s,new object[]{id});
        bool Complete(object s,AudioClip c)=>(bool)Method(s.GetType(),"Complete",typeof(AudioClip)).Invoke(s,new object[]{c});
        [Test] public void ActualMusicVoicesCanEndWhileAmbienceStillLoops()
        {
            var root=new GameObject("pre17 source behavior");WorldAudioPlayer player=null;
            try{player=new WorldAudioPlayer(root.transform,library);var voices=root.GetComponentsInChildren<AudioSource>();Assert.AreEqual(2,voices.Count(v=>v.name.StartsWith("Music ")));Assert.AreEqual(2,voices.Count(v=>v.name.StartsWith("Ambience ")));Assert.IsTrue(voices.Where(v=>v.name.StartsWith("Ambience ")).All(v=>v.loop));Assert.IsTrue(voices.Where(v=>v.name.StartsWith("Music ")).All(v=>!v.loop),"Looping a single waveform prevents the next full composition from beginning.");Assert.AreEqual(16,player.AllocatedSources);}
            finally{player?.Dispose();Object.DestroyImmediate(root);}
        }
        [Test] public void AppendingAChoicePreservesTheFirstCompatibilityBinding(){Append();Assert.AreSame(first,library.Music("organic").Clip);Assert.AreEqual(2,Method(typeof(WorldAudioLibrary),"MusicCount",typeof(string)).Invoke(library,new object[]{"organic"}));}
        [Test] public void AnUnfinishedPieceSurvivesAnotherContext(){Append();library.AddMusic("danger",second,.4f);var sequence=Sequence();Assert.AreSame(first,Select(sequence,"organic").Clip);Assert.AreSame(second,Select(sequence,"danger").Clip);Assert.AreSame(first,Select(sequence,"organic").Clip);}
        [Test] public void OnlyCompletionAdvancesAndDuplicateCompletionCannotSkip(){Append();var sequence=Sequence();Assert.AreSame(first,Select(sequence,"organic").Clip);Assert.True(Complete(sequence,first));Assert.AreSame(second,Select(sequence,"organic").Clip);Assert.False(Complete(sequence,first));Assert.AreSame(second,Select(sequence,"organic").Clip);}
        [Test] public void ExistingReplacementRemainsAReplacement(){Append();library.AddMusic("organic",second,.2f);Assert.AreSame(second,library.Music("organic").Clip);Assert.AreEqual(1,Method(typeof(WorldAudioLibrary),"MusicCount",typeof(string)).Invoke(library,new object[]{"organic"}));}
    }
}
