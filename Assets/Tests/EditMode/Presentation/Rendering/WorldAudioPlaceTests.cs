using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioPlaceTests
 {
  GameObject root;AudioClip clip;WorldAudioPlayer player;WorldAudioLibrary library;WorldAudioSoundscape scene;
  Zone zone;Entity actor;float oldMusic,oldAmbience,oldSfx;bool oldPause;
  [SetUp] public void Setup()
  {
   oldMusic=WorldAudioSettings.MusicVolume;oldAmbience=WorldAudioSettings.AmbienceVolume;oldSfx=WorldAudioSettings.SfxVolume;oldPause=AudioListener.pause;
   WorldAudioSettings.MusicVolume=WorldAudioSettings.AmbienceVolume=WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
   root=new GameObject("place audio");clip=AudioClip.Create("detail",48000,1,48000,false);library=new WorldAudioLibrary();
   foreach(var id in new[]{"bird","insect","frog","dog","water-lap","fire","wood-creak","stone-tick","kitchen"})library.AddDetail(id,clip,.3f);
   foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))library.AddSound(cue,clip,.4f);
   player=new WorldAudioPlayer(root.transform,library);scene=new WorldAudioSoundscape(player);
   zone=new Zone("Overworld.10.10.0");actor=WorldAudioAdversarialTests.Actor("listener");Assert.True(zone.AddEntity(actor,10,10));
   foreach(var cell in zone.Cells){cell.IsVisible=true;cell.Explored=true;}
  }
  [TearDown] public void Cleanup()
  {player.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(clip);WorldAudioSettings.MusicVolume=oldMusic;WorldAudioSettings.AmbienceVolume=oldAmbience;WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;}
  Entity Add(string name,int x=11,int y=10)
  {var e=new Entity{ID=name,BlueprintName=name};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart());Assert.True(zone.AddEntity(e,x,y));return e;}
  void Observe(BiomeType biome=BiomeType.Spread,int depth=0,DayBand band=DayBand.Dawn,bool visible=true)=>scene.Observe(zone,actor,biome,depth,band,visible);
  void Advance(float seconds=60)
  {for(int i=0;i<(int)seconds;i++){scene.Tick(1,true,false);player.Update(1);}}
  [TestCase(BiomeType.Spread,"field","night-field")][TestCase(BiomeType.Grovelands,"field","night-field")]
  [TestCase(BiomeType.Jungle,"field","night-field")][TestCase(BiomeType.Sodden,"marsh","night-field")]
  [TestCase(BiomeType.Beating,"dry-wind","dry-wind")][TestCase(BiomeType.Overwrit,"dry-wind","dry-wind")]
  [TestCase(BiomeType.Stump,"dry-wind","dry-wind")][TestCase(BiomeType.Ruins,"dry-wind","dry-wind")]
  public void BedsFollowActualBiomeAndDayWithoutInventingWater(BiomeType biome,string day,string night)
  {Observe(biome);Assert.AreEqual(day,scene.AmbienceId);Observe(biome,0,DayBand.Dark);Assert.AreEqual(night,scene.AmbienceId);}
  [Test] public void UndergroundAndInteriorSuppressSurfaceAnimals()
  {
   Observe(BiomeType.Spread,1);Assert.AreEqual("cave",scene.AmbienceId);Assert.AreEqual(0,scene.HabitatDetailCount);
   zone.GetCell(10,10).IsInterior=true;Observe();Assert.Null(scene.AmbienceId);Assert.AreEqual(0,scene.HabitatDetailCount);
   zone.GetCell(10,10).IsInterior=false;Observe();Assert.AreEqual("field",scene.AmbienceId);Assert.Greater(scene.HabitatDetailCount,0);
  }
  [TestCase("water",20,true)][TestCase("brackish_water",20,false)][TestCase("water",0,false)]
  [TestCase("oil",20,false)][TestCase("acid",20,false)][TestCase("honey",20,false)]
  public void WaterNeedsActualPositiveWaterLikeVolume(string id,int volume,bool expected)
  {var e=Add("pool");e.SetTag("FlowsEast");e.AddPart(new LiquidPoolPart{LiquidId=id,Volume=volume});Observe();Assert.AreEqual(expected?"water":"field",scene.AmbienceId);Assert.AreEqual(expected?1:0,scene.SourceCount);}
  [TestCase("hidden")][TestCase("removed")][TestCase("empty")][TestCase("render")][TestCase("inventory")]
  public void KnownWaterLosesEligibilityWithoutAnotherFullScan(string change)
  {
   var pool=Add("pool");pool.SetTag("FlowsEast");pool.AddPart(new LiquidPoolPart{LiquidId="water",Volume=20});Observe();Assert.AreEqual("water",scene.AmbienceId);int scans=scene.ScanCount;
   if(change=="hidden")zone.GetCell(11,10).IsVisible=false;
   if(change=="removed")zone.RemoveEntity(pool);
   if(change=="empty")pool.GetPart<LiquidPoolPart>().Volume=0;
   if(change=="render")pool.GetPart<RenderPart>().Visible=false;
   if(change=="inventory")pool.GetPart<PhysicsPart>().InInventory=actor;
   scene.Tick(1,true,false);Assert.AreEqual("field",scene.AmbienceId);Assert.AreEqual(0,scene.SourceCount);Assert.AreEqual(scans,scene.ScanCount);
  }
  [TestCase("Bandfrog",true)][TestCase("Reedfrog",true)][TestCase("CascadeFather",false)]
  public void ActualLivingAnimalsOnlyAndLoreSilentSpeciesStaysSilent(string name,bool expected)
  {
   var e=Add(name);e.SetTag("Creature");e.AddPart(new BrainPart());e.Statistics["Hitpoints"]=new Stat{Owner=e,Name="Hitpoints",BaseValue=10,Max=10};Observe(BiomeType.Sodden);
   Assert.AreEqual(expected?1:0,scene.SourceCount);e.Statistics["Hitpoints"].BaseValue=0;scene.Tick(1,true,false);Assert.AreEqual(0,scene.SourceCount);
  }
  [Test] public void FiniteStoredHeatDoesNotPretendToBeBurning()
  {
   var e=Add("hearth");var fire=new CampfirePart{FiniteCooking=true};e.AddPart(fire);e.AddPart(new FuelPart{FuelMass=20});e.AddPart(new ThermalPart{Temperature=600,FlameTemperature=500});Observe();Assert.AreEqual(0,scene.SourceCount);
   fire.FiniteCooking=false;Observe();Assert.AreEqual(1,scene.SourceCount);
   e.GetPart<FuelPart>().FuelMass=0;scene.Tick(1,true,false);Assert.AreEqual(0,scene.SourceCount);
  }
  [Test] public void DetailsUseAmbienceAndKeepSameSixteenSources()
  {
   WorldAudioSettings.SfxVolume=0;Assert.True(player.TryPlayDetail("bird",.2f,.5f));Assert.AreEqual(1,player.ActiveDetailSources);Assert.AreEqual(16,player.AllocatedSources);
   WorldAudioSettings.AmbienceVolume=0;player.Update(.01f);Assert.AreEqual(0,player.ActiveDetailSources);Assert.False(player.TryPlayDetail("bird"));
   WorldAudioSettings.SfxVolume=1;Assert.True(player.TryPlay(WorldAudioCue.Swing));WorldAudioSettings.AmbienceVolume=1;player.Update(2);Assert.AreEqual(1,player.AcceptedDetailCount);
  }
  [Test] public void AtMostTwoDetailsAndActionsMayPreemptThem()
  {
   Assert.True(player.TryPlayDetail("bird"));Assert.True(player.TryPlayDetail("frog"));Assert.False(player.TryPlayDetail("fire"));
   int accepted=0;foreach(WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))if(player.TryPlay(cue))accepted++;
   Assert.AreEqual(12,accepted);Assert.AreEqual(0,player.ActiveDetailSources);Assert.AreEqual(12,player.ActiveSoundSources);Assert.AreEqual(16,player.AllocatedSources);
  }
  [TestCase(float.NaN)][TestCase(-1f)][TestCase(2f)] public void InvalidDetailGainCannotConsumeVoice(float gain)
  {Assert.False(player.TryPlayDetail("bird",0,gain));Assert.AreEqual(0,player.ActiveDetailSources);Assert.True(player.TryPlayDetail("bird"));}
  [Test] public void MissingDetailsAndPausedListenerNeverAllocateOrQueue()
  {Assert.False(player.TryPlayDetail("absent"));AudioListener.pause=true;Assert.False(player.TryPlayDetail("bird"));AudioListener.pause=false;player.Update(1);Assert.AreEqual(0,player.AcceptedDetailCount);}
  [Test] public void IdleSchedulingIsSparseAndNeverRescans()
  {Observe();int scans=scene.ScanCount;Advance(120);Assert.AreEqual(scans,scene.ScanCount);Assert.Greater(player.AcceptedDetailCount,0);Assert.LessOrEqual(player.AcceptedDetailCount,20);}
  [Test] public void HiddenWorldAndGraphResetDoNotReplayOldDetails()
  {
   Add("pool").AddPart(new LiquidPoolPart{LiquidId="water",Volume=20});Observe();Assert.True(player.TryPlayDetail("water-lap"));
   scene.Tick(1,false,false);Assert.AreEqual(0,player.ActiveDetailSources);Assert.AreEqual(0,scene.SourceCount);int count=player.AcceptedDetailCount;
   for(int i=0;i<60;i++)scene.Tick(1,false,false);Assert.AreEqual(count,player.AcceptedDetailCount);
   scene.Observe(null,null,BiomeType.Spread,0,DayBand.Dawn,true);Assert.Null(scene.AmbienceId);Assert.AreEqual(0,scene.SourceCount);
  }
  [Test] public void FarOrUnexploredSourcesCannotEnterSnapshot()
  {Add("pool",17,10).AddPart(new LiquidPoolPart{LiquidId="water",Volume=20});Observe();Assert.AreEqual(0,scene.SourceCount);Assert.AreEqual("field",scene.AmbienceId);}
  [Test] public void RuntimeUsesRealClockAndShelterAndKeepsItsBedWhileMenuIsHidden()
  {
   var old=TurnManager.Active;
   try
   {
    var input=root.AddComponent<InputHandler>();input.CurrentZone=zone;input.PlayerEntity=actor;input.WorldMap=new WorldMap(12);input.WorldMap.Tiles[10,10]=BiomeType.Spread;
    input.TurnManager=new TurnManager();var runtime=root.AddComponent<WorldAudioRuntime>();runtime.Initialize(input);runtime.Tick(0);Assert.AreEqual("field",runtime.Context.AmbienceId);
    input.TurnManager.AdvanceClock(900);runtime.Tick(0);Assert.AreEqual("night-field",runtime.Context.AmbienceId);
    input.enabled=false;runtime.Tick(0);Assert.AreEqual("night-field",runtime.Context.AmbienceId,"menu must not replace nocturnal insects with daytime birds");
    input.enabled=true;zone.GetCell(10,10).IsInterior=true;runtime.Tick(0);Assert.Null(runtime.Context.AmbienceId);
    int scans=runtime.Soundscape.ScanCount;for(int i=0;i<100;i++)runtime.Tick(.01f);Assert.AreEqual(scans,runtime.Soundscape.ScanCount);
   }
   finally{typeof(TurnManager).GetProperty("Active").SetValue(null,old);}
  }
  [Test] public void RuntimeMenuRetiresSourceDependentWaterButRetainsMusic()
  {
   var pool=Add("pool");pool.SetTag("FlowsEast");pool.AddPart(new LiquidPoolPart{LiquidId="water",Volume=20});
   var input=root.AddComponent<InputHandler>();input.CurrentZone=zone;input.PlayerEntity=actor;
   var runtime=root.AddComponent<WorldAudioRuntime>();runtime.Initialize(input);runtime.Tick(3);Assert.AreEqual("water",runtime.Context.AmbienceId);
   string music=runtime.Context.MusicId;input.enabled=false;runtime.Tick(.01f);
   Assert.Null(runtime.Context.AmbienceId);Assert.AreEqual(0,runtime.Player.ActiveAmbienceSources);Assert.AreEqual(music,runtime.Context.MusicId);
   input.enabled=true;runtime.Tick(0);Assert.AreEqual("water",runtime.Context.AmbienceId);
  }
  [Test] public void CreditsReaderContainsBothOriginalAndExpansionAttribution()
  {
   using(var isolation=new HotbarSaveFixture(false,false))
   {
    MessageLog.Clear();var menu=new PauseMenuController();menu.Open();menu.ClickSelect(PauseMenuController.AudioIndex,null,null);menu.ClickSelect(PauseMenuController.AudioCreditsIndex,null,null);
    var credits=MessageLog.ConsumeAnnouncement();StringAssert.Contains("Scott Buckley",credits);StringAssert.Contains("EZduzziteh",credits);StringAssert.Contains("Brandon",credits);
   }
  }
 }
}
