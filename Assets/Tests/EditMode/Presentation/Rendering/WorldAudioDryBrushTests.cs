using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public abstract class WorldAudioDryBrushFixture : FiftyWorldFixture
    {
        protected WorldAudioPlayer Audio;protected WorldAudioSoundscape Scene;
        GameObject root;AudioClip clip;float volume;bool paused;
        [SetUp] public void AudioSetup()
        {
            volume=WorldAudioSettings.AmbienceVolume;paused=AudioListener.pause;WorldAudioSettings.AmbienceVolume=1;AudioListener.pause=false;
            root=new GameObject("actual dry brush");clip=AudioClip.Create("leaf texture",48000,1,48000,false);var library=new WorldAudioLibrary();library.AddDetail("leaf-rustle",clip,.16f);
            Audio=new WorldAudioPlayer(root.transform,library);Scene=new WorldAudioSoundscape(Audio);foreach(var cell in Zone.Cells)cell.Explored=cell.IsVisible=true;
        }
        [TearDown] public void AudioCleanup(){Audio.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);WorldAudioSettings.AmbienceVolume=volume;AudioListener.pause=paused;}
        protected void Observe(BiomeType biome=BiomeType.Beating,int depth=0)=>Scene.Observe(Zone,Actor,biome,depth,DayBand.Dawn,true);
        protected void Start(){Audio.Update(2);Observe();Scene.Tick(10,true,false);Assert.AreEqual("leaf-rustle",Audio.LastDetailId);Assert.AreEqual(1,Audio.ActiveDetailSources);}
    }
    public sealed class WorldAudioDryBrushTests : WorldAudioDryBrushFixture
    {
        [TestCase(BiomeType.Beating)][TestCase(BiomeType.Desert)][TestCase(BiomeType.Overwrit)][TestCase(BiomeType.Ruins)]
        public void RealAuthoredDryBrushSuppliesLocalTextureInDryRegion(BiomeType biome)
        {
            var owner=Place("DryBrush");Assert.AreEqual("DryPlant",owner.GetPart<MaterialPart>().MaterialID);Assert.False(owner.GetPart<PhysicsPart>().Takeable);
            Observe(biome);Assert.AreEqual(1,Scene.SourceCount);Assert.AreEqual("dry-wind",Scene.AmbienceId);Scene.Tick(10,true,false);
            Assert.AreEqual("leaf-rustle",Audio.LastDetailId);Assert.AreEqual(1,Scene.MaterialPhrases);Assert.AreEqual(0,Scene.WildlifePhrases);
        }
        [Test] public void LiteralBrushRatherThanBiomeGuessAlsoWorksAtAnActualGreenRegionMargin()
        {Place("DryBrush");Observe(BiomeType.Sodden);Scene.Tick(10,true,false);Assert.AreEqual("leaf-rustle",Audio.LastDetailId);}
        [Test] public void NearestActualBrushOwnsThePan()
        {Place("DryBrush",8,10);Place("DryBrush",15,10);Start();var voice=Audio.Root.GetComponentInChildren<AudioSource>();foreach(var s in Audio.Root.GetComponentsInChildren<AudioSource>())if(s.priority==200&&s.clip!=null)voice=s;Assert.Less(voice.panStereo,0);}
        [Test] public void ExistingSparseMaterialCadenceIsPreserved()
        {
            Place("DryBrush");Observe();int scans=Scene.ScanCount;for(int i=0;i<120;i++){Scene.Tick(1,true,false);Audio.Update(1);}
            Assert.GreaterOrEqual(Scene.MaterialPhrases,4);Assert.LessOrEqual(Scene.MaterialPhrases,11);Assert.AreEqual(scans,Scene.ScanCount);Assert.LessOrEqual(Scene.LastVisitedOwners,512);Assert.AreEqual(16,Audio.AllocatedSources);
        }
        [Test] public void LeavingActualSourceRangeRetiresKnownPhraseWithoutDiscovery()
        {Place("DryBrush");Start();int scans=Scene.ScanCount;Assert.True(Zone.MoveEntity(Actor,20,10));Scene.Tick(.01f,true,false);Assert.AreEqual(0,Audio.ActiveDetailSources);Assert.AreEqual(0,Scene.SourceCount);Assert.AreEqual(scans,Scene.ScanCount);}
    }
}
