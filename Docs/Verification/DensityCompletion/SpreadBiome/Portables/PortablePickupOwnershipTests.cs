using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class PortablePickupOwnershipTests
 {
  [Test] public void BareBlueprintPlayerAutoEquipsExactPickedUpDaggerThenDropUnequipsIt()
  {
   using(var f=new DensityLootTestScope())
   {
    var player=f.Factory.CreateEntity("Player");var zone=new Zone("portable-pickup-proof");var item=f.Factory.CreateEntity("Dagger");
    Assert.IsEmpty(player.GetPart<InventoryPart>().Objects);Assert.IsEmpty(player.GetPart<InventoryPart>().EquippedItems);
    Assert.True(zone.AddEntity(player,10,10));Assert.True(zone.AddEntity(item,10,10));
    Assert.True(InventorySystem.Pickup(player,item,zone));Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
    Assert.AreSame(player,item.GetPart<PhysicsPart>().Equipped);Assert.AreSame(item,player.GetPart<InventoryPart>().FindEquippedBodyPart(item)._Equipped);
    Assert.AreEqual(1,item.GetPart<StackerPart>().StackCount);Assert.IsNull(zone.GetEntityCell(item));
    Assert.True(InventorySystem.Drop(player,item,zone));Assert.AreSame(item,zone.GetCell(10,10).Objects.Single(e=>ReferenceEquals(e,item)));
    Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
   }
  }
  [Test] public void BothOccupiedHandsLeaveSamePickedUpItemCarried()
  {
   using(var f=new DensityLootTestScope())
   {
    var player=f.Factory.CreateEntity("Player");var inv=player.GetPart<InventoryPart>();var two=f.Factory.CreateEntity("Greatsword");Assert.True(inv.AddObject(two));Assert.True(InventorySystem.Equip(player,two));
    var zone=new Zone("portable-occupied-proof");var item=f.Factory.CreateEntity("Dagger");Assert.True(zone.AddEntity(player,10,10));Assert.True(zone.AddEntity(item,10,10));
    Assert.True(InventorySystem.Pickup(player,item,zone));Assert.AreSame(player,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);Assert.True(inv.Objects.Contains(item));
   }
  }
  [Test] public void ExistingCarriedDaggerReallyMergesAndDoesNotPreserveSourceOwnership()
  {
   using(var f=new DensityLootTestScope())
   {
    var player=f.Factory.CreateEntity("Player");var inv=player.GetPart<InventoryPart>();var resident=f.Factory.CreateEntity("Dagger");Assert.True(inv.AddObject(resident));
    var zone=new Zone("portable-merge-proof");var item=f.Factory.CreateEntity("Dagger");Assert.True(zone.AddEntity(player,10,10));Assert.True(zone.AddEntity(item,10,10));
    Assert.True(InventorySystem.Pickup(player,item,zone));Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
    Assert.AreEqual(0,item.GetPart<StackerPart>().StackCount);Assert.AreEqual(2,resident.GetPart<StackerPart>().StackCount);Assert.AreSame(player,resident.GetPart<PhysicsPart>().InInventory);
   }
  }
 }
}
