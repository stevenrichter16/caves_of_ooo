using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioFootingTests
 {
  GameObject root; WorldAudioPlayer audio; WorldAudioEventRouter router; Zone zone; Entity actor,pool;
  readonly List<AudioClip> clips=new List<AudioClip>(); float oldSfx; bool oldPause;
  [SetUp] public void Setup()
  {
   oldSfx=WorldAudioSettings.SfxVolume;oldPause=AudioListener.pause;WorldAudioSettings.SfxVolume=1;AudioListener.pause=false;
   root=new GameObject("actual liquid footing");var library=new WorldAudioLibrary();
   foreach(var cue in new[]{WorldAudioCue.StepSoft,WorldAudioCue.StepStone,WorldAudioCue.StepWater})
   {var clip=AudioClip.Create(cue.ToString(),48000,1,48000,false);clips.Add(clip);library.AddSound(cue,clip,.4f);}
   audio=new WorldAudioPlayer(root.transform,library);router=new WorldAudioEventRouter(audio);
   zone=new Zone("Overworld.10.10.0");foreach(var c in zone.Cells){c.Explored=true;c.IsVisible=true;}
   actor=new Entity{ID="player"};actor.AddPart(new PhysicsPart());actor.AddPart(new RenderPart());zone.AddEntity(actor,10,10);router.SetWorld(zone,actor,true);
   pool=new Entity{ID="pool"};pool.AddPart(new PhysicsPart());pool.AddPart(new LiquidPoolPart{LiquidId="water",Volume=2});zone.AddEntity(pool,10,11);
  }
  [TearDown] public void Cleanup(){router.Dispose();audio.Dispose();UnityEngine.Object.DestroyImmediate(root);foreach(var c in clips)UnityEngine.Object.DestroyImmediate(c);clips.Clear();WorldAudioSettings.SfxVolume=oldSfx;AudioListener.pause=oldPause;}
  string Step(){Assert.True(MovementSystem.TryMove(actor,zone,0,1));return audio.Root.GetComponentsInChildren<AudioSource>().Single(s=>s.clip!=null).clip.name;}
  [TestCase(0)][TestCase(-1)] public void DepletedPoolUsesGround(int volume){pool.GetPart<LiquidPoolPart>().Volume=volume;Assert.AreEqual("StepSoft",Step());}
  [TestCase("water")][TestCase("brine")][TestCase("bog-mire")] public void PositiveLiquidStillSplashes(string liquid){pool.GetPart<LiquidPoolPart>().LiquidId=liquid;Assert.AreEqual("StepWater",Step());}
  [Test] public void EmptyPoolDoesNotHideStoneFloor(){pool.GetPart<LiquidPoolPart>().Volume=0;var floor=new Entity{ID="floor",BlueprintName="StoneFloor"};floor.SetTag("Terrain");zone.AddEntity(floor,10,11);Assert.AreEqual("StepStone",Step());}
  [Test] public void AdjacentPoolDoesNotWetFootsteps(){zone.MoveEntity(pool,11,11);Assert.AreEqual("StepSoft",Step());}
  [Test] public void RefillingActuallyRestoresSplash(){pool.GetPart<LiquidPoolPart>().Volume=0;Assert.AreEqual("StepSoft",Step());audio.ClearSounds();Assert.True(MovementSystem.TryMove(actor,zone,0,-1));audio.ClearSounds();pool.GetPart<LiquidPoolPart>().Volume=1;Assert.AreEqual("StepWater",Step());}
  [Test] public void HiddenWorldStillRefusesMovementAudio(){router.SetWorld(zone,actor,false);Assert.True(MovementSystem.TryMove(actor,zone,0,1));Assert.AreEqual(0,audio.AcceptedSoundCount);}
 }
}
