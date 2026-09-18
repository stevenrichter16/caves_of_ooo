using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class EmberSpitAudioTests
    {
        GameObject root;
        Zone zone;
        EmberSpitAudioPlayer audio;
        float oldVolume, oldSpeed;
        SpellFxMode oldMode;
        [SetUp] public void Setup()
        {
            oldVolume=SpellFxSettings.SoundVolume;oldSpeed=SpellFxSettings.AnimationSpeed;oldMode=SpellFxSettings.Mode;
            SpellFxSettings.SoundVolume=1;SpellFxSettings.AnimationSpeed=1;SpellFxSettings.Mode=SpellFxMode.Full;
            root=new GameObject("Ember audio test");zone=new Zone("EmberAudio");
            foreach(var c in zone.Cells){c.IsVisible=true;c.Explored=true;}
            audio=new EmberSpitAudioPlayer(root.transform);audio.SetZone(zone);
        }
        [TearDown] public void Cleanup()
        {
            audio.Dispose();UnityEngine.Object.DestroyImmediate(root);
            SpellFxSettings.SoundVolume=oldVolume;SpellFxSettings.AnimationSpeed=oldSpeed;SpellFxSettings.Mode=oldMode;
        }
        SpellFxSequence Sequence(bool hit=true,int seed=0,string spell="Pyromancy_EmberSpit",bool duplicate=false)
        {
            var target=new SpellFxTargetResult("target",new Point(13,10),new Point(13,10),3);
            return new SpellFxSequence(spell,zone,null,new Point(10,10),
                new[]{new Point(11,10),new Point(12,10),new Point(13,10)},new[]{new Point(13,10)},
                !hit?Array.Empty<SpellFxTargetResult>():duplicate?new[]{target,target}:new[]{target},cosmeticSeed:seed);
        }
        bool Play(SpellFxSequence sequence=null,bool native=true)=>audio.Play(sequence??Sequence(),.295f,new Point(13,10),native);
        [Test] public void PreparesNineApprovedClipsAndOnlyTwelveReusableSources()
        {
            Assert.IsTrue(audio.IsPrepared);Assert.AreEqual(12,audio.AllocatedSources);
            for(int v=1;v<=3;v++) foreach(var kind in new[]{"wind","fire","impact"})
            {
                var clip=Resources.Load<AudioClip>("Audio/EmberSpit/ember_spit_"+kind+"_"+v.ToString("D2"));
                Assert.NotNull(clip);Assert.AreEqual(48000,clip.frequency);Assert.AreEqual(1,clip.channels);
                Assert.AreEqual(kind=="impact"?33600:52800,clip.samples);
            }
            var first=audio.Root;audio.Prepare();Assert.AreSame(first,audio.Root);Assert.AreEqual(12,audio.AllocatedSources);
        }
        [TestCase(0,1)][TestCase(1,2)][TestCase(2,3)][TestCase(int.MinValue,3)]
        public void VariantsStayMatchedAcrossAllThreeLayers(int seed,int expected)
        {
            Assert.IsTrue(Play(Sequence(seed:seed)));Assert.AreEqual(expected,audio.LastVariant);
            var sources=audio.Root.GetComponentsInChildren<AudioSource>().Where(s=>s.clip!=null).ToArray();
            Assert.AreEqual(3,sources.Length);Assert.IsTrue(sources.All(s=>s.clip.name.EndsWith(expected.ToString("D2"))));
            Assert.IsTrue(sources.All(s=>s.spatialBlend==0&&!s.playOnAwake&&!s.loop&&s.dopplerLevel==0));
        }
        [Test] public void HitOccursOnceAtContactAndTailNeverReplaysIt()
        {
            Assert.IsTrue(Play());audio.Update(.294f,.294f,.294f);Assert.AreEqual(0,audio.ImpactCount);
            audio.Update(.002f,.002f,.002f);Assert.AreEqual(1,audio.ImpactCount);
            for(int i=0;i<100;i++)audio.Update(.02f,.02f,.02f);
            Assert.AreEqual(1,audio.ImpactCount);Assert.AreEqual(0,audio.ActiveVoices);
        }
        [Test] public void NativeRecoveryDelaysContactWhileFallbackUsesOrdinaryDelta()
        {
            Assert.IsTrue(Play(native:true));audio.Update(.1f,.1f,.1f);
            audio.Update(.5f,.5f,.15f);Assert.AreEqual(0,audio.ImpactCount);
            audio.Update(.05f,.05f,.05f);Assert.AreEqual(1,audio.ImpactCount);
            audio.ClearAll();Assert.IsTrue(Play(native:false));audio.Update(.5f,.5f,.15f);Assert.AreEqual(2,audio.ImpactCount);
        }
        [Test] public void DuplicateTargetResultsAndReplayedSameSequenceDoNotDoubleSound()
        {
            var sequence=Sequence(duplicate:true);Assert.IsTrue(Play(sequence));Assert.IsFalse(Play(sequence));
            audio.Update(.3f,.3f,.3f);Assert.AreEqual(1,audio.ImpactCount);Assert.AreEqual(1,audio.ActiveVoices);
        }
        [Test] public void MissAndRemoteAmbientResultHaveNoCrushingImpact()
        {
            Assert.IsTrue(Play(Sequence(hit:false)));audio.Update(.4f,.4f,.4f);Assert.AreEqual(0,audio.ImpactCount);
            audio.ClearAll();var distant=new SpellFxTargetResult("ambient",new Point(20,10),new Point(20,10),5);
            var sequence=new SpellFxSequence("Pyromancy_EmberSpit",zone,null,new Point(10,10),new[]{new Point(13,10)},targets:new[]{distant});
            Assert.IsTrue(Play(sequence));audio.Update(.4f,.4f,.4f);Assert.AreEqual(0,audio.ImpactCount);
        }
        [Test] public void ContactThatBecomesHiddenIsSkippedAndNotDelayedUntilRevealed()
        {
            Play();zone.GetCell(13,10).IsVisible=false;audio.Update(.3f,.3f,.3f);Assert.AreEqual(0,audio.ImpactCount);
            zone.GetCell(13,10).IsVisible=true;audio.Update(.1f,.1f,.1f);Assert.AreEqual(0,audio.ImpactCount);
        }
        [TestCase("unsupported")][TestCase("wrong-zone")][TestCase("hidden")][TestCase("off")][TestCase("muted")]
        public void InvalidOrSuppressedSequencesDoNotStartAudio(string reason)
        {
            var sequence=Sequence();
            if(reason=="unsupported")sequence=Sequence(spell:"Pyromancy_FlamingHands");
            if(reason=="wrong-zone")audio.SetZone(new Zone("other"));
            if(reason=="hidden")foreach(var c in zone.Cells)c.IsVisible=false;
            if(reason=="off")SpellFxSettings.Mode=SpellFxMode.Off;
            if(reason=="muted")SpellFxSettings.SoundVolume=0;
            Assert.IsFalse(Play(sequence));Assert.AreEqual(0,audio.ActiveVoices);
        }
        [Test] public void FourVoiceBoundReusesPoolAndMaintainsCommonLayerGain()
        {
            for(int i=0;i<4;i++)Assert.IsTrue(Play(Sequence(seed:i)));
            Assert.IsFalse(Play(Sequence(seed:99)));Assert.AreEqual(4,audio.ActiveVoices);Assert.AreEqual(12,audio.AllocatedSources);
            Assert.IsTrue(audio.Root.GetComponentsInChildren<AudioSource>().All(s=>Mathf.Approximately(s.volume,.25f)));
            audio.ClearAll();Assert.IsTrue(Play());Assert.AreEqual(12,audio.AllocatedSources);
        }
        [TestCase("clear")][TestCase("zone")][TestCase("off")][TestCase("mute")][TestCase("timeout")][TestCase("dispose")]
        public void InterruptionStopsEveryLayerAndPreventsLateContact(string reason)
        {
            Play();
            if(reason=="clear")audio.ClearAll();
            if(reason=="zone")audio.SetZone(new Zone("other"));
            if(reason=="off")SpellFxSettings.Mode=SpellFxMode.Off;
            if(reason=="mute")SpellFxSettings.SoundVolume=0;
            if(reason=="dispose")audio.Dispose();
            audio.Update(reason=="timeout"?5:.3f,.3f,.3f);
            Assert.AreEqual(0,audio.ActiveVoices);Assert.AreEqual(0,audio.ImpactCount);
        }
        [Test] public void NativeCancellationLeavesUnrelatedFallbackSoundAlone()
        {
            Play(Sequence(seed:0),true);Play(Sequence(seed:1),false);audio.CancelNative();
            Assert.AreEqual(1,audio.ActiveVoices);audio.Update(.3f,.3f,.3f);Assert.AreEqual(1,audio.ImpactCount);
        }
        [TestCase(.25f)][TestCase(1f)][TestCase(4f)]
        public void VisualSpeedControlsContactAndAudioPitchIsBounded(float speed)
        {
            SpellFxSettings.AnimationSpeed=speed;Play();audio.Update(.29f/speed,.29f,.29f);Assert.AreEqual(0,audio.ImpactCount);
            audio.Update(.01f/speed,.01f,.01f);Assert.AreEqual(1,audio.ImpactCount);
            Assert.IsTrue(audio.Root.GetComponentsInChildren<AudioSource>().Where(s=>s.clip!=null).All(s=>Mathf.Approximately(s.pitch,Mathf.Min(3,speed))));
        }
        [Test] public void VolumeChangesAllLayersTogetherAndInvalidValuesClampSafely()
        {
            Play();SpellFxSettings.SoundVolume=.4f;audio.Update(.01f,.01f,.01f);
            Assert.IsTrue(audio.Root.GetComponentsInChildren<AudioSource>().Where(s=>s.clip!=null).All(s=>Mathf.Approximately(s.volume,.4f)));
            SpellFxSettings.SoundVolume=float.NaN;Assert.AreEqual(1,SpellFxSettings.SoundVolume);
            SpellFxSettings.SoundVolume=-2;Assert.AreEqual(0,SpellFxSettings.SoundVolume);
        }
        [Test] public void CoordinatorRoutesOnlyEmberAndSoundTailDoesNotHoldTurn()
        {
            var grid=new GameObject("Audio coordinator",typeof(Grid));grid.transform.SetParent(root.transform);
            var map=new GameObject("FX",typeof(Tilemap),typeof(TilemapRenderer));map.transform.SetParent(grid.transform);
            using(var world=new WorldFxCoordinator(new AsciiFxRenderer(map.GetComponent<Tilemap>()),root.transform))
            {
                world.SetZone(zone);world.Update(0,false);var handle=world.Play(Sequence());
                Assert.AreEqual(1,world.EmberAudio.ActiveVoices);world.Update(.6f,false);
                Assert.IsTrue(handle.IsFinished);Assert.IsFalse(world.HasBlockingFx);Assert.AreEqual(1,world.EmberAudio.ActiveVoices);
                world.CancelAll();Assert.AreEqual(0,world.EmberAudio.ActiveVoices);
                world.Play(Sequence(spell:"Pyromancy_FlamingHands"));Assert.AreEqual(0,world.EmberAudio.ActiveVoices);
            }
        }
    }
}
