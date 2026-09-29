using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadGenerationReceiptValidationTests
 {
  static Func<bool> Proof(Zone zone,params Entity[] owners)=>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{zone,owners});
  static Entity Add(Zone z,string id,int x){var e=new Entity{ID=id,BlueprintName="fixture"};e.AddPart(new PhysicsPart());Assert.True(z.AddEntity(e,x,5));return e;}
  [Test]public void SelectedDuplicateAddedAndRemovedIsRecountedOnEveryProofInvocation()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var b=Add(z,"b",6);var proof=Proof(z,a,b);Assert.True(proof());var duplicate=Add(z,new string(new[]{'a'}),7);Assert.False(proof());Assert.True(z.RemoveEntity(duplicate));Assert.True(proof());}
  [Test]public void UnrelatedDuplicateIdsDoNotBroadenTheSelectedOwnerContract()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var b=Add(z,"b",6);var proof=Proof(z,a,b);Add(z,"other",7);Add(z,new string("other".ToCharArray()),8);Assert.True(proof());var duplicate=Add(z,"a",9);Assert.False(proof());Assert.True(z.RemoveEntity(duplicate));Assert.True(proof());}
  [Test]public void SelectedIdMutationCannotReuseItsEarlierUniqueCount()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var b=Add(z,"b",6);var proof=Proof(z,a,b);Assert.True(proof());a.ID="changed";Assert.False(proof());a.ID="a";Assert.True(proof());a.ID="b";Assert.False(proof());a.ID="a";Assert.True(proof());}
  [Test]public void SelectedMovementIsPinnedWhileUnselectedMovementIsPermitted()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var b=Add(z,"b",6);var other=Add(z,"other",7);var proof=Proof(z,a,b);Assert.True(proof());Assert.True(z.MoveEntity(other,8,5));Assert.True(proof());Assert.True(z.MoveEntity(a,9,5));Assert.False(proof());Assert.True(z.MoveEntity(a,5,5));Assert.True(proof());}
  [Test]public void SelectedNullIdRetainsItsExactOneOwnerRule()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var b=Add(z,"b",6);a.ID=null;var proof=Proof(z,a,b);Assert.True(proof());var duplicate=Add(z,"duplicate",7);duplicate.ID=null;Assert.False(proof());duplicate.ID="duplicate";Assert.True(proof());}
  [Test]public void UnrelatedNullIdsDoNotInvalidateASelectedNonNullPacket()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var proof=Proof(z,a);var one=Add(z,"one",7);var two=Add(z,"two",8);one.ID=two.ID=null;Assert.True(proof());two.ID="a";Assert.False(proof());two.ID=null;Assert.True(proof());}
  [Test]public void FreshIdCountsDoNotReplaceTheCapturedPartAndStateProof()
  {var z=new Zone("fixture");var a=Add(z,"a",5);var b=Add(z,"b",6);var proof=Proof(z,a,b);var part=a.GetPart<PhysicsPart>();Assert.True(proof());part.Weight++;Assert.False(proof());part.Weight--;Assert.True(proof());a.RemovePart(part);a.AddPart(new PhysicsPart());Assert.False(proof());}
 }
}
