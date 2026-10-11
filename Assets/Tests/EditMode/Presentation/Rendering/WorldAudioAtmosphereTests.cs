using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioAtmosphereTests
 {
  GameObject root;AudioClip clip;WorldAudioLibrary library;WorldAudioPlayer player;WorldAudioSoundscape scene;Zone zone;Entity actor;
  float oldAmbience,oldMusic,oldSfx;bool oldPause;int id;
  [SetUp] public void Setup()
  {
   oldAmbience=WorldAudioSettings.AmbienceVolume;oldMusic=WorldAudioSettings.MusicVolume;oldSfx=WorldAudioSettings.SfxVolume;oldPause=AudioListener.pause;
   WorldAudioSettings.AmbienceVolume=WorldAudioSettings.MusicVolume=WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
   root=new GameObject("atmosphere test");clip=AudioClip.Create("phrase",24000,1,48000,false);library=new WorldAudioLibrary();
   foreach(var detail in new[]{"frog","dog","bird","insect","leaf-rustle","reed-rustle","wood-settle","stone-shift","water-drop","wood-creak","stone-tick","water-lap","fire"})library.AddDetail(detail,clip,.3f);
   foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))library.AddSound(cue,clip,.4f);
   player=new WorldAudioPlayer(root.transform,library);scene=new WorldAudioSoundscape(player);zone=new Zone("Overworld.10.10.0");
   actor=WorldAudioAdversarialTests.Actor("listener");Assert.True(zone.AddEntity(actor,10,10));foreach(var c in zone.Cells){c.Explored=c.IsVisible=true;}
  }
  [TearDown] public void Cleanup(){player.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;}
  Entity Add(string bp,int x=11,int y=10,bool animal=false)
  {
   var e=new Entity{ID="atmosphere-"+id++,BlueprintName=bp};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());
   if(animal){e.SetTag("Creature");e.AddPart(new BrainPart());e.Statistics["Hitpoints"]=new Stat{Owner=e,Name="Hitpoints",BaseValue=10,Min=0,Max=10};}
   Assert.True(zone.AddEntity(e,x,y));return e;
  }
  void Observe()=>scene.Observe(zone,actor,BiomeType.Spread,0,DayBand.Dawn,true);
  void Seconds(int seconds,bool refresh=false,bool danger=false){for(int i=0;i<seconds;i++){if(refresh)Observe();scene.Tick(1,true,danger);player.Update(1);}}
  [Test] public void NearbyWildlifeGetsAnEarlyVoiceDespiteScenery()
  {
   Add("Reedfrog",animal:true);Add("Tree");Add("pool").AddPart(new LiquidPoolPart{LiquidId="water",Volume=20});Observe();Seconds(9);
   Assert.GreaterOrEqual(scene.WildlifePhrases,1);Assert.LessOrEqual(scene.WildlifePhrases,2);Assert.AreEqual(16,player.AllocatedSources);
  }
  [Test] public void AllThreeLanesRemainAudibleWithoutContinuousNoise()
  {
   Add("Reedfrog",animal:true);Add("Tree");Observe();Seconds(80);
   Assert.GreaterOrEqual(scene.WildlifePhrases,3);Assert.GreaterOrEqual(scene.MaterialPhrases,2);Assert.GreaterOrEqual(scene.HabitatPhrases,2);
   Assert.LessOrEqual(player.AcceptedDetailCount,22);Assert.AreEqual(16,player.AllocatedSources);
  }
  [Test] public void OrdinaryContextRefreshDoesNotRestartWildlifeTimers()
  {Add("PetDog",animal:true);Observe();Seconds(15,true);Assert.Greater(scene.WildlifePhrases,0);}
  [Test] public void TwoAnimalKindsAlternateInsteadOfLosingRandomRolls()
  {
   Add("PetDog",11,10,true);Add("Reedfrog",10,11,true);scene.Observe(zone,actor,BiomeType.Sodden,0,DayBand.Dawn,true);
   var ids=new List<string>();int previous=0;
   for(int i=0;i<70;i++){scene.Tick(1,true,false);if(scene.WildlifePhrases>previous){ids.Add(player.LastDetailId);previous=scene.WildlifePhrases;}player.Update(1);}
   Assert.GreaterOrEqual(ids.Count,4);for(int i=1;i<ids.Count;i++)Assert.AreNotEqual(ids[i-1],ids[i]);
  }
  [TestCase("hidden")][TestCase("dead")][TestCase("absent")]
  public void WildlifeLaneDoesNotInventAnimals(string refused)
  {
   if(refused!="absent"){var e=Add("Reedfrog",animal:true);if(refused=="hidden")zone.GetEntityCell(e).IsVisible=false;else e.GetStat("Hitpoints").BaseValue=0;}
   Add("Tree",10,11);Observe();Seconds(60);Assert.AreEqual(0,scene.WildlifePhrases);Assert.Greater(scene.MaterialPhrases,0);Assert.Greater(scene.HabitatPhrases,0);
  }
  [Test] public void MutedTimeDoesNotQueueImmediatePhrasesOnResume()
  {Add("PetDog",animal:true);Observe();WorldAudioSettings.AmbienceVolume=0;Seconds(40);Assert.AreEqual(0,player.AcceptedDetailCount);WorldAudioSettings.AmbienceVolume=1;Seconds(2);Assert.AreEqual(0,player.AcceptedDetailCount);Seconds(20);Assert.Greater(scene.WildlifePhrases,0);}
  [Test] public void LaneWorkDoesNotRescanIdleWorld()
  {Add("Tree");Observe();int scans=scene.ScanCount;Seconds(120);Assert.AreEqual(scans,scene.ScanCount);Assert.LessOrEqual(scene.LastVisitedOwners,512);}
  [Test] public void SharedGapPreventsSameFrameLaneBursts()
  {
   Add("PetDog",animal:true);Add("Tree");Observe();int last=-100,previous=0;
   for(int i=0;i<80;i++){scene.Tick(1,true,false);if(player.AcceptedDetailCount>previous){Assert.GreaterOrEqual(i-last,3);last=i;previous=player.AcceptedDetailCount;}player.Update(1);}
   Assert.Greater(previous,5);
  }
  [TestCase("Tree",0,false,"leaf-rustle")][TestCase("Bush",0,false,"leaf-rustle")]
  [TestCase("Reeds",0,false,"reed-rustle")][TestCase("timber",0,true,"wood-settle")]
  [TestCase("stone",1,false,"stone-shift")][TestCase("stone",0,true,"stone-shift")]
  [TestCase("pool",1,false,"water-drop")][TestCase("pool",0,true,"water-drop")]
  public void ActualMaterialsChooseDistinctTextures(string blueprint,int level,bool inside,string expected)
  {
   foreach(var name in new[]{"leaf-rustle","reed-rustle","wood-settle","stone-shift","water-drop"})library.AddDetail(name,clip,.3f);
   var e=Add(blueprint);zone.GetCell(10,10).IsInterior=zone.GetCell(11,10).IsInterior=inside;
   if(blueprint=="timber"){e.SetTag("Wall");e.AddPart(new MaterialPart{MaterialID="Wood"});}
   if(blueprint=="stone"){e.SetTag("Wall");e.AddPart(new MaterialPart{MaterialID="Stone"});}
   if(blueprint=="pool")e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=3});
   scene.Observe(zone,actor,BiomeType.Sodden,level,DayBand.Dawn,true);scene.Tick(10,true,false);
   Assert.AreEqual(expected,player.LastDetailId);Assert.AreEqual(1,scene.MaterialPhrases);
  }
  [TestCase("Tree",true,0)][TestCase("Reeds",false,1)][TestCase("Tree",false,1)]
  public void NoWindblownVegetationInShelterOrUnderground(string blueprint,bool inside,int level)
  {Add(blueprint);zone.GetCell(10,10).IsInterior=zone.GetCell(11,10).IsInterior=inside;scene.Observe(zone,actor,BiomeType.Sodden,level,DayBand.Dawn,true);Seconds(30);Assert.AreEqual(0,scene.MaterialPhrases);}
  [Test] public void PortableWoodAndDryStoneCannotInventStructuralOrWaterSounds()
  {
   var e=Add("timber");e.SetTag("Wall");e.AddPart(new MaterialPart{MaterialID="Wood"});e.GetPart<PhysicsPart>().Takeable=true;
   zone.GetCell(10,10).IsInterior=zone.GetCell(11,10).IsInterior=true;Observe();Seconds(30);Assert.AreEqual(0,scene.MaterialPhrases);
  }
  [Test] public void ThirdCatalogActuallyLoadsDistinctShortTextureRecordings()
  {
   var real=WorldAudioLibrary.LoadResources();
   foreach(var id in new[]{"leaf-rustle","reed-rustle","wood-settle","stone-shift","water-drop"})
   {var a=real.Detail(id,0);var b=real.Detail(id,1);Assert.NotNull(a,id);Assert.NotNull(b,id);Assert.AreNotSame(a.Clip,b.Clip,id);Assert.Less(a.Clip.length,3.5f);Assert.Less(b.Clip.length,3.5f);}
  }
 }
}
