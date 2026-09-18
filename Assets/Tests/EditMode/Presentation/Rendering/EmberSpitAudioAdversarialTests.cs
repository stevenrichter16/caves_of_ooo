using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class EmberSpitAudioAdversarialTests
    {
        EmberSpitAudioPlayer audio; Zone zone; float volume,speed; SpellFxMode mode; bool pause;
        [SetUp] public void Setup()
        {
            volume=SpellFxSettings.SoundVolume;speed=SpellFxSettings.AnimationSpeed;mode=SpellFxSettings.Mode;pause=AudioListener.pause;
            SpellFxSettings.SoundVolume=1;SpellFxSettings.AnimationSpeed=1;SpellFxSettings.Mode=SpellFxMode.Full;AudioListener.pause=false;
            zone=new Zone("audio-adversarial");foreach(var c in zone.Cells){c.Explored=true;c.IsVisible=true;}
            audio=new EmberSpitAudioPlayer();audio.SetZone(zone);
        }
        [TearDown] public void Teardown()
        {audio.Dispose();SpellFxSettings.SoundVolume=volume;SpellFxSettings.AnimationSpeed=speed;SpellFxSettings.Mode=mode;AudioListener.pause=pause;}
        SpellFxSequence Sequence(bool died=false,bool resisted=false)=>new SpellFxSequence("Pyromancy_EmberSpit",zone,null,new Point(10,10),
            new[]{new Point(11,10)},targets:new[]{new SpellFxTargetResult("target",new Point(11,10),new Point(-1,-1),resisted?0:3,resisted,died)});
        bool Play()=>audio.Play(Sequence(),.245f,new Point(11,10),true);
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(float.NegativeInfinity)][TestCase(-1f)][TestCase(5f)][TestCase(float.MaxValue)]
        public void MalformedContactNeverArmsDelayedImpact(float at)
        {Assert.IsFalse(audio.Play(Sequence(),at,new Point(11,10),true));Assert.AreEqual(0,audio.ActiveVoices);}
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)]
        public void MalformedDeltasDoNotPoisonLaterValidContact(float dt)
        {Play();audio.Update(dt,dt,dt);Assert.AreEqual(0,audio.ImpactCount);audio.Update(.25f,.25f,.25f);Assert.AreEqual(1,audio.ImpactCount);}
        [TestCase(true)][TestCase(false)]
        public void LostHierarchyCancelsOldVoiceAndCanPrepareNextCast(bool wholeRoot)
        {
            Play();if(wholeRoot)UnityEngine.Object.DestroyImmediate(audio.Root.gameObject);
            else UnityEngine.Object.DestroyImmediate(audio.Root.GetComponentInChildren<AudioSource>());
            audio.Update(.3f,.3f,.3f);Assert.AreEqual(0,audio.ImpactCount);Assert.AreEqual(0,audio.ActiveVoices);
            Assert.IsTrue(audio.Prepare());Assert.IsTrue(Play());Assert.AreEqual(12,audio.AllocatedSources);
        }
        [TestCase(true,false)][TestCase(false,true)]
        public void CopiedDeathOrResistanceStillHasOnePhysicalHit(bool died,bool resisted)
        {Assert.IsTrue(audio.Play(Sequence(died,resisted),.245f,new Point(11,10),true));audio.Update(.3f,.3f,.3f);Assert.AreEqual(1,audio.ImpactCount);}
        [Test] public void VisibleButUnexploredContactDoesNotLeakHit()
        {Play();zone.GetCell(11,10).Explored=false;audio.Update(.3f,.3f,.3f);Assert.AreEqual(0,audio.ImpactCount);}
        [Test] public void ListenerPauseClearsAndDoesNotResumeOldCast()
        {Play();AudioListener.pause=true;audio.Update(.3f,.3f,.3f);Assert.AreEqual(0,audio.ActiveVoices);Assert.IsFalse(Play());AudioListener.pause=false;audio.Update(.3f,.3f,.3f);Assert.AreEqual(0,audio.ImpactCount);Assert.IsTrue(Play());}
        [Test] public void DisposedPlayerCannotBeResurrectedByZoneBinding()
        {audio.Dispose();audio.SetZone(zone);Assert.IsFalse(audio.Prepare());Assert.IsFalse(Play());Assert.AreEqual(0,audio.AllocatedSources);}
        [Test] public void NullZoneCancelsAndRejectsUntilRebound()
        {Play();audio.SetZone(null);Assert.IsFalse(Play());Assert.AreEqual(0,audio.ActiveVoices);audio.SetZone(zone);Assert.IsTrue(Play());}
        [Test] public void NullRequestIsHarmless()
        {Assert.IsFalse(audio.Play(null,.2f,new Point(11,10),false));Assert.AreEqual(0,audio.ActiveVoices);}
        [Test] public void ClipsArePreloadedPcmWithoutMonoRenormalization()
        {
            foreach(string path in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/EmberSpit"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var s=importer.defaultSampleSettings;
                Assert.IsFalse(importer.forceToMono);Assert.IsFalse(importer.loadInBackground);
                Assert.AreEqual(AudioCompressionFormat.PCM,s.compressionFormat);Assert.IsTrue(s.preloadAudioData);
                Assert.AreEqual(AudioClipLoadType.DecompressOnLoad,s.loadType);Assert.AreEqual(AudioSampleRateSetting.PreserveSampleRate,s.sampleRateSetting);
            }
        }
        [Test] public void SteadyAudioClockDoesNotAllocateManagedMemory()
        {
            Play();audio.Update(0,0,0);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)audio.Update(.0001f,.0001f,.0001f);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.AreEqual(0,allocated);Assert.AreEqual(1,audio.ActiveVoices);
        }
        [Test] public void NativeCoordinatorUsesFilteredEndpointAndContactNotRawDuplicateTail()
        {
            using(var f=new NativeSpellFixture())
            {
                var go=new GameObject("audio-native-map",typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(f.Host.transform);
                using(var world=new WorldFxCoordinator(new AsciiFxRenderer(go.GetComponent<Tilemap>()),f.Host.transform,f.Library))
                {
                    world.SetZone(f.Zone);world.SetNativeSurface(f.Surface);
                    var sequence=f.Sequence(path:new[]{new Point(10,10),new Point(11,10),new Point(13,10),new Point(11,10),new Point(-1,-1)},
                        targets:new[]{NativeSpellFixture.Hit("physical",13,10)});
                    world.Play(sequence);Assert.AreEqual(new Point(13,10),world.NativeRenderer.LastContactCell);
                    Assert.That(world.NativeRenderer.LastContactSeconds,Is.EqualTo(.27f).Within(.00001));
                    world.Update(.269f);Assert.AreEqual(0,world.EmberAudio.ImpactCount);
                    world.Update(.002f);Assert.AreEqual(1,world.EmberAudio.ImpactCount);
                    world.SetNativeSurface(null);Assert.AreEqual(0,world.EmberAudio.ActiveVoices);
                }
            }
        }
    }
}
