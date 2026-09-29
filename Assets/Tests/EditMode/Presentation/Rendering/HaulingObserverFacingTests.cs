using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 // Calls the actual runtime scenario method; no copied signature implementation.
 [TestFixture] public sealed class HaulingObserverFacingTests
 {
  EntityFactory factory;Zone zone;Entity player,load;
  HaulingContentScope scope;
  [SetUp] public void Setup(){scope=new HaulingContentScope();scope.Seed(64);factory=scope.Factory;}
  [TearDown] public void Cleanup()=>scope?.Dispose();
  void SetupLoad(string bp,int dx=-1,int dy=0){zone=new Zone("Overworld.10.10.0");player=factory.CreateEntity("Player");load=factory.CreateEntity(bp);Assert.True(zone.AddEntity(player,10,10));Assert.True(zone.AddEntity(load,10-dx,10-dy));}
  static string Facts(Entity e)=>(string)typeof(QuestFreeSpreadStateNativePlayer).GetMethod("HaulFacts",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{e});
  [TestCase("FallenBeam",-1,0,EntityVisualFacing.West)][TestCase("HaulBarrel",-1,0,EntityVisualFacing.West)]
  [TestCase("FallenBeam",1,0,EntityVisualFacing.East)][TestCase("HaulBarrel",1,0,EntityVisualFacing.East)]
  [TestCase("FallenBeam",0,-1,EntityVisualFacing.North)][TestCase("HaulBarrel",0,-1,EntityVisualFacing.North)]
  [TestCase("FallenBeam",0,1,EntityVisualFacing.South)][TestCase("HaulBarrel",0,1,EntityVisualFacing.South)]
  public void RealPullPreservesAuthoredFactsWhileFacingTracksMotion(string bp,int dx,int dy,EntityVisualFacing expected)
  {
   SetupLoad(bp,dx,dy);string before=Facts(load);Assert.AreEqual(EntityVisualFacing.South,load.GetPart<RenderPart>().VisualFacing);
   Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,load,zone));Assert.True(MovementSystem.TryMove(player,zone,dx,dy));
   Assert.AreEqual((10,10),zone.GetEntityPosition(load));Assert.AreEqual(expected,load.GetPart<RenderPart>().VisualFacing);
   string after=Facts(load);
   Assert.AreEqual(before,after,"Only actual movement facing may differ; immutable authored signature must remain stable.");
  }
  [TestCase("FallenBeam","id")][TestCase("HaulBarrel","id")][TestCase("FallenBeam","weight")][TestCase("HaulBarrel","weight")][TestCase("FallenBeam","glyph")][TestCase("HaulBarrel","glyph")]
  public void AuthoredIdentityAndVisualChangesStillInvalidateFacts(string bp,string changed)
  {SetupLoad(bp);string before=Facts(load);if(changed=="id")load.ID+="-changed";if(changed=="weight")load.GetPart<PhysicsPart>().Weight++;if(changed=="glyph")load.GetPart<RenderPart>().RenderString="X";Assert.AreNotEqual(before,Facts(load));}
  [TestCase("FallenBeam")][TestCase("HaulBarrel")] public void ActualPostPullFacingSurvivesSaveReconstruction(string bp)
  {
   SetupLoad(bp);Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(player,load,zone));Assert.True(MovementSystem.TryMove(player,zone,-1,0));Assert.True(DragSystem.Release(player));var facing=load.GetPart<RenderPart>().VisualFacing;string facts=Facts(load);
   var manager=OverworldZoneManager.CreateDetached(factory,64);manager.SetActiveZone(zone);var turns=new TurnManager();turns.AddEntity(player);var s=GameSessionState.Capture("facing-probe","native-observer-facts",manager,turns,player);GameSessionState restored;
   using(var bytes=new MemoryStream()){s.Save(new SaveWriter(bytes));bytes.Position=0;restored=GameSessionState.Load(new SaveReader(bytes,factory));}
   var other=restored.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e=>e.ID==load.ID);Assert.AreNotSame(load,other);Assert.AreEqual(facing,other.GetPart<RenderPart>().VisualFacing);Assert.AreEqual(facts,Facts(other));
  }
 }
}
