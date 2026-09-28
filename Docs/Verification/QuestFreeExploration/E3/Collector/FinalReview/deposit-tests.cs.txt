using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCollectorDepositPoseTests
 {
  [TestCase(true)][TestCase(false)]
  public void ActualFullHomeMergePlaysDepositWhileRefusedFullHomeKeepsCarry(bool compatible)
  {
   using(var f=new SpreadCollectorCarryTests.Fixture("Hatchet"))
   {
    var resident=f.F.Factory.CreateEntity(compatible?"Hatchet":"Cudgel");Assert.NotNull(resident);resident.GetPart<StackerPart>().StackCount=2;
    var container=f.Home.GetPart<ContainerPart>();Assert.IsEmpty(container.Contents);Assert.True(container.AddItem(resident));container.MaxItems=1;
    Assert.AreEqual(compatible,resident.GetPart<StackerPart>().CanStackWith(f.Item));var original=f.Item;string residentId=resident.ID;
    f.Pick();Assert.True(f.View(out var carried));Assert.AreEqual("Pickup",f.ActionState);f.Act();f.F.Refresh();
    Assert.AreSame(original,f.Item);Assert.AreSame(resident,container.Contents[0]);Assert.AreEqual(residentId,resident.ID);Assert.AreEqual(1,container.Contents.Count);
    if(compatible)
    {
     Assert.AreEqual("Deposited",f.Phase);Assert.AreEqual(3,resident.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,original.GetPart<StackerPart>().StackCount);
     Assert.IsNull(original.GetPart<PhysicsPart>().InInventory);Assert.IsNull(f.Carried);Assert.False(f.View(out _));SpawnRing3DIntegrationFixture.Hidden(carried);
     Assert.AreEqual("Deposit",f.ActionState,"A real same-owner whole-stack deposit can merge into its actual resident; the exhausted incoming ID need not remain in Contents.");
    }
    else
    {
     Assert.AreEqual("Stopped",f.Phase);Assert.AreEqual(2,resident.GetPart<StackerPart>().StackCount);Assert.AreEqual(1,original.GetPart<StackerPart>().StackCount);
     Assert.AreSame(original,f.Carried);Assert.AreSame(f.Actor,original.GetPart<PhysicsPart>().InInventory);Assert.True(f.View(out var retained));Assert.AreSame(carried,retained);f.Proof();Assert.AreNotEqual("Deposit",f.ActionState);
    }
   }
  }
 }
}
