using System;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensitySmokeVisibilityTests
 {
  Zone z;Action<string> oldFull;Action<int,int,string> oldCell;int full,cells;
  [SetUp]public void Setup(){z=new Zone("SmokeVisibility");oldFull=ZoneRenderHooks.FullDirtyCallback;oldCell=ZoneRenderHooks.CellDirtyCallback;ZoneRenderHooks.FullDirtyCallback=_=>full++;ZoneRenderHooks.CellDirtyCallback=(x,y,r)=>cells++;full=cells=0;}
  [TearDown]public void Cleanup(){ZoneRenderHooks.FullDirtyCallback=oldFull;ZoneRenderHooks.CellDirtyCallback=oldCell;}
  [TestCase("smoke",true)][TestCase("steam",false)][TestCase("unknown",false)]
  public void ActualCloudObscuresVisionButIsNotAWallOrMovementBlock(string id,bool hidden){z.TileState.WriteCloud(5,5,id,3);FieldOfView.Compute(z,3,5,10);Assert.True(z.GetCell(5,5).IsVisible);Assert.AreEqual(!hidden,z.GetCell(7,5).IsVisible);Assert.AreEqual(!hidden,AIHelpers.HasLineOfSight(z,3,5,7,5));Assert.AreEqual(!hidden,AIHelpers.HasLineOfSight(z,7,5,3,5));Assert.False(z.GetCell(5,5).IsWall());Assert.False(z.GetCell(5,5).IsSolid());Assert.False(z.GetCell(5,5).BlocksMovement());}
  [Test]public void CloudsLeaveLegacyFurnitureVisibilityRulesIntact(){var prop=new Entity();prop.SetTag("Solid");z.AddEntity(prop,5,5);FieldOfView.Compute(z,3,5,10);Assert.True(z.GetCell(7,5).IsVisible);Assert.False(AIHelpers.HasLineOfSight(z,3,5,7,5));}
  [Test]public void FovAndAiRecoverWhenSmokeExpires(){z.TileState.WriteCloud(5,5,"smoke",1);FieldOfView.Compute(z,3,5,10);Assert.False(z.GetCell(7,5).IsVisible);z.TileState.Tick();FieldOfView.Compute(z,3,5,10);Assert.True(z.GetCell(7,5).IsVisible);Assert.True(AIHelpers.HasLineOfSight(z,3,5,7,5));}
  [Test]public void LightMapRebuildsWithoutEntityOrEquipmentMovement(){z.AmbientLevel=0;var lamp=new Entity();lamp.AddPart(new LightSourcePart{Radius=10,Intensity=1,LightColor="&W"});z.AddEntity(lamp,3,5);var light=new LightMap();light.Compute(z);float clear=light.GetBrightness(7,5);Assert.Greater(clear,0);z.TileState.WriteCloud(5,5,"smoke",2);light.Compute(z);Assert.Zero(light.GetBrightness(7,5));z.TileState.Clear(5,5);light.Compute(z);Assert.AreEqual(clear,light.GetBrightness(7,5));}
  [Test]public void CreatingAndRemovingSmokeInvalidatesWholeFov(){ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCloud(5,5,"smoke",3);Assert.AreEqual(1,full);z.TileState.Clear(5,5);Assert.AreEqual(2,full);}
  [Test]public void OrdinaryLayerWritesStayCellOnly(){ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCoating(5,5,"water",3);z.TileState.AddHeat(5,5,1);z.TileState.WriteCloud(5,5,"steam",3);Assert.Zero(full);Assert.Greater(cells,0);}
  [Test]public void ExtendingSmokeDoesNotRebuildAnUnchangedShadow(){ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCloud(5,5,"smoke",1);Assert.AreEqual(1,full);z.TileState.WriteCloud(5,5,"smoke",3);Assert.AreEqual(1,full);z.TileState.Tick();Assert.AreEqual(1,full);z.TileState.Tick();Assert.AreEqual(1,full);z.TileState.Tick();Assert.AreEqual(2,full);}
  [Test]public void ExpiringSmokeWithPermanentCoatingStillInvalidates(){ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCoating(5,5,"water",ZoneTileState.Permanent);z.TileState.WriteCloud(5,5,"smoke",1);full=0;z.TileState.Tick();Assert.AreEqual(1,full);Assert.True(z.TileState.HasCoating(5,5,"water"));}
  [Test]public void ReplacingSmokeWithSteamInvalidatesButRemainsWalkable(){ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCloud(5,5,"smoke",3);full=0;z.TileState.WriteCloud(5,5,"steam",2);Assert.AreEqual(1,full);Assert.True(AIHelpers.HasLineOfSight(z,3,5,7,5));Assert.False(z.GetCell(5,5).BlocksMovement());}
  [Test]public void RebindingRenderHookDetachesInactiveZone(){ZoneTileStateSystem.BindRenderHook(z);var active=new Zone("OtherSmoke");ZoneTileStateSystem.BindRenderHook(active);z.TileState.WriteCloud(5,5,"smoke",2);Assert.Zero(full);Assert.Zero(cells);active.TileState.WriteCloud(5,5,"smoke",2);Assert.AreEqual(1,full);}
  [TestCase(true)][TestCase(false)]
  public void ReplacingLoadedStateInvalidatesSmokeTopology(bool incomingSmoke){ZoneTileStateSystem.BindRenderHook(z);var other=new Zone("Other");if(incomingSmoke)other.TileState.WriteCloud(5,5,"smoke",3);else z.TileState.WriteCloud(5,5,"smoke",3);full=0;z.TileState.LoadFromString(other.TileState.ToSaveString());Assert.AreEqual(1,full);Assert.AreEqual(!incomingSmoke,AIHelpers.HasLineOfSight(z,3,5,7,5));}
  [Test]public void InvalidLoadThatClearsSmokeInvalidatesOldShadow(){ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCloud(5,5,"smoke",2);full=0;z.TileState.LoadFromString(null);Assert.AreEqual(1,full);Assert.True(AIHelpers.HasLineOfSight(z,3,5,7,5));}
  [Test]public void SaveRoundTripRetainsOpaqueLifetime(){z.TileState.WriteCloud(5,5,"smoke",2);var loaded=new Zone("LoadedSmoke");loaded.TileState.LoadFromString(z.TileState.ToSaveString());Assert.False(AIHelpers.HasLineOfSight(loaded,3,5,7,5));loaded.TileState.Tick();Assert.False(AIHelpers.HasLineOfSight(loaded,3,5,7,5));loaded.TileState.Tick();Assert.True(AIHelpers.HasLineOfSight(loaded,3,5,7,5));}
  [Test]public void SmokeAtRayEndpointRemainsVisibleLikeAnOpaqueSurface(){z.TileState.WriteCloud(5,5,"smoke",3);Assert.True(AIHelpers.HasLineOfSight(z,3,5,5,5));FieldOfView.Compute(z,3,5,10);Assert.True(z.GetCell(5,5).IsVisible);}
  [Test]public void ReactionProducedSmokeUsesTheSameVisibilitySeam(){TileReactionSystem.Initialize(UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Data/TileReactions/Reactions").text);try{z.TileState.WriteCoating(5,5,"oil",5);z.TileState.AddHeat(5,5,1);TileReactionSystem.ResolveZone(z);Assert.AreEqual("smoke",z.TileState.Cloud(5,5));Assert.False(AIHelpers.HasLineOfSight(z,3,5,7,5));}finally{TileReactionSystem.ResetForTests();}}
 }
}
