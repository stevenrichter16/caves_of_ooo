using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHuntDetachedReceiptTests
 {
  static void Claim(SpreadGenerationReceipt r){Assert.True((bool)typeof(SpreadGenerationReceipt).GetMethod("TryConsume",HuntFixture.All).Invoke(r,null));}
  static bool Matches(SpreadGenerationReceipt r,ISet<Entity> removed){var m=typeof(SpreadGenerationReceipt).GetMethod("MatchesOwnedState",HuntFixture.All,null,new[]{typeof(ISet<Entity>)},null);Assert.NotNull(m,"Preserve exact original owned actor graphs across our intentional root detach.");return(bool)m.Invoke(r,new object[]{removed});}
  static Func<bool> Removed(SpreadGenerationReceipt r,Entity owner){var m=typeof(SpreadGenerationReceipt).GetMethod("CaptureDetachedOwnerState",HuntFixture.All);Assert.NotNull(m);return(Func<bool>)m.Invoke(r,new object[]{owner});}
  [Test]public void PartialAndCompleteGroupDetachPreserveTheExactOriginalGraphs()
  {using(var f=new HuntFixture()){var r=f.Population.SourceReceipt;Claim(r);var removed=new HashSet<Entity>();Assert.True(Matches(r,removed));foreach(var owner in r.Owners){Assert.True(f.Zone.RemoveEntity(owner));removed.Add(owner);Assert.True(Matches(r,removed));}foreach(var owner in r.Owners){Assert.IsNotNull(Removed(r,owner));Assert.True(Removed(r,owner)());}}}
  [Test]public void PretendingAStillPlacedOwnerIsDetachedOrOmittingARemovedOwnerRefuses()
  {using(var f=new HuntFixture()){var r=f.Population.SourceReceipt;Claim(r);var owner=r.Owners[0];Assert.False(Matches(r,new HashSet<Entity>{owner}));Assert.True(f.Zone.RemoveEntity(owner));Assert.False(Matches(r,new HashSet<Entity>()));Assert.True(Matches(r,new HashSet<Entity>{owner}));}}
  [Test]public void RemovedActorProofStillPinsActualCarriedEquipmentState()
  {using(var f=new HuntFixture()){var r=f.Population.SourceReceipt;Claim(r);var owner=r.Owners[0];var item=DensityLootTestScope.Gear(owner).First();var p=item.GetPart<PhysicsPart>();var proof=Removed(r,owner);Assert.True(f.Zone.RemoveEntity(owner));Assert.True(proof());p.Weight++;Assert.False(proof());p.Weight--;Assert.True(proof());}}
  [Test]public void SameIdReplacementInTheOriginalZoneCannotValidateDetachedAuthority()
  {using(var f=new HuntFixture()){var r=f.Population.SourceReceipt;Claim(r);var owner=r.Owners[0];var at=f.Zone.GetEntityPosition(owner);var proof=Removed(r,owner);Assert.True(f.Zone.RemoveEntity(owner));Assert.True(proof());var foreign=f.Factory.CreateEntity("Viper");foreign.ID=owner.ID;Assert.True(f.Zone.AddEntity(foreign,at.x,at.y));Assert.False(proof());Assert.True(f.Zone.RemoveEntity(foreign));Assert.True(proof());}}
  [Test]public void TransferOfRemovedOwnerToAnotherZoneInvalidatesTheProof()
  {using(var f=new HuntFixture()){var r=f.Population.SourceReceipt;Claim(r);var owner=r.Owners[0];var proof=Removed(r,owner);Assert.True(f.Zone.RemoveEntity(owner));Assert.True(proof());var other=new Zone("foreign");Assert.True(other.AddEntity(owner,5,5));Assert.False(proof());}}
  [Test]public void AForeignOwnerCannotObtainARemovalProofFromThisReceipt()
  {using(var f=new HuntFixture()){var r=f.Population.SourceReceipt;Claim(r);var foreign=f.Factory.CreateEntity("MarlbackScrabbler");var proof=Removed(r,foreign);Assert.True(proof==null||!proof());Assert.True(Matches(r,new HashSet<Entity>()));}}
 }
}
