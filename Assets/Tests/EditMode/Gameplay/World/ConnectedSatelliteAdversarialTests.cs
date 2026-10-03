using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class ConnectedSatelliteAdversarialTests
 {
  public sealed class Created:Part{public static Action<Entity> Callback;public override void Initialize(){Callback?.Invoke(ParentEntity);}}
  static void Observe(ConnectedSatelliteTests.Fixture f,string bp,Action<Entity> callback)
  {f.Factory.RegisterPartType<Created>("SatelliteCreated");f.Factory.Blueprints[bp].Parts["SatelliteCreated"]=new Dictionary<string,string>();Created.Callback=callback;}
  [TearDown]public void Clear()=>Created.Callback=null;
  [Test]public void AuthorityRevokedDuringCreationLeavesNoSupplementOrMovedOwner()
  {using(var f=new ConnectedSatelliteTests.Fixture()){bool allowed=true;var original=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(original);Observe(f,"StoneWall",e=>allowed=false);Assert.False(f.Place("WetCrossing",()=>allowed));Assert.True(proof());CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());}}
  [Test]public void LaterFactoryCallbackCannotChangeAnEarlierStagedWall()
  {using(var f=new ConnectedSatelliteTests.Fixture()){Entity first=null;var original=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(original);Observe(f,"StoneWall",e=>{if(first==null)first=e;else first.GetPart<PhysicsPart>().Solid=false;});Assert.False(f.Place("HeavyFrame"));Assert.True(proof());CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());}}
  [Test]public void ANewSupplementCannotAliasAnOriginalStockID()
  {using(var f=new ConnectedSatelliteTests.Fixture()){var original=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(original);var item=f.Containers.SourceReceipt.Owners.SelectMany(e=>e.GetPart<ContainerPart>().Contents).First();Observe(f,"DitchkeepersFootwork",e=>e.ID=item.ID);Assert.False(f.Place("HeavyFrame"));Assert.True(proof());CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());}}
  [Test]public void SourceMutationIsDetectedWithoutOverwritingTheIndependentChange()
  {using(var f=new ConnectedSatelliteTests.Fixture()){var cache=f.Containers.SourceReceipt.Owners[0];var original=f.Zone.GetReadOnlyEntities().ToArray();var at=original.ToDictionary(e=>e,f.Zone.GetEntityPosition);Observe(f,"StoneWall",e=>cache.GetPart<ContainerPart>().Locked=true);Assert.False(f.Place("WetCrossing"));Assert.True(cache.GetPart<ContainerPart>().Locked);CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());foreach(var e in original)Assert.AreEqual(at[e],f.Zone.GetEntityPosition(e));}}
  [TestCase("WetCrossing")][TestCase("HeavyFrame")]
  public void LateRefusalRestoresStockPositionsAndEveryAddedPoolProjection(string family)
  {using(var f=new ConnectedSatelliteTests.Fixture()){var original=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(original);string tiles=f.Zone.TileState.ToSaveString();Assert.False(f.Place(family,()=>!f.Zone.GetReadOnlyEntities().Any(e=>e.Properties.ContainsKey("ConnectedSatellite.Role"))));Assert.True(proof());CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());}}
  [Test]public void FinalPacketRejectsAConsumedManualAndAReblockedRequiredApproach()
  {using(var f=new ConnectedSatelliteTests.Fixture()){Assert.True(f.Place("HeavyFrame"));var cache=f.Owners.Single(e=>e.HasPart<ContainerPart>());var manual=cache.GetPart<ContainerPart>().Contents.Single(e=>e.BlueprintName=="DitchkeepersFootwork");Assert.True(cache.GetPart<ContainerPart>().RemoveItem(manual));Assert.False(f.Final());}}
  [Test]public void AFullOriginalContainerIsNeverExpandedOrItsContentsOverwritten()
  {using(var f=new ConnectedSatelliteTests.Fixture(capacity:1)){Assert.True(f.Containers.SourceReceipt.IsCurrent);Assert.True(f.Containers.SourceReceipt.Owners.All(e=>e.GetPart<ContainerPart>().Contents.Count==1&&e.GetPart<ContainerPart>().MaxItems==1));var original=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(original);Assert.False(f.Place("HeavyFrame"));Assert.True(proof());CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());}}
 }
}
