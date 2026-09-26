using System;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensitySmokeVisibilityAdversarialTests
 {
  Zone z;
  [SetUp]public void Setup()=>z=new Zone("AdversarialSmoke");
  [TestCase(-1,0)][TestCase(0,-1)][TestCase(80,0)][TestCase(0,25)]
  public void OutOfBoundsWriteCannotCreateSmokeOrInvalidate(int x,int y){int before=z.TileState.SightVersion;z.TileState.WriteCloud(x,y,"smoke",2);Assert.AreEqual(before,z.TileState.SightVersion);Assert.Zero(z.TileState.WrittenCount);}
  [TestCase(0)][TestCase(-1)][TestCase(int.MinValue)]
  public void NonpositiveWriteCannotReplaceExistingLiveSmoke(int lifetime){z.TileState.WriteCloud(5,5,"smoke",2);int version=z.TileState.SightVersion;z.TileState.WriteCloud(5,5,"steam",lifetime);Assert.True(z.TileState.ObscuresSight(5,5));Assert.AreEqual(version,z.TileState.SightVersion);}
  [TestCase("Smoke")][TestCase("SMOKE")][TestCase("smoky")][TestCase("steam")]
  public void OnlyAuthoredSmokeIdIsOpaque(string id){z.TileState.WriteCloud(5,5,id,2);Assert.False(z.TileState.ObscuresSight(5,5));Assert.Zero(z.TileState.SightVersion);}
  [Test]public void ThrowingVisibilityObserverCannotAbortStateMutationOrLaterExpiry(){z.TileState.OnSightChanged=()=>throw new InvalidOperationException("test observer");Assert.DoesNotThrow(()=>z.TileState.WriteCloud(5,5,"smoke",1));Assert.True(z.TileState.ObscuresSight(5,5));Assert.DoesNotThrow(()=>z.TileState.Tick());Assert.False(z.TileState.ObscuresSight(5,5));Assert.AreEqual(2,z.TileState.SightVersion);}
  [Test]public void ClearingInsideVisibilityObserverCannotResurrectSmoke(){int count=0;z.TileState.OnSightChanged=()=>{count++;z.TileState.Clear(5,5);};z.TileState.WriteCloud(5,5,"smoke",2);Assert.False(z.TileState.ObscuresSight(5,5));Assert.Zero(z.TileState.WrittenCount);Assert.AreEqual(2,count);}
  [Test]public void BatchExpiryRaisesOneInvalidationForManyClouds(){for(int i=0;i<20;i++)z.TileState.WriteCloud(i,5,"smoke",1);int version=z.TileState.SightVersion,calls=0;z.TileState.OnSightChanged=()=>calls++;z.TileState.Tick();Assert.AreEqual(version+1,z.TileState.SightVersion);Assert.AreEqual(1,calls);Assert.Zero(z.TileState.WrittenCount);}
  [Test]public void LoadingMalformedReplacementStillDropsOldShadow(){z.TileState.WriteCloud(5,5,"smoke",2);int version=z.TileState.SightVersion;z.TileState.LoadFromString("{");Assert.False(z.TileState.ObscuresSight(5,5));Assert.AreEqual(version+1,z.TileState.SightVersion);}
  [Test]public void ObserverFailureCannotHideSuccessfulLoad(){var saved=new Zone("saved");saved.TileState.WriteCloud(5,5,"smoke",2);z.TileState.OnSightChanged=()=>throw new InvalidOperationException("test observer");Assert.DoesNotThrow(()=>z.TileState.LoadFromString(saved.TileState.ToSaveString()));Assert.True(z.TileState.ObscuresSight(5,5));}
  [Test]public void EmptyClearDoesNotInvalidateSight(){int version=z.TileState.SightVersion;Assert.Zero(z.TileState.Clear(5,5));Assert.AreEqual(version,z.TileState.SightVersion);}
  [Test]public void UnrelatedClearLeavesSmokeEpochUntouched(){z.TileState.WriteCloud(5,5,"smoke",2);z.TileState.WriteCoating(6,5,"water",3);int version=z.TileState.SightVersion;z.TileState.Clear(6,5);Assert.AreEqual(version,z.TileState.SightVersion);Assert.True(z.TileState.ObscuresSight(5,5));}
  [Test]public void IndependentZonesDoNotShareOpacityVersion(){var other=new Zone("other");z.TileState.WriteCloud(5,5,"smoke",2);Assert.Zero(other.TileState.SightVersion);Assert.False(other.TileState.ObscuresSight(5,5));}
  [Test]public void WallRemainsOpaqueAfterSmokeClears(){var wall=new Entity();wall.SetTag("Wall");wall.SetTag("Solid");z.AddEntity(wall,5,5);z.TileState.WriteCloud(5,5,"smoke",1);z.TileState.Tick();FieldOfView.Compute(z,3,5,10);Assert.False(z.GetCell(7,5).IsVisible);Assert.False(AIHelpers.HasLineOfSight(z,3,5,7,5));}
  [Test]public void BindingSameZoneTwiceDoesNotDuplicateVisibilityNotification(){var old=ZoneRenderHooks.FullDirtyCallback;int count=0;try{ZoneRenderHooks.FullDirtyCallback=_=>count++;ZoneTileStateSystem.BindRenderHook(z);ZoneTileStateSystem.BindRenderHook(z);z.TileState.WriteCloud(5,5,"smoke",2);Assert.AreEqual(1,count);}finally{ZoneRenderHooks.FullDirtyCallback=old;}}
 }
}
